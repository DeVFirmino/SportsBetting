using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess;

namespace Integration.Test;

[Collection(SqlServerCollection.Name)]
public sealed class PlaceBetUseCaseTests
{
    private readonly SqlServerFixture _fixture;

    public PlaceBetUseCaseTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ShouldDebitWalletAndPersistBetInTheSameCommitWhenBetIsPlaced()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        BetResponse response = await Execute(provider, stake: 30m, "key-1");

        response.Id.Should().BeGreaterThan(0);

        await using SportsBettingDbContext verification = _fixture.NewContext();
        Wallet wallet = await verification.Wallets.AsNoTracking().SingleAsync(item => item.Id == walletId);
        Bet bet = await verification.Bets.AsNoTracking().SingleAsync(item => item.UserId == user.Id);

        wallet.Balance.Should().Be(70m);
        bet.Stake.Should().Be(30m);
        bet.Market.Should().Be(BettingMarket.HomeWin);
        bet.Odds.Should().Be(2.10m);
        bet.PotentialReturn.Should().Be(63m);
    }

    [Fact]
    public async Task ShouldReturnStoredBetWithoutSecondDebitWhenIdempotencyKeyIsReplayed()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        BetResponse first = await Execute(provider, stake: 25m, "key-1");
        BetResponse replay = await Execute(provider, stake: 25m, "key-1");

        replay.Id.Should().Be(first.Id);

        await using SportsBettingDbContext verification = _fixture.NewContext();
        (await verification.Bets.CountAsync(item => item.UserId == user.Id)).Should().Be(1);
        (await verification.Wallets.SingleAsync(item => item.Id == walletId)).Balance.Should().Be(75m);
    }

    [Fact]
    public async Task ShouldPersistOneBetWhenConcurrentRequestsUseTheSameIdempotencyKey()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        CommitBarrier barrier = new(participants: 2);
        await using ServiceProvider provider = _fixture.BuildProvider(
            user,
            new FootballApiStub(),
            barrier.SignalAndWaitAsync);

        Task<BetResponse> first = Execute(provider, stake: 30m, "shared-key");
        Task<BetResponse> second = Execute(provider, stake: 30m, "shared-key");
        BetResponse[] responses = await Task.WhenAll(first, second);

        responses.Select(response => response.Id).Distinct().Should().ContainSingle();

        await using SportsBettingDbContext verification = _fixture.NewContext();
        (await verification.Bets.CountAsync(item => item.UserId == user.Id)).Should().Be(1);
        (await verification.Wallets.SingleAsync(item => item.Id == walletId)).Balance.Should().Be(70m);
    }

    [Fact]
    public async Task ShouldPreventOverdrawWhenConcurrentBetsUseDifferentIdempotencyKeys()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 50m);
        CommitBarrier barrier = new(participants: 2);
        await using ServiceProvider provider = _fixture.BuildProvider(
            user,
            new FootballApiStub(),
            barrier.SignalAndWaitAsync);

        Task<BetResponse> first = Execute(provider, stake: 40m, "key-1");
        Task<BetResponse> second = Execute(provider, stake: 40m, "key-2");

        Func<Task> act = () => Task.WhenAll(first, second);

        await act.Should().ThrowAsync<ConcurrencyException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();
        (await verification.Bets.CountAsync(item => item.UserId == user.Id)).Should().Be(1);
        (await verification.Wallets.SingleAsync(item => item.Id == walletId)).Balance.Should().Be(10m);
    }

    private static async Task<BetResponse> Execute(
        ServiceProvider provider,
        decimal stake,
        string idempotencyKey)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IPlaceBetUseCase useCase = scope.ServiceProvider.GetRequiredService<IPlaceBetUseCase>();

        return await useCase.Execute(
            new PlaceBetRequest
            {
                FixtureId = FootballApiStub.FixtureId,
                Stake = stake,
                Market = SportsBetting.Communication.Enums.BettingMarket.HomeWin,
            },
            idempotencyKey,
            CancellationToken.None);
    }
}

file sealed class CommitBarrier
{
    private readonly TaskCompletionSource _release = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private int _remaining;

    public CommitBarrier(int participants)
    {
        _remaining = participants;
    }

    public async Task SignalAndWaitAsync()
    {
        if (Interlocked.Decrement(ref _remaining) == 0)
            _release.TrySetResult();

        await _release.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}

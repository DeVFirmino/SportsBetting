using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess;

namespace Integration.Test;

/// <summary>
/// Drives the registered <see cref="IPlaceBetUseCase"/> against SQL Server. The guarantees under
/// test — one atomic commit, a rowversion that stops a lost update, and an idempotency key that
/// survives a concurrent claim — only exist in the database, so nothing here re-implements them.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class PlaceBetUseCaseTests
{
    private readonly SqlServerFixture _fixture;

    public PlaceBetUseCaseTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ShouldDebitTheWalletRecordTheLedgerEntryAndTheBetInOneCommitWhenTheBetIsPlaced()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        BetResponse response = await Execute(provider, amount: 30m, idempotencyKey: null);

        response.Id.Should().BeGreaterThan(0);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(70m);

        Bet bet = await verification.Bets.AsNoTracking().SingleAsync(entity => entity.UserId == user.Id);
        bet.Amount.Should().Be(30m);
        bet.Status.Should().Be(BetStatus.Pending);

        WalletTransaction entry = await verification.WalletTransactions.AsNoTracking()
            .SingleAsync(transaction => transaction.WalletId == walletId);

        entry.Type.Should().Be(WalletTransactionType.BetDebit);
        entry.Amount.Should().Be(30m);
        entry.BalanceAfter.Should().Be(70m);
        entry.BetId.Should().Be(bet.Id);
    }

    [Fact]
    public async Task ShouldReturnTheStoredBetWhenTheSameIdempotencyKeyIsSentAgain()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());
        string key = Guid.NewGuid().ToString();

        BetResponse first = await Execute(provider, amount: 25m, key);
        BetResponse replay = await Execute(provider, amount: 25m, key);

        replay.Id.Should().Be(first.Id);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == user.Id)).Should().Be(1);
        (await verification.WalletTransactions.AsNoTracking().CountAsync(entry => entry.WalletId == walletId))
            .Should().Be(1);

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(75m);
    }

    [Fact]
    public async Task ShouldReplayTheWinnerWhenTheIdempotencyKeyIsClaimedWhileTheRequestIsInFlight()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        string key = Guid.NewGuid().ToString();

        // The pre-read cannot see a bet that does not exist yet. Claiming the key while the use
        // case is out at API-Football reproduces the exact interleaving the unique index guards,
        // which used to reach the caller as a 500 instead of a replay.
        long winnerId = 0;
        FootballApiStub stub = new(async () =>
        {
            if (winnerId != 0)
                return;

            winnerId = await ClaimKeyFromAnotherConnection(user.Id, key);
        });

        await using ServiceProvider provider = _fixture.BuildProvider(user, stub);

        BetResponse response = await Execute(provider, amount: 30m, key);

        response.Id.Should().Be(winnerId);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == user.Id)).Should().Be(1);

        // The losing request rolled back whole: no second debit and no orphan ledger entry.
        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(100m);
        (await verification.WalletTransactions.AsNoTracking().AnyAsync(entry => entry.WalletId == walletId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReplayTheWinnerWhenTheWinnerAlsoDebitedTheWalletUnderTheSameKey()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        string key = Guid.NewGuid().ToString();

        // The realistic duplicate: the winning request committed its bet AND its wallet debit, so
        // the loser can fail on the wallet's rowversion instead of the unique index. Whichever of
        // the two shapes surfaces, the caller must get the winner back — not a 409 and not a 500.
        long winnerId = 0;
        await using ServiceProvider provider = _fixture.BuildProvider(
            user,
            new FootballApiStub(),
            beforeCommit: async () =>
            {
                if (winnerId != 0)
                    return;

                winnerId = await ClaimKeyAndDebitFromAnotherConnection(user.Id, walletId, key);
            });

        BetResponse response = await Execute(provider, amount: 30m, key);

        response.Id.Should().Be(winnerId);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == user.Id)).Should().Be(1);

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(70m);

        (await verification.WalletTransactions.AsNoTracking().CountAsync(entry => entry.WalletId == walletId))
            .Should().Be(1);
    }

    [Fact]
    public async Task ShouldRejectTheReplayWhenTheSameKeyCarriesADifferentAmount()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());
        string key = Guid.NewGuid().ToString();

        await Execute(provider, amount: 25m, key);

        // Reusing the key with a different payload must not answer with the stored bet as if the
        // new amount had been accepted.
        Func<Task> replayWithDifferentAmount = () => Execute(provider, amount: 30m, key);

        await replayWithDifferentAmount.Should().ThrowAsync<ErrorOnValidationException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == user.Id)).Should().Be(1);

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(75m);
    }

    [Fact]
    public async Task ShouldPersistNothingWhenTheWalletMovedUnderTheRequest()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);

        // Another request commits a debit after this one read the wallet but before it writes.
        // The rowversion catches the lost update, and the whole commit — debit, ledger entry and
        // bet — has to fall away with it.
        bool moved = false;
        await using ServiceProvider provider = _fixture.BuildProvider(
            user,
            new FootballApiStub(),
            beforeCommit: async () =>
            {
                if (moved)
                    return;

                moved = true;
                await DebitFromAnotherConnection(walletId, 5m);
            });

        Func<Task> place = () => Execute(provider, amount: 30m, idempotencyKey: null);

        await place.Should().ThrowAsync<ConcurrencyException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(95m);

        (await verification.Bets.AsNoTracking().AnyAsync(bet => bet.UserId == user.Id)).Should().BeFalse();
        (await verification.WalletTransactions.AsNoTracking().AnyAsync(entry => entry.WalletId == walletId))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ShouldRejectTheRequestWhenTheIdempotencyKeyIsLongerThanTheColumn()
    {
        (User user, _) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        Func<Task> place = () => Execute(provider, amount: 10m, idempotencyKey: new string('k', 129));

        // A client-supplied header that is too long is a bad request, not a failed INSERT.
        await place.Should().ThrowAsync<ErrorOnValidationException>();
    }

    [Fact]
    public async Task ShouldPlaceSeveralBetsWhenNoIdempotencyKeyIsSupplied()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 100m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        await Execute(provider, amount: 10m, idempotencyKey: null);
        await Execute(provider, amount: 10m, idempotencyKey: null);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        // The unique index is filtered on a non-null key, so opting out of idempotency must not
        // collapse into one bet per user.
        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == user.Id)).Should().Be(2);

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(80m);
    }

    [Fact]
    public async Task ShouldRejectTheBetWhenTheWalletCannotCoverIt()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 5m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        Func<Task> place = () => Execute(provider, amount: 30m, idempotencyKey: null);

        await place.Should().ThrowAsync<ErrorOnValidationException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(5m);
    }

    private static async Task<BetResponse> Execute(
        ServiceProvider provider,
        decimal amount,
        string? idempotencyKey)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IPlaceBetUseCase useCase = scope.ServiceProvider.GetRequiredService<IPlaceBetUseCase>();

        PlaceBetRequest request = new()
        {
            FixtureId = FootballApiStub.FixtureId,
            Amount = amount,
            BetType = "HomeWin",
        };

        return await useCase.Execute(request, idempotencyKey, CancellationToken.None);
    }

    private async Task<long> ClaimKeyFromAnotherConnection(long userId, string clientRequestId)
    {
        await using SportsBettingDbContext context = _fixture.NewContext();

        // The winner mirrors the request under test: a replay only answers for a key stored with
        // the same payload.
        Bet winner = Bet.Place(
            userId,
            FootballApiStub.FixtureId,
            amount: 30m,
            BetType.HomeWin,
            odds: 2.5m,
            eventName: "Home FC vs Away FC",
            clientRequestId,
            DateTime.UtcNow);

        await context.Bets.AddAsync(winner);
        await context.SaveChangesAsync();

        return winner.Id;
    }

    private async Task<long> ClaimKeyAndDebitFromAnotherConnection(
        long userId,
        long walletId,
        string clientRequestId)
    {
        await using SportsBettingDbContext context = _fixture.NewContext();

        Bet winner = Bet.Place(
            userId,
            FootballApiStub.FixtureId,
            amount: 30m,
            BetType.HomeWin,
            odds: 2.5m,
            eventName: "Home FC vs Away FC",
            clientRequestId,
            DateTime.UtcNow);

        Wallet wallet = await context.Wallets.FirstAsync(entity => entity.Id == walletId);
        WalletTransaction entry = wallet.Debit(30m, winner);

        await context.Bets.AddAsync(winner);
        await context.WalletTransactions.AddAsync(entry);
        await context.SaveChangesAsync();

        return winner.Id;
    }

    private async Task DebitFromAnotherConnection(long walletId, decimal amount)
    {
        await using SportsBettingDbContext context = _fixture.NewContext();

        Wallet wallet = await context.Wallets.FirstAsync(entity => entity.Id == walletId);
        wallet.Balance -= amount;

        await context.SaveChangesAsync();
    }
}

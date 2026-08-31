using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess;

namespace Integration.Test;

/// <summary>
/// Money in is as replayable as money out: two deposits of the same amount are legitimately
/// distinct, so only the client key can tell a retry from a second deposit, and only the database
/// can enforce it.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class DepositUseCaseTests
{
    private readonly SqlServerFixture _fixture;

    public DepositUseCaseTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ShouldCreateTheWalletAndRecordTheLedgerEntryWhenTheFirstDepositArrives()
    {
        (User user, _) = await _fixture.SeedAsync(balance: null);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        await Execute(provider, amount: 75m, idempotencyKey: null);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().SingleAsync(entity => entity.UserId == user.Id);
        wallet.Balance.Should().Be(75m);

        WalletTransaction entry = await verification.WalletTransactions.AsNoTracking()
            .SingleAsync(transaction => transaction.WalletId == wallet.Id);

        entry.Type.Should().Be(WalletTransactionType.Deposit);
        entry.Amount.Should().Be(75m);
        entry.BalanceAfter.Should().Be(75m);
        entry.BetId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldCreditOnceWhenTheSameIdempotencyKeyIsSentAgain()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 20m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());
        string key = Guid.NewGuid().ToString();

        await Execute(provider, amount: 50m, key);
        await Execute(provider, amount: 50m, key);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(70m);

        (await verification.WalletTransactions.AsNoTracking().CountAsync(entry => entry.WalletId == walletId))
            .Should().Be(1);
    }

    [Fact]
    public async Task ShouldCreditOnceWhenTheKeyIsClaimedWhileTheRequestIsInFlight()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 20m);
        string key = Guid.NewGuid().ToString();

        // A concurrent retry commits first. The loser must collide with the ledger's unique index
        // and settle as a no-op rather than crediting the balance a second time.
        bool claimed = false;
        await using ServiceProvider provider = _fixture.BuildProvider(
            user,
            new FootballApiStub(),
            beforeCommit: async () =>
            {
                if (claimed)
                    return;

                claimed = true;
                await DepositFromAnotherConnection(walletId, 50m, key);
            });

        await Execute(provider, amount: 50m, key);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(70m);

        (await verification.WalletTransactions.AsNoTracking().CountAsync(entry => entry.WalletId == walletId))
            .Should().Be(1);
    }

    [Fact]
    public async Task ShouldCreditTwiceWhenNoIdempotencyKeyIsSupplied()
    {
        (User user, long walletId) = await _fixture.SeedAsync(balance: 0m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        await Execute(provider, amount: 10m, idempotencyKey: null);
        await Execute(provider, amount: 10m, idempotencyKey: null);

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(20m);

        (await verification.WalletTransactions.AsNoTracking().CountAsync(entry => entry.WalletId == walletId))
            .Should().Be(2);
    }

    [Fact]
    public async Task ShouldRejectTheRequestWhenTheIdempotencyKeyIsLongerThanTheColumn()
    {
        (User user, _) = await _fixture.SeedAsync(balance: 10m);
        await using ServiceProvider provider = _fixture.BuildProvider(user, new FootballApiStub());

        Func<Task> deposit = () => Execute(provider, amount: 10m, idempotencyKey: new string('k', 129));

        await deposit.Should().ThrowAsync<ErrorOnValidationException>();
    }

    private static async Task Execute(ServiceProvider provider, decimal amount, string? idempotencyKey)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IDepositUseCase useCase = scope.ServiceProvider.GetRequiredService<IDepositUseCase>();

        await useCase.Execute(new DepositRequest { Amount = amount }, idempotencyKey, CancellationToken.None);
    }

    private async Task DepositFromAnotherConnection(long walletId, decimal amount, string clientRequestId)
    {
        await using SportsBettingDbContext context = _fixture.NewContext();

        Wallet wallet = await context.Wallets.FirstAsync(entity => entity.Id == walletId);

        await context.WalletTransactions.AddAsync(wallet.Deposit(amount, clientRequestId));
        await context.SaveChangesAsync();
    }
}

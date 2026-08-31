using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess;

namespace Integration.Test;

/// <summary>
/// The wallet's rowversion is a SQL Server concurrency token, so it can only be proven against
/// SQL Server: the server is what moves the value on every UPDATE.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class WalletConcurrencyTests
{
    private readonly SqlServerFixture _fixture;

    public WalletConcurrencyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task ShouldThrowConcurrencyExceptionWhenAnotherWriterDebitedTheWalletFirst()
    {
        (long _, long walletId) = await _fixture.SeedFundedUserAsync(100m);

        await using SportsBettingDbContext loser = _fixture.NewContext();
        Wallet losingWallet = await loser.Wallets.FirstAsync(wallet => wallet.Id == walletId);
        losingWallet.Debit(10m);

        await using (SportsBettingDbContext winner = _fixture.NewContext())
        {
            Wallet winningWallet = await winner.Wallets.FirstAsync(wallet => wallet.Id == walletId);
            winningWallet.Debit(25m);
            await winner.SaveChangesAsync();
        }

        UnitOfWork unitOfWork = new(loser);

        Func<Task> commit = () => unitOfWork.CommitAsync(CancellationToken.None);

        await commit.Should().ThrowAsync<ConcurrencyException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();
        Wallet persisted = await verification.Wallets.AsNoTracking().FirstAsync(wallet => wallet.Id == walletId);

        // Only the writer that won the rowversion check moved the balance.
        persisted.Balance.Should().Be(75m);
    }

    [SqlServerFact]
    public async Task ShouldMoveRowVersionWhenTheWalletIsUpdated()
    {
        (long _, long walletId) = await _fixture.SeedFundedUserAsync(50m);

        byte[] before;

        await using (SportsBettingDbContext context = _fixture.NewContext())
        {
            Wallet wallet = await context.Wallets.FirstAsync(entity => entity.Id == walletId);
            before = wallet.RowVersion;

            wallet.Deposit(10m);
            await context.SaveChangesAsync();

            wallet.RowVersion.Should().NotEqual(before);
        }

        await using SportsBettingDbContext verification = _fixture.NewContext();
        Wallet persisted = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);

        persisted.Balance.Should().Be(60m);
        persisted.RowVersion.Should().NotEqual(before);
    }
}

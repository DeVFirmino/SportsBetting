using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.DataAccess.Repositories;

namespace Integration.Test;

/// <summary>
/// Placing a bet moves three rows: the wallet balance, an append-only ledger entry and the bet
/// itself. They are written in one <see cref="UnitOfWork.CommitAsync"/>, so either all three land
/// or none do. These tests check both outcomes against SQL Server.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class BetPlacementAtomicityTests
{
    private readonly SqlServerFixture _fixture;

    public BetPlacementAtomicityTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task ShouldPersistWalletDebitLedgerEntryAndBetTogetherWhenTheCommitSucceeds()
    {
        (long userId, long walletId) = await _fixture.SeedFundedUserAsync(100m);

        await using (SportsBettingDbContext context = _fixture.NewContext())
        {
            await PlaceBetAsync(context, userId, walletId, amount: 30m, clientRequestId: null);
            await new UnitOfWork(context).CommitAsync(CancellationToken.None);
        }

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(70m);

        List<Bet> bets = await verification.Bets.AsNoTracking()
            .Where(bet => bet.UserId == userId).ToListAsync();
        bets.Should().ContainSingle();

        List<WalletTransaction> ledger = await verification.WalletTransactions.AsNoTracking()
            .Where(entry => entry.UserId == userId).ToListAsync();

        ledger.Should().ContainSingle();
        ledger[0].Type.Should().Be(WalletTransactionType.BetDebit);
        ledger[0].Amount.Should().Be(30m);
        ledger[0].BalanceAfter.Should().Be(70m);
        ledger[0].BetId.Should().Be(bets[0].Id);
    }

    [SqlServerFact]
    public async Task ShouldPersistNothingWhenTheWalletUpdateLosesTheConcurrencyCheck()
    {
        (long userId, long walletId) = await _fixture.SeedFundedUserAsync(100m);

        await using SportsBettingDbContext context = _fixture.NewContext();
        await PlaceBetAsync(context, userId, walletId, amount: 30m, clientRequestId: null);

        // Another request commits first, moving the wallet's rowversion out from under this one.
        await using (SportsBettingDbContext winner = _fixture.NewContext())
        {
            Wallet winningWallet = await winner.Wallets.FirstAsync(entity => entity.Id == walletId);
            winningWallet.Debit(5m);
            await winner.SaveChangesAsync();
        }

        Func<Task> commit = () => new UnitOfWork(context).CommitAsync(CancellationToken.None);

        await commit.Should().ThrowAsync<ConcurrencyException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(95m);

        // The bet and the ledger entry were part of the same failed SaveChanges, so neither row
        // may exist: a bet without its debit, or a debit without its bet, would both be wrong.
        (await verification.Bets.AsNoTracking().AnyAsync(bet => bet.UserId == userId))
            .Should().BeFalse();
        (await verification.WalletTransactions.AsNoTracking().AnyAsync(entry => entry.UserId == userId))
            .Should().BeFalse();
    }

    /// <summary>
    /// Mirrors what <c>PlaceBetUseCase</c> stages before it commits: a tracked wallet debit, the
    /// ledger entry the debit produced, and the new bet.
    /// </summary>
    internal static async Task<Bet> PlaceBetAsync(
        SportsBettingDbContext context,
        long userId,
        long walletId,
        decimal amount,
        string? clientRequestId)
    {
        WalletRepository wallets = new(context);
        BetRepository bets = new(context);
        WalletTransactionRepository transactions = new(context);

        Wallet wallet = await wallets.GetByIdAsync(walletId, CancellationToken.None);

        Bet bet = Bet.Place(
            userId,
            fixtureId: 12345,
            amount,
            BetType.HomeWin,
            odds: 2.5m,
            eventName: "Home vs Away",
            clientRequestId,
            DateTime.UtcNow);

        WalletTransaction transaction = wallet.Debit(amount);
        transaction.Bet = bet;

        await transactions.AddAsync(transaction, CancellationToken.None);
        wallets.Update(wallet);
        await bets.AddAsync(bet, CancellationToken.None);

        return bet;
    }
}

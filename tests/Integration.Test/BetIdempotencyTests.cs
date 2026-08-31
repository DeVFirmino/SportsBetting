using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.DataAccess.Repositories;

namespace Integration.Test;

/// <summary>
/// Bet placement is idempotent per user and client request id. The guarantee has two halves: the
/// use case replays the stored bet when it recognises the key, and a filtered unique index in SQL
/// Server refuses a duplicate that slipped past the read — the second half only exists in the
/// database, so it is proven here.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class BetIdempotencyTests
{
    private readonly SqlServerFixture _fixture;

    public BetIdempotencyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task ShouldReturnTheOriginalBetWhenTheSameClientRequestIdIsReplayed()
    {
        (long userId, long walletId) = await _fixture.SeedFundedUserAsync(100m);
        string clientRequestId = Guid.NewGuid().ToString();

        long originalBetId;

        await using (SportsBettingDbContext context = _fixture.NewContext())
        {
            Bet bet = await BetPlacementAtomicityTests.PlaceBetAsync(
                context, userId, walletId, amount: 20m, clientRequestId);

            await new UnitOfWork(context).CommitAsync(CancellationToken.None);
            originalBetId = bet.Id;
        }

        await using SportsBettingDbContext replay = _fixture.NewContext();
        BetRepository bets = new(replay);

        Bet? replayed = await bets.GetByClientRequestIdAsync(userId, clientRequestId, CancellationToken.None);

        replayed.Should().NotBeNull();
        replayed!.Id.Should().Be(originalBetId);
        replayed.Amount.Should().Be(20m);

        // Deterministic replay means no second debit and no second ledger entry.
        Wallet wallet = await replay.Wallets.AsNoTracking().FirstAsync(entity => entity.Id == walletId);
        wallet.Balance.Should().Be(80m);

        (await replay.WalletTransactions.AsNoTracking().CountAsync(entry => entry.UserId == userId))
            .Should().Be(1);
    }

    [SqlServerFact]
    public async Task ShouldRejectASecondBetWhenTheClientRequestIdIsAlreadyUsedByTheSameUser()
    {
        (long userId, long walletId) = await _fixture.SeedFundedUserAsync(100m);
        string clientRequestId = Guid.NewGuid().ToString();

        await using (SportsBettingDbContext first = _fixture.NewContext())
        {
            await BetPlacementAtomicityTests.PlaceBetAsync(
                first, userId, walletId, amount: 20m, clientRequestId);
            await new UnitOfWork(first).CommitAsync(CancellationToken.None);
        }

        await using SportsBettingDbContext duplicate = _fixture.NewContext();
        await BetPlacementAtomicityTests.PlaceBetAsync(
            duplicate, userId, walletId, amount: 20m, clientRequestId);

        Func<Task> commit = () => new UnitOfWork(duplicate).CommitAsync(CancellationToken.None);

        await commit.Should().ThrowAsync<DbUpdateException>();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == userId))
            .Should().Be(1);
    }

    [SqlServerFact]
    public async Task ShouldAllowTheSameClientRequestIdWhenItBelongsToADifferentUser()
    {
        string clientRequestId = Guid.NewGuid().ToString();

        (long firstUserId, long firstWalletId) = await _fixture.SeedFundedUserAsync(100m);
        (long secondUserId, long secondWalletId) = await _fixture.SeedFundedUserAsync(100m);

        await using (SportsBettingDbContext first = _fixture.NewContext())
        {
            await BetPlacementAtomicityTests.PlaceBetAsync(
                first, firstUserId, firstWalletId, amount: 10m, clientRequestId);
            await new UnitOfWork(first).CommitAsync(CancellationToken.None);
        }

        await using SportsBettingDbContext second = _fixture.NewContext();
        await BetPlacementAtomicityTests.PlaceBetAsync(
            second, secondUserId, secondWalletId, amount: 10m, clientRequestId);

        Func<Task> commit = () => new UnitOfWork(second).CommitAsync(CancellationToken.None);

        await commit.Should().NotThrowAsync();
    }

    [SqlServerFact]
    public async Task ShouldAllowSeveralBetsWhenNoClientRequestIdIsSupplied()
    {
        (long userId, long walletId) = await _fixture.SeedFundedUserAsync(100m);

        // The unique index is filtered on a non-null ClientRequestId, so opting out of
        // idempotency must not collapse into a single-bet-per-user rule.
        for (int attempt = 0; attempt < 2; attempt++)
        {
            await using SportsBettingDbContext context = _fixture.NewContext();
            await BetPlacementAtomicityTests.PlaceBetAsync(
                context, userId, walletId, amount: 10m, clientRequestId: null);
            await new UnitOfWork(context).CommitAsync(CancellationToken.None);
        }

        await using SportsBettingDbContext verification = _fixture.NewContext();

        (await verification.Bets.AsNoTracking().CountAsync(bet => bet.UserId == userId))
            .Should().Be(2);
    }
}

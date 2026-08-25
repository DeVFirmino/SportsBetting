using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.DataAccess;

namespace UseCase.Test.Infrastructure;

/// <summary>
/// The wallet carries a concurrency token; these tests check that losing it actually reaches the
/// caller as a domain exception instead of an EF one. The conflict is a real database conflict:
/// a second connection updates the row, and the first write then matches nothing.
/// SQL Server moves a rowversion on its own. SQLite has no equivalent column, so the competing
/// writer here moves the token explicitly — what is under test, the losing UPDATE, is identical.
/// </summary>
public class UnitOfWorkConcurrencyTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"sportsbetting-{Guid.NewGuid():N}.db");

    public UnitOfWorkConcurrencyTests()
    {
        using var context = NewContext();
        context.Database.EnsureCreated();

        context.Database.ExecuteSqlRaw(
            "INSERT INTO Users (Id, Active, CreatedOn, Name, Email, Password, UserIdentifier) " +
            "VALUES (1, 1, '2026-01-01 00:00:00', 'Tester', 'tester@example.com', 'hash', '11111111-1111-1111-1111-111111111111')");

        context.Database.ExecuteSqlRaw(
            "INSERT INTO Wallets (Id, Active, CreatedOn, UserId, Balance, RowVersion) " +
            "VALUES (1, 1, '2026-01-01 00:00:00', 1, 100.00, X'01')");
    }

    [Fact]
    public async Task Commit_WhenAnotherWriterMovedTheWallet_ThrowsConcurrencyException()
    {
        using var context = NewContext();
        var wallet = await context.Wallets.FirstAsync();
        wallet.Balance -= 10m;                  // this bet's deduction, not saved yet

        AnotherBetSettlesFirst(newBalance: 40m);

        var unitOfWork = new UnitOfWork(context);

        await FluentActions.Awaiting(() => unitOfWork.Commit())
            .Should().ThrowAsync<ConcurrencyException>()
            .WithMessage(ResourcesMessagesException.CONCURRENT_BET_DETECTED);
    }

    [Fact]
    public async Task Commit_WhenAnotherWriterMovedTheWallet_LeavesTheWinningBalanceIntact()
    {
        using var context = NewContext();
        var wallet = await context.Wallets.FirstAsync();
        wallet.Balance -= 10m;

        AnotherBetSettlesFirst(newBalance: 40m);

        await FluentActions.Awaiting(() => new UnitOfWork(context).Commit())
            .Should().ThrowAsync<ConcurrencyException>();

        using var verification = NewContext();
        var stored = await verification.Wallets.FirstAsync();
        stored.Balance.Should().Be(40m);        // the loser did not overwrite the winner
    }

    [Fact]
    public async Task Commit_WhenNobodyElseTouchedTheWallet_Persists()
    {
        using var context = NewContext();
        var wallet = await context.Wallets.FirstAsync();
        wallet.Balance -= 10m;

        await new UnitOfWork(context).Commit();

        using var verification = NewContext();
        var stored = await verification.Wallets.FirstAsync();
        stored.Balance.Should().Be(90m);
    }

    private void AnotherBetSettlesFirst(decimal newBalance)
    {
        using var context = NewContext();
        context.Database.ExecuteSqlRaw(
            "UPDATE Wallets SET Balance = {0}, RowVersion = X'02' WHERE Id = 1", newBalance);
    }

    private SportsBettingDbContext NewContext() => new(
        new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlite($"Data Source={_path}")
            .Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
        GC.SuppressFinalize(this);
    }
}

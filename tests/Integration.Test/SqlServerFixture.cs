using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Infrastructure.DataAccess;
using Testcontainers.MsSql;

namespace Integration.Test;

/// <summary>
/// One SQL Server container shared by the whole collection. The schema comes from the versioned
/// Entity Framework Core migrations rather than <c>EnsureCreated</c>, so what the tests run
/// against is the same schema a deployment produces — including the rowversion column and the
/// filtered unique index that back concurrency and idempotency.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (SqlServerAvailability.IsAvailable is false)
            return;

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using SportsBettingDbContext context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (SqlServerAvailability.IsAvailable is false)
            return;

        await _container.DisposeAsync();
    }

    public SportsBettingDbContext NewContext()
    {
        DbContextOptions<SportsBettingDbContext> options =
            new DbContextOptionsBuilder<SportsBettingDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        return new SportsBettingDbContext(options);
    }

    /// <summary>
    /// Creates a user with a funded wallet. Every test gets its own so they can run in any order
    /// against the one container without seeing each other's rows.
    /// </summary>
    public async Task<(long UserId, long WalletId)> SeedFundedUserAsync(decimal balance)
    {
        await using SportsBettingDbContext context = NewContext();

        User user = new()
        {
            Name = "Integration Tester",
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "hash",
            UserIdentifier = Guid.NewGuid(),
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        Wallet wallet = new()
        {
            UserId = user.Id,
            Balance = balance,
        };

        await context.Wallets.AddAsync(wallet);
        await context.SaveChangesAsync();

        return (user.Id, wallet.Id);
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

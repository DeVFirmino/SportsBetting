using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SportsBetting.Application;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Infrastructure;
using SportsBetting.Infrastructure.DataAccess;
using Testcontainers.MsSql;

namespace Integration.Test;

/// <summary>
/// One SQL Server container shared by the collection. The schema comes from the versioned Entity
/// Framework Core migrations rather than <c>EnsureCreated</c>, so the rowversion column and the
/// unique index behind concurrency and idempotency are the ones a deployment produces.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using SportsBettingDbContext context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public SportsBettingDbContext NewContext()
    {
        DbContextOptions<SportsBettingDbContext> options =
            new DbContextOptionsBuilder<SportsBettingDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        return new SportsBettingDbContext(options);
    }

    /// <summary>
    /// Builds the application's real dependency graph against the container, replacing only the
    /// two edges a test cannot supply for itself: who is logged in, and what the upstream fixture
    /// feed answers. Everything under test — use cases, repositories, unit of work, mapper — is
    /// the registration the API runs.
    /// </summary>
    /// <param name="beforeCommit">
    /// Runs inside the real unit of work, immediately before it saves. It is the only way to make
    /// something happen in the window the wallet's rowversion exists to police.
    /// </param>
    public ServiceProvider BuildProvider(
        User loggedUser,
        IFootballApiService footballApi,
        Func<Task>? beforeCommit = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Settings:Jwt:SigningKey"] = "an-integration-test-signing-key-32",
                ["Settings:Jwt:Issuer"] = "sportsbetting-tests",
                ["Settings:Jwt:Audience"] = "sportsbetting-tests",
                ["Settings:Jwt:ExpirationTimeMinutes"] = "60",
                ["Settings:FootballApi:BaseUrl"] = "https://football.example",
                ["Settings:FootballApi:ApiKey"] = "test-api-key",
                ["Settings:FootballApi:CacheSeconds"] = "30",
                ["Settings:FootballApi:TimeoutSeconds"] = "10",
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);

        services.RemoveAll<ILoggedUser>();
        services.AddScoped<ILoggedUser>(_ => new FixedLoggedUser(loggedUser));

        services.RemoveAll<IFootballApiService>();
        services.AddSingleton(footballApi);

        if (beforeCommit is not null)
        {
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork>(provider => new InterceptingUnitOfWork(
                new UnitOfWork(provider.GetRequiredService<SportsBettingDbContext>()),
                beforeCommit));
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a user, optionally with a funded wallet. Every test seeds its own so they can run
    /// in any order against the single container without seeing each other's rows.
    /// </summary>
    public async Task<(User User, long WalletId)> SeedAsync(decimal balance)
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

        Wallet wallet = new() { UserId = user.Id, Balance = balance };

        await context.Wallets.AddAsync(wallet);
        await context.SaveChangesAsync();

        return (user, wallet.Id);
    }
}

/// <summary>
/// Wraps the real unit of work so a test can act in the instant before the commit. The commit
/// itself, and the concurrency translation it performs, stay production code.
/// </summary>
file sealed class InterceptingUnitOfWork : IUnitOfWork
{
    private readonly IUnitOfWork _inner;
    private readonly Func<Task> _beforeCommit;

    public InterceptingUnitOfWork(IUnitOfWork inner, Func<Task> beforeCommit)
    {
        _inner = inner;
        _beforeCommit = beforeCommit;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _beforeCommit();
        await _inner.CommitAsync(cancellationToken);
    }
}

file sealed class FixedLoggedUser : ILoggedUser
{
    private readonly User _user;

    public FixedLoggedUser(User user)
    {
        _user = user;
    }

    public Task<User> GetUserAsync(CancellationToken cancellationToken) => Task.FromResult(_user);
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

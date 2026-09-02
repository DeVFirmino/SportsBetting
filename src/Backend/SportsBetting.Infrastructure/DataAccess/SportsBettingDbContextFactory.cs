using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SportsBetting.Infrastructure.DataAccess;

/// <summary>
/// Design-time factory for the Entity Framework Core tooling. The connection string is required
/// from the environment: a default here would be a credential in source, and it would let a
/// mistyped command run against whatever happens to be listening locally.
/// </summary>
public sealed class SportsBettingDbContextFactory : IDesignTimeDbContextFactory<SportsBettingDbContext>
{
    private const string ConnectionVariable = "SPORTSBETTING_EF_CONNECTION";

    public SportsBettingDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable(ConnectionVariable)
            ?? throw new InvalidOperationException(
                $"Set {ConnectionVariable} to the target database before running the Entity Framework Core tools.");

        DbContextOptions<SportsBettingDbContext> options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SportsBettingDbContext(options);
    }
}

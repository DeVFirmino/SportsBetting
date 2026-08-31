using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SportsBetting.Infrastructure.DataAccess;

public sealed class SportsBettingDbContextFactory : IDesignTimeDbContextFactory<SportsBettingDbContext>
{
    public SportsBettingDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("SPORTSBETTING_EF_CONNECTION")
            ?? "Server=localhost,1434;Database=SportsBetting;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;Application Name=SportsBetting;";

        DbContextOptions<SportsBettingDbContext> options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new SportsBettingDbContext(options);
    }
}

using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Infrastructure.DataAccess;

public class SportsBettingDbContext : DbContext
{
    public SportsBettingDbContext(DbContextOptions<SportsBettingDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    
    public DbSet<Wallet> Wallets { get; set; }
    
    public DbSet<Bet> Bets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportsBettingDbContext).Assembly);
    }
    
    
}
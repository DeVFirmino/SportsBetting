using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Infrastructure.DataAcess;

public class SportsBettingDbContext : DbContext
{
    public SportsBettingDbContext(DbContextOptions options) : base(options) { }
    
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportsBettingDbContext).Assembly);
    }
    
    
}
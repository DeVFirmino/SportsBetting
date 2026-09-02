using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Infrastructure.DataAccess;

public sealed class SportsBettingDbContext : DbContext
{
    internal const string BetIdempotencyIndexName = "UX_Bets_UserId_IdempotencyKey";

    public SportsBettingDbContext(DbContextOptions<SportsBettingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Bet> Bets => Set<Bet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder.Entity<User>());
        ConfigureWallet(modelBuilder.Entity<Wallet>());
        ConfigureBet(modelBuilder.Entity<Bet>());
    }

    private static void ConfigureUser(EntityTypeBuilder<User> user)
    {
        user.ToTable("Users");
        user.HasKey(entity => entity.Id);

        user.Property(entity => entity.Email)
            .HasMaxLength(255)
            .IsRequired();

        user.HasIndex(entity => entity.Email)
            .IsUnique();

        user.Property(entity => entity.Password)
            .HasMaxLength(255)
            .IsRequired();
    }

    private static void ConfigureWallet(EntityTypeBuilder<Wallet> wallet)
    {
        wallet.ToTable("Wallets");
        wallet.HasKey(entity => entity.Id);

        wallet.Property(entity => entity.Balance)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0)
            .IsRequired();

        wallet.Property(entity => entity.RowVersion)
            .IsRowVersion()
            .IsRequired();

        wallet.HasOne(entity => entity.User)
            .WithOne()
            .HasForeignKey<Wallet>(entity => entity.UserId);

        wallet.HasIndex(entity => entity.UserId)
            .IsUnique();
    }

    private static void ConfigureBet(EntityTypeBuilder<Bet> bet)
    {
        bet.ToTable("Bets");
        bet.HasKey(entity => entity.Id);

        bet.Property(entity => entity.Stake)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        bet.Property(entity => entity.Odds)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        bet.Property(entity => entity.PotentialReturn)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        bet.Property(entity => entity.EventName)
            .HasMaxLength(255)
            .IsRequired();

        bet.Property(entity => entity.Market)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        bet.Property(entity => entity.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        bet.HasOne(entity => entity.User)
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        bet.HasIndex(entity => entity.UserId);
        bet.HasIndex(entity => entity.FixtureId);
        bet.HasIndex(entity => new { entity.UserId, entity.IdempotencyKey })
            .HasDatabaseName(BetIdempotencyIndexName)
            .IsUnique();
    }
}

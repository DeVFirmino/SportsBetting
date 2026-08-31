using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Infrastructure.DataAccess;

public sealed class SportsBettingDbContext : DbContext
{
    public SportsBettingDbContext(DbContextOptions<SportsBettingDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<Bet> Bets => Set<Bet>();

    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUser(modelBuilder.Entity<User>());
        ConfigureWallet(modelBuilder.Entity<Wallet>());
        ConfigureBet(modelBuilder.Entity<Bet>());
        ConfigureWalletTransaction(modelBuilder.Entity<WalletTransaction>());
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

        bet.Property(entity => entity.FixtureId)
            .IsRequired();

        bet.Property(entity => entity.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        bet.Property(entity => entity.Odds)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        bet.Property(entity => entity.PotentialWinning)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        bet.Property(entity => entity.EventName)
            .HasMaxLength(255)
            .IsRequired();

        bet.Property(entity => entity.BetType)
            .HasMaxLength(100)
            .IsRequired();

        bet.Property(entity => entity.Status)
            .HasDefaultValue(BetStatus.Pending)
            .IsRequired();

        bet.Property(entity => entity.ClientRequestId)
            .HasMaxLength(128);

        bet.HasOne(entity => entity.User)
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        bet.HasIndex(entity => entity.UserId);
        bet.HasIndex(entity => entity.FixtureId);
        bet.HasIndex(entity => new { entity.UserId, entity.ClientRequestId })
            .IsUnique()
            .HasFilter("[ClientRequestId] IS NOT NULL");
    }

    private static void ConfigureWalletTransaction(EntityTypeBuilder<WalletTransaction> transaction)
    {
        transaction.ToTable("WalletTransactions");
        transaction.HasKey(entity => entity.Id);

        transaction.Property(entity => entity.Type)
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        transaction.Property(entity => entity.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        transaction.Property(entity => entity.BalanceAfter)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        transaction.Property(entity => entity.OccurredAt)
            .IsRequired();

        transaction.HasOne(entity => entity.User)
            .WithMany()
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        transaction.HasOne(entity => entity.Bet)
            .WithMany()
            .HasForeignKey(entity => entity.BetId)
            .OnDelete(DeleteBehavior.Restrict);

        transaction.HasIndex(entity => entity.UserId);
        transaction.HasIndex(entity => entity.BetId);
    }
}

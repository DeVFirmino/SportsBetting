using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Infrastructure.DataAccess.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets").HasKey(w => w.Id);
        
        builder.Property(w => w.Balance).HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);
        
        builder.HasOne(w => w.User)
            .WithOne()
            .HasForeignKey<Wallet>(w => w.UserId);

        builder.HasIndex(w => w.UserId).IsUnique();
    }
}
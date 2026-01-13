using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Infrastructure.DataAccess.Configurations;

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("Bets");
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.FixtureId).IsRequired();
 
        builder.Property(b => b.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(b => b.Odds)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(b => b.PotentialWinning)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        builder.Property(b => b.EventName).HasMaxLength(255).IsRequired();

        builder.Property(b => b.BetType).HasMaxLength(100).IsRequired();
        
        builder.Property(b => b.Status)
            .HasDefaultValue(BetStatus.Pending)
            .IsRequired();
        
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.UserId);
        builder.HasIndex(b => b.FixtureId);

    }
    
}
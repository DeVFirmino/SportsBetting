using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Infrastructure.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
         builder.ToTable("Users");
         builder.HasKey(x => x.Id);

         builder.Property(x => x.Email).
             HasMaxLength(255)
             .IsRequired(); 
         
         builder.HasIndex(x => x.Email).IsUnique();
        
         builder.Property(x => x.Password)
             .IsRequired()
             .HasMaxLength(255);
          
         
    }
}
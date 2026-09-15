using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Users;
using Domain.Companies;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<Domain.Users.User>
{
    public void Configure(EntityTypeBuilder<Domain.Users.User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.Plan).HasConversion<int>().HasDefaultValue(Domain.Users.Plan.Free);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.LastLoginAt);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.CompanyId);
        builder.HasIndex(u => u.Plan);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

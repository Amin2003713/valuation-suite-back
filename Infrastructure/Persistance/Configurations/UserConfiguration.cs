using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Identity already maps the base user columns; add our extension properties only.
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.Plan).HasConversion<int>().HasDefaultValue(Plan.Free);
        builder.Property(u => u.PlanExpiresAt);
        builder.Property(u => u.LastLoginAt);

        builder.HasIndex(u => u.Plan);
        builder.HasIndex(u => u.CompanyId);

        builder.HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.Attempts)
            .WithOne()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

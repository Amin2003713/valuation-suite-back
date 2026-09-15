using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Companies;

namespace Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Industry).HasMaxLength(200);
        builder.Property(c => c.LogoUrl).HasMaxLength(500);

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.Name);

        builder.Property(c => c.Plan).HasConversion<int>().HasDefaultValue(Plan.Free);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasMany(c => c.Assessments)
            .WithOne()
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

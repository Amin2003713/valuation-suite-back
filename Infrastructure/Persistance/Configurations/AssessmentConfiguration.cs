using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Assessments;
using Domain.Companies;

namespace Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Code).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Description).HasMaxLength(1000);

        builder.HasIndex(a => new { a.Code, a.CompanyId }).IsUnique();
        builder.HasIndex(a => a.CompanyId);
        builder.HasIndex(a => a.IsPublished);
        builder.HasIndex(a => a.IsArchived);

        builder.Property(a => a.IsPublished).HasDefaultValue(false);
        builder.Property(a => a.IsArchived).HasDefaultValue(false);
        builder.Property(a => a.PublishedVersion).HasDefaultValue(0);

        builder.HasOne<Company>()
            .WithMany(c => c.Assessments)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

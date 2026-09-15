using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Assessments;

namespace Infrastructure.Persistence.Configurations;

public class AssessmentVersionConfiguration : IEntityTypeConfiguration<AssessmentVersion>
{
    public void Configure(EntityTypeBuilder<AssessmentVersion> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Title).HasMaxLength(200);
        builder.Property(v => v.VersionNumber).IsRequired();

        builder.HasIndex(v => new { v.AssessmentId, v.VersionNumber }).IsUnique();
        builder.HasIndex(v => v.IsPublished);
        builder.HasIndex(v => v.IsDraft);

        builder.Property(v => v.IsDraft).HasDefaultValue(true);
        builder.Property(v => v.IsPublished).HasDefaultValue(false);

        builder.HasOne<Assessment>()
            .WithMany(a => a.Versions)
            .HasForeignKey(v => v.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Steps)
            .WithOne()
            .HasForeignKey("VersionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

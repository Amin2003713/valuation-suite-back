using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Assessments;

namespace Persistence.Configurations;

public class StepConfiguration : IEntityTypeConfiguration<Step>
{
    public void Configure(EntityTypeBuilder<Step> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Order).IsRequired();

        builder.HasIndex(s => new { s.VersionId, s.Order }).IsUnique();

        builder.HasOne(s => s.Version)
            .WithMany(v => v.Steps)
            .HasForeignKey(s => s.VersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

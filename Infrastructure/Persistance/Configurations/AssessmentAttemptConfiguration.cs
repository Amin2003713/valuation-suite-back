using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Attempts;
using Domain.Assessments;
using Domain.Users;

namespace Persistence.Configurations;

public class AssessmentAttemptConfiguration : IEntityTypeConfiguration<AssessmentAttempt>
{
    public void Configure(EntityTypeBuilder<AssessmentAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).HasConversion<int>().HasDefaultValue(AttemptStatus.InProgress);
        builder.Property(a => a.TotalSteps).HasDefaultValue(0);
        builder.Property(a => a.CompletedSteps).HasDefaultValue(0);
        builder.Property(a => a.ClientRevision).HasDefaultValue(0);

        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.VersionId);
        builder.HasIndex(a => new { a.UserId, a.VersionId });

        builder.HasOne<AssessmentVersion>()
            .WithMany()
            .HasForeignKey(a => a.VersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.Attempts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

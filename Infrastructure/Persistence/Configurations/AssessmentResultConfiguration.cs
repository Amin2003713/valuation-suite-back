using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Results;
using Domain.Attempts;
using Domain.Assessments;

namespace Infrastructure.Persistence.Configurations;

public class AssessmentResultConfiguration : IEntityTypeConfiguration<AssessmentResult>
{
    public void Configure(EntityTypeBuilder<AssessmentResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.OverallScore).HasDefaultValue(0);
        builder.Property(r => r.Level).HasMaxLength(50);
        builder.Property(r => r.LevelLabel).HasMaxLength(100);
        builder.Property(r => r.IsCalculated).HasDefaultValue(false);
        builder.Property(r => r.CalculatedAt);

        builder.HasIndex(r => r.AttemptId).IsUnique();
        builder.HasIndex(r => r.AssessmentId);
        builder.HasIndex(r => r.IsCalculated);

        builder.HasOne<AssessmentAttempt>()
            .WithOne(a => a.Result)
            .HasForeignKey<AssessmentResult>(r => r.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Assessment>()
            .WithMany()
            .HasForeignKey(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(r => r.ScoredAnswers, sa =>
        {
            sa.WithOwner().HasForeignKey("ResultId");
        });
        builder.Ignore(r => r.Metadata);
        builder.Ignore(r => r.StepScores);
    }
}

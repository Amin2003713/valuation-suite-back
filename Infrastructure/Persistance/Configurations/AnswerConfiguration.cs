using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Answers;
using Domain.Attempts;

namespace Persistence.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ValueType).HasConversion<int>().HasDefaultValue(AnswerValueType.Text);
        builder.Property(a => a.TextValue).HasMaxLength(4000);
        builder.Property(a => a.JsonData).HasMaxLength(4000);
        builder.Property(a => a.ClientRevision).HasDefaultValue(0);

        builder.HasIndex(a => a.AttemptId);
        builder.HasIndex(a => a.AttemptId).IsUnique();
        builder.HasIndex(a => a.SynchronizedAt);

        builder.HasOne<AssessmentAttempt>()
            .WithMany(a => a.Answers)
            .HasForeignKey("AttemptId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

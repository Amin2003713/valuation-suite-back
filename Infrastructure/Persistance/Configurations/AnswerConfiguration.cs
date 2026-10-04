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

        // An attempt holds MANY answers (one per question). The previous model
        // declared a unique index on AttemptId alone, which would reject the
        // second answer synced for an attempt — the correct key is
        // (AttemptId, QuestionId). Keep a plain FK index for lookups by attempt.
        builder.HasIndex(a => new { a.AttemptId, a.QuestionId }).IsUnique();
        builder.HasIndex(a => a.AttemptId);
        builder.HasIndex(a => a.SynchronizedAt);

        builder.HasOne(a => a.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey("AttemptId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

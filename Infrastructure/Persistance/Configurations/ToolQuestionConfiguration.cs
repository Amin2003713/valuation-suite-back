using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ToolQuestionConfiguration : IEntityTypeConfiguration<ToolQuestion>
{
    public void Configure(EntityTypeBuilder<ToolQuestion> builder)
    {
        builder.Property(q => q.ToolCode).IsRequired().HasMaxLength(50);
        builder.Property(q => q.QuestionId).IsRequired().HasMaxLength(100);
        builder.Property(q => q.SectionKey).HasMaxLength(50);
        builder.Property(q => q.SectionTitle).HasMaxLength(200);
        builder.Property(q => q.Text).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.OptionsJson).HasMaxLength(4000);

        // One row per (tool, question id); lookups always filter by ToolCode.
        builder.HasIndex(q => new { q.ToolCode, q.QuestionId }).IsUnique();
        builder.HasIndex(q => new { q.ToolCode, q.SortOrder });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Assessments;

namespace Persistence.Configurations;

public class OptionConfiguration : IEntityTypeConfiguration<Option>
{
    public void Configure(EntityTypeBuilder<Option> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Label).IsRequired().HasMaxLength(500);
        builder.Property(o => o.Value).HasMaxLength(500);
        builder.Property(o => o.Order).IsRequired();
        builder.Property(o => o.IsCorrect).HasDefaultValue(false);
        builder.Property(o => o.Score).HasDefaultValue(0.0);

        builder.HasOne<Question>()
            .WithMany(q => q.Options)
            .HasForeignKey("QuestionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

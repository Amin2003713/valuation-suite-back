using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Assessments;
using Domain.Common;

namespace Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Text).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.Key).HasMaxLength(100);
        builder.Property(q => q.Type).HasConversion<int>().HasDefaultValue(QuestionType.Text);
        builder.Property(q => q.Order).IsRequired();
        builder.Property(q => q.IsRequired).HasDefaultValue(false);
        builder.Property(q => q.HelpText).HasMaxLength(1000);

        builder.HasIndex(q => q.StepId);
        builder.HasIndex(q => q.Key);
        builder.HasIndex(q => q.Type);

        builder.HasOne<Step>()
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.StepId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Options)
            .WithOne()
            .HasForeignKey("QuestionId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Validations)
            .WithOne()
            .HasForeignKey("QuestionId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Assessments;
using Domain.Common;

namespace Persistence.Configurations;

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

        builder.HasOne(q => q.Step)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.StepId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Options)
            .WithOne()
            .HasForeignKey("QuestionId")
            .OnDelete(DeleteBehavior.Cascade);

        // ── Value objects (owned types) ──────────────────────────────
        // ValidationRule / VisibilityCondition: per-question value objects.
        builder.OwnsMany(q => q.Validations, v =>
        {
            v.Property(x => x.Field).IsRequired().HasMaxLength(200);
            v.Property(x => x.Type).HasConversion<int>().HasDefaultValue(ValidationType.Required);
            v.Property(x => x.Pattern).HasMaxLength(500);
            v.Property(x => x.ErrorMessage).HasMaxLength(500);
        });
        builder.OwnsMany(q => q.VisibilityConditions, vc =>
        {
            vc.Property(x => x.TargetQuestionId).IsRequired().HasMaxLength(100);
            vc.Property(x => x.Type).HasConversion<int>().HasDefaultValue(VisibilityConditionType.Equals);
            vc.Property(x => x.Value).HasMaxLength(500);
        });
        // CalculationConfig: owned reference, collections serialized to JSON columns.
        builder.OwnsOne(q => q.Calculation, calc =>
        {
            calc.Property(c => c.OutputKey).IsRequired().HasMaxLength(200);
            calc.Property(c => c.Expression).IsRequired().HasMaxLength(1000);
            calc.Property(c => c.ResultLabel).HasMaxLength(200);
            calc.Property(c => c.InputQuestionIds).HasConversion(
                ids => string.Join('|', ids),
                s => s.Length == 0 ? new List<string>() : s.Split('|').ToList());
            calc.Property(c => c.Weights).HasConversion(
                w => System.Text.Json.JsonSerializer.Serialize(w, (System.Text.Json.JsonSerializerOptions?)null),
                s => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(s, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, double>());
        });
        // MathExpression: owned reference with nested owned collections.
        builder.OwnsOne(q => q.MathExpression, me =>
        {
            me.Property(m => m.Expression).IsRequired().HasMaxLength(1000);
            me.Property(m => m.PostfixNotation).HasMaxLength(1000);
            me.OwnsMany(m => m.Variables, mv =>
            {
                mv.Property(x => x.Name).HasMaxLength(100);
                mv.Property(x => x.Label).HasMaxLength(200);
            });
            me.OwnsMany(m => m.Operations, mo =>
            {
                mo.Property(x => x.Type).HasConversion<int>();
                mo.Property(x => x.Operand).HasMaxLength(100);
            });
        });
    }
}

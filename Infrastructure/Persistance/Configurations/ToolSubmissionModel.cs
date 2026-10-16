using Domain.Tools;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

/// <summary>
///     Shared table definitions for every per-tool table family:
///     <list type="bullet">
///         <item><c>&lt;Tool&gt;Submissions</c> — user runs/answers (ToolSubmissionBase),</item>
///         <item><c>&lt;Tool&gt;Questions</c> — input definitions (ToolQuestionBase),</item>
///         <item><c>&lt;Tool&gt;Localizations</c> — per-language tool info + question labels
///             (ToolLocalizationBase).</item>
///     </list>
///     EF maps each concrete subclass to its own table; applying the same configuration
///     to every registered type keeps the "common base definition" in one place —
///     adding a tool contributes two/three empty subclasses and gets every column,
///     index and FK rule for free.
/// </summary>
public static class ToolSubmissionModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        foreach (var type in ToolRegistry.SubmissionTypes)
            modelBuilder.Entity(type, builder => ConfigureSubmission(builder));

        foreach (var type in ToolRegistry.QuestionTypes)
            modelBuilder.Entity(type, builder => ConfigureQuestion(builder));

        foreach (var type in ToolRegistry.LocalizationTypes)
            modelBuilder.Entity(type, builder => ConfigureLocalization(builder));
    }

    private static void ConfigureSubmission(EntityTypeBuilder builder)
    {
        // Column rules mirror the legacy ToolSubmission configuration 1:1 so the
        // per-tool tables are drop-in replacements for the shared table.
        builder.Property(nameof(ToolSubmissionBase.ToolCode)).IsRequired().HasMaxLength(50);
        builder.Property(nameof(ToolSubmissionBase.Name)).HasMaxLength(200);
        builder.Property(nameof(ToolSubmissionBase.Language)).IsRequired().HasMaxLength(10);
        builder.Property(nameof(ToolSubmissionBase.Status)).IsRequired();

        builder.HasIndex(nameof(ToolSubmissionBase.UserId));
        builder.HasIndex(nameof(ToolSubmissionBase.UserId), nameof(ToolSubmissionBase.ToolCode));
        builder.HasIndex(nameof(ToolSubmissionBase.CreatedAt));
        // Industry benchmark (spider-web radar) aggregates filter by industry.
        builder.HasIndex(nameof(ToolSubmissionBase.Industry));

        builder.HasOne(typeof(ApplicationUser))
            .WithMany()
            .HasForeignKey(nameof(ToolSubmissionBase.UserId))
            .OnDelete(DeleteBehavior.ClientSetNull);
    }

    private static void ConfigureQuestion(EntityTypeBuilder builder)
    {
        // Column rules mirror the legacy ToolQuestion configuration.
        builder.Property(nameof(ToolQuestionBase.ToolCode)).IsRequired().HasMaxLength(50);
        builder.Property(nameof(ToolQuestionBase.QuestionId)).IsRequired().HasMaxLength(100);
        builder.Property(nameof(ToolQuestionBase.SectionKey)).HasMaxLength(50);
        builder.Property(nameof(ToolQuestionBase.SectionTitle)).HasMaxLength(200);
        builder.Property(nameof(ToolQuestionBase.Text)).IsRequired().HasMaxLength(2000);
        builder.Property(nameof(ToolQuestionBase.OptionsJson)).HasMaxLength(4000);
        builder.Property(nameof(ToolQuestionBase.Placeholder)).HasMaxLength(500);
        builder.Property(nameof(ToolQuestionBase.HelpText)).HasMaxLength(2000);
        builder.Property(nameof(ToolQuestionBase.AnswerType)).IsRequired().HasMaxLength(20);

        // One row per question id inside the tool's own table.
        builder.HasIndex(nameof(ToolQuestionBase.QuestionId)).IsUnique();
        builder.HasIndex(nameof(ToolQuestionBase.SortOrder));
    }

    private static void ConfigureLocalization(EntityTypeBuilder builder)
    {
        builder.Property(nameof(ToolLocalizationBase.ToolCode)).IsRequired().HasMaxLength(50);
        builder.Property(nameof(ToolLocalizationBase.Language)).IsRequired().HasMaxLength(10);
        // Tool-level info rows use the "" sentinel (see ToolLocalizationBase.ToolLevelQuestionId)
        // so the unique index below also constrains them.
        builder.Property(nameof(ToolLocalizationBase.QuestionId)).IsRequired().HasMaxLength(100);
        builder.Property(nameof(ToolLocalizationBase.Title)).HasMaxLength(200);
        builder.Property(nameof(ToolLocalizationBase.Description)).HasMaxLength(1000);
        builder.Property(nameof(ToolLocalizationBase.Text)).HasMaxLength(2000);
        builder.Property(nameof(ToolLocalizationBase.SectionTitle)).HasMaxLength(200);
        builder.Property(nameof(ToolLocalizationBase.Placeholder)).HasMaxLength(500);
        builder.Property(nameof(ToolLocalizationBase.HelpText)).HasMaxLength(2000);
        builder.Property(nameof(ToolLocalizationBase.OptionsJson)).HasMaxLength(4000);

        // One row per (language, question) inside the tool's own table.
        builder.HasIndex(nameof(ToolLocalizationBase.Language), nameof(ToolLocalizationBase.QuestionId)).IsUnique();
    }
}

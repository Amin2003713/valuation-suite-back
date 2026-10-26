using Domain.Tools;

namespace Application.Interfaces;

/// <summary>
///     Read model over the per-tool <c>&lt;Tool&gt;Questions</c> tables (one table per tool).
///     <see cref="All"/> unions every tool's table; <see cref="ByTool"/> hits only the
///     tool's own table. Rows mirror <see cref="ToolQuestionBase"/> so query bodies port
///     1:1 from the legacy shared ToolQuestions table.
/// </summary>
public interface IToolQuestionQueries
{
    IQueryable<ToolQuestionRow> All { get; }
    IQueryable<ToolQuestionRow> ByTool(string toolCode);
}

/// <summary>Union-compatible projection of a per-tool question row.</summary>
public sealed class ToolQuestionRow
{
    public Guid Id { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
    public string? SectionKey { get; set; }
    public string? SectionTitle { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string OptionsJson { get; set; } = "[]";
    public string AnswerType { get; set; } = "text";
    public bool IsRequired { get; set; }
    public int? ScaleMin { get; set; }
    public int? ScaleMax { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
///     Read model over the per-tool <c>&lt;Tool&gt;Localizations</c> tables.
///     One row per (language, question) — QuestionId = "" is the tool-level info row.
/// </summary>
public interface IToolContentQueries
{
    IQueryable<ToolLocalizationRow> All { get; }
    IQueryable<ToolLocalizationRow> ByTool(string toolCode);
}

/// <summary>Union-compatible projection of a per-tool localization row.</summary>
public sealed class ToolLocalizationRow
{
    public Guid Id { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Text { get; set; }
    public string? SectionTitle { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? OptionsJson { get; set; }
}

/// <summary>
///     Tracked write access to per-tool question rows (admin content editing).
///     Loads the real entity so SaveChanges persists admin text/option edits.
/// </summary>
public interface IToolQuestionStore
{
    /// <summary>Tracked row for (toolCode, questionId), or null.</summary>
    Task<ToolQuestionBase?> FindAsync(string toolCode, string questionId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
///     Tracked write access to per-tool localization rows (admin bilingual edits).
///     Creates runtime-typed rows for a tool's own localization table.
/// </summary>
public interface IToolLocalizationStore
{
    /// <summary>Tracked row for (toolCode, language, questionId), or null.</summary>
    Task<ToolLocalizationBase?> FindAsync(
        string toolCode, string language, string questionId, CancellationToken ct = default);

    /// <summary>Creates and tracks a new localization row for the tool/language.</summary>
    ToolLocalizationBase Create(string toolCode, string language, string questionId);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

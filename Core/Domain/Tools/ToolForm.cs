using Common.Base;

namespace Domain.Tools;

public enum ToolKind
{
    /// <summary>Question/answer based assessment scored by the backend.</summary>
    Assessment = 1,
    /// <summary>Typed-input calculator (valuation engine).</summary>
    Calculator = 2,
}

/// <summary>
///     A tool registered by the backend. The client is pure UI: it renders the form
///     described by <see cref="SchemaJson"/> (defaults, labels, reference data) and sends
///     user input to the tool's run endpoint. All math lives in the backend.
/// </summary>
public sealed class ToolForm : BaseEntity
{
    public string ToolCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ToolKind Kind { get; set; }
    /// <summary>JSON document with defaults, field metadata and static reference tables.</summary>
    public string SchemaJson { get; set; } = "{}";
    public int SortOrder { get; set; }

    public static ToolForm Create(string code, string title, string? description, ToolKind kind, string schemaJson, int sortOrder) =>
        new() { ToolCode = code, Title = title, Description = description, Kind = kind, SchemaJson = schemaJson, SortOrder = sortOrder };
}

/// <summary>
///     A persisted run/submission of a tool: the raw input, the backend-computed
///     result and a normalized 0-100 overall score (where the tool has one).
/// </summary>
public sealed class ToolSubmission : BaseEntity
{
    public Guid UserId { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string InputJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    /// <summary>Normalized 0-100 score when the tool produces one; null for pure calculators.</summary>
    public double? OverallScore { get; set; }

    public static ToolSubmission Create(Guid userId, string toolCode, string? name, string inputJson, string resultJson, double? overallScore) =>
        new() { UserId = userId, ToolCode = toolCode, Name = name, InputJson = inputJson, ResultJson = resultJson, OverallScore = overallScore };
}

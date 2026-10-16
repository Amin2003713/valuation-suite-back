using Domain.Tools;

namespace Application.Interfaces;

/// <summary>
///     Read model over the per-tool submission tables (one table per tool).
///     <see cref="All"/> unions every tool's table; <see cref="ByTool"/> hits only the
///     tool's own table. Rows are projected into <see cref="ToolSubmissionRow"/>, whose
///     property names mirror <see cref="ToolSubmissionBase"/> so existing query bodies
///     port 1:1. The legacy shared <c>ToolSubmissions</c> table is an archive and is
///     NOT part of these queries.
/// </summary>
public interface IToolSubmissionQueries
{
    /// <summary>UNION ALL over every per-tool submissions table.</summary>
    IQueryable<ToolSubmissionRow> All { get; }

    /// <summary>The single per-tool table for <paramref name="toolCode"/>.</summary>
    IQueryable<ToolSubmissionRow> ByTool(string toolCode);
}

/// <summary>Union-compatible projection of a per-tool submission row.</summary>
public sealed class ToolSubmissionRow
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Guid UserId { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string InputJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public double? OverallScore { get; set; }
    public string Language { get; set; } = "fa";
    public ToolSubmissionStatus Status { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
}

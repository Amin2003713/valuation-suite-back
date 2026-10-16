using Common.Base;

namespace Domain.Tools;

/// <summary>Lifecycle status of a stored tool run.</summary>
public enum ToolSubmissionStatus
{
    /// <summary>Run completed and the result was stored.</summary>
    Completed = 1,
}

/// <summary>
///     Common definition shared by every per-tool submission table.
///     Each tool owns its own table: a concrete subclass (<see cref="IdeaAssessmentSubmission"/>,
///     …) is mapped to its own table by EF (one entity type = one table), while every column,
///     index and audit rule lives here — adding a tool means one new empty subclass
///     (+ one migration), never a re-declared column.
///
///     The shape mirrors the legacy shared <see cref="ToolSubmission"/> row so every
///     existing query ports 1:1 onto the union read model (<c>ToolSubmissionRow</c>).
///     <see cref="ToolCode"/> is kept as a (redundant but constant) column so union rows
///     carry their code verbatim.
/// </summary>
public abstract class ToolSubmissionBase : BaseEntity
{
    public Guid UserId { get; set; }

    public string ToolCode { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string InputJson { get; set; } = "{}";

    public string ResultJson { get; set; } = "{}";

    /// <summary>Normalized 0-100 score when the tool produces one; null for pure calculators.</summary>
    public double? OverallScore { get; set; }

    /// <summary>The language the run was requested in ("fa" | "en") — submitted with the input.</summary>
    public string Language { get; set; } = "fa";

    /// <summary>Run status (see <see cref="ToolSubmissionStatus"/>).</summary>
    public ToolSubmissionStatus Status { get; set; } = ToolSubmissionStatus.Completed;

    /// <summary>Snapshot of the submitting user's company at run time (benchmark grouping).</summary>
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
}

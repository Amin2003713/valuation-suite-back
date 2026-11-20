using Common.Base;

namespace Domain.Watch;

/// <summary>
///     A saved watch query: what the company wants to be alerted about. The query
///     fields are stored as plain text (comma-separated) so a profile survives any
///     future source adapter without a schema change.
/// </summary>
public sealed class WatchProfile : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string? Applicants { get; set; }
    public string? Jurisdictions { get; set; }
    public string? NiceClasses { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
}

/// <summary>
///     One monitored filing found for a profile. Deduplicated on
///     (ProfileId, Source, ExternalId): importing the same source twice never
///     duplicates a hit, which is what makes scheduled re-runs safe.
/// </summary>
public sealed class WatchHit : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid WatchProfileId { get; set; }
    public string Source { get; set; } = "MANUAL";
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Applicant { get; set; }
    public string? PublicationNumber { get; set; }
    public DateTime? PublicationDate { get; set; }
    public string? Url { get; set; }
    public bool Seen { get; set; }
    public DateTime? SeenAt { get; set; }
}

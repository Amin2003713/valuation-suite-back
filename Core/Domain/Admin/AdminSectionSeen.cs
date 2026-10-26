using Common.Base;

namespace Domain.Admin;

/// <summary>
///     Per-staff "seen" marker for an admin area (new-submissions badge,
///     messaging inbox, adviser queue, ...). Opening/acknowledging a section
///     stores the acknowledgement time so counters only report what is newer.
/// </summary>
public sealed class AdminSectionSeen : BaseEntity
{
    /// <summary>The staff user that acknowledged the section.</summary>
    public Guid UserId { get; set; }

    /// <summary>Stable section key: submissions | customers | messaging | adviser-queue.</summary>
    public string SectionKey { get; set; } = string.Empty;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}

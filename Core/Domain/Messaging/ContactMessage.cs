using Common.Base;

namespace Domain.Messaging;

/// <summary>
///     A message submitted through the public /contact form. Read + triaged in
///     the admin area; nothing here is coupled to payments or pricing.
/// </summary>
public sealed class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>Optional subject — free text, kept verbatim.</summary>
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Privacy consent flag recorded at submission time (GDPR).</summary>
    public bool ConsentGiven { get; set; }

    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;

    /// <summary>Bare npm-style honeypot: bots filling a hidden field get dropped.</summary>
    public bool IsSpam { get; set; }

    /// <summary>Free-text internal note an admin can attach while triaging.</summary>
    public string? AdminNote { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public void MarkResolved(string? note)
    {
        Status = ContactMessageStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        if (note is not null) AdminNote = note;
    }

    public void MarkInProgress(string? note)
    {
        Status = ContactMessageStatus.InProgress;
        if (note is not null) AdminNote = note;
    }
}

public enum ContactMessageStatus
{
    New = 1,
    InProgress = 2,
    Resolved = 3,
    Spam = 4,
}

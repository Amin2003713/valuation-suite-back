using Common.Base;

namespace Domain.Tools;

/// <summary>
///     One message in the advice thread attached to a tool submission — the
///     "guidance" customers buy, plus the customer's follow-up questions.
///     Delivered as rich text and/or a voice recording (stored as base64 audio in
///     <see cref="AudioBase64"/>; small clips, a few minutes at most).
/// </summary>
public sealed class SubmissionNote : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public Guid AdviserUserId { get; set; }

    /// <summary>True when the customer wrote this message; false for staff/adviser guidance.</summary>
    public bool AuthorIsCustomer { get; set; }

    /// <summary>Free-text guidance (markdown-lite allowed).</summary>
    public string? Text { get; set; }

    /// <summary>Base64-encoded webm/mp3 voice clip, or null.</summary>
    public string? AudioBase64 { get; set; }

    /// <summary>Audio MIME type when AudioBase64 is set ("audio/webm", "audio/mp3", ...).</summary>
    public string? AudioMimeType { get; set; }

    /// <summary>Duration of the voice clip in seconds (client-measured).</summary>
    public int? AudioSeconds { get; set; }

    /// <summary>True once the customer has seen it (drives the unread badge).</summary>
    public bool SeenByCustomer { get; set; }

    public DateTime? SeenAt { get; set; }

    /// <summary>True once staff has read a customer reply (drives the adviser unread badge).</summary>
    public bool SeenByAdviser { get; set; }

    public DateTime? SeenByAdviserAt { get; set; }

    public static SubmissionNote Create(Guid submissionId, Guid adviserId, string? text, string? audio, string? audioMime, int? audioSeconds) =>
        new()
        {
            SubmissionId = submissionId,
            AdviserUserId = adviserId,
            AuthorIsCustomer = false,
            Text = text,
            AudioBase64 = audio,
            AudioMimeType = audioMime,
            AudioSeconds = audioSeconds,
        };

    /// <summary>Customer follow-up in the advice chat (text only).</summary>
    public static SubmissionNote CreateCustomerReply(Guid submissionId, Guid customerId, string? text) =>
        new()
        {
            SubmissionId = submissionId,
            AdviserUserId = customerId, // thread owner; the customer is the other side of the conversation
            AuthorIsCustomer = true,
            Text = text,
        };

    public void MarkSeen() { SeenByCustomer = true; SeenAt = DateTime.UtcNow; }

    public void MarkSeenByAdviser() { SeenByAdviser = true; SeenByAdviserAt = DateTime.UtcNow; }
}

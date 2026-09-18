using Common.Base;

namespace Domain.Tools;

/// <summary>
///     An adviser's note attached to a tool submission — the "guidance" customers buy.
///     Delivered as rich text and/or a voice recording (stored as base64 audio in
///     <see cref="AudioBase64"/>; small clips, a few minutes at most).
/// </summary>
public sealed class SubmissionNote : BaseEntity
{
    public Guid SubmissionId { get; set; }
    public Guid AdviserUserId { get; set; }

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

    public static SubmissionNote Create(Guid submissionId, Guid adviserId, string? text, string? audio, string? audioMime, int? audioSeconds) =>
        new()
        {
            SubmissionId = submissionId,
            AdviserUserId = adviserId,
            Text = text,
            AudioBase64 = audio,
            AudioMimeType = audioMime,
            AudioSeconds = audioSeconds,
        };

    public void MarkSeen() { SeenByCustomer = true; SeenAt = DateTime.UtcNow; }
}

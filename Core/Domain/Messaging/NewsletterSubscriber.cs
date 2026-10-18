using Common.Base;

namespace Domain.Messaging;

/// <summary>
///     A newsletter subscriber from the footer form. Re-subscribing an existing
///     email is idempotent; unsubscribing soft-deletes through IsActive, so an
///     address can come back later without losing history.
/// </summary>
public sealed class NewsletterSubscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Normalised store key: lower-case trimmed email — unique lookup.</summary>
    public string EmailKey { get; set; } = string.Empty;

    /// <summary>Source label ("footer", "contact", …) for analytics-lite attribution.</summary>
    public string Source { get; set; } = "footer";

    /// <summary>Privacy consent flag recorded at subscribe time (GDPR).</summary>
    public bool ConsentGiven { get; set; }

    public void Unsubscribe() => IsActive = false;

    public void Resubscribe() => IsActive = true;
}

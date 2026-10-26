using Common.Base;

namespace Domain.Content;

/// <summary>
///     Blog post managed from the admin area and rendered on the landing page.
///     Bilingual by design (fa/en title + summary); body is plain text (blank
///     line = paragraph break) to stay XSS-safe without a sanitiser.
/// </summary>
public sealed class BlogPost : BaseEntity
{
    public string Slug { get; set; } = string.Empty;

    public string TitleFa { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;

    public string SummaryFa { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;

    /// <summary>Plain-text body (double newline splits paragraphs).</summary>
    public string BodyFa { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;

    /// <summary>Draft posts stay admin-only until Published, then go public.</summary>
    public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;

    /// <summary>
    ///     Release date. Admin-manageable: set/override from the admin editor;
    ///     Publish() only stamps "now" when no explicit date was chosen.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    public void Publish()
    {
        Status = BlogPostStatus.Published;
        // Keep an admin-set release date; stamp now only as the default.
        PublishedAt ??= DateTime.UtcNow;
    }

    public void Unpublish()
    {
        // Keep PublishedAt so a re-publish restores the original release date.
        Status = BlogPostStatus.Draft;
    }
}

public enum BlogPostStatus
{
    Draft = 1,
    Published = 2,
}

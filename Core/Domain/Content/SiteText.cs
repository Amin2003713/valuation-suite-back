using Common.Base;

namespace Domain.Content;

/// <summary>
///     One admin-managed UI label / marketing string, in both languages.
///     Rows are *overrides*: the frontend always ships a built-in Persian and
///     English default for every key it renders, so the site is never blank if
///     this table is empty (or a row is deactivated). Empty <see cref="TextFa" /> /
///     <see cref="TextEn" /> likewise fall back to the built-in string.
/// </summary>
public sealed class SiteText : BaseEntity
{
    /// <summary>
    ///     Stable dotted key the frontend looks the text up by
    ///     (e.g. <c>home.hero.title</c>). Never shown to users.
    /// </summary>
    public string TextKey { get; set; } = string.Empty;

    /// <summary>Admin grouping for the editor (e.g. <c>home</c>, <c>auth</c>).</summary>
    public string GroupKey { get; set; } = string.Empty;

    /// <summary>Human hint shown to staff so the key is understandable.</summary>
    public string Label { get; set; } = string.Empty;

    public string TextFa { get; set; } = string.Empty;
    public string TextEn { get; set; } = string.Empty;

    /// <summary>Sort rank inside its group, ascending.</summary>
    public int SortOrder { get; set; }
}

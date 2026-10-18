using Common.Base;

namespace Domain.Content;

/// <summary>
///     One link in a footer column, managed from the admin area. Columns are
///     simple string groups; ordering is explicit so admins can rank links.
///     The site footer (all public pages + landing) renders whatever is
///     IsActive here; if the table is empty the frontend falls back to its
///     built-in defaults so the first deploy is never linkless.
/// </summary>
public sealed class FooterLink : BaseEntity
{
    /// <summary>Column title (fa / en pair, one row per language rendered).</summary>
    public string ColumnFa { get; set; } = string.Empty;
    public string ColumnEn { get; set; } = string.Empty;

    public string LabelFa { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;

    /// <summary>Route or absolute URL ("/pricing", "https://…").</summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>Sort rank within its column, ascending.</summary>
    public int SortOrder { get; set; }
}

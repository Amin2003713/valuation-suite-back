namespace Application.Tools;

/// <summary>
///     Per-request language preference for tool runs. The client sends "lang"
///     ("fa" | "en") inside the tool input; runners use it to localize any
///     human-readable strings in results (personas, labels, statuses).
///     Math never depends on language.
/// </summary>
public sealed class ToolLocalization
{
    public const string Fa = "fa";
    public const string En = "en";

    public string Lang { get; }

    public ToolLocalization(string? lang)
    {
        Lang = string.Equals(lang, En, StringComparison.OrdinalIgnoreCase) ? En : Fa;
    }

    public bool IsEnglish => Lang == En;

    /// <summary>Picks fa/en variant of a literal.</summary>
    public string Pick(string fa, string en) => IsEnglish ? en : fa;

    /// <summary>
    ///     Normalizes legacy Persian status values (e.g. useStatus "فعال") and any
    ///     future English variants onto a canonical code, so math keys off the code
    ///     instead of a display string.
    /// </summary>
    public static string NormalizeUseStatus(string? value) => value?.Trim() switch
    {
        "فعال" or "active" or "Active" => "active",
        "در حال توسعه" or "in development" or "In development" => "developing",
        _ => "inactive",
    };

    /// <summary>Localized display label for a canonical use-status code.</summary>
    public string UseStatusLabel(string code) => code switch
    {
        "active" => Pick("فعال", "Active"),
        "developing" => Pick("در حال توسعه", "In development"),
        _ => Pick("غیرفعال", "Inactive"),
    };
}

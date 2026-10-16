namespace Application.Common;

/// <summary>
///     The single supported-languages config list (appsettings "Languages" section).
///     Start: fa + en — both already present in the project (client <c>Lang</c> type,
///     backend <c>ToolLocalization</c>). Adding a language = append to
///     <see cref="Supported"/> + provide translations; no migration, no client build.
///     Default and fallback language is Persian (<c>fa</c>).
/// </summary>
public sealed class LanguageOptions
{
    public const string SectionName = "Languages";

    /// <summary>Language codes the API serves, in priority-independent order.</summary>
    public string[] Supported { get; set; } = ["fa", "en"];

    /// <summary>Default / fallback language — Persian per product rule.</summary>
    public string Default { get; set; } = "fa";

    /// <summary>True when <paramref name="code"/> is in the supported list (case-insensitive).</summary>
    public bool IsSupported(string? code) =>
        !string.IsNullOrWhiteSpace(code) &&
        Supported.Any(s => string.Equals(s, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the supported code matching <paramref name="code"/>, else the default.</summary>
    public string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Default;
        var hit = Supported.FirstOrDefault(s => string.Equals(s, code, StringComparison.OrdinalIgnoreCase));
        return hit ?? Default;
    }
}

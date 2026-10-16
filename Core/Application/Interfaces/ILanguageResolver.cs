namespace Application.Interfaces;

/// <summary>
///     The language actually served for the current request. Resolved once per
///     request by the language middleware with the priority:
///     explicit <c>?lang</c> query param → <c>Accept-Language</c> header → configured
///     default (<c>fa</c>). Handlers read it to localize tool info and question labels;
///     responses echo it (payload <c>lang</c> field + <c>Content-Language</c> header)
///     so the client can set direction (RTL for fa).
/// </summary>
public interface ILanguageResolver
{
    /// <summary>Normalized supported language code ("fa" | "en" | …).</summary>
    string Lang { get; }
}

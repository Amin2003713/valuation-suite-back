using System.Text.RegularExpressions;

namespace Application.Security;

/// <summary>
///     What a piece of user-supplied text looks like after inspection.
///     <see cref="Reasons" /> carries stable codes (not sentences) so callers can
///     log or filter on them without parsing prose.
/// </summary>
public sealed record ThreatReport(bool IsPhishing, bool HasMarkup, IReadOnlyList<string> Reasons)
{
    public static readonly ThreatReport Clean = new(false, false, Array.Empty<string>());

    public bool IsSuspicious => IsPhishing || HasMarkup;
}

/// <summary>
///     Heuristic detector for the two ways user text is abused here: script/markup
///     injection (stored XSS) and phishing (links that impersonate the platform or
///     ask for credentials).
///
///     It is deliberately conservative — every signal is combined with context, so
///     an ordinary message that merely contains a URL stays clean. Detections are
///     meant to flag and quarantine content (spam triage, rejected payloads), never
///     to silently alter it.
/// </summary>
public static partial class ThreatDetector
{
    /* Markup / script injection. */
    [GeneratedRegex(@"<\s*(script|iframe|object|embed|svg|img|style)\b|javascript\s*:|vbscript\s*:|data\s*:\s*text/html|\bon[a-z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MarkupPattern();

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"https?://\d{1,3}(?:\.\d{1,3}){3}(?::\d+)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RawIpHostPattern();

    [GeneratedRegex(@"https?://(?:www\.)?(?:bit\.ly|tinyurl\.com|t\.co|goo\.gl|is\.gd|cutt\.ly|shorturl\.at|rb\.gy|rebrand\.ly|tiny\.cc)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlShortenerPattern();

    [GeneratedRegex(@"xn--", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PunycodePattern();

    /* Credential / payment requests — the payload of most phishing. */
    [GeneratedRegex(@"(رمز\s*(عبور|اول|دوم|یکبار|یک‌بار)|گذرواژه|کد\s*(پیامک|تأیید|تایید)|اطلاعات\s*کارت|شماره\s*کارت|cvv2?|رمز\s*کارت|password|passcode|one[-\s]?time\s*code|card\s*number|verify\s+your\s+account|confirm\s+your\s+(account|identity)|حساب\s*خود\s*را\s*تأیید)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CredentialPattern();

    /* Urgency phrasing that only appears in scams, not in normal business text. */
    [GeneratedRegex(@"(ظرف\s*۲۴\s*ساعت|در\s*اسرع\s*وقت\s*اقدام|حساب\s*شما\s*(مسدود|تعلیق)|within\s+24\s+hours|account\s+(will\s+be\s+)?(suspended|locked|closed))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrgencyPattern();

    /// <summary>True when the text carries executable markup rather than plain text.</summary>
    public static bool ContainsMarkup(string? text) =>
        !string.IsNullOrWhiteSpace(text) && MarkupPattern().IsMatch(text);

    /// <summary>
    ///     Inspect free text submitted by a user (contact form, adviser question,
    ///     notes, company name…).
    ///     <paramref name="trustedDomain" /> is the platform's own host: links with a
    ///     look-alike host are reported as phishing.
    /// </summary>
    public static ThreatReport Inspect(string? text, string? trustedDomain = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ThreatReport.Clean;

        var reasons = new List<string>(4);
        var hasMarkup = MarkupPattern().IsMatch(text);
        if (hasMarkup)
            reasons.Add("markup");

        var isPhishing = false;

        var links = LinkPattern().Matches(text);
        if (links.Count > 0)
        {
            if (links.Count > 6)
            {
                reasons.Add("link_flood");
                isPhishing = true;
            }

            if (RawIpHostPattern().IsMatch(text))
            {
                reasons.Add("raw_ip_link");
                isPhishing = true;
            }

            if (UrlShortenerPattern().IsMatch(text))
            {
                reasons.Add("url_shortener");
                isPhishing = true;
            }

            if (PunycodePattern().IsMatch(text))
            {
                reasons.Add("punycode_host");
                isPhishing = true;
            }

            var credentialAsk = CredentialPattern().IsMatch(text);
            var urgency = UrgencyPattern().IsMatch(text);

            // A credential request plus a link is phishing in every legitimate
            // reading; urgency alone only raises the score.
            if (credentialAsk && links.Count > 0)
            {
                reasons.Add("credential_request_with_link");
                isPhishing = true;
            }
            else if (urgency && links.Count > 0)
            {
                reasons.Add("urgency_with_link");
                isPhishing = true;
            }

            if (!string.IsNullOrWhiteSpace(trustedDomain) && LooksLikeDomainSpoof(text, trustedDomain))
            {
                reasons.Add("lookalike_domain");
                isPhishing = true;
            }
        }

        return reasons.Count == 0
            ? ThreatReport.Clean
            : new ThreatReport(isPhishing, hasMarkup, reasons);
    }

    /// <summary>
    ///     A host within edit distance 1–2 of our own domain is a spoof unless it is
    ///     literally ours (typosquatting: arjip.co vs arjlp.co).
    /// </summary>
    private static bool LooksLikeDomainSpoof(string text, string trustedDomain)
    {
        var trusted = NormaliseHost(trustedDomain);
        if (trusted.Length < 5)
            return false;

        foreach (Match link in LinkPattern().Matches(text))
        {
            if (!Uri.TryCreate(link.Value, UriKind.Absolute, out var uri))
                continue;

            var host = NormaliseHost(uri.Host);
            if (host == trusted || host.EndsWith("." + trusted, StringComparison.Ordinal))
                continue;

            var distance = EditDistance(host, trusted);
            if (distance is > 0 and <= 2 && Math.Abs(host.Length - trusted.Length) <= 2)
                return true;
        }

        return false;
    }

    private static string NormaliseHost(string host) =>
        host.Trim().TrimEnd('.').ToLowerInvariant().Replace("www.", string.Empty, StringComparison.Ordinal);

    /// <summary>Levenshtein distance, two-row implementation.</summary>
    private static int EditDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}

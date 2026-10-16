using Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RequestHandlers.Tools;

/// <summary>
///     Resolves the request language once and stores it in <c>HttpContext.Items["lang"]</c>:
///     explicit <c>?lang</c> query param → <c>Accept-Language</c> header (quality-ordered,
///     matched against the configured supported list) → configured default (fa).
///     An unsupported code falls back to the default with a warning — never an error.
///     Every response gets a <c>Content-Language</c> header with the language actually
///     served, so clients can set direction (RTL for fa) without parsing payloads.
/// </summary>
public sealed class LanguageMiddleware(RequestDelegate next, ILogger<LanguageMiddleware> logger)
{
    public const string ItemKey = "lang";

    public async Task InvokeAsync(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<LanguageOptions>>().Value;
        var lang = Resolve(context, options, logger);
        context.Items[ItemKey] = lang;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers.ContentLanguage = lang;
            return Task.CompletedTask;
        });

        await next(context);
    }

    /// <summary>Priority: ?lang → Accept-Language → default. Exposed for tests.</summary>
    public static string Resolve(HttpContext context, LanguageOptions options, ILogger logger)
    {
        // 1. Explicit query param wins.
        var explicitLang = context.Request.Query["lang"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(explicitLang))
        {
            if (options.IsSupported(explicitLang))
                return options.Normalize(explicitLang);

            logger.LogWarning("Unsupported ?lang={Lang} requested — falling back to default {Default}", explicitLang, options.Default);
            return options.Default;
        }

        // 2. Accept-Language header (quality-ordered).
        var header = context.Request.Headers.AcceptLanguage.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
        {
            var best = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part =>
                {
                    var segments = part.Split(';', StringSplitOptions.TrimEntries);
                    var code = segments[0];
                    var q = 1.0;
                    foreach (var p in segments.Skip(1))
                        if (p.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                            double.TryParse(p[2..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                            q = parsed;
                    return (code, q);
                })
                .OrderByDescending(x => x.q)
                .Select(x => x.code)
                .FirstOrDefault(c => options.IsSupported(c));

            if (best is not null)
                return options.Normalize(best);

            logger.LogInformation("No supported language in Accept-Language={Header} — using default {Default}", header, options.Default);
        }

        // 3. Default (fa).
        return options.Default;
    }
}

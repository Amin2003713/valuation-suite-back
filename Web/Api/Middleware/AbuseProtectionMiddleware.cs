using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Application.Security;

namespace Web.Api.Middleware;

/// <summary>
///     Cross-cutting abuse protection for the public API:
///
///     1. Security headers on every response (clickjacking, MIME sniffing,
///        referrer leakage, powerful browser APIs).
///     2. Rejects requests whose URL or path carries traversal / markup payloads
///        — the cheapest possible stop for scanner traffic.
///     3. Per-IP sliding window on the endpoints that cost money or send mail
///        (login, register, contact, newsletter) plus tool execution, so
///        credential stuffing and form spam never reach the handlers.
///
///     Deliberately framework-level: handlers stay about business rules and no
///     controller repeats these checks. Rate-limit state is in-process, which is
///     correct for this single-instance deployment and degrades to "no limit"
///     rather than "no service" if the process restarts.
/// </summary>
public sealed partial class AbuseProtectionMiddleware(
    RequestDelegate next,
    ILogger<AbuseProtectionMiddleware> logger)
{
    private sealed class Counter
    {
        public long WindowStartTicks;
        public int Count;
    }

    private static readonly ConcurrentDictionary<string, Counter> Counters = new();
    private static long _lastPruneTicks;

    private static readonly (string Path, int Limit, TimeSpan Span)[] Rules =
    [
        ("/api/auth/login", 10, TimeSpan.FromMinutes(5)),
        ("/api/auth/register", 5, TimeSpan.FromHours(1)),
        ("/api/messaging/contact", 5, TimeSpan.FromHours(1)),
        ("/api/messaging/newsletter", 10, TimeSpan.FromHours(1)),
        ("/api/adviser/ask", 40, TimeSpan.FromMinutes(10)),
    ];

    /* Tool execution is expensive (math + DB write) — cap it per IP too. */
    private const int ToolRunLimit = 120;
    private static readonly TimeSpan ToolRunSpan = TimeSpan.FromMinutes(10);

    [GeneratedRegex(@"\.\./|%00|\x00|<\s*script|javascript\s*:|union\s+select|'\s*or\s+1\s*=\s*1",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AttackPattern();

    public async Task InvokeAsync(HttpContext context)
    {
        ApplySecurityHeaders(context);

        var path = context.Request.Path.Value ?? string.Empty;

        if (AttackPattern().IsMatch(path) || AttackPattern().IsMatch(context.Request.QueryString.Value ?? string.Empty))
        {
            logger.LogWarning("Blocked suspicious request from {Ip} to {Path}", ClientKey(context), path);
            await RejectAsync(context, StatusCodes.Status400BadRequest, "درخواست نامعتبر است.");
            return;
        }

        if (context.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE"
            && !TryConsumeRateLimit(context, path, out var retryAfter))
        {
            logger.LogWarning("Rate limit hit for {Ip} on {Path}", ClientKey(context), path);
            context.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
            await RejectAsync(context, StatusCodes.Status429TooManyRequests, "تعداد درخواست‌ها زیاد است؛ کمی بعد دوباره تلاش کنید.");
            return;
        }

        await next(context);
    }

    private static void ApplySecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Only meaningful over TLS; browsers ignore it on plain HTTP.
        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }

    private static bool TryConsumeRateLimit(HttpContext context, string path, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;

        int limit;
        TimeSpan span;

        if (path.Contains("/run", StringComparison.OrdinalIgnoreCase) && path.StartsWith("/api/tools", StringComparison.OrdinalIgnoreCase))
        {
            limit = ToolRunLimit;
            span = ToolRunSpan;
        }
        else
        {
            var rule = Rules.FirstOrDefault(r => path.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase));
            if (rule.Path is null)
                return true;                       // not a rate-limited endpoint
            limit = rule.Limit;
            span = rule.Span;
        }

        var key = $"{ClientKey(context)}|{path.ToLowerInvariant()}";
        var now = DateTime.UtcNow.Ticks;
        PruneIfNeeded(now);

        var counter = Counters.GetOrAdd(key, _ => new Counter { WindowStartTicks = now });

        lock (counter)
        {
            if (now - counter.WindowStartTicks >= span.Ticks)
            {
                counter.WindowStartTicks = now;
                counter.Count = 0;
            }

            counter.Count++;
            if (counter.Count <= limit)
                return true;

            retryAfter = TimeSpan.FromTicks(counter.WindowStartTicks + span.Ticks - now);
            return false;
        }
    }

    private static void PruneIfNeeded(long nowTicks)
    {
        var last = Interlocked.Read(ref _lastPruneTicks);
        if (nowTicks - last < TimeSpan.FromMinutes(5).Ticks)
            return;
        if (Interlocked.CompareExchange(ref _lastPruneTicks, nowTicks, last) != last)
            return;

        foreach (var entry in Counters)
        {
            // Anything untouched for an hour is long past every window above.
            if (nowTicks - entry.Value.WindowStartTicks > TimeSpan.FromHours(1).Ticks)
                Counters.TryRemove(entry.Key, out _);
        }
    }

    private static string ClientKey(HttpContext context)
    {
        // Behind the reverse proxy the client IP arrives in X-Forwarded-For.
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
            if (IPAddress.TryParse(first, out var parsed))
                return parsed.ToString();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async Task RejectAsync(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = status == StatusCodes.Status429TooManyRequests ? "Too Many Requests" : "Bad Request",
            status,
            detail = message,
            errorCode = status == StatusCodes.Status429TooManyRequests ? "RATE_LIMITED" : "INVALID_REQUEST",
        });
    }
}

using System.Text.Json;
using Application.Adviser;
using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Tools;
using Common.Exceptions;
using Domain.Adviser;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Adviser;

/// <summary>
///     Builds the adviser's reply for one question in the context of a tool:
///     the caller's latest submission of that tool (inputs + result), their
///     company/industry snapshot and anonymous peer aggregates of the same
///     tool. Both messages are persisted server-side; the full thread per user
///     per tool is returned so the client can render the conversation without
///     extra round-trips.
/// </summary>
public sealed class AskAdviserCommandHandler(
    IQueryRepository<AdviserMessage> history,
    ICommandRepository<AdviserMessage> historyCommands,
    IToolSubmissionQueries submissions,
    IQueryRepository<Domain.Users.ApplicationUser> users,
    IQueryRepository<Domain.Tools.ToolForm> toolForms,
    IAdviserRateLimiter rateLimiter,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<AskAdviserCommand, AdviserAskResponse>
{
    private const int MaxQuestionLength = 4000;

    public async Task<AdviserAskResponse> Handle(AskAdviserCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw ValuationException.Forbidden("برای گفتگو با کارشناس ابتدا وارد حساب خود شوید.");
        if (string.IsNullOrWhiteSpace(request.ToolCode))
            throw ValuationException.BadRequest("کد ابزار ارسال نشده است.");
        var question = (request.Question ?? "").Trim();
        if (question.Length == 0)
            throw ValuationException.BadRequest("سؤال خالی است.");
        if (question.Length > MaxQuestionLength)
            question = question[..MaxQuestionLength];

        if (!rateLimiter.TryAcquire(currentUser.UserId))
            throw ValuationException.BadRequest("تعداد درخواست‌ها زیاد است؛ کمی بعد دوباره تلاش کنید.");

        // ── Context: latest submission of this tool by this user ──
        var latest = await submissions
            .ByTool(request.ToolCode)
            .Where(s => s.UserId == currentUser.UserId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var dims = latest is null ? [] : ResultDimensions(latest.ResultJson);
        var peers = await PeerStats(request.ToolCode, currentUser.UserId, 50, ct);

        string? industry = null;
        var user = await users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user?.CompanyId is { } cid && cid != Guid.Empty)
            industry = null; // industry comes from the submission snapshot when available

        // Tool title (localized content falls back to the base column).
        var toolTitle = await toolForms.TableNoTracking
            .Where(f => f.ToolCode == request.ToolCode)
            .Select(f => f.Title)
            .FirstOrDefaultAsync(ct) ?? request.ToolCode;

        var ctx = new AdviserContext(
            request.ToolCode,
            toolTitle,
            latest?.OverallScore,
            dims,
            latest?.CompanyName,
            latest?.Industry ?? industry,
            peers);

        var answer = AdviserAnswerBot.Answer(ctx, question);

        var contextJson = JsonSerializer.Serialize(new
        {
            toolCode = request.ToolCode,
            overallScore = latest?.OverallScore,
            dimensions = dims.Select(d => new { key = d.Key, score = d.Score }),
            company = latest?.CompanyName,
            industry = latest?.Industry ?? industry,
        });

        await historyCommands.AddAsync(new AdviserMessage
        {
            UserId = currentUser.UserId,
            ToolCode = request.ToolCode,
            Author = AdviserAuthor.User,
            Text = question,
        }, ct, saveNow: false);
        await historyCommands.AddAsync(new AdviserMessage
        {
            UserId = currentUser.UserId,
            ToolCode = request.ToolCode,
            Author = AdviserAuthor.Adviser,
            Text = answer,
            ContextJson = contextJson,
        }, ct);

        return new AdviserAskResponse(await ToRows(request.ToolCode, history, currentUser.UserId, ct));
    }

    internal static async Task<List<AdviserMessageRow>> ToRows(
        string toolCode, IQueryRepository<AdviserMessage> history, Guid userId, CancellationToken ct)
    {
        var rows = await history.TableNoTracking
            .Where(m => m.UserId == userId && m.ToolCode == toolCode)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(m => new AdviserMessageRow(m.Id, m.Author.ToString(), m.Text, m.CreatedAt.ToString("O"))).ToList();
    }    /// <summary>
    ///     Aggregated min/max/avg per metric across OTHER users' submissions of
    ///     the tool (anonymous, capped sample). Empty when no subset available.
    /// </summary>
    private async Task<Dictionary<string, (double Min, double Max, double Avg, int Count)>> PeerStats(
        string toolCode, Guid excludeUserId, int takeLast, CancellationToken ct)
    {
        var rows = await PeerSubmissionRows(toolCode, excludeUserId, takeLast, ct);
        if (rows.Count == 0) return [];
        var acc = new Dictionary<string, List<double>>();
        foreach (var json in rows)
        {
            foreach (var (key, value) in ResultDimensions(json))
            {
                if (!acc.TryGetValue(key, out var list))
                    acc[key] = list = [];
                list.Add(value);
            }
        }
        return acc.ToDictionary(
            kv => kv.Key,
            kv => (kv.Value.Min(), kv.Value.Max(), kv.Value.Average(), kv.Value.Count));
    }

    private Task<List<string>> PeerSubmissionRows(
        string toolCode, Guid excludeUserId, int takeLast, CancellationToken ct)
    {
        return submissions.ByTool(toolCode)
            .Where(s => s.UserId != excludeUserId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Take(takeLast)
            .Select(s => s.ResultJson)
            .ToListAsync(ct);
    }

    /// <summary>Extracts 0-100 section/indicator scores from a stored result JSON.</summary>
    internal static List<(string Key, double Score)> ResultDimensions(string resultJson)
    {
        var list = new List<(string, double)>();
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return list;

            if (root.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in sections.EnumerateArray())
                {
                    if (s.ValueKind != JsonValueKind.Object) continue;
                    var key = s.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                    if (key is null) continue;
                    foreach (var p in new[] { "percent", "score", "value" })
                    {
                        if (s.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && d >= 0 && d <= 100)
                        {
                            list.Add((key, d));
                            break;
                        }
                    }
                }
            }
            else if (root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var r in rows.EnumerateArray())
                {
                    if (r.ValueKind == JsonValueKind.Object &&
                        r.TryGetProperty("C", out var c) && c.ValueKind == JsonValueKind.Number && c.TryGetDouble(out var cv) &&
                        cv >= 0 && cv <= 100)
                        list.Add(($"axis{i}", cv));
                    i++;
                }
            }
            else if (root.TryGetProperty("indicators", out var indicators) && indicators.ValueKind == JsonValueKind.Array)
            {
                var raw = new List<(string, double)>();
                foreach (var s in indicators.EnumerateArray())
                {
                    if (s.ValueKind != JsonValueKind.Object) continue;
                    var key = s.TryGetProperty("key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                    if (key is null) continue;
                    if (s.TryGetProperty("score", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && d >= 0 && d <= 10)
                        raw.Add((key, d * 10));
                }
                list.AddRange(raw);
            }
        }
        catch (JsonException)
        {
            // corrupt/legacy result row — no dimension context
        }
        return list;
    }
}

/// <summary>Returns the caller's persisted chat history for one tool (oldest first).</summary>
public sealed class GetAdviserHistoryQueryHandler(
    IQueryRepository<AdviserMessage> history,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetAdviserHistoryQuery, AdviserAskResponse>
{
    public async Task<AdviserAskResponse> Handle(GetAdviserHistoryQuery request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw ValuationException.Forbidden("برای مشاهدهٔ گفتگو ابتدا وارد حساب خود شوید.");
        return new AdviserAskResponse(
            await AskAdviserCommandHandler.ToRows(request.ToolCode, history, currentUser.UserId, ct));
    }
}

/// <summary>"New conversation": clears the caller's chat history for one tool.</summary>
public sealed class ClearAdviserHistoryCommandHandler(
    ICommandRepository<AdviserMessage> historyCommands,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<ClearAdviserHistoryCommand>
{
    public async Task Handle(ClearAdviserHistoryCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw ValuationException.Forbidden("برای پاک کردن گفتگو ابتدا وارد حساب خود شوید.");
        await historyCommands.DeleteAsync(
            m => m.UserId == currentUser.UserId && m.ToolCode == request.ToolCode,
            ct);
    }
}

using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace RequestHandlers.Tools;

/// <inheritdoc/> — contract now lives in Application.Interfaces (shared with
/// persistence handlers); the old local declaration is gone.
public sealed class HttpCurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    public Guid UserId
    {
        get
        {
            var value = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? accessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var value = accessor.HttpContext?.User?.FindFirstValue("companyId");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsStaff =>
        Application.Admin.AdminRoles.All.Any(role =>
            accessor.HttpContext?.User?.IsInRole(role) == true);
}

/// <summary>
///     Sections of a tool result considered "advanced, paid value": Monte Carlo,
///     tornado/sensitivity, scenario analysis and method-by-method breakdowns.
///     Everything else stays free.
/// </summary>
public static class AdvancedSections
{
    /* scenarioValues/allValues are NOT stripped: they are summary ranges that
     * every user sees (probabilistic + summary panels) — stripping them crashed
     * the free-tier summary UI. */
    private static readonly string[] Keys =
        ["mc", "mcResults", "tornado", "scenarios", "vals", "methods"];

    /// <summary>
    ///     Serializes the result (camelCase, exactly as the client receives it) and
    ///     removes advanced keys at every object level. Uses in-place JsonNode removal
    ///     so every remaining property name — including explicit [JsonPropertyName]
    ///     overrides like "Nd1" — is preserved verbatim (a dictionary round-trip with
    ///     DictionaryKeyPolicy would silently rename them and break the client).
    /// </summary>
    public static JsonElement Strip(object result)
    {
        var node = JsonSerializer.SerializeToNode(result, ToolInput.WriteOptions);
        if (node is not null)
            StripNode(node);
        return node?.Deserialize<JsonElement>()
               ?? JsonSerializer.SerializeToElement(new { }, ToolInput.WriteOptions);
    }

    private static void StripNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                foreach (var key in obj.Where(p => Keys.Contains(p.Key, StringComparer.OrdinalIgnoreCase))
                    .Select(p => p.Key).ToList())
                {
                    obj.Remove(key);
                }

                foreach (var (_, value) in obj)
                {
                    if (value is not null)
                        StripNode(value);
                }
                break;
            }

            case JsonArray arr:
            {
                foreach (var item in arr)
                {
                    if (item is not null)
                        StripNode(item);
                }
                break;
            }
        }
    }
}

public sealed class RunToolCommandHandler(
    IToolRunnerResolver resolver,
    IToolSubmissionStore submissions,
    IToolQuestionQueries questions,
    IEntitlementService entitlements,
    ILanguageResolver languages,
    IOptions<LanguageOptions> languageOptions,
    ICurrentUserAccessor currentUser,
    IQueryRepository<Domain.Users.ApplicationUser> usersQueries,
    IMemoryCache cache) : IRequestHandler<RunToolCommand, ToolRunResponse>
{
    /// <summary>
    ///     Per-tool question rows only change when staff edit content; they are
    ///     needed on every run for answer validation. Same 60s window as the
    ///     public tool-form cache, so edits show up just as quickly.
    /// </summary>
    private static readonly TimeSpan QuestionsCacheTtl = TimeSpan.FromSeconds(60);
    /// <summary>Minimum company profile (name + industry) a user must have before running any tool.</summary>
    private const string MissingProfileMessage =
        "برای اجرای ابزار ابتدا نام شرکت و حوزه فعالیت آن را در پروفایل تکمیل کنید. (PROFILE_MISSING)";

    public async Task<ToolRunResponse> Handle(RunToolCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw Common.Exceptions.ValuationException.Forbidden("برای اجرای ابزار ابتدا وارد حساب خود شوید.");

        /* Company-profile gate: every run needs the user's company name and
         * industry (the spider-web benchmarks group results by industry). The
         * client shows a fill-in dialog when it sees the PROFILE_MISSING tag. */
        var profile = await LoadProfileAsync(currentUser.UserId, ct)
            ?? throw Common.Exceptions.ValuationException.Forbidden("کاربر یافت نشد.");

        if (string.IsNullOrWhiteSpace(profile.CompanyName) || string.IsNullOrWhiteSpace(profile.CompanyIndustry))
            throw Common.Exceptions.ValuationException.Forbidden(MissingProfileMessage);

        /* Run-on-behalf: when the body carries runFor (or the dedicated endpoint
         * maps it), the submission is attributed to the target user when that
         * user is a member of the target company; otherwise it stays with the
         * caller and the context is preserved inside the stored input JSON. */
        var runFor = request.RunFor ?? ExtractRunFor(request.Input);
        var effectiveUserId = currentUser.UserId;
        string? effectiveCompanyName = profile.CompanyName;
        string? effectiveIndustry = profile.CompanyIndustry;
        if (runFor is { } ctx)
        {
            if (ctx.UserId is { } targetUser && await BelongsToCompanyAsync(targetUser, ctx.CompanyId, ct))
            {
                effectiveUserId = targetUser;
                // Attribute the snapshot to the target's own company (case: an
                // admin in a different company runs for a client).
                var targetProfile = await LoadProfileAsync(targetUser, ct);
                if (targetProfile is not null)
                {
                    effectiveCompanyName = targetProfile.CompanyName;
                    effectiveIndustry = targetProfile.CompanyIndustry;
                }
            }
        }

        var inputForStorage = runFor is null
            ? request.Input
            : WithRunForRemoved(request.Input);

        /* Server-side answer validation against the tool's stored questions
         * (required / answer type / option values / scale bounds). Row-driven:
         * a no-op for tools without question rows (calculators, search). */
        var questionsKey = $"tool:questions:{request.ToolCode.ToUpperInvariant()}";
        if (!cache.TryGetValue(questionsKey, out List<Application.Interfaces.ToolQuestionRow>? questionRows)
            || questionRows is null)
        {
            questionRows = await questions.ByTool(request.ToolCode)
                .OrderBy(q => q.SortOrder)
                .ToListAsync(ct);
            cache.Set(questionsKey, questionRows, QuestionsCacheTtl);
        }
        ToolAnswerValidator.Validate(request.ToolCode, inputForStorage, questionRows);

        var runner = resolver.Resolve(request.ToolCode);
        var outcome = runner.Run(inputForStorage);

        /* Reuse the Pro flag already read for the caller; a run-on-behalf target
         * still needs its own entitlement read (the flag is per user). */
        var advancedIncluded = effectiveUserId == currentUser.UserId
            ? await entitlements.CanViewAdvancedAsync(effectiveUserId, request.ToolCode, profile.IsPro, ct)
            : await entitlements.CanViewAdvancedAsync(effectiveUserId, request.ToolCode, ct);
        if (!advancedIncluded)
            outcome = outcome with { Result = AdvancedSections.Strip(outcome.Result) };

        // Preview run (persist:false): compute and return, but do NOT create a
        // submission. The client saves explicitly or when leaving the page, so
        // live typing no longer writes a row (and an entitlement read) per edit.
        if (!request.Persist)
            return new ToolRunResponse(
                Guid.Empty,
                request.ToolCode,
                ToolInput.ToJsonElement(outcome.Result),
                outcome.OverallScore,
                advancedIncluded);

        /* One submissions table per tool: instantiate the tool's own row type
         * (ToolSubmissionRegistry) — the shared columns come from ToolSubmissionBase —
         * and stamp the run's language and status. */
        var submission = ToolRegistry.CreateSubmission(
            request.ToolCode,
            effectiveUserId,
            outcome.Name,
            JsonSerializer.Serialize(inputForStorage),
            // WriteOptions: engine results may carry the know-how NaN/Infinity
            // quirks, which the default options refuse to serialise.
            JsonSerializer.Serialize(outcome.Result, ToolInput.WriteOptions),
            outcome.OverallScore,
            ResolveRunLanguage(request.Input));
        submission.CompanyId = profile.CompanyId ?? Guid.Empty;
        submission.CompanyName = effectiveCompanyName;
        submission.Industry = effectiveIndustry;

        await submissions.AddAsync(submission, ct);

        return new ToolRunResponse(
            submission.Id,
            request.ToolCode,
            ToolInput.ToJsonElement(outcome.Result),
            outcome.OverallScore,
            advancedIncluded);
    }

    /// <summary>The handful of user/company fields a run needs, in one projection.</summary>
    private sealed record RunProfile(
        Guid? CompanyId, bool IsPro, string? CompanyName, string? CompanyIndustry);

    /// <summary>
    ///     Reads the user plus the two company fields the run needs. This used to be a
    ///     user query followed by a company query (two round-trips, both materializing
    ///     full entities), and the Pro flag was fetched a third time by the entitlement
    ///     check. One projected join now covers all of it.
    /// </summary>
    private async Task<RunProfile?> LoadProfileAsync(Guid userId, CancellationToken ct)
    {
        var row = await usersQueries.TableNoTracking
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.CompanyId,
                u.Plan,
                u.PlanExpiresAt,
                CompanyName = u.Company != null ? u.Company.Name : null,
                CompanyIndustry = u.Company != null ? u.Company.Industry : null,
            })
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return null;

        // Mirrors ApplicationUser.IsPro (kept in memory so the comparison does not
        // depend on the provider's DateTime.UtcNow translation).
        var isPro = row.Plan == Domain.Companies.Plan.Pro
                    && (row.PlanExpiresAt == null || row.PlanExpiresAt > DateTime.UtcNow);

        return new RunProfile(row.CompanyId, isPro, row.CompanyName, row.CompanyIndustry);
    }

    private async Task<bool> BelongsToCompanyAsync(Guid userId, Guid? companyId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return false;

        var user = await usersQueries.TableNoTracking
            .Where(u => u.Id == userId)
            .Select(u => new { u.CompanyId })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return false;

        return companyId is null || companyId == Guid.Empty || user.CompanyId == companyId;
    }

    /// <summary>Run language: the input's lang when it is supported, else the request language.</summary>
    private string ResolveRunLanguage(JsonElement input)
    {
        if (input.ValueKind == JsonValueKind.Object &&
            input.TryGetProperty("lang", out var l) &&
            l.ValueKind == JsonValueKind.String &&
            languageOptions.Value.IsSupported(l.GetString()))
            return languageOptions.Value.Normalize(l.GetString());

        return languages.Lang;
    }

    /// <summary>Reads runFor from the input body (client sends it inline) and removes it from the engine input.</summary>
    private static RunForContext? ExtractRunFor(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
            return null;

        if (!input.TryGetProperty("runFor", out var el) || el.ValueKind != JsonValueKind.Object)
            return null;

        Guid? companyId = null, userId = null;
        string? note = null;

        if (el.TryGetProperty("companyId", out var c) && Guid.TryParse(c.GetString(), out var cid)) companyId = cid;
        if (el.TryGetProperty("userId", out var u) && Guid.TryParse(u.GetString(), out var uid)) userId = uid;
        if (el.TryGetProperty("note", out var n) && n.ValueKind == JsonValueKind.String) note = n.GetString();

        if (companyId is null && userId is null && note is null)
            return null;

        return new RunForContext { CompanyId = companyId, UserId = userId, Note = note };
    }

    /// <summary>Strips the runFor envelope from the input before handing it to the math engine / storage.</summary>
    private static JsonElement WithRunForRemoved(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("runFor", out _))
            return input;

        var node = System.Text.Json.Nodes.JsonNode.Parse(input.GetRawText());
        if (node is System.Text.Json.Nodes.JsonObject obj)
            obj.Remove("runFor");

        return node?.Deserialize<JsonElement>() ?? input;
    }
}

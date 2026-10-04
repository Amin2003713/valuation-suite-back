using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Interfaces;
using Application.Interfaces.Base;

namespace RequestHandlers.Tools;

/// <summary>Provides the current user's id (kept here so handlers stay testable).</summary>
public interface ICurrentUserAccessor
{
    Guid UserId { get; }
}

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
    ICommandRepository<ToolSubmission> submissions,
    IEntitlementService entitlements,
    ICurrentUserAccessor currentUser,
    IQueryRepository<Domain.Users.ApplicationUser> usersQueries) : IRequestHandler<RunToolCommand, ToolRunResponse>
{
    public async Task<ToolRunResponse> Handle(RunToolCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw Common.Exceptions.ValuationException.Forbidden("برای اجرای ابزار ابتدا وارد حساب خود شوید.");

        /* Run-on-behalf: when the body carries runFor (or the dedicated endpoint
         * maps it), the submission is attributed to the target user when that
         * user is a member of the target company; otherwise it stays with the
         * caller and the context is preserved inside the stored input JSON. */
        var runFor = request.RunFor ?? ExtractRunFor(request.Input);
        var effectiveUserId = currentUser.UserId;
        if (runFor is { } ctx)
        {
            if (ctx.UserId is { } targetUser && await BelongsToCompanyAsync(targetUser, ctx.CompanyId, ct))
                effectiveUserId = targetUser;
        }

        var inputForStorage = runFor is null
            ? request.Input
            : WithRunForRemoved(request.Input);

        var runner = resolver.Resolve(request.ToolCode);
        var outcome = runner.Run(inputForStorage);

        var advancedIncluded = await entitlements.CanViewAdvancedAsync(effectiveUserId, request.ToolCode, ct);
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

        var submission = ToolSubmission.Create(
            effectiveUserId,
            request.ToolCode,
            outcome.Name,
            JsonSerializer.Serialize(WithRunForRemoved(request.Input)),
            JsonSerializer.Serialize(outcome.Result),
            outcome.OverallScore);

        await submissions.AddAsync(submission, ct, saveNow: true);

        return new ToolRunResponse(
            submission.Id,
            request.ToolCode,
            ToolInput.ToJsonElement(outcome.Result),
            outcome.OverallScore,
            advancedIncluded);
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

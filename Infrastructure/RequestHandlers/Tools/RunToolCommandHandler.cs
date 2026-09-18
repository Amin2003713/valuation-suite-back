using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private static readonly string[] Keys =
        ["mc", "mcResults", "tornado", "scenarios", "scenarioValues", "allValues", "vals", "methods"];

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
    ICurrentUserAccessor currentUser) : IRequestHandler<RunToolCommand, ToolRunResponse>
{
    public async Task<ToolRunResponse> Handle(RunToolCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw Common.Exceptions.ValuationException.Forbidden("برای اجرای ابزار ابتدا وارد حساب خود شوید.");

        var runner = resolver.Resolve(request.ToolCode);
        var outcome = runner.Run(request.Input);

        var advancedIncluded = await entitlements.CanViewAdvancedAsync(currentUser.UserId, request.ToolCode, ct);
        if (!advancedIncluded)
            outcome = outcome with { Result = AdvancedSections.Strip(outcome.Result) };

        var submission = ToolSubmission.Create(
            currentUser.UserId,
            request.ToolCode,
            outcome.Name,
            JsonSerializer.Serialize(request.Input),
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
}

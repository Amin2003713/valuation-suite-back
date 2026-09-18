using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
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

    /// <summary>Deep-clones the result and removes advanced keys at every object level.</summary>
    public static JsonElement Strip(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteStripped(element, writer);
            writer.Flush();
        }
        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static void WriteStripped(JsonElement el, Utf8JsonWriter w)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                w.WriteStartObject();
                foreach (var p in el.EnumerateObject())
                {
                    if (Keys.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                        continue;
                    w.WritePropertyName(p.Name);
                    WriteStripped(p.Value, w);
                }
                w.WriteEndObject();
                break;

            case JsonValueKind.Array:
                w.WriteStartArray();
                foreach (var item in el.EnumerateArray())
                    WriteStripped(item, w);
                w.WriteEndArray();
                break;

            default:
                el.WriteTo(w);
                break;
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
        if (!advancedIncluded && outcome.Result is JsonElement raw)
            outcome = outcome with { Result = AdvancedSections.Strip(raw) };

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

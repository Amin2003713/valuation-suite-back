using Common.Exceptions;

namespace Application.Tools;

/// <summary>
///     Computes a tool's result from raw user input. Implementations are the backend's
///     math engines — one per tool. The client never computes anything.
/// </summary>
public interface IToolRunner
{
    string ToolCode { get; }
    ToolRunOutcome Run(JsonElement input);
}

public record ToolRunOutcome(object Result, double? OverallScore, string? Name = null);

/// <summary>Resolves the right <see cref="IToolRunner"/> for a tool code.</summary>
public interface IToolRunnerResolver
{
    IToolRunner Resolve(string toolCode);
}

public sealed class ToolRunnerResolver : IToolRunnerResolver
{
    private readonly Dictionary<string, IToolRunner> _runners;

    public ToolRunnerResolver(IEnumerable<IToolRunner> runners)
    {
        _runners = runners.ToDictionary(r => r.ToolCode, StringComparer.OrdinalIgnoreCase);
    }

    public IToolRunner Resolve(string toolCode)
    {
        if (_runners.TryGetValue(toolCode, out var runner))
            return runner;

        throw ValuationException.NotFound($"ابزار «{toolCode}» یافت نشد.");
    }
}

/// <summary>Shared JSON helpers for tool runners (camelCase, tolerant input parsing).</summary>
public static class ToolInput
{
    public static JsonSerializerOptions ReadOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>Client-facing serialization: camelCase to match the frontend result types.</summary>
    public static JsonSerializerOptions WriteOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        // The know-how engine's documented NaN/Infinity quirks must serialize as
        // named tokens (the client normalises them in parseBody).
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public static T Bind<T>(JsonElement input) where T : class
    {
        try
        {
            return input.Deserialize<T>(ReadOptions)
                   ?? throw ValuationException.BadRequest("ورودی ارسال‌شده نامعتبر است.");
        }
        catch (JsonException)
        {
            /* A value that does not fit the input model (wrong type, a number
             * outside int range, malformed JSON) is the caller's error, not a
             * server fault — answer 400 instead of letting it surface as 500. */
            throw ValuationException.BadRequest(
                "ورودی ارسال‌شده نامعتبر است. (INVALID_INPUT)");
        }
    }

    public static JsonElement ToJsonElement<T>(T value) where T : class
        => JsonSerializer.SerializeToElement(value, WriteOptions);
}

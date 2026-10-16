using System.Text.Json;
using Application.Interfaces;
using Common.Exceptions;

namespace Application.Tools;

/// <summary>
///     Server-side validation of submitted answers against the stored questions of a
///     tool (per-tool <c>&lt;Tool&gt;Questions</c> rows: required, answer type, option
///     values, scale bounds).
///
///     Row-driven and conservative:
///     <list type="bullet">
///         <item>Tools without question rows (calculators, search) are a no-op — the
///             runner's typed binding remains their validation.</item>
///         <item>Only keys the DB knows are validated; unknown/envelope keys
///             (lang, name, runFor, …) are ignored.</item>
///         <item>A key that is present is type-checked (choice ∈ options, scale/number
///             numeric and inside [min,max], multi ⊆ options, text = string).</item>
///         <item>IsRequired is enforced only when the tool marked the question required
///             (the client gates those runs too).</item>
///     </list>
///     Errors throw ValuationException.BadRequest with the offending key, matching the
///     project's Persian error style.
/// </summary>
public static class ToolAnswerValidator
{
    public static void Validate(string toolCode, JsonElement input, IReadOnlyList<ToolQuestionRow> questions)
    {
        if (questions.Count == 0) return;
        if (input.ValueKind != JsonValueKind.Object) return;

        foreach (var q in questions)
        {
            /* Answer location is tool-specific and often nested (IDEA/WIPO submit
             * sections[].answers{…}, IPSCORE patents[].answers{…}, INNOV submits a
             * flat map). The lookup is therefore recursive/shape-agnostic: the first
             * object anywhere in the input carrying the question id wins. */
            var hasValue = TryFindAnswer(input, q.QuestionId, out var answer);
            if (!hasValue || answer.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (q.IsRequired)
                    throw ValuationException.BadRequest($"پاسخ سؤال «{q.QuestionId}» الزامی است. ({toolCode})");
                continue;
            }

            switch (q.AnswerType)
            {
                case "choice":
                    ValidateChoice(toolCode, q, answer);
                    break;

                case "multi":
                    ValidateMulti(toolCode, q, answer);
                    break;

                case "scale":
                case "number":
                    ValidateNumber(toolCode, q, answer);
                    break;

                case "text":
                    if (answer.ValueKind != JsonValueKind.String)
                        throw ValuationException.BadRequest($"مقدار «{q.QuestionId}» باید متن باشد. ({toolCode})");
                    break;

                // Unknown answer type ("taglist", future types) → no check (forward-compatible).
            }
        }
    }

    private static void ValidateChoice(string toolCode, ToolQuestionRow q, JsonElement answer)
    {
        var options = QuestionOptions.Parse(q.OptionsJson);
        if (options.Count == 0) return;

        var raw = Normalize(answer);
        if (!options.Any(o => string.Equals(o.Value, raw, StringComparison.OrdinalIgnoreCase)))
            throw ValuationException.BadRequest($"مقدار «{q.QuestionId}» یکی از گزینه‌های مجاز نیست. ({toolCode})");
    }

    private static void ValidateMulti(string toolCode, ToolQuestionRow q, JsonElement answer)
    {
        var options = QuestionOptions.Parse(q.OptionsJson);
        if (options.Count == 0) return;

        if (answer.ValueKind != JsonValueKind.Array)
            throw ValuationException.BadRequest($"مقدار «{q.QuestionId}» باید فهرستی از گزینه‌ها باشد. ({toolCode})");

        foreach (var item in answer.EnumerateArray())
        {
            var raw = Normalize(item);
            if (!options.Any(o => string.Equals(o.Value, raw, StringComparison.OrdinalIgnoreCase)))
                throw ValuationException.BadRequest($"گزینه «{raw}» در «{q.QuestionId}» مجاز نیست. ({toolCode})");
        }
    }

    private static void ValidateNumber(string toolCode, ToolQuestionRow q, JsonElement answer)
    {
        double value;
        if (answer.ValueKind == JsonValueKind.Number)
            value = answer.GetDouble();
        else if (answer.ValueKind == JsonValueKind.String &&
                 double.TryParse(answer.GetString(), System.Globalization.NumberStyles.Float,
                     System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            value = parsed;
        else
            throw ValuationException.BadRequest($"مقدار «{q.QuestionId}» باید عددی باشد. ({toolCode})");

        if (q.ScaleMin is { } min && value < min || q.ScaleMax is { } max && value > max)
            throw ValuationException.BadRequest(
                $"مقدار «{q.QuestionId}» خارج از بازه مجاز {q.ScaleMin ?? int.MinValue} تا {q.ScaleMax ?? int.MaxValue} است. ({toolCode})");
    }

    /// <summary>Depth-first search for the first property named <paramref name="key"/>.</summary>
    private static bool TryFindAnswer(JsonElement el, string key, out JsonElement value)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                if (el.TryGetProperty(key, out value)) return true;
                foreach (var p in el.EnumerateObject())
                    if (TryFindAnswer(p.Value, key, out value)) return true;
                break;

            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    if (TryFindAnswer(item, key, out value)) return true;
                break;
        }

        value = default;
        return false;
    }

    /// <summary>Tolerant scalar comparison: numbers and numeric strings unify ("1" ≡ 1).</summary>
    private static string Normalize(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString() ?? "",
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => el.GetRawText(),
    };
}

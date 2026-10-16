using System.Text.Json;
using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Base;
using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Tools;

/// <summary>A question with its labels resolved for the request language.</summary>
public sealed record LocalizedQuestion(
    string Id, string? SectionKey, string? SectionTitle, string Text,
    string? Placeholder, string? HelpText,
    string AnswerType, bool Required, int? Min, int? Max,
    List<(string Value, string Label)> Options);

/// <summary>Tool info + questions, localized for the request language.</summary>
public sealed record LocalizedToolContent(
    string ToolCode,
    string Lang,
    string Title,
    string? Description,
    List<LocalizedQuestion> Questions);

/// <summary>
///     Loads and localizes DB-driven tool content: the tool's own info
///     (title/description) and its questions (text, section title, placeholder,
///     help text, option labels).
///
///     Storage model: the per-tool <c>&lt;Tool&gt;Questions</c> table holds the input
///     definitions with base columns in the default language (fa); the per-tool
///     <c>&lt;Tool&gt;Localizations</c> table holds one row per (language, question)
///     — QuestionId "" is the tool-level info row. Fallback is <b>per field</b>:
///     localization column → fa base column → the key + a logged warning.
/// </summary>
public interface IToolContentResolver
{
    /// <summary>The request language being served (via ILanguageResolver).</summary>
    string Lang { get; }

    /// <summary>Loads the tool's info + questions localized for the request language.</summary>
    Task<LocalizedToolContent> GetAsync(string toolCode, CancellationToken ct = default);
}

public sealed class ToolContentResolver(
    ILanguageResolver language,
    IOptions<LanguageOptions> options,
    IQueryRepository<ToolForm> forms,
    IToolQuestionQueries questions,
    IToolContentQueries localizations,
    ILogger<ToolContentResolver> logger) : IToolContentResolver
{
    public string Lang => language.Lang;

    public async Task<LocalizedToolContent> GetAsync(string toolCode, CancellationToken ct = default)
    {
        var form = await forms.TableNoTracking
            .FirstOrDefaultAsync(t => t.ToolCode == toolCode, ct)
            ?? throw global::Common.Exceptions.ValuationException.NotFound($"ابزار «{toolCode}» یافت نشد.");

        var rows = await questions.ByTool(toolCode).OrderBy(q => q.SortOrder).ToListAsync(ct);
        var locRows = await localizations.ByTool(toolCode).ToListAsync(ct);

        var byLang = locRows.ToLookup(l => l.Language, StringComparer.OrdinalIgnoreCase);
        var requested = byLang[Lang].ToDictionary(l => l.QuestionId, StringComparer.Ordinal);
        var defaultLang = options.Value.Default;
        var fallback = string.Equals(Lang, defaultLang, StringComparison.OrdinalIgnoreCase)
            ? null
            : byLang[defaultLang].ToDictionary(l => l.QuestionId, StringComparer.Ordinal);

        // Tool-level info row (QuestionId == "") — localized title/description;
        // the fa base values live on the ToolForm row itself.
        ToolLocalizationRow? info = requested.GetValueOrDefault(ToolLocalizationBase.ToolLevelQuestionId)
                                    ?? fallback?.GetValueOrDefault(ToolLocalizationBase.ToolLevelQuestionId);

        var title = Field(info?.Title, form.Title, "title", toolCode);
        var description = info?.Description ?? form.Description;

        var localized = new List<LocalizedQuestion>();
        foreach (var q in rows)
        {
            var loc = requested.GetValueOrDefault(q.QuestionId) ?? fallback?.GetValueOrDefault(q.QuestionId);
            var logKey = $"{toolCode}/{q.QuestionId}";

            var text = Field(loc?.Text, q.Text, "text", logKey);
            var optionsList = ResolveOptions(q.OptionsJson, loc?.OptionsJson, logKey);

            localized.Add(new LocalizedQuestion(
                q.QuestionId,
                q.SectionKey,
                loc?.SectionTitle ?? q.SectionTitle,
                text,
                loc?.Placeholder ?? q.Placeholder,
                loc?.HelpText ?? q.HelpText,
                q.AnswerType, q.IsRequired, q.ScaleMin, q.ScaleMax,
                optionsList));
        }

        return new LocalizedToolContent(toolCode, Lang, title, description, localized);
    }

    /// <summary>
    ///     Per-field fallback: request-language value → default-language (fa) value →
    ///     the key + a logged warning (never render empty for required text).
    /// </summary>
    private string Field(string? translated, string? baseValue, string field, string logKey)
    {
        if (!string.IsNullOrWhiteSpace(translated))
            return translated;

        if (!string.IsNullOrWhiteSpace(baseValue))
            return baseValue;

        logger.LogWarning("No {Lang} translation and no fa value for {Key}.{Field} — returning key", Lang, logKey, field);
        return $"<{logKey}.{field}>";
    }

    /// <summary>Option labels: request-language row → fa base row, matched by option value.</summary>
    private static List<(string Value, string Label)> ResolveOptions(string baseJson, string? translatedJson, string logKey)
    {
        var rows = QuestionOptions.Parse(baseJson);
        if (rows.Count == 0 || string.IsNullOrWhiteSpace(translatedJson))
            return rows;

        var translated = QuestionOptions.Parse(translatedJson)
            .ToDictionary(o => o.Value, o => o.Label, StringComparer.Ordinal);

        return rows.Select(row =>
            translated.TryGetValue(row.Value, out var label) && !string.IsNullOrWhiteSpace(label)
                ? (row.Value, label)
                : row)
            .ToList();
    }
}

/// <summary>Tolerant {value,label}[] parsing shared by the validator and the content resolver.</summary>
public static class QuestionOptions
{
    public static List<(string Value, string Label)> Parse(string? optionsJson)
    {
        var list = new List<(string, string)>();
        if (string.IsNullOrWhiteSpace(optionsJson)) return list;

        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(optionsJson);
            if (doc.ValueKind != JsonValueKind.Array) return list;

            foreach (var el in doc.EnumerateArray())
            {
                var value = el.TryGetProperty("value", out var v) ? v.ToString() : "";
                var label = el.TryGetProperty("label", out var l) ? l.ToString() : value;
                list.Add((value, label));
            }
        }
        catch (JsonException)
        {
            /* malformed options degrade to "no options" — same as ToolQuestion.Options() */
        }

        return list;
    }
}

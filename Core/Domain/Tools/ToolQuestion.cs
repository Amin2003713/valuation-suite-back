using Common.Base;
using System.Text.Json;

namespace Domain.Tools;

/// <summary>
///     One editable question of a tool's questionnaire, persisted separately from
///     the immutable form schema so admins can retitle questions and option labels
///     at runtime (admin content management) without touching code.
///     Question texts/option labels the client renders; ids map to the runner input.
/// </summary>
public sealed class ToolQuestion : BaseEntity
{
    public string ToolCode { get; set; } = string.Empty;

    /// <summary>Stable id the runner binds answers to (e.g. "tmRegistered", "A1", "innovation_q1").</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>Section/group key for grouping in UI (e.g. "trademark", "A", "innovation").</summary>
    public string? SectionKey { get; set; }

    /// <summary>Display title of the section (e.g. "علامت‌های تجاری", "A — وضعیت حقوقی").</summary>
    public string? SectionTitle { get; set; }

    /// <summary>The question text shown to the user (editable).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>JSON array of {value,label} option rows; "[]" when the question is free-form/scored.</summary>
    public string OptionsJson { get; set; } = "[]";

    public int SortOrder { get; set; }

    public static ToolQuestion Create(
        string toolCode, string questionId, string? sectionKey, string? sectionTitle,
        string text, string optionsJson, int sortOrder) =>
        new()
        {
            ToolCode = toolCode,
            QuestionId = questionId,
            SectionKey = sectionKey,
            SectionTitle = sectionTitle,
            Text = text,
            OptionsJson = string.IsNullOrWhiteSpace(optionsJson) ? "[]" : optionsJson,
            SortOrder = sortOrder,
        };

    public void UpdateText(string text) => Text = text;

    public void UpdateOptions(IEnumerable<(string Value, string Label)> options) =>
        OptionsJson = JsonSerializer.Serialize(
            options.Select(o => new { value = o.Value, label = o.Label }),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    /// <summary>Deserialized option rows (value/label) for read models.</summary>
    public List<(string Value, string Label)> Options()
    {
        try
        {
            var doc = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(OptionsJson) ? "[]" : OptionsJson);
            var list = new List<(string, string)>();
            if (doc.ValueKind == JsonValueKind.Array)
                foreach (var el in doc.EnumerateArray())
                {
                    var value = el.TryGetProperty("value", out var v) ? v.ToString() : "";
                    var label = el.TryGetProperty("label", out var l) ? l.ToString() : value;
                    list.Add((value, label));
                }
            return list;
        }
        catch
        {
            return [];
        }
    }
}

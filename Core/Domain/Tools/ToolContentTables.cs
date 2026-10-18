using Common.Base;
using System.Text.Json;

namespace Domain.Tools;

/* ═══════════════════════════════════════════════════════════════════════
 * Per-tool content tables.
 *
 * Every tool owns TWO content tables besides its submissions table:
 *   • <Tool>Questions        — the tool's input definitions (question text,
 *                              label, placeholder, help text, option labels,
 *                              answer type, required, scale bounds).
 *   • <Tool>Localizations    — per-language overrides of the tool info
 *                              (title/description) and of every question
 *                              label, one row per (language, question).
 *
 * The fa (default language) text lives in the Question row's base columns;
 * localization rows carry every OTHER language, one row per (Language,
 * QuestionId) — per-field fallback = localization column → base column →
 * key + logged warning (see Application.Tools.ToolContentResolver).
 *
 * Adding a tool = one Question class + one Localization class + registry
 * entries + one migration; content comes from the one seed.
 * ═══════════════════════════════════════════════════════════════════════ */

/// <summary>Shared definition of one input/question row of a tool's form.</summary>
public abstract class ToolQuestionBase : BaseEntity
{
    /// <summary>Tool this question belongs to (also implied by the table).</summary>
    public string ToolCode { get; set; } = string.Empty;

    /// <summary>Stable id the runner binds the answer to (e.g. "tmRegistered", "A1", "trl").</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>Section/group key for grouping in the UI (e.g. "trademark", "A", "kth").</summary>
    public string? SectionKey { get; set; }

    /// <summary>Display title of the section (default language = fa base column).</summary>
    public string? SectionTitle { get; set; }

    /// <summary>The question/label text shown to the user (default language = fa base column).</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Placeholder shown in the input (default language = fa base column).</summary>
    public string? Placeholder { get; set; }

    /// <summary>Help/hint text shown under the question (default language = fa base column).</summary>
    public string? HelpText { get; set; }

    /// <summary>JSON array of {value,label} option rows; "[]" when the input is free-form/scored.</summary>
    public string OptionsJson { get; set; } = "[]";

    /// <summary>
    ///     Answer shape used for server-side validation of submitted input:
    ///     "scale" | "number" (numeric, bounds in ScaleMin/ScaleMax),
    ///     "choice" (one of the option values), "multi" (array of option values),
    ///     "taglist" (unvalidated list), "text".
    /// </summary>
    public string AnswerType { get; set; } = "text";

    /// <summary>True when the runner requires an answer for this key (validated server-side).</summary>
    public bool IsRequired { get; set; }

    public int? ScaleMin { get; set; }
    public int? ScaleMax { get; set; }

    public int SortOrder { get; set; }

    /// <summary>Deserialized option rows (value/label) — same tolerance as the legacy ToolQuestion.</summary>
    public List<(string Value, string Label)> Options()
    {
        try
        {
            var doc = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(
                string.IsNullOrWhiteSpace(OptionsJson) ? "[]" : OptionsJson);
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

    public void UpdateText(string text) => Text = text;

    public void UpdateOptions(IEnumerable<(string Value, string Label)> options) =>
        OptionsJson = System.Text.Json.JsonSerializer.Serialize(
            options.Select(o => new { value = o.Value, label = o.Label }),
            new System.Text.Json.JsonSerializerOptions
                { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
}

/// <summary>Shared definition of one localization row (language × question or tool info).</summary>
public abstract class ToolLocalizationBase : BaseEntity
{
    /// <summary>Sentinel QuestionId for the tool-level info row (title/description).</summary>
    public const string ToolLevelQuestionId = "";

    public string ToolCode { get; set; } = string.Empty;

    /// <summary>Language code this row translates ("en", …). fa lives in the base columns.</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Question this row translates; <see cref="ToolLevelQuestionId"/> = the tool info row.</summary>
    public string QuestionId { get; set; } = ToolLevelQuestionId;

    // ── tool-level info (QuestionId == ToolLevelQuestionId) ──
    public string? Title { get; set; }
    public string? Description { get; set; }

    // ── question-level fields (QuestionId = a question id) ──
    public string? Text { get; set; }
    public string? SectionTitle { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }

    /// <summary>JSON array of {value,label} option rows in this language, or null.</summary>
    public string? OptionsJson { get; set; }
}

/* ─── Concrete per-tool entities (EF maps each to its own table) ─── */

public sealed class IdeaAssessmentQuestion : ToolQuestionBase;
public sealed class InnovationReadinessQuestion : ToolQuestionBase;
public sealed class IpAuditQuestion : ToolQuestionBase;
public sealed class JobEvaluationQuestion : ToolQuestionBase;
public sealed class WipoDiagnosticsQuestion : ToolQuestionBase;
public sealed class IpscoreQuestion : ToolQuestionBase;
public sealed class PatentSearchQuestion : ToolQuestionBase;
public sealed class StartupValuationQuestion : ToolQuestionBase;
public sealed class BrandValuationQuestion : ToolQuestionBase;
public sealed class TrademarkValuationQuestion : ToolQuestionBase;
public sealed class PatentValuationQuestion : ToolQuestionBase;
public sealed class PharmaPatentValuationQuestion : ToolQuestionBase;
public sealed class IntangibleAssetsQuestion : ToolQuestionBase;
public sealed class KnowhowValuationQuestion : ToolQuestionBase;
public sealed class IconScorecardQuestion : ToolQuestionBase;
public sealed class BinomialValuationQuestion : ToolQuestionBase;
public sealed class SoftwareValuationQuestion : ToolQuestionBase;
public sealed class RnpvCalculatorQuestion : ToolQuestionBase;
public sealed class PatentValueIndexQuestion : ToolQuestionBase;
public sealed class KnowhowProQuestion : ToolQuestionBase;
public sealed class ExportReadinessQuestion : ToolQuestionBase;
public sealed class DigitalAiReadinessQuestion : ToolQuestionBase;
public sealed class MaturityAssessmentQuestion : ToolQuestionBase;

public sealed class StartupAssessmentQuestion : ToolQuestionBase;

public sealed class IdeaAssessmentLocalization : ToolLocalizationBase;
public sealed class InnovationReadinessLocalization : ToolLocalizationBase;
public sealed class IpAuditLocalization : ToolLocalizationBase;
public sealed class JobEvaluationLocalization : ToolLocalizationBase;
public sealed class WipoDiagnosticsLocalization : ToolLocalizationBase;
public sealed class IpscoreLocalization : ToolLocalizationBase;
public sealed class PatentSearchLocalization : ToolLocalizationBase;
public sealed class StartupValuationLocalization : ToolLocalizationBase;
public sealed class BrandValuationLocalization : ToolLocalizationBase;
public sealed class TrademarkValuationLocalization : ToolLocalizationBase;
public sealed class PatentValuationLocalization : ToolLocalizationBase;
public sealed class PharmaPatentValuationLocalization : ToolLocalizationBase;
public sealed class IntangibleAssetsLocalization : ToolLocalizationBase;
public sealed class KnowhowValuationLocalization : ToolLocalizationBase;
public sealed class IconScorecardLocalization : ToolLocalizationBase;
public sealed class BinomialValuationLocalization : ToolLocalizationBase;
public sealed class SoftwareValuationLocalization : ToolLocalizationBase;
public sealed class RnpvCalculatorLocalization : ToolLocalizationBase;
public sealed class PatentValueIndexLocalization : ToolLocalizationBase;
public sealed class KnowhowProLocalization : ToolLocalizationBase;
public sealed class ExportReadinessLocalization : ToolLocalizationBase;
public sealed class DigitalAiReadinessLocalization : ToolLocalizationBase;
public sealed class MaturityAssessmentLocalization : ToolLocalizationBase;

public sealed class StartupAssessmentLocalization : ToolLocalizationBase;

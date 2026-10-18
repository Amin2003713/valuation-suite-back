using System.Text.Json;
using System.Text.Json.Nodes;
using Common.Exceptions;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════════════
 * Brand Valuation v2 — the standalone HTML client ("brand-valuation-corrected")
 * embeds a 13-sheet audited workbook (Specifications, Guide, Replacement cost,
 * Direct income, Market, Scorecard, Relief from royalty, With & without,
 * Excess earnings, Binomial, Black–Scholes, Sensitivity, Comparison & blending,
 * References) and recalculates every derived cell through HyperFormula in the
 * browser. Porting that formula graph 1:1 to C# was out of scope, so the
 * engine is moved INTO the client — exactly as the standalone HTML does — and
 * this runner becomes the server's book(r) for the workbook:
 *
 *   • schema check: snapshot/{schema:01.02 inFormState inputs:{"r,c":…}} or the
 *     same payload wrapped with the client's evaluated overlay
 *       derived:{ sheets:{"name":{cells:{…},rows?},…}, dashboard:{…}, blendedValue }
 *   • blendedValue → OverallScore (dashboard + adviser chat data)
 *   • echoes derived for the submission record (AdvancedSections strip applies
 *     on `derived.sheets/values` only, so free users still see the blended
 *     number they can see in the paid workbook anyway... for v2 Advanced is
 *     priced at 0 → strip is a no-op; kept for forward compatibility).
 *
 * The inactive runner's brand-valuation internals (RFR etc.) remain in git
 * history; the workbook snapshot format keeps full auditability of inputs.
 * ════════════════════════════════════════════════════════════════════════════ */

public sealed class BrandValuationRunner : IToolRunner
{
    public string ToolCode => "BRAND-VAL";

    private const string SchemaV2 = "brand-valuation-v2";

    public ToolRunOutcome Run(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
            throw ValuationException.BadRequest("ورودی ارزش‌گذاری برند نامعتبر است.");

        string? schema = TryString(input, "schemaVal") ?? TryString(input, "schema") ?? TryString(input, "schemaForm");
        if (schema is not null && schema != SchemaV2)
            throw ValuationException.BadRequest(
                "ساختار داده ارزش‌گذاری برند قدیمی است؛ صفحه را نوسازی کنید یا داده JSON معتبر بارگذاری کنید.");

        var inputs = input.TryGetProperty("inputs", out var inp) && inp.ValueKind == JsonValueKind.Object
            ? (JsonElement?)inp
            : null;
        if (inputs is null)
            throw ValuationException.BadRequest(
                "ورودی‌های ورک‌بوک ارزش‌گذاری برند ارسال نشده است.");

        double? blended = TryNumber(input, "blendedValue")
            ?? TryNumber(input, "blended")
            ?? TryNumber(input, "totalValue");

        // Reconstruct the user-facing title from Specifications sheet cells.
        var name =
            TryString(inputs.Value, "4_1")   // B5 — brand name
            ?? TryString(inputs.Value, "5_1") // B6
            ?? null;

        var result = new BrandV2Result
        {
            Engine = "workbook-v2",
            BlendedValue = blended,
            Inputs = inputs.Value,
        };

        // Persist the client's evaluated overlay, too, so the adviser chat /
        // ResultView can render per-sheet summaries without a HyperFormula host.
        if (input.TryGetProperty("derived", out var derived) && derived.ValueKind == JsonValueKind.Object)
            result.Derived = derived;

        return new ToolRunOutcome(result, blended, name);
    }

    private static string? TryString(JsonElement e, string prop)
        => e.ValueKind == JsonValueKind.Object &&
           e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static double? TryNumber(JsonElement e, string prop)
        => e.ValueKind == JsonValueKind.Object &&
           e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDouble()
            : null;

    public sealed class BrandV2Result
    {
        public string Engine { get; set; } = "workbook-v2";
        public double? BlendedValue { get; set; }
        public object? Inputs { get; set; }
        public object? Derived { get; set; }
        public bool AdvancedIncluded { get; set; } = true;
    }
}

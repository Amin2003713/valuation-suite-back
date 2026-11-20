using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * rNPV calculator — ported from app/rnpv-calculator/logic.ts, operation for
 * operation:
 *   - Cumulative POS is the running product of each stage's POS.
 *   - Milestone nominal amounts are risked by the cumulative POS up to and
 *     including their own stage (posApplicable = [1, cum[0..4]]).
 *   - Commercial cash (royalty + sales milestones) is risked by the overall
 *     POS (cum[4]).
 *   - Sales milestones trigger on the year cumulative net sales CROSSES the
 *     threshold (prev < th <= cum).
 *   - discount = 1 / (1 + discountRate) ^ year, applied to every row.
 * The five stages and 13 terms are validated for presence; labels come from
 * RnpvOptions.MilestoneLabels.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class RnpvCalculatorRunner(IOptions<RnpvOptions> options) : IToolRunner
{
    public string ToolCode => "RNPV";

    private static readonly string[] RequiredTerms =
        ["upfront", "m1", "m2", "m3", "m4", "m5", "royaltyRate", "peakSales", "discountRate", "th1", "sm1", "th2", "sm2"];

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<RnpvInput>(input);
        var o = options.Value;

        if (i.Stages is null || i.Stages.Count != o.StageCount)
            throw ValuationException.BadRequest($"دقیقاً {o.StageCount} مرحله لازم است.");
        if (i.Terms is null)
            throw ValuationException.BadRequest("شروط قرارداد ارسال نشده است.");
        if (i.Ramp is null || i.Ramp.Count == 0)
            throw ValuationException.BadRequest("منحنی فروش (ramp) ارسال نشده است.");

        foreach (var s in i.Stages)
        {
            if (s.Pos is < 0 or > 1)
                throw ValuationException.BadRequest($"POS مرحله «{s.Name}» باید بین ۰ تا ۱ باشد.");
            if (s.Duration < 0)
                throw ValuationException.BadRequest($"مدت مرحله «{s.Name}» نمی‌تواند منفی باشد.");
        }
        foreach (var t in RequiredTerms)
        {
            if (!i.Terms.ContainsKey(t))
                throw ValuationException.BadRequest($"شرط «{t}» ارسال نشده است.");
        }
        if (i.Ramp.Any(v => v < 0))
            throw ValuationException.BadRequest("منحنی فروش نمی‌تواند مقدار منفی داشته باشد.");

        var result = Compute(i, o);
        var tornado = Sensitivity(i, o, result.Rnpv);
        return new ToolRunOutcome(result with { Tornado = tornado }, result.Rnpv, i.Name);
    }

    /// <summary>
    ///     The ported rNPV model, unchanged. Extracted so the sensitivity sweep can
    ///     re-run exactly this calculation on perturbed copies of the input, instead
    ///     of approximating the impact of a driver.
    /// </summary>
    private static RnpvResult Compute(RnpvInput i, RnpvOptions o)
    {
        double T(string key) => i.Terms![key];

        // ── Cumulative POS and milestone years ───────────────────────────────
        var cum = new List<double>();
        double running = 1;
        foreach (var s in i.Stages)
        {
            running *= s.Pos;
            cum.Add(running);
        }

        var milestoneYears = new List<double> { 0 };
        double tAcc = 0;
        foreach (var s in i.Stages)
        {
            tAcc += s.Duration;
            milestoneYears.Add(tAcc);
        }

        var launchYear = milestoneYears[5];
        var amounts = new[] { T("upfront"), T("m1"), T("m2"), T("m3"), T("m4"), T("m5") };
        var posApplicable = new[] { 1.0, cum[0], cum[1], cum[2], cum[3], cum[4] };
        var overallPos = cum[4];

        // ── Year rows ────────────────────────────────────────────────────────
        var totalYears = launchYear + i.Ramp.Count;
        var years = new List<YearRowC>();
        double cumNetSales = 0, prevCumNetSales = 0;

        for (var y = 0; y < totalYears; y++)
        {
            double milestoneNominal = 0;
            double? milestonePos = null;
            var milestoneLabel = "";

            for (var idx = 0; idx < 6; idx++)
            {
                if (milestoneYears[idx] == y)
                {
                    milestoneNominal += amounts[idx];
                    milestonePos = posApplicable[idx];
                    milestoneLabel = o.MilestoneLabels[idx];
                }
            }

            double netSales = 0, royaltyNominal = 0, salesMilestoneNominal = 0;
            if (y >= launchYear && y < launchYear + i.Ramp.Count)
            {
                var rampIdx = (int)(y - launchYear);
                netSales = i.Ramp[rampIdx] * T("peakSales"); // $mm
                prevCumNetSales = cumNetSales;
                cumNetSales += netSales;
                royaltyNominal = netSales * T("royaltyRate") * 1000; // $k
                if (cumNetSales >= T("th1") && prevCumNetSales < T("th1")) salesMilestoneNominal += T("sm1");
                if (cumNetSales >= T("th2") && prevCumNetSales < T("th2")) salesMilestoneNominal += T("sm2");
            }

            years.Add(new YearRowC
            {
                Year = y,
                MilestoneNominal = milestoneNominal,
                MilestoneLabel = milestoneLabel,
                MilestonePOS = milestonePos,
                RoyaltyNominal = royaltyNominal,
                SalesMilestoneNominal = salesMilestoneNominal,
                NetSales = netSales,
            });
        }

        foreach (var yo in years)
        {
            yo.MilestoneRiskAdj = yo.MilestoneNominal * (yo.MilestonePOS ?? 0);
            yo.CommercialRiskAdj = (yo.RoyaltyNominal + yo.SalesMilestoneNominal) * overallPos;
            yo.TotalRiskAdj = yo.MilestoneRiskAdj + yo.CommercialRiskAdj;
            yo.Discount = 1 / Math.Pow(1 + T("discountRate"), yo.Year);
            yo.Pv = yo.TotalRiskAdj * yo.Discount;
            yo.MilestoneNominalPV = yo.MilestoneNominal * yo.Discount;
            yo.CommercialNominalPV = (yo.RoyaltyNominal + yo.SalesMilestoneNominal) * yo.Discount;
        }

        var rnpv = years.Sum(y => y.Pv);
        var unriskedNpv = years.Sum(y =>
            (y.MilestoneNominal + y.RoyaltyNominal + y.SalesMilestoneNominal) * y.Discount);

        return new RnpvResult(
            years.ToArray(), rnpv, unriskedNpv, overallPos, cum.ToArray(), launchYear, []);
    }

    /// <summary>
    ///     Tornado analysis: each driver moves ±20% around the submitted value and the
    ///     same model is re-run, so a row is a real rNPV result, not an estimate.
    ///     Stage POS is scaled with a 0–1 clamp because it is a probability.
    /// </summary>
    private static TornadoRow[] Sensitivity(RnpvInput input, RnpvOptions o, double baseRnpv)
    {
        const double delta = 0.2;
        var rows = new List<TornadoRow>();

        void Add(string label, Func<double, RnpvInput> mutate)
        {
            var high = Compute(mutate(1 + delta), o).Rnpv;
            var low = Compute(mutate(1 - delta), o).Rnpv;
            rows.Add(new TornadoRow(label, Math.Min(low, high), Math.Max(low, high), baseRnpv));
        }

        RnpvInput ScaledTerm(string key, double factor)
        {
            var clone = Clone(input);
            clone.Terms![key] *= factor;
            return clone;
        }

        Add("نرخ تنزیل", f => ScaledTerm("discountRate", f));
        Add("نرخ رویالتی", f => ScaledTerm("royaltyRate", f));
        Add("اوج فروش", f => ScaledTerm("peakSales", f));
        Add("POS مراحل", f =>
        {
            var clone = Clone(input);
            foreach (var s in clone.Stages!)
                s.Pos = Math.Clamp(s.Pos * f, 0, 1);
            return clone;
        });

        return rows.OrderByDescending(r => Math.Abs(r.High - r.Low)).ToArray();
    }

    private static RnpvInput Clone(RnpvInput i) => new()
    {
        Name = i.Name,
        Stages = i.Stages!
            .Select(s => new RnpvStageInput { Name = s.Name, Duration = s.Duration, Pos = s.Pos })
            .ToList(),
        Terms = new Dictionary<string, double>(i.Terms!),
        Ramp = [.. i.Ramp!],
    };

    public sealed class RnpvInput
    {
        public string? Name { get; set; }
        public List<RnpvStageInput>? Stages { get; set; }
        /** term key → value (rates as raw fractions, e.g. royaltyRate 0.03). */
        /// <summary>
        ///     term key → value. Accepts either a bare number or the tool page's
        ///     `{ label, val, note, isPct }` object (it stores terms that way for
        ///     save/restore) — see <see cref="TermMapConverter" />.
        /// </summary>
        [JsonConverter(typeof(TermMapConverter))]
        public Dictionary<string, double>? Terms { get; set; }
        public List<double>? Ramp { get; set; }
    }

    public sealed class RnpvStageInput
    {
        public string? Name { get; set; }
        public double Duration { get; set; }
        public double Pos { get; set; }
    }

    /// <summary>
    ///     Binds `terms` from either shape a caller can legitimately send:
    ///     <c>{ "peakSales": 500 }</c> (documented API) or
    ///     <c>{ "peakSales": { "label": "…", "val": 500, "isPct": false } }</c>
    ///     (what the rNPV page holds and restores). Previously the object shape threw
    ///     a JsonException and the run answered 400 INVALID_INPUT.
    /// </summary>
    private sealed class TermMapConverter : JsonConverter<Dictionary<string, double>>
    {
        public override Dictionary<string, double>? Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("terms must be an object.");

            var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) return map;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("terms entries must be key/value pairs.");

                var key = reader.GetString() ?? throw new JsonException("term key is missing.");
                reader.Read();
                map[key] = ReadValue(ref reader);
            }

            throw new JsonException("unterminated terms object.");
        }

        /// <summary>Reads a number, numeric string, or <c>{ …, val: number }</c>.</summary>
        private static double ReadValue(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetDouble();

                case JsonTokenType.String:
                    return double.TryParse(
                        reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)
                        ? s
                        : throw new JsonException("term is not a number.");

                case JsonTokenType.StartObject:
                    double? val = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject) break;
                        if (reader.TokenType != JsonTokenType.PropertyName)
                            throw new JsonException("term object is malformed.");

                        var name = reader.GetString();
                        reader.Read();
                        if (string.Equals(name, "val", StringComparison.OrdinalIgnoreCase))
                            val = ReadValue(ref reader);
                        else
                            ConsumeValue(ref reader);
                    }
                    return val ?? throw new JsonException("term object has no numeric 'val'.");

                default:
                    throw new JsonException("term must be a number or an object with 'val'.");
            }
        }

        /// <summary>Leaves the reader on the last token of an ignored value.</summary>
        private static void ConsumeValue(ref Utf8JsonReader reader)
        {
            if (reader.TokenType is not (JsonTokenType.StartObject or JsonTokenType.StartArray))
                return;

            var depth = 0;
            do
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) depth++;
                else if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray) depth--;
                if (depth == 0) return;
            } while (reader.Read());
        }

        public override void Write(
            Utf8JsonWriter writer, Dictionary<string, double> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var (key, v) in value) writer.WriteNumber(key, v);
            writer.WriteEndObject();
        }
    }

    public sealed class YearRowC
    {
        public int Year { get; set; }
        public double MilestoneNominal { get; set; }
        public string MilestoneLabel { get; set; } = "";
        /** Applied cumulative POS; omitted (undefined) when no milestone falls in this year. */
        [JsonPropertyName("milestonePOS")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MilestonePOS { get; set; }
        public double RoyaltyNominal { get; set; }
        public double SalesMilestoneNominal { get; set; }
        public double NetSales { get; set; }
        public double MilestoneRiskAdj { get; set; }
        public double CommercialRiskAdj { get; set; }
        public double TotalRiskAdj { get; set; }
        public double Discount { get; set; }
        public double Pv { get; set; }
        public double MilestoneNominalPV { get; set; }
        public double CommercialNominalPV { get; set; }
    }

    public sealed record RnpvResult(
        YearRowC[] Years,
        [property: JsonPropertyName("rNPV")] double Rnpv,
        [property: JsonPropertyName("unriskedNPV")] double UnriskedNpv,
        [property: JsonPropertyName("overallPOS")] double OverallPos,
        double[] Cum,
        double LaunchYear,
        // ±20% one-at-a-time sensitivity, widest swing first.
        TornadoRow[] Tornado);

    /// <summary><see cref="Low"/> and <see cref="High"/> are rNPV with the driver at −20% and +20%.</summary>
    public sealed record TornadoRow(string Label, double Low, double High, double Base);
}

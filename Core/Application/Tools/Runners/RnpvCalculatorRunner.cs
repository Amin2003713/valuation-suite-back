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

        var result = new RnpvResult(
            years.ToArray(), rnpv, unriskedNpv, overallPos, cum.ToArray(), launchYear);
        return new ToolRunOutcome(result, rnpv, i.Name);
    }

    public sealed class RnpvInput
    {
        public string? Name { get; set; }
        public List<RnpvStageInput>? Stages { get; set; }
        /** term key → value (rates as raw fractions, e.g. royaltyRate 0.03). */
        public Dictionary<string, double>? Terms { get; set; }
        public List<double>? Ramp { get; set; }
    }

    public sealed class RnpvStageInput
    {
        public string? Name { get; set; }
        public double Duration { get; set; }
        public double Pos { get; set; }
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
        double LaunchYear);
}

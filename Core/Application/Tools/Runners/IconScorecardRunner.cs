using System.Text.Json;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Icon Scorecard (Trademark Icon Scorecard) — ported from
 * app/icon-scorecard/logic.ts, which itself was converted from the
 * original Icon_scorecard.html. RFR (primary) / Premium / Market /
 * Scorecard-as-control → reconciliation-weighted final + MC range.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class IconScorecardRunner : IToolRunner
{
    public string ToolCode => "ICON-SCORECARD";

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<IconInput>(input);

        var cf   = ClassCoef(s.ClassCount, s.Items, s.NiceClass, s.OtherClasses);
        var risk = Math.Max(0.5, 1 - s.LegalRisk / 10.0);

        // Relief from Royalty — 10-year discounted royalty savings (primary).
        double rfr = 0;
        for (var y = 1; y <= 10; y++)
        {
            var cash = Math.Max(0, s.BaseRevenue * Math.Pow(1 + s.Growth, y) * s.Royalty * (1 - s.Tax) - s.Maintenance);
            rfr += cash * s.Attribution * risk * cf / Math.Pow(1 + s.Discount, y);
        }

        // Premium Profit — 5-year discounted premium benefit.
        double premium = 0;
        for (var z = 1; z <= 5; z++)
        {
            premium += s.BaseRevenue * Math.Pow(1 + s.Growth, z) * s.PremiumRate * s.Margin
                       * (1 - s.Tax) * risk * cf / Math.Pow(1 + s.Discount, z);
        }

        // Market — royalty capitalisation of comparable licences.
        var market = s.BaseRevenue * s.MarketRoyalty * 10 * s.Attribution * cf * s.Similarity * s.Geo * s.LegalFactor;

        // Scorecard is not an independent value — it adjusts/controls RFR.
        var scoreTotal = s.Scorecard.Sum(r => r.Weight * r.Score);
        var scorecard  = rfr * (0.5 + 0.5 * (scoreTotal / 100));

        // Monte Carlo proxy: RFR is the central estimate; the uncertainty band
        // scales with how the MC growth bounds compare to the point growth.
        var gScale = s.Growth > 0 ? 1.0 : 1.0;
        var lo = s.Growth > 0 ? Math.Max(0.4, Math.Min(1, s.McGrowthLow / s.Growth)) : 1;
        var hi = s.Growth > 0 ? Math.Max(1, Math.Min(2.2, s.McGrowthHigh / s.Growth)) : 1;
        var mcRangeLow  = rfr * 0.65 * lo;
        var mcRangeHigh = rfr * 1.5 * hi;

        // Weighted final — RFR primary, market and scorecard are controls.
        var wRfr = 0.6 * 0.9;
        var wMkt = 0.25 * 0.55;
        var wSc  = 0.15 * 0.15;
        var final = (rfr * wRfr + market * wMkt + rfr * wSc) / (wRfr + wMkt + wSc);

        var result = new IconResult(
            Rfr: rfr,
            Premium: premium,
            Market: market,
            Scorecard: scorecard,
            ScoreTotal: scoreTotal,
            Mc: rfr,
            McLow: mcRangeLow,
            McHigh: mcRangeHigh,
            Final: final,
            Conservative: final * 0.8,
            Optimistic: final * 1.2,
            ClassCoef: cf,
            RiskFactor: risk);

        return new ToolRunOutcome(result, null, s.Name);
    }

    /// <summary>Class/breadth coefficient — mirrors the original `cf` formula.</summary>
    private static double ClassCoef(int classCount, int items, int niceClass, string? otherClasses)
    {
        var cc     = Math.Max(1, classCount);
        var it     = Math.Max(1, items);
        var nc     = Math.Max(1, Math.Min(45, niceClass));
        var others = (otherClasses ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length;

        return Math.Min(1.5, 1 + 0.04 * (cc - 1) + 0.002 * it + 0.01 * others) * (1 + 0.001 * nc);
    }

    // ── Input model ──
    public sealed class IconInput
    {
        public string? Name { get; set; }
        public string? Owner { get; set; }
        public string? Lang { get; set; }
        public int NiceClass { get; set; } = 30;
        public string? OtherClasses { get; set; }
        public int ClassCount { get; set; } = 1;
        public int Items { get; set; } = 1;

        public double BaseRevenue { get; set; } = 100_000_000_000;
        public double Growth { get; set; } = 0.2;
        public double Royalty { get; set; } = 0.02;
        public double Tax { get; set; } = 0.2;
        public double Discount { get; set; } = 0.25;
        public double Maintenance { get; set; } = 50_000_000;
        public double Attribution { get; set; } = 0.7;
        public double LegalRisk { get; set; } = 1;

        public double PremiumRate { get; set; } = 0.05;
        public double Margin { get; set; } = 0.6;

        public double MarketRoyalty { get; set; } = 0.02;
        public double Similarity { get; set; } = 0.8;
        public double Geo { get; set; } = 1;
        public double LegalFactor { get; set; } = 1;

        public double McGrowthLow { get; set; } = 0.08;
        public double McGrowthHigh { get; set; } = 0.35;

        public List<ScoreRow> Scorecard { get; set; } = [];
    }

    public sealed class ScoreRow
    {
        public string Key { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double Score { get; set; }
    }

    // ── Result model ──
    public sealed record IconResult(
        double Rfr,
        double Premium,
        double Market,
        double Scorecard,
        double ScoreTotal,
        double Mc,
        double McLow,
        double McHigh,
        double Final,
        double Conservative,
        double Optimistic,
        double ClassCoef,
        double RiskFactor);
}

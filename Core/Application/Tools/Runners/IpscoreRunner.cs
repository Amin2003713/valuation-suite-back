using System.Text.Json;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * IPscore (patent-valuation) — ported from app/patent-valuation/data.ts.
 * Per patent: 5 categories (A legal 8q, B tech 9q, C market 9q, D finance 6q,
 * E strategy 8q) scored 1..5 → percent; NPV; opportunity/risk matrix quadrant.
 * ════════════════════════════════════════════════════════════════════ */

public sealed class IpscoreRunner : IToolRunner
{
    public string ToolCode => "IPSCORE";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<IpscoreInput>(input);

        var patents = (i.Patents ?? []).Select(p =>
        {
            var scores = new IpscoreScores(
                CalcGroup(p.Answers, "A", 8),
                CalcGroup(p.Answers, "B", 9),
                CalcGroup(p.Answers, "C", 9),
                CalcGroup(p.Answers, "D", 6),
                CalcGroup(p.Answers, "E", 8));

            var overall = (int)Math.Round((scores.Legal + scores.Tech + scores.Market + scores.Finance + scores.Strategy) / 5.0);
            var npv = CalculateNpv(p.Financials);
            var quadrant = GetQuadrant(scores);

            return new IpscorePatentResult(p.Id, p.Title, p.AppNo, p.PatNo, scores, overall, npv, quadrant);
        }).ToList();

        var portfolioAvg = patents.Count > 0 ? Math.Round(patents.Average(p => p.Overall), 1) : 0;
        var totalNpv = patents.Sum(p => p.Npv);

        var result = new IpscoreResult(patents, portfolioAvg, totalNpv);
        return new ToolRunOutcome(result, portfolioAvg, i.Name);
    }

    private static int CalcGroup(Dictionary<string, double>? answers, string prefix, int count)
    {
        double sum = 0;
        for (var q = 1; q <= count; q++)
        {
            var v = 4.0; // unanswered default (matches original: || 4)
            if (answers is not null && answers.TryGetValue(prefix + q, out var stored) && stored >= 1)
                v = stored;
            sum += v;
        }
        return (int)Math.Round(sum / (count * 5.0) * 100);
    }

    private static long CalculateNpv(IpscoreFinancials? f)
    {
        if (f is null) return 0;
        var rate = f.Discount / 100.0;
        double npv = -f.Inv;
        for (var t = 1; t <= 5; t++) npv += f.Rev / Math.Pow(1 + rate, t);
        return (long)Math.Round(npv);
    }

    private static string GetQuadrant(IpscoreScores s)
    {
        var isHighOpp = s.Strategy >= 65 || s.Market >= 65;
        var isHighRisk = s.Legal < 60 || s.Finance < 60;
        if (isHighOpp && !isHighRisk) return "golden";
        if (isHighOpp && isHighRisk) return "risky";
        if (!isHighOpp && !isHighRisk) return "cheap";
        return "divest";
    }

    public sealed class IpscoreInput
    {
        public string? Name { get; set; }
        public List<IpscorePatent>? Patents { get; set; }
    }

    public sealed class IpscorePatent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? AppNo { get; set; }
        public string? PatNo { get; set; }
        public Dictionary<string, double>? Answers { get; set; }
        public IpscoreFinancials? Financials { get; set; }
    }

    public sealed class IpscoreFinancials
    {
        public double Inv { get; set; }
        public double Rev { get; set; }
        public double Discount { get; set; }
        public double Turnover { get; set; }
        public double Direct { get; set; }
        public double Indirect { get; set; }
        public double DepPeriod { get; set; }
        public double Share { get; set; }
        public string? Area { get; set; }
    }

    public sealed record IpscoreScores(int Legal, int Tech, int Market, int Finance, int Strategy);
    public sealed record IpscorePatentResult(int Id, string Title, string? AppNo, string? PatNo, IpscoreScores Scores, int Overall, long Npv, string Quadrant);
    public sealed record IpscoreResult(List<IpscorePatentResult> Patents, double PortfolioAverage, long TotalNpv);
}

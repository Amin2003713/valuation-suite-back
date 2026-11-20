using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Tools.Options;
using Common.Exceptions;
using Microsoft.Extensions.Options;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════
 * Binomial research valuation — ported from app/binomial-research-valuation/logic.ts.
 * Cox–Ross–Rubinstein binomial tree with AMERICAN exercise: each node takes
 * MAX(continue, exercise). Same derived parameters, same edge behaviour
 * (n is a free integer; the model does not guard n = 0 — the source didn't either,
 * but server-side validation now enforces the slider ranges).
 * ════════════════════════════════════════════════════════════════════ */

public sealed class BinomialResearchRunner(IOptions<BinomialOptions> options) : IToolRunner
{
    public string ToolCode => "BINOMIAL";

    public ToolRunOutcome Run(JsonElement input)
    {
        var i = ToolInput.Bind<BinomialInput>(input);
        var o = options.Value;

        if (i.S0 < o.S0Min || i.S0 > o.S0Max)
            throw ValuationException.BadRequest($"S0 باید بین {o.S0Min} و {o.S0Max} باشد.");
        if (i.K < o.KMin || i.K > o.KMax)
            throw ValuationException.BadRequest($"K باید بین {o.KMin} و {o.KMax} باشد.");
        if (i.T < o.TMin || i.T > o.TMax)
            throw ValuationException.BadRequest($"T باید بین {o.TMin} و {o.TMax} باشد.");
        if (i.Sigma < o.SigmaMin || i.Sigma > o.SigmaMax)
            throw ValuationException.BadRequest($"نوسان (sigma) باید بین {o.SigmaMin} و {o.SigmaMax} باشد.");
        if (i.R < o.RMin || i.R > o.RMax)
            throw ValuationException.BadRequest($"نرخ بدون ریسک (r) باید بین {o.RMin} و {o.RMax} باشد.");
        if (i.N < o.StepsMin || i.N > o.StepsMax)
            throw ValuationException.BadRequest($"تعداد گام‌ها (n) باید بین {o.StepsMin} و {o.StepsMax} باشد.");

        var result = Compute(i);
        var tornado = Sensitivity(i, result.Rov);
        return new ToolRunOutcome(result with { Tornado = tornado }, Math.Round(result.Rov), i.Name);
    }

    /// <summary>The ported CRR tree, unchanged — extracted so the sensitivity sweep
    /// re-runs exactly this model on perturbed inputs.</summary>
    private static BinomialResult Compute(BinomialInput i)
    {
        var dt = i.T / i.N;
        var u = Math.Exp(i.Sigma * Math.Sqrt(dt));
        var d = 1 / u;
        var p = (Math.Exp(i.R * dt) - d) / (u - d);
        var disc = Math.Exp(-i.R * dt);

        // Asset tree: S[i][j] at step i with j down-moves.
        var S = new double[i.N + 1][];
        for (var step = 0; step <= i.N; step++)
        {
            S[step] = new double[step + 1];
            for (var j = 0; j <= step; j++)
                S[step][j] = i.S0 * Math.Pow(u, step - j) * Math.Pow(d, j);
        }

        // Option tree with American exercise.
        var V = new double[i.N + 1][];
        var exercised = new bool[i.N + 1][];
        for (var step = 0; step <= i.N; step++)
        {
            V[step] = new double[step + 1];
            exercised[step] = new bool[step + 1];
        }

        for (var j = 0; j <= i.N; j++)
        {
            var payoff = Math.Max(S[i.N][j] - i.K, 0);
            V[i.N][j] = payoff;
            exercised[i.N][j] = payoff > 0;
        }

        for (var step = i.N - 1; step >= 0; step--)
        {
            for (var j = 0; j <= step; j++)
            {
                var cont = disc * (p * V[step + 1][j] + (1 - p) * V[step + 1][j + 1]);
                var exVal = S[step][j] - i.K;
                if (exVal > cont)
                {
                    V[step][j] = exVal;
                    exercised[step][j] = true;
                }
                else
                {
                    V[step][j] = cont;
                    exercised[step][j] = false;
                }
            }
        }

        var rov = V[0][0];
        return new BinomialResult(
            U: u, D: d, P: p, Disc: disc, Dt: dt,
            Rov: rov,
            Flexibility: rov - Math.Max(i.S0 - i.K, 0),
            StaticNpv: i.S0 - i.K,
            S: S, V: V, Exercised: exercised,
            Tornado: []);
    }

    /// <summary>
    ///     ±20% one-at-a-time sensitivity on every scalar input the lattice uses
    ///     (S0, K, volatility, time, risk-free rate). Each row is the same CRR tree
    ///     re-run on a perturbed copy — no approximation.
    /// </summary>
    private static TornadoRow[] Sensitivity(BinomialInput input, double baseRov)
    {
        const double delta = 0.2;
        var rows = new List<TornadoRow>();

        void Add(string label, Action<BinomialInput, double> mutate)
        {
            var high = Clone(input);
            mutate(high, 1 + delta);
            var low = Clone(input);
            mutate(low, 1 - delta);
            var hi = Compute(high).Rov;
            var lo = Compute(low).Rov;
            rows.Add(new TornadoRow(label, Math.Min(lo, hi), Math.Max(lo, hi), baseRov));
        }

        Add("ارزش پایه (S0)", (c, f) => c.S0 *= f);
        Add("هزینه اعمال (K)", (c, f) => c.K *= f);
        Add("نوسان (σ)", (c, f) => c.Sigma *= f);
        Add("زمان (T)", (c, f) => c.T *= f);
        Add("نرخ بدون ریسک (r)", (c, f) => c.R *= f);

        return rows.OrderByDescending(r => Math.Abs(r.High - r.Low)).ToArray();
    }

    private static BinomialInput Clone(BinomialInput i) => new()
    {
        Name = i.Name, S0 = i.S0, K = i.K, T = i.T, Sigma = i.Sigma, R = i.R, N = i.N,
    };

    public sealed class BinomialInput
    {
        public string? Name { get; set; }
        public double S0 { get; set; }
        public double K { get; set; }
        public double T { get; set; }
        public double Sigma { get; set; }
        public double R { get; set; }
        public int N { get; set; }
    }

    public sealed record BinomialResult(
        [property: JsonPropertyName("u")] double U,
        [property: JsonPropertyName("d")] double D,
        [property: JsonPropertyName("p")] double P,
        [property: JsonPropertyName("disc")] double Disc,
        [property: JsonPropertyName("dt")] double Dt,
        [property: JsonPropertyName("rov")] double Rov,
        double Flexibility,
        double StaticNpv,
        [property: JsonPropertyName("S")] double[][] S,
        [property: JsonPropertyName("V")] double[][] V,
        bool[][] Exercised,
        // ±20% one-at-a-time sensitivity, widest swing first.
        TornadoRow[] Tornado);

    /// <summary><see cref="Low"/>/<see cref="High"/> are the option value with the driver at −20%/+20%.</summary>
    public sealed record TornadoRow(string Label, double Low, double High, double Base);
}

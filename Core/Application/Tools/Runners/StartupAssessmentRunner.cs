using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════════════
 * Startup Assessment (ارزیابی استارتاپ) — 1:1 port of the standalone HTML
 * engine (`itemErr` / `calcAxis` / `calcAll`).
 *
 * 10 axes × 6 criteria. Every criterion carries:
 *   w     internal weight (editable)
 *   act   0/1 applicable flag (0 = not applicable, needs a reason)
 *   score 0..5 (null = not assessed; empty is NOT zero)
 *   q     evidence quality 0..3 (null = not recorded)
 *   ev    evidence text / link / date
 *   crit  critical-risk flag (warning only — forces the r_crit result)
 *
 * Axis score   = 20 × Σ(w×score) / Σ(w of active scored criteria)   (0..100) → C
 * Axis norm w. = profile weight / Σ profile weights                → B
 * Weighted     = B × C                                             → D
 * Provisional  = Σ(D of scored axes) / Σ(B of scored axes)
 * Final        = Σ(D) / Σ(B); shown only when every positively-weighted
 *                axis is fully covered (cov === 1)
 * Axis cov.    = weight of scored active criteria / weight of active criteria
 *
 * The client sends its whole state (defState shape minus lang); the page
 * renders numbers by position — row i = axis i — and resolves the status /
 * result / action codes to bilingual labels from its own copy of T.
 * ════════════════════════════════════════════════════════════════════════════ */

public sealed class StartupAssessmentRunner : IToolRunner
{
    public string ToolCode => "STARTUP-ASSESS";

    private const double Eps = 1e-9;

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<StartupAssessmentInput>(input);
        var c = Compute(s);
        return new ToolRunOutcome(c, c.Fin, s.Name);
    }

    private static bool IsNum(double? v) =>
        v is not null && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value);

    /// <summary>Mirrors the JS itemErr — any malformed field trips the axis into as_err.</summary>
    private static bool ItemErr(SaItem it)
    {
        var wOk = IsNum(it.W) && it.W >= 0;
        var actOk = it.Act is 0 or 1;
        var critOk = it.Crit is 0 or 1;
        var scoreOk = it.Score is null || (IsNum(it.Score) && it.Score >= 0 && it.Score <= 5);
        var qOk = it.Q is null || (IsNum(it.Q) && it.Q >= 0 && it.Q <= 3);
        return !(wOk && actOk && critOk && scoreOk && qOk);
    }

    private static SaAxis CalcAxis(List<SaItem> items)
    {
        double P = 0, A = 0, J = 0, K = 0, Q = 0, R = 0, Sx = 0, crit = 0;
        foreach (var it in items)
        {
            var e = ItemErr(it);
            if (e) P++;
            var w = IsNum(it.W) ? it.W!.Value : 0;
            A += w * (it.Act == 1 ? 1 : 0);
            var j = (!e && it.Act == 1 && IsNum(it.Score)) ? w : 0;
            J += j;
            if (j > 0)
            {
                K += j * it.Score!.Value;
                if (IsNum(it.Q)) { Q += j; R += j * it.Q!.Value; }
                if (!string.IsNullOrWhiteSpace(it.Ev)) Sx += j;
            }
            if (it.Act == 1 && it.Crit == 1) crit++;
        }

        double? score = (P == 0 && J > 0) ? 20 * K / J : null;
        var cov = (P == 0 && A > 0) ? J / A : 0;
        var status =
            P > 0 ? "as_err" :
            A == 0 ? "as_nowt" :
            J == 0 ? "as_none" :
            cov < 1 - Eps ? "as_part" : "as_full";

        return new SaAxis
        {
            Err = P, Active = A, Scored = J, Score = score, Cov = cov, Crit = crit,
            Status = status,
            Quality = Q > 0 ? R / Q : null,
            Qcov = J > 0 ? Q / J : 0,
            Tcov = J > 0 ? Sx / J : 0,
            C = J > 0 ? score : null,
        };
    }

    /// <summary>Stage weights: fixed design curves for idea/seed/growth (the HTML's
    /// normProf() resets them), manual for the user's own numbers.</summary>
    private static double[] ResolveF(StartupAssessmentInput s)
    {
        double[] d = s.Stage switch
        {
            "idea" => [25, 20, 15, 12, 8, 8, 4, 4, 2, 2],
            "growth" => [15, 15, 6, 10, 6, 12, 18, 10, 4, 4],
            _ => [20, 15, 10, 12, 8, 10, 10, 7, 4, 4], // seed + anything unknown
        };
        var manual = s.Stage == "manual" ? s.Manual : null;
        if (manual is { Length: 10 })
        {
            var arr = new double[10];
            for (var i = 0; i < 10; i++) arr[i] = IsNum(manual[i]) ? manual[i] : 0;
            return arr;
        }
        return d;
    }

    private static SaResult Compute(StartupAssessmentInput s)
    {
        var F = ResolveF(s);
        var okSet = F.Length == 10 && F.All(v => IsNum(v)) && F.Min() >= 0 && F.Sum() > 0;
        var sumF = F.Where(v => IsNum(v)).Sum();

        // Pad defensive so a legacy/partial payload still yields 10 rows.
        var axList = new List<List<SaItem>>();
        if (s.Ax is not null)
            foreach (var axis in s.Ax.Take(10))
            {
                var list = new List<SaItem>();
                if (axis is not null) list.AddRange(axis.Take(24));
                axList.Add(list);
            }
        for (var i = axList.Count; i < 10; i++) axList.Add([]);

        var rows = new List<SaRow>();
        for (var i = 0; i < 10; i++)
        {
            var c = CalcAxis(axList[i]);
            var B = okSet ? F[i] / sumF : 0;
            var D = IsNum(c.C) ? B * c.C!.Value : (double?)null;
            var act =
                c.Crit > 0 ? "ac_crit" :
                c.Status == "as_err" ? "ac_fix" :
                B == 0 ? "ac_nowt" :
                c.Cov < 1 - Eps ? "ac_score" :
                (c.Qcov < 1 || c.Tcov < 1) ? "ac_ev" :
                (c.C ?? 0) < 65 ? "ac_imp" : "ac_mon";
            rows.Add(new SaRow
            {
                Err = c.Err, Active = c.Active, Scored = c.Scored, Score = c.Score,
                Cov = c.Cov, Crit = c.Crit, Status = c.Status, Quality = c.Quality,
                Qcov = c.Qcov, Tcov = c.Tcov, C = c.C, B = B, D = D, Act = act,
            });
        }

        var sumB = rows.Sum(r => r.B);
        var covTot = sumB > 0 ? rows.Sum(r => r.B * r.Cov) / sumB : 0;
        var den = rows.Sum(r => IsNum(r.C) ? r.B : 0);
        var prov = den > 0 ? rows.Sum(r => IsNum(r.D) ? r.D!.Value : 0) / den : (double?)null;
        var nAssessed = rows.Count(r => IsNum(r.C));
        var allFull = rows.All(r => !(r.B > 0 && r.Cov < 1 - Eps));
        var fin = (okSet && sumB > 0 && nAssessed > 0 && allFull)
            ? rows.Sum(r => IsNum(r.D) ? r.D!.Value : 0) / sumB
            : (double?)null;

        var critTot = rows.Sum(r => r.Crit);
        var docsGap = rows.Sum(r => r.B * (r.Qcov < 1 ? 1 : 0) + r.B * (r.Tcov < 1 ? 1 : 0)) > 0;

        var res =
            !okSet ? "r_bad" :
            rows.Any(r => r.Status == "as_err") ? "r_err" :
            critTot > 0 ? "r_crit" :
            fin is null ? "r_inc" :
            docsGap ? "r_docs" :
            fin >= 80 ? "r_strong" :
            fin >= 65 ? "r_cond" :
            fin >= 50 ? "r_imp" : "r_weak";

        return new SaResult
        {
            OkSet = okSet,
            SumF = sumF,
            Normalised = okSet && Math.Abs(sumF - 100) > 1e-6,
            CovTot = covTot,
            Prov = prov,
            Fin = fin,
            CritTot = critTot,
            Res = res,
            DocsGap = docsGap,
            NAssessed = nAssessed,
            Rows = rows,
        };
    }

    // ────────────────────────── input model ──────────────────────────
    public sealed class SaItem
    {
        public double? W { get; set; }
        public double? Act { get; set; } = 1;
        public double? Score { get; set; }
        public string? Ev { get; set; }
        public double? Q { get; set; }
        public double? Crit { get; set; }
        public string? Note { get; set; }
    }

    public sealed class StartupAssessmentInput
    {
        public string? Lang { get; set; }
        public string? View { get; set; }
        public string? Stage { get; set; } = "seed";
        public string? Name { get; set; }
        public string? Category { get; set; }
        public string? Industry { get; set; }
        public string? Evaluator { get; set; }
        public string? Date { get; set; }
        public double? Target { get; set; } = 70;

        /// <summary>Manual stage weights (S.prof.manual on the client).</summary>
        public double[]? Manual { get; set; }
        public List<List<SaItem>>? Ax { get; set; }
    }

    // ────────────────────────── result model ──────────────────────────
    public class SaAxis
    {
        public double Err { get; set; }
        public double Active { get; set; }
        public double Scored { get; set; }
        public double? Score { get; set; }
        public double Cov { get; set; }
        public double Crit { get; set; }
        public string Status { get; set; } = "as_none";
        public double? Quality { get; set; }
        public double Qcov { get; set; }
        public double Tcov { get; set; }

        /// <summary>Axis score 0-100 (JS row.C); "C" kept verbatim for the client.</summary>
        [JsonPropertyName("C")]
        public double? C { get; set; }
    }

    /** One row per axis, matching the JS calcAll row spread {...c,B,D,act}. */
    public sealed class SaRow : SaAxis
    {
        public double B { get; set; }
        public double? D { get; set; }
        public string Act { get; set; } = "ac_mon";
    }

    public sealed class SaResult
    {
        /// <summary>Exposed so the client's manual-weights table can reuse B (stage-code activator)</summary>
        public bool OkSet { get; set; }
        public double SumF { get; set; }
        public bool Normalised { get; set; }
        public double CovTot { get; set; }
        public double? Prov { get; set; }
        public double? Fin { get; set; }
        public double CritTot { get; set; }
        public string Res { get; set; } = "r_inc";
        public bool DocsGap { get; set; }
        public int NAssessed { get; set; }
        public List<SaRow> Rows { get; set; } = [];
    }
}

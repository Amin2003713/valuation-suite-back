using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Tools.Finance;

namespace Application.Tools.Runners;

/* ════════════════════════════════════════════════════════════════════════════
 * Startup Valuation — 1:1 port of the workbook HTML engine (`compute(S)`).
 *
 * Eight methods (Scorecard, Berkus, Risk factors, Venture capital, Market
 * comparison, DCF, First Chicago, Re-creation cost) feed one blended final
 * comparison. The engine also produces the workbook's soft-warning "flags"
 * (never blocking) that the client renders next to every sheet tab.
 *
 * The client sends the raw inputs and renders exactly what comes back — no
 * valuation math lives in the browser.
 * ════════════════════════════════════════════════════════════════════════════ */

public sealed class StartupValuationRunner : IToolRunner
{
    public string ToolCode => "STARTUP-VAL";

    private static readonly string[] Methods = ["sc", "bk", "rk", "vc", "mk", "dcf", "ch", "rp"];

    private static readonly Dictionary<string, double[]> StageFit = new()
    {
        ["sc"] = [30, 25, 20, 10, 5],
        ["bk"] = [30, 25, 10, 0, 0],
        ["rk"] = [15, 15, 10, 5, 0],
        ["vc"] = [5, 15, 25, 20, 10],
        ["mk"] = [0, 0, 15, 30, 35],
        ["dcf"] = [0, 0, 5, 20, 40],
        ["ch"] = [5, 15, 15, 10, 10],
        ["rp"] = [15, 5, 0, 5, 0],
    };

    // ── numeric helpers (mirror the JS isN/cnt/sum/mn/mx semantics) ──
    private static bool IsN(double? v) => v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value);
    private static int Cnt(params double?[] a) => a.Count(IsN);
    private static double Sum(IEnumerable<double?> a) => a.Where(IsN).Sum(v => v!.Value);
    private static double? MinN(IEnumerable<double?> a)
    {
        var l = a.Where(IsN).Select(v => v!.Value).ToList();
        return l.Count > 0 ? l.Min() : null;
    }
    private static double? MaxN(IEnumerable<double?> a)
    {
        var l = a.Where(IsN).Select(v => v!.Value).ToList();
        return l.Count > 0 ? l.Max() : null;
    }
    private static double? At(IReadOnlyList<double?> list, int i) => i >= 0 && i < list.Count ? list[i] : null;
    private static double? D(double? v) => v;

    public ToolRunOutcome Run(JsonElement input)
    {
        var s = ToolInput.Bind<StartupInput>(input);
        var c = Compute(s);
        return new ToolRunOutcome(c.Result, null, s.Name);
    }

    // ────────────────────────────────────────────────────────────────────────
    private static Computed Compute(StartupInput s)
    {
        var med = IsN(s.Median) && s.Median > 0 ? s.Median : null;

        // ── shared / stage-suggested assumptions ──
        var sg = IsN(s.Stage) && s.Stage >= 1 && s.Stage <= 5 && s.Stage == Math.Floor(s.Stage!.Value);
        double? Pick(double[] a) => sg ? a[(int)s.Stage!.Value - 1] : null;
        double? Ovr(double? v, double? def, Func<double, bool> ok)
            => !sg ? null : v is null ? def : (ok(v.Value) ? v : null);

        var years = Ovr(s.Years, Pick([7, 6, 5, 4, 3]), v => v > 0);
        var vcret = Ovr(s.Vcret, Pick([.65, .55, .45, .35, .25]), v => v >= 0);
        var wacc = Ovr(s.Wacc, Pick([.45, .4, .35, .28, .22]), v => v >= 0);
        var g = Ovr(s.G, .03, v => v > -1);
        var mult = Ovr(s.Evrev, Pick([2, 2, 3, 4, 3]), v => v > 0);
        var exg = Ovr(s.Exg, Pick([.5, .5, .4, .3, .15]), v => v > -1);
        var surv = Ovr(s.Surv, Pick([.5, .6, .7, .8, .9]), v => v > 0 && v <= 1);

        var cnd = new double?[] { s.Cash, s.Debt, s.Newcap };
        var warn =
            !sg ? "stage"
            : Cnt(cnd) < 3 ? "incomplete"
            : (MinN(cnd) ?? 0) < 0 ? "neg"
            : Cnt(years, vcret, wacc, g, mult, exg, surv) < 7 ? "override"
            : wacc <= g ? "rate"
            : "ok";

        var cd = Cnt(s.Cash, s.Debt) == 2 && (MinN([s.Cash, s.Debt]) ?? -1) >= 0;
        double? Bridge(double? ev) => IsN(ev) && cd && ev!.Value + s.Cash!.Value - s.Debt!.Value >= 0
            ? ev!.Value + s.Cash!.Value - s.Debt!.Value : null;

        // ── Scorecard ──
        var scAuto = s.ScAuto;
        var scW = Enumerable.Range(0, 7).Select(i => At(s.ScWeights, i)).ToList();
        if (scAuto)
        {
            var f6 = scW.Take(6).ToList();
            scW[6] = Cnt([.. f6]) == 6 && Sum(f6) <= 1 + 1e-9 ? Math.Max(0, 1 - Sum(f6)) : null;
        }
        var scD = Enumerable.Range(0, 7).Select(i => At(s.ScScores, i)).ToList();
        var scE = scW.Select((w, i) => IsN(scD[i]) ? (IsN(w) ? w!.Value : 0) * scD[i]!.Value : (double?)null).ToList();
        var scWok = Cnt([.. scW]) == 7 && (MinN(scW) ?? -1) >= 0 && Math.Abs(Sum(scW) - 1) < 1e-4;
        var scSumW = Sum(scW);
        var scRes = scWok && Cnt([.. scD]) == 7 && (MinN(scD) ?? -1) >= 0 && med is not null
            ? med * Sum(scE) : null;

        // ── Berkus ──
        var bkCap0 = med;
        var bkCap = s.BkCap is null ? med : (IsN(s.BkCap) && s.BkCap > 0 ? s.BkCap : null);
        var bkComp0 = IsN(bkCap) ? bkCap / 5 : null;
        var bkComp = Enumerable.Range(0, 5).Select(i =>
        {
            var o = At(s.BkCaps, i);
            return o is null ? bkComp0 : (IsN(o) && o >= 0 ? o : null);
        }).ToList();
        var bkD = Enumerable.Range(0, 5).Select(i => At(s.BkScores, i)).ToList();
        var bkE = bkD.Select((d, i) => IsN(bkComp[i]) && IsN(d) ? bkComp[i]!.Value * d!.Value : (double?)null).ToList();
        var bkRes = Cnt([.. bkD]) == 5 && (MinN(bkD) ?? -1) >= 0 && (MaxN(bkD) ?? 2) <= 1 && Cnt([.. bkComp]) == 5
            ? (double?)Sum(bkE) : null;

        // ── Risk factors ──
        var rkStep = s.RkStep is null ? .05 : (IsN(s.RkStep) && s.RkStep >= 0 && s.RkStep <= 1 ? s.RkStep : null);
        var rk = Enumerable.Range(0, 12).Select(i => At(s.RkScores, i)).ToList();
        var rkSum = Sum(rk);
        var rkRes = Cnt([.. rk]) == 12 && (MinN(rk) ?? -3) >= -2 && (MaxN(rk) ?? 3) <= 2 && IsN(rkStep) && med is not null
                    && 1 + rkStep!.Value * rkSum >= 0
            ? med * (1 + rkStep!.Value * rkSum) : null;

        // ── Venture capital ──
        double? Loc(double? v, double? def, Func<double, bool> ok) => v is null ? def : (ok(v.Value) ? v : null);
        var vcYears = Loc(s.VcYears, years, v => v > 0);
        var vcRet = Loc(s.VcReturn, vcret, v => v >= 0);
        var vcSurv = Loc(s.VcSurv, surv, v => v > 0 && v <= 1);
        var vcExg = Loc(s.VcExg, exg, v => v > -1);
        var vcMult = Loc(s.VcMult, mult, v => v > 0);
        var vcNew0 = IsN(s.Newcap) ? s.Newcap : null;
        var vcNew = Loc(s.VcNew, vcNew0, v => v >= 0);
        var vcExrev0 = IsN(s.Rev) && s.Rev >= 0 && IsN(vcYears) && IsN(vcExg)
            ? s.Rev * Math.Pow(1 + vcExg!.Value, vcYears!.Value) : null;
        var vcExrev = Loc(s.VcExrev, vcExrev0, v => v >= 0);
        var vcExeq0 = IsN(vcExrev) && IsN(vcMult) ? vcExrev * vcMult : null;
        var vcExeq = Loc(s.VcExeq, vcExeq0, v => v >= 0);
        var vcPost0 = Cnt(vcExeq, vcYears, vcRet, vcSurv) == 4 && vcExeq > 0 && vcYears > 0 && vcRet >= 0 && vcSurv > 0 && vcSurv <= 1
            ? vcExeq * vcSurv / Math.Pow(1 + vcRet!.Value, vcYears!.Value) : null;
        var vcPost = s.VcPost is not null
            ? (IsN(s.VcPost) && s.VcPost > 0 ? s.VcPost : null)
            : s.VcShare is not null
                ? (IsN(s.VcShare) && s.VcShare > 0 && s.VcShare <= 1 && IsN(vcNew) && vcNew > 0
                    ? vcNew / s.VcShare : null)
                : vcPost0;
        var vcRes = IsN(vcPost) && IsN(vcNew) && vcNew >= 0 && vcPost >= vcNew ? vcPost - vcNew : null;
        var vcShare0 = IsN(vcPost0) && IsN(vcNew) && vcPost0 > 0 ? vcNew / vcPost0 : null;
        var vcShare = IsN(vcRes) && vcPost > 0 ? vcNew / vcPost : null;
        var vcExshare = IsN(vcShare) && IsN(vcSurv) ? vcShare * vcSurv : null;

        // ── Market comparison ──
        var crit = s.MkCrit;
        var mkMult0 = IsN(mult) ? mult : null;
        var mkMult = IsN(crit) && (crit == 1 || crit == 2)
            ? (s.MkMult is null ? (crit == 1 ? mkMult0 : null) : (IsN(s.MkMult) && s.MkMult > 0 ? s.MkMult : null))
            : null;
        var mkMetric0 = crit == 1 ? (IsN(s.Rev) ? s.Rev : null) : crit == 2 ? (IsN(s.Ebitda) ? s.Ebitda : null) : null;
        var mkMetric = s.MkMetric is null ? mkMetric0 : (IsN(s.MkMetric) ? s.MkMetric : null);
        var mkEv = Cnt(mkMult, mkMetric) == 2 && mkMult > 0 && mkMetric > 0 ? mkMult * mkMetric : null;
        var mkRes = Bridge(mkEv);

        // ── DCF ──
        var fc = Enumerable.Range(0, 5).Select(i => At(s.DcfF, i)).ToList();
        var dcfR = Loc(s.DcfR, wacc, v => v >= 0);
        var dcfG = Loc(s.DcfG, g, v => v > -1);
        var rOk = IsN(dcfR) && dcfR >= 0;
        var dcfD = Enumerable.Range(1, 5).Select(t => rOk ? 1 / Math.Pow(1 + dcfR!.Value, t) : (double?)null).ToList();
        var dcfPv = fc.Select((f, i) => IsN(f) && IsN(dcfD[i]) ? f * dcfD[i] : null).ToList();
        var dcfTv = IsN(fc[4]) && Cnt(dcfR, dcfG) == 2 && dcfR >= 0 && dcfG > -1 && dcfR > dcfG && fc[4] >= 0
            ? fc[4] * (1 + dcfG!.Value) / (dcfR!.Value - dcfG!.Value) : null;
        var dcfEv = Cnt([.. fc]) == 5 && Cnt([.. dcfPv]) == 5 && IsN(dcfTv)
            ? (double?)(Sum(dcfPv) + dcfTv!.Value * dcfD[4]!.Value) : null;
        var dcfTvShare = IsN(dcfEv) && dcfEv > 0 ? dcfTv * dcfD[4] / dcfEv : null;
        var dcfRes = Bridge(dcfEv);

        // ── First Chicago ──
        var chP = Enumerable.Range(0, 3).Select(i =>
        {
            var d = At(s.ChP, i);
            return d is null ? new[] { .3, .5, .2 }[i] : (IsN(d) && d >= 0 && d <= 1 ? d : null);
        }).ToList();
        if (s.ChAuto)
            chP[2] = IsN(chP[0]) && IsN(chP[1]) && chP[0] + chP[1] <= 1 + 1e-9
                ? Math.Max(0, 1 - chP[0]!.Value - chP[1]!.Value) : null;
        var chF = Enumerable.Range(0, 3).Select(i => At(s.ChF, i)).ToList();
        var lk = s.ChLink;
        var chYears = lk ? vcYears : Loc(s.ChYears, years, v => v > 0);
        var chRet = lk ? vcRet : Loc(s.ChRet, vcret, v => v >= 0);
        var chSurv = lk ? vcSurv : Loc(s.ChSurv, surv, v => v > 0 && v <= 1);
        var chNew = lk ? vcNew : Loc(s.ChNew, vcNew0, v => v >= 0);
        var chOk = IsN(chYears) && IsN(chRet) && chYears > 0 && chRet >= 0 && IsN(chSurv) && chSurv > 0 && chSurv <= 1
                   && IsN(chNew) && chNew >= 0;
        var chG = chF.Select(f => IsN(f) && f >= 0 && chOk
            ? f * chSurv / Math.Pow(1 + chRet!.Value, chYears!.Value) - chNew : null).ToList();
        var chSumP = Sum(chP);
        var chSp = chP.Select((p, i) => IsN(p) && IsN(chG[i]) ? p!.Value * chG[i]!.Value : 0).Sum();
        var chRes = Cnt([.. chP]) == 3 && Cnt([.. chF]) == 3 && Cnt([.. chG]) == 3 && (MinN(chF) ?? -1) >= 0
                    && (MinN(chP) ?? -1) >= 0 && (MaxN(chP) ?? 2) <= 1 && Math.Abs(Sum(chP) - 1) < 1e-4 && chSp >= 0
            ? (double?)chSp : null;

        // ── Re-creation cost ──
        var rc = Enumerable.Range(0, 6).Select(i => At(s.RpCosts, i)).ToList();
        var rd = Enumerable.Range(0, 6).Select(i => At(s.RpDep, i)).ToList();
        var rpE = rc.Select((c0, i) => Cnt(c0, rd[i]) == 2 && c0 >= 0 && rd[i] >= 0 && rd[i] <= 1
            ? c0 * (1 - rd[i]!.Value) : null).ToList();
        var rpSum = Sum(rpE);
        var rpRes = Cnt([.. rpE]) == 6 ? Bridge(rpSum) : null;

        var resMap = new Dictionary<string, double?>
        {
            ["sc"] = scRes, ["bk"] = bkRes, ["rk"] = rkRes, ["vc"] = vcRes,
            ["mk"] = mkRes, ["dcf"] = dcfRes, ["ch"] = chRes, ["rp"] = rpRes,
        };

        // ── Final comparison ──
        var rows = Methods.Select(id =>
        {
            var res = resMap[id];
            var d0 = sg ? StageFit[id][(int)s.Stage!.Value - 1] : (double?)null;
            var fo = s.Fit.GetValueOrDefault(id);
            var d = fo is null ? d0 : (IsN(fo) && fo >= 0 ? fo : null);
            var q = s.Quality.GetValueOrDefault(id);
            var w = s.Factor.GetValueOrDefault(id);
            var raw = IsN(res) && IsN(d) && res >= 0 && IsN(q) && q >= 1 && q <= 3 && q == Math.Floor(q!.Value) && IsN(w) && w >= 0
                ? d!.Value * q!.Value / 3 * w!.Value : 0;
            return new FinalRow { Id = id, Res = res, D = d, D0 = d0, Q = q, W = w, Raw = raw };
        }).ToList();
        var totRaw = rows.Sum(r => r.Raw);
        foreach (var r in rows)
        {
            r.H = totRaw > 0 ? r.Raw / totRaw : 0;
            r.I = IsN(r.Res) ? r.Res!.Value * r.H : 0;
            r.Status = !IsN(r.Res) ? "invalid" : r.Raw == 0 ? "dropped" : "active";
        }
        var fPre = totRaw > 0 ? rows.Sum(r => r.I) : (double?)null;
        var fPost = IsN(fPre) && IsN(s.Newcap) && s.Newcap >= 0 ? fPre + s.Newcap : null;
        var fShare = IsN(fPost) && fPost > 0 ? s.Newcap / fPost : null;
        var fN = rows.Count(r => r.Raw > 0);
        var act = rows.Where(r => r.Raw > 0 && IsN(r.Res)).ToList();
        var fMin = fN > 0 ? MinN(act.Select(r => r.Res)) : null;
        var fMax = fN > 0 ? MaxN(act.Select(r => r.Res)) : null;

        // ── soft warnings (never block a calculation) ──
        var fl = new Dictionary<string, List<Flag>>();
        foreach (var k in new[] { "status", "sc", "bk", "rk", "vc", "mk", "dcf", "ch", "rp", "final" })
            fl[k] = [];
        void Add(string sheet, string code, string level, double? v = null, string? f = null,
            double? w = null, string[]? list = null, string? id = null)
            => fl[sheet].Add(new Flag(code, level, v, w, f, list, id));

        var rev = s.Rev;
        var med0 = med;
        if (sg)
        {
            if (s.Stage >= 3 && rev == 0) Add("status", "zeroRev", "warn");
            if (IsN(rev) && IsN(s.Ebitda) && rev > 0 && s.Ebitda > rev) Add("status", "ebitdaGtRev", "warn");
            else if (IsN(rev) && IsN(s.Ebitda) && rev > 0 && s.Ebitda / rev > .6) Add("status", "marginHigh", "warn", s.Ebitda / rev, "p");
            if (IsN(s.Newcap) && med0 is not null)
            {
                if (s.Newcap > med0) Add("status", "capGtMedian", "warn");
                else if (s.Newcap > .5 * med0) Add("status", "capLarge", "info");
            }
            if (IsN(s.Debt) && IsN(rev) && rev > 0 && s.Debt > 3 * rev) Add("status", "debtHigh", "info");
            if (IsN(years) && years > 10) Add("status", "yearsLong", "warn", years);
            if (IsN(years) && years < 1) Add("status", "yearsShort", "warn", years);
            if (IsN(vcret) && vcret > 1) Add("status", "retHigh", "warn", vcret, "p");
            if (IsN(vcret) && vcret < .1) Add("status", "retLow", "warn", vcret, "p");
            if (IsN(wacc) && wacc > .8) Add("status", "rateHigh", "warn", wacc, "p");
            if (IsN(wacc) && wacc < .08) Add("status", "rateLow", "warn", wacc, "p");
            if (IsN(g) && g > .06) Add("status", "gHigh", "warn", g, "p");
            if (IsN(g) && g < 0) Add("status", "gNeg", "info", g, "p");
            if (IsN(mult) && mult > 15) Add("status", "multHigh", "warn", mult);
            if (IsN(exg) && exg > 1) Add("status", "exgHigh", "warn", exg, "p");
            if (IsN(surv) && surv < .3) Add("status", "survLow", "warn", surv, "p");
        }
        // scorecard
        if (scAuto && Cnt([.. scW.Take(6)]) == 6 && Sum(scW.Take(6)) > 1 + 1e-9)
            Add("sc", "wOver", "warn", Sum(scW.Take(6)), "p");
        foreach (var w in scW) if (IsN(w) && w > .5) Add("sc", "wMax", "warn", w, "p");
        if (scD.Any(d => IsN(d) && (d > 2 || (d < .25 && d >= 0)))) Add("sc", "scoreExtreme", "warn");
        // berkus
        if (IsN(s.BkCap) && med0 is not null && s.BkCap > 3 * med0) Add("bk", "capHigh", "warn", s.BkCap / med0);
        if (Enumerable.Range(0, 5).Any(i => IsN(At(s.BkCaps, i))) && IsN(bkCap) && Cnt([.. bkComp]) == 5
            && Math.Abs(Sum(bkComp) - bkCap!.Value) > .01 * bkCap!.Value)
            Add("bk", "compSum", "info", Sum(bkComp), null, bkCap);
        // risk
        if (IsN(rkStep) && rkStep > .15) Add("rk", "stepHigh", "warn", rkStep, "p");
        if (Cnt([.. rk]) == 12 && Math.Abs(rkSum) >= 18) Add("rk", "scoresExtreme", "warn", rkSum);
        if (IsN(rkRes) && med0 is not null && (rkRes / med0 > 2 || rkRes / med0 < .3)) Add("rk", "moveLarge", "warn", rkRes / med0);
        // vc
        if (IsN(vcShare))
        {
            if (vcShare > .5) Add("vc", "shareHigh", "warn", vcShare, "p");
            else if (vcShare < .01) Add("vc", "shareLow", "warn", vcShare, "p");
        }
        if (IsN(vcRet) && vcRet > 1) Add("vc", "retHigh", "warn", vcRet, "p");
        if (IsN(vcYears) && (vcYears > 10 || vcYears < 1)) Add("vc", "yearsOut", "warn", vcYears);
        if (IsN(vcPost) && IsN(vcNew) && vcPost < vcNew) Add("vc", "postBelowNew", "warn");
        if (IsN(vcExeq) && IsN(vcPost) && vcPost > 0 && vcExeq / vcPost > 30) Add("vc", "exitMultiple", "warn", vcExeq / vcPost);
        if (IsN(vcPost) && med0 is not null && (vcPost / med0 > 5 || vcPost / med0 < .2)) Add("vc", "postVsMedian", "warn", vcPost / med0);
        if (IsN(vcNew) && IsN(s.Newcap) && Math.Abs(vcNew!.Value - s.Newcap!.Value) > 1e-9) Add("vc", "newDiff", "warn", vcNew, null, s.Newcap);
        // market
        if (IsN(mkMult))
        {
            if (crit == 1 && mkMult > 15) Add("mk", "revMultHigh", "warn", mkMult);
            if (crit == 2 && (mkMult > 40 || mkMult < 3)) Add("mk", "ebitdaMult", "warn", mkMult);
        }
        if (crit == 2 && s.MkMult is null) Add("mk", "needMult", "info");
        // dcf
        if (IsN(dcfTvShare) && dcfTvShare > .85) Add("dcf", "tvHigh", "warn", dcfTvShare, "p");
        if (IsN(dcfR) && IsN(dcfG) && dcfR > dcfG && dcfR - dcfG < .03) Add("dcf", "spreadSmall", "warn", dcfR - dcfG, "p");
        if (IsN(dcfR) && dcfR < .08) Add("dcf", "rateLow", "warn", dcfR, "p");
        if (IsN(dcfG) && dcfG > .06) Add("dcf", "gHigh", "warn", dcfG, "p");
        if (Cnt([.. fc]) == 5 && fc.All(f => f < 0)) Add("dcf", "allNeg", "warn");
        else if (IsN(fc[3]) && IsN(fc[4]) && fc[3] > 0 && fc[4] < .8 * fc[3]) Add("dcf", "fcffDrop", "info");
        // chicago
        if (chP.Any(p => IsN(p) && p > .8)) Add("ch", "concentrated", "warn");
        if (Cnt([.. chF]) == 3 && (chF[0] > chF[1] || chF[1] > chF[2])) Add("ch", "order", "warn");
        if (chG.Any(x => IsN(x) && x < 0)) Add("ch", "negScenario", "info");
        if (!lk)
        {
            var df = new List<string>();
            if (chYears != vcYears) df.Add("years");
            if (chRet != vcRet) df.Add("ret");
            if (chSurv != vcSurv) df.Add("surv");
            if (chNew != vcNew) df.Add("new");
            if (df.Count > 0) Add("ch", "diffVc", "warn", null, null, null, [.. df]);
        }
        // replacement
        if (Enumerable.Range(0, 6).Any(i => IsN(rc[i]) && rc[i] > 0 && IsN(rd[i]) && rd[i] > .95)) Add("rp", "obs", "info");
        if (IsN(rpSum) && med0 is not null && rpSum > 2 * med0) Add("rp", "rpHigh", "warn", rpSum / med0);

        // final reliability + outliers
        var fWarn = fN == 0 ? "none" : fN < 2 ? "one" : act.All(r => r.Q != 3) ? "nostrong" : "review";
        var vals = act.Select(r => r.Res!.Value).OrderBy(x => x).ToList();
        var md = vals.Count == 0 ? (double?)null
            : vals.Count % 2 == 1 ? vals[(vals.Count - 1) / 2]
            : (vals[vals.Count / 2 - 1] + vals[vals.Count / 2]) / 2;
        if (act.Count >= 3 && md > 0)
            foreach (var r in act)
            {
                var q = r.Res!.Value / md!.Value;
                if (q > 3 || q < 1 / 3.0) Add(r.Id, "outlier", "warn", q);
            }
        if (IsN(fMin) && fMin > 0 && fMax / fMin > 5) Add("final", "spread", "warn", fMax / fMin);
        foreach (var r in rows) if (r.H > .6) Add("final", "dominant", "warn", r.H, "p", null, null, r.Id);
        if (IsN(fPre) && med0 is not null && (fPre / med0 > 3 || fPre / med0 < 1 / 3.0)) Add("final", "vsMedian", "warn", fPre / med0);

        // ── suggested values for the "empty = suggested" placeholders ──
        var suggested = new Dictionary<string, double?>
        {
            ["vc_years"] = years, ["vc_ret"] = vcret, ["vc_surv"] = surv, ["vc_exg"] = exg,
            ["vc_mult"] = mult, ["vc_new"] = vcNew0, ["vc_exrev"] = vcExrev0, ["vc_exeq"] = vcExeq0,
            ["vc_post"] = vcPost0, ["vc_share"] = vcShare0, ["mk_mult"] = mkMult0, ["mk_metric"] = mkMetric0,
            ["dcf_r"] = wacc, ["dcf_g"] = g, ["ch_years"] = years, ["ch_ret"] = vcret,
            ["ch_surv"] = surv, ["ch_new"] = vcNew0, ["bk_cap"] = bkCap0, ["rk_step"] = .05,
            ["ch_p0"] = .3, ["ch_p1"] = .5, ["ch_p2"] = .2,
        };
        for (var i = 0; i < 5; i++) suggested["bk_c" + i] = bkComp0;
        foreach (var r in rows) suggested["fit_" + r.Id] = r.D0;

        var result = new StartupResult
        {
            Effective = new EffectiveOut
            {
                Stage = sg ? (int?)s.Stage : null, Years = years, Vcret = vcret, Wacc = wacc, G = g,
                Mult = mult, Exg = exg, Surv = surv, Warn = warn,
            },
            Scorecard = new ScorecardOut { Weights = scW, Weighted = scE, SumW = scSumW, Check = scWok, Value = scRes },
            Berkus = new BerkusOut { Cap0 = bkCap0, Cap = bkCap, Comp0 = bkComp0, Comps = bkComp, Values = bkE, Value = bkRes },
            Risk = new RiskOut { Step = rkStep, Sum = rkSum, Value = rkRes },
            Vc = new VcOut
            {
                Years = vcYears, Ret = vcRet, Surv = vcSurv, Exg = vcExg, Mult = vcMult, New0 = vcNew0, New = vcNew,
                Exrev0 = vcExrev0, Exrev = vcExrev, Exeq0 = vcExeq0, Exeq = vcExeq, Post0 = vcPost0, Post = vcPost,
                Res = vcRes, Share0 = vcShare0, Share = vcShare, Exshare = vcExshare,
            },
            Market = new MarketOut { Mult0 = mkMult0, Mult = mkMult, Metric0 = mkMetric0, Metric = mkMetric, Ev = mkEv, Value = mkRes },
            Dcf = new DcfOut { R = dcfR, G = dcfG, Factors = dcfD, Pvs = dcfPv, Tv = dcfTv, Ev = dcfEv, TvShare = dcfTvShare, Value = dcfRes },
            Chicago = new ChicagoOut { P = chP, Years = chYears, Ret = chRet, Surv = chSurv, New = chNew, G = chG, SumP = chSumP, Value = chRes },
            Replacement = new ReplacementOut { Values = rpE, Sum = rpSum, Value = rpRes },
            Rows = rows,
            Final = new FinalOut { Pre = fPre, Post = fPost, Share = fShare, N = fN, Min = fMin, Max = fMax, Reliability = fWarn },
            Flags = fl,
            Suggested = suggested,
        };
        return new Computed(result);
    }

    private sealed record Computed(StartupResult Result);

    // ────────────────────────────────────────────────────────────────────────
    // Input model — mirrors the workbook state (nullable = empty field)
    // ────────────────────────────────────────────────────────────────────────
    public sealed class StartupInput
    {
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public string? Desc { get; set; }
        public double? Stage { get; set; }
        public double? Rev { get; set; }
        public double? Ebitda { get; set; }
        public double? Cash { get; set; }
        public double? Debt { get; set; }
        public double? Newcap { get; set; }
        public double? Median { get; set; }
        public double? Years { get; set; }
        public double? Vcret { get; set; }
        public double? Wacc { get; set; }
        public double? G { get; set; }
        public double? Evrev { get; set; }
        public double? Exg { get; set; }
        public double? Surv { get; set; }

        public bool ScAuto { get; set; } = true;
        public List<double?> ScWeights { get; set; } = [];
        public List<double?> ScScores { get; set; } = [];

        public double? BkCap { get; set; }
        public List<double?> BkCaps { get; set; } = [];
        public List<double?> BkScores { get; set; } = [];

        public double? RkStep { get; set; }
        public List<double?> RkScores { get; set; } = [];

        public double? VcYears { get; set; }
        // Named vcReturn (not vcRet) so it never collides case-insensitively with the shared "vcret" field.
        public double? VcReturn { get; set; }
        public double? VcSurv { get; set; }
        public double? VcExg { get; set; }
        public double? VcMult { get; set; }
        public double? VcNew { get; set; }
        public double? VcPost { get; set; }
        public double? VcShare { get; set; }
        public double? VcExrev { get; set; }
        public double? VcExeq { get; set; }

        public int? MkCrit { get; set; } = 1;
        public double? MkMult { get; set; }
        public double? MkMetric { get; set; }

        public double? DcfR { get; set; }
        public double? DcfG { get; set; }
        public List<double?> DcfF { get; set; } = [];

        public bool ChAuto { get; set; } = true;
        public bool ChLink { get; set; } = true;
        public double? ChYears { get; set; }
        public double? ChRet { get; set; }
        public double? ChSurv { get; set; }
        public double? ChNew { get; set; }
        public List<double?> ChP { get; set; } = [];
        public List<double?> ChF { get; set; } = [];

        public List<double?> RpCosts { get; set; } = [];
        public List<double?> RpDep { get; set; } = [];

        public Dictionary<string, double?> Quality { get; set; } = new();
        public Dictionary<string, double?> Factor { get; set; } = new();
        public Dictionary<string, double?> Fit { get; set; } = new();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Result model — serialized camelCase to the client
    // ────────────────────────────────────────────────────────────────────────
    public sealed class FinalRow
    {
        public string Id { get; set; } = string.Empty;
        public double? Res { get; set; }
        public double? D { get; set; }
        public double? D0 { get; set; }
        public double? Q { get; set; }
        public double? W { get; set; }
        public double Raw { get; set; }
        public double H { get; set; }
        public double I { get; set; }
        public string Status { get; set; } = "invalid";
    }

    public sealed record Flag(string Code, string Level, double? V, double? W, string? F, string[]? List, string? Id);

    public sealed class EffectiveOut
    {
        public int? Stage { get; set; }
        public double? Years { get; set; }
        public double? Vcret { get; set; }
        public double? Wacc { get; set; }
        public double? G { get; set; }
        public double? Mult { get; set; }
        public double? Exg { get; set; }
        public double? Surv { get; set; }
        public string Warn { get; set; } = "stage";
    }

    public sealed class ScorecardOut
    {
        public List<double?> Weights { get; set; } = [];
        public List<double?> Weighted { get; set; } = [];
        public double SumW { get; set; }
        public bool Check { get; set; }
        public double? Value { get; set; }
    }

    public sealed class BerkusOut
    {
        public double? Cap0 { get; set; }
        public double? Cap { get; set; }
        public double? Comp0 { get; set; }
        public List<double?> Comps { get; set; } = [];
        public List<double?> Values { get; set; } = [];
        public double? Value { get; set; }
    }

    public sealed class RiskOut
    {
        public double? Step { get; set; }
        public double Sum { get; set; }
        public double? Value { get; set; }
    }

    public sealed class VcOut
    {
        public double? Years { get; set; }
        public double? Ret { get; set; }
        public double? Surv { get; set; }
        public double? Exg { get; set; }
        public double? Mult { get; set; }
        public double? New0 { get; set; }
        public double? New { get; set; }
        public double? Exrev0 { get; set; }
        public double? Exrev { get; set; }
        public double? Exeq0 { get; set; }
        public double? Exeq { get; set; }
        public double? Post0 { get; set; }
        public double? Post { get; set; }
        public double? Res { get; set; }
        public double? Share0 { get; set; }
        public double? Share { get; set; }
        public double? Exshare { get; set; }
    }

    public sealed class MarketOut
    {
        public double? Mult0 { get; set; }
        public double? Mult { get; set; }
        public double? Metric0 { get; set; }
        public double? Metric { get; set; }
        public double? Ev { get; set; }
        public double? Value { get; set; }
    }

    public sealed class DcfOut
    {
        public double? R { get; set; }
        public double? G { get; set; }
        public List<double?> Factors { get; set; } = [];
        public List<double?> Pvs { get; set; } = [];
        public double? Tv { get; set; }
        public double? Ev { get; set; }
        public double? TvShare { get; set; }
        public double? Value { get; set; }
    }

    public sealed class ChicagoOut
    {
        public List<double?> P { get; set; } = [];
        public double? Years { get; set; }
        public double? Ret { get; set; }
        public double? Surv { get; set; }
        public double? New { get; set; }
        public List<double?> G { get; set; } = [];
        public double SumP { get; set; }
        public double? Value { get; set; }
    }

    public sealed class ReplacementOut
    {
        public List<double?> Values { get; set; } = [];
        public double Sum { get; set; }
        public double? Value { get; set; }
    }

    public sealed class FinalOut
    {
        public double? Pre { get; set; }
        public double? Post { get; set; }
        public double? Share { get; set; }
        public int N { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public string Reliability { get; set; } = "none";
    }

    public sealed class StartupResult
    {
        public EffectiveOut Effective { get; set; } = new();
        public ScorecardOut Scorecard { get; set; } = new();
        public BerkusOut Berkus { get; set; } = new();
        public RiskOut Risk { get; set; } = new();
        public VcOut Vc { get; set; } = new();
        public MarketOut Market { get; set; } = new();
        public DcfOut Dcf { get; set; } = new();
        public ChicagoOut Chicago { get; set; } = new();
        public ReplacementOut Replacement { get; set; } = new();
        public List<FinalRow> Rows { get; set; } = [];
        public FinalOut Final { get; set; } = new();
        public Dictionary<string, List<Flag>> Flags { get; set; } = new();
        public Dictionary<string, double?> Suggested { get; set; } = new();
    }
}

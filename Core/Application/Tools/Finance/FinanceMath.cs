namespace Application.Tools.Finance;

/// <summary>Shared numerical helpers for the valuation engines (ported from the frontend logic.ts files).</summary>
public static class FinanceMath
{
    public static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));

    /// <summary>Triangular distribution sample.</summary>
    public static double Triangular(double low, double mode, double high)
    {
        var r = Random.Shared.NextDouble();
        var f = (mode - low) / (high - low);
        if (r < f)
            return low + Math.Sqrt(r * (high - low) * (mode - low));
        return high - Math.Sqrt((1 - r) * (high - low) * (high - mode));
    }

    /// <summary>Percentile of an already-sorted array (linear interpolation).</summary>
    public static double Percentile(IReadOnlyList<double> sorted, double p)
    {
        var idx = p * (sorted.Count - 1);
        var lo = (int)Math.Floor(idx);
        var hi = (int)Math.Ceiling(idx);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
    }

    /// <summary>Standard normal CDF (Abramowitz-Stegun style polynomial used by the original engine).</summary>
    public static double NormSDist(double x)
    {
        var t = 1.0 / (1 + 0.2316419 * Math.Abs(x));
        var d = 0.3989423 * Math.Exp((-x * x) / 2);
        var p = d * t * (0.3193815 + t * (-0.3565638 + t * (1.781478 + t * (-1.821256 + t * 1.330274))));
        if (x > 0) p = 1 - p;
        return p;
    }

    public static double RandNormal(double mean, double sd)
    {
        double u1, u2;
        u1 = Random.Shared.NextDouble();
        u2 = Random.Shared.NextDouble();
        if (u1 < 1e-12) u1 = 1e-12;
        var z = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        return mean + sd * z;
    }

    public static double GrowingAnnuityPV(double p, double g, double d, double n)
        => (p * (1 + g)) / (d - g) * (1 - Math.Pow((1 + g) / (1 + d), n));

    public static double OrdinaryAnnuityPV(double p, double d, double n)
        => (p / d) * (1 - Math.Pow(1 / (1 + d), n));

    public static (double Mean, double P10, double P50, double P90, double Min, double Max, double Stdev, int[] Hist, string[] HistLabels) Summarize(
        double[] values, int bins = 12)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / Math.Max(1, values.Length - 1);
        var min = sorted[0];
        var max = sorted[^1];
        var hist = new int[bins];
        var w = (max - min) / bins;
        if (w <= 0) w = 1;
        foreach (var v in values)
        {
            var idx = (int)Math.Floor((v - min) / w);
            if (idx >= bins) idx = bins - 1;
            if (idx < 0) idx = 0;
            hist[idx]++;
        }
        var labels = hist.Select((_, i) => Math.Round(min + i * w).ToString()).ToArray();
        return (mean, Percentile(sorted, 0.10), Percentile(sorted, 0.50), Percentile(sorted, 0.90), min, max, Math.Sqrt(variance), hist, labels);
    }
}

namespace Application.Tools.Finance;

/// <summary>Weighted-score row shared by several calculators (scorecard / risk / legal patterns).</summary>
public sealed record WeightedRow(string Name, double Weight, double Score)
{
    public double Weighted => Weight * Score;
}

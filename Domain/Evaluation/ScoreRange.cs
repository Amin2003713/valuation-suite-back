using Domain.Common;

namespace Domain.Evaluation;

public sealed class ScoreRange
{
    public required double Min { get; init; }
    public required double Max { get; init; }
    public required ScoreLevel Level { get; init; }
    public required string Label { get; init; }
    public string? Color { get; init; }
}

using Domain.Common;

namespace Domain.Evaluation;

public record ScoringProfile
{
    public required string Name { get; init; }
    public required List<ScoreRange> Ranges { get; init; }
    public string? Description { get; init; }
}

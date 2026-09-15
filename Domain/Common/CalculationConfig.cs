namespace Domain.Common;

public sealed record CalculationConfig
{
    public required string Expression { get; init; }
    public required List<string> InputQuestionIds { get; init; }
    public required string OutputKey { get; init; }
    public string? Formula { get; init; }
    public Dictionary<string, double> Weights { get; init; } = [];
    public string? ResultLabel { get; init; }
}

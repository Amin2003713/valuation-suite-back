namespace Domain.Assessments;

public sealed record Option
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Label { get; init; }
    public string? Value { get; init; }
    public int Order { get; init; }
    public bool IsCorrect { get; init; }
    public double? Score { get; init; }
}

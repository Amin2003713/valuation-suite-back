namespace Domain.Common;

public enum VisibilityConditionType
{
    Equals = 1,
    NotEquals = 2,
    GreaterThan = 3,
    LessThan = 4,
    IsFilled = 5,
    IsEmpty = 6
}

public sealed record VisibilityCondition
{
    public required string TargetQuestionId { get; init; }
    public required VisibilityConditionType Type { get; init; }
    public string? Value { get; init; }
}

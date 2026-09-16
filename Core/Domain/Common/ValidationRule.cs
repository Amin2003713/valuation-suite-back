namespace Domain.Common;

public sealed record ValidationRule
{
    public required string Field { get; init; }
    public required ValidationType Type { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Pattern { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum ValidationType
{
    Required = 1,
    Range = 2,
    MinLength = 3,
    MaxLength = 4,
    Regex = 5,
    Custom = 6
}

namespace Domain.Common;

public record MathVariable
{
    public required string Name { get; init; }
    public required string Label { get; init; }
    public double MinValue { get; init; } = double.MinValue;
    public double MaxValue { get; init; } = double.MaxValue;
    public double DefaultValue { get; init; } = 0;
    public bool IsRequired { get; init; } = true;
}

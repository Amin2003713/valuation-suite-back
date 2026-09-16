namespace Domain.Common;

public record MathOperation
{
    public required OperationType Type { get; init; }
    public string? Operand { get; init; }
    public double? Constant { get; init; }
}

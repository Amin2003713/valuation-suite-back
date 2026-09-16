namespace Domain.Common;

public record MathExpression
{
    public required string Expression { get; init; }
    public required List<MathVariable> Variables { get; init; }
    public List<MathOperation> Operations { get; init; } = [];
    public string? PostfixNotation { get; init; }
}

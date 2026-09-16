using Application.Assessments.Commands.SetQuestionMathExpression;

namespace RequestHandlers.Assessments.Commands.SetQuestionMathExpression;

public sealed class SetQuestionMathExpressionCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<SetQuestionMathExpressionCommand>
{
    public async Task Handle(SetQuestionMathExpressionCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.QuestionId, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        var question = version.Steps.SelectMany(s => s.Questions).First(q => q.Id == request.QuestionId);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        question.SetMathExpression(new Domain.Common.MathExpression
        {
            Expression = request.Expression.Expression,
            Variables = request.Expression.Variables.Select(v => new Domain.Common.MathVariable
            {
                Name = v.Name,
                Label = v.Label,
                MinValue = v.MinValue,
                MaxValue = v.MaxValue,
                DefaultValue = v.DefaultValue,
                IsRequired = v.IsRequired
            }).ToList(),
            Operations = request.Expression.Operations.Select(o => new Domain.Common.MathOperation
            {
                Type = o.Type,
                Operand = o.Operand,
                Constant = o.Constant
            }).ToList(),
            PostfixNotation = request.Expression.PostfixNotation
        });

        await versionRepository.SaveChangesAsync(ct);
    }
}

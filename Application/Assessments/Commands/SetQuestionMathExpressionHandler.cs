using MediatR;
using Domain.Assessments;
using Domain.Common;
using Application.Common;

namespace Application.Assessments.Commands;

public class SetQuestionMathExpressionHandler : IRequestHandler<SetQuestionMathExpressionCommand>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public SetQuestionMathExpressionHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task Handle(SetQuestionMathExpressionCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.QuestionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var question = version.Steps.SelectMany(s => s.Questions).FirstOrDefault(q => q.Id == request.QuestionId)
            ?? throw new InvalidOperationException("Question not found");

        question.SetMathExpression(new MathExpression
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
        await _uow.SaveChangesAsync(ct);
    }
}

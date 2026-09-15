using MediatR;
using Domain.Assessments;
using Domain.Common;
using Application.Common;

namespace Application.Assessments.Commands;

public class SetQuestionCalculationHandler : IRequestHandler<SetQuestionCalculationCommand>
{
    private readonly IAssessmentVersionRepository _versionRepo;
    private readonly IUnitOfWork _uow;

    public SetQuestionCalculationHandler(IAssessmentVersionRepository versionRepo, IUnitOfWork uow)
    {
        _versionRepo = versionRepo;
        _uow = uow;
    }

    public async Task Handle(SetQuestionCalculationCommand request, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(request.QuestionId, ct)
            ?? throw new InvalidOperationException("Version not found");

        var question = version.Steps.SelectMany(s => s.Questions).FirstOrDefault(q => q.Id == request.QuestionId)
            ?? throw new InvalidOperationException("Question not found");

        question.SetCalculation(new CalculationConfig
        {
            Expression = request.Calculation.Expression,
            InputQuestionIds = request.Calculation.InputQuestionIds,
            OutputKey = request.Calculation.OutputKey,
            Formula = request.Calculation.Formula,
            Weights = request.Calculation.Weights,
            ResultLabel = request.Calculation.ResultLabel
        });
        await _uow.SaveChangesAsync(ct);
    }
}

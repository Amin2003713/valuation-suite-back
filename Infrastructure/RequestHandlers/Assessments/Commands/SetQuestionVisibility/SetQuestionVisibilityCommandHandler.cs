using Application.Assessments.Commands.SetQuestionVisibility;

namespace RequestHandlers.Assessments.Commands.SetQuestionVisibility;

public sealed class SetQuestionVisibilityCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<SetQuestionVisibilityCommand>
{
    public async Task Handle(SetQuestionVisibilityCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.QuestionId, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        var question = version.Steps.SelectMany(s => s.Questions).First(q => q.Id == request.QuestionId);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        question.SetVisibilityConditions(request.Conditions.Select(c => new Domain.Common.VisibilityCondition
        {
            TargetQuestionId = c.TargetQuestionId,
            Type = c.Type,
            Value = c.Value
        }).ToList());

        await versionRepository.SaveChangesAsync(ct);
    }
}

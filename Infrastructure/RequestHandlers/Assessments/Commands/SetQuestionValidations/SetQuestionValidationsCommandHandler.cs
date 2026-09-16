using Application.Assessments.Commands.SetQuestionValidations;

namespace RequestHandlers.Assessments.Commands.SetQuestionValidations;

public sealed class SetQuestionValidationsCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<SetQuestionValidationsCommand>
{
    public async Task Handle(SetQuestionValidationsCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.QuestionId, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        var question = version.Steps.SelectMany(s => s.Questions).First(q => q.Id == request.QuestionId);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        question.SetValidations(request.Validations.Select(v => new Domain.Common.ValidationRule
        {
            Field = v.Field,
            Type = v.Type,
            MinValue = v.MinValue,
            MaxValue = v.MaxValue,
            MinLength = v.MinLength,
            MaxLength = v.MaxLength,
            Pattern = v.Pattern,
            ErrorMessage = v.ErrorMessage
        }).ToList());

        await versionRepository.SaveChangesAsync(ct);
    }
}

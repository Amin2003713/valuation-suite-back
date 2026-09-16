using Application.Assessments.Commands.UpdateQuestion;

namespace RequestHandlers.Assessments.Commands.UpdateQuestion;

public sealed class UpdateQuestionCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<UpdateQuestionCommand>
{
    public async Task Handle(UpdateQuestionCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        var question = version.Steps.SelectMany(s => s.Questions).First(q => q.Id == request.Id);

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        if (!string.IsNullOrWhiteSpace(request.Text))
            question.Text = request.Text;
        if (request.Key is not null)
            question.SetKey(request.Key);
        if (request.IsRequired is not null)
            question.SetRequired(request.IsRequired.Value);
        if (request.HelpText is not null)
            question.SetHelpText(request.HelpText);

        await versionRepository.SaveChangesAsync(ct);
    }
}

public sealed class RemoveQuestionCommandHandler(
    IAssessmentVersionCommandRepository versionRepository
) : IRequestHandler<RemoveQuestionCommand>
{
    public async Task Handle(RemoveQuestionCommand request, CancellationToken ct)
    {
        var version = await versionRepository.GetTrackedByQuestionIdAsync(request.QuestionId, ct)
            ?? throw ValuationException.NotFound("Question not found.");

        if (!version.IsDraft)
            throw ValuationException.Conflict("Only draft versions can be edited.");

        var step = version.Steps.FirstOrDefault(s => s.Questions.Any(q => q.Id == request.QuestionId));
        step?.RemoveQuestion(request.QuestionId);

        await versionRepository.SaveChangesAsync(ct);
    }
}

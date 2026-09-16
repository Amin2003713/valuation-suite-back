using Application.Attempts.Commands.SyncAnswers;
using Domain.Answers;

namespace RequestHandlers.Attempts.Commands.SyncAnswers;

public sealed class SyncAnswersCommandHandler(
    IAssessmentAttemptCommandRepository attemptRepository,
    IAssessmentVersionQueryRepository versionRepository
) : IRequestHandler<SyncAnswersCommand, SyncAnswersResponse>
{
    public async Task<SyncAnswersResponse> Handle(SyncAnswersCommand request, CancellationToken ct)
    {
        var attempt = await attemptRepository.GetTrackedAsync(request.AttemptId, ct)
            ?? throw ValuationException.NotFound("Attempt not found.");

        if (attempt.Status != Domain.Assessments.AttemptStatus.InProgress)
            throw ValuationException.Conflict("Attempt is no longer in progress.");

        var version = await versionRepository.GetWithStepsAsync(attempt.VersionId, ct)
            ?? throw ValuationException.NotFound("Assessment version not found.");

        var conflicts = 0;
        foreach (var data in request.Answers)
        {
            var question = version.Steps.SelectMany(s => s.Questions).FirstOrDefault(q => q.Id == data.QuestionId)
                ?? throw ValuationException.BadRequest($"Question {data.QuestionId} is not part of this assessment.");

            var existing = attempt.GetAnswer(question.Id);
            if (existing is not null && existing.ClientRevision > request.ClientRevision)
            {
                conflicts++;
                continue;
            }

            var answer = existing ?? Answer.Create(attempt.Id, question.Id, (AnswerValueType)(int)question.Type);
            ApplyValues(answer, data);
            answer.ClientRevision = request.ClientRevision;
            attempt.AddAnswer(answer);
        }

        attempt.IncrementRevision();
        await attemptRepository.SaveChangesAsync(ct);

        return new SyncAnswersResponse(request.Answers.Count - conflicts, conflicts, attempt.ClientRevision);
    }

    private static void ApplyValues(Answer answer, AnswerData data)
    {
        switch (answer.ValueType)
        {
            case AnswerValueType.Boolean:
                answer.SetBooleanValue(data.BooleanValue ?? false);
                break;
            case AnswerValueType.Number:
            case AnswerValueType.Decimal:
            case AnswerValueType.Rating:
                answer.SetNumericValue(data.NumericValue ?? 0);
                break;
            case AnswerValueType.SingleChoice:
            case AnswerValueType.MultipleChoice:
            case AnswerValueType.Dropdown:
                answer.SetChoiceValues(data.ChoiceValues ?? []);
                break;
            case AnswerValueType.Date:
                if (data.DateValue.HasValue) answer.SetDateValue(data.DateValue.Value);
                break;
            case AnswerValueType.LongText:
            case AnswerValueType.Information:
            case AnswerValueType.Conditional:
            case AnswerValueType.Mathematics:
                if (!string.IsNullOrEmpty(data.JsonData)) answer.SetJsonData(data.JsonData);
                else if (!string.IsNullOrEmpty(data.TextValue)) answer.SetTextValue(data.TextValue);
                break;
            default:
                if (!string.IsNullOrEmpty(data.TextValue)) answer.SetTextValue(data.TextValue);
                break;
        }
    }
}

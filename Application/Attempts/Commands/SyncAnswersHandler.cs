using MediatR;
using Domain.Assessments;
using Domain.Answers;
using Application.Common;
using Application.Assessments.Mappers;

namespace Application.Attempts.Commands;

public class SyncAnswersHandler : IRequestHandler<SyncAnswersCommand, SyncAnswersResponse>
{
    private readonly IAssessmentAttemptRepository _attemptRepo;
    private readonly IAnswerRepository _answerRepo;
    private readonly IUnitOfWork _uow;

    public SyncAnswersHandler(IAssessmentAttemptRepository attemptRepo, IAnswerRepository answerRepo, IUnitOfWork uow)
    {
        _attemptRepo = attemptRepo;
        _answerRepo = answerRepo;
        _uow = uow;
    }

    public async Task<SyncAnswersResponse> Handle(SyncAnswersCommand request, CancellationToken ct)
    {
        var attempt = await _attemptRepo.GetByIdAsync(request.AttemptId, ct)
            ?? throw new InvalidOperationException("Attempt not found");

        if (attempt.Status != AttemptStatus.InProgress)
            throw new InvalidOperationException("Attempt is not in progress");

        int savedCount = 0;
        int conflicts = 0;

        attempt.IncrementRevision();

        foreach (var answerData in request.Answers)
        {
            var existing = await _answerRepo.GetByQuestionAsync(request.AttemptId, answerData.QuestionId, ct);

            if (existing != null && existing.ClientRevision > request.ClientRevision)
            {
                conflicts++;
                continue;
            }

            var valueType = DetermineValueType(answerData);
            var answer = existing ?? Answer.Create(request.AttemptId, answerData.QuestionId, valueType);

            switch (valueType)
            {
                case AnswerValueType.Text:
                case AnswerValueType.LongText:
                    answer.SetTextValue(answerData.TextValue ?? "");
                    break;
                case AnswerValueType.Number:
                case AnswerValueType.Decimal:
                case AnswerValueType.Rating:
                    if (answerData.NumericValue.HasValue)
                        answer.SetNumericValue(answerData.NumericValue.Value);
                    break;
                case AnswerValueType.Boolean:
                    if (answerData.BooleanValue.HasValue)
                        answer.SetBooleanValue(answerData.BooleanValue.Value);
                    break;
                case AnswerValueType.Date:
                    if (answerData.DateValue.HasValue)
                        answer.SetDateValue(answerData.DateValue.Value);
                    break;
                case AnswerValueType.SingleChoice:
                case AnswerValueType.MultipleChoice:
                case AnswerValueType.Dropdown:
                    if (answerData.ChoiceValues != null)
                        answer.SetChoiceValues(answerData.ChoiceValues);
                    break;
                default:
                    if (answerData.JsonData != null)
                        answer.SetJsonData(answerData.JsonData);
                    break;
            }

            if (existing == null)
                await _answerRepo.AddAsync(answer, ct);
            else
                await _answerRepo.UpdateAsync(answer, ct);

            savedCount++;
        }

        await _uow.SaveChangesAsync(ct);
        return new SyncAnswersResponse(savedCount, conflicts, attempt.ClientRevision);
    }

    private static AnswerValueType DetermineValueType(AnswerData data)
    {
        if (data.TextValue != null) return AnswerValueType.Text;
        if (data.NumericValue.HasValue) return AnswerValueType.Number;
        if (data.BooleanValue.HasValue) return AnswerValueType.Boolean;
        if (data.DateValue.HasValue) return AnswerValueType.Date;
        if (data.ChoiceValues != null && data.ChoiceValues.Count > 1) return AnswerValueType.MultipleChoice;
        if (data.ChoiceValues != null) return AnswerValueType.SingleChoice;
        if (data.JsonData != null) return AnswerValueType.Mathematics;
        return AnswerValueType.Text;
    }
}

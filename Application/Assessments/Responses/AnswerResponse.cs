using Domain.Answers;

namespace Application.Assessments.Responses;

public record AnswerResponse(
    Guid Id,
    Guid AttemptId,
    Guid QuestionId,
    AnswerValueType ValueType,
    string? TextValue,
    double? NumericValue,
    bool? BooleanValue,
    DateTime? DateValue,
    List<string>? ChoiceValues,
    int ClientRevision,
    DateTime SynchronizedAt
);

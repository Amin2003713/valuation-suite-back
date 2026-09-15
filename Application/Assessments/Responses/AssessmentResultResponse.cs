using Domain.Common;
using Domain.Results;

namespace Application.Assessments.Responses;

public record AssessmentResultResponse(
    Guid Id,
    Guid AttemptId,
    Guid AssessmentId,
    int OverallScore,
    string? Level,
    string? LevelLabel,
    bool IsCalculated,
    DateTime CalculatedAt,
    Dictionary<string, int> StepScores,
    List<ScoredAnswerResponse> ScoredAnswers
);

public record ScoredAnswerResponse(
    Guid QuestionId,
    string QuestionKey,
    QuestionType QuestionType,
    int Score,
    int MaxScore,
    double? CalculatedValue
);

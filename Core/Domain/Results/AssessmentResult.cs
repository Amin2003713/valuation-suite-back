using Domain.Common;
using Domain.Evaluation;
using Domain.Attempts;

namespace Domain.Results;

public sealed class AssessmentResult : BaseEntity
{
    public Guid AttemptId { get; set; }
    public Guid AssessmentId { get; set; }
    public int OverallScore { get; set; }
    public string? Level { get; set; }
    public string? LevelLabel { get; set; }
    public bool IsCalculated { get; set; }
    public DateTime CalculatedAt { get; set; }
    public Dictionary<string, int> StepScores { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = [];
    public List<ScoredAnswer> ScoredAnswers { get; set; } = [];
    public AssessmentAttempt Attempt { get; set; } = null!;

    public static AssessmentResult Create(Guid attemptId, Guid assessmentId)
    {
        return new AssessmentResult
        {
            AttemptId = attemptId,
            AssessmentId = assessmentId
        };
    }

    public void Calculate(int overallScore, string? level, string? levelLabel, Dictionary<string, int> stepScores, List<ScoredAnswer> scoredAnswers)
    {
        OverallScore = overallScore;
        Level = level;
        LevelLabel = levelLabel;
        StepScores = stepScores;
        ScoredAnswers = scoredAnswers;
        IsCalculated = true;
        CalculatedAt = DateTime.UtcNow;
    }
}

public sealed record ScoredAnswer
{
    public Guid QuestionId { get; init; }
    public string QuestionKey { get; init; } = string.Empty;
    public QuestionType QuestionType { get; init; }
    public int Score { get; init; }
    public int MaxScore { get; init; }
    public double? CalculatedValue { get; init; }
}

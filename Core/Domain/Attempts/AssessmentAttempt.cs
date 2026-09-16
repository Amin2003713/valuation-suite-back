using Domain.Common;
using Domain.Assessments;
using Domain.Evaluation;
using Domain.Answers;
using Domain.Results;

namespace Domain.Attempts;

public sealed class AssessmentAttempt : BaseEntity
{
    public Guid VersionId { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ClientRevision { get; set; }
    public AssessmentVersion Version { get; set; } = null!;
    public List<Answer> Answers { get; set; } = [];
    public AssessmentResult? Result { get; set; }

    public static AssessmentAttempt Create(Guid versionId, Guid userId, Guid companyId, int totalSteps)
    {
        return new AssessmentAttempt
        {
            VersionId = versionId,
            UserId = userId,
            CompanyId = companyId,
            TotalSteps = totalSteps,
            StartedAt = DateTime.UtcNow
        };
    }

    public void Complete()
    {
        Status = AttemptStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Abandon() => Status = AttemptStatus.Abandoned;
    public void IncrementRevision() => ClientRevision++;

    public void AddAnswer(Answer answer)
    {
        var existing = Answers.FirstOrDefault(a => a.QuestionId == answer.QuestionId);
        if (existing != null)
            Answers.Remove(existing);
        Answers.Add(answer);
    }

    public Answer? GetAnswer(Guid questionId) => Answers.FirstOrDefault(a => a.QuestionId == questionId);
}

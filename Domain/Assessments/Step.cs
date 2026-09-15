using Domain.Common;
using Domain.Evaluation;

namespace Domain.Assessments;

public sealed class Step : BaseEntity
{
    public Guid VersionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public List<Question> Questions { get; set; } = [];
    public AssessmentVersion Version { get; set; } = null!;

    public void AddQuestion(Question question) => Questions.Add(question);
    public void RemoveQuestion(Guid questionId) => Questions.RemoveAll(q => q.Id == questionId);
    public void SetDescription(string description) => Description = description;
}

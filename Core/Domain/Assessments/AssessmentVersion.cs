using Domain.Common;
using Domain.Evaluation;

namespace Domain.Assessments;

public sealed class AssessmentVersion : BaseEntity
{
    public Guid AssessmentId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDraft { get; set; } = true;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<Step> Steps { get; set; } = [];
    public Assessment Assessment { get; set; } = null!;

    public static AssessmentVersion Create(Assessment assessment, int versionNumber, bool isDraft)
    {
        return new AssessmentVersion
        {
            AssessmentId = assessment.Id,
            VersionNumber = versionNumber,
            IsDraft = isDraft,
            Title = $"Version {versionNumber}"
        };
    }

    public void Publish()
    {
        IsDraft = false;
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
    }

    public void AddStep(Step step) => Steps.Add(step);
    public void RemoveStep(Guid stepId) => Steps.RemoveAll(s => s.Id == stepId);
    public void ReorderSteps(List<Guid> stepIdsInOrder)
    {
        var ordered = stepIdsInOrder.Select(id => Steps.First(s => s.Id == id)).ToList();
        Steps.Clear();
        Steps.AddRange(ordered);
    }
}

using Domain.Common;
using Domain.Evaluation;

namespace Domain.Assessments;

public sealed class Assessment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CompanyId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public int PublishedVersion { get; set; }
    public List<AssessmentVersion> Versions { get; set; } = [];
    public Guid? CurrentDraftId { get; set; }

    public static Assessment Create(string name, string code, string? description, Guid companyId)
    {
        return new Assessment
        {
            Name = name,
            Code = code,
            Description = description,
            CompanyId = companyId
        };
    }

    public void CreateDraftVersion()
    {
        var versionNumber = Versions.Count + 1;
        var version = AssessmentVersion.Create(this, versionNumber, isDraft: true);
        Versions.Add(version);
        CurrentDraftId = version.Id;
    }

    public void PublishVersion(Guid versionId)
    {
        var version = Versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException("Version not found");
        if (version.IsDraft == false)
            throw new InvalidOperationException("Only draft versions can be published");

        version.Publish();
        PublishedVersion = version.VersionNumber;
        IsPublished = true;
    }

    public void Archive()
    {
        IsArchived = true;
        IsPublished = false;
    }
}

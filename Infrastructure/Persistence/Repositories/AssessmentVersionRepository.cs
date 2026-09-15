using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Assessments;

namespace Infrastructure.Persistence.Repositories;

public class AssessmentVersionRepository : Application.Common.IAssessmentVersionRepository
{
    private readonly AssessmentDbContext _db;

    public AssessmentVersionRepository(AssessmentDbContext db) => _db = db;

    public async Task<AssessmentVersion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.AssessmentVersions
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Options)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Validations)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<AssessmentVersion?> GetCurrentDraftAsync(Guid assessmentId, CancellationToken ct = default)
        => await _db.AssessmentVersions
            .Include(v => v.Steps).ThenInclude(s => s.Questions)
            .FirstOrDefaultAsync(v => v.AssessmentId == assessmentId && v.IsDraft, ct);

    public async Task<AssessmentVersion?> GetPublishedVersionAsync(Guid assessmentId, CancellationToken ct = default)
        => await _db.AssessmentVersions
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Options)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Validations)
            .FirstOrDefaultAsync(v => v.AssessmentId == assessmentId && v.IsPublished, ct);

    public async Task<AssessmentVersion?> GetByVersionNumberAsync(Guid assessmentId, int versionNumber, CancellationToken ct = default)
        => await _db.AssessmentVersions.FirstOrDefaultAsync(v => v.AssessmentId == assessmentId && v.VersionNumber == versionNumber, ct);
}

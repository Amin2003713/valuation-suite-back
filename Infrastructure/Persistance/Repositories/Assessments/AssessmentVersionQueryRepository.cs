using Domain.Assessments;

namespace Persistence.Repositories.Assessments;

public class AssessmentVersionQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<AssessmentVersion>> logger
)
    : QueryRepository<AssessmentVersion>(dbContext, logger),
        IAssessmentVersionQueryRepository
{
    public async Task<AssessmentVersion?> GetWithStepsAsync(Guid id, CancellationToken ct = default)
        => await TableNoTracking
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Options)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Validations)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.VisibilityConditions)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Calculation)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.MathExpression).ThenInclude(me => me.Variables)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.MathExpression).ThenInclude(me => me.Operations)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<AssessmentVersion?> GetCurrentDraftAsync(Guid assessmentId, CancellationToken ct = default)
        => await TableNoTracking
            .Include(v => v.Steps)
            .FirstOrDefaultAsync(v => v.AssessmentId == assessmentId && v.IsDraft, ct);

    public async Task<AssessmentVersion?> GetPublishedVersionAsync(Guid assessmentId, CancellationToken ct = default)
        => await TableNoTracking
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Options)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Validations)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.VisibilityConditions)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.Calculation)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.MathExpression).ThenInclude(me => me.Variables)
            .Include(v => v.Steps).ThenInclude(s => s.Questions).ThenInclude(q => q.MathExpression).ThenInclude(me => me.Operations)
            .FirstOrDefaultAsync(v => v.AssessmentId == assessmentId && v.IsPublished, ct);

    public async Task<AssessmentVersion?> GetByVersionNumberAsync(Guid assessmentId, int versionNumber, CancellationToken ct = default)
        => await TableNoTracking
            .FirstOrDefaultAsync(v => v.AssessmentId == assessmentId && v.VersionNumber == versionNumber, ct);

    public async Task<List<AssessmentVersion>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default)
        => await TableNoTracking
            .Where(v => v.AssessmentId == assessmentId)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync(ct);
}

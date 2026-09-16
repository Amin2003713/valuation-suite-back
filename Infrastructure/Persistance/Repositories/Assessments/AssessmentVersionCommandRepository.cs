using Domain.Assessments;

namespace Persistence.Repositories.Assessments;

public class AssessmentVersionCommandRepository(
    WriteOnlyDbContext dbContext,
    IdentityService identityService,
    ILogger<CommandRepository<AssessmentVersion>> logger
)
    : CommandRepository<AssessmentVersion>(dbContext, identityService, logger),
        IAssessmentVersionCommandRepository
{
    public async Task<AssessmentVersion?> GetTrackedWithStepsAsync(Guid id, CancellationToken ct = default)
        => await Table
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Options)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Validations)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.VisibilityConditions)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Calculation)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.MathExpression)
            .ThenInclude(me => me.Variables)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.MathExpression)
            .ThenInclude(me => me.Operations)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<AssessmentVersion?> GetTrackedByStepIdAsync(Guid stepId, CancellationToken ct = default)
        => await Table
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Options)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Validations)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.VisibilityConditions)
            .FirstOrDefaultAsync(v => v.Steps.Any(s => s.Id == stepId), ct);

    public async Task<AssessmentVersion?> GetTrackedByQuestionIdAsync(Guid questionId, CancellationToken ct = default)
        => await Table
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Options)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Validations)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.VisibilityConditions)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.Calculation)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.MathExpression)
            .ThenInclude(me => me.Variables)
            .Include(v => v.Steps)
            .ThenInclude(s => s.Questions)
            .ThenInclude(q => q.MathExpression)
            .ThenInclude(me => me.Operations)
            .FirstOrDefaultAsync(v => v.Steps.Any(s => s.Questions.Any(q => q.Id == questionId)), ct);
}

using Application.Interfaces.Base;
using Domain.Answers;
using Domain.Assessments;
using Domain.Attempts;
using Domain.Companies;
using Domain.Results;
using Domain.Users;

namespace Application.Interfaces;

// =========================================================
// Assessments
// =========================================================

public interface IAssessmentCommandRepository : ICommandRepository<Assessment>
{
    Task<Assessment?> GetTrackedAsync(Guid id, CancellationToken ct = default);
}

public interface IAssessmentQueryRepository : IQueryRepository<Assessment>
{
    Task<Assessment?> GetWithVersionsAsync(Guid id, CancellationToken ct = default);
    Task<List<Assessment>> GetByCompanyAsync(Guid companyId, CancellationToken ct = default);
}

public interface IAssessmentVersionCommandRepository : ICommandRepository<AssessmentVersion>
{
    Task<AssessmentVersion?> GetTrackedWithStepsAsync(Guid id, CancellationToken ct = default);
    Task<AssessmentVersion?> GetTrackedByStepIdAsync(Guid stepId, CancellationToken ct = default);
    Task<AssessmentVersion?> GetTrackedByQuestionIdAsync(Guid questionId, CancellationToken ct = default);
}

public interface IAssessmentVersionQueryRepository : IQueryRepository<AssessmentVersion>
{
    Task<AssessmentVersion?> GetWithStepsAsync(Guid id, CancellationToken ct = default);
    Task<AssessmentVersion?> GetCurrentDraftAsync(Guid assessmentId, CancellationToken ct = default);
    Task<AssessmentVersion?> GetPublishedVersionAsync(Guid assessmentId, CancellationToken ct = default);
    Task<AssessmentVersion?> GetByVersionNumberAsync(Guid assessmentId, int versionNumber, CancellationToken ct = default);
    Task<List<AssessmentVersion>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default);
}

// =========================================================
// Attempts & answers
// =========================================================

public interface IAssessmentAttemptCommandRepository : ICommandRepository<AssessmentAttempt>
{
    Task<AssessmentAttempt?> GetTrackedAsync(Guid id, CancellationToken ct = default);
}

public interface IAssessmentAttemptQueryRepository : IQueryRepository<AssessmentAttempt>
{
    Task<AssessmentAttempt?> GetWithAnswersAsync(Guid id, CancellationToken ct = default);
    Task<AssessmentAttempt?> GetActiveAttemptAsync(Guid versionId, Guid userId, CancellationToken ct = default);
    Task<List<AssessmentAttempt>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IAnswerCommandRepository : ICommandRepository<Answer>;
public interface IAnswerQueryRepository : IQueryRepository<Answer>
{
    Task<Answer?> GetByQuestionAsync(Guid attemptId, Guid questionId, CancellationToken ct = default);
    Task<List<Answer>> GetByAttemptAsync(Guid attemptId, CancellationToken ct = default);
}

// =========================================================
// Results
// =========================================================

public interface IAssessmentResultCommandRepository : ICommandRepository<AssessmentResult>;
public interface IAssessmentResultQueryRepository : IQueryRepository<AssessmentResult>
{
    Task<AssessmentResult?> GetByAttemptAsync(Guid attemptId, CancellationToken ct = default);
    Task<List<AssessmentResult>> GetByAssessmentAsync(Guid assessmentId, CancellationToken ct = default);
}

// =========================================================
// Companies & users
// =========================================================

public interface ICompanyCommandRepository : ICommandRepository<Company>
{
    Task<Company?> GetTrackedAsync(Guid id, CancellationToken ct = default);
}

public interface ICompanyQueryRepository : IQueryRepository<Company>
{
    Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public interface IUserCommandRepository : ICommandRepository<ApplicationUser>;
public interface IUserQueryRepository : IQueryRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<ApplicationUser?> GetWithCompanyAsync(Guid id, CancellationToken ct = default);
}

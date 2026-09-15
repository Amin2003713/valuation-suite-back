using MediatR;

namespace Application.Common;

public interface IAssessmentRepository
{
    Task<Domain.Assessments.Assessment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Domain.Assessments.Assessment assessment, CancellationToken ct = default);
    Task UpdateAsync(Domain.Assessments.Assessment assessment, CancellationToken ct = default);
}

public interface IAssessmentVersionRepository
{
    Task<Domain.Assessments.AssessmentVersion?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Assessments.AssessmentVersion?> GetCurrentDraftAsync(Guid assessmentId, CancellationToken ct = default);
    Task<Domain.Assessments.AssessmentVersion?> GetPublishedVersionAsync(Guid assessmentId, CancellationToken ct = default);
    Task<Domain.Assessments.AssessmentVersion?> GetByVersionNumberAsync(Guid assessmentId, int versionNumber, CancellationToken ct = default);
}

public interface IAssessmentAttemptRepository
{
    Task<Domain.Attempts.AssessmentAttempt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Attempts.AssessmentAttempt?> GetActiveAttemptAsync(Guid versionId, Guid userId, CancellationToken ct = default);
    Task<Domain.Attempts.AssessmentAttempt?> GetWithAnswersAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Domain.Attempts.AssessmentAttempt attempt, CancellationToken ct = default);
    Task UpdateAsync(Domain.Attempts.AssessmentAttempt attempt, CancellationToken ct = default);
}

public interface IAnswerRepository
{
    Task<Domain.Answers.Answer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Answers.Answer?> GetByQuestionAsync(Guid attemptId, Guid questionId, CancellationToken ct = default);
    Task<List<Domain.Answers.Answer>> GetByAttemptAsync(Guid attemptId, CancellationToken ct = default);
    Task AddAsync(Domain.Answers.Answer answer, CancellationToken ct = default);
    Task UpdateAsync(Domain.Answers.Answer answer, CancellationToken ct = default);
}

public interface ICompanyRepository
{
    Task<Domain.Companies.Company?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Domain.Companies.Company?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Domain.Companies.Company company, CancellationToken ct = default);
    Task UpdateAsync(Domain.Companies.Company company, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<Domain.Users.User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Domain.Users.User user, CancellationToken ct = default);
    Task UpdateAsync(Domain.Users.User user, CancellationToken ct = default);
}

using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Attempts;
using Domain.Assessments;

namespace Infrastructure.Persistence.Repositories;

public class AssessmentAttemptRepository : Application.Common.IAssessmentAttemptRepository
{
    private readonly AssessmentDbContext _db;

    public AssessmentAttemptRepository(AssessmentDbContext db) => _db = db;

    public async Task<AssessmentAttempt?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.AssessmentAttempts.Include(a => a.Answers).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<AssessmentAttempt?> GetActiveAttemptAsync(Guid versionId, Guid userId, CancellationToken ct = default)
        => await _db.AssessmentAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.VersionId == versionId && a.UserId == userId && a.Status == AttemptStatus.InProgress, ct);

    public async Task<AssessmentAttempt?> GetWithAnswersAsync(Guid id, CancellationToken ct = default)
        => await _db.AssessmentAttempts
            .Include(a => a.Answers)
            .Include(a => a.Result)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(AssessmentAttempt attempt, CancellationToken ct = default)
        => await _db.AssessmentAttempts.AddAsync(attempt, ct);

    public async Task UpdateAsync(AssessmentAttempt attempt, CancellationToken ct = default)
        => _db.AssessmentAttempts.Update(attempt);
}

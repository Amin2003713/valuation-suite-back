using Domain.Answers;

namespace Persistence.Repositories.Attempts;

public class AnswerQueryRepository(
    ReadOnlyDbContext dbContext,
    ILogger<QueryRepository<Answer>> logger
)
    : QueryRepository<Answer>(dbContext, logger),
        IAnswerQueryRepository
{
    public async Task<Answer?> GetByQuestionAsync(Guid attemptId, Guid questionId, CancellationToken ct = default)
        => await TableNoTracking
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);

    public async Task<List<Answer>> GetByAttemptAsync(Guid attemptId, CancellationToken ct = default)
        => await TableNoTracking
            .Where(a => a.AttemptId == attemptId)
            .ToListAsync(ct);
}

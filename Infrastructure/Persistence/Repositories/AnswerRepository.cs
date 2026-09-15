using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Answers;

namespace Infrastructure.Persistence.Repositories;

public class AnswerRepository : Application.Common.IAnswerRepository
{
    private readonly AssessmentDbContext _db;

    public AnswerRepository(AssessmentDbContext db) => _db = db;

    public async Task<Answer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Answers.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Answer?> GetByQuestionAsync(Guid attemptId, Guid questionId, CancellationToken ct = default)
        => await _db.Answers.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);

    public async Task<List<Answer>> GetByAttemptAsync(Guid attemptId, CancellationToken ct = default)
        => await _db.Answers.Where(a => a.AttemptId == attemptId).ToListAsync(ct);

    public async Task AddAsync(Answer answer, CancellationToken ct = default)
        => await _db.Answers.AddAsync(answer, ct);

    public async Task UpdateAsync(Answer answer, CancellationToken ct = default)
        => _db.Answers.Update(answer);
}

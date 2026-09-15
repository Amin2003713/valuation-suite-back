using Infrastructure.Persistence;
using Application.Common;

namespace Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AssessmentDbContext _db;

    public UnitOfWork(AssessmentDbContext db) => _db = db;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}

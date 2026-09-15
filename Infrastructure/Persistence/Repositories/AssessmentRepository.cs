using Infrastructure.Persistence;
using Domain.Companies;
using Domain.Assessments;
using Domain.Users;
using Domain.Attempts;
using Domain.Answers;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class AssessmentRepository : Application.Common.IAssessmentRepository
{
    private readonly AssessmentDbContext _db;

    public AssessmentRepository(AssessmentDbContext db) => _db = db;

    public async Task<Assessment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Assessments.Include(a => a.Versions).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Assessment?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _db.Assessments.FirstOrDefaultAsync(a => a.Code == code, ct);

    public async Task<List<Assessment>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
        => await _db.Assessments.Where(a => a.CompanyId == companyId).Include(a => a.Versions).ToListAsync(ct);

    public async Task AddAsync(Assessment assessment, CancellationToken ct = default)
        => await _db.Assessments.AddAsync(assessment, ct);

    public async Task UpdateAsync(Assessment assessment, CancellationToken ct = default)
        => _db.Assessments.Update(assessment);
}

using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Companies;

namespace Infrastructure.Persistence.Repositories;

public class CompanyRepository : Application.Common.ICompanyRepository
{
    private readonly AssessmentDbContext _db;

    public CompanyRepository(AssessmentDbContext db) => _db = db;

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Company?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _db.Companies.FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task AddAsync(Company company, CancellationToken ct = default)
        => await _db.Companies.AddAsync(company, ct);

    public async Task UpdateAsync(Company company, CancellationToken ct = default)
        => _db.Companies.Update(company);
}

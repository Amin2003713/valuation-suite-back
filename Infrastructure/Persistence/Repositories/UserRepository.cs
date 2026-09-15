using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Users;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository : Application.Common.IUserRepository
{
    private readonly AssessmentDbContext _db;

    public UserRepository(AssessmentDbContext db) => _db = db;

    public async Task<Domain.Users.User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<Domain.Users.User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task AddAsync(Domain.Users.User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public async Task UpdateAsync(Domain.Users.User user, CancellationToken ct = default)
        => _db.Users.Update(user);
}

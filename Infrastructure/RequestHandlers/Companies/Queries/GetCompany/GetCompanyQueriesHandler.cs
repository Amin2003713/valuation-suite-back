using Application.Companies.Queries.GetCompany;

namespace RequestHandlers.Companies.Queries.GetCompany;

public sealed class GetCompanyBySlugQueryHandler(
    ICompanyQueryRepository repository
) : IRequestHandler<GetCompanyBySlugQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetCompanyBySlugQuery request, CancellationToken ct)
    {
        var company = await repository.GetBySlugAsync(request.Slug, ct);
        return company is null ? null : company.ToCompanyResponse();
    }
}

public sealed class GetMyCompanyQueryHandler(
    IUserQueryRepository userRepository,
    ICurrentUserAccessor currentUser
) : IRequestHandler<GetMyCompanyQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetMyCompanyQuery request, CancellationToken ct)
    {
        // Always the caller's own company — the user id is not client-supplied.
        var userId = currentUser.RequireUserId();
        var user = await userRepository.GetWithCompanyAsync(userId, ct);
        return user?.Company is null ? null : user.Company.ToCompanyResponse();
    }
}

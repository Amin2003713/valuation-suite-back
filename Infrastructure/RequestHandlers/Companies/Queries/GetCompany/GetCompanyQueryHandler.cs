namespace RequestHandlers.Companies.Queries.GetCompany;

public sealed class GetCompanyQueryHandler(
    ICompanyQueryRepository repository
) : IRequestHandler<GetCompanyQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetCompanyQuery request, CancellationToken ct)
    {
        var company = await repository.GetByIdAsync(ct, request.Id);
        return company is null ? null : company.ToCompanyResponse();
    }
}

using Application.Companies.Commands.CreateCompany;

namespace RequestHandlers.Companies.Commands.CreateCompany;

public sealed class CreateCompanyCommandHandler(
    ICompanyCommandRepository companyRepository
) : IRequestHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        var company = Domain.Companies.Company.Create(request.Name, request.Slug);
        if (!string.IsNullOrWhiteSpace(request.Industry))
            company.SetIndustry(request.Industry);

        await companyRepository.AddAsync(company, ct, saveNow: true);
        return company.Id;
    }
}

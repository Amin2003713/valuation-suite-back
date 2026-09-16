using Application.Companies.Commands.UpdateCompany;

namespace RequestHandlers.Companies.Commands.UpdateCompany;

public sealed class UpdateCompanyCommandHandler(
    ICompanyCommandRepository companyRepository
) : IRequestHandler<UpdateCompanyCommand>
{
    public async Task Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var company = await companyRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Company not found.");

        if (!string.IsNullOrWhiteSpace(request.Name))
            company.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Industry))
            company.SetIndustry(request.Industry);

        await companyRepository.SaveChangesAsync(ct);
    }
}

public sealed class UpgradeCompanyToProCommandHandler(
    ICompanyCommandRepository companyRepository
) : IRequestHandler<UpgradeCompanyToProCommand>
{
    public async Task Handle(UpgradeCompanyToProCommand request, CancellationToken ct)
    {
        var company = await companyRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Company not found.");

        company.UpgradeToPro();
        await companyRepository.SaveChangesAsync(ct);
    }
}

public sealed class DowngradeCompanyToFreeCommandHandler(
    ICompanyCommandRepository companyRepository
) : IRequestHandler<DowngradeCompanyToFreeCommand>
{
    public async Task Handle(DowngradeCompanyToFreeCommand request, CancellationToken ct)
    {
        var company = await companyRepository.GetTrackedAsync(request.Id, ct)
            ?? throw ValuationException.NotFound("Company not found.");

        company.DowngradeToFree();
        await companyRepository.SaveChangesAsync(ct);
    }
}

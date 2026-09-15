using MediatR;
using Domain.Companies;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Commands;

namespace Application.Companies.Commands;

public class CreateCompanyHandler : IRequestHandler<CreateCompanyCommand, Guid>
{
    private readonly ICompanyRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateCompanyHandler(ICompanyRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        var company = Company.Create(request.Name, request.Slug);
        if (!string.IsNullOrEmpty(request.Industry))
            company.SetIndustry(request.Industry);
        await _repo.AddAsync(company, ct);
        await _uow.SaveChangesAsync(ct);
        return company.Id;
    }
}

public class UpgradeCompanyToProHandler : IRequestHandler<UpgradeCompanyToProCommand>
{
    private readonly ICompanyRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpgradeCompanyToProHandler(ICompanyRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(UpgradeCompanyToProCommand request, CancellationToken ct)
    {
        var company = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Company not found");
        company.UpgradeToPro();
        await _uow.SaveChangesAsync(ct);
    }
}

public class DowngradeCompanyToFreeHandler : IRequestHandler<DowngradeCompanyToFreeCommand>
{
    private readonly ICompanyRepository _repo;
    private readonly IUnitOfWork _uow;

    public DowngradeCompanyToFreeHandler(ICompanyRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(DowngradeCompanyToFreeCommand request, CancellationToken ct)
    {
        var company = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException("Company not found");
        company.DowngradeToFree();
        await _uow.SaveChangesAsync(ct);
    }
}

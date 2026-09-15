using MediatR;
using Domain.Companies;
using Application.Common;
using Application.Assessments.Mappers;
using Application.Assessments.Responses;

namespace Application.Companies.Queries;

public class GetCompanyHandler : IRequestHandler<GetCompanyQuery, CompanyResponse?>
{
    private readonly ICompanyRepository _repo;

    public GetCompanyHandler(ICompanyRepository repo) => _repo = repo;

    public async Task<CompanyResponse?> Handle(GetCompanyQuery request, CancellationToken ct)
        => (await _repo.GetByIdAsync(request.Id, ct))?.ToCompanyResponse();
}

public class GetCompanyBySlugHandler : IRequestHandler<GetCompanyBySlugQuery, CompanyResponse?>
{
    private readonly ICompanyRepository _repo;

    public GetCompanyBySlugHandler(ICompanyRepository repo) => _repo = repo;

    public async Task<CompanyResponse?> Handle(GetCompanyBySlugQuery request, CancellationToken ct)
        => (await _repo.GetBySlugAsync(request.Slug, ct))?.ToCompanyResponse();
}

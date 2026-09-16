using Application.Assessments.Responses;
using MediatR;

namespace Application.Companies.Queries.GetCompany;

public class GetCompanyQuery : IRequest<CompanyResponse?>
{
    public Guid Id { get; set; }
}

public class GetCompanyBySlugQuery : IRequest<CompanyResponse?>
{
    public string Slug { get; set; } = default!;
}

public class GetMyCompanyQuery : IRequest<CompanyResponse?>
{
    public Guid UserId { get; set; }
}

using MediatR;
using Application.Assessments.Responses;

namespace Application.Companies.Queries;

public record GetCompanyQuery(Guid Id) : IRequest<CompanyResponse?>;
public record GetCompanyBySlugQuery(string Slug) : IRequest<CompanyResponse?>;
public record GetMyCompanyQuery(Guid UserId) : IRequest<CompanyResponse?>;

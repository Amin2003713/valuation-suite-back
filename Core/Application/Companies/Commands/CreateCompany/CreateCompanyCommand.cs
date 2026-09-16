using MediatR;

namespace Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommand : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Industry { get; set; }
}

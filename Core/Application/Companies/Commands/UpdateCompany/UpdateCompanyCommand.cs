using MediatR;

namespace Application.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommand : IRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Industry { get; set; }
}

public class UpgradeCompanyToProCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DowngradeCompanyToFreeCommand : IRequest
{
    public Guid Id { get; set; }
}

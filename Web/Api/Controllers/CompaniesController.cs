using ApiFramework.Controller;
using Application.Companies.Commands.CreateCompany;
using Application.Companies.Commands.UpdateCompany;
using Application.Companies.Queries.GetCompany;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ApiBaseController
{
    [HttpGet("{id:guid}", Name = "GetCompanyById")]
    public Task<IActionResult> GetCompany(Guid id, CancellationToken ct)
        => ExecuteAsync(new GetCompanyQuery { Id = id }, ct);

    [HttpGet("by-slug/{slug}")]
    public Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        => ExecuteAsync(new GetCompanyBySlugQuery { Slug = slug }, ct);

    [HttpGet("my")]
    public Task<IActionResult> GetMyCompany([FromQuery] Guid userId, CancellationToken ct)
        => ExecuteAsync(new GetMyCompanyQuery { UserId = userId }, ct);

    [HttpPost]
    public Task<IActionResult> CreateCompany([FromBody] CreateCompanyCommand command, CancellationToken ct)
        => ExecuteCreateAsync(command, nameof(GetCompany), ct);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyCommand command, CancellationToken ct)
    {
        command.Id = id;
        return ExecuteAsync(command, ct);
    }

    [HttpPost("{id:guid}/upgrade")]
    public Task<IActionResult> UpgradeToPro(Guid id, CancellationToken ct)
        => ExecuteAsync(new UpgradeCompanyToProCommand { Id = id }, ct);

    [HttpPost("{id:guid}/downgrade")]
    public Task<IActionResult> DowngradeToFree(Guid id, CancellationToken ct)
        => ExecuteAsync(new DowngradeCompanyToFreeCommand { Id = id }, ct);
}

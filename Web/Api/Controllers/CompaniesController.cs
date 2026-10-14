using ApiFramework.Controller;
using Application.Companies.Commands.CreateCompany;
using Application.Companies.Commands.UpdateCompany;
using Application.Companies.Queries.GetCompany;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Company records. The whole surface requires an authenticated caller; the
///     "my company" lookup and profile edits derive the acting user from the JWT,
///     and plan changes are staff-only because the legitimate upgrade path is a
///     paid checkout (PaymentsController), never a client call.
/// </summary>
[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ApiBaseController
{
    [HttpGet("{id:guid}", Name = "GetCompanyById")]
    public Task<IActionResult> GetCompany(Guid id, CancellationToken ct)
        => ExecuteAsync(new GetCompanyQuery { Id = id }, ct);

    [HttpGet("by-slug/{slug}")]
    public Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        => ExecuteAsync(new GetCompanyBySlugQuery { Slug = slug }, ct);

    /// <summary>The caller's own company — identity comes from the JWT, never a query string.</summary>
    [HttpGet("my")]
    public Task<IActionResult> GetMyCompany(CancellationToken ct)
        => ExecuteAsync(new GetMyCompanyQuery(), ct);

    /// <summary>Staff-only: real sign-up creates the company inside /api/auth/register.</summary>
    [HttpPost]
    [Authorize(Policy = "perm:customers.manage")]
    public Task<IActionResult> CreateCompany([FromBody] CreateCompanyCommand command, CancellationToken ct)
        => ExecuteCreateAsync(command, nameof(GetCompany), ct);

    /// <summary>Renames the caller's own company, or any company for staff.</summary>
    [HttpPut("{id:guid}")]
    public Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyCommand command, CancellationToken ct)
    {
        command.Id = id;
        return ExecuteAsync(command, ct);
    }

    /// <summary>Staff-only: plan grants go through the payment gateway for customers.</summary>
    [HttpPost("{id:guid}/upgrade")]
    [Authorize(Policy = "perm:customers.manage")]
    public Task<IActionResult> UpgradeToPro(Guid id, CancellationToken ct)
        => ExecuteAsync(new UpgradeCompanyToProCommand { Id = id }, ct);

    [HttpPost("{id:guid}/downgrade")]
    [Authorize(Policy = "perm:customers.manage")]
    public Task<IActionResult> DowngradeToFree(Guid id, CancellationToken ct)
        => ExecuteAsync(new DowngradeCompanyToFreeCommand { Id = id }, ct);
}

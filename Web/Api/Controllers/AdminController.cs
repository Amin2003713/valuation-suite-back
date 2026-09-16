using Application.Admin;
using ApiFramework.Controller;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Controllers;

/// <summary>
///     Thin HTTP surface for the admin area. No business logic — every action maps
///     to a MediatR request handled in the application layer, and every route is
///     gated by a permission policy ("perm:*" claims, stamped at login).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>KPI dashboard — totals, revenue, tool usage, recent signups.</summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "perm:dashboard.view")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminDashboardQuery(), ct));

    /// <summary>Paged, searchable customer list (users + companies).</summary>
    [HttpGet("customers")]
    [Authorize(Policy = "perm:customers.read")]
    public async Task<IActionResult> Customers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? plan = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAdminCustomersQuery(page, pageSize, search, plan, isActive), ct));

    /// <summary>Customer detail: profile + payments + tool submissions (results).</summary>
    [HttpGet("customers/{id:guid}")]
    [Authorize(Policy = "perm:customers.read")]
    public async Task<IActionResult> Customer(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminCustomerQuery(id), ct));

    /// <summary>Modify a customer: display name, plan/expiry, active flag.</summary>
    [HttpPut("customers/{id:guid}")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateAdminCustomerCommand(
            id, request.DisplayName, request.Plan, request.PlanExpiresAt, request.IsActive), ct));

    /// <summary>Assign admin roles (Admin/Support/Analyst) — Admin-only.</summary>
    [HttpPut("customers/{id:guid}/roles")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> SetRoles(Guid id, [FromBody] SetRolesRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new SetAdminRolesCommand(id, request.Roles), ct));

    /// <summary>Paged payments list with status filter.</summary>
    [HttpGet("payments")]
    [Authorize(Policy = "perm:payments.read")]
    public async Task<IActionResult> Payments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAdminPaymentsQuery(page, pageSize, userId, status), ct));

    public sealed record SetRolesRequest(List<string> Roles);
}

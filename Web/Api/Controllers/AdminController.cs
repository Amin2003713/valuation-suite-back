using System.Text;
using Application.Admin;
using ApiFramework.Controller;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Controllers;

/// <summary>
///     Thin HTTP surface for the admin area. No business logic — every action maps
///     to a MediatR request handled in the application layer, and every route is
///     gated by a permission policy ("perm:*" claims, stamped at login).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController(
    IMediator mediator,
    Application.Interfaces.Base.IQueryRepository<Domain.Tools.ToolForm> toolForms) : ControllerBase
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

    /// <summary>Paged, filterable submissions explorer across all users.</summary>
    [HttpGet("submissions")]
    [Authorize(Policy = "perm:submissions.read")]
    public async Task<IActionResult> Submissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? toolCode = null,
        [FromQuery] string? search = null,
        [FromQuery] Guid? userId = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAdminSubmissionsQuery(page, pageSize, toolCode, search, userId), ct));

    /// <summary>Tool catalog with usage stats.</summary>
    [HttpGet("tools")]
    [Authorize(Policy = "perm:dashboard.view")]
    public async Task<IActionResult> Tools(CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminToolsQuery(), ct));

    /// <summary>Tool price list for the pricing builder (advanced/advice Toman).</summary>
    [HttpGet("tools/prices")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> ToolPrices(CancellationToken ct)
    {
        var forms = await toolForms.TableNoTracking.OrderBy(t => t.SortOrder).ToListAsync(ct);
        return Ok(forms.Select(t => new
        {
            t.ToolCode, t.Title,
            AdvancedPriceToman = t.AdvancedPriceToman,
            AdvicePriceToman = t.AdvicePriceToman,
        }).OrderBy(x => x.ToolCode).ToList());
    }

    /// <summary>
    ///     Update advanced/advice prices of one tool (pricing builder). Prices are
    ///     read live by the pricing catalog, tool forms and checkout, so changes
    ///     reflect everywhere on the next request — no caching layer involved.
    /// </summary>
    public record UpdateToolPricesRequest(long AdvancedPriceToman, long AdvicePriceToman);

    [HttpPut("tools/{toolCode}/prices")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> UpdateToolPrices(string toolCode, [FromBody] UpdateToolPricesRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateAdminToolPricesCommand(toolCode, request.AdvancedPriceToman, request.AdvicePriceToman), ct));

    /// <summary>Paged company list with member/revenue aggregates.</summary>
    [HttpGet("companies")]
    [Authorize(Policy = "perm:customers.read")]
    public async Task<IActionResult> Companies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAdminCompaniesQuery(page, pageSize, search), ct));

    /// <summary>Admin-initiated password reset (support override).</summary>
    [HttpPut("customers/{id:guid}/password")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await mediator.Send(new ResetAdminPasswordCommand(id, request.NewPassword), ct);
        return NoContent();
    }

    // ─── User manager (staff administration) ───

    /// <summary>Paged user list with role filter and search — user manager page.</summary>
    [HttpGet("users")]
    [Authorize(Policy = "perm:customers.read")]
    public async Task<IActionResult> Users(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAdminUsersQuery(page, pageSize, search, role), ct));

    /// <summary>Creates a staff user (Admin/Support/Analyst/Adviser) with an initial password.</summary>
    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new CreateAdminUserCommand(request.Name, request.Email, request.Password, request.Roles), ct));

    /// <summary>Activates/deactivates a user account.</summary>
    [HttpPut("users/{id:guid}/active")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> SetUserActive(Guid id, [FromBody] SetUserActiveRequest request, CancellationToken ct)
    {
        await mediator.Send(new SetAdminUserActiveCommand(id, request.IsActive), ct);
        return NoContent();
    }

    /// <summary>CSV export of the payments ledger.</summary>
    [HttpGet("payments/export")]
    [Authorize(Policy = "perm:payments.read")]
    public async Task<IActionResult> ExportPayments(CancellationToken ct)
    {
        var payments = await mediator.Send(new GetAdminPaymentsQuery(1, 10_000, null, null), ct);

        var sb = new StringBuilder("Amount,Gateway,RefId,Status,Description,PaidAt,CreatedAt\n");
        foreach (var p in payments.Items)
        {
            sb.Append(p.Amount).Append(',')
              .Append(p.Gateway).Append(',')
              .Append(p.RefId ?? string.Empty).Append(',')
              .Append(p.Status).Append(',')
              .Append('"').Append((p.Description ?? string.Empty).Replace("\"", "\"\"")).Append("\",")
              .Append(p.PaidAt?.ToString("O") ?? string.Empty).Append(',')
              .Append(p.CreatedAt.ToString("O"))
              .Append('\n');
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"payments-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

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

    // ─── Session 5: visual analytics, access & notes management ───

    /// <summary>Chart-ready analytics (visual reporting).</summary>
    [HttpGet("analytics")]
    [Authorize(Policy = "perm:dashboard.view")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminAnalyticsQuery(), ct));

    /// <summary>All active tool-access grants (optionally by user).</summary>
    [HttpGet("grants")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> Grants([FromQuery] Guid? userId, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminGrantsQuery(userId), ct));

    /// <summary>Grant a tool access (Days=null → perpetual).</summary>
    [HttpPost("grants")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> Grant([FromBody] GrantRequest request, CancellationToken ct)
    {
        await mediator.Send(new GrantAdminAccessCommand(request.UserId, request.ToolCode, request.Days), ct);
        return NoContent();
    }

    /// <summary>Revoke a grant.</summary>
    [HttpDelete("grants/{id:guid}")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> RevokeGrant(Guid id, CancellationToken ct)
    {
        await mediator.Send(new RevokeAdminAccessCommand(id), ct);
        return NoContent();
    }

    /// <summary>Delete a package (marks inactive).</summary>
    [HttpDelete("packages/{id:guid}")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> DeletePackage(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new DeleteAdminPackageCommand(id), ct));

    /// <summary>List packages (paged).</summary>
    [HttpGet("packages")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> Packages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetAdminPackagesQuery(page, pageSize), ct));

    /// <summary>Create or update an access package.</summary>
    [HttpPut("packages/{id:guid}")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> UpdatePackage(Guid id, [FromBody] PackageRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpsertAdminPackageCommand(
            id, request.Name, request.Description, request.PickCount,
            request.ToolCodes, request.DurationDays, request.PriceToman, request.IsActive, request.SortOrder), ct));

    /// <summary>Create a package.</summary>
    [HttpPost("packages")]
    [Authorize(Policy = "perm:access.manage")]
    public async Task<IActionResult> CreatePackage([FromBody] PackageRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpsertAdminPackageCommand(
            null, request.Name, request.Description, request.PickCount,
            request.ToolCodes, request.DurationDays, request.PriceToman, request.IsActive, request.SortOrder), ct));

    /// <summary>Advice threads: new purchases plus open conversations (customer replied last).</summary>
    [HttpGet("adviser-queue")]
    [Authorize(Policy = "perm:notes.write")]
    public async Task<IActionResult> AdviserQueue(CancellationToken ct)
        => Ok(await mediator.Send(new GetAdviserQueueQuery(), ct));

    /// <summary>Add a text/voice note to a submission (adviser).</summary>
    [HttpPost("submissions/{id:guid}/notes")]
    [Authorize(Policy = "perm:notes.write")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddNoteRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new AddSubmissionNoteCommand(
            id, request.Text, request.AudioBase64, request.AudioMimeType, request.AudioSeconds), ct));

    /// <summary>Notes on one submission (adviser view).</summary>
    [HttpGet("submissions/{id:guid}/notes")]
    [Authorize(Policy = "perm:submissions.read")]
    public async Task<IActionResult> Notes(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetSubmissionNotesQuery(id), ct));

    // ─── Tool content management (editable titles / question texts / option labels) ───

    /// <summary>Editable question rows of one tool (texts + option labels).</summary>
    [HttpGet("tools/{code}/questions")]
    [Authorize(Policy = "perm:submissions.read")]
    public async Task<IActionResult> ToolQuestions(string code, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminToolQuestionsQuery(code), ct));

    /// <summary>Rename a question and/or its option labels.</summary>
    [HttpPut("tools/{code}/questions/{questionId}")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> UpdateToolQuestion(
        string code, string questionId, [FromBody] UpdateQuestionRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateAdminToolQuestionCommand(
            code, questionId, request.Text, request.Options), ct));

    /// <summary>Tool-level fields: title / description.</summary>
    [HttpPut("tools/{code}")]
    [Authorize(Policy = "perm:customers.manage")]
    public async Task<IActionResult> UpdateTool(string code, [FromBody] UpdateToolRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateAdminToolCommand(code, request.Title, request.Description), ct));

    /// <summary>Members of one company ("see members" modal).</summary>
    [HttpGet("companies/{id:guid}/members")]
    [Authorize(Policy = "perm:customers.read")]
    public async Task<IActionResult> CompanyMembers(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetAdminCompanyMembersQuery(id), ct));

    public sealed record UpdateQuestionRequest(string? Text, List<QuestionOptionRow>? Options);
    public sealed record UpdateToolRequest(string? Title, string? Description);

    public sealed record SetRolesRequest(List<string> Roles);
    public sealed record GrantRequest(Guid UserId, string ToolCode, int? Days);
    public sealed record PackageRequest(
        string Name, string? Description, int? PickCount, List<string> ToolCodes,
        int? DurationDays, long PriceToman, bool IsActive, int SortOrder);
    public sealed record AddNoteRequest(string? Text, string? AudioBase64, string? AudioMimeType, int? AudioSeconds);
    public sealed record SetUserActiveRequest(bool IsActive);
}

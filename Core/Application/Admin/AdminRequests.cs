using MediatR;

namespace Application.Admin;

// ════════════════════════════════════════════════════════════════════
// Queries (customer management)
// ════════════════════════════════════════════════════════════════════

/// <summary>KPI dashboard: totals, revenue, tool usage, recent signups.</summary>
public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardResponse>;

/// <summary>Paged customer list with search (name/email) and optional plan/active filter.</summary>
public sealed record GetAdminCustomersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Plan = null,
    bool? IsActive = null) : IRequest<AdminCustomerListResponse>;

/// <summary>One customer with payments and tool submissions (results).</summary>
public sealed record GetAdminCustomerQuery(Guid Id) : IRequest<AdminCustomerDetail>;

/// <summary>Paged payments list (optionally by user).</summary>
public sealed record GetAdminPaymentsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UserId = null,
    string? Status = null) : IRequest<AdminPaymentListResponse>;

/// <summary>Distinct plan options for the UI (from the domain enum).</summary>
public sealed record GetAdminPlansQuery : IRequest<List<string>>;

// ════════════════════════════════════════════════════════════════════
// Commands (customer management)
// ════════════════════════════════════════════════════════════════════

/// <summary>
///     Modifies a customer: display name, plan (+expiry), active flag.
///     Plan changes go through the domain methods (UpgradeToPro/DowngradeToFree).
/// </summary>
public sealed record UpdateAdminCustomerCommand(
    Guid Id,
    string? DisplayName,
    string? Plan,
    DateTime? PlanExpiresAt,
    bool? IsActive) : IRequest<AdminCustomerRow>;

/// <summary>Sets or removes admin roles on a customer (Admin/Support/Analyst).</summary>
public sealed record SetAdminRolesCommand(Guid Id, List<string> Roles) : IRequest<List<string>>;

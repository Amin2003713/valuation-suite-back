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

/// <summary>Paged, global submissions explorer (all users) with filters.</summary>
public sealed record GetAdminSubmissionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? ToolCode = null,
    string? Search = null,
    Guid? UserId = null) : IRequest<AdminSubmissionListResponse>;

/// <summary>Full tool catalog with usage stats (for the tools management page).</summary>
public sealed record GetAdminToolsQuery : IRequest<List<AdminToolRow>>;

/// <summary>Paged company list with member and revenue aggregates.</summary>
public sealed record GetAdminCompaniesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<AdminCompanyListResponse>;

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

/// <summary>Admin-initiated password reset (no old-password check — admin override).</summary>
public sealed record ResetAdminPasswordCommand(Guid Id, string NewPassword) : IRequest;

// ════════════════════════════════════════════════════════════════
// Access & notes management (session 5)
// ════════════════════════════════════════════════════════════

/// <summary>Visual analytics for the admin dashboard (chart-ready, no raw JSON).</summary>
public sealed record GetAdminAnalyticsQuery : IRequest<AdminAnalyticsResponse>;

/// <summary>All active grants (optionally by user) for the access management view.</summary>
public sealed record GetAdminGrantsQuery(Guid? UserId = null) : IRequest<List<AdminGrantRow>>;

/// <summary>Grants/extends a tool access for a user (admin comp or goodwill).</summary>
public sealed record GrantAdminAccessCommand(Guid UserId, string ToolCode, int? Days) : IRequest;

/// <summary>Revokes a grant.</summary>
public sealed record RevokeAdminAccessCommand(Guid GrantId) : IRequest;

/// <summary>Package CRUD (upsert by id; null id creates).</summary>
public sealed record UpsertAdminPackageCommand(
    Guid? Id, string Name, string? Description, int? PickCount,
    List<string> ToolCodes, int? DurationDays, long PriceToman, bool IsActive, int SortOrder) : IRequest<Guid>;

/// <summary>Adviser note on a submission — text and/or voice (base64 audio).</summary>
public sealed record AddSubmissionNoteCommand(
    Guid SubmissionId, string? Text, string? AudioBase64, string? AudioMimeType, int? AudioSeconds)
    : IRequest<AdminNoteRow>;

/// <summary>Notes for one submission (adviser view + customer delivery).</summary>
public sealed record GetSubmissionNotesQuery(Guid SubmissionId) : IRequest<List<AdminNoteRow>>;

/// <summary>Submissions that await an adviser note (paid advice, no note yet).</summary>
public sealed record GetAdviserQueueQuery : IRequest<List<AdminSubmissionRow>>;

/// <summary>Customer fetches (and marks seen) the adviser notes on their own submission.</summary>
public sealed record GetMySubmissionNotesQuery(Guid SubmissionId) : IRequest<List<AdminNoteRow>>;

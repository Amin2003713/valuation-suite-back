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

/// <summary>All packages (pricing manager) — read + delete.</summary>
public sealed record GetAdminPackagesQuery(int Page = 1, int PageSize = 20) : IRequest<AdminPackageListResponse>;

/// <summary>Deletes a package (soft: marks inactive).</summary>
public sealed record DeleteAdminPackageCommand(Guid Id) : IRequest<Guid>;

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

/// <summary>Customer follow-up question in the advice chat (advice must be paid).</summary>
public sealed record AddCustomerReplyCommand(Guid SubmissionId, string? Text) : IRequest<AdminNoteRow>;

/// <summary>Full advice thread for one submission (adviser view + customer delivery).</summary>
public sealed record GetSubmissionNotesQuery(Guid SubmissionId) : IRequest<List<AdminNoteRow>>;

/// <summary>
///     Submissions with a paid advice purchase — new ones (no messages yet) plus
///     open threads (customer replied after the last staff message). Answered
///     threads stay reachable via the customer/admin submission views.
/// </summary>
public sealed record GetAdviserQueueQuery : IRequest<List<AdminSubmissionRow>>;

/// <summary>
///     Customer fetches (and marks seen) the full advice thread on their own
///     submission, plus whether the advice was purchased and a reply is allowed.
/// </summary>
public sealed record GetMySubmissionNotesQuery(Guid SubmissionId) : IRequest<MyAdviceThreadResponse>;

/// <summary>
///     The customer's most recent advice thread for a tool (their latest submission
///     that has any note or a paid advice payment). Lets the chat survive reloads
///     for auto-computing calculator pages, which create fresh submissions.
/// </summary>
public sealed record GetMyToolAdviceThreadQuery(string ToolCode) : IRequest<MyAdviceThreadResponse?>;

// ════════════════════════════════════════════════════════════════
// Tool content management (editable questions/labels) + company members
// ════════════════════════════════════════════════════════════════

/// <summary>Editable question list of one tool (falls back to seed content).</summary>
public sealed record GetAdminToolQuestionsQuery(string ToolCode) : IRequest<List<AdminQuestionRow>>;

/// <summary>Edits one question's text and/or option labels.</summary>
public sealed record UpdateAdminToolQuestionCommand(
    string ToolCode, string QuestionId, string? Text, List<QuestionOptionRow>? Options) : IRequest<AdminQuestionRow>;

/// <summary>Updates tool-level editable fields (title; description optional).</summary>
public sealed record UpdateAdminToolCommand(string ToolCode, string? Title, string? Description) : IRequest<AdminToolRow>;

/// <summary>Admin pricing builder: set advanced/advice prices (Toman) for one tool.</summary>
public sealed record UpdateAdminToolPricesCommand(string ToolCode, long AdvancedPriceToman, long AdvicePriceToman) : IRequest<AdminToolPriceRow>;

/// <summary>Members of one company (admin "see members" modal).</summary>
public sealed record GetAdminCompanyMembersQuery(Guid CompanyId) : IRequest<List<AdminCompanyMemberRow>>;

// ════════════════════════════════════════════════════════════════
// User management (admin staff administration)
// ════════════════════════════════════════════════════════════════

/// <summary>Paged user list with role filter and search (name/email) — user manager page.</summary>
public sealed record GetAdminUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null) : IRequest<AdminUserListResponse>;

/// <summary>Creates a staff user (admin/support/analyst/adviser) with an initial password.</summary>
public sealed record CreateAdminUserCommand(
    string Name, string Email, string Password, List<string> Roles) : IRequest<AdminUserRow>;

/// <summary>Sets granular permission overrides — reserved for fine-grained access.</summary>
public sealed record SetAdminUserActiveCommand(Guid Id, bool IsActive) : IRequest;

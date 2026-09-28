using System;
using System.Collections.Generic;

namespace Application.Admin;

// ════════════════════════════════════════════════════════════════════
// Admin read models (response records only — no EF types leak out)
// ════════════════════════════════════════════════════════════════════

public sealed record AdminDashboardResponse(
    int TotalCustomers,
    int ProCustomers,
    int ActiveUsers,
    int NewCustomersThisMonth,
    int TotalSubmissions,
    int SubmissionsThisMonth,
    long TotalPaidAmount,
    long PaidPaymentsCount,
    long PendingPaymentsCount,
    long FailedPaymentsCount,
    List<ToolUsageRow> TopTools,
    List<RecentCustomerRow> RecentCustomers);

public sealed record ToolUsageRow(string ToolCode, long Uses, long DistinctUsers);

public sealed record RecentCustomerRow(
    Guid Id, string Name, string Email, string Plan, bool IsActive,
    DateTime CreatedAt, long PaidTotal, long Submissions);

public sealed record AdminCustomerListResponse(
    int Page, int PageSize, int TotalCount, List<AdminCustomerRow> Items);

public sealed record AdminCustomerRow(
    Guid Id,
    string Name,
    string Email,
    string Plan,
    bool IsPro,
    DateTime? PlanExpiresAt,
    bool IsActive,
    Guid? CompanyId,
    string? CompanyName,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    long Submissions,
    long PaidTotal,
    List<string> Roles);

public sealed record AdminCustomerDetail(
    AdminCustomerRow Customer,
    List<AdminPaymentRow> Payments,
    List<AdminSubmissionRow> Submissions);

public sealed record AdminPaymentRow(
    Guid Id, long Amount, string Gateway, string? RefId, string Status,
    string? Description, string? ErrorMessage, DateTime? PaidAt, DateTime CreatedAt);

public sealed record AdminSubmissionRow(
    Guid Id, Guid UserId, string UserEmail, string ToolCode, string? Name, double? OverallScore,
    JsonElement Input, JsonElement Result, DateTime CreatedAt,
    bool HasNotes, int UnreadNotes, bool AwaitingReply = true);

public sealed record AdminSubmissionListResponse(
    int Page, int PageSize, int TotalCount, List<AdminSubmissionRow> Items);

public sealed record AdminPaymentListResponse(
    int Page, int PageSize, int TotalCount, List<AdminPaymentRow> Items);

public sealed record AdminToolRow(
    string ToolCode, string Title, string Kind, long Uses, long DistinctUsers);

public sealed record AdminCompanyRow(
    Guid Id, string Name, string Slug, int Members, int ProMembers,
    long PaidTotal, DateTime CreatedAt);

public sealed record AdminCompanyListResponse(
    int Page, int PageSize, int TotalCount, List<AdminCompanyRow> Items);

// ─── Access & notes management (session 5) ──────────────────────────

public sealed record AdminGrantRow(
    Guid Id, Guid UserId, string UserEmail, string ToolCode,
    DateTime? ExpiresAt, DateTime CreatedAt);

public sealed record AdminPackageRow(
    Guid Id, string Name, string? Description, int? PickCount,
    List<string> ToolCodes, int? DurationDays, long PriceToman, bool IsActive, int SortOrder);

public sealed record AdminPackageListResponse(
    int Page, int PageSize, int TotalCount, List<AdminPackageRow> Items);

public sealed record AdminNoteRow(
    Guid Id, Guid SubmissionId, string? Text, string? AudioBase64, string? AudioMimeType,
    int? AudioSeconds, bool SeenByCustomer, DateTime CreatedAt,
    bool AuthorIsCustomer = false, bool SeenByAdviser = false);

/// <summary>Chart-ready analytics (visual reporting — no raw JSON).</summary>
public sealed record AdminAnalyticsResponse(
    List<ChartPoint> SubmissionsPerDay,
    List<ToolUsageRow> ToolUsage,
    List<PlanSlice> PlanDistribution,
    List<ScoreBucket> ScoreDistribution,
    List<RevenuePoint> RevenuePerDay,
    List<PaymentSlice> PaymentStatusSplit);

public sealed record ChartPoint(string Label, long Value);
public sealed record PlanSlice(string Plan, long Count);
public sealed record ScoreBucket(string Label, long Count);
public sealed record RevenuePoint(string Label, long Amount);
public sealed record PaymentSlice(string Status, long Count);

// ════════════════════════════════════════════════════════════════════
// Write-side requests
// ════════════════════════════════════════════════════════════════════

public sealed record UpdateCustomerRequest(
    string? DisplayName, string? Plan, DateTime? PlanExpiresAt, bool? IsActive);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed record CreateUserRequest(string Name, string Email, string Password, List<string> Roles);
public sealed record SetUserActiveRequest(bool IsActive);

// ─── User manager read models ───────────────────────────────────────

public sealed record AdminUserRow(
    Guid Id, string? Name, string Email, string Plan, bool IsActive,
    DateTime CreatedAt, DateTime? LastLoginAt, List<string> Roles,
    List<string> Perms, long Submissions);

public sealed record AdminUserListResponse(
    int Page, int PageSize, int TotalCount, List<AdminUserRow> Items);

/// <summary>Customer-side advice thread: messages + entitlement to reply.</summary>
public sealed record MyAdviceThreadResponse(
    Guid SubmissionId,
    List<AdminNoteRow> Notes,
    bool AdvicePurchased,
    bool CanReply);

// ─── Tool content management + company members ──────────────────────

/// <summary>One editable option row of a question.</summary>
public sealed record QuestionOptionRow(string Value, string Label);

/// <summary>Editable question row (text + option labels) for the admin tools editor.</summary>
public sealed record AdminQuestionRow(
    string Id, string? SectionKey, string? SectionTitle, string Text, List<QuestionOptionRow> Options);

/// <summary>Member of a company (admin "see members" modal).</summary>
public sealed record AdminCompanyMemberRow(
    Guid Id, string? Name, string Email, string Plan, bool IsActive,
    long Submissions, long PaidTotal, DateTime CreatedAt, DateTime? LastLoginAt);

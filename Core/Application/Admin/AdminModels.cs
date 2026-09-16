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
    Guid Id, string ToolCode, string? Name, double? OverallScore,
    JsonElement Input, JsonElement Result, DateTime CreatedAt);

public sealed record AdminPaymentListResponse(
    int Page, int PageSize, int TotalCount, List<AdminPaymentRow> Items);

// ════════════════════════════════════════════════════════════════════
// Write-side requests
// ════════════════════════════════════════════════════════════════════

public sealed record UpdateCustomerRequest(
    string? DisplayName, string? Plan, DateTime? PlanExpiresAt, bool? IsActive);

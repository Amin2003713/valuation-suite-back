using Application.Admin;
using Domain.Payments;
using Domain.Tools;
using Domain.Users;

namespace Application.Interfaces;

/// <summary>
///     Read-side contract for the admin area. Implemented in Persistence; the admin
///     handlers depend on this abstraction only (Uncle Bob: high-level policy does
///     not depend on EF details).
/// </summary>
public interface IAdminQueryRepository
{
    // ── Dashboard ──
    Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken ct = default);

    // ── Customers ──
    Task<(List<AdminCustomerRow> Items, int TotalCount)> GetCustomersAsync(
        int page, int pageSize, string? search, string? plan, bool? isActive, CancellationToken ct = default);

    Task<AdminCustomerRow?> GetCustomerAsync(Guid id, CancellationToken ct = default);

    // ── Payments ──
    Task<(List<AdminPaymentRow> Items, int TotalCount)> GetPaymentsAsync(
        int page, int pageSize, Guid? userId, string? status, CancellationToken ct = default);

    // ── Submissions (tool results) ──
    Task<List<AdminSubmissionRow>> GetSubmissionsAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>Paged, global submissions explorer (all users) with optional filters.</summary>
    Task<(List<AdminSubmissionRow> Items, int TotalCount)> SearchSubmissionsAsync(
        int page, int pageSize, string? toolCode, string? search, Guid? userId, CancellationToken ct = default);

    // ── Reference data ──
    Task<List<AdminToolRow>> GetToolsAsync(CancellationToken ct = default);
    Task<List<ToolUsageRow>> GetTopToolsAsync(int take, CancellationToken ct = default);

    // ── Companies ──
    Task<(List<AdminCompanyRow> Items, int TotalCount)> GetCompaniesAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    // ── Session 5: analytics, grants, adviser queue ──

    Task<AdminAnalyticsResponse> GetAnalyticsAsync(CancellationToken ct = default);
    Task<List<AdminGrantRow>> GetGrantsAsync(Guid? userId, CancellationToken ct = default);
    Task<List<AdminSubmissionRow>> GetAdviserQueueAsync(CancellationToken ct = default);

    // ── User manager ──
    Task<(List<AdminUserRow> Items, int TotalCount)> GetUsersAsync(
        int page, int pageSize, string? search, string? role, CancellationToken ct = default);
}

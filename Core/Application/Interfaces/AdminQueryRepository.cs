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

    // ── Reference data ──
    Task<List<ToolUsageRow>> GetTopToolsAsync(int take, CancellationToken ct = default);
}

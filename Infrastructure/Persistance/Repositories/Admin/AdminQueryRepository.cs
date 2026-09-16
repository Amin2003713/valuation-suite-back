using System.Text.Json;
using Application.Admin;
using Application.Interfaces;
using Domain.Payments;
using Domain.Tools;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories.Common;

namespace Persistence.Repositories.Admin;

/// <summary>
///     EF Core read model for the admin area. All aggregates are queried through
///     projection queries (no tracked entities, no lazy loading).
/// </summary>
public class AdminQueryRepository(ReadOnlyDbContext dbContext) : IAdminQueryRepository
{
    private ReadOnlyDbContext Db { get; } = dbContext;

    // ────────────────────────────────────────────────────────
    // Dashboard
    // ────────────────────────────────────────────────────────

    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken ct = default)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var users = Db.Users.AsNoTracking();
        var payments = Db.Set<Payment>().AsNoTracking();
        var submissions = Db.Set<ToolSubmission>().AsNoTracking();

        var totalCustomers = await users.LongCountAsync(ct);
        var proCustomers = await users.LongCountAsync(u => u.Plan == Plan.Pro, ct);
        var activeUsers = await users.LongCountAsync(u => u.IsActive, ct);
        var newThisMonth = await users.LongCountAsync(u => u.CreatedAt >= monthStart, ct);

        var totalSubmissions = await submissions.LongCountAsync(ct);
        var subsThisMonth = await submissions.LongCountAsync(s => s.CreatedAt >= monthStart, ct);

        var totalPaid = await payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => (long?)p.Amount, ct) ?? 0;

        var paidCount = await payments.LongCountAsync(p => p.Status == PaymentStatus.Paid, ct);
        var pendingCount = await payments.LongCountAsync(p => p.Status == PaymentStatus.Pending, ct);
        var failedCount = await payments.LongCountAsync(p => p.Status == PaymentStatus.Failed, ct);

        var topTools = await GetTopToolsAsync(5, ct);
        var recent = await GetRecentCustomersAsync(8, ct);

        return new AdminDashboardResponse(
            (int)totalCustomers, (int)proCustomers, (int)activeUsers, (int)newThisMonth,
            (int)totalSubmissions, (int)subsThisMonth,
            totalPaid, paidCount, pendingCount, failedCount,
            topTools, recent);
    }

    // ────────────────────────────────────────────────────────
    // Customers
    // ────────────────────────────────────────────────────────

    public async Task<(List<AdminCustomerRow> Items, int TotalCount)> GetCustomersAsync(
        int page, int pageSize, string? search, string? plan, bool? isActive, CancellationToken ct = default)
    {
        var query = Db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.DisplayName.Contains(term) ||
                (u.Email ?? "").Contains(term) ||
                (u.UserName ?? "").Contains(term));
        }

        if (Enum.TryParse<Plan>(plan, ignoreCase: true, out var p))
            query = query.Where(u => u.Plan == p);

        if (isActive is { } active)
            query = query.Where(u => u.IsActive == active);

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminCustomerRow(
                u.Id,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.Plan.ToString(),
                u.Plan == Plan.Pro && (u.PlanExpiresAt == null || u.PlanExpiresAt > DateTime.UtcNow),
                u.PlanExpiresAt,
                u.IsActive,
                u.CompanyId,
                u.Company != null ? u.Company.Name : null,
                u.CreatedAt,
                u.LastLoginAt,
                Db.Set<ToolSubmission>().Count(s => s.UserId == u.Id),
                Db.Set<Payment>()
                    .Where(pay => pay.UserId == u.Id && pay.Status == PaymentStatus.Paid)
                    .Sum(pay => (long?)pay.Amount) ?? 0,
                new List<string>()))
            .ToListAsync(ct);

        return (items, (int)total);
    }

    public async Task<AdminCustomerRow?> GetCustomerAsync(Guid id, CancellationToken ct = default)
    {
        var row = await Db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new AdminCustomerRow(
                u.Id,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.Plan.ToString(),
                u.Plan == Plan.Pro && (u.PlanExpiresAt == null || u.PlanExpiresAt > DateTime.UtcNow),
                u.PlanExpiresAt,
                u.IsActive,
                u.CompanyId,
                u.Company != null ? u.Company.Name : null,
                u.CreatedAt, u.LastLoginAt,
                Db.Set<ToolSubmission>().Count(s => s.UserId == u.Id),
                Db.Set<Payment>()
                    .Where(pay => pay.UserId == u.Id && pay.Status == PaymentStatus.Paid)
                    .Sum(pay => (long?)pay.Amount) ?? 0,
                new List<string>()))
            .FirstOrDefaultAsync(ct);

        return row;
    }

    // ────────────────────────────────────────────────────────
    // Payments
    // ─────────────────────────────────────────────────══════─

    public async Task<(List<AdminPaymentRow> Items, int TotalCount)> GetPaymentsAsync(
        int page, int pageSize, Guid? userId, string? status, CancellationToken ct = default)
    {
        var query = Db.Set<Payment>().AsNoTracking();

        if (userId is { } uid)
            query = query.Where(p => p.UserId == uid);

        if (Enum.TryParse<PaymentStatus>(status, ignoreCase: true, out var st))
            query = query.Where(p => p.Status == st);

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPaymentRow(
                p.Id, p.Amount, p.Gateway, p.RefId, p.Status.ToString(),
                p.Description, p.ErrorMessage, p.PaidAt, p.CreatedAt))
            .ToListAsync(ct);

        return (items, (int)total);
    }

    // ────────────────────────────────────────────────────────
    // Submissions
    // ────────────────────────────────────────────────────────

    public async Task<List<AdminSubmissionRow>> GetSubmissionsAsync(Guid userId, int take, CancellationToken ct = default)
    {
        return await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .Select(s => new AdminSubmissionRow(
                s.Id, s.ToolCode, s.Name, s.OverallScore,
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.InputJson) ? "{}" : s.InputJson),
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.ResultJson) ? "{}" : s.ResultJson),
                s.CreatedAt))
            .ToListAsync(ct);
    }

    // ────────────────────────────────────────────────────────
    // Reference data
    // ────────────────────────────────────────────────────────

    public async Task<List<ToolUsageRow>> GetTopToolsAsync(int take, CancellationToken ct = default)
    {
        return await Db.Set<ToolSubmission>().AsNoTracking()
            .GroupBy(s => s.ToolCode)
            .Select(g => new ToolUsageRow(g.Key, g.LongCount(), g.Select(x => x.UserId).Distinct().LongCount()))
            .OrderByDescending(t => t.Uses)
            .Take(take)
            .ToListAsync(ct);
    }

    // ────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────

    private async Task<List<RecentCustomerRow>> GetRecentCustomersAsync(int take, CancellationToken ct)
    {
        return await Db.Users.AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(take)
            .Select(u => new RecentCustomerRow(
                u.Id, u.DisplayName, u.Email ?? string.Empty, u.Plan.ToString(), u.IsActive,
                u.CreatedAt,
                Db.Set<Payment>()
                    .Where(pay => pay.UserId == u.Id && pay.Status == PaymentStatus.Paid)
                    .Sum(pay => (long?)pay.Amount) ?? 0,
                Db.Set<ToolSubmission>().Count(s => s.UserId == u.Id)))
            .ToListAsync(ct);
    }
}

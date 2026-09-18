using System.Text.Json;
using Application.Admin;
using Application.Interfaces;
using Domain.Payments;
using Domain.Tools;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Repositories.Common;
using UserToolAccess = Domain.Users.UserToolAccess;

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
    // Analytics (visual reporting — chart-ready aggregates)
    // ────────────────────────────────────────────────────────

    public async Task<AdminAnalyticsResponse> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-29);
        var dayStart = new DateTime(since.Year, since.Month, since.Day, 0, 0, 0, DateTimeKind.Utc);

        // Submissions per day — grouped in SQL, day buckets materialized in memory.
        var subStamps = await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => s.CreatedAt >= dayStart)
            .Select(s => s.CreatedAt)
            .ToListAsync(ct);
        var submissionsPerDay = subStamps
            .GroupBy(t => t.ToString("MM/dd"))
            .OrderBy(g => g.Key)
            .Select(g => new ChartPoint(g.Key, g.Count()))
            .ToList();

        // Tool usage — top 10.
        var toolUsage = (await GetTopToolsAsync(10, ct))
            .Select(t => new ToolUsageRow(t.ToolCode, t.Uses, t.DistinctUsers))
            .ToList();

        // Plan distribution.
        var planRows = await Db.Users.AsNoTracking()
            .GroupBy(u => u.Plan)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var planDistribution = planRows
            .Select(r => new PlanSlice(r.Plan.ToString(), r.Count))
            .ToList();

        // Score distribution across all submissions (buckets of 10).
        var scores = await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => s.OverallScore != null)
            .Select(s => s.OverallScore!.Value)
            .ToListAsync(ct);
        var scoreDistribution = scores
            .GroupBy(s => Math.Clamp((int)(s / 10), 0, 9) * 10)
            .OrderBy(g => g.Key)
            .Select(g => new ScoreBucket($"{g.Key}-{g.Key + 9}", g.Count()))
            .ToList();

        // Revenue per day (paid payments only).
        var payStamps = await Db.Set<Payment>().AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Paid && p.CreatedAt >= dayStart)
            .Select(p => new { p.CreatedAt, p.Amount })
            .ToListAsync(ct);
        var revenuePerDay = payStamps
            .GroupBy(p => p.CreatedAt.ToString("MM/dd"))
            .OrderBy(g => g.Key)
            .Select(g => new RevenuePoint(g.Key, g.Sum(x => x.Amount)))
            .ToList();

        // Payment status split.
        var statusRows = await Db.Set<Payment>().AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var paymentSplit = statusRows
            .Select(r => new PaymentSlice(r.Status.ToString(), r.Count))
            .ToList();

        return new AdminAnalyticsResponse(
            submissionsPerDay, toolUsage, planDistribution, scoreDistribution,
            revenuePerDay, paymentSplit);
    }

    // ────────────────────────────────────────────────────────
    // Access grants + adviser queue
    // ────────────────────────────────────────────────────────

    public async Task<List<AdminGrantRow>> GetGrantsAsync(Guid? userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var query = Db.Set<UserToolAccess>().AsNoTracking()
            .Where(g => g.ToolCode != "__PICK_CREDIT__" && (g.ExpiresAt == null || g.ExpiresAt > now));

        if (userId is { } uid)
            query = query.Where(g => g.UserId == uid);

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Take(500)
            .Select(g => new AdminGrantRow(
                g.Id, g.UserId,
                Db.Users.Where(u => u.Id == g.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                g.ToolCode, g.ExpiresAt, g.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<List<AdminSubmissionRow>> GetAdviserQueueAsync(CancellationToken ct = default)
    {
        var paidAdviceIds = await Db.Set<Payment>().AsNoTracking()
            .Where(p => p.Kind == PaymentKind.Advice && p.Status == PaymentStatus.Paid && p.SubmissionId != null)
            .Select(p => p.SubmissionId!.Value)
            .ToListAsync(ct);

        if (paidAdviceIds.Count == 0)
            return [];

        var notedIds = await Db.Set<SubmissionNote>().AsNoTracking()
            .Select(n => n.SubmissionId)
            .Distinct()
            .ToListAsync(ct);

        var pending = paidAdviceIds.Except(notedIds).ToList();

        return await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => pending.Contains(s.Id))
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .Select(s => new AdminSubmissionRow(
                s.Id, s.UserId,
                Db.Users.Where(u => u.Id == s.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                s.ToolCode, s.Name, s.OverallScore,
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.InputJson) ? "{}" : s.InputJson),
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.ResultJson) ? "{}" : s.ResultJson),
                s.CreatedAt, false, 0))
            .ToListAsync(ct);
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
                s.Id, s.UserId,
                Db.Users.Where(u => u.Id == s.UserId).Select(u => u.Email ?? "").FirstOrDefault(),
                s.ToolCode, s.Name, s.OverallScore,
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.InputJson) ? "{}" : s.InputJson),
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.ResultJson) ? "{}" : s.ResultJson),
                s.CreatedAt,
                Db.Set<SubmissionNote>().Any(n => n.SubmissionId == s.Id),
                Db.Set<SubmissionNote>().Count(n => n.SubmissionId == s.Id && !n.SeenByCustomer)))
            .ToListAsync(ct);
    }

    public async Task<(List<AdminSubmissionRow> Items, int TotalCount)> SearchSubmissionsAsync(
        int page, int pageSize, string? toolCode, string? search, Guid? userId, CancellationToken ct = default)
    {
        var query = Db.Set<ToolSubmission>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(toolCode))
            query = query.Where(s => s.ToolCode == toolCode);

        if (userId is { } uid)
            query = query.Where(s => s.UserId == uid);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                (s.Name ?? "").Contains(term) ||
                s.ToolCode.Contains(term) ||
                Db.Users.Any(u => u.Id == s.UserId && (u.Email ?? "").Contains(term)));
        }

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new AdminSubmissionRow(
                s.Id, s.UserId,
                Db.Users.Where(u => u.Id == s.UserId).Select(u => u.Email ?? "").FirstOrDefault(),
                s.ToolCode, s.Name, s.OverallScore,
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.InputJson) ? "{}" : s.InputJson),
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.ResultJson) ? "{}" : s.ResultJson),
                s.CreatedAt,
                Db.Set<SubmissionNote>().Any(n => n.SubmissionId == s.Id),
                Db.Set<SubmissionNote>().Count(n => n.SubmissionId == s.Id && !n.SeenByCustomer)))
            .ToListAsync(ct);

        return (items, (int)total);
    }

    public async Task<List<AdminToolRow>> GetToolsAsync(CancellationToken ct = default)
    {
        var forms = await Db.Set<ToolForm>().AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .Select(t => new { t.ToolCode, t.Title, t.Kind })
            .ToListAsync(ct);

        var uses = await Db.Set<ToolSubmission>().AsNoTracking()
            .GroupBy(s => s.ToolCode)
            .Select(g => new { ToolCode = g.Key, Uses = g.Count() })
            .ToListAsync(ct);
        var usesMap = uses.ToDictionary(u => u.ToolCode, u => (long)u.Uses);

        var pairs = await Db.Set<ToolSubmission>().AsNoTracking()
            .Select(s => new { s.ToolCode, s.UserId })
            .Distinct()
            .ToListAsync(ct);
        var usersMap = pairs.GroupBy(p => p.ToolCode)
            .ToDictionary(g => g.Key, g => (long)g.Count());

        return forms
            .Select(f => new AdminToolRow(
                f.ToolCode, f.Title, f.Kind.ToString(),
                usesMap.GetValueOrDefault(f.ToolCode),
                usersMap.GetValueOrDefault(f.ToolCode)))
            .ToList();
    }

    public async Task<(List<AdminCompanyRow> Items, int TotalCount)> GetCompaniesAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = Db.Set<Company>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Slug.Contains(term));
        }

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCompanyRow(
                c.Id, c.Name, c.Slug,
                Db.Users.Count(u => u.CompanyId == c.Id),
                Db.Users.Count(u => u.CompanyId == c.Id && u.Plan == Plan.Pro),
                Db.Set<Payment>()
                    .Where(pay =>
                        pay.Status == PaymentStatus.Paid &&
                        Db.Users.Any(u => u.Id == pay.UserId && u.CompanyId == c.Id))
                    .Sum(pay => (long?)pay.Amount) ?? 0,
                c.CreatedAt))
            .ToListAsync(ct);

        return (items, (int)total);
    }

    // ────────────────────────────────────────────────────────
    // Reference data
    // ────────────────────────────────────────────────────────

    public async Task<List<ToolUsageRow>> GetTopToolsAsync(int take, CancellationToken ct = default)
    {
        // EF can translate simple GroupBy+Count aggregates, but not nested
        // Distinct().Count() subqueries inside the group projection — so count
        // distinct users from a separate flat projection.
        var uses = await Db.Set<ToolSubmission>().AsNoTracking()
            .GroupBy(s => s.ToolCode)
            .Select(g => new { ToolCode = g.Key, Uses = g.Count() })
            .ToListAsync(ct);

        var pairs = await Db.Set<ToolSubmission>().AsNoTracking()
            .Select(s => new { s.ToolCode, s.UserId })
            .Distinct()
            .ToListAsync(ct);

        var usersPerTool = pairs
            .GroupBy(p => p.ToolCode)
            .ToDictionary(g => g.Key, g => (long)g.Count());

        return uses
            .OrderByDescending(u => u.Uses)
            .Take(take)
            .Select(u => new ToolUsageRow(u.ToolCode, u.Uses, usersPerTool.GetValueOrDefault(u.ToolCode)))
            .ToList();
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

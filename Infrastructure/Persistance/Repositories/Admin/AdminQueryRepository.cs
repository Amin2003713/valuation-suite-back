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

        /* One grouped query per table instead of N separate COUNT/SCAN round-trips:
         * users → 4 counters, payments → status split + paid total,
         * submissions → total + this-month. Falls back to sequential queries
         * is avoided; each group below translates to a single SQL aggregate. */
        var userStats = await users
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pro = g.Count(u => u.Plan == Plan.Pro),
                Active = g.Count(u => u.IsActive),
                NewThisMonth = g.Count(u => u.CreatedAt >= monthStart),
            })
            .FirstOrDefaultAsync(ct);

        var paymentStats = await payments
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Paid = g.Sum(x => (long?)x.Amount) })
            .ToListAsync(ct);

        var subStats = await submissions
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                ThisMonth = g.Count(s => s.CreatedAt >= monthStart),
            })
            .FirstOrDefaultAsync(ct);

        var topTools = await GetTopToolsAsync(5, ct);
        var recent = await GetRecentCustomersAsync(8, ct);

        long paidCount = 0, pendingCount = 0, failedCount = 0, totalPaid = 0;
        foreach (var row in paymentStats)
        {
            if (row.Status == PaymentStatus.Paid)   { paidCount = row.Count;   totalPaid = row.Paid ?? 0; }
            else if (row.Status == PaymentStatus.Pending) pendingCount = row.Count;
            else if (row.Status == PaymentStatus.Failed)  failedCount = row.Count;
        }

        return new AdminDashboardResponse(
            userStats?.Total ?? 0, userStats?.Pro ?? 0, userStats?.Active ?? 0, userStats?.NewThisMonth ?? 0,
            subStats?.Total ?? 0, subStats?.ThisMonth ?? 0,
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

        var baseRows = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id, u.DisplayName, u.Email, u.Plan, u.PlanExpiresAt, u.IsActive,
                u.CompanyId, CompanyName = u.Company != null ? u.Company.Name : null,
                u.CreatedAt, u.LastLoginAt,
            })
            .ToListAsync(ct);

        /* Aggregates for the page's users in ONE grouped query per table,
         * instead of a correlated COUNT/SUM subquery per row (N+1 → 2). */
        var ids = baseRows.Select(u => u.Id).ToList();
        var subsByUser = await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => ids.Contains(s.UserId))
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => (long)x.Count, ct);

        var paidByUser = await Db.Set<Payment>().AsNoTracking()
            .Where(p => ids.Contains(p.UserId) && p.Status == PaymentStatus.Paid)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Amount = g.Sum(x => (long?)x.Amount) })
            .ToDictionaryAsync(x => x.UserId, x => x.Amount ?? 0, ct);

        var items = baseRows
            .Select(u => new AdminCustomerRow(
                u.Id,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.Plan.ToString(),
                u.Plan == Plan.Pro && (u.PlanExpiresAt == null || u.PlanExpiresAt > DateTime.UtcNow),
                u.PlanExpiresAt,
                u.IsActive,
                u.CompanyId,
                u.CompanyName,
                u.CreatedAt,
                u.LastLoginAt,
                subsByUser.GetValueOrDefault(u.Id),
                paidByUser.GetValueOrDefault(u.Id),
                new List<string>()))
            .ToList();

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

        // Submissions per day — grouped fully in SQL (no raw timestamps pulled).
        var subStamps = await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => s.CreatedAt >= dayStart)
            .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month, s.CreatedAt.Day })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Count() })
            .ToListAsync(ct);
        var submissionsPerDay = subStamps
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
            .Select(x => new ChartPoint($"{x.Month:D2}/{x.Day:D2}", x.Count))
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

        // Score distribution across all submissions (buckets of 10, grouped in SQL).
        var scoreRows = await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => s.OverallScore != null)
            .GroupBy(s => (int)(s.OverallScore!.Value / 10))
            .Select(g => new { Bucket = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var scoreDistribution = scoreRows
            .GroupBy(x => Math.Clamp(x.Bucket, 0, 9))
            .OrderBy(g => g.Key)
            .Select(g => new ScoreBucket($"{g.Key * 10}-{g.Key * 10 + 9}", (long)g.Sum(x => x.Count)))
            .ToList();

        // Revenue per day (paid payments only) — grouped fully in SQL.
        var payStamps = await Db.Set<Payment>().AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Paid && p.CreatedAt >= dayStart)
            .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month, p.CreatedAt.Day })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Amount = g.Sum(x => (long?)x.Amount) })
            .ToListAsync(ct);
        var revenuePerDay = payStamps
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
            .Select(x => new RevenuePoint($"{x.Month:D2}/{x.Day:D2}", x.Amount ?? 0))
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
        // Advice threads stay visible: new purchases (no messages yet) and open
        // conversations (customer sent the latest message) form the working queue;
        // answered threads (staff replied last) are still listed so nothing ever
        // "disappears" after answering — the UI greys them out under "answered".
        return await Db.Set<ToolSubmission>().AsNoTracking()
            .Where(s => Db.Set<Payment>().Any(p =>
                p.SubmissionId == s.Id && p.Kind == PaymentKind.Advice && p.Status == PaymentStatus.Paid))
            .OrderByDescending(s => s.CreatedAt)
            .Take(100)
            .Select(s => new AdminSubmissionRow(
                s.Id, s.UserId,
                Db.Users.Where(u => u.Id == s.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                s.ToolCode, s.Name, s.OverallScore,
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.InputJson) ? "{}" : s.InputJson),
                JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(s.ResultJson) ? "{}" : s.ResultJson),
                s.CreatedAt,
                Db.Set<SubmissionNote>().Any(n => n.SubmissionId == s.Id),
                // Unread badge for the adviser: unseen customer replies.
                Db.Set<SubmissionNote>().Count(n =>
                    n.SubmissionId == s.Id && n.AuthorIsCustomer && !n.SeenByAdviser),
                // Awaiting reply: no messages yet, or the customer sent the latest one.
                !Db.Set<SubmissionNote>().Any(n => n.SubmissionId == s.Id)
                    || Db.Set<SubmissionNote>()
                        .Where(n => n.SubmissionId == s.Id)
                        .OrderByDescending(n => n.CreatedAt)
                        .Select(n => (bool?)n.AuthorIsCustomer)
                        .FirstOrDefault() == true))
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

        var baseRows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new { c.Id, c.Name, c.Slug, c.CreatedAt })
            .ToListAsync(ct);

        /* Page-scoped aggregates: one grouped query per table instead of
         * nested correlated subqueries (members, pro members, revenue). */
        var ids = baseRows.Select(c => c.Id).ToList();

        var memberRows = await Db.Users.AsNoTracking()
            .Where(u => u.CompanyId != null && ids.Contains(u.CompanyId.Value))
            .Select(u => new { u.CompanyId, u.Plan })
            .ToListAsync(ct);

        var paidRows = await (
            from pay in Db.Set<Payment>().AsNoTracking()
            join u in Db.Users.AsNoTracking() on pay.UserId equals u.Id
            where pay.Status == PaymentStatus.Paid && u.CompanyId != null && ids.Contains(u.CompanyId.Value)
            group pay by u.CompanyId into g
            select new { CompanyId = g.Key, Amount = g.Sum(x => (long?)x.Amount) })
            .ToDictionaryAsync(x => x.CompanyId!.Value, x => x.Amount ?? 0, ct);

        var items = baseRows
            .Select(c =>
            {
                var members = memberRows.Where(m => m.CompanyId == c.Id).ToList();
                return new AdminCompanyRow(
                    c.Id, c.Name, c.Slug,
                    members.Count,
                    members.Count(m => m.Plan == Plan.Pro),
                    paidRows.GetValueOrDefault(c.Id),
                    c.CreatedAt);
            })
            .ToList();

        return (items, (int)total);
    }

    // ────────────────────────────────────────────────────────
    // Reference data
    // ────────────────────────────────────────────────────────

    public async Task<List<ToolUsageRow>> GetTopToolsAsync(int take, CancellationToken ct = default)
    {
        /* Single grouped query for uses; distinct users computed from a grouped
         * SQL projection (GROUP BY toolCode, userId) rather than pulling every
         * distinct pair into memory. */
        var uses = await Db.Set<ToolSubmission>().AsNoTracking()
            .GroupBy(s => s.ToolCode)
            .Select(g => new { ToolCode = g.Key, Uses = g.Count() })
            .ToListAsync(ct);

        var distinctPairs = await Db.Set<ToolSubmission>().AsNoTracking()
            .GroupBy(s => new { s.ToolCode, s.UserId })
            .Select(g => new { g.Key.ToolCode })
            .ToListAsync(ct);

        var usersPerTool = distinctPairs
            .GroupBy(p => p.ToolCode)
            .ToDictionary(g => g.Key, g => (long)g.Count());

        return uses
            .OrderByDescending(u => u.Uses)
            .Take(take)
            .Select(u => new ToolUsageRow(u.ToolCode, u.Uses, usersPerTool.GetValueOrDefault(u.ToolCode)))
            .ToList();
    }

    // ────────────────────────────────────────────────────────
    // User manager
    // ────────────────────────────────────────────────────────

    public async Task<(List<AdminUserRow> Items, int TotalCount)> GetUsersAsync(
        int page, int pageSize, string? search, string? role, CancellationToken ct = default)
    {
        var query = Db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.DisplayName.Contains(term) ||
                (u.Email ?? "").Contains(term));
        }

        // Role filter via the Identity join table (UserRoles → Roles).
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u =>
                Db.UserRoles.Any(ur => ur.UserId == u.Id && Db.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserRow(
                u.Id,
                u.DisplayName,
                u.Email ?? string.Empty,
                u.Plan.ToString(),
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt,
                Db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(Db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToList(),
                new List<string>(),
                Db.Set<ToolSubmission>().LongCount(s => s.UserId == u.Id)))
            .ToListAsync(ct);

        return (items, (int)total);
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

using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Payments;
using Domain.Tools;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Tools;

/// <summary>Public catalog row for the pricing page. Prices in Toman.</summary>
public record ToolPriceRow(
    string ToolCode, string Route, string Title, string? Description, string Kind,
    long AdvancedPriceToman, long AdvicePriceToman, bool AdvancedUnlocked, bool FreeAdvanced);

/// <summary>A purchasable bundle shown on the pricing page.</summary>
public record PackageRow(
    Guid Id, string Name, string? Description, int? PickCount,
    List<string> ToolCodes, int? DurationDays, long PriceToman);

/// <summary>What the current user owns, for the "my accesses" panel.</summary>
public record MyAccessRow(string ToolCode, bool Pro, bool Granted, DateTime? ExpiresAt);

public record PricingCatalogResponse(List<ToolPriceRow> Tools, List<PackageRow> Packages, List<MyAccessRow> MyAccesses);

/// <summary>
///     Single source of truth for "which advanced sections may this user see":
///     Pro subscription unlocks everything; otherwise a valid per-tool grant is required.
/// </summary>
public interface IEntitlementService
{
    Task<bool> IsProAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CanViewAdvancedAsync(Guid userId, string toolCode, CancellationToken ct = default);
    Task<bool> HasPaidAdviceAsync(Guid userId, Guid submissionId, CancellationToken ct = default);
    Task GrantAsync(Guid userId, string toolCode, DateTime? expiresAt, Guid? paymentId, Guid? grantedBy, CancellationToken ct = default);
    Task GrantPickCreditsAsync(Guid userId, int credits, Guid paymentId, CancellationToken ct = default);
    Task<int> RemainingPickCreditsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ConsumePickCreditAsync(Guid userId, string toolCode, CancellationToken ct = default);
}

public sealed class EntitlementService(
    IQueryRepository<ApplicationUser> users,
    ICommandRepository<UserToolAccess> grantCommands,
    IQueryRepository<UserToolAccess> grants,
    IQueryRepository<ToolSubmission> submissions,
    IQueryRepository<Payment> payments) : IEntitlementService
{
    public async Task<bool> IsProAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return false;

        return await users.TableNoTracking
            .Where(u => u.Id == userId)
            .Select(u => (bool?)u.IsPro)
            .FirstOrDefaultAsync(ct) == true;
    }

    public async Task<bool> CanViewAdvancedAsync(Guid userId, string toolCode, CancellationToken ct = default)
    {
        if (await IsProAsync(userId, ct))
            return true;

        var now = DateTime.UtcNow;
        return await grants.TableNoTracking.AnyAsync(
            g => g.UserId == userId && g.ToolCode == toolCode &&
                 (g.ExpiresAt == null || g.ExpiresAt > now), ct);
    }

    public async Task<bool> HasPaidAdviceAsync(Guid userId, Guid submissionId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || submissionId == Guid.Empty)
            return false;

        return await payments.TableNoTracking.AnyAsync(
            p => p.UserId == userId && p.SubmissionId == submissionId &&
                 p.Kind == PaymentKind.Advice && p.Status == PaymentStatus.Paid, ct);
    }

    public async Task GrantAsync(Guid userId, string toolCode, DateTime? expiresAt, Guid? paymentId, Guid? grantedBy, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Extend instead of duplicating when a live perpetual grant already exists.
        var existing = await grantCommands.Table
            .FirstOrDefaultAsync(g => g.UserId == userId && g.ToolCode == toolCode && g.ExpiresAt == null, ct);

        if (existing is not null)
            return;

        grantCommands.Add(UserToolAccess.Grant(userId, toolCode, expiresAt, paymentId, grantedBy), saveNow: false);
    }

    public Task GrantPickCreditsAsync(Guid userId, int credits, Guid paymentId, CancellationToken ct = default)
    {
        grantCommands.Add(
            UserToolAccess.Grant(userId, ToolCodes.PickCredit, DateTime.UtcNow.AddDays(365 * 5), paymentId, null),
            saveNow: false);
        // Each credit row carries count in its own record; simplest faithful model:
        // one row per credit.
        for (var i = 1; i < credits; i++)
            grantCommands.Add(
                UserToolAccess.Grant(userId, ToolCodes.PickCredit, DateTime.UtcNow.AddDays(365 * 5), paymentId, null),
                saveNow: false);
        return Task.CompletedTask;
    }

    public async Task<int> RemainingPickCreditsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await grants.TableNoTracking
            .CountAsync(g => g.UserId == userId && g.ToolCode == ToolCodes.PickCredit &&
                             (g.ExpiresAt == null || g.ExpiresAt > now), ct);
    }

    public async Task<bool> ConsumePickCreditAsync(Guid userId, string toolCode, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var credit = await grantCommands.Table
            .Where(g => g.UserId == userId && g.ToolCode == ToolCodes.PickCredit &&
                        (g.ExpiresAt == null || g.ExpiresAt > now))
            .OrderBy(g => g.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (credit is null)
            return false;

        grantCommands.Delete(credit, saveNow: false);
        grantCommands.Add(UserToolAccess.Grant(userId, toolCode, null, credit.PaymentId, null), saveNow: false);
        return true;
    }

    /// <summary>Sentinel tool code for pick-N credits stored in UserToolAccess rows.</summary>
    public static class ToolCodes
    {
        public const string PickCredit = "__PICK_CREDIT__";
    }
}

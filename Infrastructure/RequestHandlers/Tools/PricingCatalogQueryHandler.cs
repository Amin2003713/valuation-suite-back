using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Tools;
using Domain.Tools;
using Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ICurrentUserAccessor = RequestHandlers.Tools.ICurrentUserAccessor;

namespace RequestHandlers.Tools;

/// <summary>
///     Pricing page data: every tool (all free basics — advanced is what costs),
///     purchasable packages, and the current user's access state.
/// </summary>
public sealed class GetPricingCatalogQueryHandler(
    IQueryRepository<ToolForm> forms,
    IQueryRepository<UserToolAccess> grants,
    IQueryRepository<AccessPackage> packages,
    IEntitlementService entitlements,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetPricingCatalogQuery, PricingCatalogResponse>
{
    public async Task<PricingCatalogResponse> Handle(GetPricingCatalogQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var isProUser = await entitlements.IsProAsync(userId, ct);

        var tools = await forms.TableNoTracking
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var myGrants = userId == Guid.Empty
            ? new List<UserToolAccess>()
            : await grants.TableNoTracking
                .Where(g => g.UserId == userId && (g.ExpiresAt == null || g.ExpiresAt > now))
                .ToListAsync(ct);

        var toolRows = tools.Select(t => new ToolPriceRow(
            t.ToolCode, t.Route, t.Title, t.Description, t.Kind.ToString(),
            t.AdvancedPriceToman, t.AdvicePriceToman,
            AdvancedUnlocked: isProUser || myGrants.Any(g => g.ToolCode == t.ToolCode),
            FreeAdvanced: t.AdvancedPriceToman == 0)).ToList();

        var packageRows = await packages.TableNoTracking
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new PackageRow(
                p.Id, p.Name, p.Description, p.PickCount,
                p.ToolCodes(), p.DurationDays, p.PriceToman))
            .ToListAsync(ct);

        var myAccesses = tools
            .Where(t => myGrants.Any(g => g.ToolCode == t.ToolCode))
            .Select(t =>
            {
                var g = myGrants.Where(x => x.ToolCode == t.ToolCode)
                    .OrderByDescending(x => x.ExpiresAt ?? DateTime.MaxValue)
                    .First();
                return new MyAccessRow(t.ToolCode, isProUser, true, g.ExpiresAt);
            })
            .ToList();

        return new PricingCatalogResponse(toolRows, packageRows, myAccesses);
    }
}

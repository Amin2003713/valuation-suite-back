using Application.Payments;
using Application.Interfaces.Base;
using Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Web.Api.Services;

/// <summary>
///     Effective Zarinpal / Pro-pricing settings: the admin-edited DB row when it
///     exists, otherwise the "Zarinpal" appsettings section. Cached for a few seconds
///     because every checkout and gateway call needs it; saving from admin drops the
///     cache entry so changes apply immediately.
/// </summary>
public sealed class PaymentSettingsProvider(
    IQueryRepository<PaymentSettings> rows,
    IOptions<ZarinpalOptions> defaults,
    IMemoryCache cache) : IPaymentSettingsProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public async Task<PaymentSettingsSnapshot> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(PaymentSettingsCache.Key, out var hit) && hit is PaymentSettingsSnapshot cached)
            return cached;

        var row = await rows.TableNoTracking
            .OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var d = defaults.Value;

        var snapshot = row is null
            ? new PaymentSettingsSnapshot(
                d.MerchantId, d.Sandbox, d.DefaultAmountToman, d.ProPlanDurationDays, d.FrontendRedirectUrl)
            : new PaymentSettingsSnapshot(
                row.MerchantId, row.Sandbox, row.ProMonthlyPriceToman, row.ProPlanDurationDays, row.FrontendRedirectUrl);

        cache.Set(PaymentSettingsCache.Key, snapshot, CacheTtl);
        return snapshot;
    }
}

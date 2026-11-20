using Application.Admin;
using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Payments;
using Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace RequestHandlers.Admin;

/// <summary>Extended reporting bundle (revenue split, funnel, tool performance, …).</summary>
public sealed class GetAdminReportsQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminReportsQuery, AdminReportsResponse>
{
    public Task<AdminReportsResponse> Handle(GetAdminReportsQuery request, CancellationToken ct)
        => repo.GetReportsAsync(ct);
}

/// <summary>
///     Reads the effective gateway settings. "Persisted" tells the admin panel whether
///     it is editing the DB row or looking at the appsettings fallback.
/// </summary>
public sealed class GetAdminPaymentSettingsQueryHandler(
    IPaymentSettingsProvider provider,
    IQueryRepository<PaymentSettings> rows)
    : IRequestHandler<GetAdminPaymentSettingsQuery, AdminPaymentSettingsResponse>
{
    public async Task<AdminPaymentSettingsResponse> Handle(GetAdminPaymentSettingsQuery request, CancellationToken ct)
    {
        var settings = await provider.GetAsync(ct);
        var persisted = await rows.TableNoTracking.AnyAsync(ct);

        return new AdminPaymentSettingsResponse(
            settings.MerchantId,
            settings.Sandbox,
            settings.ProMonthlyPriceToman,
            settings.ProPlanDurationDays,
            settings.FrontendRedirectUrl,
            persisted);
    }
}

/// <summary>
///     Upserts the single settings row and drops the cached snapshot, so the next
///     checkout / gateway call uses the new merchant id, mode and price.
/// </summary>
public sealed class UpdateAdminPaymentSettingsCommandHandler(
    ICommandRepository<PaymentSettings> commands,
    IQueryRepository<PaymentSettings> queries,
    IMemoryCache cache)
    : IRequestHandler<UpdateAdminPaymentSettingsCommand, AdminPaymentSettingsResponse>
{
    public async Task<AdminPaymentSettingsResponse> Handle(
        UpdateAdminPaymentSettingsCommand request, CancellationToken ct)
    {
        if (request.ProMonthlyPriceToman < 0 || request.ProPlanDurationDays <= 0)
            throw Common.Exceptions.ValuationException.BadRequest(
                "قیمت ماهانه و مدت اشتراک باید معتبر باشند.");

        var row = await queries.FirstOrDefaultAsync(predicate: null, cancellationToken: ct);
        if (row is null)
        {
            row = new PaymentSettings();
            commands.Add(row, saveNow: false);
        }

        row.MerchantId = request.MerchantId.Trim();
        row.Sandbox = request.Sandbox;
        row.ProMonthlyPriceToman = request.ProMonthlyPriceToman;
        row.ProPlanDurationDays = request.ProPlanDurationDays;
        row.FrontendRedirectUrl = request.FrontendRedirectUrl.Trim();

        await commands.SaveChangesAsync(ct);
        cache.Remove(PaymentSettingsCache.Key);

        return new AdminPaymentSettingsResponse(
            row.MerchantId, row.Sandbox, row.ProMonthlyPriceToman,
            row.ProPlanDurationDays, row.FrontendRedirectUrl, Persisted: true);
    }
}

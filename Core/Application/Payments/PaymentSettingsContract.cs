namespace Application.Payments;

/// <summary>
///     Effective payment settings (DB row when present, otherwise appsettings) as the
///     checkout, the gateway client and the admin panel see them.
/// </summary>
public sealed record PaymentSettingsSnapshot(
    string MerchantId,
    bool Sandbox,
    long ProMonthlyPriceToman,
    int ProPlanDurationDays,
    string FrontendRedirectUrl)
{
    /// <summary>False when the merchant id is missing or still the placeholder.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(MerchantId) &&
        !MerchantId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Reads the effective payment settings (DB override with appsettings fallback).</summary>
public interface IPaymentSettingsProvider
{
    Task<PaymentSettingsSnapshot> GetAsync(CancellationToken ct = default);
}

/// <summary>Shared cache key so saving settings from admin invalidates the same entry.</summary>
public static class PaymentSettingsCache
{
    public const string Key = "payments:settings";
}

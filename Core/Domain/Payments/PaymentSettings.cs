using Domain.Common;

namespace Domain.Payments;

/// <summary>
///     Admin-editable payment gateway / pricing settings. Exactly one row is used
///     (the most recently modified one); when it does not exist the appsettings
///     "Zarinpal" section is the source of truth. Letting staff edit the merchant
///     id, sandbox mode and the Pro monthly price from the admin panel avoids
///     redeploys just to change commercial terms.
/// </summary>
public sealed class PaymentSettings : BaseEntity
{
    /// <summary>Zarinpal merchant id (36-char UUID). Empty = gateway not configured.</summary>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>Sandbox (test) gateway vs production.</summary>
    public bool Sandbox { get; set; } = true;

    /// <summary>Base monthly Pro price in Toman before the multi-month tier discount.</summary>
    public long ProMonthlyPriceToman { get; set; } = 1_000_000;

    /// <summary>Default Pro subscription length in days (used when a payment has no Months).</summary>
    public int ProPlanDurationDays { get; set; } = 30;

    /// <summary>Where the gateway returns the browser after payment.</summary>
    public string FrontendRedirectUrl { get; set; } = string.Empty;
}

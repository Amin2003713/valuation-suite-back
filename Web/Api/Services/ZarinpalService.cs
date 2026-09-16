using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Api.Services;

/// <summary>Options bound from the "Zarinpal" configuration section.</summary>
public class ZarinpalOptions
{
    public const string SectionName = "Zarinpal";

    /// <summary>Zarinpal merchant ID (GUID-like). Use sandbox value in development.</summary>
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>When true, calls go to sandbox.zarinpal.com.</summary>
    public bool Sandbox { get; set; } = true;

    /// <summary>Default payment amount in Toman when the client does not send one.</summary>
    public long DefaultAmountToman { get; set; } = 1_000_000;

    /// <summary>How many days the Pro plan lasts after a successful payment.</summary>
    public int ProPlanDurationDays { get; set; } = 30;

    /// <summary>Where the gateway redirects the user after payment (frontend route).</summary>
    public string FrontendRedirectUrl { get; set; } = "http://localhost:3000/payment/callback";

    public string PaymentGatewayUrl => Sandbox
        ? "https://sandbox.zarinpal.com/pg/v4/payment"
        : "https://payment.zarinpal.com/pg/v4/payment";

    public string StartPayUrl(string authority) => Sandbox
        ? $"https://sandbox.zarinpal.com/pg/StartPay/{authority}"
        : $"https://www.zarinpal.com/pg/StartPay/{authority}";
}

public record ZarinpalStartResult(bool Success, string? Authority, string? GatewayUrl, int? Code, string? Error);
public record ZarinpalVerifyResult(bool Success, string? RefId, long? AmountVerified, int? Code, string? Error);

public class ZarinpalService(HttpClient http, IOptions<ZarinpalOptions> options, ILogger<ZarinpalService> logger)
{
    private readonly ZarinpalOptions _options = options.Value;

    /// <summary>
    ///     Step 1 — request a payment authority from Zarinpal (POST /pg/v4/payment/request.json).
    /// </summary>
    public async Task<ZarinpalStartResult> CreatePaymentAsync(
        long amountToman,
        string callbackUrl,
        string? description = null,
        string? email = null,
        string? mobile = null,
        CancellationToken ct = default)
    {
        var payload = new
        {
            merchant_id = _options.MerchantId,
            amount = amountToman, // Toman
            callback_url = callbackUrl,
            description = description ?? "Valuation Suite Pro subscription",
            metadata = new { email, mobile },
        };

        try
        {
            var response = await http.PostAsJsonAsync($"{_options.PaymentGatewayUrl}/request.json", payload, ct);
            var body = await response.Content.ReadFromJsonAsync<ZarinpalResponse<ZarinpalRequestData>>(cancellationToken: ct);

            if (body?.Data is not null && body.Data.Authority is { Length: > 0 } authority)
            {
                logger.LogInformation("Zarinpal payment requested, authority {Authority}", authority);
                return new ZarinpalStartResult(true, authority, _options.StartPayUrl(authority), body.Data.Code, null);
            }

            var error = body?.Errors?.Message ?? body?.Errors?.Code?.ToString() ?? "Unknown Zarinpal error";
            logger.LogWarning("Zarinpal request failed: {Error}", error);
            return new ZarinpalStartResult(false, null, null, body?.Errors?.Code, error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Zarinpal request call threw");
            return new ZarinpalStartResult(false, null, null, null, ex.Message);
        }
    }

    /// <summary>
    ///     Step 2 — verify a payment after gateway redirect (POST /pg/v4/payment/verify.json).
    /// </summary>
    public async Task<ZarinpalVerifyResult> VerifyPaymentAsync(long amountToman, string authority, CancellationToken ct = default)
    {
        var payload = new
        {
            merchant_id = _options.MerchantId,
            amount = amountToman,
            authority,
        };

        try
        {
            var response = await http.PostAsJsonAsync($"{_options.PaymentGatewayUrl}/verify.json", payload, ct);
            var body = await response.Content.ReadFromJsonAsync<ZarinpalResponse<ZarinpalVerifyData>>(cancellationToken: ct);

            // code 100 = verified, 101 = already verified
            if (body?.Data is not null && (body.Data.Code == 100 || body.Data.Code == 101))
            {
                logger.LogInformation("Zarinpal payment verified, refId {RefId}", body.Data.RefId);
                return new ZarinpalVerifyResult(true, body.Data.RefId?.ToString(), null, body.Data.Code, null);
            }

            var error = body?.Errors?.Message ?? $"Verification failed (code {body?.Data?.Code})";
            logger.LogWarning("Zarinpal verify failed: {Error}", error);
            return new ZarinpalVerifyResult(false, null, null, body?.Data?.Code ?? body?.Errors?.Code, error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Zarinpal verify call threw");
            return new ZarinpalVerifyResult(false, null, null, null, ex.Message);
        }
    }
}

// ─── Zarinpal v4 API wire models ───────────────────────────────

internal class ZarinpalResponse<TData>
{
    [JsonPropertyName("data")]
    public TData? Data { get; set; }

    [JsonPropertyName("errors")]
    public ZarinpalError? Errors { get; set; }
}

internal class ZarinpalRequestData
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("fee")]
    public long? Fee { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal class ZarinpalVerifyData
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("ref_id")]
    public long? RefId { get; set; }

    [JsonPropertyName("card_pan")]
    public string? CardPan { get; set; }

    [JsonPropertyName("card_hash")]
    public string? CardHash { get; set; }

    [JsonPropertyName("fee")]
    public long? Fee { get; set; }
}

internal class ZarinpalError
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("validations")]
    public object? Validations { get; set; }
}

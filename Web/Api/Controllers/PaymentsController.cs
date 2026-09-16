using Application.Interfaces;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Payments;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Api.Services;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    ZarinpalService zarinpal,
    IOptions<ZarinpalOptions> zarinpalOptions,
    UserManager<ApplicationUser> userManager,
    ICommandRepository<Payment> paymentCommands,
    IQueryRepository<Payment> paymentQueries) : ControllerBase
{
    private readonly ZarinpalOptions _options = zarinpalOptions.Value;

    public record StartPaymentRequest(long? AmountToman, string? Description);

    /// <summary>
    ///     Starts a Zarinpal checkout. Returns the gateway URL the frontend must redirect to.
    /// </summary>
    [HttpPost("zarinpal/start")]
    [Authorize]
    public async Task<IActionResult> Start([FromBody] StartPaymentRequest? request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            throw new ForbiddenException("دسترسی غیرمجاز");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("کاربر یافت نشد");

        if (string.IsNullOrWhiteSpace(_options.MerchantId) || _options.MerchantId.StartsWith("YOUR_"))
            throw new ValuationException("درگاه پرداخت پیکربندی نشده است (Zarinpal MerchantId)");

        var amount = request?.AmountToman ?? _options.DefaultAmountToman;
        if (amount < 1000)
            throw new ValuationException("مبلغ پرداخت نامعتبر است");

        var callbackUrl = Url.ActionLink(nameof(Callback), controller: "Payments", values: new { userId })
            ?? $"{_options.FrontendRedirectUrl}";

        var payment = Payment.Create(userId, amount, request?.Description ?? "ارتقا به حساب Pro");
        paymentCommands.Add(payment, saveNow: false);

        var result = await zarinpal.CreatePaymentAsync(
            amount, callbackUrl, payment.Description, user.Email, user.PhoneNumber, ct);

        if (!result.Success || result.Authority is null)
        {
            payment.MarkFailed(result.Error);
            await paymentCommands.SaveChangesAsync(ct);
            throw new ValuationException($"خطا در ایجاد پرداخت: {result.Error}");
        }

        payment.MarkAwaiting(result.Authority);
        await paymentCommands.SaveChangesAsync(ct);

        return Ok(new { paymentId = payment.Id, authority = result.Authority, gatewayUrl = result.GatewayUrl });
    }

    /// <summary>
    ///     Zarinpal redirects the payer here. Verifies the payment and upgrades the user to Pro.
    /// </summary>
    [HttpGet("zarinpal/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? Authority,
        [FromQuery] string? Status,
        [FromQuery] Guid userId,
        CancellationToken ct)
    {
        var redirect = (bool ok, string? message = null) =>
            Redirect($"{_options.FrontendRedirectUrl}?payment={(ok ? "success" : "failed")}{(message is null ? "" : $"&message={Uri.EscapeDataString(message)}")}");

        if (string.IsNullOrWhiteSpace(Authority) || !string.Equals(Status, "OK", StringComparison.OrdinalIgnoreCase))
            return redirect(false, "پرداخت لغو شد");

        var payment = await paymentQueries.Table
            .FirstOrDefaultAsync(p => p.Authority == Authority, ct);

        if (payment is null)
            return redirect(false, "پرداخت یافت نشد");

        if (payment.Status == PaymentStatus.Paid)
            return redirect(true);

        var verify = await zarinpal.VerifyPaymentAsync(payment.Amount, Authority, ct);
        if (!verify.Success)
        {
            payment.MarkFailed(verify.Error);
            await paymentCommands.SaveChangesAsync(ct);
            return redirect(false, verify.Error);
        }

        payment.MarkPaid(verify.RefId);

        var user = await userManager.FindByIdAsync(payment.UserId.ToString());
        if (user is not null)
        {
            user.UpgradeToPro(DateTime.UtcNow.AddDays(_options.ProPlanDurationDays));
            await userManager.UpdateAsync(user);
        }

        await paymentCommands.SaveChangesAsync(ct);
        return redirect(true);
    }

    /// <summary>Payment history for the current user.</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var payments = await paymentQueries.TableNoTracking
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Amount,
                p.Status,
                p.RefId,
                p.Description,
                p.PaidAt,
                p.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(payments);
    }
}

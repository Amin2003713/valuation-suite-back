using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Tools;
using Common.Exceptions;
using Domain.Payments;
using Domain.Tools;
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
    IQueryRepository<Payment> paymentQueries,
    IQueryRepository<ToolForm> toolForms,
    IQueryRepository<AccessPackage> packages,
    IQueryRepository<ToolSubmission> submissions,
    IEntitlementService entitlements) : ControllerBase
{
    private readonly ZarinpalOptions _options = zarinpalOptions.Value;

    // ─── Legacy/simple start (Pro plan) — kept for compatibility ───

    public record StartPaymentRequest(long? AmountToman, string? Description);

    /// <summary>Starts a Zarinpal checkout. Returns the gateway URL the frontend must redirect to.</summary>
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
        payment.Kind = PaymentKind.ProPlan;
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

    // ─── Typed checkout (server-computed amounts — the client never dictates price) ───

    /// <summary>
    ///     Starts a typed checkout: pro (subscription), tool (advanced unlock for one tool),
    ///     package (a bundle; with pick list when the package has pick-credits), or
    ///     advice (paid adviser review of one submission). Amount is resolved server-side.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        if (userId == Guid.Empty)
            throw new ForbiddenException("دسترسی غیرمجاز");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("کاربر یافت نشد");

        if (string.IsNullOrWhiteSpace(_options.MerchantId) || _options.MerchantId.StartsWith("YOUR_"))
            throw new ValuationException("درگاه پرداخت پیکربندی نشده است (Zarinpal MerchantId)");

        // Resolve kind + amount from server data only.
        long amount;
        string description;
        var payment = Payment.Create(userId, 0, string.Empty);

        switch (request.Kind)
        {
            case "pro":
                amount = _options.DefaultAmountToman;
                description = "ارتقا به حساب Pro";
                payment.Kind = PaymentKind.ProPlan;
                break;

            case "tool":
            {
                var tool = await toolForms.TableNoTracking
                    .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct)
                    ?? throw new NotFoundException("ابزار یافت نشد");
                if (tool.AdvancedPriceToman <= 0)
                    throw new ValuationException("بخش پیشرفته این ابزار رایگان است.");
                if (await entitlements.CanViewAdvancedAsync(userId, tool.ToolCode, ct))
                    throw new ConflictException("شما از قبل به بخش پیشرفته این ابزار دسترسی دارید.");

                amount = tool.AdvancedPriceToman;
                description = $"دسترسی پیشرفته — {tool.Title}";
                payment.Kind = PaymentKind.ToolAdvanced;
                payment.ToolCode = tool.ToolCode;
                break;
            }

            case "package":
            {
                var package = await packages.TableNoTracking
                    .FirstOrDefaultAsync(p => p.Id == request.PackageId && p.IsActive, ct)
                    ?? throw new NotFoundException("بسته یافت نشد");

                amount = package.PriceToman;
                description = $"بسته {package.Name}";
                payment.Kind = PaymentKind.Package;
                payment.PackageId = package.Id;
                break;
            }

            case "advice":
            {
                var sub = await submissions.TableNoTracking
                    .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct)
                    ?? throw new NotFoundException("ارزیابی یافت نشد");
                if (sub.UserId != userId)
                    throw new ForbiddenException("این ارزیابی متعلق به شما نیست.");

                var tool = await toolForms.TableNoTracking
                    .FirstOrDefaultAsync(t => t.ToolCode == sub.ToolCode, ct)
                    ?? throw new NotFoundException("ابزار یافت نشد");
                if (tool.AdvicePriceToman <= 0)
                    throw new ValuationException("برای این ابزار مشاوره ارائه نمی‌شود.");
                if (await entitlements.HasPaidAdviceAsync(userId, sub.Id, ct))
                    throw new ConflictException("برای این ارزیابی قبلاً مشاوره خریداری شده است.");

                amount = tool.AdvicePriceToman;
                description = $"بررسی کارشناسی — {tool.Title}";
                payment.Kind = PaymentKind.Advice;
                payment.SubmissionId = sub.Id;
                break;
            }

            default:
                throw new ValuationException("نوع پرداخت نامعتبر است.");
        }

        payment.Amount = amount;
        payment.Description = description;

        var callbackUrl = Url.ActionLink(nameof(Callback), controller: "Payments", values: new { userId })
            ?? $"{_options.FrontendRedirectUrl}";

        paymentCommands.Add(payment, saveNow: false);

        var result = await zarinpal.CreatePaymentAsync(
            amount, callbackUrl, description, user.Email, user.PhoneNumber, ct);

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

    public record CheckoutRequest(
        string Kind, string? ToolCode = null, Guid? PackageId = null,
        Guid? SubmissionId = null, List<string>? PickToolCodes = null);

    /// <summary>
    ///     Zarinpal redirects the payer here. Verifies the payment and grants the
    ///     entitlements for the payment's kind (pro / tool / package / advice).
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
        await GrantEntitlementsAsync(payment, ct);
        await paymentCommands.SaveChangesAsync(ct);

        return redirect(true);
    }

    /// <summary>Grants what the payment bought, by kind (domain side-effects on success).</summary>
    private async Task GrantEntitlementsAsync(Payment payment, CancellationToken ct)
    {
        switch (payment.Kind)
        {
            case PaymentKind.ProPlan:
            {
                var user = await userManager.FindByIdAsync(payment.UserId.ToString());
                if (user is not null)
                {
                    // Extend from the later of now / current expiry.
                    var from = user.PlanExpiresAt is { } e && e > DateTime.UtcNow ? e : DateTime.UtcNow;
                    user.UpgradeToPro(from.AddDays(_options.ProPlanDurationDays));
                    await userManager.UpdateAsync(user);
                }
                break;
            }

            case PaymentKind.ToolAdvanced:
                if (payment.ToolCode is { } toolCode)
                    await entitlements.GrantAsync(payment.UserId, toolCode, null, payment.Id, null, ct);
                break;

            case PaymentKind.Package:
                if (payment.PackageId is { } pkgId)
                {
                    var pkg = await packages.TableNoTracking.FirstOrDefaultAsync(p => p.Id == pkgId, ct);
                    if (pkg is not null)
                    {
                        var expires = pkg.DurationDays is { } d ? DateTime.UtcNow.AddDays(d) : (DateTime?)null;
                        foreach (var bundledTool in pkg.ToolCodes())
                            await entitlements.GrantAsync(payment.UserId, bundledTool, expires, payment.Id, null, ct);

                        if (pkg.PickCount is { } n && n > 0)
                            await entitlements.GrantPickCreditsAsync(payment.UserId, n, payment.Id, ct);
                    }
                }
                break;

            case PaymentKind.Advice:
                // Entitlement is verified via the paid Payment row itself — no grant row needed.
                break;
        }
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
                p.Kind,
                p.ToolCode,
                p.PackageId,
                p.SubmissionId,
                p.RefId,
                p.Description,
                p.PaidAt,
                p.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(payments);
    }
}

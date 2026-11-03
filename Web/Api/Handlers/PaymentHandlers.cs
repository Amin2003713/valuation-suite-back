using Application.Interfaces;
using Application.Interfaces.Base;
using Application.Tools;
using Common.Exceptions;
using Domain.Payments;
using Domain.Tools;
using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Web.Api.Services;

namespace Web.Api.Handlers;

/* Payments — every use case lives here.
 *
 * The controller only maps HTTP to these requests (plus generating the absolute
 * gateway return URL, which is an HTTP-routing concern). Amounts are always
 * resolved from server data: the client picks *what* to buy, never *how much*.
 */

/// <summary>Legacy/simple Pro checkout (kept for compatibility).</summary>
public sealed record StartProPaymentCommand(
    Guid UserId, long? AmountToman, string? Description, string CallbackUrl)
    : IRequest<StartPaymentResult>;

/// <summary>Typed checkout: pro | tool | package | advice.</summary>
public sealed record StartCheckoutCommand(
    Guid UserId,
    string Kind,
    string? ToolCode,
    Guid? PackageId,
    Guid? SubmissionId,
    List<string>? PickToolCodes,
    int? Months,
    string CallbackUrl) : IRequest<StartPaymentResult>;

public sealed record StartPaymentResult(Guid PaymentId, string Authority, string? GatewayUrl);

public sealed record HandlePaymentCallbackCommand(string? Authority, string? Status)
    : IRequest<PaymentCallbackResult>;

public sealed record PaymentCallbackResult(bool Success, string? Message, string RedirectUrl);

public sealed record GetMyPaymentsQuery(Guid UserId) : IRequest<List<MyPaymentDto>>;

public sealed record MyPaymentDto(
    Guid Id,
    long Amount,
    PaymentStatus Status,
    PaymentKind Kind,
    string? ToolCode,
    Guid? PackageId,
    Guid? SubmissionId,
    string? RefId,
    string Description,
    DateTime? PaidAt,
    DateTime CreatedAt);

/// <summary>Tiered Pro pricing shared by the checkout and the pricing catalog.</summary>
public static class ProPricing
{
    /// <summary>Unit price per month with volume discounts. Mirrors /plans on the client.</summary>
    public static readonly (int Months, decimal PerMonthFactor)[] Tiers =
    [
        (1, 1.00m),
        (3, 0.90m),
        (6, 0.80m),
        (12, 0.65m),
    ];

    /// <summary>Price in Toman for a plan length (monthly base × months × tier discount).</summary>
    public static long Price(long monthlyBaseToman, int months)
    {
        var factor = Tiers.FirstOrDefault(t => t.Months == months, Tiers[0]).PerMonthFactor;
        return (long)Math.Round(monthlyBaseToman * months * factor);
    }

    /// <summary>Only the published tier lengths are purchasable; anything else = 1 month.</summary>
    public static int NormalizeMonths(int? months) =>
        months is { } m && Tiers.Any(t => t.Months == m) ? m : 1;
}

internal static class PaymentRules
{
    public static void EnsureGatewayConfigured(ZarinpalOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.MerchantId) || options.MerchantId.StartsWith("YOUR_"))
            throw new ValuationException("درگاه پرداخت پیکربندی نشده است (Zarinpal MerchantId)");
    }

    public static string CallbackOrDefault(string? callbackUrl, ZarinpalOptions options) =>
        string.IsNullOrWhiteSpace(callbackUrl) ? options.FrontendRedirectUrl : callbackUrl;

    public static Guid RequireUserId(Guid userId) =>
        userId == Guid.Empty ? throw new ForbiddenException("دسترسی غیرمجاز") : userId;
}

public sealed class StartProPaymentHandler(
    ZarinpalService zarinpal,
    IOptions<ZarinpalOptions> options,
    UserManager<ApplicationUser> userManager,
    ICommandRepository<Payment> payments)
    : IRequestHandler<StartProPaymentCommand, StartPaymentResult>
{
    public async Task<StartPaymentResult> Handle(StartProPaymentCommand request, CancellationToken ct)
    {
        var settings = options.Value;

        var userId = PaymentRules.RequireUserId(request.UserId);
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("کاربر یافت نشد");

        PaymentRules.EnsureGatewayConfigured(settings);

        var amount = request.AmountToman ?? settings.DefaultAmountToman;
        if (amount < 1000)
            throw new ValuationException("مبلغ پرداخت نامعتبر است");

        var payment = Payment.Create(userId, amount, request.Description ?? "ارتقا به حساب Pro");
        payment.Kind = PaymentKind.ProPlan;
        payments.Add(payment, saveNow: false);

        var result = await zarinpal.CreatePaymentAsync(
            amount,
            PaymentRules.CallbackOrDefault(request.CallbackUrl, settings),
            payment.Description,
            user.Email,
            user.PhoneNumber,
            ct);

        if (!result.Success || result.Authority is null)
        {
            payment.MarkFailed(result.Error);
            await payments.SaveChangesAsync(ct);
            throw new ValuationException($"خطا در ایجاد پرداخت: {result.Error}");
        }

        payment.MarkAwaiting(result.Authority);
        await payments.SaveChangesAsync(ct);

        return new StartPaymentResult(payment.Id, result.Authority, result.GatewayUrl);
    }
}

public sealed class StartCheckoutHandler(
    ZarinpalService zarinpal,
    IOptions<ZarinpalOptions> options,
    UserManager<ApplicationUser> userManager,
    ICommandRepository<Payment> payments,
    IQueryRepository<ToolForm> toolForms,
    IQueryRepository<AccessPackage> packages,
    IToolSubmissionQueries submissions,
    IEntitlementService entitlements)
    : IRequestHandler<StartCheckoutCommand, StartPaymentResult>
{
    public async Task<StartPaymentResult> Handle(StartCheckoutCommand request, CancellationToken ct)
    {
        var settings = options.Value;

        var userId = PaymentRules.RequireUserId(request.UserId);
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("کاربر یافت نشد");

        PaymentRules.EnsureGatewayConfigured(settings);

        long amount;
        string description;
        var payment = Payment.Create(userId, 0, string.Empty);

        switch (request.Kind)
        {
            case "pro":
            {
                var months = ProPricing.NormalizeMonths(request.Months);
                amount = ProPricing.Price(settings.DefaultAmountToman, months);
                description = months == 1 ? "اشتراک یک‌ماهه Pro" : $"اشتراک {months}-ماهه Pro";
                payment.Kind = PaymentKind.ProPlan;
                payment.Months = months;
                break;
            }

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
                var submission = await submissions.All
                    .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct)
                    ?? throw new NotFoundException("ارزیابی یافت نشد");
                if (submission.UserId != userId)
                    throw new ForbiddenException("این ارزیابی متعلق به شما نیست.");

                var tool = await toolForms.TableNoTracking
                    .FirstOrDefaultAsync(t => t.ToolCode == submission.ToolCode, ct)
                    ?? throw new NotFoundException("ابزار یافت نشد");
                if (tool.AdvicePriceToman <= 0)
                    throw new ValuationException("برای این ابزار مشاوره ارائه نمی‌شود.");
                if (await entitlements.HasPaidAdviceAsync(userId, submission.Id, ct))
                    throw new ConflictException("برای این ارزیابی قبلاً مشاوره خریداری شده است.");

                amount = tool.AdvicePriceToman;
                description = $"بررسی کارشناسی — {tool.Title}";
                payment.Kind = PaymentKind.Advice;
                payment.SubmissionId = submission.Id;
                break;
            }

            default:
                throw new ValuationException("نوع پرداخت نامعتبر است.");
        }

        payment.Amount = amount;
        payment.Description = description;

        payments.Add(payment, saveNow: false);

        var result = await zarinpal.CreatePaymentAsync(
            amount,
            PaymentRules.CallbackOrDefault(request.CallbackUrl, settings),
            description,
            user.Email,
            user.PhoneNumber,
            ct);

        if (!result.Success || result.Authority is null)
        {
            payment.MarkFailed(result.Error);
            await payments.SaveChangesAsync(ct);
            throw new ValuationException($"خطا در ایجاد پرداخت: {result.Error}");
        }

        payment.MarkAwaiting(result.Authority);
        await payments.SaveChangesAsync(ct);

        return new StartPaymentResult(payment.Id, result.Authority, result.GatewayUrl);
    }
}

/// <summary>
///     Verifies the gateway return and grants what was bought. Idempotent: a
///     payment already marked Paid short-circuits, so a double callback (browser
///     refresh) never double-grants.
/// </summary>
public sealed class HandlePaymentCallbackHandler(
    ZarinpalService zarinpal,
    IOptions<ZarinpalOptions> options,
    UserManager<ApplicationUser> userManager,
    ICommandRepository<Payment> payments,
    IQueryRepository<AccessPackage> packages,
    IEntitlementService entitlements)
    : IRequestHandler<HandlePaymentCallbackCommand, PaymentCallbackResult>
{
    public async Task<PaymentCallbackResult> Handle(HandlePaymentCallbackCommand request, CancellationToken ct)
    {
        var settings = options.Value;

        PaymentCallbackResult Redirect(bool ok, string? message = null) => new(
            ok,
            message,
            $"{settings.FrontendRedirectUrl}?payment={(ok ? "success" : "failed")}" +
            (message is null ? "" : $"&message={Uri.EscapeDataString(message)}"));

        if (string.IsNullOrWhiteSpace(request.Authority) ||
            !string.Equals(request.Status, "OK", StringComparison.OrdinalIgnoreCase))
            return Redirect(false, "پرداخت لغو شد");

        var authority = request.Authority!;

        // Load through the WRITE-side repository: this entity is mutated below.
        var payment = await payments.Table.FirstOrDefaultAsync(p => p.Authority == authority, ct);
        if (payment is null)
            return Redirect(false, "پرداخت یافت نشد");

        if (payment.Status == PaymentStatus.Paid)
            return Redirect(true);

        var verify = await zarinpal.VerifyPaymentAsync(payment.Amount, authority, ct);
        if (!verify.Success)
        {
            payment.MarkFailed(verify.Error);
            await payments.SaveChangesAsync(ct);
            return Redirect(false, verify.Error);
        }

        payment.MarkPaid(verify.RefId);
        await GrantEntitlementsAsync(payment, settings, ct);
        await payments.SaveChangesAsync(ct);

        return Redirect(true);
    }

    /// <summary>Grants what the payment bought, by kind (domain side-effects on success).</summary>
    private async Task GrantEntitlementsAsync(Payment payment, ZarinpalOptions settings, CancellationToken ct)
    {
        switch (payment.Kind)
        {
            case PaymentKind.ProPlan:
            {
                var user = await userManager.FindByIdAsync(payment.UserId.ToString());
                if (user is not null)
                {
                    // Extend from the later of now / current expiry.
                    var from = user.PlanExpiresAt is { } expiry && expiry > DateTime.UtcNow
                        ? expiry
                        : DateTime.UtcNow;
                    var months = payment.Months ?? Math.Max(1, settings.ProPlanDurationDays / 30);
                    user.UpgradeToPro(from.AddMonths(months));
                    await userManager.UpdateAsync(user);
                }
                break;
            }

            case PaymentKind.ToolAdvanced:
                if (payment.ToolCode is { } toolCode)
                    await entitlements.GrantAsync(payment.UserId, toolCode, null, payment.Id, null, ct);
                break;

            case PaymentKind.Package:
                if (payment.PackageId is { } packageId)
                {
                    var package = await packages.TableNoTracking.FirstOrDefaultAsync(p => p.Id == packageId, ct);
                    if (package is not null)
                    {
                        var expires = package.DurationDays is { } days
                            ? DateTime.UtcNow.AddDays(days)
                            : (DateTime?)null;
                        foreach (var bundledTool in package.ToolCodes())
                            await entitlements.GrantAsync(payment.UserId, bundledTool, expires, payment.Id, null, ct);

                        if (package.PickCount is { } picks && picks > 0)
                            await entitlements.GrantPickCreditsAsync(payment.UserId, picks, payment.Id, ct);
                    }
                }
                break;

            case PaymentKind.Advice:
                // Entitlement is verified via the paid Payment row itself — no grant row needed.
                break;
        }
    }
}

public sealed class GetMyPaymentsHandler(IQueryRepository<Payment> payments)
    : IRequestHandler<GetMyPaymentsQuery, List<MyPaymentDto>>
{
    public async Task<List<MyPaymentDto>> Handle(GetMyPaymentsQuery request, CancellationToken ct) =>
        await payments.TableNoTracking
            .Where(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new MyPaymentDto(
                p.Id, p.Amount, p.Status, p.Kind, p.ToolCode, p.PackageId,
                p.SubmissionId, p.RefId, p.Description, p.PaidAt, p.CreatedAt))
            .ToListAsync(ct);
}

using ApiFramework.Controller;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Services;

namespace Web.Api.Controllers;

/// <summary>
///     Thin HTTP surface for payments. Amounts, validation, gateway calls and
///     entitlement grants all live in <see cref="Handlers" /> — this controller
///     only maps routes to MediatR requests.
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Starts a Zarinpal checkout. Returns the gateway URL the frontend must redirect to.</summary>
    [HttpPost("zarinpal/start")]
    [Authorize]
    public async Task<IActionResult> Start([FromBody] StartPaymentRequest? request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await mediator.Send(
            new Handlers.StartProPaymentCommand(userId, request?.AmountToman, request?.Description, CallbackUrl(userId)),
            ct));
    }

    public record StartPaymentRequest(long? AmountToman, string? Description);

    /// <summary>
    ///     Starts a typed checkout: pro (subscription), tool (advanced unlock for one
    ///     tool), package (a bundle; with pick list when the package has pick-credits),
    ///     or advice (paid adviser review of one submission). Amount is resolved
    ///     server-side.
    /// </summary>
    [HttpPost("checkout")]
    [Authorize]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        return Ok(await mediator.Send(new Handlers.StartCheckoutCommand(
            userId,
            request.Kind,
            request.ToolCode,
            request.PackageId,
            request.SubmissionId,
            request.PickToolCodes,
            request.Months,
            CallbackUrl(userId)), ct));
    }

    public record CheckoutRequest(
        string Kind, string? ToolCode = null, Guid? PackageId = null,
        Guid? SubmissionId = null, List<string>? PickToolCodes = null,
        int? Months = null);

    /// <summary>
    ///     Zarinpal redirects the payer here. Verifies the payment and grants the
    ///     entitlements for the payment's kind (pro / tool / package / advice).
    /// </summary>
    [HttpGet("zarinpal/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? Authority,
        [FromQuery] string? Status,
        [FromQuery] string? userId,
        CancellationToken ct)
    {
        var result = await mediator.Send(new Handlers.HandlePaymentCallbackCommand(Authority, Status), ct);
        return Redirect(result.RedirectUrl);
    }

    /// <summary>Payment history for the current user.</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await mediator.Send(new Handlers.GetMyPaymentsQuery(User.GetUserId()), ct));

    /// <summary>
    ///     Absolute URL the gateway returns to. Generating a route link needs the
    ///     current request, so it stays here; the handler falls back to the
    ///     configured frontend URL when this is empty.
    /// </summary>
    private string CallbackUrl(Guid userId) =>
        Url.ActionLink(nameof(Callback), "Payments", new { userId }) ?? string.Empty;
}

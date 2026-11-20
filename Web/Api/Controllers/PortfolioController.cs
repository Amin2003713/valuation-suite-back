using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestHandlers.Portfolio;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public sealed class PortfolioController(IMediator mediator) : ControllerBase
{
    /// <summary>The company's IP assets, each with its latest value and change.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await mediator.Send(new GetPortfolioQuery(), ct));

    [HttpPost("assets")]
    public async Task<IActionResult> CreateAsset([FromBody] CreateAssetRequest request, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new CreateAssetCommand(request), ct) });

    [HttpDelete("assets/{id:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid id, CancellationToken ct)
        => Ok(new { deleted = await mediator.Send(new DeleteAssetCommand(id), ct) });

    [HttpGet("assets/{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetAssetHistoryQuery(id), ct));

    [HttpPost("assets/{id:guid}/valuations")]
    public async Task<IActionResult> AttachValuation(
        Guid id, [FromBody] AttachValuationRequest request, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new AttachValuationCommand(id, request), ct) });

    [HttpDelete("valuations/{id:guid}")]
    public async Task<IActionResult> DeleteValuation(Guid id, CancellationToken ct)
        => Ok(new { deleted = await mediator.Send(new DeleteValuationCommand(id), ct) });
}

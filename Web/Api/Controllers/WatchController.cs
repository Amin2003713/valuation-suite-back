using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RequestHandlers.Watch;

namespace Web.Api.Controllers;

[ApiController]
[Route("api/watch")]
[Authorize]
public sealed class WatchController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? profileId, CancellationToken ct)
        => Ok(await mediator.Send(new GetWatchQuery(profileId), ct));

    [HttpPost("profiles")]
    public async Task<IActionResult> Create([FromBody] CreateWatchProfileRequest request, CancellationToken ct)
        => Ok(new { id = await mediator.Send(new CreateWatchProfileCommand(request), ct) });

    [HttpDelete("profiles/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => Ok(new { deleted = await mediator.Send(new DeleteWatchProfileCommand(id), ct) });

    /// <summary>
    ///     Import hits for a profile (adviser/TISC search output). Safe to call
    ///     repeatedly: hits are de-duplicated on (source, externalId).
    /// </summary>
    [HttpPost("profiles/{id:guid}/hits")]
    public async Task<IActionResult> Import(Guid id, [FromBody] ImportWatchHitRequest[] hits, CancellationToken ct)
        => Ok(new { added = await mediator.Send(new ImportWatchHitsCommand(id, hits), ct) });

    [HttpPost("hits/{id:guid}/seen")]
    public async Task<IActionResult> MarkSeen(Guid id, CancellationToken ct)
        => Ok(new { ok = await mediator.Send(new MarkWatchHitSeenCommand(id), ct) });
}

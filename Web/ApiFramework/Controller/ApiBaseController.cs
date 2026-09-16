namespace ApiFramework.Controller;

public abstract class ApiBaseController : ControllerBase
{
    private ISender? _sender;
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>Sends a command/query and returns 204 No Content when the result is null/unit.</summary>
    protected async Task<IActionResult> ExecuteAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var result = await Sender.Send(request, ct);
        if (result is null)
            return NoContent();

        return Ok(result);
    }

    protected async Task<IActionResult> ExecuteAsync(IRequest request, CancellationToken ct = default)
    {
        await Sender.Send(request, ct);
        return NoContent();
    }

    /// <summary>Returns 201 Created with the new resource's id.</summary>
    protected async Task<IActionResult> ExecuteCreateAsync(IRequest<Guid> request, string routeName, CancellationToken ct = default)
    {
        var id = await Sender.Send(request, ct);
        return CreatedAtRoute(routeName, new { id }, new { id });
    }
}

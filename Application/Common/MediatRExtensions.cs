using MediatR;

namespace Application.Common;

public static class MediatRExtensions
{
    public static async Task<TResponse> SendAndSaveAsync<TResponse>(
        this IMediator mediator,
        IRequest<TResponse> request,
        IUnitOfWork uow,
        CancellationToken ct = default)
    {
        var response = await mediator.Send(request, ct);
        await uow.SaveChangesAsync(ct);
        return response;
    }
}

using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using Application.Interfaces.Base;

namespace RequestHandlers.Tools;

/// <summary>Provides the current user's id (kept here so handlers stay testable).</summary>
public interface ICurrentUserAccessor
{
    Guid UserId { get; }
}

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    public Guid UserId
    {
        get
        {
            var value = accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? accessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }
}

public sealed class RunToolCommandHandler(
    IToolRunnerResolver resolver,
    ICommandRepository<ToolSubmission> submissions,
    ICurrentUserAccessor currentUser) : IRequestHandler<RunToolCommand, ToolRunResponse>
{
    public async Task<ToolRunResponse> Handle(RunToolCommand request, CancellationToken ct)
    {
        if (currentUser.UserId == Guid.Empty)
            throw Common.Exceptions.ValuationException.Forbidden("برای اجرای ابزار ابتدا وارد حساب خود شوید.");

        var runner = resolver.Resolve(request.ToolCode);
        var outcome = runner.Run(request.Input);

        var submission = ToolSubmission.Create(
            currentUser.UserId,
            request.ToolCode,
            outcome.Name,
            JsonSerializer.Serialize(request.Input),
            JsonSerializer.Serialize(outcome.Result),
            outcome.OverallScore);

        await submissions.AddAsync(submission, ct, saveNow: true);

        return new ToolRunResponse(
            submission.Id,
            request.ToolCode,
            ToolInput.ToJsonElement(outcome.Result),
            outcome.OverallScore);
    }
}

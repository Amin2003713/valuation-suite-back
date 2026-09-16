using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Application.Interfaces.Base;

namespace RequestHandlers.Tools;

public sealed class GetToolFormQueryHandler(IQueryRepository<ToolForm> forms)
    : IRequestHandler<GetToolFormQuery, ToolFormResponse?>
{
    public async Task<ToolFormResponse?> Handle(GetToolFormQuery request, CancellationToken ct)
    {
        var form = await forms.TableNoTracking
            .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct);

        if (form is null)
            throw Common.Exceptions.ValuationException.NotFound($"ابزار «{request.ToolCode}» یافت نشد.");

        return new ToolFormResponse(
            form.ToolCode, form.Title, form.Description,
            form.Kind.ToString(), JsonSerializer.Deserialize<JsonElement>(form.SchemaJson));
    }
}

public sealed class GetToolFormsQueryHandler(IQueryRepository<ToolForm> forms)
    : IRequestHandler<GetToolFormsQuery, List<ToolFormSummary>>
{
    public async Task<List<ToolFormSummary>> Handle(GetToolFormsQuery request, CancellationToken ct)
    {
        var list = await forms.TableNoTracking
            .OrderBy(t => t.SortOrder)
            .Select(t => new ToolFormSummary(t.ToolCode, t.Title, t.Description, t.Kind.ToString(), t.SortOrder))
            .ToListAsync(ct);
        return list;
    }
}

public sealed class GetToolSubmissionsQueryHandler(
    IQueryRepository<ToolSubmission> submissions,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetToolSubmissionsQuery, List<ToolSubmissionResponse>>
{
    public async Task<List<ToolSubmissionResponse>> Handle(GetToolSubmissionsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw Common.Exceptions.ValuationException.Forbidden("برای مشاهده تاریخچه ابتدا وارد حساب خود شوید.");

        var query = submissions.TableNoTracking.Where(s => s.UserId == userId);
        if (!string.IsNullOrWhiteSpace(request.ToolCode))
            query = query.Where(s => s.ToolCode == request.ToolCode);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(50)
            .Select(s => new ToolSubmissionResponse(
                s.Id, s.ToolCode, s.Name,
                JsonSerializer.Deserialize<JsonElement>(s.InputJson),
                JsonSerializer.Deserialize<JsonElement>(s.ResultJson),
                s.OverallScore, s.CreatedAt))
            .ToListAsync(ct);
    }
}

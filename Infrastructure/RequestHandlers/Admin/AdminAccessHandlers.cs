using Application.Admin;
using Application.Interfaces;
using Common.Exceptions;
using Domain.Tools;
using Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ICurrentUserAccessor = RequestHandlers.Tools.ICurrentUserAccessor;

namespace RequestHandler.Admin;

/// <summary>Chart-ready analytics for visual reporting (no raw JSON).</summary>
public sealed class GetAdminAnalyticsQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminAnalyticsQuery, AdminAnalyticsResponse>
{
    public Task<AdminAnalyticsResponse> Handle(GetAdminAnalyticsQuery request, CancellationToken ct)
        => repo.GetAnalyticsAsync(ct);
}

public sealed class GetAdminGrantsQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdminGrantsQuery, List<AdminGrantRow>>
{
    public Task<List<AdminGrantRow>> Handle(GetAdminGrantsQuery request, CancellationToken ct)
        => repo.GetGrantsAsync(request.UserId, ct);
}

public sealed class GetAdviserQueueQueryHandler(IAdminQueryRepository repo)
    : IRequestHandler<GetAdviserQueueQuery, List<AdminSubmissionRow>>
{
    public Task<List<AdminSubmissionRow>> Handle(GetAdviserQueueQuery request, CancellationToken ct)
        => repo.GetAdviserQueueAsync(ct);
}

/// <summary>
///     Admin comp/goodwill grant. Days=null → perpetual. Separate rows are kept
///     for time-limited grants; perpetual grants replace each other.
/// </summary>
public sealed class GrantAdminAccessCommandHandler(
    ICommandRepository<UserToolAccess> grants,
    IQueryRepository<ApplicationUser> users)
    : IRequestHandler<GrantAdminAccessCommand>
{
    public async Task Handle(GrantAdminAccessCommand request, CancellationToken ct)
    {
        if (!await users.TableNoTracking.AnyAsync(u => u.Id == request.UserId, ct))
            throw ValuationException.NotFound("کاربر یافت نشد.");

        if (string.IsNullOrWhiteSpace(request.ToolCode))
            throw ValuationException.BadRequest("کد ابزار الزامی است.");

        grants.Add(
            UserToolAccess.Grant(
                request.UserId, request.ToolCode,
                request.Days is { } d ? DateTime.UtcNow.AddDays(d) : null,
                paymentId: null, grantedBy: null),
            saveNow: true);
    }
}

public sealed class RevokeAdminAccessCommandHandler(ICommandRepository<UserToolAccess> grants)
    : IRequestHandler<RevokeAdminAccessCommand>
{
    public async Task Handle(RevokeAdminAccessCommand request, CancellationToken ct)
    {
        var grant = await grants.Table
            .FirstOrDefaultAsync(g => g.Id == request.GrantId, ct)
            ?? throw ValuationException.NotFound("دسترسی یافت نشد.");

        grants.Delete(grant, saveNow: true);
    }
}

public sealed class UpsertAdminPackageCommandHandler(ICommandRepository<AccessPackage> packages)
    : IRequestHandler<UpsertAdminPackageCommand, Guid>
{
    public async Task<Guid> Handle(UpsertAdminPackageCommand request, CancellationToken ct)
    {
        if (request.PickCount is null && (request.ToolCodes is null || request.ToolCodes.Count == 0))
            throw ValuationException.BadRequest("بسته باید ابزار ثابت داشته باشد یا تعداد انتخاب آزاد.");

        AccessPackage? entity;
        if (request.Id is { } id)
        {
            entity = await packages.Table.FirstOrDefaultAsync(p => p.Id == id, ct)
                     ?? throw ValuationException.NotFound("بسته یافت نشد.");

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.PickCount = request.PickCount;
            entity.ToolCodesJson = System.Text.Json.JsonSerializer.Serialize(request.ToolCodes ?? []);
            entity.DurationDays = request.DurationDays;
            entity.PriceToman = request.PriceToman;
            entity.IsActive = request.IsActive;
            entity.SortOrder = request.SortOrder;
            await packages.SaveChangesAsync(ct);
            return entity.Id;
        }

        entity = AccessPackage.Create(
            request.Name, request.Description, request.PickCount,
            request.ToolCodes ?? [], request.DurationDays, request.PriceToman, request.SortOrder);
        await packages.AddAsync(entity, ct, saveNow: true);
        return entity.Id;
    }
}

/// <summary>
///     Adviser note on a submission. Requires notes.write permission (controller gate).
///     Voice arrives as base64 data; kept small by the client (max ~2 minutes).
/// </summary>
public sealed class AddSubmissionNoteCommandHandler(
    ICommandRepository<SubmissionNote> notes,
    IQueryRepository<ToolSubmission> submissions,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<AddSubmissionNoteCommand, AdminNoteRow>
{
    public async Task<AdminNoteRow> Handle(AddSubmissionNoteCommand request, CancellationToken ct)
    {
        var adviserId = currentUser.UserId;
        if (adviserId == Guid.Empty)
            throw ValuationException.Forbidden("دسترسی غیرمجاز.");

        if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.AudioBase64))
            throw ValuationException.BadRequest("یادداشت متنی یا ویس الزامی است.");

        if (!await submissions.TableNoTracking.AnyAsync(s => s.Id == request.SubmissionId, ct))
            throw ValuationException.NotFound("پاسخ یافت نشد.");

        var note = SubmissionNote.Create(
            request.SubmissionId, adviserId,
            request.Text, request.AudioBase64, request.AudioMimeType, request.AudioSeconds);

        await notes.AddAsync(note, ct, saveNow: true);

        return new AdminNoteRow(note.Id, note.SubmissionId, note.Text, note.AudioBase64, note.AudioMimeType, note.AudioSeconds, false, note.CreatedAt);
    }
}

public sealed class GetSubmissionNotesQueryHandler(IQueryRepository<SubmissionNote> notes)
    : IRequestHandler<GetSubmissionNotesQuery, List<AdminNoteRow>>
{
    public Task<List<AdminNoteRow>> Handle(GetSubmissionNotesQuery request, CancellationToken ct)
        => notes.TableNoTracking
            .Where(n => n.SubmissionId == request.SubmissionId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new AdminNoteRow(
                n.Id, n.SubmissionId, n.Text, n.AudioBase64, n.AudioMimeType, n.AudioSeconds,
                n.SeenByCustomer, n.CreatedAt))
            .ToListAsync(ct);
}

/// <summary>
///     Customer-side: marks the notes on their own submission as seen and returns them.
/// </summary>
public sealed class GetMySubmissionNotesQueryHandler(
    IQueryRepository<ToolSubmission> submissions,
    ICommandRepository<SubmissionNote> notes,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetMySubmissionNotesQuery, List<AdminNoteRow>>
{
    public async Task<List<AdminNoteRow>> Handle(GetMySubmissionNotesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw ValuationException.Forbidden("دسترسی غیرمجاز.");

        var sub = await submissions.TableNoTracking
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct)
            ?? throw ValuationException.NotFound("پاسخ یافت نشد.");

        if (sub.UserId != userId)
            throw ValuationException.Forbidden("این ارزیابی متعلق به شما نیست.");

        var list = await notes.TableNoTracking
            .Where(n => n.SubmissionId == request.SubmissionId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);

        foreach (var n in list.Where(n => !n.SeenByCustomer))
            n.MarkSeen();
        await notes.SaveChangesAsync(ct);

        return list.Select(n => new AdminNoteRow(
            n.Id, n.SubmissionId, n.Text, n.AudioBase64, n.AudioMimeType, n.AudioSeconds, true, n.CreatedAt)).ToList();
    }
}

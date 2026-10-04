using Application.Admin;
using Application.Interfaces;
using Common.Exceptions;
using Domain.Payments;
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

/// <summary>
///     Customer follow-up in the advice chat. Requires the advice to have been
///     paid for and ownership of the submission. Text-only (voice stays staff-side).
/// </summary>
public sealed class AddCustomerReplyCommandHandler(
    ICommandRepository<SubmissionNote> notes,
    IQueryRepository<ToolSubmission> submissions,
    IQueryRepository<Payment> payments,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<AddCustomerReplyCommand, AdminNoteRow>
{
    public async Task<AdminNoteRow> Handle(AddCustomerReplyCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw ValuationException.Forbidden("دسترسی غیرمجاز.");

        if (string.IsNullOrWhiteSpace(request.Text))
            throw ValuationException.BadRequest("متن پیام الزامی است.");

        var sub = await submissions.TableNoTracking
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct)
            ?? throw ValuationException.NotFound("پاسخ یافت نشد.");

        if (sub.UserId != userId)
            throw ValuationException.Forbidden("این ارزیابی متعلق به شما نیست.");

        var paid = await payments.TableNoTracking.AnyAsync(
            p => p.UserId == userId && p.SubmissionId == sub.Id &&
                 p.Kind == PaymentKind.Advice && p.Status == PaymentStatus.Paid, ct);
        if (!paid)
            throw ValuationException.Forbidden("برای گفتگو ابتدا بررسی کارشناسی را خریداری کنید.");

        var note = SubmissionNote.CreateCustomerReply(request.SubmissionId, userId, request.Text.Trim());
        await notes.AddAsync(note, ct, saveNow: true);

        return new AdminNoteRow(note.Id, note.SubmissionId, note.Text, null, null, null, true, note.CreatedAt,
            AuthorIsCustomer: true, SeenByAdviser: false);
    }
}

/// <summary>
///     Full thread for one submission (adviser view). Opening it marks unseen
///     customer replies as read so the adviser's unread badge clears.
/// </summary>
public sealed class GetSubmissionNotesQueryHandler(ICommandRepository<SubmissionNote> notes)
    : IRequestHandler<GetSubmissionNotesQuery, List<AdminNoteRow>>
{
    public async Task<List<AdminNoteRow>> Handle(GetSubmissionNotesQuery request, CancellationToken ct)
    {
        var list = await notes.TableNoTracking
            .Where(n => n.SubmissionId == request.SubmissionId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);

        var unseenIds = list
            .Where(n => n.AuthorIsCustomer && !n.SeenByAdviser)
            .Select(n => n.Id)
            .ToList();
        if (unseenIds.Count > 0)
        {
            // Re-attach the tracked instances in ONE query so the flag change
            // persists — previously this issued a SELECT per unseen note (N+1).
            var trackedUnseen = await notes.Table
                .Where(n => unseenIds.Contains(n.Id))
                .ToListAsync(ct);
            foreach (var n in trackedUnseen) n.MarkSeenByAdviser();
            await notes.SaveChangesAsync(ct);
        }

        return list.Select(n => new AdminNoteRow(
            n.Id, n.SubmissionId, n.Text, n.AudioBase64, n.AudioMimeType, n.AudioSeconds,
            n.SeenByCustomer, n.CreatedAt, n.AuthorIsCustomer, n.SeenByAdviser || unseenIds.Contains(n.Id)))
            .ToList();
    }
}

/// <summary>
///     Customer-side: marks staff messages on their own submission as seen and
///     returns the whole thread plus entitlement (purchased / can reply).
/// </summary>
public sealed class GetMySubmissionNotesQueryHandler(
    IQueryRepository<ToolSubmission> submissions,
    ICommandRepository<SubmissionNote> notes,
    IQueryRepository<Payment> payments,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetMySubmissionNotesQuery, MyAdviceThreadResponse>
{
    public async Task<MyAdviceThreadResponse> Handle(GetMySubmissionNotesQuery request, CancellationToken ct)
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

        // Persist read receipts in ONE tracked query + save (the list above is
        // no-tracking/detached, so mutating it alone would never save). This
        // used to issue one SELECT per unseen note (N+1).
        var unseenIds = list
            .Where(n => !n.AuthorIsCustomer && !n.SeenByCustomer)
            .Select(n => n.Id)
            .ToList();
        if (unseenIds.Count > 0)
        {
            var trackedUnseen = await notes.Table
                .Where(n => unseenIds.Contains(n.Id))
                .ToListAsync(ct);
            foreach (var n in trackedUnseen) n.MarkSeen();
            await notes.SaveChangesAsync(ct);
        }

        var paid = await payments.TableNoTracking.AnyAsync(
            p => p.UserId == userId && p.SubmissionId == request.SubmissionId &&
                 p.Kind == PaymentKind.Advice && p.Status == PaymentStatus.Paid, ct);

        return new MyAdviceThreadResponse(
            request.SubmissionId,
            list.Select(n => new AdminNoteRow(
                n.Id, n.SubmissionId, n.Text, n.AudioBase64, n.AudioMimeType, n.AudioSeconds,
                true, n.CreatedAt, n.AuthorIsCustomer, n.SeenByAdviser)).ToList(),
            paid, paid);
    }
}

/// <summary>
///     The customer's most recent advice thread for a tool: their latest submission
///     that has a note or a paid advice payment. Null when none exists — the client
///     then simply renders nothing instead of an empty buy box.
/// </summary>
public sealed class GetMyToolAdviceThreadQueryHandler(
    IQueryRepository<ToolSubmission> submissions,
    IQueryRepository<SubmissionNote> notes,
    IQueryRepository<Payment> payments,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetMyToolAdviceThreadQuery, MyAdviceThreadResponse?>
{
    public async Task<MyAdviceThreadResponse?> Handle(GetMyToolAdviceThreadQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw ValuationException.Forbidden("دسترسی غیرمجاز.");

        var subIds = await submissions.TableNoTracking
            .Where(s => s.UserId == userId && s.ToolCode == request.ToolCode)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (subIds.Count == 0) return null;

        var paidSubIdSet = (await payments.TableNoTracking
                .Where(p => p.UserId == userId && p.Kind == PaymentKind.Advice &&
                            p.Status == PaymentStatus.Paid && p.SubmissionId != null && subIds.Contains(p.SubmissionId.Value))
                .Select(p => p.SubmissionId!.Value)
                .ToListAsync(ct))
            .ToHashSet();

        Guid? latest;
        if (paidSubIdSet.Count > 0)
        {
            // Newest submission with a paid advice payment.
            latest = subIds.FirstOrDefault(id => paidSubIdSet.Contains(id));
        }
        else
        {
            // Newest submission that has any note — one DISTINCT query instead
            // of an EXISTS round-trip per submission (N+1 → 1).
            var noteSubIds = (await notes.TableNoTracking
                    .Where(n => subIds.Contains(n.SubmissionId))
                    .Select(n => n.SubmissionId)
                    .Distinct()
                    .ToListAsync(ct))
                .ToHashSet();

            var firstWithNote = subIds.FirstOrDefault(id => noteSubIds.Contains(id));
            latest = firstWithNote != Guid.Empty ? firstWithNote : null;
        }

        if (latest is not { } subId) return null;

        var list = await notes.TableNoTracking
            .Where(n => n.SubmissionId == subId)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);

        var paid = paidSubIdSet.Contains(subId);

        return new MyAdviceThreadResponse(
            subId,
            list.Select(n => new AdminNoteRow(
                n.Id, n.SubmissionId, n.Text, n.AudioBase64, n.AudioMimeType, n.AudioSeconds,
                true, n.CreatedAt, n.AuthorIsCustomer, n.SeenByAdviser)).ToList(),
            paid, paid);
    }
}

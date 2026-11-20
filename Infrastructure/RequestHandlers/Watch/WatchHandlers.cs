using Application.Interfaces;
using Application.Tools;
using Common.Exceptions;
using Domain.Watch;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.DbContexts;

namespace RequestHandlers.Watch;

public sealed record WatchProfileRow(
    Guid Id, string Name, string Keywords, string? Applicants, string? Jurisdictions,
    string? NiceClasses, bool IsActive, DateTime? LastRunAt, int HitCount, int UnseenCount);

public sealed record WatchHitRow(
    Guid Id, Guid ProfileId, string Source, string Title, string? Applicant,
    string? PublicationNumber, DateTime? PublicationDate, string? Url, bool Seen, DateTime CreatedAt);

public sealed record WatchResponse(WatchProfileRow[] Profiles, WatchHitRow[] Hits, bool IsPro);

public sealed record CreateWatchProfileRequest(
    string Name, string Keywords, string? Applicants, string? Jurisdictions, string? NiceClasses);

public sealed record ImportWatchHitRequest(
    string Source, string ExternalId, string Title, string? Applicant,
    string? PublicationNumber, DateTime? PublicationDate, string? Url);

public sealed record GetWatchQuery(Guid? ProfileId) : IRequest<WatchResponse>;

public sealed record CreateWatchProfileCommand(CreateWatchProfileRequest Body) : IRequest<Guid>;

public sealed record DeleteWatchProfileCommand(Guid ProfileId) : IRequest<bool>;

public sealed record ImportWatchHitsCommand(Guid ProfileId, ImportWatchHitRequest[] Hits) : IRequest<int>;

public sealed record MarkWatchHitSeenCommand(Guid HitId) : IRequest<bool>;

internal static class WatchGuard
{
    public static async Task<(Guid CompanyId, Guid UserId)> RequireProAsync(
        ICurrentUserAccessor user, IEntitlementService entitlements, CancellationToken ct)
    {
        var companyId = user.CompanyId
            ?? throw ValuationException.BadRequest(
                "برای پایش IP ابتدا نام شرکت و حوزهٔ فعالیت را در پروفایل تکمیل کنید.");
        if (!await entitlements.IsProAsync(user.UserId, ct))
            throw ValuationException.Forbidden("پایش و هشدار IP مخصوص اشتراک Pro است.");
        return (companyId, user.UserId);
    }
}

public sealed class GetWatchQueryHandler(ValuationDbContext db, ICurrentUserAccessor currentUser, IEntitlementService entitlements)
    : IRequestHandler<GetWatchQuery, WatchResponse>
{
    public async Task<WatchResponse> Handle(GetWatchQuery request, CancellationToken ct)
    {
        if (currentUser.CompanyId is not { } companyId)
            return new WatchResponse([], [], false);

        var isPro = await entitlements.IsProAsync(currentUser.UserId, ct);

        var profiles = await db.Set<WatchProfile>().AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var hitsQuery = db.Set<WatchHit>().AsNoTracking().Where(h => h.CompanyId == companyId);
        if (request.ProfileId is { } profileId)
            hitsQuery = hitsQuery.Where(h => h.WatchProfileId == profileId);

        var hits = await hitsQuery.OrderByDescending(h => h.CreatedAt).Take(200).ToListAsync(ct);
        var allHits = await db.Set<WatchHit>().AsNoTracking()
            .Where(h => h.CompanyId == companyId)
            .Select(h => new { h.WatchProfileId, h.Seen })
            .ToListAsync(ct);

        var profileRows = profiles.Select(p =>
        {
            var mine = allHits.Where(h => h.WatchProfileId == p.Id).ToList();
            return new WatchProfileRow(
                p.Id, p.Name, p.Keywords, p.Applicants, p.Jurisdictions, p.NiceClasses,
                p.IsActive, p.LastRunAt, mine.Count, mine.Count(h => !h.Seen));
        }).ToArray();

        var hitRows = hits.Select(h => new WatchHitRow(
            h.Id, h.WatchProfileId, h.Source, h.Title, h.Applicant, h.PublicationNumber,
            h.PublicationDate, h.Url, h.Seen, h.CreatedAt)).ToArray();

        return new WatchResponse(profileRows, hitRows, isPro);
    }
}

public sealed class CreateWatchProfileCommandHandler(
    ValuationDbContext db, ICurrentUserAccessor currentUser, IEntitlementService entitlements)
    : IRequestHandler<CreateWatchProfileCommand, Guid>
{
    public async Task<Guid> Handle(CreateWatchProfileCommand request, CancellationToken ct)
    {
        var (companyId, userId) = await WatchGuard.RequireProAsync(currentUser, entitlements, ct);
        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Keywords))
            throw ValuationException.BadRequest("نام و کلیدواژه‌های پایش الزامی است.");

        var profile = new WatchProfile
        {
            CompanyId = companyId,
            CreatedByUserId = userId,
            Name = body.Name.Trim(),
            Keywords = body.Keywords.Trim(),
            Applicants = body.Applicants,
            Jurisdictions = body.Jurisdictions,
            NiceClasses = body.NiceClasses,
        };

        db.Set<WatchProfile>().Add(profile);
        await db.SaveChangesAsync(ct);
        return profile.Id;
    }
}

public sealed class DeleteWatchProfileCommandHandler(
    ValuationDbContext db, ICurrentUserAccessor currentUser, IEntitlementService entitlements)
    : IRequestHandler<DeleteWatchProfileCommand, bool>
{
    public async Task<bool> Handle(DeleteWatchProfileCommand request, CancellationToken ct)
    {
        var (companyId, _) = await WatchGuard.RequireProAsync(currentUser, entitlements, ct);
        var profile = await db.Set<WatchProfile>()
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId && p.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("پایش یافت نشد.");

        var hits = await db.Set<WatchHit>()
            .Where(h => h.WatchProfileId == profile.Id).ToListAsync(ct);
        foreach (var h in hits) db.Set<WatchHit>().Remove(h);
        db.Set<WatchProfile>().Remove(profile);

        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class ImportWatchHitsCommandHandler(
    ValuationDbContext db, ICurrentUserAccessor currentUser, IEntitlementService entitlements)
    : IRequestHandler<ImportWatchHitsCommand, int>
{
    public async Task<int> Handle(ImportWatchHitsCommand request, CancellationToken ct)
    {
        var (companyId, _) = await WatchGuard.RequireProAsync(currentUser, entitlements, ct);
        var profile = await db.Set<WatchProfile>()
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId && p.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("پایش یافت نشد.");

        if (request.Hits.Length == 0) return 0;

        var existing = await db.Set<WatchHit>().AsNoTracking()
            .Where(h => h.WatchProfileId == profile.Id)
            .Select(h => new { h.Source, h.ExternalId })
            .ToListAsync(ct);
        var seen = existing.Select(e => $"{e.Source}::{e.ExternalId}").ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = 0;
        foreach (var hit in request.Hits)
        {
            var source = string.IsNullOrWhiteSpace(hit.Source) ? "MANUAL" : hit.Source.Trim();
            var externalId = (hit.ExternalId ?? string.Empty).Trim();
            if (externalId.Length == 0 || string.IsNullOrWhiteSpace(hit.Title)) continue;
            if (!seen.Add($"{source}::{externalId}")) continue;

            db.Set<WatchHit>().Add(new WatchHit
            {
                CompanyId = companyId,
                WatchProfileId = profile.Id,
                Source = source,
                ExternalId = externalId,
                Title = hit.Title.Trim(),
                Applicant = hit.Applicant,
                PublicationNumber = hit.PublicationNumber,
                PublicationDate = hit.PublicationDate,
                Url = hit.Url,
            });
            added++;
        }

        profile.LastRunAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return added;
    }
}

public sealed class MarkWatchHitSeenCommandHandler(ValuationDbContext db, ICurrentUserAccessor currentUser)
    : IRequestHandler<MarkWatchHitSeenCommand, bool>
{
    public async Task<bool> Handle(MarkWatchHitSeenCommand request, CancellationToken ct)
    {
        if (currentUser.CompanyId is not { } companyId)
            throw ValuationException.BadRequest("ابتدا وارد حساب خود شوید.");

        var hit = await db.Set<WatchHit>()
            .FirstOrDefaultAsync(h => h.Id == request.HitId && h.CompanyId == companyId, ct)
            ?? throw ValuationException.NotFound("هشدار یافت نشد.");

        hit.Seen = true;
        hit.SeenAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

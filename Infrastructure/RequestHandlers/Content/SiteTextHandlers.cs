using System.Text.RegularExpressions;
using Application.Content;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandlers.Content;

/* Admin-managed bilingual labels.
 *
 * Every handler here owns the whole use case: validation, uniqueness and
 * persistence. The controller only maps HTTP to these requests. Texts are
 * overrides of the client's built-in defaults, so an empty TextFa/TextEn is
 * valid and simply keeps the shipped string.
 */

/// <summary>Keys are dotted lower-case segments: <c>home.hero.title</c>.</summary>
internal static partial class SiteTextRules
{
    [GeneratedRegex(@"^[a-z0-9]+(?:[.\-][a-z0-9]+)*$")]
    private static partial Regex KeyPattern();

    [GeneratedRegex(@"^[a-z0-9]+(?:[.\-][a-z0-9]+)*$")]
    private static partial Regex GroupPattern();

    public static bool IsValidKey(string key) => key.Length is >= 2 and <= 120 && KeyPattern().IsMatch(key);

    public static bool IsValidGroup(string group) => group.Length is >= 1 and <= 60 && GroupPattern().IsMatch(group);
}

public sealed class GetSiteTextsQueryHandler(IQueryRepository<SiteText> texts)
    : IRequestHandler<GetSiteTextsQuery, List<SiteTextDto>>
{
    public async Task<List<SiteTextDto>> Handle(GetSiteTextsQuery request, CancellationToken ct) =>
        await texts.TableNoTracking
            .Where(t => t.IsActive)
            .OrderBy(t => t.GroupKey).ThenBy(t => t.SortOrder).ThenBy(t => t.TextKey)
            .Select(t => new SiteTextDto(t.TextKey, t.GroupKey, t.TextFa, t.TextEn, t.SortOrder))
            .ToListAsync(ct);
}

public sealed class GetAdminSiteTextsQueryHandler(IQueryRepository<SiteText> texts)
    : IRequestHandler<GetAdminSiteTextsQuery, List<AdminSiteTextDto>>
{
    public async Task<List<AdminSiteTextDto>> Handle(GetAdminSiteTextsQuery request, CancellationToken ct) =>
        await texts.TableNoTracking
            .OrderBy(t => t.GroupKey).ThenBy(t => t.SortOrder).ThenBy(t => t.TextKey)
            .Select(t => new AdminSiteTextDto(
                t.Id, t.TextKey, t.GroupKey, t.Label, t.TextFa, t.TextEn, t.SortOrder, t.IsActive))
            .ToListAsync(ct);
}

public sealed class UpsertSiteTextCommandHandler(
    IQueryRepository<SiteText> queries,
    ICommandRepository<SiteText> commands)
    : IRequestHandler<UpsertSiteTextCommand, Guid>
{
    public async Task<Guid> Handle(UpsertSiteTextCommand req, CancellationToken ct)
    {
        var key = (req.Key ?? "").Trim().ToLowerInvariant();
        var group = (req.Group ?? "").Trim().ToLowerInvariant();
        var label = (req.Label ?? "").Trim();
        var fa = (req.Fa ?? "").Trim();
        var en = (req.En ?? "").Trim();

        if (!SiteTextRules.IsValidKey(key))
            throw new ValuationException("کلید متن باید ۲ تا ۱۲۰ نویسه و شامل حروف کوچک انگلیسی، عدد و نقطه باشد (مثل home.hero.title).");
        if (!SiteTextRules.IsValidGroup(group))
            throw new ValuationException("گروه را وارد کنید: حروف کوچک انگلیسی، عدد، نقطه یا خط تیره (حداکثر ۶۰ نویسه).");
        if (label.Length > 200)
            throw new ValuationException("توضیح راهنما حداکثر ۲۰۰ نویسه.");
        if (fa.Length > 2000 || en.Length > 2000)
            throw new ValuationException("متن هر زبان حداکثر ۲۰۰۰ نویسه.");

        var selfId = req.Id ?? Guid.Empty;

        var duplicate = await queries.TableNoTracking
            .AnyAsync(t => t.TextKey == key && t.Id != selfId, ct);
        if (duplicate)
            throw new ValuationException("این کلید قبلاً ثبت شده است؛ همان ردیف را ویرایش کنید.");

        SiteText entity;
        if (req.Id is { } id)
        {
            entity = await commands.Table.FirstOrDefaultAsync(t => t.Id == id, ct)
                     ?? throw new ValuationException("متن موردنظر یافت نشد.");
        }
        else
        {
            entity = new SiteText();
            commands.Add(entity, saveNow: false);
        }

        entity.TextKey = key;
        entity.GroupKey = group;
        entity.Label = label;
        entity.TextFa = fa;
        entity.TextEn = en;
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;

        await commands.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public sealed class DeleteSiteTextCommandHandler(ICommandRepository<SiteText> commands)
    : IRequestHandler<DeleteSiteTextCommand, bool>
{
    public async Task<bool> Handle(DeleteSiteTextCommand request, CancellationToken ct)
    {
        var entity = await commands.Table.FirstOrDefaultAsync(t => t.Id == request.Id, ct);
        if (entity is null) return false;

        commands.Delete(entity); // soft delete via the DbContext's entity rules
        await commands.SaveChangesAsync(ct);
        return true;
    }
}

using System.Text.Json;
using Application.Admin;
using Application.Interfaces;
using Application.Tools;
using Application.Interfaces.Base;
using Common.Exceptions;
using Domain.Payments;
using Domain.Tools;
using Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace RequestHandler.Admin;

/// <summary>
///     Tool content management (editable question texts / option labels / tool title)
///     and the company-members read for the admin "see members" modal.
///     Reads fall back to seeded content; edits persist in ToolQuestion rows.
/// </summary>
public static class AdminToolContentHandlers
{
    // ─── GET /api/admin/tools/{code}/questions ──────────────────────────

    public sealed class GetAdminToolQuestionsQueryHandler(
        Application.Interfaces.IToolQuestionQueries questions,
        IToolContentQueries localizations,
        IQueryRepository<ToolForm> tools)
        : IRequestHandler<GetAdminToolQuestionsQuery, List<AdminQuestionRow>>
    {
        public async Task<List<AdminQuestionRow>> Handle(GetAdminToolQuestionsQuery request, CancellationToken ct)
        {
            await EnsureToolExistsAsync(request.ToolCode, tools, ct);

            var rows = await questions.ByTool(request.ToolCode)
                .OrderBy(q => q.SortOrder)
                .ToListAsync(ct);

            var en = (await localizations.ByTool(request.ToolCode)
                    .Where(l => l.Language == "en")
                    .ToListAsync(ct))
                .ToDictionary(l => l.QuestionId, StringComparer.Ordinal);

            return rows.Select(q => ToAdminRow(q, en.GetValueOrDefault(q.QuestionId))).ToList();
        }
    }

    // ─── GET /api/admin/tools/{code}/content ─────────────────────────────

    public sealed class GetAdminToolContentQueryHandler(
        Application.Interfaces.IToolQuestionQueries questions,
        IToolContentQueries localizations,
        IQueryRepository<ToolForm> tools)
        : IRequestHandler<GetAdminToolContentQuery, AdminToolContentRow>
    {
        public async Task<AdminToolContentRow> Handle(GetAdminToolContentQuery request, CancellationToken ct)
        {
            var form = await tools.TableNoTracking
                .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct)
                ?? throw ValuationException.NotFound("ابزار یافت نشد.");

            var rows = await questions.ByTool(request.ToolCode)
                .OrderBy(q => q.SortOrder)
                .ToListAsync(ct);

            var en = (await localizations.ByTool(request.ToolCode)
                    .Where(l => l.Language == "en")
                    .ToListAsync(ct))
                .ToDictionary(l => l.QuestionId, StringComparer.Ordinal);

            var info = en.GetValueOrDefault(ToolLocalizationBase.ToolLevelQuestionId);

            return new AdminToolContentRow(
                request.ToolCode,
                form.Title, info?.Title,
                form.Description, info?.Description,
                rows.Select(q => ToAdminRow(q, en.GetValueOrDefault(q.QuestionId))).ToList());
        }
    }

    // ─── PUT /api/admin/tools/{code}/questions/{questionId} ─────────────

    public sealed class UpdateAdminToolQuestionCommandHandler(
        Application.Interfaces.IToolQuestionStore questions,
        IToolLocalizationStore localizations,
        IQueryRepository<ToolForm> tools)
        : IRequestHandler<UpdateAdminToolQuestionCommand, AdminQuestionRow>
    {
        public async Task<AdminQuestionRow> Handle(UpdateAdminToolQuestionCommand request, CancellationToken ct)
        {
            await EnsureToolExistsAsync(request.ToolCode, tools, ct);

            var entity = await questions.FindAsync(request.ToolCode, request.QuestionId, ct)
                ?? throw ValuationException.NotFound("سوال یافت نشد.");

            // ── fa base columns ──
            if (request.Text is { } text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw ValuationException.BadRequest("متن سوال نمی‌تواند خالی باشد.");
                entity.UpdateText(text.Trim());
            }
            if (request.SectionTitle is { } sectionTitle) entity.SectionTitle = NullIfEmpty(sectionTitle);
            if (request.Placeholder is { } placeholder) entity.Placeholder = NullIfEmpty(placeholder);
            if (request.HelpText is { } helpText) entity.HelpText = NullIfEmpty(helpText);
            if (request.Options is { } options)
                entity.UpdateOptions(options.Select(o => (o.Value, o.Label)));

            // ── en localization row (null = fall back to fa) ──
            var touchesEn = request.TextEn is not null || request.SectionTitleEn is not null
                || request.PlaceholderEn is not null || request.HelpTextEn is not null
                || request.OptionsEn is not null;

            ToolLocalizationBase? loc = null;
            if (touchesEn)
            {
                loc = await localizations.FindAsync(request.ToolCode, "en", request.QuestionId, ct)
                    ?? localizations.Create(request.ToolCode, "en", request.QuestionId);

                if (request.TextEn is { } textEn) loc.Text = NullIfEmpty(textEn);
                if (request.SectionTitleEn is { } sectionTitleEn) loc.SectionTitle = NullIfEmpty(sectionTitleEn);
                if (request.PlaceholderEn is { } placeholderEn) loc.Placeholder = NullIfEmpty(placeholderEn);
                if (request.HelpTextEn is { } helpTextEn) loc.HelpText = NullIfEmpty(helpTextEn);
                if (request.OptionsEn is { } optionsEn)
                    loc.OptionsJson = optionsEn.Count == 0 ? null : SerializeOptions(optionsEn);
            }

            await questions.SaveChangesAsync(ct);

            var locRow = loc is null
                ? (await localizations.FindAsync(request.ToolCode, "en", request.QuestionId, ct))
                : null;

            var optionsFa = entity.Options().Select(o => new QuestionOptionRow(o.Value, o.Label)).ToList();
            var optionsEnRows = QuestionOptions.Parse(loc?.OptionsJson ?? locRow?.OptionsJson)
                .Select(o => new QuestionOptionRow(o.Value, o.Label)).ToList();

            return new AdminQuestionRow(
                entity.QuestionId, entity.SectionKey,
                entity.SectionTitle, loc?.SectionTitle ?? locRow?.SectionTitle,
                entity.Text, loc?.Text ?? locRow?.Text ?? "",
                entity.Placeholder, loc?.Placeholder ?? locRow?.Placeholder,
                entity.HelpText, loc?.HelpText ?? locRow?.HelpText,
                optionsFa, optionsEnRows);
        }
    }

    // ─── PUT /api/admin/tools/{code} — tool-level fields ─────────────────

    public sealed class UpdateAdminToolCommandHandler(
        ICommandRepository<ToolForm> tools,
        IQueryRepository<ToolForm> toolQueries,
        IToolLocalizationStore localizations)
        : IRequestHandler<UpdateAdminToolCommand, AdminToolRow>
    {
        public async Task<AdminToolRow> Handle(UpdateAdminToolCommand request, CancellationToken ct)
        {
            var tool = await tools.Table
                .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct)
                ?? throw ValuationException.NotFound("ابزار یافت نشد.");

            if (!string.IsNullOrWhiteSpace(request.Title))
                tool.Title = request.Title.Trim();
            if (request.Description is not null)
                tool.Description = request.Description.Trim() is { Length: > 0 } d ? d : null;

            if (request.TitleEn is not null || request.DescriptionEn is not null)
            {
                var info = await localizations.FindAsync(
                        request.ToolCode, "en", ToolLocalizationBase.ToolLevelQuestionId, ct)
                    ?? localizations.Create(request.ToolCode, "en", ToolLocalizationBase.ToolLevelQuestionId);

                if (request.TitleEn is { } titleEn) info.Title = NullIfEmpty(titleEn);
                if (request.DescriptionEn is { } descriptionEn) info.Description = NullIfEmpty(descriptionEn);
            }

            await tools.SaveChangesAsync(ct);
            await localizations.SaveChangesAsync(ct);

            var uses = await toolQueries.TableNoTracking
                .Where(t => t.ToolCode == tool.ToolCode)
                .CountAsync(ct);

            return new AdminToolRow(tool.ToolCode, tool.Title,
                tool.Kind == ToolKind.Assessment ? "Assessment" : "Calculator", uses, 0);
        }
    }

    // ─── PUT /api/admin/tools/{code}/prices — pricing builder ────────────

    public sealed class UpdateAdminToolPricesCommandHandler(
        ICommandRepository<ToolForm> tools)
        : IRequestHandler<UpdateAdminToolPricesCommand, AdminToolPriceRow>
    {
        public async Task<AdminToolPriceRow> Handle(UpdateAdminToolPricesCommand request, CancellationToken ct)
        {
            if (request.AdvancedPriceToman < 0 || request.AdvicePriceToman < 0)
                throw ValuationException.BadRequest("قیمت نمی‌تواند منفی باشد.");

            var tool = await tools.Table
                .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct)
                ?? throw ValuationException.NotFound("ابزار یافت نشد.");

            tool.AdvancedPriceToman = request.AdvancedPriceToman;
            tool.AdvicePriceToman = request.AdvicePriceToman;
            await tools.SaveChangesAsync(ct);

            return new AdminToolPriceRow(
                tool.ToolCode, tool.Title, tool.AdvancedPriceToman, tool.AdvicePriceToman);
        }
    }

    // ─── GET /api/admin/companies/{id}/members ─────────────────────────

    public sealed class GetAdminCompanyMembersQueryHandler(
        IQueryRepository<Domain.Users.ApplicationUser> users,
        Application.Interfaces.IToolSubmissionQueries submissions,
        IQueryRepository<Payment> payments)
        : IRequestHandler<GetAdminCompanyMembersQuery, List<AdminCompanyMemberRow>>
    {
        public async Task<List<AdminCompanyMemberRow>> Handle(GetAdminCompanyMembersQuery request, CancellationToken ct)
        {
            var members = await users.TableNoTracking
                .Where(u => u.CompanyId == request.CompanyId)
                .OrderBy(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id, u.DisplayName, u.Email, u.Plan, u.IsActive, u.CreatedAt, u.LastLoginAt,
                })
                .ToListAsync(ct);

            if (members.Count == 0) return [];

            var ids = members.Select(m => m.Id).ToList();

            var subsByUser = await submissions.All
                .Where(s => ids.Contains(s.UserId))
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Count = g.LongCount() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

            var paidByUser = await payments.TableNoTracking
                .Where(p => ids.Contains(p.UserId) && p.Status == PaymentStatus.Paid)
                .GroupBy(p => p.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
                .ToDictionaryAsync(x => x.UserId, x => x.Total, ct);

            return members.Select(m => new AdminCompanyMemberRow(
                m.Id,
                m.DisplayName,
                m.Email ?? "",
                m.Plan.ToString(),
                m.IsActive,
                subsByUser.TryGetValue(m.Id, out var s) ? s : 0,
                paidByUser.TryGetValue(m.Id, out var t) ? t : 0,
                m.CreatedAt,
                m.LastLoginAt)).ToList();
        }
    }

    private static async Task EnsureToolExistsAsync(
        string toolCode, IQueryRepository<ToolForm> tools, CancellationToken ct)
    {
        if (!await tools.TableNoTracking.AnyAsync(t => t.ToolCode == toolCode, ct))
            throw ValuationException.NotFound("ابزار یافت نشد.");
    }

    private static AdminQuestionRow ToAdminRow(ToolQuestionRow q, ToolLocalizationRow? en) =>
        new(
            q.QuestionId, q.SectionKey,
            q.SectionTitle, en?.SectionTitle,
            q.Text, en?.Text ?? "",
            q.Placeholder, en?.Placeholder,
            q.HelpText, en?.HelpText,
            QuestionOptions.Parse(q.OptionsJson)
                .Select(o => new QuestionOptionRow(o.Value, o.Label)).ToList(),
            QuestionOptions.Parse(en?.OptionsJson)
                .Select(o => new QuestionOptionRow(o.Value, o.Label)).ToList());

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SerializeOptions(IEnumerable<QuestionOptionRow> options) =>
        JsonSerializer.Serialize(
            options.Where(o => !string.IsNullOrWhiteSpace(o.Value))
                .Select(o => new { value = o.Value, label = o.Label }),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
}

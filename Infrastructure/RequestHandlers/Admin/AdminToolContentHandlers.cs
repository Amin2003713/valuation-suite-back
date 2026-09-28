using Application.Admin;
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
        IQueryRepository<ToolQuestion> questions,
        IQueryRepository<ToolForm> tools)
        : IRequestHandler<GetAdminToolQuestionsQuery, List<AdminQuestionRow>>
    {
        public async Task<List<AdminQuestionRow>> Handle(GetAdminToolQuestionsQuery request, CancellationToken ct)
        {
            await EnsureToolExistsAsync(request.ToolCode, tools, ct);

            var rows = await questions.TableNoTracking
                .Where(q => q.ToolCode == request.ToolCode)
                .OrderBy(q => q.SortOrder)
                .ToListAsync(ct);

            return rows.Select(q => new AdminQuestionRow(
                q.QuestionId, q.SectionKey, q.SectionTitle, q.Text,
                q.Options().Select(o => new QuestionOptionRow(o.Value, o.Label)).ToList())).ToList();
        }
    }

    // ─── PUT /api/admin/tools/{code}/questions/{questionId} ─────────────

    public sealed class UpdateAdminToolQuestionCommandHandler(
        ICommandRepository<ToolQuestion> questions,
        IQueryRepository<ToolQuestion> questionQueries,
        IQueryRepository<ToolForm> tools)
        : IRequestHandler<UpdateAdminToolQuestionCommand, AdminQuestionRow>
    {
        public async Task<AdminQuestionRow> Handle(UpdateAdminToolQuestionCommand request, CancellationToken ct)
        {
            await EnsureToolExistsAsync(request.ToolCode, tools, ct);

            var entity = await questions.Table
                .FirstOrDefaultAsync(q => q.ToolCode == request.ToolCode && q.QuestionId == request.QuestionId, ct)
                ?? throw ValuationException.NotFound("سوال یافت نشد.");

            if (request.Text is { } text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw ValuationException.BadRequest("متن سوال نمی‌تواند خالی باشد.");
                entity.UpdateText(text.Trim());
            }

            if (request.Options is { } options)
                entity.UpdateOptions(options.Select(o => (o.Value, o.Label)));

            await questions.SaveChangesAsync(ct);

            return new AdminQuestionRow(
                entity.QuestionId, entity.SectionKey, entity.SectionTitle, entity.Text,
                entity.Options().Select(o => new QuestionOptionRow(o.Value, o.Label)).ToList());
        }
    }

    // ─── PUT /api/admin/tools/{code} — tool-level fields ─────────────────

    public sealed class UpdateAdminToolCommandHandler(
        ICommandRepository<ToolForm> tools,
        IQueryRepository<ToolForm> toolQueries)
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

            await tools.SaveChangesAsync(ct);

            var uses = await toolQueries.TableNoTracking
                .Where(t => t.ToolCode == tool.ToolCode)
                .CountAsync(ct);

            return new AdminToolRow(tool.ToolCode, tool.Title,
                tool.Kind == ToolKind.Assessment ? "Assessment" : "Calculator", uses, 0);
        }
    }

    // ─── GET /api/admin/companies/{id}/members ───────────────────────────

    public sealed class GetAdminCompanyMembersQueryHandler(
        IQueryRepository<Domain.Users.ApplicationUser> users,
        IQueryRepository<ToolSubmission> submissions,
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

            var subsByUser = await submissions.TableNoTracking
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
}

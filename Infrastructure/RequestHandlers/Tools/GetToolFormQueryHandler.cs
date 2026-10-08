using Application.Tools;
using Common.Exceptions;
using Domain.Tools;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Interfaces.Base;

namespace RequestHandlers.Tools;

public sealed class GetToolFormQueryHandler(
    IQueryRepository<ToolForm> forms,
    IQueryRepository<ToolQuestion> questions)
    : IRequestHandler<GetToolFormQuery, ToolFormResponse?>
{
    public async Task<ToolFormResponse?> Handle(GetToolFormQuery request, CancellationToken ct)
    {
        var form = await forms.TableNoTracking
            .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct);

        if (form is null)
            throw Common.Exceptions.ValuationException.NotFound($"ابزار «{request.ToolCode}» یافت نشد.");

        var schema = JsonSerializer.Deserialize<JsonElement>(form.SchemaJson);

        return new ToolFormResponse(
            form.ToolCode, form.Title, form.Description,
            form.Kind.ToString(), await MergeQuestionsAsync(schema, request.ToolCode, ct),
            form.Route, form.AdvancedPriceToman, form.AdvicePriceToman);
    }

    /// <summary>
    ///     Merges the editable ToolQuestion rows into the form schema so clients
    ///     always render the current (admin-edited) question texts and option
    ///     labels. The schema gains a unified <c>questions</c> array:
    ///     [{ id, sectionKey, sectionTitle, text, options:[{value,label}] }]
    ///     ordered by SortOrder. Tools without question rows keep their schema as-is.
    /// </summary>
    private async Task<JsonElement> MergeQuestionsAsync(
        JsonElement schema, string toolCode, CancellationToken ct)
    {
        var rows = await questions.TableNoTracking
            .Where(q => q.ToolCode == toolCode)
            .OrderBy(q => q.SortOrder)
            .ToListAsync(ct);

        if (rows.Count == 0) return schema;

        var node = JsonNode.Parse(schema.GetRawText()) as JsonObject;
        if (node is null) return schema;

        var dbQuestions = new JsonArray();
        foreach (var q in rows)
        {
            var options = new JsonArray();
            foreach (var (value, label) in q.Options())
                options.Add(new JsonObject { ["value"] = value, ["label"] = label });

            dbQuestions.Add(new JsonObject
            {
                ["id"] = q.QuestionId,
                ["sectionKey"] = q.SectionKey,
                ["sectionTitle"] = q.SectionTitle,
                ["text"] = q.Text,
                ["options"] = options,
            });
        }

        node["questions"] = dbQuestions;
        return JsonSerializer.SerializeToElement(node);
    }
}

public sealed class GetToolFormsQueryHandler(IQueryRepository<ToolForm> forms)
    : IRequestHandler<GetToolFormsQuery, List<ToolFormSummary>>
{
    public async Task<List<ToolFormSummary>> Handle(GetToolFormsQuery request, CancellationToken ct)
    {
        var list = await forms.TableNoTracking
            .OrderBy(t => t.SortOrder)
            .Select(t => new ToolFormSummary(
                t.ToolCode, t.Title, t.Description, t.Kind.ToString(), t.SortOrder,
                t.Route, t.AdvancedPriceToman, t.AdvicePriceToman))
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

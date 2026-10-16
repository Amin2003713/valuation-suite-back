using Application.Tools;
using Domain.Tools;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Interfaces;
using Application.Interfaces.Base;

namespace RequestHandlers.Tools;

public sealed class GetToolFormQueryHandler(
    IQueryRepository<ToolForm> forms,
    IToolContentResolver content) : IRequestHandler<GetToolFormQuery, ToolFormResponse?>
{
    public async Task<ToolFormResponse?> Handle(GetToolFormQuery request, CancellationToken ct)
    {
        var form = await forms.TableNoTracking
            .FirstOrDefaultAsync(t => t.ToolCode == request.ToolCode, ct);

        if (form is null)
            throw Common.Exceptions.ValuationException.NotFound($"ابزار «{request.ToolCode}» یافت نشد.");

        var schema = JsonSerializer.Deserialize<JsonElement>(form.SchemaJson);

        /* Tool info + questions, localized for the request language with per-field
         * fa fallback (per-tool <Tool>Questions / <Tool>Localizations tables). */
        var localized = await content.GetAsync(form.ToolCode, ct);

        return new ToolFormResponse(
            form.ToolCode, localized.Title, localized.Description,
            form.Kind.ToString(), MergeQuestions(schema, localized),
            form.Route, form.AdvancedPriceToman, form.AdvicePriceToman,
            localized.Lang);
    }

    /// <summary>
    ///     Merges the localized question rows into the form schema so clients always
    ///     render the current texts in the request language. The schema gains a unified
    ///     <c>questions</c> array:
    ///     [{ id, sectionKey, sectionTitle, text, placeholder, helpText,
    ///        answerType, required, min, max, options:[{value,label}] }]
    ///     ordered by SortOrder. Tools without questions keep their schema as-is.
    /// </summary>
    private static JsonElement MergeQuestions(JsonElement schema, LocalizedToolContent localized)
    {
        if (localized.Questions.Count == 0) return schema;

        var node = JsonNode.Parse(schema.GetRawText()) as JsonObject;
        if (node is null) return schema;

        var dbQuestions = new JsonArray();
        foreach (var q in localized.Questions)
        {
            var options = new JsonArray();
            foreach (var (value, label) in q.Options)
                options.Add(new JsonObject { ["value"] = value, ["label"] = label });

            dbQuestions.Add(new JsonObject
            {
                ["id"] = q.Id,
                ["sectionKey"] = q.SectionKey,
                ["sectionTitle"] = q.SectionTitle,
                ["text"] = q.Text,
                ["placeholder"] = q.Placeholder,
                ["helpText"] = q.HelpText,
                ["answerType"] = q.AnswerType,
                ["required"] = q.Required,
                ["min"] = q.Min,
                ["max"] = q.Max,
                ["options"] = options,
            });
        }

        node["questions"] = dbQuestions;
        return JsonSerializer.SerializeToElement(node);
    }
}

public sealed class GetToolFormsQueryHandler(
    IQueryRepository<ToolForm> forms,
    IToolContentQueries localizations,
    Application.Interfaces.ILanguageResolver language)
    : IRequestHandler<GetToolFormsQuery, List<ToolFormSummary>>
{
    public async Task<List<ToolFormSummary>> Handle(GetToolFormsQuery request, CancellationToken ct)
    {
        var lang = language.Lang;
        var rows = await forms.TableNoTracking
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct);

        /* One extra query for every tool-level localization row of the served
         * language (QuestionId == "" — the tool info row of each tool's table). */
        var infoByTool = (await localizations.All
                .Where(l => l.QuestionId == ToolLocalizationBase.ToolLevelQuestionId && l.Language == lang)
                .ToListAsync(ct))
            .ToDictionary(l => l.ToolCode, StringComparer.OrdinalIgnoreCase);

        return rows.Select(t =>
        {
            infoByTool.TryGetValue(t.ToolCode, out var info);
            return new ToolFormSummary(
                t.ToolCode,
                info?.Title ?? t.Title,
                info?.Description ?? t.Description,
                t.Kind.ToString(), t.SortOrder,
                t.Route, t.AdvancedPriceToman, t.AdvicePriceToman,
                lang);
        })
        .ToList();
    }
}

public sealed class GetToolSubmissionsQueryHandler(
    IToolSubmissionQueries submissions,
    Application.Interfaces.ICurrentUserAccessor currentUser)
    : IRequestHandler<GetToolSubmissionsQuery, List<ToolSubmissionResponse>>
{
    public async Task<List<ToolSubmissionResponse>> Handle(GetToolSubmissionsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw Common.Exceptions.ValuationException.Forbidden("برای مشاهده تاریخچه ابتدا وارد حساب خود شوید.");

        var baseQuery = string.IsNullOrWhiteSpace(request.ToolCode)
            ? submissions.All
            : submissions.ByTool(request.ToolCode);
        var query = baseQuery.Where(s => s.UserId == userId);

        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return rows.Select(s => new ToolSubmissionResponse(
            s.Id, s.ToolCode, s.Name,
            JsonSerializer.Deserialize<JsonElement>(s.InputJson),
            JsonSerializer.Deserialize<JsonElement>(s.ResultJson),
            s.OverallScore, s.CreatedAt, s.Language, s.Status.ToString()))
            .ToList();
    }
}

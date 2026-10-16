using System.Reflection;
using Application.Interfaces;
using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Persistence.DbContexts;

namespace Persistence.Repositories;

/// <summary>
///     Context-bound union read model over the per-tool question tables
///     (same pattern as <see cref="ToolSubmissionQuery"/> — EF requires every part of a
///     composed query to come from the same DbContext).
/// </summary>
public static class ToolQuestionQuery
{
    public static IQueryable<ToolQuestionRow> All(DbContext db)
    {
        IQueryable<ToolQuestionRow>? result = null;
        foreach (var type in ToolRegistry.QuestionTypes)
        {
            var part = ProjectSet(db, type);
            result = result is null ? part : result.Concat(part);
        }

        return result ?? Queryable.AsQueryable(Array.Empty<ToolQuestionRow>());
    }

    public static IQueryable<ToolQuestionRow> ByTool(DbContext db, string toolCode)
    {
        if (!ToolRegistry.TryGetQuestion(toolCode, out var type))
            return Queryable.AsQueryable(Array.Empty<ToolQuestionRow>());

        return ProjectSet(db, type);
    }

    private static IQueryable<ToolQuestionRow> ProjectSet(DbContext context, Type entityType)
    {
        var set = (IQueryable)typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType)
            .Invoke(context, null)!;

        return (IQueryable<ToolQuestionRow>)typeof(ToolQuestionQuery)
            .GetMethod(nameof(Project), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType)
            .Invoke(null, [set])!;
    }

    private static IQueryable<ToolQuestionRow> Project<T>(IQueryable<T> set) where T : ToolQuestionBase =>
        set.Select(q => new ToolQuestionRow
        {
            Id = q.Id,
            ToolCode = q.ToolCode,
            QuestionId = q.QuestionId,
            SectionKey = q.SectionKey,
            SectionTitle = q.SectionTitle,
            Text = q.Text,
            Placeholder = q.Placeholder,
            HelpText = q.HelpText,
            OptionsJson = q.OptionsJson,
            AnswerType = q.AnswerType,
            IsRequired = q.IsRequired,
            ScaleMin = q.ScaleMin,
            ScaleMax = q.ScaleMax,
            SortOrder = q.SortOrder,
        });
}

/// <summary>Context-bound union read model over the per-tool localization tables.</summary>
public static class ToolLocalizationQuery
{
    public static IQueryable<ToolLocalizationRow> All(DbContext db)
    {
        IQueryable<ToolLocalizationRow>? result = null;
        foreach (var type in ToolRegistry.LocalizationTypes)
        {
            var part = ProjectSet(db, type);
            result = result is null ? part : result.Concat(part);
        }

        return result ?? Queryable.AsQueryable(Array.Empty<ToolLocalizationRow>());
    }

    public static IQueryable<ToolLocalizationRow> ByTool(DbContext db, string toolCode)
    {
        if (!ToolRegistry.TryGetLocalization(toolCode, out var type))
            return Queryable.AsQueryable(Array.Empty<ToolLocalizationRow>());

        return ProjectSet(db, type);
    }

    private static IQueryable<ToolLocalizationRow> ProjectSet(DbContext context, Type entityType)
    {
        var set = (IQueryable)typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType)
            .Invoke(context, null)!;

        return (IQueryable<ToolLocalizationRow>)typeof(ToolLocalizationQuery)
            .GetMethod(nameof(Project), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType)
            .Invoke(null, [set])!;
    }

    private static IQueryable<ToolLocalizationRow> Project<T>(IQueryable<T> set) where T : ToolLocalizationBase =>
        set.Select(l => new ToolLocalizationRow
        {
            Id = l.Id,
            ToolCode = l.ToolCode,
            Language = l.Language,
            QuestionId = l.QuestionId,
            Title = l.Title,
            Description = l.Description,
            Text = l.Text,
            SectionTitle = l.SectionTitle,
            Placeholder = l.Placeholder,
            HelpText = l.HelpText,
            OptionsJson = l.OptionsJson,
        });
}

/// <summary><see cref="IToolQuestionQueries"/> over the scoped ValuationDbContext.</summary>
public sealed class ToolQuestionQueries(ValuationDbContext db) : IToolQuestionQueries
{
    public IQueryable<ToolQuestionRow> All => ToolQuestionQuery.All(db);

    public IQueryable<ToolQuestionRow> ByTool(string toolCode) => ToolQuestionQuery.ByTool(db, toolCode);
}

/// <summary><see cref="IToolContentQueries"/> over the scoped ValuationDbContext.</summary>
public sealed class ToolContentQueries(ValuationDbContext db) : IToolContentQueries
{
    public IQueryable<ToolLocalizationRow> All => ToolLocalizationQuery.All(db);

    public IQueryable<ToolLocalizationRow> ByTool(string toolCode) => ToolLocalizationQuery.ByTool(db, toolCode);
}

/// <summary>
///     Tracked per-tool question access for admin edits. The tool's question table is
///     small (≤ ~70 rows), so a tracked materialize + in-memory match keeps the
///     runtime-type lookup simple while SaveChanges still persists edits.
/// </summary>
public sealed class ToolQuestionStore(ValuationDbContext db) : IToolQuestionStore
{
    public async Task<ToolQuestionBase?> FindAsync(string toolCode, string questionId, CancellationToken ct = default)
    {
        if (!ToolRegistry.TryGetQuestion(toolCode, out var type))
            return null;

        var set = (IQueryable)typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(type)
            .Invoke(db, null)!;

        var rows = await set
            .Cast<ToolQuestionBase>()
            .ToListAsync(ct);

        return rows.FirstOrDefault(q => q.QuestionId == questionId);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

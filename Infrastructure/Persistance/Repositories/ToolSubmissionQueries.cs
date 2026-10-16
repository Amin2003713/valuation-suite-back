using System.Reflection;
using Application.Interfaces;
using Domain.Tools;
using Microsoft.EntityFrameworkCore;
using Persistence.DbContexts;

namespace Persistence.Repositories;

/// <summary>
///     Union read model over the per-tool submission tables (one table per tool).
///     <see cref="All(DbContext)"/> is a UNION ALL built by projecting every registered
///     per-tool entity into the shared <see cref="ToolSubmissionRow"/> shape (property
///     names identical to <see cref="ToolSubmissionBase"/>, so query bodies port 1:1
///     from the legacy shared table); <see cref="ByTool(DbContext, string)"/> hits only
///     the one tool's table.
///
///     Static + context-bound on purpose: EF Core requires every part of a composed
///     query to come from the SAME DbContext, and this repository family uses several
///     contexts (ReadOnly/WriteOnly/Valuation). Callers pass their own <c>Db</c>.
/// </summary>
public static class ToolSubmissionQuery
{
    public static IQueryable<ToolSubmissionRow> All(DbContext db)
    {
        IQueryable<ToolSubmissionRow>? result = null;
        foreach (var type in ToolRegistry.SubmissionTypes)
        {
            var part = ProjectSet(db, type);
            result = result is null ? part : result.Concat(part);
        }

        return result ?? Queryable.AsQueryable(Array.Empty<ToolSubmissionRow>());
    }

    public static IQueryable<ToolSubmissionRow> ByTool(DbContext db, string toolCode)
    {
        if (!ToolRegistry.TryGetSubmission(toolCode, out var type))
            return Queryable.AsQueryable(Array.Empty<ToolSubmissionRow>());

        return ProjectSet(db, type);
    }

    /// <summary>db.Set&lt;T&gt;() for a runtime entity type, projected to the row shape.</summary>
    private static IQueryable<ToolSubmissionRow> ProjectSet(DbContext context, Type entityType)
    {
        var set = (IQueryable)typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType)
            .Invoke(context, null)!;

        return (IQueryable<ToolSubmissionRow>)typeof(ToolSubmissionQuery)
            .GetMethod(nameof(Project), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType)
            .Invoke(null, [set])!;
    }

    /// <summary>
    ///     Shared projection: every per-tool table has identical columns (from
    ///     ToolSubmissionBase), so one generic projection covers all 23 tables.
    /// </summary>
    private static IQueryable<ToolSubmissionRow> Project<T>(IQueryable<T> set) where T : ToolSubmissionBase =>
        set.Select(s => new ToolSubmissionRow
        {
            Id = s.Id,
            IsActive = s.IsActive,
            CreatedBy = s.CreatedBy,
            CreatedAt = s.CreatedAt,
            ModifiedBy = s.ModifiedBy,
            ModifiedAt = s.ModifiedAt,
            DeletedBy = s.DeletedBy,
            DeletedAt = s.DeletedAt,
            UserId = s.UserId,
            ToolCode = s.ToolCode,
            Name = s.Name,
            InputJson = s.InputJson,
            ResultJson = s.ResultJson,
            OverallScore = s.OverallScore,
            Language = s.Language,
            Status = s.Status,
            CompanyId = s.CompanyId,
            CompanyName = s.CompanyName,
            Industry = s.Industry,
        });
}

/// <summary><see cref="IToolSubmissionQueries"/> over the scoped ValuationDbContext.</summary>
public sealed class ToolSubmissionQueries(ValuationDbContext db) : IToolSubmissionQueries
{
    public IQueryable<ToolSubmissionRow> All => ToolSubmissionQuery.All(db);

    public IQueryable<ToolSubmissionRow> ByTool(string toolCode) => ToolSubmissionQuery.ByTool(db, toolCode);
}

/// <summary>
///     <see cref="IToolSubmissionStore"/>: writes a run into its per-tool table.
///     EF resolves the target set from the row's runtime type (one of the
///     ToolSubmissionRegistry entities); the shared columns come from the base class.
/// </summary>
public sealed class ToolSubmissionStore(ValuationDbContext db) : IToolSubmissionStore
{
    public async Task AddAsync(ToolSubmissionBase row, CancellationToken ct = default)
    {
        await db.AddAsync(row, ct);
        await db.SaveChangesAsync(ct);
    }
}

using Domain.Tools;

namespace Application.Interfaces;

/// <summary>
///     Write path for per-tool submissions: adds the (already typed) row to its
///     own table. Callers build rows through <see cref="ToolSubmissionRegistry.Create"/>.
/// </summary>
public interface IToolSubmissionStore
{
    Task AddAsync(ToolSubmissionBase row, CancellationToken ct = default);
}

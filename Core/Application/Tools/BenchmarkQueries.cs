using MediatR;

namespace Application.Tools;

/// <summary>Aggregated stats for one metric across a bucket of submissions.</summary>
public record BenchmarkStat(double Min, double Max, double Avg, int Count);

/// <summary>
///     Benchmark data for the spider-web chart: per-metric min/max/avg within the
///     caller's company industry, plus the same stats across ALL submissions of
///     the tool (every industry) and the per-industry submission count.
/// </summary>
public record ToolBenchmarkResponse(
    string ToolCode,
    string? Industry,
    int AreaCount,
    int OverallCount,
    Dictionary<string, BenchmarkStat> Area,
    Dictionary<string, BenchmarkStat> Overall);

/// <summary>
///     Asks for benchmark statistics of one tool. Results are read from stored
///     submissions (ResultJson) and grouped by the industry snapshot taken at
///     each run. When <see cref="Industry"/> is null the caller gets only the
///     overall buckets.
/// </summary>
public sealed record GetToolBenchmarkQuery(string ToolCode, Guid UserId) : IRequest<ToolBenchmarkResponse>;

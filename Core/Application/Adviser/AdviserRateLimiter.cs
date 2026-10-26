using System.Collections.Concurrent;

namespace Application.Adviser;

/// <summary>
///     Per-user sliding-window rate limiter (in-memory, single-node).
///     Guards the public admission chat from abuse; enough for the feature and
///     does not add a distributed-cache dependency.
/// </summary>
public interface IAdviserRateLimiter
{
    /// <summary>True when the call is allowed; false when the user exceeded the budget.</summary>
    bool TryAcquire(Guid userId);
}

public sealed class AdviserRateLimiter : IAdviserRateLimiter
{
    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> _hits = new();
    private readonly int _maxPerWindow;
    private readonly TimeSpan _window;

    /// <param name="maxPerWindow">Default: 20 asks per minute per user.</param>
    public AdviserRateLimiter(int maxPerWindow = 20)
    {
        _maxPerWindow = maxPerWindow;
        _window = TimeSpan.FromMinutes(1);
    }

    public bool TryAcquire(Guid userId)
    {
        if (userId == Guid.Empty) return false;
        var now = DateTimeOffset.UtcNow;
        var q = _hits.GetOrAdd(userId, _ => new Queue<DateTimeOffset>());
        lock (q)
        {
            while (q.Count > 0 && now - q.Peek() > _window) q.Dequeue();
            if (q.Count >= _maxPerWindow) return false;
            q.Enqueue(now);
            return true;
        }
    }
}

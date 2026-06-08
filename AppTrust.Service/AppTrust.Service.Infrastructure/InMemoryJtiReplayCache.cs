using System.Collections.Concurrent;
using AppTrust.Service.Contracts;

namespace AppTrust.Service.Infrastructure;

public sealed class InMemoryJtiReplayCache : IJtiReplayCache
{
    private readonly ConcurrentDictionary<string, DateTime> _consumed = new();

    public bool TryConsume(string jti, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return false;

        PurgeExpired();

        var added = _consumed.TryAdd(jti, expiresAtUtc);
        return added;
    }

    private void PurgeExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in _consumed)
        {
            if (entry.Value <= now)
                _consumed.TryRemove(entry.Key, out _);
        }
    }
}

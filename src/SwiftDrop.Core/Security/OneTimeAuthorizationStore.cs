using System.Collections.Concurrent;

namespace SwiftDrop.Core.Security;

public sealed class OneTimeAuthorizationStore
{
    private readonly ConcurrentDictionary<string, long> _entries = new(StringComparer.Ordinal);
    private readonly object _admissionGate = new();
    private readonly int _maximumEntries;

    public OneTimeAuthorizationStore(int maximumEntries = 1024)
    {
        if (maximumEntries is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        _maximumEntries = maximumEntries;
    }

    public int Count => _entries.Count;

    public void Register(string nonce, DateTimeOffset expiresUtc, DateTimeOffset? nowUtc = null)
    {
        ValidateNonce(nonce);
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        if (expiresUtc <= now)
            throw new ArgumentOutOfRangeException(nameof(expiresUtc), "Authorization expiration must be in the future.");

        lock (_admissionGate)
        {
            PruneExpired(now);
            if (_entries.ContainsKey(nonce))
                throw new InvalidOperationException("Authorization nonce is already active.");
            if (_entries.Count >= _maximumEntries)
                throw new InvalidOperationException("Too many active one-time authorizations.");
            if (!_entries.TryAdd(nonce, expiresUtc.UtcDateTime.Ticks))
                throw new InvalidOperationException("Authorization nonce is already active.");
        }
    }

    public bool TryConsume(string? nonce, DateTimeOffset? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(nonce)) return false;
        var nowTicks = (nowUtc ?? DateTimeOffset.UtcNow).UtcDateTime.Ticks;
        if (!_entries.TryRemove(nonce, out var expiresTicks)) return false;
        return expiresTicks > nowTicks;
    }

    public int PruneExpired(DateTimeOffset? nowUtc = null)
    {
        var nowTicks = (nowUtc ?? DateTimeOffset.UtcNow).UtcDateTime.Ticks;
        var removed = 0;
        foreach (var pair in _entries)
        {
            if (pair.Value > nowTicks) continue;
            if (_entries.TryRemove(new KeyValuePair<string, long>(pair.Key, pair.Value))) removed++;
        }
        return removed;
    }

    public void Clear() => _entries.Clear();

    private static void ValidateNonce(string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        if (nonce.Length is < 16 or > 128 || nonce.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_'))
            throw new ArgumentException("Authorization nonce must be bounded base64url-style text.", nameof(nonce));
    }
}

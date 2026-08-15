using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Discovery;

public sealed class DiscoveryRegistry
{
    private readonly Dictionary<string, PeerDevice> _peers = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly TimeSpan _expiry;

    public DiscoveryRegistry(TimeSpan? expiry = null)
    {
        _expiry = expiry ?? TimeSpan.FromSeconds(15);
        if (_expiry <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(expiry));
    }

    public bool Upsert(PeerDevice peer, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(peer);
        if (string.IsNullOrWhiteSpace(peer.Id)) return false;
        if (string.IsNullOrWhiteSpace(peer.Name)) return false;
        if (peer.Port is <= 0 or > 65535) return false;

        var normalized = peer with { LastSeenUtc = nowUtc };
        lock (_gate)
        {
            var changed = !_peers.TryGetValue(peer.Id, out var existing) ||
                          existing.Name != normalized.Name ||
                          existing.Platform != normalized.Platform ||
                          existing.Host != normalized.Host ||
                          existing.Port != normalized.Port ||
                          existing.CertificateFingerprint != normalized.CertificateFingerprint ||
                          existing.IsTrusted != normalized.IsTrusted;
            _peers[peer.Id] = normalized;
            return changed;
        }
    }

    public bool RemoveExpired(DateTimeOffset nowUtc)
    {
        lock (_gate)
        {
            var expired = _peers
                .Where(x => x.Value.LastSeenUtc is null || nowUtc - x.Value.LastSeenUtc.Value >= _expiry)
                .Select(x => x.Key)
                .ToArray();
            foreach (var id in expired) _peers.Remove(id);
            return expired.Length > 0;
        }
    }

    public IReadOnlyList<PeerDevice> Snapshot(string? excludeDeviceId = null)
    {
        lock (_gate)
        {
            return _peers.Values
                .Where(x => !string.Equals(x.Id, excludeDeviceId, StringComparison.Ordinal))
                .OrderByDescending(x => x.IsTrusted)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public void Clear()
    {
        lock (_gate) _peers.Clear();
    }
}

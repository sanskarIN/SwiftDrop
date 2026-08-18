using SwiftDrop.Core.Discovery;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Tests;

public sealed class DiscoveryRegistryStateMachineTests
{
    [Fact]
    public void DeterministicStateMachine_MatchesReferenceModel()
    {
        var expiry = TimeSpan.FromSeconds(7);
        var registry = new DiscoveryRegistry(expiry);
        var model = new ReferenceDiscoveryRegistry(expiry);
        var random = new Random(0x4D5C0A17);
        var now = new DateTimeOffset(2026, 8, 18, 10, 0, 0, TimeSpan.Zero);

        for (var step = 0; step < 3_000; step++)
        {
            now = now.AddMilliseconds(random.Next(0, 2_001));
            var operation = random.Next(100);

            if (operation < 61)
            {
                var peer = CreatePeer(random);
                Assert.Equal(model.Upsert(peer, now), registry.Upsert(peer, now));
            }
            else if (operation < 80)
            {
                Assert.Equal(model.RemoveExpired(now), registry.RemoveExpired(now));
            }
            else if (operation < 94)
            {
                var excludedId = random.Next(4) == 0 ? $"device-{random.Next(0, 12):D2}" : null;
                Assert.Equal(model.Snapshot(excludedId), registry.Snapshot(excludedId));
            }
            else
            {
                model.Clear();
                registry.Clear();
            }

            Assert.Equal(model.Snapshot(), registry.Snapshot());
        }
    }

    private static PeerDevice CreatePeer(Random random)
    {
        var index = random.Next(0, 12);
        var id = $"device-{index:D2}";
        var name = $"Device {index:D2} Variant {random.Next(0, 5)}";
        var port = 40_000 + random.Next(0, 1_000);

        switch (random.Next(24))
        {
            case 0:
                id = "";
                break;
            case 1:
                name = " ";
                break;
            case 2:
                port = 0;
                break;
            case 3:
                port = 65_536;
                break;
        }

        return new PeerDevice(
            id,
            name,
            random.Next(2) == 0 ? "Android" : "Windows",
            $"10.0.{index / 8}.{10 + index}",
            port,
            random.Next(3) == 0 ? null : $"fingerprint-{index:D2}-{random.Next(0, 3)}",
            random.Next(2) == 0);
    }

    private sealed class ReferenceDiscoveryRegistry
    {
        private readonly Dictionary<string, PeerDevice> _peers = new(StringComparer.Ordinal);
        private readonly TimeSpan _expiry;

        public ReferenceDiscoveryRegistry(TimeSpan expiry) => _expiry = expiry;

        public bool Upsert(PeerDevice peer, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(peer.Id) ||
                string.IsNullOrWhiteSpace(peer.Name) ||
                peer.Port is <= 0 or > 65_535)
            {
                return false;
            }

            var normalized = peer with { LastSeenUtc = now };
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

        public bool RemoveExpired(DateTimeOffset now)
        {
            var expired = _peers
                .Where(pair => pair.Value.LastSeenUtc is null || now - pair.Value.LastSeenUtc.Value >= _expiry)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var id in expired)
                _peers.Remove(id);

            return expired.Length > 0;
        }

        public IReadOnlyList<PeerDevice> Snapshot(string? excludeDeviceId = null) => _peers.Values
            .Where(peer => !string.Equals(peer.Id, excludeDeviceId, StringComparison.Ordinal))
            .OrderByDescending(peer => peer.IsTrusted)
            .ThenBy(peer => peer.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        public void Clear() => _peers.Clear();
    }
}

using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class TrustedDevicesService
{
    private readonly TrustStore _store;
    private bool _initialized;

    public TrustedDevicesService()
    {
        _store = new TrustStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _store.InitializeAsync(ct);
        _initialized = true;
    }

    public async Task<IReadOnlyList<TrustedPeer>> GetAllAsync(CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        return await _store.GetAllAsync(ct);
    }

    public async Task<TrustedPeer?> GetAsync(string deviceId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        return await _store.GetAsync(deviceId, ct);
    }

    public async Task<bool> MatchesAsync(string deviceId, string fingerprint, CancellationToken ct = default)
    {
        var peer = await GetAsync(deviceId, ct);
        return peer is not null &&
               string.Equals(peer.CertificateFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    public async Task TrustAsync(string deviceId, string deviceName, string fingerprint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        await InitializeAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var existing = await _store.GetAsync(deviceId, ct);
        await _store.UpsertAsync(new TrustedPeer(
            deviceId,
            deviceName,
            fingerprint,
            existing?.TrustedAtUtc ?? now,
            now), ct);
    }

    public async Task RevokeAsync(string deviceId, CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await _store.RemoveAsync(deviceId, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await InitializeAsync(ct);
        await _store.ClearAsync(ct);
    }
}

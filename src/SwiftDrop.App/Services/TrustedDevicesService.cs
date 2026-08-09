using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.App.Services;

public sealed class TrustedDevicesService
{
    private readonly TrustStore _store;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;

    public TrustedDevicesService()
    {
        _store = new TrustStore(Path.Combine(FileSystem.AppDataDirectory, "swiftdrop.db"));
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initializeGate.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await _store.InitializeAsync(ct);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
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
        return peer is not null && Fingerprint.FixedTimeEquals(peer.CertificateFingerprint, fingerprint);
    }

    public async Task TrustAsync(string deviceId, string deviceName, string fingerprint, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);
        if (!Fingerprint.FixedTimeEquals(fingerprint, fingerprint) || fingerprint.Replace(":", string.Empty).Length != 64)
            throw new ArgumentException("Trusted-device certificate fingerprint must be a SHA-256 value.", nameof(fingerprint));

        await InitializeAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var existing = await _store.GetAsync(deviceId, ct);
        await _store.UpsertAsync(new TrustedPeer(
            deviceId,
            deviceName,
            fingerprint.Replace(":", string.Empty).ToUpperInvariant(),
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

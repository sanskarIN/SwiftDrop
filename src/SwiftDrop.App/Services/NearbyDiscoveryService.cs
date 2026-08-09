using SwiftDrop.Core.Discovery;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public sealed class NearbyDiscoveryService : IAsyncDisposable
{
    private readonly DeviceIdentityService _identity;
    private readonly DiscoveryRegistry _registry = new(TimeSpan.FromSeconds(15));
    private readonly CancellationTokenSource _cts = new();
    private UdpDiscoveryService? _udp;
    private Task? _listenTask;
    private Task? _announceTask;
    private Task? _expiryTask;

    public NearbyDiscoveryService(DeviceIdentityService identity)
    {
        _identity = identity;
    }

    public event EventHandler? PeersChanged;

    public bool IsRunning => _udp is not null;

    public IReadOnlyList<PeerDevice> Snapshot()
        => _registry.Snapshot(_identity.DeviceId);

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_udp is not null) return;
        await _identity.InitializeAsync();
        _udp = new UdpDiscoveryService(ProtocolConstants.DefaultPort + 1);
        var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        _listenTask = Task.Run(() => ListenLoopAsync(linked.Token), linked.Token);
        _announceTask = Task.Run(() => AnnounceLoopAsync(linked.Token), linked.Token);
        _expiryTask = Task.Run(() => ExpiryLoopAsync(linked.Token), linked.Token);
    }

    private PeerDevice Self()
        => new(
            _identity.DeviceId,
            _identity.DeviceName,
            DeviceInfo.Platform.ToString(),
            string.Empty,
            ProtocolConstants.DefaultPort,
            Fingerprint.FromCertificate(_identity.Certificate),
            false,
            DateTimeOffset.UtcNow);

    private async Task AnnounceLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_udp is not null) await _udp.AnnounceAsync(Self(), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        if (_udp is null) return;
        await foreach (var peer in _udp.ListenAsync(ct))
        {
            if (string.Equals(peer.Id, _identity.DeviceId, StringComparison.Ordinal)) continue;
            if (_registry.Upsert(peer, DateTimeOffset.UtcNow))
                MainThread.BeginInvokeOnMainThread(() => PeersChanged?.Invoke(this, EventArgs.Empty));
        }
    }

    private async Task ExpiryLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            if (_registry.RemoveExpired(DateTimeOffset.UtcNow))
                MainThread.BeginInvokeOnMainThread(() => PeersChanged?.Invoke(this, EventArgs.Empty));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_udp is not null) await _udp.DisposeAsync();
        foreach (var task in new[] { _listenTask, _announceTask, _expiryTask }.Where(x => x is not null))
        {
            try { await task!; } catch (OperationCanceledException) { }
        }
        _cts.Dispose();
    }
}

using SwiftDrop.Core.Discovery;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.Services;

public sealed class NearbyDiscoveryService : IAsyncDisposable
{
    private readonly DeviceIdentityService _identity;
    private readonly DiscoveryRegistry _registry = new(TimeSpan.FromSeconds(15));
    private readonly CancellationTokenSource _lifetimeCts = new();
    private UdpDiscoveryService? _udp;
    private MdnsDiscoveryService? _mdns;
    private CancellationTokenSource? _runCts;
    private Task? _udpListenTask;
    private Task? _mdnsListenTask;
    private Task? _announceTask;
    private Task? _expiryTask;

    public NearbyDiscoveryService(DeviceIdentityService identity)
    {
        _identity = identity;
    }

    public event EventHandler? PeersChanged;

    public bool IsRunning => _udp is not null || _mdns is not null;
    public bool IsUdpRunning => _udp is not null;
    public bool IsMdnsRunning => _mdns is not null;

    public IReadOnlyList<PeerDevice> Snapshot()
        => _registry.Snapshot(_identity.DeviceId);

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;
        await _identity.InitializeAsync();

        Exception? mdnsError = null;
        Exception? udpError = null;
        try { _mdns = new MdnsDiscoveryService(); }
        catch (Exception ex) when (ex is SocketException or InvalidOperationException) { mdnsError = ex; }

        try { _udp = new UdpDiscoveryService(ProtocolConstants.DefaultPort + 1); }
        catch (Exception ex) when (ex is SocketException or InvalidOperationException) { udpError = ex; }

        if (_mdns is null && _udp is null)
            throw new InvalidOperationException("Local discovery could not start with either mDNS or UDP fallback.", mdnsError ?? udpError);

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token);
        var token = _runCts.Token;
        if (_udp is not null) _udpListenTask = Task.Run(() => UdpListenLoopAsync(token), token);
        if (_mdns is not null) _mdnsListenTask = Task.Run(() => MdnsListenLoopAsync(token), token);
        _announceTask = Task.Run(() => AnnounceLoopAsync(token), token);
        _expiryTask = Task.Run(() => ExpiryLoopAsync(token), token);

        if (_mdns is not null)
        {
            try { await _mdns.QueryAsync(token); } catch (Exception ex) when (ex is SocketException or InvalidOperationException) { }
        }
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
            var self = Self();
            if (_mdns is not null)
            {
                try { await _mdns.AnnounceAsync(self, ct); } catch (Exception ex) when (ex is SocketException or InvalidOperationException) { }
            }
            if (_udp is not null)
            {
                try { await _udp.AnnounceAsync(self, ct); } catch (Exception ex) when (ex is SocketException or InvalidOperationException) { }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(3), ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task UdpListenLoopAsync(CancellationToken ct)
    {
        if (_udp is null) return;
        await foreach (var peer in _udp.ListenAsync(ct)) RegisterPeer(peer);
    }

    private async Task MdnsListenLoopAsync(CancellationToken ct)
    {
        if (_mdns is null) return;
        await foreach (var peer in _mdns.ListenAsync(Self(), ct)) RegisterPeer(peer);
    }

    private void RegisterPeer(PeerDevice peer)
    {
        if (string.Equals(peer.Id, _identity.DeviceId, StringComparison.Ordinal)) return;
        if (_registry.Upsert(peer, DateTimeOffset.UtcNow))
            MainThread.BeginInvokeOnMainThread(() => PeersChanged?.Invoke(this, EventArgs.Empty));
    }

    private async Task ExpiryLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { break; }
            if (_registry.RemoveExpired(DateTimeOffset.UtcNow))
                MainThread.BeginInvokeOnMainThread(() => PeersChanged?.Invoke(this, EventArgs.Empty));
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetimeCts.Cancel();
        _runCts?.Cancel();
        if (_mdns is not null) await _mdns.DisposeAsync();
        if (_udp is not null) await _udp.DisposeAsync();

        foreach (var task in new[] { _udpListenTask, _mdnsListenTask, _announceTask, _expiryTask }.Where(x => x is not null))
        {
            try { await task!; } catch (OperationCanceledException) { } catch (ObjectDisposedException) { }
        }

        _runCts?.Dispose();
        _lifetimeCts.Dispose();
    }
}

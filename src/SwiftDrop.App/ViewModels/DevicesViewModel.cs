using System.Collections.ObjectModel;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App.ViewModels;

public sealed class DevicesViewModel : ObservableObject, IDisposable
{
    private readonly NearbyDiscoveryService _discovery;
    private string _status = string.Empty;
    private bool _subscribed;

    public DevicesViewModel(NearbyDiscoveryService discovery)
    {
        _discovery = discovery;
    }

    public ObservableCollection<DeviceRow> Items { get; } = new();

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task StartOrRefreshAsync(CancellationToken ct = default)
    {
        Subscribe();
        await _discovery.StartAsync(ct);
        RefreshSnapshot();
    }

    public PeerDevice? FindPeer(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _discovery.Snapshot().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
    }

    public void RefreshSnapshot()
    {
        var peers = _discovery.Snapshot().Select(DeviceRow.FromPeer).ToArray();
        Items.Clear();
        foreach (var peer in peers) Items.Add(peer);
        Status = peers.Length == 1 ? "1 device" : $"{peers.Length} devices";
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        _discovery.PeersChanged += DiscoveryOnPeersChanged;
        _subscribed = true;
    }

    private void DiscoveryOnPeersChanged(object? sender, EventArgs e) => RefreshSnapshot();

    public void Dispose()
    {
        if (!_subscribed) return;
        _discovery.PeersChanged -= DiscoveryOnPeersChanged;
        _subscribed = false;
    }

    public sealed record DeviceRow(
        string Id,
        string Name,
        string Platform,
        string AddressText,
        string LastSeenText)
    {
        public static DeviceRow FromPeer(PeerDevice peer)
        {
            var lastSeen = peer.LastSeenUtc is null
                ? "Last seen: unknown"
                : $"Last seen: {peer.LastSeenUtc.Value.LocalDateTime:T}";
            return new DeviceRow(
                peer.Id,
                peer.Name,
                peer.Platform,
                $"{peer.Host}:{peer.Port}",
                lastSeen);
        }
    }
}

using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;

namespace SwiftDrop.App;

public partial class DevicesPage : ContentPage
{
    private readonly NearbyDiscoveryService _discovery;

    public DevicesPage(NearbyDiscoveryService discovery)
    {
        InitializeComponent();
        _discovery = discovery;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        _discovery.PeersChanged += DiscoveryOnPeersChanged;
        await _discovery.StartAsync();
        RefreshList();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _discovery.PeersChanged -= DiscoveryOnPeersChanged;
    }

    private void DiscoveryOnPeersChanged(object? sender, EventArgs e) => RefreshList();

    private void RefreshClicked(object? sender, EventArgs e) => RefreshList();

    private void RefreshList()
    {
        var peers = _discovery.Snapshot()
            .Select(DeviceRow.FromPeer)
            .ToArray();
        DevicesList.ItemsSource = peers;
        StatusLabel.Text = peers.Length == 1 ? "1 device" : $"{peers.Length} devices";
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

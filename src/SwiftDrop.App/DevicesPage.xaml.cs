using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class DevicesPage : ContentPage
{
    private readonly NearbyDiscoveryService _discovery;
    private readonly NearbyPairingService _pairing;
    private readonly PairingSelectionService _selection;

    public DevicesPage(
        NearbyDiscoveryService discovery,
        NearbyPairingService pairing,
        PairingSelectionService selection)
    {
        InitializeComponent();
        _discovery = discovery;
        _pairing = pairing;
        _selection = selection;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        _discovery.PeersChanged += DiscoveryOnPeersChanged;
        try
        {
            await _discovery.StartAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Discovery unavailable", ex.Message, "OK");
        }
        RefreshList();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _discovery.PeersChanged -= DiscoveryOnPeersChanged;
    }

    private void DiscoveryOnPeersChanged(object? sender, EventArgs e) => RefreshList();

    private async void RefreshClicked(object? sender, EventArgs e)
    {
        try
        {
            await _discovery.StartAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Discovery unavailable", ex.Message, "OK");
        }
        RefreshList();
    }

    private async void PairClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string id) return;
        var peer = _discovery.Snapshot().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        if (peer is null)
        {
            await DisplayAlert("Device unavailable", "The device is no longer advertising. Refresh and try again.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(peer.CertificateFingerprint))
        {
            await DisplayAlert("Pairing unavailable", "This discovery record does not include a certificate fingerprint. Use QR pairing instead.", "OK");
            return;
        }

        var mode = await DisplayActionSheet(
            $"Pair with {peer.Name}",
            "Cancel",
            null,
            "Request pairing",
            "Use 8-digit code");
        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, "Cancel", StringComparison.Ordinal)) return;

        string? code = null;
        if (string.Equals(mode, "Use 8-digit code", StringComparison.Ordinal))
        {
            code = (await DisplayPromptAsync(
                "One-time pairing code",
                "Enter the 8-digit code shown on the receiving device. The code expires quickly and is still combined with TLS certificate verification and receiver approval.",
                "Continue",
                "Cancel",
                keyboard: Keyboard.Numeric,
                maxLength: 8))?.Trim();
            if (string.IsNullOrWhiteSpace(code)) return;
            if (code.Length != 8 || code.Any(ch => ch is < '0' or > '9'))
            {
                await DisplayAlert("Invalid code", "Enter exactly eight digits.", "OK");
                return;
            }
        }
        else
        {
            var requested = await DisplayAlert(
                "Request pairing?",
                $"Device: {peer.Name}\nAddress: {peer.Host}:{peer.Port}\nCertificate: {Fingerprint.Pretty(peer.CertificateFingerprint)}\n\nThe other device must approve this request.",
                "Request",
                "Cancel");
            if (!requested) return;
        }

        try
        {
            button.IsEnabled = false;
            button.Text = "Waiting for approval…";
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var payload = await _pairing.RequestAsync(peer, code, cts.Token);
            _selection.Set(payload);
            await Navigation.PopAsync();
        }
        catch (OperationCanceledException)
        {
            await DisplayAlert("Pairing timed out", "The pairing request expired before it was approved.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Pairing failed", ex.Message, "OK");
        }
        finally
        {
            button.Text = "Request pairing";
            button.IsEnabled = true;
        }
    }

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

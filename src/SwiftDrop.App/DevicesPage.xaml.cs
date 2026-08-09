using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
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
        ManualPortEntry.Text = ProtocolConstants.DefaultPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
            await DisplayAlertAsync("Discovery unavailable", ex.Message, "OK");
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
            await DisplayAlertAsync("Discovery unavailable", ex.Message, "OK");
        }
        RefreshList();
    }

    private async void PairClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string id) return;
        var peer = _discovery.Snapshot().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        if (peer is null)
        {
            await DisplayAlertAsync("Device unavailable", "The device is no longer advertising. Refresh and try again.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(peer.CertificateFingerprint))
        {
            await DisplayAlertAsync("Pairing unavailable", "This discovery record does not include a certificate fingerprint. Use QR pairing instead.", "OK");
            return;
        }

        var mode = await DisplayActionSheetAsync(
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
                await DisplayAlertAsync("Invalid code", "Enter exactly eight digits.", "OK");
                return;
            }
        }
        else
        {
            var requested = await DisplayAlertAsync(
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
            await DisplayAlertAsync("Pairing timed out", "The pairing request expired before it was approved.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Pairing failed", ex.Message, "OK");
        }
        finally
        {
            button.Text = "Request pairing";
            button.IsEnabled = true;
        }
    }

    private async void ManualPairClicked(object? sender, EventArgs e)
    {
        var host = ManualIpEntry.Text?.Trim() ?? string.Empty;
        var code = ManualCodeEntry.Text?.Trim() ?? string.Empty;
        if (!int.TryParse(ManualPortEntry.Text, out var port))
        {
            await DisplayAlertAsync("Invalid port", "Enter a numeric port from 1 to 65535.", "OK");
            return;
        }
        if (code.Length != 8 || code.Any(ch => ch is < '0' or > '9'))
        {
            await DisplayAlertAsync("Invalid code", "Enter the fresh 8-digit code shown on the receiving device.", "OK");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Manual pairing bootstrap",
            "Manual IP pairing initially connects before the receiver certificate fingerprint is known. The 8-digit code and receiver approval authorize the bootstrap. SwiftDrop then binds the returned invitation to the exact TLS certificate it observed and will ask you to visually confirm that fingerprint before sending. Continue only on a network and device you expect.",
            "Continue",
            "Cancel");
        if (!confirmed) return;

        try
        {
            if (sender is Button button) button.IsEnabled = false;
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var payload = await _pairing.RequestManualIpAsync(host, port, code, cts.Token);
            ManualCodeEntry.Text = string.Empty;
            _selection.Set(payload);
            await Navigation.PopAsync();
        }
        catch (OperationCanceledException)
        {
            await DisplayAlertAsync("Pairing timed out", "Manual pairing expired before it completed.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Manual pairing failed", ex.Message, "OK");
        }
        finally
        {
            if (sender is Button button) button.IsEnabled = true;
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

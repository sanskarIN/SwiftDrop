using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class TrustedDevicesPage : ContentPage
{
    private readonly TrustedDevicesService _trusted;

    public TrustedDevicesPage(TrustedDevicesService trusted)
    {
        InitializeComponent();
        _trusted = trusted;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var rows = (await _trusted.GetAllAsync())
            .Select(TrustedDeviceRow.FromPeer)
            .ToArray();
        TrustedList.ItemsSource = rows;
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshAsync();

    private async void RevokeClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string deviceId) return;
        var confirmed = await DisplayAlertAsync("Revoke trusted device?", "Future transfers from this device will require explicit confirmation again.", "Revoke", "Cancel");
        if (!confirmed) return;
        await _trusted.RevokeAsync(deviceId);
        await RefreshAsync();
    }

    private async void ClearAllClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync("Clear all trusted devices?", "This removes all locally stored trust decisions.", "Clear", "Cancel");
        if (!confirmed) return;
        await _trusted.ClearAsync();
        await RefreshAsync();
    }

    public sealed record TrustedDeviceRow(
        string DeviceId,
        string DeviceName,
        string FingerprintText,
        string TrustedText)
    {
        public static TrustedDeviceRow FromPeer(TrustedPeer peer)
            => new(
                peer.DeviceId,
                peer.DeviceName,
                Fingerprint.Pretty(peer.CertificateFingerprint),
                $"Trusted {peer.TrustedAtUtc.LocalDateTime:g} • Last seen {peer.LastSeenUtc.LocalDateTime:g}");
    }
}

using System.Collections.ObjectModel;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App.ViewModels;

public sealed class TrustedDevicesViewModel : ObservableObject
{
    private readonly TrustedDevicesService _trusted;
    private string _status = string.Empty;

    public TrustedDevicesViewModel(TrustedDevicesService trusted)
    {
        _trusted = trusted;
    }

    public ObservableCollection<TrustedDeviceRow> Items { get; } = new();

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var rows = (await _trusted.GetAllAsync(ct))
            .Select(TrustedDeviceRow.FromPeer)
            .ToArray();
        Items.Clear();
        foreach (var row in rows) Items.Add(row);
        Status = $"{rows.Length:N0} trusted device{(rows.Length == 1 ? string.Empty : "s")}";
    }

    public async Task RevokeAsync(string deviceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        await _trusted.RevokeAsync(deviceId, ct);
        await RefreshAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _trusted.ClearAsync(ct);
        await RefreshAsync(ct);
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

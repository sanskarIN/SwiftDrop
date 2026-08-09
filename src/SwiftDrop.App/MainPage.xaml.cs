using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class MainPage : ContentPage
{
    private readonly DeviceIdentityService _identity;
    private readonly TransferCoordinator _transfers;
    private readonly TransferHistoryService _history;
    private readonly NetworkDiagnosticsService _diagnostics;
    private readonly IServiceProvider _services;
    private PairingPayload? _remote;
    private FileResult? _selectedFile;
    private ReceiveServerService? _receiveServer;
    private CancellationTokenSource? _sendCts;

    public MainPage(
        DeviceIdentityService identity,
        TransferCoordinator transfers,
        TransferHistoryService history,
        NetworkDiagnosticsService diagnostics,
        IServiceProvider services)
    {
        InitializeComponent();
        _identity = identity;
        _transfers = transfers;
        _history = history;
        _diagnostics = diagnostics;
        _services = services;
        Loaded += async (_, _) => await InitializeAsync();
        Unloaded += async (_, _) => await StopReceiveServerAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _identity.InitializeAsync();
            await _history.InitializeAsync();
            DeviceNameLabel.Text = _identity.DeviceName;
            DeviceIdLabel.Text = _identity.DeviceId;
            if (_receiveServer is null)
            {
                var receiveRoot = Path.Combine(FileSystem.AppDataDirectory, "Received");
                _receiveServer = new ReceiveServerService(
                    _identity.Certificate,
                    receiveRoot,
                    _identity.TryConsumePairingNonce,
                    ApproveIncomingAsync,
                    RecordIncomingAsync);
                _receiveServer.Start();
                TransferStatusLabel.Text = $"Ready to receive into {receiveRoot}";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Startup error", ex.Message, "OK");
        }
    }

    private async Task<bool> ApproveIncomingAsync(IncomingTransferPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var risk = preview.RiskLevel switch
        {
            FileRiskLevel.High => "\n\nWARNING: This file type can execute code or install software. Only accept it if you expected it and trust the sender.",
            FileRiskLevel.Caution => "\n\nCaution: This file type can contain other files or active content. Inspect it before opening.",
            _ => string.Empty
        };
        var message =
            $"Sender: {preview.SenderDeviceName}\n" +
            $"File: {preview.Entry.RelativePath}\n" +
            $"Size: {preview.Entry.Length:N0} bytes\n" +
            $"Sender certificate: {Fingerprint.Pretty(preview.SenderCertificateFingerprint)}" + risk +
            "\n\nSwiftDrop will not open the file automatically.";

        return await MainThread.InvokeOnMainThreadAsync(() =>
            DisplayAlert("Incoming transfer", message, "Accept", "Reject"));
    }

    private Task RecordIncomingAsync(IncomingTransferPreview preview, string status, bool verified, CancellationToken ct)
        => _history.AddAsync(
            "received",
            preview.SenderDeviceName,
            preview.Entry.RelativePath,
            preview.Entry.Length,
            status,
            verified,
            ct);

    private async Task StopReceiveServerAsync()
    {
        if (_receiveServer is null) return;
        await _receiveServer.DisposeAsync();
        _receiveServer = null;
    }

    private void CreatePairingClicked(object? sender, EventArgs e)
    {
        var link = _identity.CreatePairingLink();
        PairingLinkEntry.Text = link;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(8);
        QrImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
        QrImage.IsVisible = true;
    }

    private async void CopyLinkClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PairingLinkEntry.Text))
            await Clipboard.Default.SetTextAsync(PairingLinkEntry.Text);
    }

    private async void ShareLinkClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(PairingLinkEntry.Text))
            await Share.Default.RequestAsync(new ShareTextRequest { Text = PairingLinkEntry.Text, Title = "SwiftDrop pairing link" });
    }

    private async void ValidatePairingClicked(object? sender, EventArgs e)
    {
        try
        {
            _remote = PairingCodec.Decode(RemoteLinkEntry.Text ?? string.Empty);
            RemotePeerLabel.Text = $"{_remote.DeviceName} • {_remote.Host}:{_remote.Port}\nFingerprint: {Fingerprint.Pretty(_remote.CertificateFingerprint)}";
            var confirmed = await DisplayAlert(
                "Confirm device fingerprint",
                $"Verify this fingerprint on the receiving device before sending:\n\n{Fingerprint.Pretty(_remote.CertificateFingerprint)}",
                "I verified it",
                "Cancel");
            if (!confirmed)
            {
                _remote = null;
                RemotePeerLabel.Text = "Pairing cancelled. Generate a fresh invitation when ready.";
            }
        }
        catch (Exception ex)
        {
            _remote = null;
            await DisplayAlert("Pairing failed", ex.Message, "OK");
        }
    }

    private async void ChooseFileClicked(object? sender, EventArgs e)
    {
        _selectedFile = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a file to send" });
        SelectedFileLabel.Text = _selectedFile?.FullPath ?? "No file selected";
    }

    private async void SendFileClicked(object? sender, EventArgs e)
    {
        if (_remote is null)
        {
            await DisplayAlert("Device required", "Validate a pairing link first.", "OK");
            return;
        }
        if (_selectedFile is null)
        {
            await DisplayAlert("File required", "Choose a file first.", "OK");
            return;
        }

        _sendCts?.Dispose();
        _sendCts = new CancellationTokenSource();
        var fileInfo = new FileInfo(_selectedFile.FullPath);
        try
        {
            TransferStatusLabel.Text = "Sending…";
            CancelSendButton.IsEnabled = true;
            SendFileButton.IsEnabled = false;
            var progress = new Progress<double>(value => TransferProgress.Progress = value);
            await _transfers.SendAsync(_remote, _selectedFile.FullPath, progress, _sendCts.Token);
            TransferStatusLabel.Text = "Completed and verified";
            await _history.AddAsync("sent", _remote.DeviceName, fileInfo.Name, fileInfo.Length, "completed", true);
        }
        catch (OperationCanceledException)
        {
            TransferStatusLabel.Text = "Cancelled";
            await _history.AddAsync("sent", _remote.DeviceName, fileInfo.Name, fileInfo.Length, "cancelled", false);
        }
        catch (Exception ex)
        {
            TransferStatusLabel.Text = "Failed";
            await _history.AddAsync("sent", _remote.DeviceName, fileInfo.Name, fileInfo.Length, "failed", false);
            await DisplayAlert("Transfer failed", ex.Message, "OK");
        }
        finally
        {
            CancelSendButton.IsEnabled = false;
            SendFileButton.IsEnabled = true;
        }
    }

    private void CancelSendClicked(object? sender, EventArgs e) => _sendCts?.Cancel();

    private async void OpenDevicesClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DevicesPage>());

    private async void OpenSettingsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    private async void OpenHistoryClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<HistoryPage>());

    private async void RunDiagnosticsClicked(object? sender, EventArgs e)
    {
        var results = _diagnostics.InspectLocalNetwork();
        var message = string.Join("\n\n", results.Select(r => $"{r.Title}\n{r.Message}"));
        await DisplayAlert("Local network diagnostics", message, "OK");
    }
}

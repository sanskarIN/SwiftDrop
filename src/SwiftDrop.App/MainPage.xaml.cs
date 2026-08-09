using System.Text;
using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using SwiftDrop.App.Services;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class MainPage : ContentPage
{
    private readonly DeviceIdentityService _identity;
    private readonly TransferCoordinator _transfers;
    private readonly TransferHistoryService _history;
    private readonly TrustedDevicesService _trustedDevices;
    private readonly AppSettingsService _settings;
    private readonly PairingSelectionService _pairingSelection;
    private readonly OneTimePairingCodeManager _pairingCodes;
    private readonly IServiceProvider _services;
    private PairingPayload? _remote;
    private FileResult? _selectedFile;
    private ReceiveServerService? _receiveServer;
    private CancellationTokenSource? _sendCts;

    public MainPage(
        DeviceIdentityService identity,
        TransferCoordinator transfers,
        TransferHistoryService history,
        TrustedDevicesService trustedDevices,
        AppSettingsService settings,
        PairingSelectionService pairingSelection,
        OneTimePairingCodeManager pairingCodes,
        IServiceProvider services)
    {
        InitializeComponent();
        _identity = identity;
        _transfers = transfers;
        _history = history;
        _trustedDevices = trustedDevices;
        _settings = settings;
        _pairingSelection = pairingSelection;
        _pairingCodes = pairingCodes;
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
            await _trustedDevices.InitializeAsync();
            DeviceNameLabel.Text = _identity.DeviceName;
            DeviceIdLabel.Text = _identity.DeviceId;
            DeviceFingerprintLabel.Text = $"Certificate: {Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate))}";

            if (_receiveServer is null)
            {
                var receiveRoot = Path.Combine(FileSystem.AppDataDirectory, "Received");
                _receiveServer = new ReceiveServerService(
                    _identity.Certificate,
                    receiveRoot,
                    _identity.TryConsumePairingNonce,
                    ApproveIncomingAsync,
                    RecordIncomingAsync,
                    ApproveIncomingTextAsync,
                    RecordIncomingTextAsync,
                    ApproveNearbyPairingAsync,
                    _identity.CreatePairingLink,
                    code => _pairingCodes.TryConsume(code, DateTimeOffset.UtcNow));
                _receiveServer.Start();
                TransferStatusLabel.Text = $"Ready to receive into {receiveRoot}";
            }

            await ApplyPendingPairingAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Startup error", ex.Message, "OK");
        }
    }

    private async Task ApplyPendingPairingAsync()
    {
        var pending = _pairingSelection.Current;
        if (pending is null) return;
        _pairingSelection.Clear();
        RemoteLinkEntry.Text = PairingCodec.Encode(pending);
        await ConfirmRemotePairingAsync(pending);
    }

    private async Task<bool> ApproveIncomingAsync(IncomingTransferPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var settings = _settings.Load();
        var trustedMatch = await _trustedDevices.MatchesAsync(
            preview.SenderDeviceId,
            preview.SenderCertificateFingerprint,
            ct);

        if (trustedMatch && settings.AutoAcceptTrustedDevices && preview.RiskLevel == FileRiskLevel.Normal)
            return true;

        var trustState = trustedMatch ? "\nTrusted device: certificate matches the stored fingerprint." : string.Empty;
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
            $"Sender certificate: {Fingerprint.Pretty(preview.SenderCertificateFingerprint)}" +
            trustState + risk +
            "\n\nSwiftDrop will not open the file automatically.";

        var accepted = await MainThread.InvokeOnMainThreadAsync(() =>
            DisplayAlert("Incoming transfer", message, "Accept", "Reject"));
        if (!accepted) return false;

        if (!trustedMatch)
        {
            var trust = await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert(
                    "Trust this device?",
                    "Trust stores this exact device ID and certificate fingerprint locally. You can revoke it from Settings. Choose Not now if this is a one-time transfer.",
                    "Trust device",
                    "Not now"));
            if (trust)
            {
                await _trustedDevices.TrustAsync(
                    preview.SenderDeviceId,
                    preview.SenderDeviceName,
                    preview.SenderCertificateFingerprint,
                    ct);
            }
        }

        return true;
    }

    private async Task<bool> ApproveNearbyPairingAsync(IncomingPairingRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var localFingerprint = Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate));
        var senderFingerprint = Fingerprint.Pretty(request.SenderCertificateFingerprint);
        return await MainThread.InvokeOnMainThreadAsync(() =>
            DisplayAlert(
                "Nearby pairing request",
                $"{request.SenderDeviceName} wants to pair with this device.\n\nSender certificate:\n{senderFingerprint}\n\nThis device certificate:\n{localFingerprint}\n\nApprove only if you initiated this pairing and can compare fingerprints on both devices.",
                "Approve",
                "Reject"));
    }

    private async Task<IncomingTextDecision> ApproveIncomingTextAsync(IncomingTextPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var displayText = preview.Text.Length <= 900 ? preview.Text : preview.Text[..900] + "…";
        var fingerprint = Fingerprint.Pretty(preview.SenderCertificateFingerprint);
        var choice = await MainThread.InvokeOnMainThreadAsync(() =>
            DisplayActionSheet(
                $"Text from {preview.SenderDeviceName}\nCertificate: {fingerprint}\n{preview.CharacterCount:N0} characters\n\n{displayText}",
                "Reject",
                null,
                "Accept",
                "Accept and copy"));

        if (string.Equals(choice, "Accept and copy", StringComparison.Ordinal))
        {
            await MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(preview.Text));
            return IncomingTextDecision.AcceptAndCopy;
        }

        return string.Equals(choice, "Accept", StringComparison.Ordinal)
            ? IncomingTextDecision.Accept
            : IncomingTextDecision.Reject;
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

    private Task RecordIncomingTextAsync(IncomingTextPreview preview, string status, CancellationToken ct)
        => _history.AddAsync(
            "received",
            preview.SenderDeviceName,
            "Text snippet",
            Encoding.UTF8.GetByteCount(preview.Text),
            status,
            false,
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

    private void CreatePairingCodeClicked(object? sender, EventArgs e)
    {
        var snapshot = _pairingCodes.Create(DateTimeOffset.UtcNow);
        PairingCodeLabel.Text = snapshot.Code;
        PairingCodeLabel.IsVisible = true;
        PairingCodeExpiryLabel.Text = $"Expires at {snapshot.ExpiresUtc.LocalDateTime:T}. The code is one-time and is not a long-lived trust credential.";
        PairingCodeExpiryLabel.IsVisible = true;
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
            var payload = PairingCodec.Decode(RemoteLinkEntry.Text ?? string.Empty);
            await ConfirmRemotePairingAsync(payload);
        }
        catch (Exception ex)
        {
            _remote = null;
            await DisplayAlert("Pairing failed", ex.Message, "OK");
        }
    }

    private async Task ConfirmRemotePairingAsync(PairingPayload payload)
    {
        RemotePeerLabel.Text = $"{payload.DeviceName} • {payload.Host}:{payload.Port}\nFingerprint: {Fingerprint.Pretty(payload.CertificateFingerprint)}";
        var confirmed = await DisplayAlert(
            "Confirm device fingerprint",
            $"Verify this fingerprint on the receiving device before sending:\n\n{Fingerprint.Pretty(payload.CertificateFingerprint)}",
            "I verified it",
            "Cancel");
        if (confirmed)
        {
            _remote = payload;
            RemotePeerLabel.Text += "\nVerified for this one-time invitation.";
        }
        else
        {
            _remote = null;
            RemotePeerLabel.Text = "Pairing cancelled. Generate a fresh invitation when ready.";
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

        var remote = _remote;
        _sendCts?.Dispose();
        _sendCts = new CancellationTokenSource();
        var fileInfo = new FileInfo(_selectedFile.FullPath);
        try
        {
            TransferStatusLabel.Text = "Sending…";
            CancelSendButton.IsEnabled = true;
            SendFileButton.IsEnabled = false;
            var progress = new Progress<double>(value => TransferProgress.Progress = value);
            await _transfers.SendAsync(remote, _selectedFile.FullPath, progress, _sendCts.Token);
            TransferStatusLabel.Text = "Completed and verified. A fresh pairing invitation is required for another transfer.";
            await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Length, "completed", true);
        }
        catch (OperationCanceledException)
        {
            TransferStatusLabel.Text = "Cancelled. Use a fresh pairing invitation to resume safely.";
            await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Length, "cancelled", false);
        }
        catch (Exception ex)
        {
            TransferStatusLabel.Text = "Failed. Use a fresh pairing invitation before retrying.";
            await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Length, "failed", false);
            await DisplayAlert("Transfer failed", ex.Message, "OK");
        }
        finally
        {
            _remote = null;
            CancelSendButton.IsEnabled = false;
            SendFileButton.IsEnabled = true;
        }
    }

    private async void PasteClipboardClicked(object? sender, EventArgs e)
    {
        try
        {
            TextSnippetEditor.Text = await Clipboard.Default.GetTextAsync() ?? string.Empty;
            TextTransferStatusLabel.Text = "Clipboard read once by your explicit action.";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Clipboard unavailable", ex.Message, "OK");
        }
    }

    private async void SendTextClicked(object? sender, EventArgs e)
    {
        if (_remote is null)
        {
            await DisplayAlert("Device required", "Validate a fresh pairing link first.", "OK");
            return;
        }

        var remote = _remote;
        var text = TextSnippetEditor.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert("Text required", "Type or explicitly paste a text snippet first.", "OK");
            return;
        }

        try
        {
            TextTransferStatusLabel.Text = "Sending encrypted text…";
            await _transfers.SendTextAsync(remote, text, CancellationToken.None);
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                "Text snippet",
                Encoding.UTF8.GetByteCount(text),
                "completed",
                false);
            TextSnippetEditor.Text = string.Empty;
            TextTransferStatusLabel.Text = "Text delivered. A fresh pairing invitation is required for another transfer.";
        }
        catch (Exception ex)
        {
            TextTransferStatusLabel.Text = "Text transfer failed. Use a fresh pairing invitation before retrying.";
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                "Text snippet",
                Encoding.UTF8.GetByteCount(text),
                "failed",
                false);
            await DisplayAlert("Text transfer failed", ex.Message, "OK");
        }
        finally
        {
            _remote = null;
        }
    }

    private void CancelSendClicked(object? sender, EventArgs e) => _sendCts?.Cancel();

    private async void OpenDevicesClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DevicesPage>());

    private async void OpenSettingsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    private async void OpenHistoryClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<HistoryPage>());

    private async void OpenDiagnosticsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DiagnosticsPage>());
}

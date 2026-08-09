using System.Diagnostics;
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
    private readonly ReceiveLocationService _receiveLocation;
    private readonly PairingSelectionService _pairingSelection;
    private readonly OneTimePairingCodeManager _pairingCodes;
    private readonly IServiceProvider _services;

    private PairingPayload? _remote;
    private FileResult? _selectedFile;
    private FileResult[] _selectedBatchFiles = Array.Empty<FileResult>();
    private ReceiveServerService? _receiveServer;

    private CancellationTokenSource? _singleSendCts;
    private CancellationTokenSource? _batchSendCts;
    private bool _singlePauseRequested;
    private bool _batchPauseRequested;
    private string? _pausedSinglePath;
    private string[] _pausedBatchPaths = Array.Empty<string>();

    public MainPage(
        DeviceIdentityService identity,
        TransferCoordinator transfers,
        TransferHistoryService history,
        TrustedDevicesService trustedDevices,
        AppSettingsService settings,
        ReceiveLocationService receiveLocation,
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
        _receiveLocation = receiveLocation;
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
                var receiveRoot = _receiveLocation.ResolveReceiveRoot();
                ReceiveFolderLabel.Text = $"Receive folder: {receiveRoot}";
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
                    code => _pairingCodes.TryConsume(code, DateTimeOffset.UtcNow),
                    ApproveIncomingBatchAsync);
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

        var trustState = trustedMatch
            ? "\nTrusted device: certificate matches the stored fingerprint."
            : string.Empty;
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

    private async Task<IncomingBatchDecision> ApproveIncomingBatchAsync(
        IncomingBatchPreview preview,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var settings = _settings.Load();
        var trustedMatch = await _trustedDevices.MatchesAsync(
            preview.SenderDeviceId,
            preview.SenderCertificateFingerprint,
            ct);

        if (trustedMatch && settings.AutoAcceptTrustedDevices && preview.HighestRisk == FileRiskLevel.Normal)
            return IncomingBatchDecision.AcceptAll(preview.Files);

        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = new BatchApprovalPage(preview);
            await Navigation.PushModalAsync(new NavigationPage(page));
            return await page.DecisionTask;
        });
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
        {
            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text = PairingLinkEntry.Text,
                Title = "SwiftDrop pairing link"
            });
        }
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
        _selectedFile = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a file to send"
        });
        SelectedFileLabel.Text = _selectedFile?.FullPath ?? "No file selected";
    }

    private async void ChooseMultipleFilesClicked(object? sender, EventArgs e)
    {
        try
        {
            var selected = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Choose files to send"
            });
            _selectedBatchFiles = selected
                .Where(x => !string.IsNullOrWhiteSpace(x.FullPath))
                .Take(2048)
                .ToArray();
            SelectedBatchLabel.Text = _selectedBatchFiles.Length == 0
                ? "No batch selected"
                : _selectedBatchFiles.Length == 1
                    ? _selectedBatchFiles[0].FileName
                    : $"{_selectedBatchFiles.Length:N0} files selected";
        }
        catch (Exception ex)
        {
            await DisplayAlert("File selection failed", ex.Message, "OK");
        }
    }

    private async void SendFileClicked(object? sender, EventArgs e)
    {
        if (_selectedFile is null)
        {
            await DisplayAlert("File required", "Choose a file first.", "OK");
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert("Device required", "Validate a fresh pairing invitation first.", "OK");
            return;
        }

        _pausedSinglePath = null;
        ResumeSendButton.IsEnabled = false;
        await RunSingleSendAsync(remote, _selectedFile.FullPath);
    }

    private async void ResumeSendClicked(object? sender, EventArgs e)
    {
        var path = _pausedSinglePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _pausedSinglePath = null;
            ResumeSendButton.IsEnabled = false;
            await DisplayAlert("Resume unavailable", "The original source file is no longer available.", "OK");
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert("Fresh pairing required", "Pair with the receiver again, verify its fingerprint, then press Resume.", "OK");
            return;
        }

        await RunSingleSendAsync(remote, path);
    }

    private async Task RunSingleSendAsync(PairingPayload remote, string path)
    {
        _singleSendCts?.Dispose();
        _singleSendCts = new CancellationTokenSource();
        _singlePauseRequested = false;
        var fileInfo = new FileInfo(path);
        try
        {
            TransferStatusLabel.Text = _pausedSinglePath is null ? "Sending…" : "Resuming…";
            SendFileButton.IsEnabled = false;
            PauseSendButton.IsEnabled = true;
            ResumeSendButton.IsEnabled = false;
            CancelSendButton.IsEnabled = true;
            var progress = new Progress<double>(value => TransferProgress.Progress = value);
            await _transfers.SendAsync(remote, path, progress, _singleSendCts.Token);
            TransferProgress.Progress = 1;
            TransferStatusLabel.Text = "Completed and verified. A fresh pairing invitation is required for another transfer.";
            _pausedSinglePath = null;
            await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Length, "completed", true);
        }
        catch (OperationCanceledException)
        {
            if (_singlePauseRequested)
            {
                _pausedSinglePath = path;
                TransferStatusLabel.Text = "Paused safely. Pair with the same receiver again, verify its fingerprint, then press Resume.";
                ResumeSendButton.IsEnabled = true;
                await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Length, "paused", false);
            }
            else
            {
                _pausedSinglePath = null;
                TransferStatusLabel.Text = "Cancelled.";
                await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Length, "cancelled", false);
            }
        }
        catch (Exception ex)
        {
            _pausedSinglePath = File.Exists(path) ? path : null;
            ResumeSendButton.IsEnabled = _pausedSinglePath is not null;
            TransferStatusLabel.Text = _pausedSinglePath is null
                ? "Failed."
                : "Failed safely. Pair with the receiver again and press Resume to reuse any verified partial offset.";
            await _history.AddAsync("sent", remote.DeviceName, fileInfo.Name, fileInfo.Exists ? fileInfo.Length : 0, "failed", false);
            await DisplayAlert("Transfer failed", ex.Message, "OK");
        }
        finally
        {
            PauseSendButton.IsEnabled = false;
            CancelSendButton.IsEnabled = false;
            SendFileButton.IsEnabled = true;
            ResumeSendButton.IsEnabled = _pausedSinglePath is not null;
        }
    }

    private async void SendBatchClicked(object? sender, EventArgs e)
    {
        if (_selectedBatchFiles.Length == 0)
        {
            await DisplayAlert("Files required", "Choose one or more files first.", "OK");
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert("Device required", "Validate a fresh pairing invitation first.", "OK");
            return;
        }

        _pausedBatchPaths = Array.Empty<string>();
        ResumeBatchButton.IsEnabled = false;
        await RunBatchSendAsync(remote, _selectedBatchFiles.Select(x => x.FullPath).ToArray());
    }

    private async void ResumeBatchClicked(object? sender, EventArgs e)
    {
        var paths = _pausedBatchPaths.Where(File.Exists).ToArray();
        if (paths.Length == 0)
        {
            _pausedBatchPaths = Array.Empty<string>();
            ResumeBatchButton.IsEnabled = false;
            await DisplayAlert("Resume unavailable", "The original batch source files are no longer available.", "OK");
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert("Fresh pairing required", "Pair with the receiver again, verify its fingerprint, then press Resume.", "OK");
            return;
        }

        await RunBatchSendAsync(remote, paths);
    }

    private async Task RunBatchSendAsync(PairingPayload remote, string[] selectedPaths)
    {
        _batchSendCts?.Dispose();
        _batchSendCts = new CancellationTokenSource();
        _batchPauseRequested = false;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            SendBatchButton.IsEnabled = false;
            PauseBatchButton.IsEnabled = true;
            ResumeBatchButton.IsEnabled = false;
            CancelBatchButton.IsEnabled = true;
            BatchTransferProgress.Progress = 0;
            BatchTransferStatusLabel.Text = _pausedBatchPaths.Length == 0 ? "Preparing checksums…" : "Preparing resume checksums…";
            var progress = new Progress<BatchProgress>(value =>
            {
                BatchTransferProgress.Progress = value.Fraction;
                var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
                var speed = value.CompletedBytes / seconds;
                var remaining = Math.Max(0, value.TotalBytes - value.CompletedBytes);
                var eta = speed <= 1
                    ? "calculating"
                    : TimeSpan.FromSeconds(remaining / speed).ToString(@"hh\:mm\:ss");
                BatchTransferStatusLabel.Text =
                    $"{value.CompletedItems}/{value.TotalItems} files • {FormatBytes(value.CompletedBytes)}/{FormatBytes(value.TotalBytes)} • {FormatBytes((long)speed)}/s • ETA {eta}\n{value.CurrentFile}";
            });

            var result = await _transfers.SendBatchAsync(remote, selectedPaths, progress, _batchSendCts.Token);
            foreach (var item in result.Completed)
            {
                await _history.AddAsync(
                    "sent",
                    remote.DeviceName,
                    item.Entry.RelativePath,
                    item.Entry.Length,
                    "completed",
                    true);
            }
            foreach (var item in result.Skipped)
            {
                await _history.AddAsync(
                    "sent",
                    remote.DeviceName,
                    item.Entry.RelativePath,
                    item.Entry.Length,
                    "not-selected",
                    false);
            }

            _pausedBatchPaths = Array.Empty<string>();
            BatchTransferProgress.Progress = 1;
            BatchTransferStatusLabel.Text = $"Completed {result.Completed.Count:N0} file(s); receiver skipped {result.Skipped.Count:N0}. A fresh pairing invitation is required for another transfer.";
        }
        catch (OperationCanceledException)
        {
            if (_batchPauseRequested)
            {
                _pausedBatchPaths = selectedPaths.Where(File.Exists).ToArray();
                BatchTransferStatusLabel.Text = "Paused safely. Pair with the same receiver again and press Resume. Existing partial files can supply resume offsets.";
                ResumeBatchButton.IsEnabled = _pausedBatchPaths.Length > 0;
                foreach (var path in _pausedBatchPaths)
                {
                    var info = new FileInfo(path);
                    await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "paused", false);
                }
            }
            else
            {
                _pausedBatchPaths = Array.Empty<string>();
                BatchTransferStatusLabel.Text = "Batch cancelled.";
                foreach (var path in selectedPaths)
                {
                    var info = new FileInfo(path);
                    await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Exists ? info.Length : 0, "cancelled", false);
                }
            }
        }
        catch (Exception ex)
        {
            _pausedBatchPaths = selectedPaths.Where(File.Exists).ToArray();
            ResumeBatchButton.IsEnabled = _pausedBatchPaths.Length > 0;
            BatchTransferStatusLabel.Text = _pausedBatchPaths.Length == 0
                ? "Batch failed."
                : "Batch failed safely. Pair with the receiver again and press Resume to reuse any available partial offsets.";
            await DisplayAlert("Batch transfer failed", ex.Message, "OK");
        }
        finally
        {
            stopwatch.Stop();
            SendBatchButton.IsEnabled = true;
            PauseBatchButton.IsEnabled = false;
            CancelBatchButton.IsEnabled = false;
            ResumeBatchButton.IsEnabled = _pausedBatchPaths.Length > 0;
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
        var text = TextSnippetEditor.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert("Text required", "Type or explicitly paste a text snippet first.", "OK");
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert("Device required", "Validate a fresh pairing invitation first.", "OK");
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
    }

    private bool TryTakeRemote(out PairingPayload remote)
    {
        if (_remote is null)
        {
            remote = null!;
            return false;
        }

        remote = _remote;
        _remote = null;
        RemotePeerLabel.Text = $"{remote.DeviceName} invitation in use. Pair again for another transfer or resume.";
        return true;
    }

    private void PauseSendClicked(object? sender, EventArgs e)
    {
        _singlePauseRequested = true;
        _singleSendCts?.Cancel();
    }

    private void ResumeSendClicked(object? sender, EventArgs e)
    {
        _ = ResumeSingleWithErrorBoundaryAsync();
    }

    private async Task ResumeSingleWithErrorBoundaryAsync()
    {
        try
        {
            var path = _pausedSinglePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _pausedSinglePath = null;
                ResumeSendButton.IsEnabled = false;
                await DisplayAlert("Resume unavailable", "The original source file is no longer available.", "OK");
                return;
            }
            if (!TryTakeRemote(out var remote))
            {
                await DisplayAlert("Fresh pairing required", "Pair with the receiver again, verify its fingerprint, then press Resume.", "OK");
                return;
            }
            await RunSingleSendAsync(remote, path);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Resume failed", ex.Message, "OK");
        }
    }

    private void CancelSendClicked(object? sender, EventArgs e)
    {
        _singlePauseRequested = false;
        _singleSendCts?.Cancel();
    }

    private void PauseBatchClicked(object? sender, EventArgs e)
    {
        _batchPauseRequested = true;
        _batchSendCts?.Cancel();
    }

    private void ResumeBatchClicked(object? sender, EventArgs e)
    {
        _ = ResumeBatchWithErrorBoundaryAsync();
    }

    private async Task ResumeBatchWithErrorBoundaryAsync()
    {
        try
        {
            var paths = _pausedBatchPaths.Where(File.Exists).ToArray();
            if (paths.Length == 0)
            {
                _pausedBatchPaths = Array.Empty<string>();
                ResumeBatchButton.IsEnabled = false;
                await DisplayAlert("Resume unavailable", "The original batch source files are no longer available.", "OK");
                return;
            }
            if (!TryTakeRemote(out var remote))
            {
                await DisplayAlert("Fresh pairing required", "Pair with the receiver again, verify its fingerprint, then press Resume.", "OK");
                return;
            }
            await RunBatchSendAsync(remote, paths);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Resume failed", ex.Message, "OK");
        }
    }

    private void CancelBatchClicked(object? sender, EventArgs e)
    {
        _batchPauseRequested = false;
        _batchSendCts?.Cancel();
    }

    private async void OpenDevicesClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DevicesPage>());

    private async void OpenQueueClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<QueuePage>());

    private async void OpenSettingsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

    private async void OpenHistoryClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<HistoryPage>());

    private async void OpenDiagnosticsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DiagnosticsPage>());

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }
}

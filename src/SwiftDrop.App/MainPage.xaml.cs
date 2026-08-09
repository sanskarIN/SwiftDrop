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
    private readonly SemaphoreSlim _receiveServerGate = new(1, 1);

    private PairingPayload? _remote;
    private FileResult? _selectedFile;
    private FileResult[] _selectedBatchFiles = Array.Empty<FileResult>();
    private ReceiveServerService? _receiveServer;
    private string? _activeReceiveRoot;
    private CancellationTokenSource? _singleCts;
    private CancellationTokenSource? _batchCts;
    private bool _pauseSingle;
    private bool _pauseBatch;
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
        _settings.Changed += SettingsChanged;
        Loaded += async (_, _) => await InitializeAsync();
        Unloaded += async (_, _) => await StopReceiveServerAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await EnsureReceiveServerMatchesSettingsAsync();
            await ApplyPendingPairingAsync();
        }
        catch
        {
        }
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

            await EnsureReceiveServerMatchesSettingsAsync();
            await ApplyPendingPairingAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Startup error", ex.Message, "OK");
        }
    }

    private async Task ApplyPendingPairingAsync()
    {
        var payload = _pairingSelection.Current;
        if (payload is null) return;
        _pairingSelection.Clear();
        RemoteLinkEntry.Text = PairingCodec.Encode(payload);
        await ConfirmRemotePairingAsync(payload);
    }

    private async Task<bool> ApproveIncomingAsync(IncomingTransferPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trusted = await _trustedDevices.MatchesAsync(preview.SenderDeviceId, preview.SenderCertificateFingerprint, ct);
        var settings = _settings.Load();
        if (trusted && settings.AutoAcceptTrustedDevices && preview.RiskLevel == FileRiskLevel.Normal)
            return true;

        var warning = preview.RiskLevel switch
        {
            FileRiskLevel.High => "\n\nWARNING: this extension can execute code or install software.",
            FileRiskLevel.Caution => "\n\nCaution: this extension can contain archives or active content.",
            _ => string.Empty
        };
        var accepted = await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(
            "Incoming transfer",
            $"Sender: {preview.SenderDeviceName}\nFile: {preview.Entry.RelativePath}\nSize: {preview.Entry.Length:N0} bytes\nCertificate: {Fingerprint.Pretty(preview.SenderCertificateFingerprint)}{warning}\n\nSwiftDrop never opens received files automatically.",
            "Accept",
            "Reject"));
        if (!accepted || trusted) return accepted;

        var trust = await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(
            "Trust this device?",
            "Trust stores this exact device ID and certificate fingerprint locally. You can revoke it from Settings.",
            "Trust device",
            "Not now"));
        if (trust)
            await _trustedDevices.TrustAsync(preview.SenderDeviceId, preview.SenderDeviceName, preview.SenderCertificateFingerprint, ct);
        return true;
    }

    private async Task<IncomingBatchDecision> ApproveIncomingBatchAsync(IncomingBatchPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var trusted = await _trustedDevices.MatchesAsync(preview.SenderDeviceId, preview.SenderCertificateFingerprint, ct);
        if (trusted && _settings.Load().AutoAcceptTrustedDevices && preview.HighestRisk == FileRiskLevel.Normal)
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
        return await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(
            "Nearby pairing request",
            $"{request.SenderDeviceName} wants to pair.\n\nSender certificate:\n{Fingerprint.Pretty(request.SenderCertificateFingerprint)}\n\nThis device certificate:\n{localFingerprint}\n\nApprove only if you initiated this pairing.",
            "Approve",
            "Reject"));
    }

    private async Task<IncomingTextDecision> ApproveIncomingTextAsync(IncomingTextPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var displayText = preview.Text.Length <= 900 ? preview.Text : preview.Text[..900] + "…";
        var choice = await MainThread.InvokeOnMainThreadAsync(() => DisplayActionSheet(
            $"Text from {preview.SenderDeviceName}\nCertificate: {Fingerprint.Pretty(preview.SenderCertificateFingerprint)}\n{preview.CharacterCount:N0} characters\n\n{displayText}",
            "Reject",
            null,
            "Accept",
            "Accept and copy"));
        if (choice == "Accept and copy")
        {
            await MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(preview.Text));
            return IncomingTextDecision.AcceptAndCopy;
        }
        return choice == "Accept" ? IncomingTextDecision.Accept : IncomingTextDecision.Reject;
    }

    private Task RecordIncomingAsync(IncomingTransferPreview preview, string status, bool verified, CancellationToken ct)
        => _history.AddAsync("received", preview.SenderDeviceName, preview.Entry.RelativePath, preview.Entry.Length, status, verified, ct);

    private Task RecordIncomingTextAsync(IncomingTextPreview preview, string status, CancellationToken ct)
        => _history.AddAsync("received", preview.SenderDeviceName, "Text snippet", Encoding.UTF8.GetByteCount(preview.Text), status, false, ct);

    private void SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        if (!e.ReceiveFolderChanged) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                TransferStatusLabel.Text = "Applying the new receive folder… active incoming transfers may be interrupted.";
                await EnsureReceiveServerMatchesSettingsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Receive folder error", ex.Message, "OK");
            }
        });
    }

    private async Task EnsureReceiveServerMatchesSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(_identity.DeviceId))
            return;

        await _receiveServerGate.WaitAsync();
        try
        {
            var receiveRoot = _receiveLocation.ResolveReceiveRoot();
            if (_receiveServer is not null && PathsEqual(_activeReceiveRoot, receiveRoot))
            {
                ReceiveFolderLabel.Text = $"Receive folder: {receiveRoot}";
                return;
            }

            if (_receiveServer is not null)
            {
                await _receiveServer.DisposeAsync();
                _receiveServer = null;
                _activeReceiveRoot = null;
            }

            var server = new ReceiveServerService(
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
            server.Start();
            _receiveServer = server;
            _activeReceiveRoot = receiveRoot;
            ReceiveFolderLabel.Text = $"Receive folder: {receiveRoot}";
            TransferStatusLabel.Text = $"Ready to receive into {receiveRoot}";
        }
        finally
        {
            _receiveServerGate.Release();
        }
    }

    private async Task StopReceiveServerAsync()
    {
        await _receiveServerGate.WaitAsync();
        try
        {
            if (_receiveServer is null) return;
            await _receiveServer.DisposeAsync();
            _receiveServer = null;
            _activeReceiveRoot = null;
        }
        finally
        {
            _receiveServerGate.Release();
        }
    }

    private static bool PathsEqual(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left)) return false;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), comparison);
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
        PairingCodeExpiryLabel.Text = $"Expires at {snapshot.ExpiresUtc.LocalDateTime:T}. One-time only.";
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
        try { await ConfirmRemotePairingAsync(PairingCodec.Decode(RemoteLinkEntry.Text ?? string.Empty)); }
        catch (Exception ex)
        {
            _remote = null;
            await DisplayAlert("Pairing failed", ex.Message, "OK");
        }
    }

    private async Task ConfirmRemotePairingAsync(PairingPayload payload)
    {
        var pretty = Fingerprint.Pretty(payload.CertificateFingerprint);
        RemotePeerLabel.Text = $"{payload.DeviceName} • {payload.Host}:{payload.Port}\nFingerprint: {pretty}";
        var confirmed = await DisplayAlert("Confirm device fingerprint", $"Verify this fingerprint on the receiving device:\n\n{pretty}", "I verified it", "Cancel");
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

    private async void ChooseMultipleFilesClicked(object? sender, EventArgs e)
    {
        try
        {
            _selectedBatchFiles = (await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = "Choose files to send" }))
                .Where(x => !string.IsNullOrWhiteSpace(x.FullPath))
                .Take(2048)
                .ToArray();
            SelectedBatchLabel.Text = _selectedBatchFiles.Length switch
            {
                0 => "No batch selected",
                1 => _selectedBatchFiles[0].FileName,
                _ => $"{_selectedBatchFiles.Length:N0} files selected"
            };
        }
        catch (Exception ex) { await DisplayAlert("File selection failed", ex.Message, "OK"); }
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
            await DisplayAlert("Fresh pairing required", "Pair with the same receiver again, verify its fingerprint, then press Resume.", "OK");
            return;
        }
        await RunSingleSendAsync(remote, path);
    }

    private async Task RunSingleSendAsync(PairingPayload remote, string path)
    {
        _singleCts?.Dispose();
        _singleCts = new CancellationTokenSource();
        _pauseSingle = false;
        var info = new FileInfo(path);
        try
        {
            SendFileButton.IsEnabled = false;
            PauseSendButton.IsEnabled = true;
            ResumeSendButton.IsEnabled = false;
            CancelSendButton.IsEnabled = true;
            TransferStatusLabel.Text = _pausedSinglePath is null ? "Sending…" : "Resuming…";
            await _transfers.SendAsync(remote, path, new Progress<double>(x => TransferProgress.Progress = x), _singleCts.Token);
            TransferProgress.Progress = 1;
            _pausedSinglePath = null;
            TransferStatusLabel.Text = "Completed and verified. Pair again for another transfer.";
            await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "completed", true);
        }
        catch (OperationCanceledException)
        {
            if (_pauseSingle)
            {
                _pausedSinglePath = path;
                TransferStatusLabel.Text = "Paused. Pair with the same receiver again, then press Resume.";
                await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "paused", false);
            }
            else
            {
                _pausedSinglePath = null;
                TransferStatusLabel.Text = "Cancelled.";
                await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "cancelled", false);
            }
        }
        catch (Exception ex)
        {
            _pausedSinglePath = File.Exists(path) ? path : null;
            TransferStatusLabel.Text = _pausedSinglePath is null ? "Failed." : "Failed safely. Pair again and press Resume to reuse any partial offset.";
            await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Exists ? info.Length : 0, "failed", false);
            await DisplayAlert("Transfer failed", ex.Message, "OK");
        }
        finally
        {
            SendFileButton.IsEnabled = true;
            PauseSendButton.IsEnabled = false;
            CancelSendButton.IsEnabled = false;
            ResumeSendButton.IsEnabled = _pausedSinglePath is not null;
        }
    }

    private void PauseSendClicked(object? sender, EventArgs e)
    {
        _pauseSingle = true;
        _singleCts?.Cancel();
    }

    private void CancelSendClicked(object? sender, EventArgs e)
    {
        _pauseSingle = false;
        _singleCts?.Cancel();
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
        await RunBatchSendAsync(remote, _selectedBatchFiles.Select(x => x.FullPath).ToArray());
    }

    private async void ResumeBatchClicked(object? sender, EventArgs e)
    {
        var paths = _pausedBatchPaths.Where(File.Exists).ToArray();
        if (paths.Length == 0)
        {
            _pausedBatchPaths = Array.Empty<string>();
            ResumeBatchButton.IsEnabled = false;
            await DisplayAlert("Resume unavailable", "The original batch files are no longer available.", "OK");
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert("Fresh pairing required", "Pair with the same receiver again, verify its fingerprint, then press Resume.", "OK");
            return;
        }
        await RunBatchSendAsync(remote, paths);
    }

    private async Task RunBatchSendAsync(PairingPayload remote, string[] paths)
    {
        _batchCts?.Dispose();
        _batchCts = new CancellationTokenSource();
        _pauseBatch = false;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            SendBatchButton.IsEnabled = false;
            PauseBatchButton.IsEnabled = true;
            ResumeBatchButton.IsEnabled = false;
            CancelBatchButton.IsEnabled = true;
            BatchTransferStatusLabel.Text = _pausedBatchPaths.Length == 0 ? "Preparing checksums…" : "Preparing resume checksums…";
            var progress = new Progress<BatchProgress>(value =>
            {
                BatchTransferProgress.Progress = value.Fraction;
                var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, .001);
                var speed = value.CompletedBytes / seconds;
                var remaining = Math.Max(0, value.TotalBytes - value.CompletedBytes);
                var eta = speed <= 1 ? "calculating" : TimeSpan.FromSeconds(remaining / speed).ToString(@"hh\:mm\:ss");
                BatchTransferStatusLabel.Text = $"{value.CompletedItems}/{value.TotalItems} files • {FormatBytes(value.CompletedBytes)}/{FormatBytes(value.TotalBytes)} • {FormatBytes((long)speed)}/s • ETA {eta}\n{value.CurrentFile}";
            });

            var result = await _transfers.SendBatchAsync(remote, paths, progress, _batchCts.Token);
            foreach (var item in result.Completed)
                await _history.AddAsync("sent", remote.DeviceName, item.Entry.RelativePath, item.Entry.Length, "completed", true);
            foreach (var item in result.Skipped)
                await _history.AddAsync("sent", remote.DeviceName, item.Entry.RelativePath, item.Entry.Length, "not-selected", false);
            _pausedBatchPaths = Array.Empty<string>();
            BatchTransferProgress.Progress = 1;
            BatchTransferStatusLabel.Text = $"Completed {result.Completed.Count:N0} file(s); receiver skipped {result.Skipped.Count:N0}. Pair again for another transfer.";
        }
        catch (OperationCanceledException)
        {
            if (_pauseBatch)
            {
                _pausedBatchPaths = paths.Where(File.Exists).ToArray();
                BatchTransferStatusLabel.Text = "Paused. Pair with the same receiver again, then press Resume.";
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
            }
        }
        catch (Exception ex)
        {
            _pausedBatchPaths = paths.Where(File.Exists).ToArray();
            BatchTransferStatusLabel.Text = _pausedBatchPaths.Length == 0 ? "Batch failed." : "Batch failed safely. Pair again and press Resume.";
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

    private void PauseBatchClicked(object? sender, EventArgs e)
    {
        _pauseBatch = true;
        _batchCts?.Cancel();
    }

    private void CancelBatchClicked(object? sender, EventArgs e)
    {
        _pauseBatch = false;
        _batchCts?.Cancel();
    }

    private async void PasteClipboardClicked(object? sender, EventArgs e)
    {
        try
        {
            TextSnippetEditor.Text = await Clipboard.Default.GetTextAsync() ?? string.Empty;
            TextTransferStatusLabel.Text = "Clipboard read once by your explicit action.";
        }
        catch (Exception ex) { await DisplayAlert("Clipboard unavailable", ex.Message, "OK"); }
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
            await _history.AddAsync("sent", remote.DeviceName, "Text snippet", Encoding.UTF8.GetByteCount(text), "completed", false);
            TextSnippetEditor.Text = string.Empty;
            TextTransferStatusLabel.Text = "Text delivered. Pair again for another transfer.";
        }
        catch (Exception ex)
        {
            await _history.AddAsync("sent", remote.DeviceName, "Text snippet", Encoding.UTF8.GetByteCount(text), "failed", false);
            TextTransferStatusLabel.Text = "Text transfer failed.";
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

    private async void OpenDevicesClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<DevicesPage>());

    private async void OpenQueueClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<QueuePage>());

    private async void OpenHistoryClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<HistoryPage>());

    private async void OpenSettingsClicked(object? sender, EventArgs e)
        => await Navigation.PushAsync(_services.GetRequiredService<SettingsPage>());

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

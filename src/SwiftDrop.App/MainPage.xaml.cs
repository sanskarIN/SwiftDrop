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
            DeviceFingerprintLabel.Text = AppText.Format(
                "CertificateFormat",
                Fingerprint.Pretty(Fingerprint.FromCertificate(_identity.Certificate)));

            await EnsureReceiveServerMatchesSettingsAsync();
            await ApplyPendingPairingAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppText.Get("StartupError"), ex.Message, AppText.Get("Ok"));
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
            FileRiskLevel.High => AppText.Get("HighRiskWarning"),
            FileRiskLevel.Caution => AppText.Get("CautionRiskWarning"),
            _ => string.Empty
        };
        var accepted = await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(
            AppText.Get("IncomingTransfer"),
            AppText.Format(
                "IncomingTransferFormat",
                preview.SenderDeviceName,
                preview.Entry.RelativePath,
                preview.Entry.Length,
                Fingerprint.Pretty(preview.SenderCertificateFingerprint),
                warning),
            AppText.Get("Accept"),
            AppText.Get("Reject")));
        if (!accepted || trusted) return accepted;

        var trust = await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(
            AppText.Get("TrustThisDeviceQuestion"),
            AppText.Get("TrustThisDeviceMessage"),
            AppText.Get("TrustDevice"),
            AppText.Get("NotNow")));
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
            AppText.Get("NearbyPairingRequest"),
            AppText.Format(
                "NearbyPairingRequestFormat",
                request.SenderDeviceName,
                Fingerprint.Pretty(request.SenderCertificateFingerprint),
                localFingerprint),
            AppText.Get("Approve"),
            AppText.Get("Reject")));
    }

    private async Task<IncomingTextDecision> ApproveIncomingTextAsync(IncomingTextPreview preview, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var displayText = preview.Text.Length <= 900 ? preview.Text : preview.Text[..900] + "…";
        var accept = AppText.Get("Accept");
        var acceptAndCopy = AppText.Get("AcceptAndCopy");
        var choice = await MainThread.InvokeOnMainThreadAsync(() => DisplayActionSheet(
            AppText.Format(
                "TextFromFormat",
                preview.SenderDeviceName,
                Fingerprint.Pretty(preview.SenderCertificateFingerprint),
                preview.CharacterCount,
                displayText),
            AppText.Get("Reject"),
            null,
            accept,
            acceptAndCopy));
        if (string.Equals(choice, acceptAndCopy, StringComparison.Ordinal))
        {
            await MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(preview.Text));
            return IncomingTextDecision.AcceptAndCopy;
        }
        return string.Equals(choice, accept, StringComparison.Ordinal)
            ? IncomingTextDecision.Accept
            : IncomingTextDecision.Reject;
    }

    private Task RecordIncomingAsync(IncomingTransferPreview preview, string status, bool verified, CancellationToken ct)
        => _history.AddAsync("received", preview.SenderDeviceName, preview.Entry.RelativePath, preview.Entry.Length, status, verified, ct);

    private Task RecordIncomingTextAsync(IncomingTextPreview preview, string status, CancellationToken ct)
        => _history.AddAsync(
            "received",
            preview.SenderDeviceName,
            AppText.Get("TextSnippetHistoryLabel"),
            Encoding.UTF8.GetByteCount(preview.Text),
            status,
            false,
            ct);

    private void SettingsChanged(object? sender, AppSettingsChangedEventArgs e)
    {
        if (!e.ReceiveFolderChanged) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                TransferStatusLabel.Text = AppText.Get("ApplyingReceiveFolder");
                await EnsureReceiveServerMatchesSettingsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert(AppText.Get("ReceiveFolderError"), ex.Message, AppText.Get("Ok"));
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
                ReceiveFolderLabel.Text = AppText.Format("ReceiveFolderFormat", receiveRoot);
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
            ReceiveFolderLabel.Text = AppText.Format("ReceiveFolderFormat", receiveRoot);
            TransferStatusLabel.Text = AppText.Format("ReadyToReceiveFormat", receiveRoot);
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
        PairingCodeExpiryLabel.Text = AppText.Format("PairingCodeExpiryFormat", snapshot.ExpiresUtc.LocalDateTime);
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
                Title = AppText.Get("PairingLinkShareTitle")
            });
        }
    }

    private async void ValidatePairingClicked(object? sender, EventArgs e)
    {
        try
        {
            await ConfirmRemotePairingAsync(PairingCodec.Decode(RemoteLinkEntry.Text ?? string.Empty));
        }
        catch (Exception ex)
        {
            _remote = null;
            await DisplayAlert(AppText.Get("PairingFailed"), ex.Message, AppText.Get("Ok"));
        }
    }

    private async Task ConfirmRemotePairingAsync(PairingPayload payload)
    {
        var pretty = Fingerprint.Pretty(payload.CertificateFingerprint);
        RemotePeerLabel.Text = AppText.Format("RemotePeerFormat", payload.DeviceName, payload.Host, payload.Port, pretty);
        var confirmed = await DisplayAlert(
            AppText.Get("ConfirmDeviceFingerprint"),
            AppText.Format("VerifyFingerprintFormat", pretty),
            AppText.Get("IVerifiedIt"),
            AppText.Get("Cancel"));
        if (confirmed)
        {
            _remote = payload;
            RemotePeerLabel.Text += Environment.NewLine + AppText.Get("VerifiedInvitationStatus");
        }
        else
        {
            _remote = null;
            RemotePeerLabel.Text = AppText.Get("PairingCancelledStatus");
        }
    }

    private async void ChooseFileClicked(object? sender, EventArgs e)
    {
        _selectedFile = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = AppText.Get("ChooseFileToSend")
        });
        SelectedFileLabel.Text = _selectedFile?.FullPath ?? AppText.Get("NoFileSelected");
    }

    private async void ChooseMultipleFilesClicked(object? sender, EventArgs e)
    {
        try
        {
            _selectedBatchFiles = (await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = AppText.Get("ChooseFilesToSend")
                }))
                .Where(x => !string.IsNullOrWhiteSpace(x.FullPath))
                .Take(2048)
                .ToArray();
            SelectedBatchLabel.Text = _selectedBatchFiles.Length switch
            {
                0 => AppText.Get("NoBatchSelected"),
                1 => _selectedBatchFiles[0].FileName,
                _ => AppText.Format("FilesSelectedFormat", _selectedBatchFiles.Length)
            };
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppText.Get("FileSelectionFailed"), ex.Message, AppText.Get("Ok"));
        }
    }

    private async void SendFileClicked(object? sender, EventArgs e)
    {
        if (_selectedFile is null)
        {
            await DisplayAlert(AppText.Get("FileRequired"), AppText.Get("ChooseFileFirst"), AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("DeviceRequired"),
                AppText.Get("FreshPairingInvitationFirst"),
                AppText.Get("Ok"));
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
            await DisplayAlert(
                AppText.Get("ResumeUnavailable"),
                AppText.Get("SourceFileUnavailable"),
                AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("FreshPairingRequired"),
                AppText.Get("FreshPairingResumeMessage"),
                AppText.Get("Ok"));
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
            TransferStatusLabel.Text = _pausedSinglePath is null
                ? AppText.Get("SendingStatus")
                : AppText.Get("ResumingStatus");
            await _transfers.SendAsync(
                remote,
                path,
                new Progress<double>(x => TransferProgress.Progress = x),
                _singleCts.Token);
            TransferProgress.Progress = 1;
            _pausedSinglePath = null;
            TransferStatusLabel.Text = AppText.Get("CompletedVerifiedStatus");
            await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "completed", true);
        }
        catch (OperationCanceledException)
        {
            if (_pauseSingle)
            {
                _pausedSinglePath = path;
                TransferStatusLabel.Text = AppText.Get("PausedResumeStatus");
                await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "paused", false);
            }
            else
            {
                _pausedSinglePath = null;
                TransferStatusLabel.Text = AppText.Get("CancelledStatus");
                await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "cancelled", false);
            }
        }
        catch (Exception ex)
        {
            _pausedSinglePath = File.Exists(path) ? path : null;
            TransferStatusLabel.Text = _pausedSinglePath is null
                ? AppText.Get("FailedStatus")
                : AppText.Get("FailedSafeResumeStatus");
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                info.Name,
                info.Exists ? info.Length : 0,
                "failed",
                false);
            await DisplayAlert(AppText.Get("TransferFailed"), ex.Message, AppText.Get("Ok"));
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
            await DisplayAlert(AppText.Get("FilesRequired"), AppText.Get("ChooseFilesFirst"), AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("DeviceRequired"),
                AppText.Get("FreshPairingInvitationFirst"),
                AppText.Get("Ok"));
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
            await DisplayAlert(
                AppText.Get("ResumeUnavailable"),
                AppText.Get("BatchFilesUnavailable"),
                AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("FreshPairingRequired"),
                AppText.Get("FreshPairingResumeMessage"),
                AppText.Get("Ok"));
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
            BatchTransferStatusLabel.Text = _pausedBatchPaths.Length == 0
                ? AppText.Get("PreparingChecksums")
                : AppText.Get("PreparingResumeChecksums");
            var progress = new Progress<BatchProgress>(value =>
            {
                BatchTransferProgress.Progress = value.Fraction;
                var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, .001);
                var speed = value.CompletedBytes / seconds;
                var remaining = Math.Max(0, value.TotalBytes - value.CompletedBytes);
                var eta = speed <= 1
                    ? AppText.Get("Calculating")
                    : TimeSpan.FromSeconds(remaining / speed).ToString(@"hh\:mm\:ss");
                BatchTransferStatusLabel.Text = AppText.Format(
                    "BatchProgressFormat",
                    value.CompletedItems,
                    value.TotalItems,
                    FormatBytes(value.CompletedBytes),
                    FormatBytes(value.TotalBytes),
                    FormatBytes((long)speed),
                    eta,
                    value.CurrentFile);
            });

            var result = await _transfers.SendBatchAsync(remote, paths, progress, _batchCts.Token);
            foreach (var item in result.Completed)
                await _history.AddAsync("sent", remote.DeviceName, item.Entry.RelativePath, item.Entry.Length, "completed", true);
            foreach (var item in result.Skipped)
                await _history.AddAsync("sent", remote.DeviceName, item.Entry.RelativePath, item.Entry.Length, "not-selected", false);
            _pausedBatchPaths = Array.Empty<string>();
            BatchTransferProgress.Progress = 1;
            BatchTransferStatusLabel.Text = AppText.Format(
                "BatchCompletedFormat",
                result.Completed.Count,
                result.Skipped.Count);
        }
        catch (OperationCanceledException)
        {
            if (_pauseBatch)
            {
                _pausedBatchPaths = paths.Where(File.Exists).ToArray();
                BatchTransferStatusLabel.Text = AppText.Get("PausedResumeStatus");
                foreach (var path in _pausedBatchPaths)
                {
                    var info = new FileInfo(path);
                    await _history.AddAsync("sent", remote.DeviceName, info.Name, info.Length, "paused", false);
                }
            }
            else
            {
                _pausedBatchPaths = Array.Empty<string>();
                BatchTransferStatusLabel.Text = AppText.Get("BatchCancelledStatus");
            }
        }
        catch (Exception ex)
        {
            _pausedBatchPaths = paths.Where(File.Exists).ToArray();
            BatchTransferStatusLabel.Text = _pausedBatchPaths.Length == 0
                ? AppText.Get("BatchFailedStatus")
                : AppText.Get("BatchFailedSafeResumeStatus");
            await DisplayAlert(AppText.Get("BatchTransferFailed"), ex.Message, AppText.Get("Ok"));
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
            TextTransferStatusLabel.Text = AppText.Get("ClipboardReadOnce");
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppText.Get("ClipboardUnavailable"), ex.Message, AppText.Get("Ok"));
        }
    }

    private async void SendTextClicked(object? sender, EventArgs e)
    {
        var text = TextSnippetEditor.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert(AppText.Get("TextRequired"), AppText.Get("TextRequiredMessage"), AppText.Get("Ok"));
            return;
        }
        if (!TryTakeRemote(out var remote))
        {
            await DisplayAlert(
                AppText.Get("DeviceRequired"),
                AppText.Get("FreshPairingInvitationFirst"),
                AppText.Get("Ok"));
            return;
        }
        try
        {
            TextTransferStatusLabel.Text = AppText.Get("SendingEncryptedText");
            await _transfers.SendTextAsync(remote, text, CancellationToken.None);
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                AppText.Get("TextSnippetHistoryLabel"),
                Encoding.UTF8.GetByteCount(text),
                "completed",
                false);
            TextSnippetEditor.Text = string.Empty;
            TextTransferStatusLabel.Text = AppText.Get("TextDelivered");
        }
        catch (Exception ex)
        {
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                AppText.Get("TextSnippetHistoryLabel"),
                Encoding.UTF8.GetByteCount(text),
                "failed",
                false);
            TextTransferStatusLabel.Text = AppText.Get("TextTransferFailedStatus");
            await DisplayAlert(AppText.Get("TextTransferFailed"), ex.Message, AppText.Get("Ok"));
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
        RemotePeerLabel.Text = AppText.Format("InvitationInUseFormat", remote.DeviceName);
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

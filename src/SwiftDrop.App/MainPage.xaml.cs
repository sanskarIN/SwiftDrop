using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using QRCoder;
using SwiftDrop.App.Services;
using SwiftDrop.App.ViewModels;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

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
    private readonly MainViewModel _viewModel;
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
        MainViewModel viewModel,
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
        _viewModel = viewModel;
        _services = services;
        BindingContext = viewModel;
        _viewModel.SelectedFile = AppText.Get("NoFileSelected");
        _viewModel.SelectedBatch = AppText.Get("NoBatchSelected");
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
            _viewModel.DeviceName = _identity.DeviceName;
            _viewModel.DeviceId = _identity.DeviceId;
            _viewModel.DeviceFingerprint = AppText.Format(
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

    private Task RecordIncomingAsync(
        IncomingTransferPreview preview,
        string status,
        bool verified,
        TimeSpan? duration,
        CancellationToken ct)
        => _history.AddAsync(
            "received",
            preview.SenderDeviceName,
            preview.Entry.RelativePath,
            preview.Entry.Length,
            status,
            verified,
            ct,
            duration);

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
                _viewModel.TransferStatus = AppText.Get("ApplyingReceiveFolder");
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
                _viewModel.ReceiveFolder = AppText.Format("ReceiveFolderFormat", receiveRoot);
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
            _viewModel.ReceiveFolder = AppText.Format("ReceiveFolderFormat", receiveRoot);
            _viewModel.TransferStatus = AppText.Format("ReadyToReceiveFormat", receiveRoot);
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
        _viewModel.RemotePeer = AppText.Format("RemotePeerFormat", payload.DeviceName, payload.Host, payload.Port, pretty);
        var confirmed = await DisplayAlert(
            AppText.Get("ConfirmDeviceFingerprint"),
            AppText.Format("VerifyFingerprintFormat", pretty),
            AppText.Get("IVerifiedIt"),
            AppText.Get("Cancel"));
        if (confirmed)
        {
            _remote = payload;
            _viewModel.RemotePeer += Environment.NewLine + AppText.Get("VerifiedInvitationStatus");
        }
        else
        {
            _remote = null;
            _viewModel.RemotePeer = AppText.Get("PairingCancelledStatus");
        }
    }

    private async void ChooseFileClicked(object? sender, EventArgs e)
    {
        _selectedFile = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = AppText.Get("ChooseFileToSend")
        });
        _viewModel.SelectedFile = _selectedFile?.FullPath ?? AppText.Get("NoFileSelected");
    }

    private async void ChooseMultipleFilesClicked(object? sender, EventArgs e)
    {
        try
        {
            var pickedFiles = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = AppText.Get("ChooseFilesToSend")
            });
            _selectedBatchFiles = (pickedFiles ?? Array.Empty<FileResult>())
                .OfType<FileResult>()
                .Where(x => !string.IsNullOrWhiteSpace(x.FullPath))
                .Take(2048)
                .ToArray();
            _viewModel.SelectedBatch = _selectedBatchFiles.Length switch
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

        var sourcePath = TransferSourcePathPolicy.ExistingDistinct([_selectedFile.FullPath]).SingleOrDefault();
        if (sourcePath is null)
        {
            await DisplayAlert(AppText.Get("ResumeUnavailable"), AppText.Get("SourceFileUnavailable"), AppText.Get("Ok"));
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
        await RunSingleSendAsync(remote, sourcePath);
    }

    private async void ResumeSendClicked(object? sender, EventArgs e)
    {
        var path = TransferSourcePathPolicy.ExistingDistinct(
                string.IsNullOrWhiteSpace(_pausedSinglePath) ? Array.Empty<string>() : [_pausedSinglePath])
            .SingleOrDefault();
        if (path is null)
        {
            _pausedSinglePath = null;
            _viewModel.ResumeSendEnabled = false;
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
        var stopwatch = Stopwatch.StartNew();
        FileInfo? info = null;
        try
        {
            info = TransferSourceSafety.GetRegularFile(path);
            _viewModel.SetSingleTransferControls(sending: true, canResume: false);
            _viewModel.TransferStatus = _pausedSinglePath is null
                ? AppText.Get("SendingStatus")
                : AppText.Get("ResumingStatus");
            var sendResult = await _transfers.SendAsync(
                remote,
                info.FullName,
                new Progress<double>(x => _viewModel.TransferProgress = x),
                _singleCts.Token);
            _viewModel.TransferProgress = 1;
            _pausedSinglePath = null;
            _viewModel.TransferStatus = AppText.Get("CompletedVerifiedStatus");
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                info.Name,
                info.Length,
                "completed",
                true,
                duration: stopwatch.Elapsed,
                measuredBytes: sendResult.TransferredBytes);
        }
        catch (OperationCanceledException)
        {
            if (_pauseSingle)
            {
                _pausedSinglePath = TransferSourcePathPolicy.ExistingDistinct([path]).SingleOrDefault();
                _viewModel.TransferStatus = _pausedSinglePath is null
                    ? AppText.Get("FailedStatus")
                    : AppText.Get("PausedResumeStatus");
                await _history.AddAsync(
                    "sent",
                    remote.DeviceName,
                    info?.Name ?? Path.GetFileName(path),
                    info?.Length ?? 0,
                    "paused",
                    false,
                    duration: stopwatch.Elapsed);
            }
            else
            {
                _pausedSinglePath = null;
                _viewModel.TransferStatus = AppText.Get("CancelledStatus");
                await _history.AddAsync(
                    "sent",
                    remote.DeviceName,
                    info?.Name ?? Path.GetFileName(path),
                    info?.Length ?? 0,
                    "cancelled",
                    false,
                    duration: stopwatch.Elapsed);
            }
        }
        catch (Exception ex)
        {
            _pausedSinglePath = TransferSourcePathPolicy.ExistingDistinct([path]).SingleOrDefault();
            _viewModel.TransferStatus = _pausedSinglePath is null
                ? AppText.Get("FailedStatus")
                : AppText.Get("FailedSafeResumeStatus");
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                info?.Name ?? Path.GetFileName(path),
                info?.Length ?? 0,
                "failed",
                false,
                duration: stopwatch.Elapsed);
            await DisplayAlert(AppText.Get("TransferFailed"), ex.Message, AppText.Get("Ok"));
        }
        finally
        {
            stopwatch.Stop();
            _viewModel.SetSingleTransferControls(sending: false, canResume: _pausedSinglePath is not null);
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

    private async void PasteClipboardClicked(object? sender, EventArgs e)
    {
        try
        {
            TextSnippetEditor.Text = await Clipboard.Default.GetTextAsync() ?? string.Empty;
            _viewModel.TextTransferStatus = AppText.Get("ClipboardReadOnce");
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
        var textStopwatch = Stopwatch.StartNew();
        try
        {
            _viewModel.TextTransferStatus = AppText.Get("SendingEncryptedText");
            await _transfers.SendTextAsync(remote, text, CancellationToken.None);
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                AppText.Get("TextSnippetHistoryLabel"),
                Encoding.UTF8.GetByteCount(text),
                "completed",
                false,
                duration: textStopwatch.Elapsed,
                measuredBytes: Encoding.UTF8.GetByteCount(text));
            TextSnippetEditor.Text = string.Empty;
            _viewModel.TextTransferStatus = AppText.Get("TextDelivered");
        }
        catch (Exception ex)
        {
            await _history.AddAsync(
                "sent",
                remote.DeviceName,
                AppText.Get("TextSnippetHistoryLabel"),
                Encoding.UTF8.GetByteCount(text),
                "failed",
                false,
                duration: textStopwatch.Elapsed);
            _viewModel.TextTransferStatus = AppText.Get("TextTransferFailedStatus");
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
        _viewModel.RemotePeer = AppText.Format("InvitationInUseFormat", remote.DeviceName);
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

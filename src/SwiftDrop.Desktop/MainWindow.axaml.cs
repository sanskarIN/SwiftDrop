using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Desktop.Services;

namespace SwiftDrop.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly DesktopIdentityService _identity = new();
    private readonly DesktopDiscoveryService _discovery;
    private readonly DesktopPairingService _pairing;
    private readonly DesktopTransferClient _transferClient;
    private readonly CancellationTokenSource _lifetimeCts = new();

    private readonly TextBlock _deviceText;
    private readonly TextBlock _statusText;
    private readonly ListBox _nearbyList;
    private readonly TextBox _remotePairingLinkBox;
    private readonly TextBox _manualHostBox;
    private readonly TextBox _manualPortBox;
    private readonly TextBox _manualCodeBox;
    private readonly TextBlock _remoteDeviceText;
    private readonly ProgressBar _transferProgress;
    private readonly TextBlock _transferDetailText;
    private readonly TextBox _sendTextBox;
    private readonly TextBox _receiveRootBox;
    private readonly TextBox _localPairingLinkBox;
    private readonly TextBlock _pairingCodeText;
    private readonly Button _sendFilesButton;
    private readonly Button _sendFolderButton;
    private readonly Button _sendTextButton;
    private readonly Button _cancelSendButton;

    private DesktopReceiveServerService? _server;
    private CancellationTokenSource? _sendCts;
    private PairingPayload? _remote;
    private List<PeerDevice> _peers = [];
    private string _receiveRoot = DesktopPaths.DefaultReceiveRoot;
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
        _discovery = new DesktopDiscoveryService(_identity);
        _pairing = new DesktopPairingService(_identity);
        _transferClient = new DesktopTransferClient(_identity);

        _deviceText = Find<TextBlock>("DeviceText");
        _statusText = Find<TextBlock>("StatusText");
        _nearbyList = Find<ListBox>("NearbyList");
        _remotePairingLinkBox = Find<TextBox>("RemotePairingLinkBox");
        _manualHostBox = Find<TextBox>("ManualHostBox");
        _manualPortBox = Find<TextBox>("ManualPortBox");
        _manualCodeBox = Find<TextBox>("ManualCodeBox");
        _remoteDeviceText = Find<TextBlock>("RemoteDeviceText");
        _transferProgress = Find<ProgressBar>("TransferProgress");
        _transferDetailText = Find<TextBlock>("TransferDetailText");
        _sendTextBox = Find<TextBox>("SendTextBox");
        _receiveRootBox = Find<TextBox>("ReceiveRootBox");
        _localPairingLinkBox = Find<TextBox>("LocalPairingLinkBox");
        _pairingCodeText = Find<TextBlock>("PairingCodeText");
        _sendFilesButton = Find<Button>("SendFilesButton");
        _sendFolderButton = Find<Button>("SendFolderButton");
        _sendTextButton = Find<Button>("SendTextButton");
        _cancelSendButton = Find<Button>("CancelSendButton");

        Find<Button>("PairNearbyButton").Click += PairNearbyClicked;
        Find<Button>("RefreshNearbyButton").Click += RefreshNearbyClicked;
        Find<Button>("UsePairingLinkButton").Click += UsePairingLinkClicked;
        Find<Button>("ManualPairButton").Click += ManualPairClicked;
        _sendFilesButton.Click += SendFilesClicked;
        _sendFolderButton.Click += SendFolderClicked;
        _sendTextButton.Click += SendTextClicked;
        _cancelSendButton.Click += CancelSendClicked;
        Find<Button>("ChangeReceiveRootButton").Click += ChangeReceiveRootClicked;
        Find<Button>("RefreshPairingLinkButton").Click += RefreshPairingLinkClicked;
        Find<Button>("CopyPairingLinkButton").Click += CopyPairingLinkClicked;
        Find<Button>("GeneratePairingCodeButton").Click += GeneratePairingCodeClicked;

        _discovery.PeersChanged += DiscoveryPeersChanged;
        Opened += WindowOpened;
        Closed += WindowClosed;
    }

    private void InitializeComponent()
        => AvaloniaXamlLoader.Load(this);

    private T Find<T>(string name) where T : Control
        => this.FindControl<T>(name) ?? throw new InvalidOperationException($"Required control '{name}' was not found.");

    private async void WindowOpened(object? sender, EventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            SetStatus("Initializing…");
            await _identity.InitializeAsync();
            _deviceText.Text = $"{_identity.DeviceName} • {_identity.PlatformName} • ID {_identity.DeviceId[..8]}";

            Directory.CreateDirectory(_receiveRoot);
            _receiveRootBox.Text = _receiveRoot;
            await RestartReceiveServerAsync(_receiveRoot);
            RefreshLocalPairingLink();
            await _discovery.StartAsync(_lifetimeCts.Token);
            RefreshNearby();
            SetStatus("Ready on local network");
        }
        catch (OperationCanceledException) when (_lifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            SetStatus($"Startup error: {ex.Message}");
        }
    }

    private async void WindowClosed(object? sender, EventArgs e)
    {
        _lifetimeCts.Cancel();
        _sendCts?.Cancel();
        try
        {
            if (_server is not null) await _server.DisposeAsync();
            await _discovery.DisposeAsync();
            await _identity.DisposeAsync();
        }
        catch
        {
        }
        finally
        {
            _sendCts?.Dispose();
            _lifetimeCts.Dispose();
        }
    }

    private void DiscoveryPeersChanged(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(RefreshNearby);

    private void RefreshNearbyClicked(object? sender, RoutedEventArgs e)
        => RefreshNearby();

    private void RefreshNearby()
    {
        _peers = _discovery.Snapshot().ToList();
        _nearbyList.ItemsSource = _peers
            .Select(peer => $"{peer.Name} • {peer.Platform} • {peer.Host}:{peer.Port}")
            .ToArray();
    }

    private async void PairNearbyClicked(object? sender, RoutedEventArgs e)
    {
        var index = _nearbyList.SelectedIndex;
        if (index < 0 || index >= _peers.Count)
        {
            SetStatus("Select a nearby device first.");
            return;
        }

        try
        {
            SetStatus("Requesting pairing approval…");
            _remote = await _pairing.RequestAsync(_peers[index], ct: _lifetimeCts.Token);
            ShowRemote(_remote);
            SetStatus("Pairing authorization ready");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus($"Pairing failed: {ex.Message}");
        }
    }

    private void UsePairingLinkClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            _remote = _pairing.DecodePairingLink(_remotePairingLinkBox.Text ?? string.Empty);
            ShowRemote(_remote);
            SetStatus("Pairing link ready");
        }
        catch (Exception ex)
        {
            SetStatus($"Invalid pairing link: {ex.Message}");
        }
    }

    private async void ManualPairClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(_manualPortBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var port))
                throw new ArgumentException("Port must be a number between 1 and 65535.");

            SetStatus("Connecting for manual pairing…");
            _remote = await _pairing.RequestManualIpAsync(
                _manualHostBox.Text ?? string.Empty,
                port,
                _manualCodeBox.Text ?? string.Empty,
                _lifetimeCts.Token);
            ShowRemote(_remote);
            SetStatus("Manual pairing authorization ready");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SetStatus($"Manual pairing failed: {ex.Message}");
        }
    }

    private async void SendFilesClicked(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose files to send with SwiftDrop",
            AllowMultiple = true
        });
        var paths = files.Select(x => x.Path.LocalPath).Where(File.Exists).ToArray();
        if (paths.Length == 0) return;
        await SendPathsAsync(paths);
    }

    private async void SendFolderClicked(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose a folder to send with SwiftDrop",
            AllowMultiple = false
        });
        var path = folders.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        await SendPathsAsync([path]);
    }

    private async Task SendPathsAsync(IReadOnlyList<string> paths)
    {
        if (_remote is null)
        {
            SetStatus("Pair with a receiving device first.");
            return;
        }

        var remote = _remote;
        _sendCts?.Dispose();
        _sendCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        SetSending(true);
        _transferProgress.Value = 0;
        try
        {
            if (paths.Count == 1 && File.Exists(paths[0]))
            {
                _transferDetailText.Text = $"Sending {Path.GetFileName(paths[0])}…";
                var progress = new Progress<double>(value => _transferProgress.Value = value * 100d);
                await _transferClient.SendFileAsync(remote, paths[0], progress, _sendCts.Token);
            }
            else
            {
                _transferDetailText.Text = "Preparing batch manifest…";
                var progress = new Progress<DesktopBatchProgress>(value =>
                {
                    _transferProgress.Value = value.Fraction * 100d;
                    _transferDetailText.Text = $"{value.CompletedItems}/{value.TotalItems} • {value.CurrentFile}";
                });
                await _transferClient.SendBatchAsync(
                    remote,
                    paths,
                    Guid.NewGuid().ToString("N"),
                    progress,
                    _sendCts.Token);
            }

            _transferProgress.Value = 100;
            _transferDetailText.Text = "Transfer completed and verified by the receiver.";
            SetStatus("Transfer completed");
        }
        catch (OperationCanceledException) when (_sendCts.IsCancellationRequested)
        {
            _transferDetailText.Text = "Transfer cancelled.";
            SetStatus("Transfer cancelled");
        }
        catch (Exception ex)
        {
            _transferDetailText.Text = $"Transfer failed safely: {ex.Message}";
            SetStatus("Transfer failed");
        }
        finally
        {
            _remote = null;
            _remoteDeviceText.Text = "Authorization consumed or discarded. Pair again before the next transfer.";
            SetSending(false);
        }
    }

    private async void SendTextClicked(object? sender, RoutedEventArgs e)
    {
        if (_remote is null)
        {
            SetStatus("Pair with a receiving device first.");
            return;
        }

        var text = _sendTextBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus("Enter text to send.");
            return;
        }

        var remote = _remote;
        _sendCts?.Dispose();
        _sendCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
        SetSending(true);
        try
        {
            await _transferClient.SendTextAsync(remote, text, _sendCts.Token);
            SetStatus("Text accepted by receiver");
        }
        catch (OperationCanceledException) when (_sendCts.IsCancellationRequested)
        {
            SetStatus("Text send cancelled");
        }
        catch (Exception ex)
        {
            SetStatus($"Text send failed: {ex.Message}");
        }
        finally
        {
            _remote = null;
            _remoteDeviceText.Text = "Authorization consumed or discarded. Pair again before the next transfer.";
            SetSending(false);
        }
    }

    private void CancelSendClicked(object? sender, RoutedEventArgs e)
        => _sendCts?.Cancel();

    private async void ChangeReceiveRootClicked(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose where SwiftDrop should receive files",
            AllowMultiple = false
        });
        var path = folders.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            Directory.CreateDirectory(path);
            _receiveRoot = Path.GetFullPath(path);
            await RestartReceiveServerAsync(_receiveRoot);
            _receiveRootBox.Text = _receiveRoot;
            RefreshLocalPairingLink();
            SetStatus("Receive folder updated");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not change receive folder: {ex.Message}");
        }
    }

    private void RefreshPairingLinkClicked(object? sender, RoutedEventArgs e)
        => RefreshLocalPairingLink();

    private void RefreshLocalPairingLink()
    {
        try
        {
            _localPairingLinkBox.Text = _identity.CreatePairingLink();
            SetStatus("Fresh pairing link generated");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not create pairing link: {ex.Message}");
        }
    }

    private async void CopyPairingLinkClicked(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null) return;
        await Avalonia.Input.Platform.ClipboardExtensions.SetTextAsync(
            clipboard,
            _localPairingLinkBox.Text ?? string.Empty);
        SetStatus("Pairing link copied");
    }

    private void GeneratePairingCodeClicked(object? sender, RoutedEventArgs e)
    {
        var code = _identity.CreatePairingCode();
        _pairingCodeText.Text = $"{code.Code}  •  expires {code.ExpiresUtc.ToLocalTime():HH:mm:ss}";
        SetStatus("One-time pairing code generated");
    }

    private async Task RestartReceiveServerAsync(string receiveRoot)
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
            _server = null;
        }

        _server = new DesktopReceiveServerService(
            _identity.Certificate,
            receiveRoot,
            _identity.TryConsumePairingNonce,
            ApproveTransferAsync,
            approveText: ApproveTextAsync,
            approvePairing: ApprovePairingAsync,
            createPairingLink: _identity.CreatePairingLink,
            consumePairingCode: _identity.TryConsumePairingCode,
            approveBatch: ApproveBatchAsync);
        _server.Start();
    }

    private Task<bool> ApproveTransferAsync(DesktopIncomingTransferPreview preview, CancellationToken ct)
        => RunOnUiAsync(
            () => ShowDecisionAsync(
                "Incoming file",
                $"{preview.SenderDeviceName} wants to send:\n\n{preview.Entry.RelativePath}\n{FormatBytes(preview.Entry.Length)}\nRisk: {preview.RiskLevel}\n\nCertificate fingerprint:\n{preview.SenderCertificateFingerprint}"),
            ct);

    private async Task<DesktopIncomingBatchDecision> ApproveBatchAsync(
        DesktopIncomingBatchPreview preview,
        CancellationToken ct)
    {
        var accepted = await RunOnUiAsync(
            () => ShowDecisionAsync(
                "Incoming files",
                $"{preview.SenderDeviceName} wants to send {preview.FileCount:N0} item(s), {FormatBytes(preview.TotalBytes)} total.\nHighest risk: {preview.HighestRisk}\n\nCertificate fingerprint:\n{preview.SenderCertificateFingerprint}"),
            ct);
        return accepted
            ? DesktopIncomingBatchDecision.AcceptAll(preview.Files)
            : DesktopIncomingBatchDecision.Reject;
    }

    private async Task<DesktopIncomingTextDecision> ApproveTextAsync(
        DesktopIncomingTextPreview preview,
        CancellationToken ct)
    {
        return await RunOnUiAsync(async () =>
        {
            var snippet = preview.Text.Length <= 1800 ? preview.Text : preview.Text[..1800] + "…";
            var accepted = await ShowDecisionAsync(
                "Incoming text",
                $"{preview.SenderDeviceName} wants to send this text:\n\n{snippet}\n\nAccept and copy it to the clipboard?");
            if (!accepted) return DesktopIncomingTextDecision.Reject;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null) return DesktopIncomingTextDecision.Accept;
            await Avalonia.Input.Platform.ClipboardExtensions.SetTextAsync(clipboard, preview.Text);
            return DesktopIncomingTextDecision.AcceptAndCopy;
        }, ct);
    }

    private Task<bool> ApprovePairingAsync(DesktopIncomingPairingRequest preview, CancellationToken ct)
        => RunOnUiAsync(
            () => ShowDecisionAsync(
                "Nearby pairing request",
                $"Allow {preview.SenderDeviceName} to receive a fresh one-time SwiftDrop pairing authorization?\n\nCertificate fingerprint:\n{preview.SenderCertificateFingerprint}"),
            ct);

    private async Task<T> RunOnUiAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (Dispatcher.UIThread.CheckAccess()) return await action();

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = ct.Register(() => completion.TrySetCanceled(ct));
        Dispatcher.UIThread.Post(async () =>
        {
            try { completion.TrySetResult(await action()); }
            catch (Exception ex) { completion.TrySetException(ex); }
        });
        return await completion.Task;
    }

    private async Task<bool> ShowDecisionAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 560,
            MinHeight = 300,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 510
        };
        var accept = new Button { Content = "Accept", MinWidth = 96 };
        var reject = new Button { Content = "Reject", MinWidth = 96 };
        accept.Click += (_, _) => dialog.Close(true);
        reject.Click += (_, _) => dialog.Close(false);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(reject);
        buttons.Children.Add(accept);

        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 18 };
        panel.Children.Add(text);
        panel.Children.Add(buttons);
        dialog.Content = panel;
        return await dialog.ShowDialog<bool>(this);
    }

    private void ShowRemote(PairingPayload remote)
        => _remoteDeviceText.Text = $"Ready: {remote.DeviceName} • {remote.Host}:{remote.Port}";

    private void SetSending(bool sending)
    {
        _sendFilesButton.IsEnabled = !sending;
        _sendFolderButton.IsEnabled = !sending;
        _sendTextButton.IsEnabled = !sending;
        _cancelSendButton.IsEnabled = sending;
    }

    private void SetStatus(string message)
        => _statusText.Text = message;

    private static string FormatBytes(long value)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        var scaled = (double)Math.Max(0, value);
        var unit = 0;
        while (scaled >= 1024d && unit < units.Length - 1)
        {
            scaled /= 1024d;
            unit++;
        }
        return $"{scaled:0.##} {units[unit]}";
    }
}

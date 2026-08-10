using SwiftDrop.App.Services;
using SwiftDrop.App.ViewModels;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.App;

public partial class DevicesPage : ContentPage
{
    private readonly DevicesViewModel _viewModel;
    private readonly NearbyPairingService _pairing;
    private readonly PairingSelectionService _selection;

    public DevicesPage(
        DevicesViewModel viewModel,
        NearbyPairingService pairing,
        PairingSelectionService selection)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _pairing = pairing;
        _selection = selection;
        BindingContext = viewModel;
        ManualPortEntry.Text = ProtocolConstants.DefaultPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
        => await StartOrRefreshAsync();

    private void OnUnloaded(object? sender, EventArgs e)
        => _viewModel.Dispose();

    private async Task StartOrRefreshAsync()
    {
        try
        {
            await _viewModel.StartOrRefreshAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("DiscoveryUnavailable"), ex.Message, AppText.Get("Ok"));
            _viewModel.RefreshSnapshot();
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e)
        => await StartOrRefreshAsync();

    private async void PairClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string id) return;
        var peer = _viewModel.FindPeer(id);
        if (peer is null)
        {
            await DisplayAlertAsync(
                AppText.Get("DeviceUnavailable"),
                AppText.Get("DeviceUnavailableMessage"),
                AppText.Get("Ok"));
            return;
        }
        if (string.IsNullOrWhiteSpace(peer.CertificateFingerprint))
        {
            await DisplayAlertAsync(
                AppText.Get("PairingUnavailable"),
                AppText.Get("PairingFingerprintMissingMessage"),
                AppText.Get("Ok"));
            return;
        }

        var codeChoice = AppText.Get("UseEightDigitCode");
        var mode = await DisplayActionSheetAsync(
            AppText.Format("PairWithFormat", peer.Name),
            AppText.Get("Cancel"),
            null,
            AppText.Get("RequestPairing"),
            codeChoice);
        if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, AppText.Get("Cancel"), StringComparison.Ordinal)) return;

        string? code = null;
        if (string.Equals(mode, codeChoice, StringComparison.Ordinal))
        {
            code = (await DisplayPromptAsync(
                AppText.Get("OneTimePairingCode"),
                AppText.Get("OneTimePairingCodePrompt"),
                AppText.Get("Continue"),
                AppText.Get("Cancel"),
                keyboard: Keyboard.Numeric,
                maxLength: 8))?.Trim();
            if (string.IsNullOrWhiteSpace(code)) return;
            if (code.Length != 8 || code.Any(ch => ch is < '0' or > '9'))
            {
                await DisplayAlertAsync(
                    AppText.Get("InvalidCode"),
                    AppText.Get("InvalidCodeMessage"),
                    AppText.Get("Ok"));
                return;
            }
        }
        else
        {
            var details = AppText.Format(
                    "RequestPairingDetailsFormat",
                    peer.Name,
                    peer.Host,
                    peer.Port,
                    Fingerprint.Pretty(peer.CertificateFingerprint))
                .Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
            var requested = await DisplayAlertAsync(
                AppText.Get("RequestPairingQuestion"),
                details,
                AppText.Get("Request"),
                AppText.Get("Cancel"));
            if (!requested) return;
        }

        try
        {
            button.IsEnabled = false;
            button.Text = AppText.Get("WaitingForApproval");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var payload = await _pairing.RequestAsync(peer, code, cts.Token);
            _selection.Set(payload);
            await Navigation.PopAsync();
        }
        catch (OperationCanceledException)
        {
            await DisplayAlertAsync(
                AppText.Get("PairingTimedOut"),
                AppText.Get("PairingTimedOutMessage"),
                AppText.Get("Ok"));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("PairingFailed"), ex.Message, AppText.Get("Ok"));
        }
        finally
        {
            button.Text = AppText.Get("RequestPairing");
            button.IsEnabled = true;
        }
    }

    private async void ManualPairClicked(object? sender, EventArgs e)
    {
        var host = ManualIpEntry.Text?.Trim() ?? string.Empty;
        var code = ManualCodeEntry.Text?.Trim() ?? string.Empty;
        if (!int.TryParse(ManualPortEntry.Text, out var port))
        {
            await DisplayAlertAsync(
                AppText.Get("InvalidPort"),
                AppText.Get("InvalidPortMessage"),
                AppText.Get("Ok"));
            return;
        }
        if (code.Length != 8 || code.Any(ch => ch is < '0' or > '9'))
        {
            await DisplayAlertAsync(
                AppText.Get("InvalidCode"),
                AppText.Get("FreshCodeMessage"),
                AppText.Get("Ok"));
            return;
        }

        var confirmed = await DisplayAlertAsync(
            AppText.Get("ManualPairingBootstrap"),
            AppText.Get("ManualPairingBootstrapMessage"),
            AppText.Get("Continue"),
            AppText.Get("Cancel"));
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
            await DisplayAlertAsync(
                AppText.Get("PairingTimedOut"),
                AppText.Get("ManualPairingTimedOutMessage"),
                AppText.Get("Ok"));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(AppText.Get("ManualPairingFailed"), ex.Message, AppText.Get("Ok"));
        }
        finally
        {
            if (sender is Button button) button.IsEnabled = true;
        }
    }
}

using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.App;

public partial class DiagnosticsPage : ContentPage
{
    private readonly NetworkDiagnosticsService _diagnostics;
    private readonly NearbyDiscoveryService _discovery;
    private readonly DiagnosticLogService _log;

    public DiagnosticsPage(
        NetworkDiagnosticsService diagnostics,
        NearbyDiscoveryService discovery,
        DiagnosticLogService log)
    {
        InitializeComponent();
        _diagnostics = diagnostics;
        _discovery = discovery;
        _log = log;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        ProtocolLabel.Text = $"Protocol version: {ProtocolConstants.CurrentVersion}";
        try
        {
            await _discovery.StartAsync();
            MdnsStatusLabel.Text = $"mDNS/Bonjour discovery: {(_discovery.IsMdnsRunning ? "running" : "unavailable")}";
            UdpStatusLabel.Text = $"UDP broadcast fallback: {(_discovery.IsUdpRunning ? "running" : "unavailable")}";
            var current = _diagnostics.InspectLocalNetwork();
            DiagnosticsList.ItemsSource = current;
            foreach (var item in current)
            {
                await _log.RecordAsync(
                    item.Severity.ToString(),
                    item.Code,
                    item.Title);
            }
            await _log.RecordAsync(
                "Info",
                "discovery.status",
                $"mDNS={_discovery.IsMdnsRunning}; UDP={_discovery.IsUdpRunning}; protocol={ProtocolConstants.CurrentVersion}");
        }
        catch (Exception ex)
        {
            MdnsStatusLabel.Text = "mDNS/Bonjour discovery: unavailable";
            UdpStatusLabel.Text = "UDP broadcast fallback: unavailable";
            DiagnosticsList.ItemsSource = new[]
            {
                new NetworkDiagnostic(
                    "diagnostics.discovery_error",
                    "Automatic discovery could not start",
                    "Automatic discovery is unavailable. QR or pasted pairing invitations can still be used when the receiver address is reachable.",
                    DiagnosticSeverity.Warning)
            };
            await _log.RecordAsync(
                "Warning",
                "diagnostics.discovery_error",
                $"Discovery startup failed with {ex.GetType().Name}.");
        }

        EventLogList.ItemsSource = await _log.GetRecentAsync(200);
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshAsync();

    private async void ExportClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await _log.ExportSafeTextAsync();
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "SwiftDrop safe diagnostics",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Export failed", $"Diagnostics export failed with {ex.GetType().Name}.", "OK");
        }
    }

    private async void ClearLogClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert(
            "Clear diagnostic log?",
            "This removes locally stored diagnostic events. Transfer history and received files are not changed.",
            "Clear",
            "Cancel");
        if (!confirmed) return;
        await _log.ClearAsync();
        EventLogList.ItemsSource = Array.Empty<object>();
    }
}

using SwiftDrop.App.Services;
using SwiftDrop.Core.Diagnostics;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.App;

public partial class DiagnosticsPage : ContentPage
{
    private readonly NetworkDiagnosticsService _diagnostics;
    private readonly NearbyDiscoveryService _discovery;

    public DiagnosticsPage(NetworkDiagnosticsService diagnostics, NearbyDiscoveryService discovery)
    {
        InitializeComponent();
        _diagnostics = diagnostics;
        _discovery = discovery;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _discovery.StartAsync();
            ProtocolLabel.Text = $"Protocol version: {ProtocolConstants.CurrentVersion}";
            UdpStatusLabel.Text = $"UDP discovery: {(_discovery.IsRunning ? "running" : "stopped")}";
            DiagnosticsList.ItemsSource = _diagnostics.InspectLocalNetwork();
        }
        catch (Exception ex)
        {
            ProtocolLabel.Text = $"Protocol version: {ProtocolConstants.CurrentVersion}";
            UdpStatusLabel.Text = "UDP discovery: unavailable";
            DiagnosticsList.ItemsSource = new[]
            {
                new NetworkDiagnostic(
                    "diagnostics.discovery_error",
                    "Discovery could not start",
                    ex.Message,
                    DiagnosticSeverity.Warning)
            };
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshAsync();
}

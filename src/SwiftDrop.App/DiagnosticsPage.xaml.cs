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
        ProtocolLabel.Text = $"Protocol version: {ProtocolConstants.CurrentVersion}";
        try
        {
            await _discovery.StartAsync();
            MdnsStatusLabel.Text = $"mDNS/Bonjour discovery: {(_discovery.IsMdnsRunning ? "running" : "unavailable")}";
            UdpStatusLabel.Text = $"UDP broadcast fallback: {(_discovery.IsUdpRunning ? "running" : "unavailable")}";
            DiagnosticsList.ItemsSource = _diagnostics.InspectLocalNetwork();
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
                    $"{ex.Message} QR or pasted pairing invitations can still be used when the receiver address is reachable.",
                    DiagnosticSeverity.Warning)
            };
        }
    }

    private async void RefreshClicked(object? sender, EventArgs e) => await RefreshAsync();
}

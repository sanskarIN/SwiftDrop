using SwiftDrop.Core.Diagnostics;

namespace SwiftDrop.Core.Tests;

public sealed class NetworkDiagnosticsServiceTests
{
    [Fact]
    public void InspectLocalNetwork_ReturnsInternallyConsistentDiagnostics()
    {
        var diagnostics = new NetworkDiagnosticsService().InspectLocalNetwork();

        Assert.NotEmpty(diagnostics);
        Assert.Equal(diagnostics.Count, diagnostics.Select(item => item.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(diagnostics, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Code));
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
            Assert.False(string.IsNullOrWhiteSpace(item.Message));
        });
    }

    [Fact]
    public void InspectLocalNetwork_ReturnsExactlyOnePrimaryNetworkState()
    {
        var diagnostics = new NetworkDiagnosticsService().InspectLocalNetwork();
        var primaryCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            "network.none",
            "network.ipv4_missing",
            "network.ready"
        };

        Assert.Single(diagnostics.Where(item => primaryCodes.Contains(item.Code)));
    }

    [Fact]
    public void InspectLocalNetwork_UsesExpectedSeverityForPrimaryState()
    {
        var primary = new NetworkDiagnosticsService()
            .InspectLocalNetwork()
            .Single(item => item.Code is "network.none" or "network.ipv4_missing" or "network.ready");

        var expected = primary.Code switch
        {
            "network.none" => DiagnosticSeverity.Error,
            "network.ipv4_missing" => DiagnosticSeverity.Warning,
            "network.ready" => DiagnosticSeverity.Info,
            _ => throw new InvalidOperationException()
        };

        Assert.Equal(expected, primary.Severity);
    }

    [Fact]
    public void InspectLocalNetwork_WifiDiagnosticIsInformationalWhenPresent()
    {
        var wifi = new NetworkDiagnosticsService()
            .InspectLocalNetwork()
            .SingleOrDefault(item => item.Code == "network.wifi");

        if (wifi is not null)
            Assert.Equal(DiagnosticSeverity.Info, wifi.Severity);
    }
}

using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SwiftDrop.Core.Diagnostics;

public sealed class NetworkDiagnosticsService
{
    public IReadOnlyList<NetworkDiagnostic> InspectLocalNetwork()
    {
        var diagnostics = new List<NetworkDiagnostic>();
        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToArray();

        if (interfaces.Length == 0)
        {
            diagnostics.Add(new NetworkDiagnostic(
                "network.none",
                "No active local network",
                "Connect this device to Wi-Fi or a local Ethernet network before discovering nearby devices.",
                DiagnosticSeverity.Error));
            return diagnostics;
        }

        var hasIpv4 = interfaces
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

        if (!hasIpv4)
        {
            diagnostics.Add(new NetworkDiagnostic(
                "network.ipv4_missing",
                "No IPv4 address detected",
                "SwiftDrop's UDP fallback currently uses IPv4. QR/manual pairing may still work when a reachable address is available.",
                DiagnosticSeverity.Warning));
        }
        else
        {
            diagnostics.Add(new NetworkDiagnostic(
                "network.ready",
                "Local network is available",
                "At least one active non-loopback interface with IPv4 connectivity was detected.",
                DiagnosticSeverity.Info));
        }

        if (interfaces.Any(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
        {
            diagnostics.Add(new NetworkDiagnostic(
                "network.wifi",
                "Wi-Fi interface detected",
                "Guest networks or access-point isolation can still prevent nearby-device connections even when Wi-Fi is connected.",
                DiagnosticSeverity.Info));
        }

        return diagnostics;
    }
}

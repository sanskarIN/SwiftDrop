using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SwiftDrop.Core.Networking;

public static class LocalAddressSelector
{
    public static IPAddress SelectBest(IEnumerable<NetworkInterface> interfaces)
    {
        ArgumentNullException.ThrowIfNull(interfaces);
        var candidates = interfaces
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses.Select(u => new Candidate(n.NetworkInterfaceType, u.Address)))
            .Where(x => LocalAddressPolicy.IsLocal(x.Address) && !IPAddress.IsLoopback(x.Address))
            .OrderByDescending(Score)
            .ToArray();

        return candidates.Length == 0 ? IPAddress.Loopback : candidates[0].Address;
    }

    private static int Score(Candidate candidate)
    {
        var score = candidate.NetworkType switch
        {
            NetworkInterfaceType.Ethernet => 50,
            NetworkInterfaceType.Wireless80211 => 45,
            _ => 20
        };
        if (candidate.Address.AddressFamily == AddressFamily.InterNetwork) score += 20;
        if (candidate.Address.AddressFamily == AddressFamily.InterNetworkV6 && candidate.Address.IsIPv6LinkLocal) score -= 5;
        return score;
    }

    private sealed record Candidate(NetworkInterfaceType NetworkType, IPAddress Address);
}

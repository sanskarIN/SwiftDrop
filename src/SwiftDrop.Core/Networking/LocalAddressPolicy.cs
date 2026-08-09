using System.Net;
using System.Net.Sockets;

namespace SwiftDrop.Core.Networking;

public static class LocalAddressPolicy
{
    public static IPAddress ParseAndValidate(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var candidate = host.Trim().Trim('[', ']');
        if (!IPAddress.TryParse(candidate, out var address))
            throw new InvalidDataException("SwiftDrop pairing requires a numeric local-network IP address.");
        if (!IsLocal(address))
            throw new InvalidDataException("SwiftDrop pairing address is not local/private/link-local.");
        return address;
    }

    public static bool IsLocal(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (IPAddress.IsLoopback(address)) return true;
        if (address.IsIPv4MappedToIPv6) return IsLocal(address.MapToIPv4());

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   bytes[0] == 169 && bytes[1] == 254 ||
                   bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
                   bytes[0] == 192 && bytes[1] == 168;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal) return true;
            // RFC 4193 unique-local addresses: fc00::/7.
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }
}

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Discovery;

public sealed class MdnsDiscoveryService : IAsyncDisposable
{
    public const int MdnsPort = 5353;
    private static readonly IPAddress MulticastAddress = IPAddress.Parse("224.0.0.251");

    private readonly UdpClient _udp;
    private readonly CancellationTokenSource _cts = new();

    public MdnsDiscoveryService()
    {
        _udp = new UdpClient(AddressFamily.InterNetwork);
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.ExclusiveAddressUse = false;
        _udp.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
        _udp.JoinMulticastGroup(MulticastAddress);
    }

    public async Task QueryAsync(CancellationToken ct = default)
    {
        var packet = MdnsCodec.CreateQuery();
        await _udp.SendAsync(packet, new IPEndPoint(MulticastAddress, MdnsPort), ct);
    }

    public async Task AnnounceAsync(PeerDevice self, CancellationToken ct = default)
    {
        var address = GetLanIpv4Address();
        if (address is null) return;
        var packet = MdnsCodec.CreateAnnouncement(self, address);
        await _udp.SendAsync(packet, new IPEndPoint(MulticastAddress, MdnsPort), ct);
    }

    public async IAsyncEnumerable<PeerDevice> ListenAsync(
        PeerDevice self,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        while (!linked.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _udp.ReceiveAsync(linked.Token);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (SocketException) when (linked.IsCancellationRequested)
            {
                yield break;
            }

            if (result.Buffer.Length is 0 or > 64 * 1024) continue;
            if (MdnsCodec.IsDiscoveryQuery(result.Buffer))
            {
                try { await AnnounceAsync(self, linked.Token); } catch (SocketException) { }
                continue;
            }

            var peer = MdnsCodec.TryParseAnnouncement(result.Buffer, result.RemoteEndPoint.Address, DateTimeOffset.UtcNow);
            if (peer is not null && !string.Equals(peer.Id, self.Id, StringComparison.Ordinal))
                yield return peer;
        }
    }

    public static IPAddress? GetLanIpv4Address()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(unicast.Address))
                    return unicast.Address;
            }
        }
        return null;
    }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try { _udp.DropMulticastGroup(MulticastAddress); } catch (SocketException) { }
        _udp.Dispose();
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}

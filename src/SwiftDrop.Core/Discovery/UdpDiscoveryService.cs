using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Discovery;

public sealed class UdpDiscoveryService : IAsyncDisposable
{
    private readonly UdpClient _udp;
    private readonly int _port;
    private readonly CancellationTokenSource _cts = new();

    public UdpDiscoveryService(int port = ProtocolConstants.DefaultPort + 1)
    {
        _port = port;
        _udp = new UdpClient(AddressFamily.InterNetwork) { EnableBroadcast = true };
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
    }

    public async Task AnnounceAsync(PeerDevice self, CancellationToken ct = default)
    {
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(self));
        if (payload.Length > 4096) throw new InvalidDataException("Discovery payload too large.");
        await _udp.SendAsync(payload, new IPEndPoint(IPAddress.Broadcast, _port), ct);
    }

    public async IAsyncEnumerable<PeerDevice> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        while (!linked.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try { result = await _udp.ReceiveAsync(linked.Token); }
            catch (OperationCanceledException) { yield break; }
            if (result.Buffer.Length is 0 or > 4096) continue;
            PeerDevice? peer = null;
            try { peer = JsonSerializer.Deserialize<PeerDevice>(result.Buffer); } catch (JsonException) { }
            if (peer is not null && peer.Port is > 0 and <= 65535)
                yield return peer with { Host = result.RemoteEndPoint.Address.ToString(), LastSeenUtc = DateTimeOffset.UtcNow };
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _udp.Dispose();
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }
}

using System.Buffers.Binary;
using System.Text;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class FrameProtocolUnknownMemberTests
{
    [Fact]
    public async Task ReadJsonAsync_RejectsUnknownTopLevelMember()
    {
        var stream = Frame("{\"accepted\":true,\"resumeOffset\":0,\"message\":null,\"unexpected\":1}");
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FrameProtocol.ReadJsonAsync<TransferAcknowledgement>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_RejectsUnknownNestedMember()
    {
        var json = """
            {
              "type":"file",
              "protocolVersion":"1",
              "pairingNonce":"ABCDEFGHIJKLMNOPQRSTUVWX",
              "senderDeviceId":"sender",
              "senderDeviceName":"Laptop",
              "entry":{
                "relativePath":"a.txt",
                "length":1,
                "sha256":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                "lastWriteUtc":"2026-08-10T00:00:00Z",
                "unexpected":true
              },
              "text":null,
              "expiresUnixSeconds":null,
              "pairingCode":null,
              "transferId":null,
              "files":null,
              "totalBytes":null
            }
            """;
        var stream = Frame(json);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FrameProtocol.ReadJsonAsync<ProtocolRequest>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_AcceptsExactKnownNestedMembers()
    {
        var json = """
            {
              "type":"file",
              "protocolVersion":"1",
              "pairingNonce":"ABCDEFGHIJKLMNOPQRSTUVWX",
              "senderDeviceId":"sender",
              "senderDeviceName":"Laptop",
              "entry":{
                "relativePath":"a.txt",
                "length":1,
                "sha256":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                "lastWriteUtc":"2026-08-10T00:00:00Z"
              },
              "text":null,
              "expiresUnixSeconds":null,
              "pairingCode":null,
              "transferId":null,
              "files":null,
              "totalBytes":null
            }
            """;
        var stream = Frame(json);
        var request = await FrameProtocol.ReadJsonAsync<ProtocolRequest>(stream, CancellationToken.None);
        Assert.Equal("a.txt", request.Entry!.RelativePath);
        Assert.Equal(1, request.Entry.Length);
    }

    private static MemoryStream Frame(string json)
    {
        var payload = Encoding.UTF8.GetBytes(json);
        var bytes = new byte[payload.Length + 4];
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(0, 4), payload.Length);
        payload.CopyTo(bytes.AsSpan(4));
        return new MemoryStream(bytes, writable: false);
    }
}

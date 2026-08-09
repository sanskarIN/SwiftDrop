using System.Buffers.Binary;
using System.Text;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class FrameProtocolBoundaryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task ReadJsonAsync_Rejects_NonPositive_Lengths(int length)
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, length);
        await using var stream = new MemoryStream(header);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_Rejects_Length_Over_Header_Limit_Before_Allocation()
    {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, ProtocolConstants.HeaderLimitBytes + 1);
        await using var stream = new MemoryStream(header);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_Rejects_Truncated_Header()
    {
        await using var stream = new MemoryStream([0, 0, 0]);
        await Assert.ThrowsAsync<EndOfStreamException>(() =>
            FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_Rejects_Truncated_Payload()
    {
        var bytes = new byte[6];
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(0, 4), 10);
        bytes[4] = (byte)'{';
        bytes[5] = (byte)'}';
        await using var stream = new MemoryStream(bytes);
        await Assert.ThrowsAsync<EndOfStreamException>(() =>
            FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
    }

    [Theory]
    [InlineData("{\"type\":\"file\",\"type\":\"text\"}")]
    [InlineData("{\"type\":\"file\",\"Type\":\"text\"}")]
    [InlineData("{\"outer\":{\"nonce\":\"one\",\"Nonce\":\"two\"}}")]
    public async Task ReadJsonAsync_Rejects_Duplicate_Properties_Case_Insensitively(string json)
    {
        await using var stream = new MemoryStream(Frame(Encoding.UTF8.GetBytes(json)));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_Rejects_Invalid_Utf8()
    {
        byte[] payload = [(byte)'{', (byte)'\"', (byte)'x', (byte)'\"', (byte)':', (byte)'\"', 0xC3, 0x28, (byte)'\"', (byte)'}'];
        await using var stream = new MemoryStream(Frame(payload));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_Rejects_Every_Truncated_Prefix_Of_Valid_Frame()
    {
        byte[] complete;
        await using (var encoded = new MemoryStream())
        {
            await FrameProtocol.WriteJsonAsync(encoded, new { type = "probe", value = 42 }, CancellationToken.None);
            complete = encoded.ToArray();
        }

        for (var length = 0; length < complete.Length; length++)
        {
            await using var stream = new MemoryStream(complete.AsSpan(0, length).ToArray());
            await Assert.ThrowsAnyAsync<EndOfStreamException>(() =>
                FrameProtocol.ReadJsonAsync<object>(stream, CancellationToken.None));
        }
    }

    private static byte[] Frame(byte[] payload)
    {
        var bytes = new byte[payload.Length + 4];
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(0, 4), payload.Length);
        payload.CopyTo(bytes, 4);
        return bytes;
    }
}

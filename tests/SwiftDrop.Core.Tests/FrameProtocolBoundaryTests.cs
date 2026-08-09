using System.Buffers.Binary;
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
}

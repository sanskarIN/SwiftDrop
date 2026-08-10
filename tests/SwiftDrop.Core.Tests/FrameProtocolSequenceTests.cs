using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class FrameProtocolSequenceTests
{
    [Fact]
    public async Task ReadJsonAsync_ConsumesExactlyOneFrameAtATime()
    {
        await using var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, new TestFrame("one", 1), CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TestFrame("two", 2), CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(stream, new TestFrame("three", 3), CancellationToken.None);
        stream.Position = 0;

        var first = await FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None);
        var second = await FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None);
        var third = await FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None);

        Assert.Equal(new TestFrame("one", 1), first);
        Assert.Equal(new TestFrame("two", 2), second);
        Assert.Equal(new TestFrame("three", 3), third);
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public async Task ReadJsonAsync_RejectsConnectionCloseBetweenFramesWithoutCorruptingEarlierFrame()
    {
        await using var complete = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(complete, new TestFrame("complete", 1), CancellationToken.None);
        await FrameProtocol.WriteJsonAsync(complete, new TestFrame("truncated", 2), CancellationToken.None);
        var bytes = complete.ToArray();
        var truncated = bytes[..^1];

        await using var stream = new MemoryStream(truncated, writable: false);
        var first = await FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None);

        Assert.Equal(new TestFrame("complete", 1), first);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadJsonAsync_RejectsMissingNextFrameAfterSuccessfulSequenceItem()
    {
        await using var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, new TestFrame("only", 1), CancellationToken.None);
        stream.Position = 0;

        var first = await FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None);
        Assert.Equal("only", first.Name);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            FrameProtocol.ReadJsonAsync<TestFrame>(stream, CancellationToken.None));
    }

    private sealed record TestFrame(string Name, int Sequence);
}

using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class FrameProtocolTests
{
    [Fact]
    public async Task JsonFrame_RoundTrips()
    {
        await using var stream = new MemoryStream();
        await FrameProtocol.WriteJsonAsync(stream, new Sample("hello", 42), CancellationToken.None);
        stream.Position = 0;
        var result = await FrameProtocol.ReadJsonAsync<Sample>(stream, CancellationToken.None);
        Assert.Equal(new Sample("hello", 42), result);
    }

    private sealed record Sample(string Name, int Value);
}

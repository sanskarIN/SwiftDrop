using System.Text;
using System.Text.Json;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Tests;

public sealed class StrictJsonGuardTests
{
    [Theory]
    [InlineData("{\"a\":1,\"a\":2}")]
    [InlineData("{\"a\":1,\"A\":2}")]
    [InlineData("{\"outer\":{\"value\":1,\"VALUE\":2}}")]
    [InlineData("[{\"id\":1,\"ID\":2}]")]
    public void Validate_RejectsDuplicateProperties(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        Assert.Throws<InvalidDataException>(() => StrictJsonGuard.Validate(bytes, 16));
    }

    [Fact]
    public void Validate_AcceptsDistinctNestedProperties()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"type\":\"file\",\"item\":{\"name\":\"a.txt\",\"size\":1}}");
        StrictJsonGuard.Validate(bytes, 16);
    }

    [Fact]
    public void Validate_RejectsInvalidUtf8OrJson()
    {
        byte[] bytes = [(byte)'{', (byte)'\"', (byte)'x', (byte)'\"', (byte)':', 0xC3, 0x28, (byte)'}'];
        Assert.Throws<JsonException>(() => StrictJsonGuard.Validate(bytes, 16));
    }

    [Fact]
    public void Validate_RejectsDepthBeyondLimit()
    {
        var json = new string('[', 20) + "0" + new string(']', 20);
        var bytes = Encoding.UTF8.GetBytes(json);
        Assert.Throws<JsonException>(() => StrictJsonGuard.Validate(bytes, 8));
    }
}

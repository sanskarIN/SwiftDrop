using SwiftDrop.Core.Diagnostics;

namespace SwiftDrop.Core.Tests;

public sealed class TransferSelfTestServiceTests
{
    [Fact]
    public async Task KnownGoodRoundTrip_Passes()
    {
        var result = await new TransferSelfTestService().RunSuccessfulRoundTripAsync();
        Assert.True(result.Passed, result.Message);
    }

    [Fact]
    public async Task ChecksumMismatch_IsRejectedAndCleaned()
    {
        var result = await new TransferSelfTestService().RunChecksumMismatchAsync();
        Assert.True(result.Passed, result.Message);
    }

    [Fact]
    public async Task InterruptedReceive_RemainsResumablePartial()
    {
        var result = await new TransferSelfTestService().RunInterruptedReceiveAsync();
        Assert.True(result.Passed, result.Message);
    }
}

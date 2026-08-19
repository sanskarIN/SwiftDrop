using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class StorageCapacityGuardTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureCapacity_RejectsBlankDestinationPath(string path)
        => Assert.Throws<ArgumentException>(() => StorageCapacityGuard.EnsureCapacity(path, 0));

    [Fact]
    public void EnsureCapacity_RejectsNegativeIncomingBytes()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            StorageCapacityGuard.EnsureCapacity(Path.GetTempPath(), -1));

    [Fact]
    public void EnsureCapacity_RejectsRequiredCapacityOverflow()
        => Assert.Throws<OverflowException>(() =>
            StorageCapacityGuard.EnsureCapacity(Path.GetTempPath(), long.MaxValue));
}

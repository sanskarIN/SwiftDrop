using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PathGuardTests
{
    [Fact]
    public void ResolveUnderRoot_AllowsNormalRelativePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "swiftdrop-test-root");
        var result = PathGuard.ResolveUnderRoot(root, Path.Combine("folder", "file.txt"));
        Assert.StartsWith(Path.GetFullPath(root), result, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("../../escape.bin")]
    public void ResolveUnderRoot_RejectsTraversal(string relative)
    {
        var root = Path.Combine(Path.GetTempPath(), "swiftdrop-test-root");
        Assert.Throws<InvalidDataException>(() => PathGuard.ResolveUnderRoot(root, relative));
    }
}

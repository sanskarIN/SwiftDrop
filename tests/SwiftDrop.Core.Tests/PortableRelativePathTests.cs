using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PortableRelativePathTests
{
    [Theory]
    [InlineData("folder/file.txt")]
    [InlineData("folder\\file.txt")]
    [InlineData("one/two/three.bin")]
    public void GetSegments_AcceptsCanonicalRelativePaths(string path)
    {
        var segments = PortableRelativePath.GetSegments(path);
        Assert.True(segments.Length >= 2);
        Assert.All(segments, segment => Assert.False(string.IsNullOrEmpty(segment)));
    }

    [Theory]
    [InlineData("/rooted.txt")]
    [InlineData("\\rooted.txt")]
    [InlineData("C:\\rooted.txt")]
    [InlineData("c:/rooted.txt")]
    [InlineData("\\\\server\\share\\file.txt")]
    public void GetSegments_RejectsPortableRootedPaths(string path)
        => Assert.Throws<InvalidDataException>(() => PortableRelativePath.GetSegments(path));

    [Theory]
    [InlineData("folder//file.txt")]
    [InlineData("folder\\\\file.txt")]
    [InlineData("folder/\\file.txt")]
    [InlineData("folder\\/file.txt")]
    [InlineData("folder/file.txt/")]
    [InlineData("folder\\file.txt\\")]
    [InlineData("./file.txt")]
    [InlineData("folder/./file.txt")]
    [InlineData("folder/../file.txt")]
    public void GetSegments_RejectsEmptyOrTraversalSegments(string path)
        => Assert.Throws<InvalidDataException>(() => PortableRelativePath.GetSegments(path));

    [Fact]
    public void NormalizeSeparators_IsHostIndependent()
        => Assert.Equal("folder/sub/file.txt", PortableRelativePath.NormalizeSeparators("folder\\sub/file.txt"));
}

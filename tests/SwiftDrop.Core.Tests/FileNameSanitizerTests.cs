using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class FileNameSanitizerTests
{
    [Fact]
    public void SanitizeSegment_Removes_Control_And_Portable_Invalid_Characters()
    {
        var value = FileNameSanitizer.SanitizeSegment("report:<bad>?\u0001.txt");
        Assert.Equal("reportbad.txt", value);
    }

    [Fact]
    public void SanitizeRelativePath_Preserves_Safe_Subfolders()
    {
        var value = FileNameSanitizer.SanitizeRelativePath("folder/sub/file.txt");
        Assert.Equal(Path.Combine("folder", "sub", "file.txt"), value);
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("folder/../../secret.txt")]
    public void SanitizeRelativePath_Rejects_Traversal(string path)
    {
        Assert.Throws<InvalidDataException>(() => FileNameSanitizer.SanitizeRelativePath(path));
    }

    [Fact]
    public void SanitizeSegment_Bounds_Long_Names()
    {
        var value = FileNameSanitizer.SanitizeSegment(new string('a', 300) + ".txt");
        Assert.True(value.Length <= 180);
        Assert.EndsWith(".txt", value, StringComparison.Ordinal);
    }
}

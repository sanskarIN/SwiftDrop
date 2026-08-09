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
        Assert.True(value.EndsWith(".txt", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("CON", "_CON")]
    [InlineData("con.txt", "_con.txt")]
    [InlineData("NUL.bin", "_NUL.bin")]
    [InlineData("COM1.log", "_COM1.log")]
    [InlineData("lpt9", "_lpt9")]
    public void SanitizeSegment_Prefixes_Reserved_Windows_Device_Names(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.SanitizeSegment(input));
    }

    [Fact]
    public void SanitizeSegment_Normalizes_Unicode_To_FormC()
    {
        const string decomposed = "Cafe\u0301.txt";
        var value = FileNameSanitizer.SanitizeSegment(decomposed);
        Assert.Equal("Café.txt", value);
    }

    [Fact]
    public void SanitizeSegment_Replaces_Name_With_Only_Invalid_Characters()
    {
        Assert.Equal("unnamed", FileNameSanitizer.SanitizeSegment("<>:?*"));
    }
}

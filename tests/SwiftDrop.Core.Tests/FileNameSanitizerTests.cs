using System.Text;
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

    [Theory]
    [InlineData("folder/file.txt", "folderfile.txt")]
    [InlineData("folder\\file.txt", "folderfile.txt")]
    public void SanitizeSegment_Removes_Both_Portable_Separator_Characters(string input, string expected)
    {
        Assert.Equal(expected, FileNameSanitizer.SanitizeSegment(input));
    }

    [Theory]
    [InlineData("folder/sub/file.txt")]
    [InlineData("folder\\sub\\file.txt")]
    [InlineData("folder\\sub/file.txt")]
    public void SanitizeRelativePath_UsesForwardSlashCanonicalWireForm(string input)
    {
        var value = FileNameSanitizer.SanitizeRelativePath(input);
        Assert.Equal("folder/sub/file.txt", value);
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("folder/../../secret.txt")]
    [InlineData("folder/./secret.txt")]
    [InlineData("folder//secret.txt")]
    [InlineData("folder\\\\secret.txt")]
    [InlineData("folder/secret.txt/")]
    [InlineData("C:\\secret.txt")]
    [InlineData("\\\\server\\share\\secret.txt")]
    public void SanitizeRelativePath_Rejects_NonCanonicalOrRootedPaths(string path)
    {
        Assert.Throws<InvalidDataException>(() => FileNameSanitizer.SanitizeRelativePath(path));
    }

    [Fact]
    public void SanitizeSegment_Bounds_Long_Names()
    {
        var value = FileNameSanitizer.SanitizeSegment(new string('a', 300) + ".txt");
        Assert.True(value.Length <= FileNameSanitizer.MaximumSegmentLength);
        Assert.EndsWith(".txt", value, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitizeSegment_Bounds_Extremely_Long_Extension()
    {
        var value = FileNameSanitizer.SanitizeSegment("a." + new string('x', 400));
        Assert.Equal(FileNameSanitizer.MaximumSegmentLength, value.Length);
        Assert.True(Encoding.UTF8.GetByteCount(value) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
    }

    [Fact]
    public void SanitizeSegment_DoesNotSplitSurrogatePairAtLengthBoundary()
    {
        var input = new string('a', 175) + "😀" + new string('b', 20) + ".txt";
        var value = FileNameSanitizer.SanitizeSegment(input);

        Assert.Equal(new string('a', 175) + ".txt", value);
        Assert.True(value.Length <= FileNameSanitizer.MaximumSegmentLength);
        Assert.True(Encoding.UTF8.GetByteCount(value) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
    }

    [Fact]
    public void SanitizeSegment_BoundsUnicodeHeavyNamesByUtf8Bytes()
    {
        var value = FileNameSanitizer.SanitizeSegment(new string('界', 100) + ".txt");

        Assert.True(Encoding.UTF8.GetByteCount(value) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
        Assert.EndsWith(".txt", value, StringComparison.Ordinal);
        Assert.DoesNotContain('\uFFFD', value);
    }

    [Fact]
    public void SanitizeSegment_BoundsEmojiNamesWithoutSplittingRunes()
    {
        var value = FileNameSanitizer.SanitizeSegment(string.Concat(Enumerable.Repeat("😀", 100)) + ".bin");

        Assert.True(Encoding.UTF8.GetByteCount(value) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
        Assert.EndsWith(".bin", value, StringComparison.Ordinal);
        Assert.DoesNotContain('\uFFFD', value);
    }

    [Fact]
    public void SanitizedSegment_LeavesByteHeadroomForPartialSuffix()
    {
        var value = FileNameSanitizer.SanitizeSegment(new string('界', 100) + ".dat");
        var stagedName = value + ".swiftdrop.part";

        Assert.True(Encoding.UTF8.GetByteCount(stagedName) <= 255);
    }

    [Fact]
    public void CreateCollisionSegment_UsesConventionalSuffixWhenItFits()
        => Assert.Equal("report (2).pdf", FileNameSanitizer.CreateCollisionSegment("report.pdf", 2));

    [Fact]
    public void CreateCollisionSegment_PreservesDistinctMarkerForMaxLengthBase()
    {
        var safe = FileNameSanitizer.SanitizeSegment(new string('a', 300) + ".txt");
        var first = FileNameSanitizer.CreateCollisionSegment(safe, 1);
        var second = FileNameSanitizer.CreateCollisionSegment(safe, 2);

        Assert.NotEqual(safe, first);
        Assert.NotEqual(first, second);
        Assert.StartsWith("(1) ", first, StringComparison.Ordinal);
        Assert.StartsWith("(2) ", second, StringComparison.Ordinal);
        Assert.True(first.Length <= FileNameSanitizer.MaximumSegmentLength);
        Assert.True(second.Length <= FileNameSanitizer.MaximumSegmentLength);
        Assert.True(Encoding.UTF8.GetByteCount(first) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
        Assert.True(Encoding.UTF8.GetByteCount(second) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
    }

    [Fact]
    public void CreateCollisionSegment_PreservesMarkerForUnicodeByteBoundBase()
    {
        var safe = FileNameSanitizer.SanitizeSegment(new string('界', 100) + ".dat");
        var collision = FileNameSanitizer.CreateCollisionSegment(safe, 9999);

        Assert.StartsWith("(9999) ", collision, StringComparison.Ordinal);
        Assert.True(Encoding.UTF8.GetByteCount(collision) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
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

    [Fact]
    public void PortableCollisionKey_Normalizes_Separators_And_Unicode()
    {
        var composed = FileNameSanitizer.GetPortableCollisionKey("Folder/Café.txt");
        var decomposed = FileNameSanitizer.GetPortableCollisionKey("Folder\\Cafe\u0301.txt");
        Assert.Equal(composed, decomposed, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void PortableCollisionKey_Reflects_Sanitized_Destination()
    {
        var first = FileNameSanitizer.GetPortableCollisionKey("report?.txt");
        var second = FileNameSanitizer.GetPortableCollisionKey("report*.txt");
        Assert.Equal(first, second, StringComparer.OrdinalIgnoreCase);
    }
}

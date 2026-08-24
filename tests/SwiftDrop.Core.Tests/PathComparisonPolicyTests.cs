using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PathComparisonPolicyTests
{
    [Fact]
    public void Comparer_MatchesDeclaredPlatformPolicy()
    {
        const string upper = "SwiftDrop/Folder/File.txt";
        const string lower = "swiftdrop/folder/file.txt";

        Assert.Equal(PathComparisonPolicy.UsesCaseInsensitivePaths, PathComparisonPolicy.Comparer.Equals(upper, lower));
    }

    [Fact]
    public void Comparison_MatchesDeclaredPlatformPolicy()
    {
        const string upper = "SwiftDrop/Folder/File.txt";
        const string lower = "swiftdrop/folder/file.txt";

        Assert.Equal(
            PathComparisonPolicy.UsesCaseInsensitivePaths,
            string.Equals(upper, lower, PathComparisonPolicy.Comparison));
    }

    [Fact]
    public void Comparer_AlwaysUsesOrdinalSemantics()
    {
        const string composed = "café";
        const string decomposed = "cafe\u0301";

        Assert.False(PathComparisonPolicy.Comparer.Equals(composed, decomposed));
    }
}

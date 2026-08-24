using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class ReceiveRootKeyTests
{
    [Fact]
    public void Create_RejectsWhitespaceRoot()
        => Assert.Throws<ArgumentException>(() => ReceiveRootKey.Create("   "));

    [Fact]
    public void Create_ReturnsCanonicalSha256Hex()
    {
        var key = ReceiveRootKey.Create(Path.Combine(Path.GetTempPath(), "swiftdrop-root-key"));

        Assert.Equal(64, key.Length);
        Assert.All(key, character => Assert.True(Uri.IsHexDigit(character)));
        Assert.Equal(key.ToUpperInvariant(), key);
    }

    [Fact]
    public void Create_NormalizesTrailingDirectorySeparators()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "swiftdrop-root-key-normalization"));
        var withSeparator = root + Path.DirectorySeparatorChar;

        Assert.Equal(ReceiveRootKey.Create(root), ReceiveRootKey.Create(withSeparator));
    }

    [Fact]
    public void Create_NormalizesRelativeAndAbsoluteForms()
        => Assert.Equal(ReceiveRootKey.Create("."), ReceiveRootKey.Create(Directory.GetCurrentDirectory()));

    [Fact]
    public void Create_UsesPlatformCaseComparisonPolicy()
    {
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "swiftdrop-root-key-case"));
        var upper = ReceiveRootKey.Create(Path.Combine(parent, "CaseProbe"));
        var lower = ReceiveRootKey.Create(Path.Combine(parent, "caseprobe"));

        Assert.Equal(PathComparisonPolicy.UsesCaseInsensitivePaths, upper == lower);
    }
}

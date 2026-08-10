using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class DiagnosticPrivacyRedactorTests
{
    [Theory]
    [InlineData("peer 192.168.1.10 connected", "peer [redacted] connected")]
    [InlineData("peer 192.168.1.10:47821 connected", "peer [redacted] connected")]
    [InlineData("peer fd00::20 connected", "peer [redacted] connected")]
    [InlineData("peer [fd00::20]:47821 connected", "peer [redacted] connected")]
    [InlineData("device 9c0d1129-56b0-4fa3-b815-60a1ef508df8 changed", "device [redacted] changed")]
    [InlineData("contact sanskarin@outlook.in", "contact [redacted]")]
    [InlineData("path C:\\Users\\Name\\file.txt", "path [redacted]")]
    [InlineData("uri swiftdrop://pair?p=secret", "uri [redacted]")]
    public void Redact_HidesCommonIdentifiers(string input, string expected)
        => Assert.Equal(expected, DiagnosticPrivacyRedactor.Redact(input));

    [Fact]
    public void Redact_HidesCompactSha256Fingerprint()
    {
        var fingerprint = new string('A', 64);
        Assert.Equal("fingerprint [redacted]", DiagnosticPrivacyRedactor.Redact($"fingerprint {fingerprint}"));
    }

    [Fact]
    public void Redact_HidesColonSeparatedSha256Fingerprint()
    {
        var fingerprint = string.Join(':', Enumerable.Repeat("AA", 32));
        Assert.Equal("fingerprint [redacted]", DiagnosticPrivacyRedactor.Redact($"fingerprint {fingerprint}"));
    }

    [Fact]
    public void Redact_PreservesGenericDiagnosticText()
        => Assert.Equal(
            "mDNS listener started successfully",
            DiagnosticPrivacyRedactor.Redact("mDNS listener started successfully"));
}

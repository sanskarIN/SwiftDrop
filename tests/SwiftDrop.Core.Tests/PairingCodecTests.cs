using System.Text;
using System.Text.Json;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class PairingCodecTests
{
    [Fact]
    public void RoundTrip_PreservesAndCanonicalizesPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { CertificateFingerprint = new string('a', 64) };
        var decoded = PairingCodec.Decode(PairingCodec.Encode(payload), now);
        Assert.Equal(payload with { CertificateFingerprint = new string('A', 64) }, decoded);
    }

    [Fact]
    public void Decode_RejectsExpiredPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { ExpiresUnixSeconds = now.AddSeconds(-1).ToUnixTimeSeconds() };
        Assert.Throws<InvalidOperationException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Fact]
    public void Decode_RejectsExcessiveLifetime()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { ExpiresUnixSeconds = now.AddHours(1).ToUnixTimeSeconds() };
        Assert.Throws<InvalidOperationException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("example.com")]
    [InlineData("2001:4860:4860::8888")]
    public void Decode_RejectsPublicOrDnsHost(string host)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { Host = host };
        Assert.Throws<InvalidDataException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Theory]
    [InlineData("192.168.1.20")]
    [InlineData("10.0.0.4")]
    [InlineData("172.20.4.8")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.10.9")]
    [InlineData("fd00::20")]
    [InlineData("fe80::1")]
    public void Decode_AcceptsLocalAddresses(string host)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { Host = host };
        var decoded = PairingCodec.Decode(PairingCodec.Encode(payload), now);
        Assert.False(string.IsNullOrWhiteSpace(decoded.Host));
    }

    [Theory]
    [InlineData("")]
    [InlineData("AA")]
    [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void Decode_RejectsInvalidFingerprint(string fingerprint)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { CertificateFingerprint = fingerprint };
        Assert.Throws<FormatException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("contains+plus-character")]
    public void Decode_RejectsInvalidNonce(string nonce)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { Nonce = nonce };
        Assert.Throws<FormatException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Fact]
    public void Decode_RejectsWrongVersion()
    {
        var now = DateTimeOffset.UtcNow;
        var payload = CreateValid(now) with { Version = "999" };
        Assert.Throws<NotSupportedException>(() => PairingCodec.Decode(PairingCodec.Encode(payload), now));
    }

    [Fact]
    public void Decode_RejectsWrongScheme()
        => Assert.Throws<FormatException>(() => PairingCodec.Decode("https://example.com"));

    [Fact]
    public void Decode_RejectsDuplicatePayloadParameter()
    {
        var now = DateTimeOffset.UtcNow;
        var link = PairingCodec.Encode(CreateValid(now));
        var encoded = link[(link.IndexOf("p=", StringComparison.Ordinal) + 2)..];
        Assert.Throws<FormatException>(() => PairingCodec.Decode($"swiftdrop://pair?p={encoded}&p={encoded}", now));
    }

    [Fact]
    public void Decode_RejectsUnexpectedQueryParameter()
    {
        var now = DateTimeOffset.UtcNow;
        var link = PairingCodec.Encode(CreateValid(now));
        Assert.Throws<FormatException>(() => PairingCodec.Decode(link + "&debug=true", now));
    }

    [Fact]
    public void Decode_RejectsUnexpectedOuterPath()
    {
        var now = DateTimeOffset.UtcNow;
        var link = PairingCodec.Encode(CreateValid(now));
        var query = link[link.IndexOf('?')..];
        Assert.Throws<FormatException>(() => PairingCodec.Decode($"swiftdrop://pair/extra{query}", now));
    }

    [Fact]
    public void Decode_RejectsExplicitOuterAuthorityPort()
    {
        var now = DateTimeOffset.UtcNow;
        var link = PairingCodec.Encode(CreateValid(now));
        var query = link[link.IndexOf('?')..];
        Assert.Throws<FormatException>(() => PairingCodec.Decode($"swiftdrop://pair:1234/{query}", now));
    }

    [Fact]
    public void Decode_RejectsDuplicateJsonProperty()
    {
        var now = DateTimeOffset.UtcNow;
        var json = SerializePayload(CreateValid(now)).TrimEnd('}');
        var link = BuildRawPayloadLink(json + ",\"version\":\"999\"}");

        Assert.Throws<FormatException>(() => PairingCodec.Decode(link, now));
    }

    [Fact]
    public void Decode_RejectsCaseVariantDuplicateJsonProperty()
    {
        var now = DateTimeOffset.UtcNow;
        var json = SerializePayload(CreateValid(now)).TrimEnd('}');
        var link = BuildRawPayloadLink(json + ",\"Version\":\"999\"}");

        Assert.Throws<FormatException>(() => PairingCodec.Decode(link, now));
    }

    [Fact]
    public void Decode_RejectsJsonCommentsAndTrailingCommas()
    {
        var now = DateTimeOffset.UtcNow;
        var json = SerializePayload(CreateValid(now));
        var commented = BuildRawPayloadLink("/*comment*/" + json);
        var trailing = BuildRawPayloadLink(json.TrimEnd('}') + ",}");

        Assert.Throws<FormatException>(() => PairingCodec.Decode(commented, now));
        Assert.Throws<FormatException>(() => PairingCodec.Decode(trailing, now));
    }

    [Fact]
    public void CreateNonce_ProducesBoundedBase64UrlEntropy()
    {
        var values = Enumerable.Range(0, 128).Select(_ => PairingCodec.CreateNonce()).ToArray();
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
        Assert.All(values, value =>
        {
            Assert.InRange(value.Length, 16, 128);
            Assert.All(value, ch => Assert.True(char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'));
        });
    }

    private static string SerializePayload(PairingPayload payload)
        => JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static string BuildRawPayloadLink(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var encoded = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"swiftdrop://pair?p={encoded}";
    }

    private static PairingPayload CreateValid(DateTimeOffset now)
        => new(
            ProtocolConstants.CurrentVersion,
            "device-abc",
            "Laptop",
            "192.168.1.20",
            ProtocolConstants.DefaultPort,
            new string('A', 64),
            PairingCodec.CreateNonce(),
            now.AddMinutes(2).ToUnixTimeSeconds());
}

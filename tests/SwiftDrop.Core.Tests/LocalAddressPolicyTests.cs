using System.Net;
using SwiftDrop.Core.Networking;

namespace SwiftDrop.Core.Tests;

public sealed class LocalAddressPolicyTests
{
    [Theory]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.50.10")]
    [InlineData("169.254.1.2")]
    [InlineData("127.0.0.1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("fe80::1")]
    [InlineData("::1")]
    public void IsLocal_AcceptsPrivateLinkLocalAndLoopback(string value)
        => Assert.True(LocalAddressPolicy.IsLocal(IPAddress.Parse(value)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.32.0.1")]
    [InlineData("192.0.2.5")]
    [InlineData("2001:4860:4860::8888")]
    public void IsLocal_RejectsPublicAddresses(string value)
        => Assert.False(LocalAddressPolicy.IsLocal(IPAddress.Parse(value)));

    [Fact]
    public void ParseAndValidate_RejectsDnsNames()
        => Assert.Throws<InvalidDataException>(() => LocalAddressPolicy.ParseAndValidate("example.com"));
}

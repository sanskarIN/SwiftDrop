using System.Net.NetworkInformation;
using SwiftDrop.Core.Networking;

namespace SwiftDrop.Core.Tests;

public sealed class LocalAddressSelectorTests
{
    [Fact]
    public void SelectBest_RejectsNullInterfaceSequence()
        => Assert.Throws<ArgumentNullException>(() => LocalAddressSelector.SelectBest(null!));

    [Fact]
    public void SelectBest_NeverReturnsPublicAddress()
    {
        var selected = LocalAddressSelector.SelectBest(NetworkInterface.GetAllNetworkInterfaces());

        Assert.True(LocalAddressPolicy.IsLocal(selected));
    }
}

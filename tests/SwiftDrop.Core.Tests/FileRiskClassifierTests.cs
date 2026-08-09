using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class FileRiskClassifierTests
{
    [Theory]
    [InlineData("photo.jpg", FileRiskLevel.Normal)]
    [InlineData("archive.zip", FileRiskLevel.Caution)]
    [InlineData("installer.exe", FileRiskLevel.High)]
    [InlineData("script.PS1", FileRiskLevel.High)]
    public void ClassifiesByExtension(string fileName, FileRiskLevel expected)
        => Assert.Equal(expected, FileRiskClassifier.Classify(fileName));
}

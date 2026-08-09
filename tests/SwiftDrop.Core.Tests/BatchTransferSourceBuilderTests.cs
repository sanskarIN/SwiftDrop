using SwiftDrop.Core.Transfer;

namespace SwiftDrop.Core.Tests;

public sealed class BatchTransferSourceBuilderTests
{
    [Fact]
    public async Task BuildAsync_IncludesFilesAndFolderRelativePaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "swiftdrop-batch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var single = Path.Combine(root, "single.txt");
            await File.WriteAllTextAsync(single, "one");
            var folder = Path.Combine(root, "folder");
            Directory.CreateDirectory(Path.Combine(folder, "nested"));
            await File.WriteAllTextAsync(Path.Combine(folder, "a.txt"), "two");
            await File.WriteAllTextAsync(Path.Combine(folder, "nested", "b.txt"), "three");

            var batch = await BatchTransferSourceBuilder.BuildAsync(new[] { single, folder });

            Assert.Equal(3, batch.FileCount);
            Assert.Contains(batch.Items, x => x.Entry.RelativePath == "single.txt");
            Assert.Contains(batch.Items, x => x.Entry.RelativePath.Replace('\\', '/') == "folder/a.txt");
            Assert.Contains(batch.Items, x => x.Entry.RelativePath.Replace('\\', '/') == "folder/nested/b.txt");
            Assert.Equal(batch.Items.Sum(x => x.Entry.Length), batch.TotalBytes);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task BuildAsync_RejectsEmptySelection()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BatchTransferSourceBuilder.BuildAsync(Array.Empty<string>()));
    }
}

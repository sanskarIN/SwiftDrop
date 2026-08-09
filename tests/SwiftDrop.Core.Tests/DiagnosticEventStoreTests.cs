using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class DiagnosticEventStoreTests
{
    [Fact]
    public async Task Store_RoundTripsAndClearsEvents()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-diag-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var store = new DiagnosticEventStore(path);
            await store.InitializeAsync();
            var entry = new DiagnosticEvent(
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                "Info",
                "network.ready",
                "Local network capability check passed.");

            await store.AddAsync(entry);
            var loaded = await store.GetRecentAsync();
            Assert.Single(loaded);
            Assert.Equal(entry.Code, loaded[0].Code);

            await store.ClearAsync();
            Assert.Empty(await store.GetRecentAsync());
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task Store_RejectsMultilineMessages()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-diag-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var store = new DiagnosticEventStore(path);
            await store.InitializeAsync();
            var entry = new DiagnosticEvent("id", DateTimeOffset.UtcNow, "Info", "test.code", "line1\nline2");
            await Assert.ThrowsAsync<ArgumentException>(() => store.AddAsync(entry));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-shm", path + "-wal" })
            if (File.Exists(candidate)) File.Delete(candidate);
    }
}

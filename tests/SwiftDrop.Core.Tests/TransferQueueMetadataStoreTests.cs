using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class TransferQueueMetadataStoreTests
{
    [Fact]
    public async Task UpsertAndRead_RoundTripsMetadataOnly()
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var now = DateTimeOffset.UtcNow.AddSeconds(-2);
            var entry = new TransferQueueMetadataEntry(
                Guid.NewGuid().ToString("N"),
                "Transfer",
                "Running",
                now,
                now.AddSeconds(1));

            await store.UpsertAsync(entry);
            var rows = await store.GetRecentAsync();

            var actual = Assert.Single(rows);
            Assert.Equal(entry.Id, actual.Id);
            Assert.Equal("Transfer", actual.Label);
            Assert.Equal("Running", actual.State);
            Assert.Null(actual.ErrorCode);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task MarkInFlightInterrupted_MarksQueuedAndRunningOnly()
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            await store.UpsertAsync(Entry("queued", "Queued", now));
            await store.UpsertAsync(Entry("running", "Running", now.AddSeconds(1)));
            await store.UpsertAsync(Entry("done", "Completed", now.AddSeconds(2)) with { FinishedUtc = now.AddSeconds(3) });

            await store.MarkInFlightInterruptedAsync(DateTimeOffset.UtcNow);
            var rows = await store.GetRecentAsync();

            Assert.Equal("Interrupted", rows.Single(x => x.Id == "queued").State);
            Assert.Equal("Interrupted", rows.Single(x => x.Id == "running").State);
            Assert.Equal("app-restarted", rows.Single(x => x.Id == "running").ErrorCode);
            Assert.Equal("Completed", rows.Single(x => x.Id == "done").State);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task DeleteFinished_KeepsOnlyActiveRows()
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            foreach (var state in new[] { "Completed", "Failed", "Cancelled", "Interrupted", "Queued", "Running" })
                await store.UpsertAsync(Entry(state, state, now));

            await store.DeleteFinishedAsync();
            var rows = await store.GetRecentAsync();

            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, x => x.State == "Queued");
            Assert.Contains(rows, x => x.State == "Running");
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Theory]
    [InlineData("unknown state", "Unknown")]
    [InlineData("unsafe error", "Failed")]
    public async Task Upsert_RejectsInvalidMetadata(string id, string state)
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var entry = Entry(id, state, DateTimeOffset.UtcNow.AddMinutes(-1)) with
            {
                ErrorCode = id == "unsafe error" ? "contains a secret-like free form message" : null
            };
            await Assert.ThrowsAsync<InvalidDataException>(() => store.UpsertAsync(entry));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    private static TransferQueueMetadataEntry Entry(string id, string state, DateTimeOffset created)
        => new(id, "Transfer", state, created);

    private static string TempDatabasePath()
        => Path.Combine(Path.GetTempPath(), "swiftdrop-queue-" + Guid.NewGuid().ToString("N") + ".db");

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-shm", path + "-wal" })
            if (File.Exists(candidate)) File.Delete(candidate);
    }
}

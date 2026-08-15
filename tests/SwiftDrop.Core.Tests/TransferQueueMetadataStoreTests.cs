using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class TransferQueueMetadataStoreTests
{
    [Fact]
    public async Task UpsertAndRead_RoundTripsRestartSafeMetadataOnly()
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var now = DateTimeOffset.UtcNow.AddSeconds(-5);
            var entry = new TransferQueueMetadataEntry(
                Guid.NewGuid().ToString("N"),
                "Transfer",
                "Running",
                now,
                now.AddSeconds(1),
                null,
                null,
                "File",
                now.AddSeconds(2),
                4_200,
                1,
                0);

            await store.UpsertAsync(entry);
            var rows = await store.GetRecentAsync();

            var actual = Assert.Single(rows);
            Assert.Equal(entry.Id, actual.Id);
            Assert.Equal("Transfer", actual.Label);
            Assert.Equal("Running", actual.State);
            Assert.Equal("File", actual.OperationKind);
            Assert.Equal(4_200, actual.ProgressBasisPoints);
            Assert.Equal(0.42d, actual.ProgressFraction, 6);
            Assert.Equal(1, actual.ItemCount);
            Assert.Equal(0, actual.CompletedItemCount);
            Assert.Equal(entry.UpdatedUtc, actual.UpdatedUtc);
            Assert.Null(actual.ErrorCode);
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task MarkInFlightInterrupted_MarksQueuedAndRunningOnlyAndPreservesProgress()
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var now = DateTimeOffset.UtcNow.AddMinutes(-1);
            await store.UpsertAsync(Entry("queued", "Queued", now) with
            {
                OperationKind = "Batch",
                ProgressBasisPoints = 2_500,
                ItemCount = 4,
                CompletedItemCount = 1,
                UpdatedUtc = now.AddSeconds(2)
            });
            await store.UpsertAsync(Entry("running", "Running", now.AddSeconds(1)));
            await store.UpsertAsync(Entry("done", "Completed", now.AddSeconds(2)) with
            {
                FinishedUtc = now.AddSeconds(3),
                UpdatedUtc = now.AddSeconds(3),
                ProgressBasisPoints = 10_000
            });

            var interruptedAt = DateTimeOffset.UtcNow;
            await store.MarkInFlightInterruptedAsync(interruptedAt);
            var rows = await store.GetRecentAsync();

            var queued = rows.Single(x => x.Id == "queued");
            Assert.Equal("Interrupted", queued.State);
            Assert.Equal("Batch", queued.OperationKind);
            Assert.Equal(2_500, queued.ProgressBasisPoints);
            Assert.Equal(4, queued.ItemCount);
            Assert.Equal(1, queued.CompletedItemCount);
            Assert.Equal("app-restarted", queued.ErrorCode);
            Assert.Equal(interruptedAt.ToUnixTimeSeconds(), queued.UpdatedUtc!.Value.ToUnixTimeSeconds());

            Assert.Equal("Interrupted", rows.Single(x => x.Id == "running").State);
            Assert.Equal("app-restarted", rows.Single(x => x.Id == "running").ErrorCode);
            Assert.Equal("Completed", rows.Single(x => x.Id == "done").State);
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
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
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Theory]
    [InlineData("unknown state", "Unknown", "Transfer", 0, null, null)]
    [InlineData("unsafe error", "Failed", "Transfer", 0, null, null)]
    [InlineData("bad operation", "Queued", "PairingSecret", 0, null, null)]
    [InlineData("bad progress", "Running", "File", 10001, 1, 0)]
    [InlineData("bad item count", "Running", "Batch", 100, 1, 2)]
    public async Task Upsert_RejectsInvalidMetadata(
        string id,
        string state,
        string operationKind,
        int progressBasisPoints,
        int? itemCount,
        int? completedItemCount)
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            var entry = Entry(id, state, DateTimeOffset.UtcNow.AddMinutes(-1)) with
            {
                ErrorCode = id == "unsafe error" ? "contains a secret-like free form message" : null,
                OperationKind = operationKind,
                ProgressBasisPoints = progressBasisPoints,
                ItemCount = itemCount,
                CompletedItemCount = completedItemCount
            };
            await Assert.ThrowsAsync<InvalidDataException>(() => store.UpsertAsync(entry));
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    [Fact]
    public async Task Upsert_DoesNotExposeAuthorizationFieldsInSchema()
    {
        var path = TempDatabasePath();
        try
        {
            var store = new TransferQueueMetadataStore(path);
            await store.InitializeAsync();

            await using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(transfer_queue_metadata);";
            await using var reader = await command.ExecuteReaderAsync();
            var columns = new List<string>();
            while (await reader.ReadAsync()) columns.Add(reader.GetString(1));

            Assert.DoesNotContain(columns, x => x.Contains("nonce", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(columns, x => x.Contains("token", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(columns, x => x.Contains("certificate", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(columns, x => x.Contains("host", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(columns, x => x.Contains("port", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            SqliteTestDatabaseCleanup.Delete(path);
        }
    }

    private static TransferQueueMetadataEntry Entry(string id, string state, DateTimeOffset created)
        => new(id, "Transfer", state, created);

    private static string TempDatabasePath()
        => Path.Combine(Path.GetTempPath(), "swiftdrop-queue-" + Guid.NewGuid().ToString("N") + ".db");
}

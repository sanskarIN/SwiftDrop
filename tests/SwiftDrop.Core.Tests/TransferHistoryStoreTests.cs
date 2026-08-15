using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class TransferHistoryStoreTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"swiftdrop-history-{Guid.NewGuid():N}.db");
    private TransferHistoryStore _store = null!;

    public async Task InitializeAsync()
    {
        _store = new TransferHistoryStore(_path);
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        SqliteTestDatabaseCleanup.Delete(_path);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAndReadRoundTripsEntry()
    {
        var entry = new TransferHistoryEntry(
            Guid.NewGuid().ToString("N"), "sent", "Desktop", "hello.txt", 42,
            DateTimeOffset.UtcNow, "completed", true, 1_250, 40);

        await _store.AddAsync(entry);
        var rows = await _store.GetRecentAsync();

        var row = Assert.Single(rows);
        Assert.Equal(entry.Id, row.Id);
        Assert.Equal("hello.txt", row.FileName);
        Assert.True(row.IntegrityVerified);
        Assert.Equal(1_250, row.DurationMilliseconds);
        Assert.Equal(40, row.MeasuredBytes);
    }

    [Fact]
    public async Task LegacyStyleEntryWithoutMeasurementRoundTripsNull()
    {
        await _store.AddAsync(new TransferHistoryEntry(
            "legacy-style", "received", "Phone", "photo.jpg", 100,
            DateTimeOffset.UtcNow, "completed", true));

        var row = Assert.Single(await _store.GetRecentAsync());
        Assert.Null(row.DurationMilliseconds);
        Assert.Null(row.MeasuredBytes);
    }

    [Fact]
    public async Task ClearRemovesEntries()
    {
        await _store.AddAsync(new TransferHistoryEntry(
            "id", "received", "Phone", "photo.jpg", 100, DateTimeOffset.UtcNow, "completed", true));

        await _store.ClearAsync();

        Assert.Empty(await _store.GetRecentAsync());
    }

    [Fact]
    public async Task AddRejectsInvalidMetadataDurationAndMeasuredBytes()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _store.AddAsync(new TransferHistoryEntry(
                "bad-size", "sent", "Phone", "file.txt", -1, DateTimeOffset.UtcNow, "failed", false)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _store.AddAsync(new TransferHistoryEntry(
                "bad-name", "sent", "Phone\nInjected", "file.txt", 1, DateTimeOffset.UtcNow, "failed", false)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _store.AddAsync(new TransferHistoryEntry(
                "bad-duration-negative", "sent", "Phone", "file.txt", 1,
                DateTimeOffset.UtcNow, "completed", true, -1, 1)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _store.AddAsync(new TransferHistoryEntry(
                "bad-duration-large", "sent", "Phone", "file.txt", 1,
                DateTimeOffset.UtcNow, "completed", true, TransferHistoryStore.MaxDurationMilliseconds + 1, 1)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _store.AddAsync(new TransferHistoryEntry(
                "bad-measured-negative", "sent", "Phone", "file.txt", 1,
                DateTimeOffset.UtcNow, "completed", true, 1, -1)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _store.AddAsync(new TransferHistoryEntry(
                "bad-measured-large", "sent", "Phone", "file.txt", 1,
                DateTimeOffset.UtcNow, "completed", true, 1, 2)));
    }

    [Fact]
    public async Task CorruptedPersistedRow_IsSkippedWithoutHidingValidRows()
    {
        var valid = new TransferHistoryEntry(
            "valid", "received", "Phone", "photo.jpg", 10, DateTimeOffset.UtcNow, "completed", true, 250, 10);
        await _store.AddAsync(valid);

        var cs = new SqliteConnectionStringBuilder { DataSource = _path }.ToString();
        await using (var db = new SqliteConnection(cs))
        {
            await db.OpenAsync();
            using var cmd = db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO transfer_history
                (id,direction,peer_device_name,file_name,size_bytes,timestamp_utc,status,integrity_verified,duration_ms,measured_bytes)
                VALUES('corrupt','received','Phone','bad.bin',1,'not-a-date','completed',1,100,1);
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var rows = await _store.GetRecentAsync();
        var row = Assert.Single(rows);
        Assert.Equal("valid", row.Id);
    }
}

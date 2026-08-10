using Microsoft.Data.Sqlite;
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

    [Fact]
    public async Task Store_SkipsCorruptedRowsAndReturnsValidRows()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-diag-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var store = new DiagnosticEventStore(path);
            await store.InitializeAsync();
            await store.AddAsync(new DiagnosticEvent(
                "valid",
                DateTimeOffset.UtcNow,
                "Info",
                "network.ready",
                "Ready."));

            var cs = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
            await using (var db = new SqliteConnection(cs))
            {
                await db.OpenAsync();
                var cmd = db.CreateCommand();
                cmd.CommandText = """
                    INSERT INTO diagnostic_events(id,timestamp_utc,level,code,message)
                    VALUES('corrupt','not-a-date','Info','bad.code','bad');
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            var loaded = await store.GetRecentAsync();
            var row = Assert.Single(loaded);
            Assert.Equal("valid", row.Id);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    [Fact]
    public async Task Store_RejectsFutureDiagnosticTimestamp()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-diag-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var store = new DiagnosticEventStore(path);
            await store.InitializeAsync();
            var entry = new DiagnosticEvent(
                "future",
                DateTimeOffset.UtcNow.AddDays(5),
                "Info",
                "test.code",
                "Future timestamp.");
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

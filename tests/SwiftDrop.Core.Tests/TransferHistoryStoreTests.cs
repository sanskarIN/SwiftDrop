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
        foreach (var suffix in new[] { string.Empty, "-shm", "-wal" })
        {
            var candidate = _path + suffix;
            if (File.Exists(candidate)) File.Delete(candidate);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAndReadRoundTripsEntry()
    {
        var entry = new TransferHistoryEntry(
            Guid.NewGuid().ToString("N"), "sent", "Desktop", "hello.txt", 42,
            DateTimeOffset.UtcNow, "completed", true);

        await _store.AddAsync(entry);
        var rows = await _store.GetRecentAsync();

        var row = Assert.Single(rows);
        Assert.Equal(entry.Id, row.Id);
        Assert.Equal("hello.txt", row.FileName);
        Assert.True(row.IntegrityVerified);
    }

    [Fact]
    public async Task ClearRemovesEntries()
    {
        await _store.AddAsync(new TransferHistoryEntry(
            "id", "received", "Phone", "photo.jpg", 100, DateTimeOffset.UtcNow, "completed", true));

        await _store.ClearAsync();

        Assert.Empty(await _store.GetRecentAsync());
    }
}

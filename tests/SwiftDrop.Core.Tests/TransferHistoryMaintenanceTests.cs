using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class TransferHistoryMaintenanceTests
{
    [Fact]
    public async Task PruneBefore_Removes_Only_Old_Rows()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-history-{Guid.NewGuid():N}.db");
        try
        {
            var store = new TransferHistoryStore(path);
            await store.InitializeAsync();
            var now = DateTimeOffset.UtcNow;
            await store.AddAsync(new TransferHistoryEntry("old", "sent", "Peer", "old.txt", 1, now.AddDays(-10), "completed", true));
            await store.AddAsync(new TransferHistoryEntry("new", "sent", "Peer", "new.txt", 1, now, "completed", true));

            var removed = await store.PruneBeforeAsync(now.AddDays(-5));
            var remaining = await store.GetRecentAsync();

            Assert.Equal(1, removed);
            Assert.Single(remaining);
            Assert.Equal("new", remaining[0].Id);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Delete_Removes_Only_Selected_Row()
    {
        var path = Path.Combine(Path.GetTempPath(), $"swiftdrop-history-{Guid.NewGuid():N}.db");
        try
        {
            var store = new TransferHistoryStore(path);
            await store.InitializeAsync();
            var now = DateTimeOffset.UtcNow;
            await store.AddAsync(new TransferHistoryEntry("a", "sent", "Peer", "a.txt", 1, now, "completed", true));
            await store.AddAsync(new TransferHistoryEntry("b", "sent", "Peer", "b.txt", 1, now, "completed", true));

            await store.DeleteAsync("a");
            var remaining = await store.GetRecentAsync();

            Assert.Single(remaining);
            Assert.Equal("b", remaining[0].Id);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

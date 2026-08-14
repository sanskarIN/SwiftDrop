using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class BatchCompletionStoreTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"swiftdrop-batch-completion-{Guid.NewGuid():N}.db");
    private BatchCompletionStore _store = null!;

    public async Task InitializeAsync()
    {
        _store = new BatchCompletionStore(_path);
        await _store.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        SqliteTestDatabaseCleanup.Delete(_path);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpsertAndGet_RoundTripsVerifiedMetadata()
    {
        var item = Item("batch", "source/a.txt", "dest/a.txt", 7, 'A');
        await _store.UpsertAsync(item);

        var loaded = await _store.GetAsync(item.TransferId, item.SourceRelativePath, item.ReceiveRootKey);
        Assert.Equal(item, loaded);
    }

    [Fact]
    public async Task Upsert_ReplacesSameTransferSourceRootMetadata()
    {
        var original = Item("batch", "a.txt", "a.txt", 7, 'A');
        var replacement = original with
        {
            DestinationRelativePath = "a (1).txt",
            Length = 8,
            Sha256 = new string('B', 64),
            CompletedUtc = original.CompletedUtc.AddMinutes(1)
        };
        await _store.UpsertAsync(original);
        await _store.UpsertAsync(replacement);

        Assert.Equal(replacement, await _store.GetAsync("batch", "a.txt", original.ReceiveRootKey));
    }

    [Fact]
    public async Task Remove_DeletesOnlyMatchingCompletion()
    {
        var first = Item("batch", "a.txt", "a.txt", 1, 'A');
        var second = Item("batch", "b.txt", "b.txt", 1, 'B');
        await _store.UpsertAsync(first);
        await _store.UpsertAsync(second);

        await _store.RemoveAsync(first.TransferId, first.SourceRelativePath, first.ReceiveRootKey);

        Assert.Null(await _store.GetAsync(first.TransferId, first.SourceRelativePath, first.ReceiveRootKey));
        Assert.NotNull(await _store.GetAsync(second.TransferId, second.SourceRelativePath, second.ReceiveRootKey));
    }

    [Fact]
    public async Task Get_SkipsCorruptedPersistedRow()
    {
        var item = Item("batch", "a.txt", "a.txt", 1, 'A');
        await _store.UpsertAsync(item);

        await using var connection = new SqliteConnection($"Data Source={_path}");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE completed_batch_items SET sha256='bad' WHERE transfer_id='batch';";
        await command.ExecuteNonQueryAsync();

        Assert.Null(await _store.GetAsync(item.TransferId, item.SourceRelativePath, item.ReceiveRootKey));
    }

    [Fact]
    public async Task Prune_RemovesExpiredRows()
    {
        var old = Item("old", "a.txt", "a.txt", 1, 'A') with { CompletedUtc = DateTimeOffset.UtcNow.AddDays(-10) };
        var recent = Item("recent", "b.txt", "b.txt", 1, 'B');
        await _store.UpsertAsync(old);
        await _store.UpsertAsync(recent);

        await _store.PruneAsync(DateTimeOffset.UtcNow.AddDays(-7));

        Assert.Null(await _store.GetAsync(old.TransferId, old.SourceRelativePath, old.ReceiveRootKey));
        Assert.NotNull(await _store.GetAsync(recent.TransferId, recent.SourceRelativePath, recent.ReceiveRootKey));
    }

    [Fact]
    public void ReceiveRootKey_DoesNotRevealAbsolutePath()
    {
        var root = Path.Combine(Path.GetTempPath(), "SwiftDrop", "Received");
        var key = ReceiveRootKey.Create(root);
        Assert.Equal(64, key.Length);
        Assert.DoesNotContain("SwiftDrop", key, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.DirectorySeparatorChar, key);
    }

    private static CompletedBatchItem Item(string transfer, string source, string destination, long length, char hash)
        => new(
            transfer,
            source,
            ReceiveRootKey.Create(Path.Combine(Path.GetTempPath(), "receive-root")),
            destination,
            length,
            new string(hash, 64),
            DateTimeOffset.UtcNow);
}

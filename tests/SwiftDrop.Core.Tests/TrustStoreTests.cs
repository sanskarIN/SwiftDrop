using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Storage;

namespace SwiftDrop.Core.Tests;

public sealed class TrustStoreTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"swiftdrop-trust-{Guid.NewGuid():N}.db");
    private TrustStore _store = null!;

    public async Task InitializeAsync()
    {
        _store = new TrustStore(_path);
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
    public async Task UpsertGetAndListTrustedPeer()
    {
        var now = DateTimeOffset.UtcNow;
        var peer = new TrustedPeer("device-1", "Laptop", new string('a', 64), now, now);

        await _store.UpsertAsync(peer);

        var loaded = await _store.GetAsync("device-1");
        Assert.NotNull(loaded);
        Assert.Equal("Laptop", loaded!.DeviceName);
        Assert.Equal(new string('A', 64), loaded.CertificateFingerprint);
        Assert.Single(await _store.GetAllAsync());
    }

    [Fact]
    public async Task RemoveAndClearDeletePeers()
    {
        var now = DateTimeOffset.UtcNow;
        await _store.UpsertAsync(new TrustedPeer("a", "A", new string('1', 64), now, now));
        await _store.UpsertAsync(new TrustedPeer("b", "B", new string('2', 64), now, now));

        await _store.RemoveAsync("a");
        Assert.Null(await _store.GetAsync("a"));
        Assert.Single(await _store.GetAllAsync());

        await _store.ClearAsync();
        Assert.Empty(await _store.GetAllAsync());
    }

    [Fact]
    public async Task UpsertRejectsMalformedFingerprint()
    {
        var now = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<FormatException>(() =>
            _store.UpsertAsync(new TrustedPeer("bad", "Bad", "AA11", now, now)));
    }

    [Fact]
    public async Task CorruptedPersistedFingerprint_IsIgnoredOnRead()
    {
        var now = DateTimeOffset.UtcNow;
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = _path,
            Mode = SqliteOpenMode.ReadWrite
        }.ToString();
        await using (var db = new SqliteConnection(cs))
        {
            await db.OpenAsync();
            var cmd = db.CreateCommand();
            cmd.CommandText = """
                INSERT INTO trusted_peers(device_id,device_name,fingerprint,trusted_utc,last_seen_utc)
                VALUES($id,$name,$fp,$trusted,$seen)
                """;
            cmd.Parameters.AddWithValue("$id", "corrupt");
            cmd.Parameters.AddWithValue("$name", "Corrupt");
            cmd.Parameters.AddWithValue("$fp", "not-a-fingerprint");
            cmd.Parameters.AddWithValue("$trusted", now.ToString("O"));
            cmd.Parameters.AddWithValue("$seen", now.ToString("O"));
            await cmd.ExecuteNonQueryAsync();
        }

        Assert.Null(await _store.GetAsync("corrupt"));
        Assert.Empty(await _store.GetAllAsync());
    }

    [Fact]
    public async Task UpsertWithChangedFingerprint_ReplacesTrustBindingForSameDeviceId()
    {
        var now = DateTimeOffset.UtcNow;
        await _store.UpsertAsync(new TrustedPeer("device", "Laptop", new string('A', 64), now, now));
        await _store.UpsertAsync(new TrustedPeer("device", "Laptop", new string('B', 64), now, now.AddMinutes(1)));

        var loaded = await _store.GetAsync("device");
        Assert.NotNull(loaded);
        Assert.Equal(new string('B', 64), loaded!.CertificateFingerprint);
    }
}

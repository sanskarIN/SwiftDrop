using System.Globalization;
using Microsoft.Data.Sqlite;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Storage;

public sealed class BatchCompletionStore
{
    private readonly string _connectionString;

    public BatchCompletionStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
    }

    public async Task UpsertAsync(CompletedBatchItem item, CancellationToken ct = default)
    {
        Validate(item);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO completed_batch_items(
                transfer_id, source_relative_path, receive_root_key,
                destination_relative_path, length, sha256, completed_utc)
            VALUES($transfer,$source,$root,$destination,$length,$sha,$completed)
            ON CONFLICT(transfer_id,source_relative_path,receive_root_key) DO UPDATE SET
                destination_relative_path=excluded.destination_relative_path,
                length=excluded.length,
                sha256=excluded.sha256,
                completed_utc=excluded.completed_utc;
            """;
        command.Parameters.AddWithValue("$transfer", item.TransferId);
        command.Parameters.AddWithValue("$source", item.SourceRelativePath);
        command.Parameters.AddWithValue("$root", item.ReceiveRootKey);
        command.Parameters.AddWithValue("$destination", item.DestinationRelativePath);
        command.Parameters.AddWithValue("$length", item.Length);
        command.Parameters.AddWithValue("$sha", item.Sha256);
        command.Parameters.AddWithValue("$completed", item.CompletedUtc.UtcDateTime.ToString("O"));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<CompletedBatchItem?> GetAsync(
        string transferId,
        string sourceRelativePath,
        string receiveRootKey,
        CancellationToken ct = default)
    {
        ValidateKey(transferId, sourceRelativePath, receiveRootKey);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT destination_relative_path,length,sha256,completed_utc
            FROM completed_batch_items
            WHERE transfer_id=$transfer AND source_relative_path=$source AND receive_root_key=$root;
            """;
        command.Parameters.AddWithValue("$transfer", transferId);
        command.Parameters.AddWithValue("$source", sourceRelativePath);
        command.Parameters.AddWithValue("$root", receiveRootKey);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        try
        {
            var item = new CompletedBatchItem(
                transferId,
                sourceRelativePath,
                receiveRootKey,
                reader.GetString(0),
                reader.GetInt64(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            Validate(item);
            return item;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidDataException or OverflowException)
        {
            return null;
        }
    }

    public async Task RemoveAsync(
        string transferId,
        string sourceRelativePath,
        string receiveRootKey,
        CancellationToken ct = default)
    {
        ValidateKey(transferId, sourceRelativePath, receiveRootKey);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM completed_batch_items
            WHERE transfer_id=$transfer AND source_relative_path=$source AND receive_root_key=$root;
            """;
        command.Parameters.AddWithValue("$transfer", transferId);
        command.Parameters.AddWithValue("$source", sourceRelativePath);
        command.Parameters.AddWithValue("$root", receiveRootKey);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task PruneAsync(DateTimeOffset cutoffUtc, int maximumRows = 4096, CancellationToken ct = default)
    {
        if (maximumRows is < 1 or > 100_000) throw new ArgumentOutOfRangeException(nameof(maximumRows));
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await DatabaseSchemaManager.EnsureCurrentAsync(connection, ct);
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM completed_batch_items WHERE completed_utc < $cutoff;
            DELETE FROM completed_batch_items
            WHERE rowid NOT IN (
                SELECT rowid FROM completed_batch_items ORDER BY completed_utc DESC LIMIT $maxRows
            );
            """;
        command.Parameters.AddWithValue("$cutoff", cutoffUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$maxRows", maximumRows);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void Validate(CompletedBatchItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ValidateKey(item.TransferId, item.SourceRelativePath, item.ReceiveRootKey);
        if (string.IsNullOrWhiteSpace(item.DestinationRelativePath) || item.DestinationRelativePath.Length > 1024)
            throw new ArgumentException("Invalid completed batch destination path.", nameof(item));
        _ = FileNameSanitizer.SanitizeRelativePath(item.DestinationRelativePath);
        if (item.Length < 0 || item.Length > ProtocolConstants.MaxSingleFileBytes)
            throw new ArgumentException("Invalid completed batch item length.", nameof(item));
        if (item.Sha256.Length != 64 || !item.Sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid completed batch item SHA-256.", nameof(item));
    }

    private static void ValidateKey(string transferId, string sourceRelativePath, string receiveRootKey)
    {
        IncomingRequestPolicy.ValidateTransferId(transferId);
        if (string.IsNullOrWhiteSpace(sourceRelativePath) || sourceRelativePath.Length > 1024)
            throw new ArgumentException("Invalid completed batch source path.", nameof(sourceRelativePath));
        _ = FileNameSanitizer.SanitizeRelativePath(sourceRelativePath);
        if (receiveRootKey.Length != 64 || !receiveRootKey.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid receive-root key.", nameof(receiveRootKey));
    }
}

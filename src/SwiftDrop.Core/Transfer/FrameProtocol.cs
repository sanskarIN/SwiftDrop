using System.Buffers.Binary;
using System.Text.Json;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class FrameProtocol
{
    private const int MaxJsonDepth = 32;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = MaxJsonDepth
    };
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        MaxDepth = MaxJsonDepth,
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow
    };

    public static async Task WriteJsonAsync<T>(Stream stream, T value, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        if (payload.Length <= 0 || payload.Length > ProtocolConstants.HeaderLimitBytes)
            throw new InvalidDataException("Protocol frame too large.");

        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await WriteWithTimeoutAsync(stream, header, ct);
        await WriteWithTimeoutAsync(stream, payload, ct);
        await FlushWithTimeoutAsync(stream, ct);
    }

    public static async Task<T> ReadJsonAsync<T>(Stream stream, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = new byte[4];
        await ReadExactlyAsync(stream, header, ct);
        var len = BinaryPrimitives.ReadInt32BigEndian(header);
        if (len <= 0 || len > ProtocolConstants.HeaderLimitBytes)
            throw new InvalidDataException("Invalid protocol frame length.");

        var payload = new byte[len];
        await ReadExactlyAsync(stream, payload, ct);
        try
        {
            using var document = JsonDocument.Parse(payload, DocumentOptions);
            EnsureNoDuplicateProperties(document.RootElement);
            return JsonSerializer.Deserialize<T>(payload, JsonOptions)
                   ?? throw new InvalidDataException("Invalid protocol frame.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Invalid protocol JSON.", ex);
        }
    }

    public static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var total = 0;
        while (total < buffer.Length)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(ProtocolConstants.IdleTimeout);
            int read;
            try
            {
                read = await stream.ReadAsync(buffer[total..], timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException("Peer did not complete a protocol frame before the idle timeout.");
            }
            if (read == 0) throw new EndOfStreamException();
            total += read;
        }
    }

    private static void EnsureNoDuplicateProperties(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        throw new InvalidDataException("Protocol JSON contains duplicate property names.");
                    EnsureNoDuplicateProperties(property.Value);
                }
                break;
            }
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    EnsureNoDuplicateProperties(item);
                break;
        }
    }

    private static async Task WriteWithTimeoutAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProtocolConstants.IdleTimeout);
        try
        {
            await stream.WriteAsync(buffer, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Peer did not accept a protocol frame before the idle timeout.");
        }
    }

    private static async Task FlushWithTimeoutAsync(Stream stream, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(ProtocolConstants.IdleTimeout);
        try
        {
            await stream.FlushAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Peer did not accept a protocol frame before the idle timeout.");
        }
    }
}

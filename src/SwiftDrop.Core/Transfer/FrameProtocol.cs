using System.Buffers.Binary;
using System.Text.Json;
using SwiftDrop.Core.Protocol;

namespace SwiftDrop.Core.Transfer;

public static class FrameProtocol
{
    public static async Task WriteJsonAsync<T>(Stream stream, T value, CancellationToken ct)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (payload.Length > ProtocolConstants.HeaderLimitBytes) throw new InvalidDataException("Protocol frame too large.");
        var header = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await stream.WriteAsync(header, ct);
        await stream.WriteAsync(payload, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<T> ReadJsonAsync<T>(Stream stream, CancellationToken ct)
    {
        var header = new byte[4];
        await ReadExactlyAsync(stream, header, ct);
        var len = BinaryPrimitives.ReadInt32BigEndian(header);
        if (len <= 0 || len > ProtocolConstants.HeaderLimitBytes) throw new InvalidDataException("Invalid protocol frame length.");
        var payload = new byte[len];
        await ReadExactlyAsync(stream, payload, ct);
        return JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("Invalid protocol frame.");
    }

    public static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], ct);
            if (read == 0) throw new EndOfStreamException();
            total += read;
        }
    }
}

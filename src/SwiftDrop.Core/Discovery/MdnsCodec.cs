using System.Buffers.Binary;
using System.Net;
using System.Text;
using SwiftDrop.Core.Models;

namespace SwiftDrop.Core.Discovery;

public static class MdnsCodec
{
    public const string ServiceName = "_swiftdrop._tcp.local";
    private const ushort TypeA = 1;
    private const ushort TypePtr = 12;
    private const ushort TypeTxt = 16;
    private const ushort TypeSrv = 33;
    private const ushort ClassIn = 1;
    private const uint TtlSeconds = 15;

    public static byte[] CreateQuery()
    {
        using var stream = new MemoryStream();
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 1);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);
        WriteName(stream, ServiceName);
        WriteUInt16(stream, TypePtr);
        WriteUInt16(stream, ClassIn);
        return stream.ToArray();
    }

    public static bool IsDiscoveryQuery(ReadOnlySpan<byte> packet)
    {
        try
        {
            var reader = new Reader(packet);
            _ = reader.ReadUInt16();
            var flags = reader.ReadUInt16();
            var questionCount = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            if ((flags & 0x8000) != 0) return false;
            for (var i = 0; i < questionCount; i++)
            {
                var name = reader.ReadName();
                var type = reader.ReadUInt16();
                _ = reader.ReadUInt16();
                if (type == TypePtr && string.Equals(name, ServiceName, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    public static byte[] CreateAnnouncement(PeerDevice device, IPAddress ipv4Address)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(ipv4Address);
        if (ipv4Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            throw new ArgumentException("mDNS announcement requires IPv4.", nameof(ipv4Address));
        if (device.Port is <= 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(device.Port));

        var instance = $"{SanitizeLabel(device.Id)}.{ServiceName}";
        var host = $"{SanitizeLabel(device.Id)}.local";
        using var stream = new MemoryStream();
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0x8400);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 4);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);

        WriteRecord(stream, ServiceName, TypePtr, ClassIn, EncodeName(instance));

        using (var srv = new MemoryStream())
        {
            WriteUInt16(srv, 0);
            WriteUInt16(srv, 0);
            WriteUInt16(srv, checked((ushort)device.Port));
            WriteName(srv, host);
            WriteRecord(stream, instance, TypeSrv, 0x8001, srv.ToArray());
        }

        var txt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = device.Id,
            ["name"] = device.Name,
            ["platform"] = device.Platform,
            ["fp"] = device.CertificateFingerprint ?? string.Empty
        };
        WriteRecord(stream, instance, TypeTxt, 0x8001, EncodeTxt(txt));
        WriteRecord(stream, host, TypeA, 0x8001, ipv4Address.GetAddressBytes());
        return stream.ToArray();
    }

    public static PeerDevice? TryParseAnnouncement(ReadOnlySpan<byte> packet, IPAddress fallbackAddress, DateTimeOffset nowUtc)
    {
        try
        {
            var reader = new Reader(packet);
            _ = reader.ReadUInt16();
            var flags = reader.ReadUInt16();
            var questions = reader.ReadUInt16();
            var answers = reader.ReadUInt16();
            var authorities = reader.ReadUInt16();
            var additionals = reader.ReadUInt16();
            if ((flags & 0x8000) == 0) return null;

            for (var i = 0; i < questions; i++)
            {
                _ = reader.ReadName();
                reader.Skip(4);
            }

            string? serviceInstance = null;
            string? id = null;
            string? name = null;
            string? platform = null;
            string? fingerprint = null;
            int port = 0;
            IPAddress? address = null;
            var recordCount = answers + authorities + additionals;

            for (var i = 0; i < recordCount; i++)
            {
                var owner = reader.ReadName();
                var type = reader.ReadUInt16();
                _ = reader.ReadUInt16();
                _ = reader.ReadUInt32();
                var length = reader.ReadUInt16();
                var dataStart = reader.Position;
                var dataEnd = checked(dataStart + length);
                if (dataEnd > reader.Length) throw new InvalidDataException("mDNS record exceeds packet boundary.");

                if (type == TypePtr && string.Equals(owner, ServiceName, StringComparison.OrdinalIgnoreCase))
                {
                    serviceInstance = reader.ReadName();
                }
                else if (type == TypeSrv && (serviceInstance is null || string.Equals(owner, serviceInstance, StringComparison.OrdinalIgnoreCase)))
                {
                    _ = reader.ReadUInt16();
                    _ = reader.ReadUInt16();
                    port = reader.ReadUInt16();
                    _ = reader.ReadName();
                }
                else if (type == TypeTxt && (serviceInstance is null || string.Equals(owner, serviceInstance, StringComparison.OrdinalIgnoreCase)))
                {
                    var values = reader.ReadTxt(length);
                    values.TryGetValue("id", out id);
                    values.TryGetValue("name", out name);
                    values.TryGetValue("platform", out platform);
                    values.TryGetValue("fp", out fingerprint);
                }
                else if (type == TypeA && length == 4)
                {
                    address = new IPAddress(reader.ReadBytes(4));
                }

                reader.Position = dataEnd;
            }

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(platform) || port is <= 0 or > 65535)
                return null;
            if (serviceInstance is not null && !serviceInstance.EndsWith('.' + ServiceName, StringComparison.OrdinalIgnoreCase))
                return null;

            return new PeerDevice(
                id,
                name,
                platform,
                (address ?? fallbackAddress).ToString(),
                port,
                string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint,
                false,
                nowUtc);
        }
        catch (Exception ex) when (ex is InvalidDataException or OverflowException or ArgumentException)
        {
            return null;
        }
    }

    private static string SanitizeLabel(string value)
    {
        var chars = value.Where(ch => char.IsLetterOrDigit(ch) || ch == '-').Take(63).ToArray();
        return chars.Length == 0 ? "swiftdrop" : new string(chars);
    }

    private static byte[] EncodeName(string name)
    {
        using var stream = new MemoryStream();
        WriteName(stream, name);
        return stream.ToArray();
    }

    private static byte[] EncodeTxt(IReadOnlyDictionary<string, string> values)
    {
        using var stream = new MemoryStream();
        foreach (var pair in values)
        {
            var bytes = Encoding.UTF8.GetBytes($"{pair.Key}={pair.Value}");
            if (bytes.Length > 255) throw new InvalidDataException("mDNS TXT value exceeds 255 bytes.");
            stream.WriteByte((byte)bytes.Length);
            stream.Write(bytes);
        }
        return stream.ToArray();
    }

    private static void WriteRecord(Stream stream, string owner, ushort type, ushort recordClass, byte[] rdata)
    {
        WriteName(stream, owner);
        WriteUInt16(stream, type);
        WriteUInt16(stream, recordClass);
        WriteUInt32(stream, TtlSeconds);
        WriteUInt16(stream, checked((ushort)rdata.Length));
        stream.Write(rdata);
    }

    private static void WriteName(Stream stream, string name)
    {
        foreach (var label in name.TrimEnd('.').Split('.'))
        {
            var bytes = Encoding.UTF8.GetBytes(label);
            if (bytes.Length is 0 or > 63) throw new InvalidDataException("Invalid DNS label length.");
            stream.WriteByte((byte)bytes.Length);
            stream.Write(bytes);
        }
        stream.WriteByte(0);
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }

    private ref struct Reader
    {
        private readonly ReadOnlySpan<byte> _data;
        public Reader(ReadOnlySpan<byte> data) => _data = data;
        public int Position { get; set; }
        public int Length => _data.Length;

        public ushort ReadUInt16()
        {
            Ensure(2);
            var value = BinaryPrimitives.ReadUInt16BigEndian(_data.Slice(Position, 2));
            Position += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            Ensure(4);
            var value = BinaryPrimitives.ReadUInt32BigEndian(_data.Slice(Position, 4));
            Position += 4;
            return value;
        }

        public byte[] ReadBytes(int count)
        {
            Ensure(count);
            var value = _data.Slice(Position, count).ToArray();
            Position += count;
            return value;
        }

        public void Skip(int count)
        {
            Ensure(count);
            Position += count;
        }

        public string ReadName()
        {
            var labels = new List<string>();
            var cursor = Position;
            var jumped = false;
            var jumpCount = 0;
            while (true)
            {
                if (cursor >= _data.Length) throw new InvalidDataException("Truncated DNS name.");
                var length = _data[cursor++];
                if (length == 0)
                {
                    if (!jumped) Position = cursor;
                    break;
                }
                if ((length & 0xC0) == 0xC0)
                {
                    if (cursor >= _data.Length) throw new InvalidDataException("Truncated DNS pointer.");
                    var offset = ((length & 0x3F) << 8) | _data[cursor++];
                    if (offset >= _data.Length || ++jumpCount > 16) throw new InvalidDataException("Invalid DNS pointer.");
                    if (!jumped) Position = cursor;
                    cursor = offset;
                    jumped = true;
                    continue;
                }
                if (length > 63 || cursor + length > _data.Length) throw new InvalidDataException("Invalid DNS label.");
                labels.Add(Encoding.UTF8.GetString(_data.Slice(cursor, length)));
                cursor += length;
                if (!jumped) Position = cursor;
            }
            return string.Join('.', labels);
        }

        public Dictionary<string, string> ReadTxt(int length)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var end = checked(Position + length);
            if (end > _data.Length) throw new InvalidDataException("Truncated TXT record.");
            while (Position < end)
            {
                var itemLength = _data[Position++];
                if (Position + itemLength > end) throw new InvalidDataException("Invalid TXT item length.");
                var item = Encoding.UTF8.GetString(_data.Slice(Position, itemLength));
                Position += itemLength;
                var separator = item.IndexOf('=');
                if (separator > 0) result[item[..separator]] = item[(separator + 1)..];
            }
            return result;
        }

        private void Ensure(int count)
        {
            if (count < 0 || Position > _data.Length - count) throw new InvalidDataException("Truncated mDNS packet.");
        }
    }
}

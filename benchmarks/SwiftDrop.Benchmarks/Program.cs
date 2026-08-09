using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using SwiftDrop.Core.Models;
using SwiftDrop.Core.Protocol;
using SwiftDrop.Core.Security;
using SwiftDrop.Core.Transfer;

var options = BenchmarkOptions.Parse(args);
var tempRoot = Path.Combine(Path.GetTempPath(), $"swiftdrop-benchmark-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempRoot);
var tempFile = Path.Combine(tempRoot, "synthetic.bin");

try
{
    await CreateSyntheticFileAsync(tempFile, options.SizeMiB);
    var hash = await BenchmarkHashingAsync(tempFile, options.Iterations);
    var manifest = BenchmarkManifestValidation(options.Iterations);
    var sanitation = BenchmarkPathSanitation(options.Iterations);

    var result = new BenchmarkReport(
        DateTimeOffset.UtcNow,
        Environment.OSVersion.ToString(),
        Environment.Version.ToString(),
        Environment.ProcessorCount,
        options,
        hash,
        manifest,
        sanitation);

    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
}
finally
{
    try
    {
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }
    catch
    {
        // Benchmark cleanup must not hide benchmark output or touch paths outside its unique temp root.
    }
}

static async Task CreateSyntheticFileAsync(string path, int sizeMiB)
{
    var chunk = new byte[1024 * 1024];
    RandomNumberGenerator.Fill(chunk);
    await using var stream = new FileStream(
        path,
        FileMode.CreateNew,
        FileAccess.Write,
        FileShare.None,
        chunk.Length,
        FileOptions.Asynchronous | FileOptions.SequentialScan);
    for (var i = 0; i < sizeMiB; i++)
        await stream.WriteAsync(chunk);
    await stream.FlushAsync();
}

static async Task<HashBenchmark> BenchmarkHashingAsync(string path, int iterations)
{
    var bytes = new FileInfo(path).Length;
    var stopwatch = Stopwatch.StartNew();
    string digest = string.Empty;
    for (var i = 0; i < iterations; i++)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        digest = Convert.ToHexString(await SHA256.HashDataAsync(stream));
    }
    stopwatch.Stop();
    var totalMiB = bytes * iterations / 1024d / 1024d;
    return new HashBenchmark(bytes, iterations, stopwatch.Elapsed.TotalMilliseconds,
        totalMiB / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.000001), digest);
}

static OperationBenchmark BenchmarkManifestValidation(int iterations)
{
    var count = Math.Min(1000, ProtocolConstants.MaxBatchFiles);
    var now = DateTimeOffset.UtcNow.AddMinutes(-1);
    var entries = Enumerable.Range(0, count)
        .Select(i => new FileManifestEntry($"folder-{i % 25}/file-{i:D5}.bin", 1024, new string('A', 64), now))
        .ToArray();
    var total = entries.Sum(x => x.Length);

    var stopwatch = Stopwatch.StartNew();
    long operations = 0;
    for (var i = 0; i < iterations; i++)
    {
        _ = BatchManifestValidator.Validate(entries, total);
        operations += entries.Length;
    }
    stopwatch.Stop();
    return OperationBenchmark.From("batch-manifest-validation", operations, stopwatch.Elapsed);
}

static OperationBenchmark BenchmarkPathSanitation(int iterations)
{
    var samples = Enumerable.Range(0, 2000)
        .Select(i => $"Folder {i % 50}/Café report {i:D5}?.txt")
        .ToArray();

    var stopwatch = Stopwatch.StartNew();
    long operations = 0;
    for (var i = 0; i < iterations; i++)
    {
        foreach (var sample in samples)
        {
            _ = FileNameSanitizer.GetPortableCollisionKey(sample);
            operations++;
        }
    }
    stopwatch.Stop();
    return OperationBenchmark.From("path-sanitation", operations, stopwatch.Elapsed);
}

public sealed record BenchmarkOptions(int SizeMiB, int Iterations)
{
    public static BenchmarkOptions Parse(string[] args)
    {
        var sizeMiB = ReadInt(args, "--size-mib", 128);
        var iterations = ReadInt(args, "--iterations", 3);
        if (sizeMiB is < 1 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(args), "--size-mib must be between 1 and 4096.");
        if (iterations is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(args), "--iterations must be between 1 and 20.");
        return new BenchmarkOptions(sizeMiB, iterations);
    }

    private static int ReadInt(string[] args, string name, int fallback)
    {
        var index = Array.FindIndex(args, x => string.Equals(x, name, StringComparison.Ordinal));
        if (index < 0) return fallback;
        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out var value))
            throw new ArgumentException($"{name} requires an integer value.");
        return value;
    }
}

public sealed record BenchmarkReport(
    DateTimeOffset TimestampUtc,
    string OperatingSystem,
    string DotNetVersion,
    int ProcessorCount,
    BenchmarkOptions Options,
    HashBenchmark Hashing,
    OperationBenchmark ManifestValidation,
    OperationBenchmark PathSanitation);

public sealed record HashBenchmark(
    long BytesPerIteration,
    int Iterations,
    double ElapsedMilliseconds,
    double MiBPerSecond,
    string LastSha256);

public sealed record OperationBenchmark(
    string Name,
    long Operations,
    double ElapsedMilliseconds,
    double OperationsPerSecond)
{
    public static OperationBenchmark From(string name, long operations, TimeSpan elapsed)
        => new(name, operations, elapsed.TotalMilliseconds,
            operations / Math.Max(elapsed.TotalSeconds, 0.000001));
}

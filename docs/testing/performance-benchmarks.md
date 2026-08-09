# SwiftDrop synthetic performance benchmarks

The benchmark harness measures repeatable local CPU/storage operations without reading user files, contacting network peers, or storing transfer content outside a uniquely named temporary directory.

## Run

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

The harness emits JSON containing the operating system, .NET version, processor count, options, SHA-256 throughput, batch-manifest validation rate, and portable path-sanitation rate.

## Safety bounds

- `--size-mib`: 1–4096 MiB.
- `--iterations`: 1–20.
- File bytes are synthetic random data created under the OS temporary directory.
- Cleanup is constrained to the harness's unique temporary directory.
- No user-selected file, clipboard data, pairing invitation, certificate, private key, history database, or network peer is accessed.

## Release use

Record results for representative release hardware and compare like-for-like runs. Do not treat a single CI runner result as a device-performance guarantee. At minimum measure Android, Windows, iOS, and macOS release candidates on representative hardware, and separately measure full peer-to-peer transfer throughput because storage hashing alone does not model Wi-Fi, TLS, platform scheduling, or receiver write performance.

Large-file validation should additionally observe peak memory and confirm that transfer code continues to stream rather than allocating a whole file. Benchmark data is engineering evidence, not a promise of a fixed transfer speed.

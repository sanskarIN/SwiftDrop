# SwiftDrop performance measurement

SwiftDrop uses two separate forms of performance evidence: a synthetic Core benchmark harness and optional local transfer-duration samples in Transfer History. Neither is a promise of fixed transfer speed.

## Synthetic benchmark harness

The benchmark harness measures repeatable local CPU/storage operations without reading user files, contacting network peers, or storing transfer content outside a uniquely named temporary directory.

### Run

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

The harness emits JSON containing the operating system, .NET version, processor count, options, SHA-256 throughput, batch-manifest validation rate, and portable path-sanitation rate.

### Safety bounds

- `--size-mib`: 1–4096 MiB.
- `--iterations`: 1–20.
- File bytes are synthetic random data created under the OS temporary directory.
- Cleanup is constrained to the harness's unique temporary directory.
- No user-selected file, clipboard data, pairing invitation, certificate, private key, history database, or network peer is accessed.

## Transfer History performance samples

Schema v6 stores optional `duration_ms` plus `measured_bytes` metadata in local Transfer History. A performance sample is eligible for throughput calculation only when all of the following are true:

- the history status is `completed`;
- the actual `measured_bytes` count is positive and does not exceed the logical history size;
- a positive elapsed duration was actually measured for that completed operation.

Legacy rows, zero-byte transfers, rejected/skipped records, and rows without both an attributable duration and attributable byte count remain unmeasured. The application does not manufacture timing data for them.

The History screen reports a **weighted aggregate throughput** over measured rows: total actual measured bytes divided by total measured elapsed time. For resumed files, only `logical size - negotiated resume offset` is recorded as measured bytes for that interval, so bytes already present before resume cannot inflate the rate. This avoids giving a tiny transfer the same statistical weight as a large transfer. Per-row duration and throughput are shown only for measured completed rows.

Elapsed timing uses monotonic `Stopwatch` measurements in the live transfer path. For outgoing single-file operations the measured interval covers the queued send operation as invoked by the page; incoming file and accepted batch-item measurements cover the actual receive-stream operation. Because protocol setup, resume offsets, storage, Wi-Fi/TLS behavior, and platform scheduling differ between paths, these numbers are observational diagnostics rather than standardized benchmarks.

Performance history adds only bounded numeric duration and measured-byte metadata. It does not add peer IP/port information, pairing nonces, tokens, certificates, private keys, transfer content, or reusable authorization. Peer/file display fields continue to follow the existing history privacy-mode and retention rules.

## Release use

Record synthetic results for representative release hardware and compare like-for-like runs. Do not treat a single CI runner result as a device-performance guarantee. At minimum measure Android, Windows, iOS, and macOS release candidates on representative hardware, and separately measure full peer-to-peer transfer throughput because storage hashing alone does not model Wi-Fi, TLS, platform scheduling, or receiver write performance.

When using History samples during release testing, compare similar devices, network conditions, file sizes, resume state, and sender/receiver roles. Do not combine unrelated environments into a marketing performance claim.

Large-file validation should additionally observe peak memory and confirm that transfer code continues to stream rather than allocating a whole file. Benchmark and history data are engineering evidence, not a guarantee of a fixed transfer speed.

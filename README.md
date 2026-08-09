# SwiftDrop

SwiftDrop is an open-source, account-free local-network file and text transfer app built with .NET MAUI and C#. It is designed for direct peer-to-peer transfers across Android, iOS, macOS (Mac Catalyst), and Windows without uploading transfer content to a SwiftDrop cloud service.

> **Privacy model:** transfer payloads stay on the local peer-to-peer path. SwiftDrop stores only local metadata required for settings, trust, history, bounded diagnostics, and privacy-minimal queue status. See `PRIVACY.md`.

## Current capabilities

- Automatic nearby discovery with internal mDNS/DNS-SD plus bounded UDP broadcast fallback.
- QR/deep-link pairing, nearby pairing requests, manual local-IP fallback, and short-lived one-time 8-digit pairing codes.
- Strict pairing-invitation validation for protocol version, local/private numeric IP address, device metadata bounds, canonical SHA-256 fingerprint, nonce format, expiry/lifetime, and unexpected URI/query data.
- Receiver-certificate SHA-256 pinning and sender client certificates over platform/.NET TLS 1.3/1.2.
- Local P-256 ECDSA peer certificates with TLS client/server EKUs, secure-storage persistence, explicit renewal/recovery policy, and user-visible identity refresh when an old certificate cannot be safely reused.
- Explicit receiver approval, sender certificate display, trusted-device storage/revocation, and optional normal-file auto-accept for explicitly trusted certificates.
- Single-file transfer with streaming progress, cancellation, safe pause/resume through fresh pairing, `.swiftdrop.part` staging, SHA-256 verification, free-space checks, manifest-bound source length, and atomic collision-safe destination reservations.
- Multi-file and recursive folder manifests with sender/receiver aggregate limits, receiver accept-all/selective/reject decisions, aggregate capacity preflight, per-file integrity verification, resumable staged files, and normalized portable destination-collision rejection.
- Strict framed protocol JSON with bounded frames/depth, invalid-UTF-8 rejection, case-insensitive duplicate-property rejection, truncation handling, cancellation, and idle timeouts.
- Explicit text-snippet transfer and user-triggered clipboard paste. SwiftDrop does not continuously monitor the clipboard.
- Configurable transfer queue/concurrency with privacy-mode redaction and restart-safe metadata-only status persistence. Stale queued/running rows become `Interrupted`; authorization is never replayed automatically.
- Android share-sheet ingestion for text/files and Android foreground data-sync lifetime for active user-initiated transfers.
- Optional Android completion/failure notifications with generic privacy-safe text. They are opt-in, Android 13+ permission is requested only on explicit enable, and denied permission never changes transfer success/failure.
- Windows desktop drag-and-drop for files, folders, text, and SwiftDrop pairing links through the same bounded external-input pipeline.
- `swiftdrop://` pairing protocol activation on Android, iOS, Mac Catalyst, and Windows.
- Local transfer history with retention pruning and per-record deletion.
- Configurable receive folder on Windows through the system folder picker; changing the receive destination restarts the listener safely against the newly resolved root.
- Conservative app-private receive storage on platforms where broad folder access is not implemented.
- Privacy-aware bounded diagnostic events, safe diagnostic export, and synthetic developer self-tests for success, interruption, and checksum mismatch behavior.
- SQLite schema versioning/migrations for metadata-only stores, including trusted peers, history, diagnostics, and privacy-minimal queue metadata.
- Portable TLS loopback tests for certificate pinning, mutual TLS transfer, checksum-verified completion, and resume staging.
- Expanded English/Hindi resource catalogs across primary and secondary XAML surfaces, with CI enforcing catalog well-formedness and key parity.
- MVVM-backed History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About surfaces; Main remains an incremental migration target because it owns complex interactive transfer orchestration.
- A synthetic benchmark harness for SHA-256 throughput, batch-manifest validation, and portable path sanitation without reading user files or contacting peers.

## Security boundaries

SwiftDrop uses established .NET/platform cryptographic primitives rather than custom encryption. Pairing invitations are short-lived and are authorization metadata, not encrypted secrets. Transfer integrity verifies that received bytes match the sender-declared SHA-256 digest; it does **not** prove that a file is safe or malware-free. Incoming files are never automatically opened.

SwiftDrop intentionally rejects public-internet/DNS peer addresses in the current local-network protocol. Supported pairing addresses must be numeric loopback, private, unique-local, or link-local IP addresses. The app does not attempt to bypass firewall, Wi-Fi isolation, operating-system background, or enterprise network policies.

Read `SECURITY.md`, `docs/security/THREAT_MODEL.md`, `docs/protocol/security.md`, and `docs/protocol/wire-format.md` before changing networking, pairing, trust, or transfer behavior.

## Repository

https://github.com/sanskarIN/SwiftDrop

## Support development

If SwiftDrop is useful to you and you want to support continued open-source development, you can support the project here:

https://buymeacoffee.com/sanskarIN

Support is optional and does not unlock transfer features, priority handling, or access to private user data.

## Requirements

- .NET 10 SDK
- .NET MAUI workloads for the target platform
- Android SDK for Android builds
- Xcode on macOS for iOS/Mac Catalyst builds
- Windows 10/11 and Windows App SDK prerequisites for Windows builds

## Build and test

The canonical solution is `SwiftDrop.slnx`.

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

Build a MAUI target after installing its workload, for example:

```bash
dotnet workload install maui-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Debug
```

Run the synthetic benchmark harness with bounded temporary data:

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

GitHub Actions includes portable core/test CI, localization parity validation, benchmark compile validation, CodeQL analysis, and target-platform compile workflows for Android, Windows, Mac Catalyst, and an unsigned iOS Simulator target. Platform signing, store packaging, and physical-device validation remain release steps and are not implied by a successful source compile.

## Pairing

You can pair in several local-network ways:

1. **QR/link:** the receiver creates a short-lived `swiftdrop://pair?...` invitation. The sender scans/opens/pastes it and visually verifies the receiver certificate fingerprint.
2. **Nearby request:** automatic discovery identifies the peer and its advertised certificate fingerprint. The receiver must approve the request.
3. **8-digit code:** the receiver creates a short-lived one-time code. Nearby/manual pairing submits it, the receiver approves, and SwiftDrop binds the returned invitation to the TLS certificate observed during pairing.
4. **Manual local IP:** intended only when automatic discovery is blocked. A fresh 8-digit code is required and the certificate fingerprint is still shown for visual confirmation before transfer.

A transfer invitation is consumed for one transfer attempt. Pause/resume and retry require fresh pairing so authorization is not silently replayed.

The baseline app generates QR pairing codes but does not request camera permission merely to scan them. Users can use a system camera/scanner capable of opening the registered `swiftdrop://pair` URI, or use the link/nearby/manual alternatives.

## Networking notes

SwiftDrop works best when both devices are on the same normal LAN/Wi-Fi. Guest networks, AP/client isolation, multicast filtering, enterprise Wi-Fi policy, mobile OS background restrictions, local-network permissions, and host firewalls can block discovery or inbound connections. QR or manual pairing can help with discovery failures but cannot bypass network policy.

See `docs/troubleshooting.md` and `docs/platform-permissions.md`.

## Local data

SwiftDrop stores metadata only in SQLite: trusted peers, transfer history, bounded diagnostic events, and generic transfer-queue status. Transfer bytes stream directly to the receive destination, with incomplete files staged as `.swiftdrop.part`. Device certificate/private-key material is stored through platform secure storage.

Queue metadata does not contain filenames, text contents, peer IP addresses, pairing invitations/nonces, credentials, or free-form exception messages. See `docs/storage/database-schema.md` and `PRIVACY.md`.

## Development and release

- Build instructions: `BUILDING.md`
- Contribution rules: `CONTRIBUTING.md`
- Architecture: `docs/architecture.md`
- Manual test matrix: `docs/testing/manual-test-matrix.md`
- Performance benchmark guide: `docs/testing/performance-benchmarks.md`
- Release checklist: `docs/release/release-checklist.md`
- Product status: `PROJECT_STATUS.md`
- Next-step roadmap: `NEXT_STEPS.md`
- Detailed implementation ledger: `what_changed.md`
- Third-party notice process: `THIRD_PARTY_NOTICES.md`

## Support and security

- Project/business inquiries: **sanskarin@outlook.in**
- General support: **supportramsandesh@gmail.com**
- Support development: **https://buymeacoffee.com/sanskarIN**
- Security-sensitive reports: follow `SECURITY.md` and use **sanskarin@outlook.in** rather than publishing exploit details or secrets in a public issue.

## License

Apache-2.0. See `LICENSE`.

---

**Made by the Sanskar**

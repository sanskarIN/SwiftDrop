# SwiftDrop

SwiftDrop is an open-source, account-free local-network file and text transfer app built with .NET MAUI and C#. It is designed for direct peer-to-peer transfers across Android, iOS, macOS (Mac Catalyst), and Windows without uploading transfer content to a SwiftDrop cloud service.

> **Privacy model:** transfer payloads stay on the local peer-to-peer path. SwiftDrop stores only local metadata required for settings, trust, history, and privacy-safe diagnostics. See `PRIVACY.md`.

## Current capabilities

- Automatic nearby discovery with mDNS/Bonjour plus bounded UDP broadcast fallback.
- QR/deep-link pairing, nearby pairing requests, manual local-IP fallback, and short-lived one-time 8-digit pairing codes.
- Strict pairing-invitation validation for protocol version, local/private numeric IP address, device metadata bounds, canonical SHA-256 fingerprint, nonce format, and expiry/lifetime.
- Receiver-certificate SHA-256 pinning and sender client certificates over platform/.NET TLS 1.3/1.2.
- Local P-256 ECDSA peer certificates with TLS client/server EKUs, secure-storage persistence, explicit renewal/recovery policy, and user-visible identity refresh when an old certificate cannot be safely reused.
- Explicit receiver approval, sender certificate display, trusted-device storage/revocation, and optional normal-file auto-accept for explicitly trusted certificates.
- Single-file transfer with streaming progress, cancellation, safe pause/resume through fresh pairing, `.swiftdrop.part` staging, SHA-256 verification, free-space checks, manifest-bound source length, and atomic collision-safe destination reservations.
- Multi-file and recursive folder manifests with sender/receiver aggregate limits, receiver accept-all/selective/reject decisions, aggregate capacity preflight, per-file integrity verification, and resumable staged files.
- Explicit text-snippet transfer and user-triggered clipboard paste. SwiftDrop does not continuously monitor the clipboard.
- Configurable transfer queue/concurrency with local queue status and privacy-mode label redaction.
- Android share-sheet ingestion for text/files and Android foreground data-sync lifetime for active user-initiated transfers.
- Windows desktop drag-and-drop for files, folders, text, and SwiftDrop pairing links through the same bounded external-input pipeline.
- `swiftdrop://` pairing protocol activation on Android, iOS, Mac Catalyst, and Windows.
- Local transfer history with retention pruning and per-record deletion.
- Configurable receive folder on Windows through the system folder picker; changing the receive destination restarts the listener safely against the newly resolved root.
- Conservative app-private receive storage on platforms where broad folder access is not implemented.
- Privacy-aware bounded diagnostic events, safe diagnostic export, and synthetic developer self-tests for success, interruption, and checksum mismatch behavior.
- SQLite schema versioning for metadata-only stores.
- Portable TLS loopback tests for certificate pinning, mutual TLS transfer, checksum-verified completion, and resume staging.
- English/Hindi localization resource catalogs, theme controls, larger-interface controls, and accessibility-oriented semantic labels on key surfaces.

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

```bash
dotnet restore SwiftDrop.sln
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

Build a MAUI target after installing its workload, for example:

```bash
dotnet workload install maui-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Debug
```

GitHub Actions includes portable core/test CI, CodeQL analysis, and target-platform compile workflows. Platform signing, store packaging, and physical-device validation remain release steps and are not implied by a successful portable unit-test run.

## Pairing

You can pair in several local-network ways:

1. **QR/link:** the receiver creates a short-lived `swiftdrop://pair?...` invitation. The sender scans/opens/pastes it and visually verifies the receiver certificate fingerprint.
2. **Nearby request:** automatic discovery identifies the peer and its advertised certificate fingerprint. The receiver must approve the request.
3. **8-digit code:** the receiver creates a short-lived one-time code. Nearby/manual pairing submits it, the receiver approves, and SwiftDrop binds the returned invitation to the TLS certificate observed during pairing.
4. **Manual local IP:** intended only when automatic discovery is blocked. A fresh 8-digit code is required and the certificate fingerprint is still shown for visual confirmation before transfer.

A transfer invitation is consumed for one transfer attempt. Pause/resume and retry require fresh pairing so authorization is not silently replayed.

## Networking notes

SwiftDrop works best when both devices are on the same normal LAN/Wi-Fi. Guest networks, AP/client isolation, multicast filtering, enterprise Wi-Fi policy, mobile OS background restrictions, local-network permissions, and host firewalls can block discovery or inbound connections. QR or manual pairing can help with discovery failures but cannot bypass network policy.

See `docs/troubleshooting.md` and `docs/platform-permissions.md`.

## Local data

SwiftDrop stores metadata only in SQLite: trusted peers, transfer history, and bounded diagnostic events. Transfer bytes stream directly to the receive destination, with incomplete files staged as `.swiftdrop.part`. Device certificate/private-key material is stored through platform secure storage.

See `docs/storage/database-schema.md` and `PRIVACY.md`.

## Development and release

- Contribution rules: `CONTRIBUTING.md`
- Architecture: `docs/architecture.md`
- Manual test matrix: `docs/testing/manual-test-matrix.md`
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

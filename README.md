# SwiftDrop

SwiftDrop is an open-source, account-free local-network transfer application built with .NET MAUI and C#. It is designed for direct peer-to-peer transfers between nearby devices without uploading user files to a SwiftDrop cloud service.

> **Privacy model:** local transfer is the product. No account, email, phone number, subscription server, or SwiftDrop cloud upload path is required for the current release.

## Current implementation

- Android, iOS, Mac Catalyst, and Windows .NET MAUI targets.
- Local device identity with a self-signed ECDSA certificate stored through platform secure storage.
- Short-lived QR/deep-link pairing invitations containing the receiver address, port, certificate fingerprint, expiration, and cryptographically random one-time nonce.
- TLS 1.3/1.2 transport through .NET/platform cryptographic primitives; no custom cipher or key-exchange implementation.
- Receiver certificate SHA-256 fingerprint pinning on the sender.
- Sender certificate presentation on the TLS channel and explicit incoming-transfer confirmation showing sender certificate fingerprint.
- User approval before incoming file bytes are accepted.
- Potentially dangerous file-type warnings before acceptance.
- Chunked direct file streaming with cancellation and progress.
- `.swiftdrop.part` staging, resumable offsets, exact-length receive loops, and SHA-256 final integrity verification.
- Root/path traversal protection and filename collision avoidance.
- Per-file safety limit and destination free-space guard.
- Local SQLite transfer-history metadata with privacy mode support.
- Local SQLite trusted-peer persistence primitives.
- Local-network diagnostics and UDP discovery core service.
- Settings and transfer-history pages.
- Unit tests for framing, pairing codec, fingerprints, paths, history, trusted peers, file-risk classification, and settings validation.
- GitHub Actions CI for the portable core and unit tests.

## Repository

https://github.com/sanskarIN/SwiftDrop

## Requirements

- .NET 10 SDK
- .NET MAUI workload (`dotnet workload install maui`)
- Android SDK for Android builds
- Xcode on macOS for iOS/Mac Catalyst builds
- Windows 10/11 plus Windows App SDK prerequisites for Windows builds

## Build

```bash
dotnet restore SwiftDrop.sln
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

For a MAUI target, install the relevant workload and build the target framework. Example for Android:

```bash
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Debug
```

Apple builds require macOS/Xcode. Windows packaging/signing requires the corresponding Windows tooling and release signing configuration.

## Pairing and sending

1. Open SwiftDrop on the receiving device.
2. Create a fresh pairing invitation. A QR image and `swiftdrop://pair?...` link are generated locally.
3. On the sending device, scan/open or paste the invitation.
4. Verify the receiver certificate fingerprint shown to the sender.
5. Choose a file and start the transfer.
6. The receiver sees the sender device name, sender certificate fingerprint, filename, size, and any file-risk warning.
7. The receiver explicitly accepts or rejects the transfer.
8. SwiftDrop streams the file into a staged partial file and finalizes it only after SHA-256 verification succeeds.

Pairing invitations expire quickly and are one-time use. They are temporary capabilities, not long-term passwords. Do not publish them.

## Transfer safety

SwiftDrop does not automatically open received files. Executable, installer, script, macro-enabled, and archive-like extensions can trigger additional warnings, but an extension warning cannot prove that a file is safe. Treat unexpected files as untrusted.

Received paths are constrained beneath the receive root. Existing final names are not silently overwritten; SwiftDrop selects a collision-free destination. Before receiving the remaining payload, SwiftDrop checks available destination capacity with a safety reserve.

## History and privacy mode

Transfer history is local metadata only. It can record direction, peer name, timestamp, size, status, integrity result, and filename. When privacy mode is enabled, newly recorded filenames are replaced with a generic history label. Transfer file contents are never stored in SQLite.

See `PRIVACY.md` for the current privacy behavior.

## Networking notes

SwiftDrop works best when both devices are on the same normal LAN/Wi-Fi network. Guest Wi-Fi, AP/client isolation, enterprise policy, local-network permissions, host firewalls, IPv4 limitations in the UDP fallback, and mobile background restrictions can prevent peer connectivity.

The repository contains a UDP broadcast discovery primitive. QR/deep-link pairing is the dependable fallback when automatic discovery is unavailable or blocked. Platform mDNS/Bonjour integration can be expanded independently without changing the core transfer security model.

See:

- `docs/troubleshooting.md`
- `docs/platform-permissions.md`
- `docs/architecture.md`

## Security

Start with:

- `SECURITY.md`
- `docs/security/THREAT_MODEL.md`
- `docs/protocol/security.md`

Report security issues privately to **sanskarin@outlook.in**. Do not place vulnerabilities, pairing invitations, private certificates, or real transferred content into a public issue.

## Testing

Portable automated tests run from:

```bash
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

Cross-platform release validation requires physical-device/manual testing. Use `docs/testing/manual-test-matrix.md` and `docs/release/release-checklist.md`.

## Project structure

```text
src/SwiftDrop.App/          .NET MAUI UI and platform integration
src/SwiftDrop.Core/         protocol, security, networking, storage, diagnostics
tests/SwiftDrop.Core.Tests/ portable automated tests
docs/                       architecture, security, testing, release guidance
```

## Contributing

Read `CONTRIBUTING.md` and `CODE_OF_CONDUCT.md`. Security-sensitive changes should include tests and should avoid custom cryptography, unbounded input handling, secret logging, or broad platform permissions that are not required by the user-initiated flow.

## Support

- Business/project inquiries: sanskarin@outlook.in
- Support: supportramsandesh@gmail.com
- GitHub profile: https://www.github.com/sanskarIN

## License

Apache-2.0. See `LICENSE`.

---

**Made by the Sanskar**

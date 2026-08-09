# SwiftDrop

SwiftDrop is an open-source, account-free local-network file and text transfer app built with .NET MAUI and C#. It is designed for fast peer-to-peer transfers across Android, iOS, macOS, and Windows without uploading user content to a cloud service.

> **Privacy model:** current releases transfer data directly over the local network. SwiftDrop does not require an account and does not provide a SwiftDrop cloud upload path.

## Highlights

- Local discovery through mDNS/Bonjour and UDP broadcast fallback.
- Secure device pairing using QR/deep-link payloads or a one-time code.
- TLS 1.3/1.2 secure channels using platform cryptographic primitives; no custom cipher design.
- Certificate fingerprint pinning for trusted devices and explicit trust revocation.
- Files, folders, and text transfer with queues, progress, pause, resume, cancel, retry, and collision policies.
- Chunked streaming with SHA-256 integrity checks and resumable `.swiftdrop.part` files.
- Safe receive paths with traversal protection, metadata validation, free-space checks, size limits, backpressure, timeouts, cancellation, and protocol version negotiation.
- SQLite metadata only. User files are streamed directly to their chosen destination.
- Diagnostics designed to avoid logging transferred content or secrets.

## Repository

https://github.com/sanskarIN/SwiftDrop

## Requirements

- .NET 10 SDK
- .NET MAUI workload (`dotnet workload install maui`)
- Android SDK for Android builds
- Xcode on macOS for iOS/macOS builds
- Windows 10/11 + Windows App SDK prerequisites for Windows builds

## Build

```bash
dotnet restore SwiftDrop.sln
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

For a MAUI target, install the relevant workload and build the target framework, for example:

```bash
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Debug
```

## Pairing

1. Open **Devices** on the receiving device and create a pairing code.
2. On the other device, scan/open the generated `swiftdrop://pair?...` QR link or paste the link/code manually.
3. Compare the displayed certificate fingerprint on both devices.
4. Confirm trust on both devices.
5. Future sessions validate the pinned peer certificate fingerprint.

Pairing data expires quickly and is not a long-term authentication secret.

## Networking notes

SwiftDrop works best when both devices are on the same normal LAN/Wi-Fi network. Guest networks, AP/client isolation, enterprise Wi-Fi policy, mobile OS background restrictions, local-network permissions, and host firewalls can block peer discovery or inbound connections. See `docs/troubleshooting.md` and `docs/platform-permissions.md`.

## Security

Read `SECURITY.md` and `docs/protocol/security.md`. Please report security issues privately to **sanskarin@outlook.in** rather than publishing exploit details in a public issue.

## Support

- Business/project inquiries: sanskarin@outlook.in
- Support: supportramsandesh@gmail.com

## License

Apache-2.0. See `LICENSE`.

---

**Made by the Sanskar**

# What changed

Date: 2026-08-09
Repository: https://github.com/sanskarIN/SwiftDrop

## Implementation

- Built a complete .NET MAUI solution layout with a reusable `SwiftDrop.Core` library, `SwiftDrop.App`, tests, documentation, CI, contribution files, and platform metadata.
- Implemented local TLS client/server networking with receiver certificate SHA-256 fingerprint pinning.
- Implemented short-lived pairing links, QR generation, cryptographically random one-time pairing nonces, expiration checks, and nonce replay prevention.
- Implemented file selection, local receive server startup, direct streaming, progress, resumable partial files, SHA-256 final integrity verification, path traversal protection, size/header limits, and safe partial-file handling.
- Implemented reusable UDP LAN discovery helper and SQLite trusted-peer metadata store.
- Added Android, iOS, Mac Catalyst, and Windows platform declarations, URL scheme handling metadata, local-network descriptions, and Windows private-network capability.
- Added app icon/splash SVG assets and the visible `Made by the Sanskar` attribution.
- Added README, security policy, architecture/protocol/platform/troubleshooting documentation, changelog, contribution guide, issue templates, and Apache-2.0 licensing.
- Added unit tests for pairing expiration/round-trip, path traversal rejection, certificate fingerprint comparison, and framed protocol serialization.
- Added GitHub Actions CI for restore, core build, and unit tests on .NET 10.

## Security and privacy decisions

- No custom cryptography was created. SwiftDrop uses platform/.NET TLS and SHA-256 primitives.
- Transfer content is not uploaded to a SwiftDrop cloud service.
- Device certificate material is stored through MAUI `SecureStorage`.
- Incoming transfers require a one-time pairing nonce created by the receiver.
- Received network paths are constrained beneath the receive root.
- Files are written as `.swiftdrop.part` until their SHA-256 digest succeeds.
- Protocol metadata frames are length-bounded before allocation.

## Known environment/repository notes

- The repository initially contained only the Apache-2.0 `LICENSE` file.
- The file-search attachment source was unavailable in this chat session, so the uploaded master-prompt attachment itself could not be re-opened by the file tool. The implementation was completed from the SwiftDrop project context available in the conversation and repository.
- The GitHub connector supports commit messages but does not expose an author/committer email field for commit creation. The requested email `sanskarin@outlook.in` is therefore included as a `Signed-off-by` trailer in the project commit message rather than being forcibly set as Git commit metadata.
- Platform UI builds require their corresponding .NET MAUI workloads/SDKs and signing configuration. CI intentionally validates the portable core and unit tests without requiring Android/Xcode/Windows signing environments.

## Next maintenance checks

- Review the first GitHub Actions run after the commit and adjust any package/workload version if the runner image changes.
- Validate Android/iOS/Windows firewall and local-network permission behavior on physical devices before publishing store packages.
- Add persistent trusted-device UX before allowing transfers without a fresh pairing invitation.

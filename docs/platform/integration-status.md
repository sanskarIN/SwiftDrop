# Platform Integration Status

This document describes source-level integration currently present in SwiftDrop. It does not replace physical-device validation or store-signing requirements.

## Android

Implemented in source:

- Local network TCP/TLS and mDNS/UDP discovery permissions.
- `swiftdrop://pair` protocol activation.
- Inbound Android share intents for text, one file, or multiple files.
- Shared content is copied into bounded app-cache staging before SwiftDrop presents it for transfer.
- Foreground data-sync service for active user-initiated queued transfers.
- Ongoing low-priority transfer notification while the foreground service is active.
- No broad storage permission is requested for ordinary picker/share workflows.

Validation still required:

- Android versions across the supported API range.
- Notification/foreground-service behavior under current Android restrictions.
- Vendor battery-management behavior.
- Local-network multicast behavior on physical Wi-Fi networks.
- Very large files and low-storage behavior.

## iOS

Implemented in source:

- Local-network usage description.
- Bonjour service declarations for SwiftDrop discovery.
- `swiftdrop://pair` protocol URL handling.
- System file picker for user-initiated outbound file selection.

Conservative current boundary:

- SwiftDrop does not claim arbitrary long-running background socket transfer on iOS.
- A dedicated iOS Share Extension target for inbound files/text is not yet shipped in the repository. The app can still receive pairing protocol links and use normal in-app file/text selection.
- Background behavior must respect iOS lifecycle and energy policies.

Validation still required:

- Local-network permission prompt behavior.
- Bonjour discovery on physical devices.
- URL activation across cold/warm starts.
- Transfer interruption when app lifecycle state changes.

## macOS / Mac Catalyst

Implemented in source:

- Local-network usage description.
- Bonjour service declarations.
- `swiftdrop://pair` URL handling.
- Standard MAUI file selection and direct local transfer path.

Conservative current boundary:

- Desktop drag-and-drop is not yet implemented as a first-class SwiftDrop transfer surface.
- A dedicated inbound share extension is not included.

Validation still required:

- macOS firewall prompts and inbound server behavior.
- Bonjour discovery across current macOS versions.
- File picker/sandbox behavior under release entitlements.

## Windows

Implemented in source:

- Private-network client/server capability.
- `swiftdrop` protocol registration and activation routing.
- Native system FolderPicker integration for choosing a receive or folder-transfer location.
- Direct local transfer over the same TLS protocol as other platforms.

Validation still required:

- Packaged and unpackaged activation behavior.
- Windows Firewall prompts/rules.
- FolderPicker access after packaging/signing.
- IPv4/IPv6 LAN combinations.

## Cross-platform behavior

Implemented consistently in shared code:

- Account-free local-first data path.
- Receiver certificate pinning and sender client certificate presentation.
- One-time pairing authorization.
- Trusted-device metadata stored locally.
- Single/multi-file transfer, selective receive, collision handling, resumable partial files, SHA-256 verification, and resource bounds.
- Text transfer and explicit clipboard access.
- Local history/settings/diagnostics metadata only.

## Release rule

A platform is not release-validated merely because shared core tests pass. A production release must also pass the platform build workflow and the physical/manual test matrix in `docs/testing/manual-test-matrix.md` and `docs/release/release-checklist.md`.

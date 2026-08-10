# Platform Integration Status

Updated: 2026-08-10

This document describes source-level integration currently present in SwiftDrop. It does not replace target-platform compilation, signed packaging, physical-device validation, or store-policy review.

## Android

Implemented in source:

- Local-network TCP/TLS plus mDNS/UDP discovery permissions.
- Reference-counted Wi-Fi multicast lock for mDNS operation.
- `swiftdrop://pair` protocol activation.
- Inbound Android share intents for text, one file, or multiple files.
- Shared content copied into bounded app-cache staging before SwiftDrop presents it for review/transfer.
- Foreground data-sync service for active user-initiated queued transfers.
- Ongoing generic low-priority foreground transfer notification.
- Optional generic completion/failure notifications where enabled/supported.
- No broad storage permission for normal picker/share workflows.
- Android application backup explicitly disabled so app-local SwiftDrop metadata is not opted into Android app backup/restore.
- Cleartext transport disabled.

Validation still required:

- Supported Android API range on physical devices.
- Current foreground-service and notification-policy behavior.
- Vendor battery-management restrictions.
- Notification permission deny/allow transitions.
- Local-network multicast behavior on real Wi-Fi.
- Very large files, many-file batches, storage pressure, sleep/lock, and network changes.

## iOS

Implemented in source:

- Local-network usage description.
- Bonjour service declaration for `_swiftdrop._tcp`.
- `swiftdrop://pair` URL handling.
- Standard system file picker for outbound user-selected files.
- Document/open-file declaration for `public.data`.
- Incoming file URLs are copied under temporary security-scoped access into bounded SwiftDrop cache staging through the shared `ExternalFileStager` before they enter the normal review/send workflow.
- Staging uses safe filenames, exact declared length, size limits, cancellation, and cleanup on failure.

Conservative current boundary:

- SwiftDrop does not claim arbitrary long-running background socket transfer on iOS.
- A dedicated first-class iOS Share Extension target for arbitrary inbound files/text is still not included.
- Opening a document/file URL is implemented; that is not presented as equivalent to a Share Extension.
- Background behavior must respect iOS lifecycle/energy/store policy.

Validation still required:

- Local-network permission prompt denied/allowed behavior.
- Bonjour discovery on physical devices.
- URL/document activation across cold/warm starts.
- Security-scoped file access under signed sandboxed builds.
- Transfer interruption during lifecycle/sleep/lock changes.
- Real file-provider/iCloud Drive/third-party document-provider cases.

## macOS / Mac Catalyst

Implemented in source:

- Local-network usage description.
- Bonjour service declaration.
- `swiftdrop://pair` URL handling.
- Standard MAUI file selection/direct local transfer.
- Document/open-file declaration for `public.data`.
- Incoming file URLs use the same bounded security-scoped staging path as iOS.
- Explicit Mac Catalyst app-sandbox entitlements with network client and network server capabilities.

Conservative current boundary:

- A first-class native Mac Catalyst file/folder/text drag-and-drop surface is still not implemented.
- A dedicated inbound Apple Share Extension target is not included.
- Security-scoped access is intentionally held only for staging; SwiftDrop does not retain broad file-provider access.

Validation still required:

- macOS firewall prompts/inbound server behavior.
- Bonjour discovery across supported macOS versions.
- Signed sandbox behavior using the committed entitlements.
- Document/file URL activation and security-scoped staging under release signing/notarization.
- File-provider behavior and large-file staging.

## Windows

Implemented in source:

- `privateNetworkClientServer` package capability.
- No general `internetClient` capability in the current package manifest; protocol v1 rejects public/DNS peer destinations.
- `swiftdrop` protocol registration and activation routing.
- Native system FolderPicker integration for choosing a receive/folder-transfer location.
- WinUI desktop drag-and-drop for files, folders, text, and SwiftDrop pairing links through the bounded `ExternalInputInbox` pipeline.
- Direct local transfer over the same pinned mutual-TLS protocol as other platforms.

Validation still required:

- Packaged/unpackaged activation behavior.
- Windows Firewall prompts/rules and private/public network profiles.
- FolderPicker access persistence after packaging/signing/update.
- Packaged native drag-and-drop.
- IPv4/IPv6 LAN combinations.
- Signed MSIX clean install/update/uninstall.

## Cross-platform behavior

Implemented consistently in shared/source code:

- Account-free local-first transfer path with no SwiftDrop-operated cloud relay.
- Receiver certificate pinning and sender client-certificate presentation.
- Strict pairing invitation validation including strict decoded JSON duplicate-property rejection.
- Bounded exact-precision one-time pairing authorization with atomic consume/replay rejection.
- Trusted-device metadata stored locally with canonical SHA-256 fingerprint enforcement at the storage boundary.
- Single/multi-file transfer, selective receive, collision handling, resumable partial files, SHA-256 verification, and resource limits.
- Shared request/response protocol validation policies for request envelope/identity/transfer IDs/batch order and sender acknowledgement lengths/offsets.
- Text transfer and explicit clipboard access only.
- Local history/settings/diagnostics metadata only.
- Privacy mode hides peer/file history labels and structurally redacts sensitive diagnostic identifiers.
- Restart-safe queue metadata does not persist source paths, text contents, pairing authorization, peer addresses, credentials, or free-form exception messages.
- English/Hindi resource catalogs cover primary/secondary XAML plus runtime dialog/status/consent/history/platform surfaces, with key and format-placeholder parity checked in CI.
- Main, History, Queue, Devices, Trusted Devices, Diagnostics, Settings, and About presentation state use dedicated view models where appropriate; platform pickers/dialogs/navigation remain page-owned.

## External platform gates

The following remain external release requirements, not source-edit tasks:

- Green platform compile/test workflows for the exact release candidate.
- Physical Android/iOS/macOS/Windows directional transfer matrix.
- Real firewall/guest-Wi-Fi/client-isolation/multicast-blocked behavior.
- Sleep/lock/background/storage/network-change behavior.
- Accessibility testing with TalkBack, VoiceOver, Narrator, keyboard-only navigation, large text, high contrast, and reduced motion.
- Production signing/provisioning/notarization/store packaging.
- Store privacy declarations/screenshots/metadata against final signed binaries.

## Release rule

A platform is not release-validated merely because source exists or shared Core tests are configured. A production release must pass the relevant platform build workflow plus the physical/manual matrix in `docs/testing/manual-test-matrix.md` and `docs/release/release-checklist.md`.

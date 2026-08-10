# Platform Integration Status

This document describes source-level integration currently present in SwiftDrop. It does not replace signed package, store, or physical-device validation.

## Android

Implemented in source:

- Local TCP/TLS networking and mDNS/UDP discovery permissions.
- Reference-counted Wi-Fi multicast lock for mDNS.
- `swiftdrop://pair` protocol activation.
- `ACTION_SEND` / `ACTION_SEND_MULTIPLE` for text/files.
- Content-URI staging with item/file limits, portable filename sanitation, provider-length checks where available, runtime byte limits when unavailable, capacity preflight, exact staging length checks, and cleanup on failure.
- Atomic text+file handoff into the common review inbox.
- Foreground data-sync service for active user-initiated queued transfers.
- Generic foreground transfer notification required by the platform lifecycle path.
- Optional generic completion/failure notifications where supported/configured.
- No broad legacy storage permission for ordinary picker/share flows.
- Application backup disabled for SwiftDrop app-local metadata.

Validation still required:

- Supported Android API range on physical devices.
- Provider URIs with known/unknown size.
- Foreground-service and notification behavior under current Android restrictions.
- Vendor battery-management behavior.
- Local-network multicast behavior on real Wi-Fi.
- Large-file/low-storage/network-change behavior.

## iOS

Implemented in source:

- Local-network usage description.
- Bonjour service declaration.
- `swiftdrop://pair` activation.
- Normal system file selection/document URL intake.
- Containing-app App Group entitlement:
  `group.in.sanskar.swiftdrop`.
- Dedicated `SwiftDrop.ShareExtension` target for files/images/movies/text/web URLs.
- Share Extension App Group entitlement matching the containing app.
- Strict versioned App Group package manifest validated by `SwiftDrop.Core`.
- Atomic extension publication from `.staging-*` to `pending-*`.
- Containing-app import of pending packages on launch/foreground activation.
- Stale extension staging cleanup.
- Strict JSON/unknown-field/package-age/path/item-size/aggregate-size validation.
- Rejection of symlink/reparse package/files where represented by the filesystem.
- Re-staging accepted package files into the app's normal bounded cache before review.
- No automatic send after extension import.

Conservative lifecycle boundary:

- SwiftDrop does not claim arbitrary long-running background TCP sockets survive iOS suspension.
- Share Extension work is bounded and completes into App Group handoff rather than attempting a background transfer from the extension.

Validation still required:

- Apple Developer App Group capability and provisioning profiles for app + extension.
- Signed physical-device Share Extension appearance/activation behavior.
- Local-network permission prompt behavior.
- Bonjour discovery on physical devices.
- URL activation across cold/warm starts.
- App Group handoff across cold/warm main-app activation.
- Transfer interruption during foreground/background/sleep transitions.
- TestFlight/App Store package embedding of the extension.

## macOS / Mac Catalyst

Implemented in source:

- Local-network usage description.
- Bonjour service declaration.
- `swiftdrop://pair` activation.
- MAUI file selection/document URL intake.
- App sandbox entitlement.
- Network client/server entitlements for local LAN transport.
- Same App Group entitlement as the Apple Share Extension.
- Dedicated Share Extension target for bounded file/text/URL handoff.
- Native `UIDropInteraction` on the main MAUI surface.
- Finder files/folders, text, and pairing-link drop support.
- Temporary security-scoped access during native staging.
- Symlink/reparse rejection for dropped files/folders.
- Per-file/count/aggregate bounds and storage preflight.
- Portable name sanitation plus collision deconfliction for staged files/directories.
- Common review-inbox handoff; no automatic transfer.

Validation still required:

- Signed sandbox/App Group entitlement acceptance.
- Mac firewall prompts and inbound server behavior.
- Bonjour discovery across supported macOS versions.
- Share Extension embedding/activation under release signing.
- Finder file/folder drops under release sandbox.
- Security-scoped URL behavior for external volumes/providers.
- App notarization/store packaging.

## Windows

Implemented in source:

- Private-network client/server capability.
- No general Internet-client capability for protocol-v1 local-only transfer.
- `swiftdrop` protocol registration and activation routing.
- Native system FolderPicker for receive/folder-transfer location.
- Native files/folders/text/pair-link drag-and-drop.
- Atomic external-input handoff for dropped content.
- Direct local TLS transfer using the same Core protocol as other platforms.

Validation still required:

- Signed/package install and update behavior.
- Windows Firewall prompts/rules.
- FolderPicker persistence after packaging/signing.
- Protocol registration across install/update.
- Drag/drop under packaged release runtime.
- IPv4/IPv6 LAN combinations.

## Cross-platform shared behavior

Implemented consistently in shared code/services:

- Account-free local-first path.
- Receiver certificate pinning and sender client certificate.
- Strict typed protocol requests/acks.
- Case-insensitive duplicate JSON member rejection.
- Unknown JSON member rejection.
- Type-specific request shape validation.
- One-time transfer authorization after authenticated client-certificate presence.
- Certificate-bound trusted-device metadata.
- Single/multi/folder/text transfer.
- Selective receive.
- Collision handling and non-overwrite final promotion.
- Receive-root path confinement including portable rooted/traversal syntax and existing reparse/symlink component rejection.
- `.swiftdrop.part` resume.
- Stable batch IDs across pause/failure/retry.
- Schema-v3 verified completed-file reuse for idempotent interrupted-batch resume.
- SHA-256 integrity verification.
- Queue/history/diagnostics/resume metadata only; transfer contents excluded from SQLite.
- UTF-8-byte-bounded external text intake.

## Source-complete vs release-validated

The current master-prompt source scope includes the previously missing Apple Share Extension and Mac Catalyst native drop implementations. That means those items are **implemented in source**, not yet **release-validated**.

A platform is release-validated only after:

1. the exact candidate commit passes configured automated jobs;
2. release workloads compile the app and any extension;
3. real signing/provisioning/package identity succeeds;
4. signed package install/upgrade works;
5. physical-device/network/transfer/resume/accessibility validation passes;
6. privacy/store declarations match the shipped binary.

See `NEXT_STEPS.md`, `docs/testing/manual-test-matrix.md`, and `docs/release/release-checklist.md`.

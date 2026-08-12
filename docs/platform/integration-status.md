# Platform Integration Status

Updated: 2026-08-12

This document describes source-level integration currently present in SwiftDrop. It does not replace signed package, store, provider, network, or physical-device validation.

## Android

Implemented in source:

- Local TCP/TLS networking and mDNS/UDP discovery permissions.
- Reference-counted Wi-Fi multicast lock for mDNS.
- `swiftdrop://pair` protocol activation.
- `ACTION_SEND` / `ACTION_SEND_MULTIPLE` for text/files.
- Content-URI staging with shared file-count/per-file/aggregate `TransferStagingBudget`.
- Provider declared-size validation where available.
- Negative provider size treated as unknown rather than trusted size metadata.
- Unknown-length content bounded by the remaining aggregate staging budget.
- Repeated free-space-reserve checks while an unknown-length provider is being copied.
- Portable filename sanitation with both character and UTF-8-byte bounds.
- Exact declared/staged length checks and partial cleanup on failure.
- Failed staging does not commit file/byte budget.
- Atomic text+file handoff into the common review inbox.
- Foreground data-sync service for active user-initiated queued transfers.
- Generic foreground transfer notification required by the platform lifecycle path.
- Optional generic completion/failure notifications where supported/configured.
- No broad legacy storage permission for ordinary picker/share flows.
- Application backup disabled for SwiftDrop app-local metadata.

Validation still required:

- Supported Android API range on physical devices.
- Providers with declared, null, negative, incorrect, and changing size metadata.
- Unknown-length providers under low-storage pressure.
- Foreground-service and notification behavior under current Android/Play restrictions.
- Vendor battery-management behavior.
- Local-network multicast behavior on real Wi-Fi.
- Large-file/low-storage/network-change behavior.
- TalkBack, large text, Hindi layout, and lifecycle validation.

## iOS

Implemented in source:

- Local-network usage description.
- Bonjour service declaration.
- Strict canonical `swiftdrop://pair` activation/decoding.
- Normal system file selection/document URL intake.
- Containing-app App Group entitlement:
  `group.in.sanskar.swiftdrop`.
- Dedicated `SwiftDrop.ShareExtension` target for files/images/movies/text/web URLs.
- Share Extension App Group entitlement matching the containing app.
- Strict versioned App Group package manifest validated by `SwiftDrop.Core`.
- Shared count/per-file/aggregate staging-budget policy.
- Provider-response timeout and extension-lifetime cancellation.
- Late cancelled/timed-out provider callbacks cannot start a fresh staging copy.
- A provider that responds before the timeout may continue a legitimate local copy beyond the response timeout; the response timer is not misused as a file-copy timeout.
- Aggregate budget is checked before copying the file that would exceed package limits.
- Portable filename sanitation with character/UTF-8 bounds and bounded collision markers.
- Atomic extension publication from `.staging-*` to `pending-*`.
- Containing-app import of pending packages on launch/foreground activation.
- Serialized containing-app package import.
- Stale extension staging cleanup.
- Strict JSON/unknown-field/package-age/path/item-size/aggregate-size validation.
- Rejection of symlink/reparse package/files where represented by the filesystem.
- Exact physical file-set validation: undeclared extra files/nested directories are rejected.
- Aggregate app-cache capacity preflight before validated package files are recopied.
- Re-staging accepted package files into the app's normal bounded cache before review.
- One pending package is surfaced for review at a time; later pending packages are not silently merged/deleted.
- No automatic send after extension import.

Conservative lifecycle boundary:

- SwiftDrop does not claim arbitrary long-running background TCP sockets survive iOS suspension.
- Share Extension work is bounded and completes into App Group handoff rather than attempting a background transfer from the extension.

Validation still required:

- Apple Developer App Group capability and provisioning profiles for app + extension.
- Signed physical-device Share Extension appearance/activation behavior.
- `NSItemProvider` response/cancellation behavior with real providers.
- Local-network permission prompt behavior.
- Bonjour discovery on physical devices.
- URL activation across cold/warm starts.
- App Group handoff across cold/warm main-app activation.
- Low-storage App Group→cache import behavior.
- Transfer interruption during foreground/background/sleep transitions.
- TestFlight/App Store package embedding of the extension.
- VoiceOver, large text, Hindi wrapping, and accessibility behavior.

## macOS / Mac Catalyst

Implemented in source:

- Local-network usage description.
- Bonjour service declaration.
- Strict canonical `swiftdrop://pair` activation.
- MAUI file selection/document URL intake.
- App sandbox entitlement.
- Network client/server entitlements for local LAN transport.
- Same App Group entitlement as the Apple Share Extension.
- Dedicated Share Extension target for bounded file/text/URL handoff.
- Native `UIDropInteraction` on the main MAUI surface.
- Finder files/folders, text, and pairing-link drop support.
- Temporary security-scoped access during native staging.
- Shared count/per-file/aggregate staging-budget policy.
- Bounded provider-response waits for native-drop file/text providers.
- Provider-response timeout does not terminate an already-started local copy.
- Symlink/reparse rejection for dropped files/folders.
- Portable filename sanitation plus bounded collision deconfliction for staged files/directories.
- Common review-inbox handoff; no automatic transfer.

Validation still required:

- Signed sandbox/App Group entitlement acceptance.
- Mac firewall prompts and inbound server behavior.
- Bonjour discovery across supported macOS versions.
- Share Extension embedding/activation under release signing.
- Finder file/folder drops under release sandbox.
- Real provider response timeout behavior.
- Security-scoped URL behavior for external volumes/providers.
- App notarization/store packaging.
- VoiceOver, keyboard-only, large-text, high-contrast, and Hindi layout validation.

## Windows

Implemented in source:

- Private-network client/server capability.
- No general Internet-client capability for protocol-v1 local-only transfer.
- `swiftdrop` protocol registration and activation routing.
- Native system FolderPicker for receive/folder-transfer location.
- Native files/folders/text/pair-link drag-and-drop.
- Atomic external-input handoff for dropped content.
- Direct local TLS transfer using the same Core protocol as other platforms.
- Sender folder manifests are canonical `/` protocol paths even though local Windows paths use `\\`.
- Direct selected/dropped file/folder sources still pass shared regular-source/link-safe source construction before send.

Validation still required:

- Signed/package install and update behavior.
- Windows Firewall prompts/rules.
- FolderPicker persistence after packaging/signing.
- Protocol registration across install/update.
- Drag/drop under packaged release runtime.
- Windows→Android/iOS/Mac folder interoperability using exact canonical relative paths.
- IPv4/IPv6 LAN combinations.
- Narrator, keyboard-only, high-DPI, high-contrast, and large-text behavior.

## Cross-platform shared behavior

Implemented consistently in shared code/services:

- Account-free local-first path.
- Receiver certificate pinning and sender client certificate.
- Strict typed protocol requests/acknowledgements.
- Case-insensitive duplicate JSON member rejection.
- Unknown JSON member rejection.
- Type-specific request shape validation.
- Canonical raw Base64URL pairing capability representation with no whitespace/query aliases.
- One-time transfer authorization after strict request/manifest validation and authenticated client-certificate presence.
- Certificate-bound trusted-device metadata.
- Single/multi/folder/text transfer.
- Selective receive.
- Canonical `/` manifest relative paths across every sender OS.
- Rooted/traversal/empty-segment/backslash/noncanonical manifest rejection before authorization.
- Maximum 64 relative-path segments and bounded manifest path metadata.
- Filename segments bounded by UTF-16 length and UTF-8 bytes.
- Bounded collision markers that remain distinct at maximum filename size.
- Outgoing single-file source revalidation at stream open.
- Bounded deterministic folder enumeration with source symlink/reparse rejection.
- Portable case/Unicode/sanitation collision deconfliction before hashing.
- Stable batch transfer-ID token syntax and stable ID across pause/failure retry.
- Removed obsolete implicit fresh-ID batch compatibility overload.
- Collision handling and non-overwrite final promotion.
- Receive-root path confinement including existing reparse/symlink component rejection.
- `.swiftdrop.part` resume.
- Schema-v3 verified completed-file reuse for idempotent interrupted-batch resume.
- Completed-file verification while building retry plan **and again immediately before zero-byte item completion ACK**.
- SHA-256 integrity verification.
- Queue/history/diagnostics/resume metadata only; transfer contents excluded from SQLite.
- UTF-8-byte-bounded external text intake.
- Shared external staging budget used by Android share, Apple Share Extension, and Mac native drop.

## Source-complete vs release-validated

The current master-prompt source scope includes the Apple Share Extension, Mac Catalyst native drop, stable batch resume, canonical cross-platform manifest paths, source-link safety, strict pairing representation, and external staging-budget controls. Those items are **implemented in source**, not yet **release-validated**.

A platform is release-validated only after:

1. the exact candidate commit passes configured automated jobs;
2. release workloads compile the app and any extension;
3. real signing/provisioning/package identity succeeds;
4. signed package install/upgrade works;
5. provider/App Group/ContentResolver behavior works under real platform conditions;
6. physical-device/network/transfer/resume/low-storage/accessibility validation passes;
7. privacy/store declarations match the shipped binary.

See `NEXT_STEPS.md`, `docs/testing/security-test-plan.md`, `docs/testing/manual-test-matrix.md`, and `docs/release/release-checklist.md`.

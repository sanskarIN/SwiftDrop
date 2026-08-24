# Platform Integration Status

Updated: 2026-08-20

This document describes source-level integration currently present in SwiftDrop. It does not replace signed package, store/distribution, provider, network, notification, or physical-device/desktop validation.

SwiftDrop maintains two UI/platform hosts over the same protocol/security Core:

- `SwiftDrop.App`: .NET MAUI for Android, iOS, Mac Catalyst, and Windows;
- `SwiftDrop.Desktop`: Avalonia desktop host for Linux (`linux-x64` and `linux-arm64` maintained package RIDs).

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
- Hosted Release compilation is part of the maintained platform gate.

Validation still required:

- Supported Android API range on physical devices.
- Providers with declared, null, negative, incorrect, and changing size metadata.
- Unknown-length providers under low-storage pressure.
- Foreground-service and notification behavior under current Android/Play restrictions.
- Android 13+ optional notification permission deny/allow transitions.
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
- Dedicated **iOS-only** `SwiftDrop.ShareExtension` target for files/images/movies/text/web URLs.
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
- Optional generic local completion/failure notifications through `UNUserNotificationCenter`.
- Alert/sound authorization is requested only when the user explicitly enables the notification preference.
- A strong notification-center delegate is retained so enabled generic terminal notifications can be presented while SwiftDrop is foregrounded.
- Notification messages are localized and contain no transfer-specific format placeholders.
- No remote-push token/service is required by the terminal notification feature.
- Hosted iOS simulator restore/build is configured to be certificate-independent at CI command scope while real project entitlements remain present for signed/device builds.

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
- Local notification authorization deny/allow, foreground/background presentation, Settings toggle persistence, and generic notification text on signed physical devices.
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
- App Group entitlement shared with the Apple handoff model where configured.
- Native `UIDropInteraction` on the main MAUI surface.
- Finder files/folders, text, and pairing-link drop support.
- Temporary security-scoped access during native staging.
- Shared count/per-file/aggregate staging-budget policy.
- Bounded provider-response waits for native-drop file/text providers.
- Provider-response timeout does not terminate an already-started local copy.
- Symlink/reparse rejection for dropped files/folders.
- Portable filename sanitation plus bounded collision deconfliction for staged files/directories.
- Common review-inbox handoff; no automatic transfer.
- Optional generic local completion/failure notifications through the Apple User Notifications framework.
- Alert/sound authorization is requested only after explicit user opt-in; generic messages do not include filename/peer/path/transfer content.
- Foreground notification presentation is enabled through the retained notification-center delegate.
- Mac Catalyst uses the containing desktop app/native-drop path; there is **no Mac Catalyst Share Extension target** in the maintained source tree.
- Hosted Mac Catalyst Release compilation is part of the maintained Apple platform gate.

Validation still required:

- Signed sandbox/App Group entitlement acceptance where used by the containing app.
- Mac firewall prompts and inbound server behavior.
- Bonjour discovery across supported macOS versions.
- Finder file/folder drops under release sandbox.
- Real provider response timeout behavior.
- Security-scoped URL behavior for external volumes/providers.
- Local notification authorization/presentation under the signed Mac Catalyst sandbox and system notification settings.
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
- Optional generic completion/failure app notifications using Windows App SDK `AppNotificationManager` / `AppNotificationBuilder`.
- Notification manager registration is lazy and failure-isolated from transfer results.
- Packaged notification activation uses matching `windows.toastNotificationActivation` and `windows.comServer` manifest registrations with a single fixed CLSID and the Windows App SDK activation argument contract.
- Notification activation is informational and carries no transfer identifiers or file/content data.
- A portable validator checks the packaged notification CLSID pairing, activation arguments, local-only capability posture, notification source contract, and placeholder-free English/Hindi terminal messages.
- Focused CI target-matrix controls prevent the Windows compile job from traversing unrelated Android/iOS/Mac Catalyst workloads.
- WinUI launch/drag event types are explicitly qualified to avoid MAUI/WinUI/legacy Windows namespace ambiguity.

Validation still required:

- Signed/package install and update behavior.
- Windows Firewall prompts/rules.
- FolderPicker persistence after packaging/signing.
- Protocol registration across install/update.
- App-notification registration/activation across signed install/update and Windows notification-settings deny/allow behavior.
- Verify generic terminal notifications do not expose transfer-specific content in the final package/runtime.
- Drag/drop under packaged release runtime.
- Windows→Android/iOS/Mac/Linux folder interoperability using exact canonical relative paths.
- IPv4/IPv6 LAN combinations within the shared transport's currently supported behavior.
- Narrator, keyboard-only, high-DPI, high-contrast, and large-text behavior.

## Linux desktop

Implemented in source:

- Dedicated Avalonia `SwiftDrop.Desktop` application targeting .NET 10.
- Maintained release RIDs: `linux-x64` and `linux-arm64`.
- Direct project reference to `SwiftDrop.Core` rather than a Linux-specific protocol fork.
- mDNS/DNS-SD discovery plus bounded UDP fallback through shared Core discovery services.
- Nearby certificate-pinned pairing.
- Strict canonical `swiftdrop://pair` decoding through shared `PairingCodec`.
- Manual numeric local-IP pairing with an eight-digit, short-lived one-time pairing code.
- Local device P-256 certificate identity with Core certificate usability/renewal policy.
- XDG-aware config/data/cache directories.
- Best-effort current-user Unix permission restriction for Linux identity/settings/SQLite files.
- Native Avalonia file/folder pickers for explicit sends and receive-location selection.
- Single-file send with SHA-256 manifest and receiver resume offset.
- Multiple-file/folder batch send using the shared deterministic, link-safe batch source builder.
- Text-snippet send.
- Explicit incoming file approval/rejection.
- Explicit incoming batch approval/rejection.
- Explicit incoming text approval/rejection; clipboard copy only after acceptance.
- Explicit incoming nearby-pairing approval.
- Shared strict typed request validation and one-time authorization consumption.
- Shared storage-capacity preflight, path confinement, collision-safe destination reservation, `.swiftdrop.part` staging, SHA-256 verification, and non-overwrite promotion.
- Metadata-only completed-batch resume persistence using the shared SQLite completion store.
- Completed-file verification during retry planning and immediate re-verification before zero-byte completion ACK.
- Cancellation and file/batch progress presentation.
- Freedesktop `.desktop` launcher with `x-scheme-handler/swiftdrop` registration.
- Desktop launch arguments restricted to `swiftdrop://pair` and passed unchanged into the strict canonical Core decoder.
- Self-contained x64/ARM64 archive generation through `scripts/publish-linux.sh`.
- User-local installer that uses an absolute installed executable path for desktop activation and an optional `$HOME/.local/bin/swiftdrop` convenience symlink.
- Dedicated integration validator covering solution/project/Core reference/RIDs/launcher/protocol-handler/package/workflow contracts.
- Dedicated Ubuntu workflow configured to build/package both Linux RIDs, validate direct/transitive vulnerability evidence, create deterministic evidence manifests, and upload archives/evidence.

Current intentional parity boundaries:

- Linux does not use the MAUI UI shell; MAUI-specific History/Queue/Trusted Devices/Diagnostics/Settings dashboards are not claimed as one-for-one Linux UI parity in this host.
- Native optional completion/failure system notifications are currently implemented on Android/iOS/Mac Catalyst/Windows, not the Linux Avalonia host.
- The shared `TlsPeerServer` currently retains its existing IPv4 listener behavior; adding the Linux UI host does not itself create IPv6-only transport parity.

Validation still required before a tagged Linux release:

- Exact candidate Linux x64 and ARM64 hosted build/package/audit jobs.
- Launch/install/update/remove behavior of generated archives on representative physical Linux systems.
- GNOME and KDE file/folder picker behavior.
- X11/XWayland operation and HiDPI scaling.
- Clipboard acceptance behavior.
- `swiftdrop:` activation from the installed desktop environment/browser.
- Local firewall behavior and mDNS/UDP discovery on real LANs.
- Linux↔Android/iOS/macOS/Windows file/folder/text transfer and rejection flows.
- Resume/interruption, collision, low-storage, integrity-failure, and shutdown/session-drain behavior.
- Keyboard-only, screen-reader/high-contrast, scaling, and accessibility review.
- ARM64 runtime validation on real ARM64 Linux hardware where available.

See `docs/platforms/linux.md` for build, install, XDG storage, networking, security, and release procedures.

## Cross-platform shared behavior

Implemented consistently in shared code/services used by the transfer hosts:

- Account-free local-first path.
- Receiver certificate pinning and sender client certificate.
- Strict typed protocol requests/acknowledgements.
- Case-insensitive duplicate JSON member rejection.
- Unknown JSON member rejection.
- Type-specific request shape validation.
- Canonical raw Base64URL pairing capability representation with no whitespace/query aliases.
- One-time transfer authorization after strict request/manifest validation and authenticated client-certificate presence.
- Single/multi/folder/text transfer protocol.
- Canonical `/` manifest relative paths across every sender OS.
- Rooted/traversal/empty-segment/backslash/noncanonical manifest rejection before authorization.
- Maximum 64 relative-path segments and bounded manifest path metadata.
- Filename segments bounded by UTF-16 length and UTF-8 bytes.
- Bounded collision markers that remain distinct at maximum filename size.
- Outgoing single-file source revalidation at stream open.
- Bounded deterministic folder enumeration with source symlink/reparse rejection.
- Portable case/Unicode/sanitation collision deconfliction before hashing.
- Stable batch transfer-ID token syntax for interoperable retry/resume flows.
- Removed obsolete implicit fresh-ID batch compatibility overload.
- Collision handling and non-overwrite final promotion.
- Receive-root path confinement including existing reparse/symlink component rejection.
- `.swiftdrop.part` resume.
- Verified completed-file reuse introduced in schema v3 and retained in current schema v6.
- Completed-file verification while building retry plan **and again immediately before zero-byte item completion ACK**.
- SHA-256 integrity verification.
- Privacy-minimal restart-safe queue status/progress/item metadata retained in current schema v6; interrupted MAUI work never replays stale authorization.
- Queue/history/diagnostics/resume metadata only; transfer contents excluded from SQLite.
- UTF-8-byte-bounded external text intake.
- Shared external staging budget used by Android share, iOS Share Extension, and Mac native drop.
- Optional terminal notification preference on notification-enabled MAUI targets is off by default and cannot change the underlying transfer result if permission/registration/presentation fails.
- Terminal notification body text is generic and English/Hindi catalog parity remains CI-validated on those targets.

Host-specific features remain documented in the platform sections above; a shared Core capability should not be mistaken for identical UI integration on every host.

## Source-complete vs release-validated

The current source scope includes the iOS Share Extension, Mac Catalyst native drop, stable/verified batch resume primitives, canonical cross-platform manifest paths, source-link safety, strict pairing representation, external staging-budget controls, schema-v6 restart-safe queue/history/resume metadata, optional native terminal notifications on Android/iOS/Mac Catalyst/Windows, and the Linux Avalonia secure transfer host/package surface. These items are **implemented in source**, not automatically **release-validated**.

A platform is release-validated only after:

1. the exact candidate commit passes configured automated jobs for that target;
2. release workloads compile the application and any applicable extension/package;
3. real signing/provisioning/package/desktop identity requirements succeed where applicable;
4. install/upgrade/launch behavior works on the real target environment;
5. provider/App Group/ContentResolver/desktop-protocol/notification behavior works under real platform conditions where applicable;
6. physical-device/desktop/network/transfer/resume/low-storage/accessibility validation passes;
7. privacy/store/distribution declarations match the shipped binary/artifact.

See `NEXT_STEPS.md`, `docs/platforms/linux.md`, `docs/testing/security-test-plan.md`, `docs/testing/manual-test-matrix.md`, and `docs/release/release-checklist.md`.

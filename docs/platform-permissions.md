# Platform permissions and entitlements

Updated: 2026-08-15

SwiftDrop follows a permission-minimization rule: request only what a user-initiated local-transfer path requires. Do not add broad storage, contacts, microphone, location, advertising, analytics, surveillance, or remote-push permissions for convenience.

## Android

Declared permissions/capabilities support:

- local TCP/TLS sockets;
- network/Wi-Fi state needed by local discovery/diagnostics;
- multicast state required by the reference-counted mDNS lock;
- foreground data-sync service while an active user-initiated transfer is running;
- notification permission declaration for Android versions that gate optional notifications.

File selection uses system picker/content-provider surfaces, so broad legacy storage permissions are intentionally avoided.

Inbound share-sheet content URIs are copied into bounded per-share app-cache staging. SwiftDrop enforces shared file-count/per-file/aggregate budgets, portable UTF-8-bounded names, capacity checks, exact provider size when available, runtime byte caps when size is unavailable/negative, repeated storage-reserve checks during unknown-length streaming, and cleanup on failure. Shared content enters the review UI and is never automatically sent.

Optional completion/failure notifications are opt-in. Where Android requires notification permission, permission is requested only after explicit enable; denial does not change transfer success/failure.

Foreground-service notification/status is a separate Android lifecycle requirement and cannot be hidden merely because optional completion notifications are disabled.

`android:allowBackup="false"` is set for SwiftDrop app-local metadata.

## iOS

The containing app declares:

- local-network usage description;
- Bonjour SwiftDrop service type;
- `swiftdrop` custom URL scheme;
- App Group entitlement:
  `group.in.sanskar.swiftdrop`.

SwiftDrop includes a dedicated **iOS-only** Share Extension with bundle ID:

`in.sanskar.swiftdrop.share`

The extension declares the same App Group. It supports bounded share activation for text, files, images, movies, and web URLs, writes only validated temporary packages into the shared App Group container, and never starts a transfer automatically.

The App Group entitlement in source does not create the capability in an Apple Developer account. Release provisioning profiles for both the iOS app and iOS extension must include the same App Group.

Hosted iOS Simulator compile jobs clear signing/provisioning requirements only at command scope. That compile mode is not evidence that production App Group provisioning is valid.

Optional local completion/failure notifications use `UNUserNotificationCenter`. SwiftDrop requests only alert and sound authorization, and only after the user explicitly enables notifications and saves Settings. The feature does not register for remote push notifications and does not require a remote notification token or cloud notification relay.

If notification authorization is denied/unavailable, SwiftDrop disables the optional preference rather than changing transfer success/failure. Final signed-device validation must confirm authorization prompts, foreground presentation, system notification settings, and generic content.

SwiftDrop does not request camera permission for baseline QR pairing; users can use a system camera/scanner that opens the registered pairing link.

SwiftDrop does not claim arbitrary background socket continuation on iOS.

## Mac Catalyst

The containing app declares:

- App Sandbox;
- network client/server entitlements required for local peer networking;
- App Group `group.in.sanskar.swiftdrop` where configured by the containing app;
- local-network/Bonjour declarations;
- `swiftdrop` URL activation.

The maintained Mac Catalyst architecture does **not** include a Mac Catalyst Share Extension target. External desktop intake uses the containing app's native `UIDropInteraction` plus normal document/file flows.

The main app supports Finder files/folders, text, and pairing links. External file/folder representations are accessed only for the user-triggered drop, copied into bounded app-cache staging while security-scoped access is valid, and then reviewed in SwiftDrop. Shared staging budgets, bounded provider-response waits, portable collision-safe names, and symlink/reparse rejection apply.

Optional completion/failure notifications use the Apple User Notifications framework with the same explicit opt-in alert/sound authorization model as iOS. No additional remote-notification entitlement/service is introduced by the feature.

A signed release must verify the real containing-app sandbox/network/App Group entitlements, notification authorization/presentation, security-scoped access, native drop behavior, signing, notarization, and store packaging. No Mac Catalyst Share Extension embedding/provisioning is expected.

## Windows

The packaged release design declares:

- `privateNetworkClientServer` for local LAN communication;
- package protocol activation for `swiftdrop://` pairing links;
- packaged app-notification activation registration using `windows.toastNotificationActivation` plus the matching COM server/class registration required by the Windows App SDK packaged notification model.

The notification toast activator CLSID and COM class ID are intentionally identical. The packaged executable activation arguments use `----AppNotificationActivated:`. A portable repository validator rejects divergence between those values.

SwiftDrop does **not** declare general `internetClient` capability for protocol-v1 local-only peer transfer. The Windows notification feature does not add an Internet capability or remote push dependency.

Windows uses:

- native FolderPicker for explicit receive/folder-transfer locations;
- native files/folders/text/pair-link drag/drop;
- Windows App SDK local app notifications for optional generic completion/failure status.

Drag/drop does not grant SwiftDrop a general filesystem crawler. Dropped paths remain explicit user input and still pass through source preflight, regular-source/link checks, canonical manifest hashing, transfer authorization, and receiver safety rules.

Optional Windows notifications are off by default. Registration failure is isolated from transfer success/failure, and terminal notification strings contain no dynamic placeholders for filenames/peer names/paths/transfer data.

Hosted Windows CI intentionally compiles with `WindowsPackageType=None` and `GenerateAppxPackageOnBuild=false`. That validates source/XAML/WinUI compatibility but does not exercise MSIX package capabilities, protocol registration, app-notification COM activation, signing, or install/update behavior. The portable manifest validator proves source-level internal consistency only; signed MSIX build/install/update/notification activation validation remains mandatory before release.

## QR pairing and camera permission

SwiftDrop generates QR codes but does not embed a camera scanner in the current baseline, so camera permission is not requested merely for pairing.

If a future in-app scanner is added, camera permission must be requested only when that scanner is explicitly opened and privacy/store declarations must be updated.

## App Group privacy boundary

Apple App Group storage is used as a bounded iOS Share Extension → containing-app handoff. The extension package can contain selected share text/file staging plus its strict metadata manifest. It does not contain:

- SwiftDrop certificate private keys;
- trusted-device credentials;
- reusable pairing authorization;
- transfer history/diagnostic database;
- automatic-send instructions.

The containing app validates exact manifest/package/file-set/size/path/link rules, preflights aggregate app-cache capacity, re-stages accepted package files into ordinary app cache, and presents them for review.

## Notification privacy boundary

Optional terminal notifications are local OS notifications. They contain only the application name and one of the localized generic completion/failure messages.

They do not contain:

- filenames;
- peer names;
- file/folder paths;
- transferred text or file contents;
- pairing links, nonces, or fingerprints;
- transfer IDs;
- reusable authorization or credentials.

Notification permission/registration/presentation failure must never change the transfer outcome.

## OS/network policy boundary

SwiftDrop does not attempt to bypass:

- guest Wi-Fi client isolation;
- enterprise firewall policy;
- multicast filtering;
- local-network permission denial;
- notification permission/system-notification denial;
- Android/iOS background suspension rules;
- app/package sandbox restrictions;
- managed-device policy;
- Apple provisioning/signing requirements;
- Windows package/signing requirements.

If direct peer-to-peer traffic or optional notification presentation is blocked by operating-system policy, SwiftDrop should fail/degrade safely rather than requesting unrelated permissions or adding an undisclosed relay.

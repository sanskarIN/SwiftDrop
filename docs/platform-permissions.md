# Platform permissions and entitlements

SwiftDrop follows a permission-minimization rule: request only what a user-initiated local-transfer path requires. Do not add broad storage, contacts, microphone, location, advertising, analytics, or surveillance permissions for convenience.

## Android

Declared permissions/capabilities support:

- local TCP/TLS sockets;
- network/Wi-Fi state needed by local discovery/diagnostics;
- multicast state required by the reference-counted mDNS lock;
- foreground data-sync service while an active user-initiated transfer is running;
- notification permission declaration for Android versions that gate optional notifications.

File selection uses system picker/content-provider surfaces, so broad legacy storage permissions are intentionally avoided.

Inbound share-sheet content URIs are copied into bounded app-cache staging. SwiftDrop enforces file/count limits, portable names, capacity checks, exact provider size when available, runtime byte caps when size is unavailable, and cleanup on failure. Shared content enters the review UI and is never automatically sent.

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

SwiftDrop includes a dedicated iOS Share Extension with bundle ID:

`in.sanskar.swiftdrop.share`

The extension declares the same App Group. It supports bounded share activation for text, files, images, movies, and web URLs, writes only validated temporary packages into the shared App Group container, and never starts a transfer automatically.

The App Group entitlement in source does not create the capability in an Apple Developer account. Release provisioning profiles for both app and extension must include the same App Group.

SwiftDrop does not request camera permission for baseline QR pairing; users can use a system camera/scanner that opens the registered pairing link.

SwiftDrop does not claim arbitrary background socket continuation on iOS.

## Mac Catalyst

The containing app declares:

- App Sandbox;
- network client/server entitlements required for local peer networking;
- App Group `group.in.sanskar.swiftdrop`;
- local-network/Bonjour declarations;
- `swiftdrop` URL activation.

The Mac Catalyst Share Extension uses:

- App Sandbox;
- the same App Group.

The main app also has native `UIDropInteraction` for Finder files/folders, text, and pairing links. External file/folder representations are accessed only for the user-triggered drop, copied into bounded app-cache staging while security-scoped access is valid, and then reviewed in SwiftDrop. Symlinks/reparse entries are rejected.

A signed release must verify the real sandbox/App Group entitlements, extension embedding, security-scoped access, and notarization/store behavior.

## Windows

SwiftDrop declares:

- `privateNetworkClientServer` for local LAN communication;
- package protocol activation for `swiftdrop://` pairing links.

SwiftDrop does **not** currently declare general `internetClient` capability for protocol-v1 local-only peer transfer.

Windows uses:

- native FolderPicker for explicit receive/folder-transfer locations;
- native files/folders/text/pair-link drag/drop.

Drag/drop does not grant SwiftDrop a general filesystem crawler. Dropped paths remain explicit user input and still pass through source preflight, manifest hashing, transfer authorization, and receiver safety rules.

## QR pairing and camera permission

SwiftDrop generates QR codes but does not embed a camera scanner in the current baseline, so camera permission is not requested merely for pairing.

If a future in-app scanner is added, camera permission must be requested only when that scanner is explicitly opened and privacy/store declarations must be updated.

## App Group privacy boundary

Apple App Group storage is used only as a bounded handoff between the Share Extension and containing SwiftDrop app. The extension package can contain selected share text/file staging plus its strict metadata manifest. It does not contain:

- SwiftDrop certificate private keys;
- trusted-device credentials;
- reusable pairing authorization;
- transfer history/diagnostic database;
- automatic-send instructions.

The containing app revalidates and re-stages accepted package files into ordinary app cache before presenting them for review.

## OS/network policy boundary

SwiftDrop does not attempt to bypass:

- guest Wi-Fi client isolation;
- enterprise firewall policy;
- multicast filtering;
- local-network permission denial;
- Android/iOS background suspension rules;
- app/package sandbox restrictions;
- managed-device policy;
- Apple provisioning/signing requirements.

If direct peer-to-peer traffic is blocked, SwiftDrop should fail safely and explain the limitation rather than requesting unrelated permissions or adding an undisclosed relay.

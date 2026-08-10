# Platform permissions

Updated: 2026-08-10

SwiftDrop follows a permission-minimization rule: request only what a user-initiated local-transfer path requires, and do not add broad storage, contacts, microphone, location, advertising, internet-relay, or background-surveillance capabilities merely for convenience.

## Android

Declared capabilities/permissions support:

- Internet socket API access required by Android for local TCP/TLS communication.
- Network-state inspection for diagnostics/connectivity behavior.
- Wi-Fi-state access required by local discovery support.
- Wi-Fi multicast-state changes for the reference-counted mDNS multicast lock.
- Foreground data-sync service status while a user-initiated transfer is active.
- Notification permission declaration for Android versions that gate optional notifications.

File selection uses Android system picker/content-provider surfaces, so broad legacy storage permissions are intentionally avoided.

Inbound Android share-sheet files arrive as content URIs. SwiftDrop copies selected shared items into bounded app-cache staging before presenting them in the normal transfer-selection UI. Staged cache entries are pruned; shared content is never automatically transferred.

Optional transfer completion/failure notifications are opt-in. On Android versions that gate notification permission, SwiftDrop requests it only after the user enables the preference and saves Settings. If permission is denied, transfers continue normally.

Android foreground-service notification/status is a separate OS lifecycle requirement. Disabling optional completion notifications does not mean SwiftDrop can hide a foreground-service status required by Android.

The Android application manifest now sets `android:allowBackup="false"`, so SwiftDrop does not opt local application metadata into Android application backup/restore.

## iOS

The app declares:

- local-network usage description;
- Bonjour service type `_swiftdrop._tcp`;
- custom `swiftdrop` URL scheme;
- a document type for `public.data` so explicit document/open-file activation can hand a file URL to SwiftDrop;
- opening documents in place support as advertised to the OS, while SwiftDrop still stages external content into its own bounded cache before transfer review.

Apple platforms may prompt for local-network permission when SwiftDrop first accesses LAN peers.

Incoming Apple file URLs are handled with temporary security-scoped access when supplied by the OS. SwiftDrop copies the file through the shared bounded `ExternalFileStager`, releases the security-scoped access immediately after staging, and presents the staged file for explicit review; it is never automatically sent.

SwiftDrop does not request camera permission for baseline QR pairing. A pairing QR encodes the custom SwiftDrop URI; users may scan it with a system camera/scanner and open the registered link where the OS supports that flow.

A dedicated inbound Apple Share Extension for arbitrary files/text remains a separate target and is not currently included. Document/open-file support must not be described as equivalent to a Share Extension.

Optional completion/failure system notifications are not implemented on iOS in the current source; unsupported targets do not pretend to provide that behavior.

## macOS / Mac Catalyst

Mac Catalyst declares:

- local-network usage description;
- Bonjour service type;
- `swiftdrop` URL activation;
- `public.data` document opening;
- an explicit app-sandbox entitlement;
- sandbox network-client and network-server entitlements required for peer-to-peer local networking.

Incoming document/file URLs use temporary security-scoped access and the same bounded cache staging path as iOS. SwiftDrop does not retain security-scoped access beyond staging.

Custom unrestricted external receive folders are not requested in the current Mac Catalyst source. Received files default to the application receive location unless a supported explicit mechanism exists.

First-class native Mac Catalyst file/folder/text drag-and-drop remains separate source work because sandbox/security-scoped URL lifetime must be handled correctly rather than assuming Windows filesystem semantics.

A dedicated Apple Share Extension and optional completion/failure system notifications are not currently implemented for Mac Catalyst.

## Windows

SwiftDrop declares only `privateNetworkClientServer` in the current package capability set for its peer networking model. The package no longer requests a general `internetClient` capability because protocol v1 rejects public-internet and DNS peer destinations.

Windows Firewall can still prompt, restrict, or block inbound traffic according to user/admin/network-profile policy.

Windows uses:

- the native system folder picker for explicit custom receive-folder selection;
- package protocol activation for `swiftdrop://` pairing links;
- WinUI desktop drag-and-drop for files, folders, text, and pairing links.

Drag-and-drop does not add broad filesystem permission. Dropped paths still pass through SwiftDrop's bounded external-input, source preflight, manifest, path, and transfer-authorization pipeline.

Optional completion/failure system notifications are not implemented on Windows in the current source.

## QR pairing and camera permission

SwiftDrop generates a QR code containing a short-lived pairing URI. The baseline app does not embed a camera/scanner library and therefore does not request camera permission merely for pairing. A user can use a system camera/scanner capable of opening the registered `swiftdrop://pair` URI, or use the pairing link/nearby/manual-code alternatives.

If an in-app QR scanner is added later, camera permission must be requested only when the user explicitly opens that scanner and privacy/store declarations must be updated.

## Background behavior

SwiftDrop does not request permissions or entitlements intended to evade normal platform lifecycle restrictions. Android foreground data-sync support exists for active user-initiated transfers. Apple targets do not claim that arbitrary TCP sockets continue indefinitely after suspension. Windows/macOS lifecycle behavior still requires packaged/signed runtime testing.

## Network and OS policy boundaries

SwiftDrop does not attempt to bypass:

- guest Wi-Fi client isolation;
- enterprise firewall policy;
- multicast filtering;
- local-network permission denial;
- Android/iOS background suspension rules;
- package/app sandbox restrictions;
- managed-device/MDM policy.

When policy blocks direct peer-to-peer traffic, SwiftDrop should fail safely and explain the limitation rather than requesting unrelated permissions or adding an undisclosed cloud relay.

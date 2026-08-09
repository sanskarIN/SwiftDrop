# Platform permissions

SwiftDrop follows a permission-minimization rule: request only what a user-initiated local-transfer path requires, and do not add broad storage, contacts, microphone, location, advertising, or background-surveillance permissions simply for convenience.

## Android

Declared capabilities/permissions support:

- Internet sockets for local TCP/TLS communication.
- Network-state inspection for diagnostics/connectivity behavior.
- Wi-Fi-state access required by local discovery support.
- Wi-Fi multicast-state changes for the reference-counted mDNS multicast lock.
- Foreground data-sync service status while a user-initiated transfer is active.
- Notification permission declaration for Android versions that gate optional notifications.

File selection uses Android's system picker/content-provider surfaces, so broad legacy storage permissions are intentionally avoided.

Inbound Android share-sheet files arrive as content URIs. SwiftDrop copies selected shared items into bounded app-cache staging before presenting them in the normal transfer-selection UI. Staged cache entries are pruned; shared content is never automatically transferred.

Optional transfer completion/failure notifications are **opt-in**. On Android 13+ SwiftDrop requests notification permission only after the user enables the notification preference and saves Settings. If permission is denied, the preference is turned back off and transfers continue normally.

Android foreground-service notification/status is a separate operating-system lifecycle requirement. Disabling optional completion notifications does not mean SwiftDrop can hide a foreground-service status that Android requires.

## iOS

The app declares:

- local-network usage description;
- Bonjour service type used by SwiftDrop discovery;
- custom `swiftdrop` URL scheme.

Apple platforms may prompt for local-network permission when SwiftDrop first accesses LAN peers.

SwiftDrop does not request camera permission for baseline QR pairing. A pairing QR encodes the custom SwiftDrop URI; users may scan it with the system camera/scanner and open the registered link where the OS supports that flow.

A dedicated inbound Apple Share Extension for arbitrary files/text is a separate application-extension target and is not yet included. SwiftDrop therefore does not claim share-extension entitlement/permission behavior that is absent from source.

Optional completion/failure system notifications are not implemented on iOS in the current source; the setting is disabled on unsupported targets rather than silently doing nothing.

## macOS / Mac Catalyst

Mac Catalyst uses the same local-network/Bonjour and `swiftdrop` URL activation model as the Apple app target, subject to sandbox and signing/entitlement behavior.

Custom unrestricted external receive folders are not requested in the current Mac Catalyst source. Files are selected through platform picker/share-safe surfaces, and received files default to app-private storage unless a supported explicit mechanism exists.

First-class Mac Catalyst file/folder/text drag-and-drop remains separate source work because security-scoped URL lifetime and sandbox behavior must be handled correctly rather than assuming Windows filesystem semantics.

Optional completion/failure system notifications are not implemented on Mac Catalyst in the current source.

## Windows

SwiftDrop declares private-network client/server capability for inbound/outbound LAN connections. Windows Firewall can still prompt, restrict, or block inbound traffic according to user/admin policy.

Windows uses:

- the native system folder picker for explicit custom receive-folder selection;
- package protocol activation for `swiftdrop://` pairing links;
- WinUI desktop drag-and-drop for files, folders, text, and pairing links.

Drag-and-drop does not add broad filesystem permission. Dropped paths still pass through SwiftDrop's bounded external-input, source preflight, manifest, path, and transfer authorization pipeline.

Optional completion/failure system notifications are not implemented on Windows in the current source; the setting is disabled on unsupported targets.

## QR pairing and camera permission

SwiftDrop generates a QR code containing a short-lived pairing URI. The baseline app does not embed a camera/scanner library and therefore does not request camera permission merely for pairing. A user can use a system camera/scanner capable of opening the registered `swiftdrop://pair` URI, or use the pairing link/nearby/manual-code alternatives.

If an in-app QR scanner is added later, camera permission must be requested only when the user explicitly opens that scanner and the privacy/store declarations must be updated.

## Network and OS policy boundaries

SwiftDrop does not attempt to bypass:

- guest Wi-Fi client isolation;
- enterprise firewall policy;
- multicast filtering;
- local-network permission denial;
- Android/iOS background suspension rules;
- package sandbox restrictions;
- managed-device policy.

When policy blocks direct peer-to-peer traffic, SwiftDrop should fail safely and explain the limitation rather than requesting unrelated permissions or adding an undisclosed cloud relay.

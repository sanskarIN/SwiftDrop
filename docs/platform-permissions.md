# Platform permissions

## Android

SwiftDrop declares Internet, network-state, and Wi-Fi-state access. File selection uses the system picker, so broad storage permissions are intentionally avoided.

## iOS / macOS

The app declares a local-network usage description and custom `swiftdrop` URL scheme. Apple platforms may prompt for local-network permission when the app first accesses LAN peers.

## Windows

The app needs private-network client/server capability for inbound LAN connections. Windows Firewall can still prompt or block inbound traffic depending on user policy.

## Principle

SwiftDrop requests only permissions required for the user-initiated transfer path. It does not request contacts, microphone, location, or camera permission for the baseline flow. QR scanning can be performed with the system camera/scanner opening the custom pairing link.

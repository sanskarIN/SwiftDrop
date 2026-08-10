# SwiftDrop Privacy

Updated: 2026-08-10

SwiftDrop is designed as a local-network, account-free transfer application.

## Current release behavior

- No SwiftDrop account is required.
- SwiftDrop does not upload transferred file/text contents to a SwiftDrop-operated cloud or relay service.
- File and text content is sent directly to the selected nearby device over the local network.
- Device identity material is generated locally.
- The device certificate/private-key material is stored using platform secure storage.
- Transfer payload bytes are streamed to the filesystem and are not stored in SQLite.
- Transfer history is local metadata only and does not contain transferred file bytes or text-snippet contents.
- Privacy mode redacts both peer display names and file/description names from newly stored history metadata and also hides those fields when older history is displayed.
- Privacy mode also applies structured redaction to diagnostic messages for IP addresses/endpoints, GUID-like identifiers, SHA-256 fingerprints, paths, email-like tokens, and SwiftDrop pairing URIs.
- Queue persistence is privacy-minimal and never persists source filenames/paths, transferred text, peer IP addresses, pairing invitations/nonces, credentials, private keys, or free-form exception messages.
- SwiftDrop does not continuously monitor the clipboard. Clipboard text is read only after an explicit user action.
- SwiftDrop does not automatically open or execute received files.
- SwiftDrop does not intentionally collect contacts, microphone data, background location, advertising identifiers, or analytics in the current source baseline.
- Optional completion/failure notification text is designed to remain generic and not expose filenames.

## Data stored on the device

SwiftDrop may store:

- a random local device ID;
- the user-visible local device name;
- a local certificate/private key in secure storage;
- app settings;
- explicitly trusted-device metadata containing device ID/name, canonical SHA-256 certificate fingerprint, and trust/last-seen timestamps;
- transfer history metadata containing direction, peer label, file/description label, size, timestamp, status, and integrity result;
- bounded diagnostic metadata;
- privacy-minimal restart-safe queue metadata;
- incomplete `.swiftdrop.part` files required for resumable transfer;
- temporary app-cache copies of files explicitly handed to SwiftDrop by a supported platform share/open surface;
- completed received files in the selected/application receive location.

Privacy mode uses a language-neutral private marker in persisted history rather than storing localized placeholder text. The UI translates that marker when displaying history.

## Platform backup behavior

- Android application backup is explicitly disabled (`android:allowBackup="false"`) so SwiftDrop does not opt its local app metadata into Android app backup/restore.
- Windows package capability is restricted to `privateNetworkClientServer`; the current protocol does not request a general internet-client capability.
- Mac Catalyst uses app-sandbox network client/server entitlements for the local peer transport.
- Platform secure-storage/keychain/keystore behavior remains controlled by the operating system. Real uninstall/reinstall, device restore, migration, and locked-device behavior must be validated on target hardware.

## Pairing invitations and one-time authorization

A pairing invitation contains temporary connection metadata, including the receiver LAN address, certificate fingerprint, expiration time, and cryptographically random one-time nonce. It does not contain the receiver private key.

Decoded pairing JSON is strictly validated for depth, malformed UTF-8/JSON behavior, comments/trailing commas, and duplicate property names before deserialization. The invitation is also validated for local/private numeric address policy, version, identity metadata, fingerprint, nonce, and expiry/lifetime. Active pairing nonces are held only in memory, are bounded, preserve exact expiration precision, and are removed atomically on first consumption.

Short numeric pairing codes are also temporary and are not treated as long-term passwords. Pairing invitations/codes should still be treated as temporary sensitive capabilities and should not be published.

## Discovery and network visibility

Local discovery traffic may reveal that a device is running SwiftDrop to other devices on the same LAN when discovery is enabled. Discovery uses mDNS/Bonjour with a bounded UDP fallback. Discovery metadata is parsed defensively and duplicate TXT keys are rejected.

Local network administrators, access points, firewalls, and operating systems can observe network metadata such as source/destination addresses and traffic volume even though TLS protects transfer contents in transit. SwiftDrop does not attempt to bypass guest-Wi-Fi isolation, firewall policy, MDM/enterprise controls, or operating-system local-network restrictions.

## Shared/opened files

On supported platforms SwiftDrop can receive files from an explicit platform share/open surface. External files are staged into SwiftDrop cache using bounded exact-length copying, sanitized names, cancellation support, and cleanup on failure before appearing in the normal review/send workflow. On Apple platforms, file-URL staging uses temporary security-scoped access where supplied by the OS. Shared/dropped/opened content is never sent automatically.

The current Apple integration includes document/open-file URL handling. A dedicated first-class Apple Share Extension target remains separate future source/release work and is not claimed as implemented.

## Trusted devices

Trusted-device records are local metadata. Trust is bound to the exact canonical SHA-256 certificate fingerprint for a device ID. Malformed persisted fingerprint rows are ignored rather than silently treated as valid trust. Resetting local identity changes the identity/certificate and clears local trust decisions through the app workflow.

## Diagnostics

Diagnostics are intentionally bounded and metadata-only. Safe exports do not include transfer contents, private keys, one-time pairing nonces, or complete pairing invitations. When privacy mode is enabled, both newly recorded and previously stored messages are passed through structured identifier redaction at read/export time.

## Deleting data

Users can delete individual transfer-history records or clear all history through the app. Users can clear diagnostic events and revoke/clear trusted devices. Received files remain normal files in their receive location and must be deleted there when no longer wanted. App-private staged/share files are temporary cache material subject to cleanup behavior.

Resetting app storage through the operating system can remove app-local metadata and identity material, subject to platform behavior. Identity reset in SwiftDrop creates a new local identity/certificate and invalidates active pairing authorization without deleting received files or transfer history.

## Future features

If a future version adds accounts, internet relay transfer, cloud synchronization, crash reporting, analytics, or another remote service, that feature must be documented separately before release and must not silently change the privacy behavior described for the current local-only mode.

## Contact

- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`

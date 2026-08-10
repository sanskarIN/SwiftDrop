# SwiftDrop Privacy

SwiftDrop is designed as a local-network, account-free transfer application.

## Current local-transfer model

- No SwiftDrop account is required.
- SwiftDrop does not upload transferred file/text contents to a SwiftDrop-operated cloud service.
- File and text content is sent directly to the selected nearby device over the local network.
- Device identity is generated locally.
- The device certificate private key is stored through platform secure storage.
- SwiftDrop does not continuously monitor the clipboard.
- SwiftDrop does not automatically open or execute received files.
- SwiftDrop does not intentionally collect contacts, microphone data, background location, advertising identifiers, or analytics in the current source.

## Data stored on the device

SwiftDrop may store locally:

- a random local device ID;
- user-visible device name;
- a local certificate/private key through platform secure storage;
- app settings;
- explicitly trusted-device metadata;
- transfer-history metadata;
- bounded diagnostic metadata;
- privacy-minimal transfer-queue status metadata;
- verified completed-batch resume metadata;
- incomplete `.swiftdrop.part` files required for resumable transfer;
- temporary external-input cache staging;
- completed received files in the approved receive location.

Transferred file bytes and transferred text contents are not stored in SQLite.

## Transfer history and privacy mode

Transfer history contains metadata such as direction, peer display name, filename/description, size, timestamp, status, and integrity result.

When privacy mode is enabled:

- new history rows store a language-neutral redaction marker instead of peer/file names;
- older rows are also redacted at read time without being silently rewritten;
- diagnostic read/export paths redact common identifying tokens including paths, email addresses, IP addresses/endpoints, GUIDs, certificate fingerprints, and SwiftDrop pairing URIs;
- queue persistence remains generic and never stores filenames/source paths or transferred text.

History retention can be configured; zero-day retention clears retained history.

## Verified batch-resume metadata

SQLite schema version 3 contains metadata that allows an interrupted batch to avoid resending a file that was already fully verified/finalized before the interruption.

A completed-batch metadata row can contain:

- stable random transfer ID;
- sender source relative path;
- SHA-256 key derived from the normalized receive root;
- effective destination relative path;
- length and SHA-256;
- completion timestamp.

The absolute receive-root path is **not** stored in this table.

This metadata is not authorization. Before SwiftDrop treats a retry item as already complete it verifies the same transfer/root/source/length/hash, confirms the destination remains beneath the receive root without symlink/reparse traversal, checks the file still exists at the expected length, and recomputes SHA-256. Pairing authorization must still be fresh.

Completion metadata is bounded/pruned and is a best-effort resume optimization; persistence failure does not change the success of a verified transfer.

## Pairing invitations

A pairing invitation contains temporary local connection metadata such as receiver LAN address, certificate fingerprint, expiration, and a random one-time nonce. It does not contain the receiver private key.

Pairing invitations should still be treated as temporary sensitive capabilities and should not be published. A transfer invitation is consumed for one transfer attempt; pause/retry/resume requires fresh pairing authorization.

## External share/drop staging

SwiftDrop never automatically sends externally shared or dropped content. External content is staged only so the user can review it inside SwiftDrop before sending.

### Android

Android share-sheet content URIs may be copied into SwiftDrop app cache. Staging is bounded by protocol file/count limits, uses portable filename sanitation, performs storage-capacity checks, validates provider-declared length where available, enforces a runtime byte cap when length is unknown, and removes failed partial staging. Stale cache content is pruned.

Android application backup is disabled for SwiftDrop app-local metadata.

### iOS / Mac Catalyst Share Extension

SwiftDrop includes a Share Extension using App Group:

`group.in.sanskar.swiftdrop`

The extension may stage selected file/text/URL content into an App Group inbox using a strict versioned manifest and atomic package publication. The containing app validates package age, schema, filenames, item sizes, aggregate size, symlink/reparse status, and exact file length before copying accepted items into regular app cache for review.

Malformed/stale packages are discarded; temporary extension staging is pruned. The extension does not receive or store SwiftDrop private keys, trusted-peer credentials, or reusable pairing authorization.

### Mac Catalyst native drop

Mac native drag/drop may temporarily access user-dropped Finder items through security-scoped URLs and copy them into bounded cache staging while access is valid. Symlinks/reparse inputs are rejected, item/file/aggregate limits are enforced, and dropped text is UTF-8-byte bounded. Nothing is automatically transferred.

### Windows native drop

Windows drag/drop supplies explicit user-selected filesystem paths/text to the review inbox. Those paths still pass through normal source preflight, manifests, path validation, and transfer authorization before bytes are sent.

## Diagnostics

Diagnostic messages are bounded and single-line. Safe export is designed to exclude:

- transferred file/text contents;
- certificate private keys;
- pairing nonces;
- complete pairing invitations;
- reusable authorization.

Privacy mode adds identifier redaction at record/read/export time.

## Network visibility

Local discovery may reveal that a device is running SwiftDrop to other devices on the same LAN. Local network administrators and operating systems can observe network metadata such as source/destination addresses and traffic volume even though TLS protects transfer contents in transit.

SwiftDrop does not attempt to bypass guest-Wi-Fi isolation, firewall policy, multicast filtering, local-network permission denial, package sandbox restrictions, or mobile OS background policy.

## Platform capability minimization

- Android uses local-network/share/foreground-service capabilities required by the implemented workflow and disables application backup.
- Apple targets use local-network/Bonjour declarations plus the App Group required for Share Extension handoff; Mac Catalyst uses sandbox/network entitlements needed for direct LAN transfer.
- Windows requests private-network client/server capability rather than a general Internet-client capability for the local-only protocol.
- Broad legacy storage permissions, contacts, microphone, background-location, advertising, and analytics permissions are not part of the current baseline.

## Deleting data

Users can clear transfer history and local diagnostic history through the app. Trusted peers can be revoked/cleared. Queue metadata is bounded and finished entries can be cleared. Temporary external-input staging is pruned and may also disappear when app cache is cleared by the OS/user.

Received files remain normal files in the selected/application receive location and must be deleted there when no longer wanted. Resetting app storage can remove local settings/history/trust/resume metadata and identity material subject to platform secure-storage behavior.

## Future privacy changes

If a future version adds accounts, relay transfer, cloud synchronization, crash reporting, analytics, remote push services, or another network service, that feature must be documented before release and must not silently change the privacy behavior described for the current local-only mode.

## Contact

- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`

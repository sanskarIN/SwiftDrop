# SwiftDrop Privacy

Updated: 2026-08-15

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
- Optional terminal notifications are generated locally through platform notification APIs and do not require a SwiftDrop-operated remote-push service.

## Data stored on the device

SwiftDrop may store locally:

- a random local device ID;
- user-visible device name;
- a local certificate/private key through platform secure storage;
- app settings;
- explicitly trusted-device metadata;
- transfer-history metadata;
- bounded diagnostic metadata;
- privacy-minimal transfer-queue status/progress metadata;
- verified completed-batch resume metadata;
- incomplete `.swiftdrop.part` files required for resumable transfer;
- temporary external-input cache staging;
- completed received files in the approved receive location.

Transferred file bytes and transferred text contents are not stored in SQLite.

## Transfer history and privacy mode

Transfer history contains metadata such as direction, peer display name, filename/description, logical size, timestamp, status, integrity result, and—when actually measured—bounded elapsed duration plus the number of bytes attributable to that interval.

When privacy mode is enabled:

- new history rows store a language-neutral redaction marker instead of peer/file names;
- older rows are also redacted at read time without being silently rewritten;
- diagnostic read/export paths redact common identifying tokens including paths, email addresses, IP addresses/endpoints, GUIDs, certificate fingerprints, and SwiftDrop pairing URIs;
- queue persistence remains generic and never stores filenames/source paths or transferred text.

History retention can be configured; zero-day retention clears retained history. Performance calculations use only completed rows that contain both a positive measured duration and a valid positive measured-byte count. Resumed transfers record only bytes actually sent/received after the negotiated resume offset; legacy rows are never assigned invented measurements.

## Restart-safe transfer queue metadata

Current SQLite schema version 6 retains the bounded queue status/progress metadata introduced through schema v4 so recent work remains understandable after an application restart without making stale work automatically executable.

A queue metadata row may contain:

- a random queue item identifier;
- the generic persisted label `Transfer`;
- state (`Queued`, `Running`, `Completed`, `Failed`, `Cancelled`, or `Interrupted`);
- creation/start/finish/update timestamps;
- a bounded machine-oriented error code;
- a non-secret operation category (`Transfer`, `File`, `Batch`, `Text`, or `Receive`);
- monotonic progress expressed as `0..10000` basis points;
- optional non-negative total/completed item counts.

SwiftDrop deliberately does **not** store reusable transfer authorization with this queue metadata. The queue table does not contain transferred text/file contents, source or destination paths, peer host/IP/port values, pairing invitations/nonces, bearer/session tokens, peer certificates, private keys, or reusable credentials. The persisted label remains generic even when the in-memory UI can show a filename while privacy mode is disabled.

Ordinary progress persistence is coarsened to bounded progress buckets plus state/item-count transitions rather than writing every progress callback to SQLite. On startup, stale persisted `Queued`/`Running` entries are changed to `Interrupted` while retaining their safe last-known progress/context. They are not replayed automatically; another transfer attempt still requires fresh authorization through the normal pairing flow.

## Verified batch-resume metadata

The `completed_batch_items` table was introduced in SQLite schema version 3 and remains part of current schema version 6. It allows an interrupted batch to avoid resending a file that was already fully verified/finalized before the interruption.

A completed-batch metadata row can contain:

- stable random transfer ID;
- sender canonical source relative path;
- SHA-256 key derived from the normalized receive root;
- effective destination relative path;
- length and SHA-256;
- completion timestamp.

The absolute receive-root path is **not** stored in this table.

This metadata is not authorization. Before SwiftDrop treats a retry item as already complete it verifies the same transfer/root/source/length/hash, confirms the destination remains beneath the receive root without symlink/reparse traversal, checks the file still exists at the expected length, and recomputes SHA-256. After the sender returns the matching batch-item-start frame, SwiftDrop verifies that completed destination again immediately before the zero-byte completion acknowledgement. Pairing authorization must still be fresh.

Completion metadata is bounded/pruned and is a best-effort resume optimization; persistence failure does not change the success of a verified transfer.

## Pairing invitations

A pairing invitation contains temporary local connection metadata such as receiver LAN address, certificate fingerprint, expiration, and a random one-time nonce. It does not contain the receiver private key.

Pairing invitations should still be treated as temporary sensitive capabilities and should not be published. A transfer invitation is consumed for one transfer attempt; pause/retry/resume requires fresh pairing authorization.

Pairing text is accepted only in SwiftDrop's canonical strict representation; malformed or alias representations are rejected rather than silently normalized into another capability string.

## Optional system notifications

Completion/failure system notifications are **off by default** and require explicit user opt-in in Settings.

The notification privacy contract is intentionally narrow. A terminal notification contains only:

- the application name `SwiftDrop`;
- one localized generic status message indicating success or failure.

It does **not** contain:

- filenames or folder names;
- peer/device names;
- source or destination paths;
- transferred text/file contents;
- pairing invitations, nonces, fingerprints, or one-time codes;
- transfer IDs;
- reusable transfer authorization or credentials.

The English/Hindi terminal notification resources are placeholder-free, so application code cannot inject transfer-specific values into those messages through formatted resource parameters. A portable Windows integration validator also enforces the placeholder-free notification contract while checking packaged notification registration.

### Android notifications

Android uses the existing local notification path. Android 13+ notification permission is requested only after the user enables notifications. The foreground-service notification required for an active Android transfer remains a separate lifecycle requirement.

### iOS and Mac Catalyst notifications

iOS and Mac Catalyst use local `UNUserNotificationCenter` notifications. SwiftDrop requests only alert/sound authorization after explicit opt-in. The notification-center delegate allows enabled generic terminal notifications to be presented while the containing app is foregrounded.

SwiftDrop does not register for a remote push token and does not require APNs/server relay infrastructure for this terminal status feature.

### Windows notifications

Windows uses local Windows App SDK app notifications. The app notification manager is registered locally and the packaged manifest declares the corresponding notification activation/COM server metadata. The notification activation handler is informational and does not receive or expose transfer identifiers.

Hosted Windows CI intentionally uses unpackaged source compilation. A separate portable validator checks the source/package registration is internally consistent, but the signed MSIX/package still requires real install/update/activation testing before release.

On every platform, notification permission/registration/presentation failure is best-effort and must not change the underlying transfer result. Transfer status remains available inside SwiftDrop.

## External share/drop staging

SwiftDrop never automatically sends externally shared or dropped content. External content is staged only so the user can review it inside SwiftDrop before sending.

A shared Core staging-budget policy limits file count, per-file bytes, and aggregate bytes for Android shares, the iOS Share Extension, and Mac native drop. Budget is committed only after a file stages successfully.

### Android

Android share-sheet content URIs may be copied into bounded per-share SwiftDrop app-cache staging. Staging uses portable UTF-8-bounded filename sanitation, performs storage-capacity checks, validates provider-declared length where available, treats negative provider size as unknown, caps unknown-length runtime bytes to the remaining aggregate budget, rechecks storage reserve while streaming unknown-length providers, verifies exact staged length, and removes failed partial files/directories. Stale cache content is pruned.

Android application backup is disabled for SwiftDrop app-local metadata.

### iOS Share Extension

SwiftDrop includes a dedicated **iOS-only** Share Extension using App Group:

`group.in.sanskar.swiftdrop`

The extension may stage selected file/text/image/movie/web-URL content into an App Group inbox using a strict versioned manifest and atomic package publication. Provider-response waits are bounded; once a provider responds and a legitimate local copy has begun, the response timer is not treated as a file-copy timeout. Extension-lifetime cancellation still bounds active work.

The containing app validates package age, schema, filenames, canonical paths, item sizes, aggregate size, exact physical file set, symlink/reparse status, and exact file length. It preflights aggregate app-cache capacity before copying accepted items into regular app cache for review. One pending package is surfaced at a time rather than silently merging later pending packages.

Malformed/stale packages are discarded; temporary extension staging is pruned. The extension does not receive or store SwiftDrop private keys, trusted-peer credentials, transfer history databases, or reusable pairing authorization.

The source App Group entitlement does not itself establish Apple Developer provisioning. Signed iOS app/extension profiles must contain the same App Group before release.

### Mac Catalyst native drop

The maintained Mac Catalyst architecture does **not** include a Mac Catalyst Share Extension. External desktop intake uses the containing app's native `UIDropInteraction` and normal file/document flows.

Mac native drag/drop may temporarily access user-dropped Finder items through security-scoped URLs and copy them into bounded cache staging while access is valid. Symlinks/reparse inputs are rejected, shared item/per-file/aggregate limits are enforced, provider-response waits are bounded, collision names remain portable and byte-bounded, and dropped text is UTF-8-byte bounded. Nothing is automatically transferred.

### Windows native drop

Windows drag/drop supplies explicit user-selected filesystem paths/text to the review inbox. Those paths still pass through normal regular-source/link-safe preflight, canonical manifests, hashing, transfer authorization, and receiver safety rules before bytes are sent.

Hosted Windows CI uses unpackaged source compilation. That does not change the shipped privacy/capability model and is not evidence that a signed MSIX package's capabilities/protocol/notification registration were validated.

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

SwiftDrop does not attempt to bypass guest-Wi-Fi isolation, firewall policy, multicast filtering, local-network permission denial, notification permission denial, package sandbox restrictions, or mobile OS background policy.

## Platform capability minimization

- Android uses local-network/share/foreground-service capabilities required by the implemented workflow, optional notification permission where the OS requires it, and disables application backup.
- iOS uses local-network/Bonjour declarations plus the App Group required for the iOS Share Extension handoff; local terminal notifications use alert/sound authorization only and do not require remote push registration.
- Mac Catalyst uses containing-app sandbox/network/App Group declarations needed by its local-transfer/native-drop model; local terminal notifications use the Apple User Notifications framework and no Mac Catalyst Share Extension entitlement is required by the maintained architecture.
- Windows packaged release design requests private-network client/server capability rather than a general Internet-client capability for the local-only protocol; packaged local app-notification activation is declared without adding Internet capability.
- Broad legacy storage permissions, contacts, microphone, background-location, advertising, analytics, and remote-push infrastructure are not part of the current baseline.

## Deleting data

Users can clear transfer history and local diagnostic history through the app. Trusted peers can be revoked/cleared. Queue metadata is bounded and finished entries can be cleared. Temporary external-input staging is pruned and may also disappear when app cache is cleared by the OS/user.

Received files remain normal files in the selected/application receive location and must be deleted there when no longer wanted. Resetting app storage can remove local settings/history/trust/resume metadata and identity material subject to platform secure-storage behavior.

Operating systems may retain delivered notification-center history according to their own notification settings/policies until the user/OS clears it. SwiftDrop's notification body is deliberately generic so this retained OS-level notification history does not contain transfer-specific content.

## Future privacy changes

If a future version adds accounts, relay transfer, cloud synchronization, crash reporting, analytics, remote push services, or another network service, that feature must be documented before release and must not silently change the privacy behavior described for the current local-only mode.

## Contact

- Business/security: `sanskarin@outlook.in`
- Support: `supportramsandesh@gmail.com`

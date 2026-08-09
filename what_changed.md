# What changed

Date: 2026-08-09
Repository: https://github.com/sanskarIN/SwiftDrop

This file is the detailed engineering ledger requested for SwiftDrop. Chat replies are intentionally kept short; implementation details, security notes, validation limits, remaining engineering work, and release boundaries are recorded here instead.

## Source prompt alignment

Work continues against `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md` and its local-first, account-free, cross-platform .NET MAUI/C# requirements. The repository preserves and now exposes:

- Apache-2.0 licensing.
- `Made by the Sanskar` branding.
- Project/business email `sanskarin@outlook.in`.
- Support email `supportramsandesh@gmail.com`.
- GitHub repository `https://github.com/sanskarIN/SwiftDrop`.
- GitHub profile `https://www.github.com/sanskarIN` where referenced by product/about documentation.
- Optional project support link `https://buymeacoffee.com/sanskarIN`.

The Buy Me a Coffee link is deliberately presented as optional project support. It does not unlock transfer features, privileged support, faster security handling, access to private transfer data, or any hidden application capability.

## Continuation work completed in this development pass

The current continuation added or completed all of the following source/repository work before this ledger was refreshed:

- Added Buy Me a Coffee support link to README.
- Added Buy Me a Coffee card/button to the in-app About page.
- Added Buy Me a Coffee support information to `SUPPORT.md`.
- Added `.github/FUNDING.yml` with the custom support URL.
- Added `NEXT_STEPS.md` with detailed P0/P1/P2 engineering/release priorities.
- Refreshed `PROJECT_STATUS.md` to distinguish implemented source from external release validation.
- Refreshed `CHANGELOG.md` with all current hardening/platform/support changes.
- Added validated settings-change notifications.
- Made the receive listener follow receive-folder settings changes.
- Added receiver-side shared batch manifest validation.
- Added aggregate accepted-batch storage-capacity preflight.
- Added active incoming-session tracking/drain during receiver shutdown/restart.
- Added atomic receive-destination reservations for concurrent transfers.
- Added portable Windows reserved-device filename sanitation.
- Added Unicode NFC filename normalization.
- Added complete sender batch preflight before hashing.
- Bound outgoing byte streams to the manifest-declared file length.
- Added staged-resume tail truncation and invalid-offset rejection.
- Tightened sender validation of receiver resume/completion responses.
- Added portable mutual-TLS loopback transfer/pinning/resume tests.
- Added native Windows desktop drag-and-drop intake for files, folders, text, and pairing links.
- Migrated MAUI startup to `CreateWindow` lifecycle handling.
- Added main-page lifetime disposal/cancellation.
- Migrated secondary-page dialog usage to current MAUI async APIs and routed MainPage dialogs through async API helpers.
- Consolidated Android mDNS multicast-lock ownership onto a reference-counted manager.
- Added explicit local identity-certificate validity/renewal/recovery policy.
- Added user-visible notice when identity is automatically regenerated.
- Added TLS client/server EKUs to local peer certificates.
- Canonicalized SHA-256 certificate fingerprints and trusted-device matching.
- Serialized trusted-device store initialization.
- Replaced the older permissive pairing codec with strict local-only pairing-link validation.
- Added rejection for duplicate/unexpected pairing-query data and unexpected outer URI authority/path data.
- Corrected protocol-compatibility tests to validate untrusted input at the decode boundary.
- Expanded certificate, fingerprint, pairing, filename, destination-reservation, batch, resume, mutation, and TLS tests.

## Implementation completed across the project

### Application structure

- Built a .NET 10 solution containing a reusable `SwiftDrop.Core` library, a .NET MAUI `SwiftDrop.App`, and portable xUnit tests.
- Added Android, iOS, Mac Catalyst, and Windows target declarations and platform metadata.
- Added dependency-injection registration for identity, settings, appearance, receive location, transfer activity, queueing, transfer coordination, history, diagnostics, trust, discovery, pairing, self-tests, pages, and view models.
- Added repository-wide `.editorconfig`, nullable/analysis rules, deterministic builds, CI, issue templates, pull-request template, contribution/security policies, Dependabot, CodeQL, platform compile workflows, release-readiness workflows, and release/test documentation.
- Added `NEXT_STEPS.md` as the prioritized roadmap and `PROJECT_STATUS.md` as the high-level engineering state.
- Began an incremental MVVM refactor rather than leaving every surface entirely in page code-behind. History and Queue use observable view models; other screens remain candidates for full conversion.
- Main transfer functionality is split into partial classes for primary UI orchestration, external input, folder picking, dialog compatibility, identity-recovery notice, and application lifetime handling.
- MAUI application startup now uses `Application.CreateWindow` instead of deprecated `Application.MainPage` assignment.
- Window destruction triggers main transfer lifetime cleanup, active send cancellation, and receiver shutdown.

### Device identity and secure storage

- Each installation creates a local device ID and self-signed P-256 ECDSA certificate.
- Certificate private-key material is stored through MAUI `SecureStorage`; it is not stored in SQLite, pairing links, diagnostics, transfer history, GitHub, or source configuration.
- New local certificates include:
  - non-CA basic constraints;
  - digital-signature key usage;
  - TLS server-auth EKU;
  - TLS client-auth EKU;
  - subject key identifier;
  - bounded five-year validity.
- Device ID input to certificate creation is length/control-character validated.
- Device name can be changed independently of cryptographic identity.
- Settings provide an explicit identity-reset workflow that creates a new device ID/certificate and clears local trust relationships rather than silently retaining stale trust.
- Added `IdentityCertificatePolicy` covering:
  - private-key presence;
  - NotBefore clock-skew tolerance;
  - expiry;
  - seven-day renewal window;
  - supported ECDSA private-key type.
- Corrupt, expired, near-expiry, missing-private-key, or otherwise unusable stored certificates are not silently reused.
- When stored identity material cannot be safely reused, SwiftDrop creates a new device ID/certificate and invalidates active pairing nonces.
- Automatic identity regeneration is surfaced to the user once so other devices can be deliberately paired again.
- Automatic identity regeneration does not delete received files or transfer history.
- Certificate fingerprints are SHA-256 based and displayed for user verification during pairing/consent flows.

### Certificate fingerprint handling

- Fingerprint parsing/normalization is centralized in `Fingerprint`.
- Fingerprints must represent exactly 32 SHA-256 bytes.
- Compact and colon-separated representations can be accepted where appropriate.
- Stored canonical form is uppercase 64-hex characters.
- Trusted-device fingerprint matching uses constant-time byte comparison.
- Temporary comparison buffers are cleared after use.
- `Pretty` renders exactly 32 colon-separated bytes and rejects malformed values instead of formatting arbitrary input.

### Discovery

- Added a reusable discovery registry with deduplication, last-seen tracking, expiry, self-filtering, and stable sorting.
- Added an internal mDNS/DNS-SD codec and discovery service in `SwiftDrop.Core`; the implementation does not depend on a claimed Zeroconf package that is absent from the project.
- Added bounded UDP IPv4 broadcast fallback with validation and automatic peer expiry.
- Added a MAUI Nearby Devices surface that consumes the discovery service.
- Added Apple Bonjour service declarations and Android multicast permission required by discovery.
- Consolidated Android multicast access through a reference-counted Wi-Fi multicast-lock manager so multiple discovery consumers do not inconsistently acquire/release separate locks.
- Automatic discovery can fail because of guest Wi-Fi, AP/client isolation, multicast filtering, firewall policy, local-network permissions, or OS lifecycle limits; SwiftDrop reports this rather than attempting to bypass network policy.

### Pairing

- Added QR/deep-link `swiftdrop://pair?...` invitations.
- Pairing invitations contain only metadata needed to establish the local TLS connection; they do not contain the device private key.
- Pairing links are short-lived and use cryptographically random base64url one-time nonces.
- Receiver nonces are atomically consumed and cannot silently authorize a second transfer.
- Added nearby pairing request/approval flow using an advertised certificate fingerprint.
- Added short-lived one-time 8-digit codes generated from cryptographically secure random input.
- Added manual local-IP + 8-digit-code fallback for networks where automatic discovery is blocked.
- Manual-IP bootstrap initially connects before the receiver fingerprint is known, but requires a fresh code and receiver approval, captures the exact certificate observed on that TLS connection, requires the returned pairing invitation to contain the same fingerprint, and then still presents the fingerprint for visual confirmation before transfer.

`PairingCodec` now strictly validates untrusted pairing input:

- Maximum overall link size.
- Maximum encoded payload size.
- Bounded JSON depth.
- Exact `swiftdrop` URI scheme.
- Exact `pair` URI host.
- No unexpected outer authority port.
- No unexpected URI path beyond empty/root.
- No fragment.
- No user-info.
- Exactly one `p` query parameter.
- No duplicate payload query parameters.
- No unexpected query parameters.
- Valid bounded base64url payload.
- Exact current protocol version.
- Bounded non-control device ID.
- Bounded non-control display name.
- Numeric local/private/link-local/loopback peer address only.
- Port 1–65535.
- Canonical valid SHA-256 certificate fingerprint.
- Bounded base64url-like nonce characters/length.
- Valid Unix expiry.
- Expiry strictly in the future.
- Maximum pairing invitation lifetime.

Current local-only protocol rejects DNS hostnames and public Internet addresses. Allowed targets are loopback, RFC1918/private IPv4, IPv4 link-local, IPv6 unique-local, and IPv6 link-local.

- Added `swiftdrop` protocol activation routing on Android, iOS, Mac Catalyst, and Windows.
- Pairing-link generation remains intentionally lightweight; validation is enforced when untrusted links are decoded/used.

### TLS and peer authentication

- Uses .NET/platform TLS rather than custom cryptographic algorithms.
- Sender pins the receiver certificate SHA-256 fingerprint learned from the pairing invitation/discovery/bootstrap.
- Sender presents its own client certificate during TLS authentication.
- Receiver requires a sender certificate and derives its fingerprint from the authenticated TLS channel instead of trusting an application JSON field.
- TLS requests TLS 1.3/1.2.
- Certificate revocation lookup is disabled for deliberately self-signed local peer certificates; authorization instead relies on explicit fingerprint pinning, one-time authorization, consent, and local trust decisions.
- Added per-source-address inbound connection attempt limiting before expensive application work.
- Added per-sender-certificate pairing attempt limiting.
- Certificate-specific attempt limiting now applies to pairing requests rather than accidentally throttling legitimate authorized file/text/batch transfers.
- File/text/batch transfer authorization relies on consumed one-time pairing nonces.
- Rate-limiter key cardinality is bounded and stale entries are pruned to avoid unlimited in-memory identity growth.

### Single-file transfer

- File selection uses the platform picker rather than broad storage access.
- Sender builds sanitized filename, size, last-write time, and SHA-256 manifest metadata before transfer.
- Sender validates the final manifest before opening the transfer path.
- Receiver validates metadata, sanitizes filenames, constrains paths beneath the receive root, checks file-size limits, and checks destination free space with a reserve.
- Payload bytes stream directly over TLS in bounded chunks instead of being loaded completely into memory.
- Outgoing streaming is now bound to the manifest-declared source length.
- If the source size changed after hashing/manifest creation, the transfer fails instead of silently sending a different frame length.
- If the source becomes shorter during transfer, an end-of-stream failure is raised.
- Source length is rechecked after streaming to detect size changes during transfer.
- Incomplete receives are staged as `.swiftdrop.part` files.
- Resume offset must be within the declared manifest length.
- Resume offset cannot exceed available staged bytes.
- If a staged partial has an unexpected tail beyond the negotiated offset, it is truncated back to the negotiated offset before receiving more bytes.
- Receiver computes SHA-256 over the completed staged file and uses constant-time hash comparison before finalizing it.
- Invalid checksum staging is deleted and never promoted to a completed file.
- Network frame and payload read/write operations use idle timeouts so peers cannot hold operations open indefinitely without progress.
- Sender validates receiver resume offset and completed-length responses.
- Sender progress, cancellation, pause, resume, failure, and completion states are surfaced in UI/history.
- Pause is implemented by cancelling the active stream and retaining resumable receiver staging; resume requires fresh pairing authorization and reuses the receiver-reported partial offset.
- Retry after a network failure follows the same fresh-pairing/resume-offset model rather than replaying old authorization.

### Atomic receive destination handling

- Added `DestinationReservationSet` for collision-safe atomic reservations before receive writes begin.
- Reservations consider both existing filesystem entries and active in-memory reservations.
- Two simultaneous incoming transfers targeting the same filename cannot both reserve the same not-yet-created final path.
- Collision names are generated deterministically as `name (1).ext`, `name (2).ext`, etc., subject to a bounded attempt count.
- Reservations are released through `IDisposable` after success/failure.
- Batch reservations are held through the batch receive operation and released in a `finally` block.
- Reservation comparisons are case-insensitive on Windows and ordinal elsewhere.

### Filename and path safety

- Rooted paths are rejected.
- `.` and `..` traversal path segments are rejected.
- Unsafe control/portable-invalid filename characters are removed.
- Unicode filenames are normalized to NFC.
- Trailing Windows-incompatible dots/spaces are removed.
- Filename segments are bounded in length.
- Empty-after-sanitation names become `unnamed`.
- Windows reserved device names are prefixed rather than passed through (`CON`, `PRN`, `AUX`, `NUL`, `CLOCK$`, `COM1`–`COM9`, `LPT1`–`LPT9`).
- Receive paths are re-resolved beneath the configured receive root before writes.

### Multi-file and folder transfer

- Added strongly typed batch source and response models.
- Added recursive file/folder manifest construction with SHA-256 per file.
- Added duplicate top-level/relative path deconfliction so two selected files with the same name do not collapse into one manifest path.
- Batch construction now performs complete source preflight before hashing:
  - source exists;
  - source file count within protocol maximum;
  - each file within per-file limit;
  - aggregate bytes within protocol maximum;
  - safe/deconflicted relative path;
  - cancellation honored.
- Expensive hashing starts only after the full source set passes preflight.
- Added central maximum batch file count and maximum aggregate batch byte limits.
- Added reusable core `BatchManifestValidator` for count, per-file metadata, unique paths, declared total, and aggregate size.
- Receiver batch metadata is sanitized, validated through the same core limits, and path-confined before consent.
- Receiver UI supports Accept All, Accept Selected, and Reject decisions.
- Batch receive negotiates accepted source paths and per-file resume offsets before bytes are streamed.
- Accepted batch remainder is summed with checked arithmetic and preflighted against destination capacity before the receiver sends an accept plan.
- Batch destination paths use atomic reservations across concurrent sessions.
- Each accepted file is transferred/staged/verified independently and recorded in local history.
- Sender rejects unknown/duplicate receiver plan paths and out-of-range resume offsets.
- Sender validates each item completion length against its manifest.
- Interrupted batch items can leave resumable partial files; future resume requires fresh pairing.
- Windows can select a folder through the native system FolderPicker and send it recursively.
- Windows desktop drag-and-drop can also supply folders to the same batch source builder.
- Other platforms retain multi-file picker/share workflows rather than requesting broad filesystem access solely to imitate unrestricted folder access.
- Empty directories are not represented in protocol version 1 because the wire format is file-content/relative-path based.

### Text transfer and clipboard behavior

- Added explicit encrypted text-snippet transfer over the same paired TLS path.
- Text snippet UTF-8 size and expiry are bounded.
- Receiver can reject, accept, or accept-and-copy.
- Clipboard is read only when the user explicitly presses the paste action; no continuous clipboard monitoring exists.
- Transfer history stores only metadata describing a text snippet, never snippet contents.
- External text shared/dropped into SwiftDrop is placed in the editor for review rather than sent automatically.

### Receive consent, trust, and file-risk warnings

- Incoming file consent displays sender name, sender certificate fingerprint, filename, declared size, and risk warnings where applicable.
- Added extension-based caution/high-risk classification for common executable, installer, script, archive, disk-image, package, and macro-enabled formats.
- This classifier is intentionally described as a warning aid only, not malware detection.
- SwiftDrop never automatically opens or executes a received file.
- Added local trusted-device SQLite persistence keyed by device ID with exact canonical certificate fingerprint matching.
- Trusted-device storage initialization is serialized to prevent concurrent first-use schema races.
- Added Trust Device / Not Now flow after accepted transfers.
- Added Trusted Devices management page with revoke and clear-all actions.
- Optional auto-accept remains disabled by default and applies only to explicitly trusted certificate matches and normal-risk files/batches.
- Identity reset clears local trust relationships explicitly.
- Automatic local certificate regeneration changes the local device ID rather than pretending the new certificate is the previous trusted identity.

### Queue, concurrency, pause/resume, progress, speed, and ETA

- Added a cancellation-aware asynchronous concurrency gate.
- Added a local transfer queue honoring configured transfer-concurrency limit.
- Single files, batches, and text sends route through the queue.
- Added Queue page and Queue view model showing queued/running/completed/failed/cancelled work.
- Privacy mode hides queue labels and detailed error messages that could expose filenames/paths.
- Added single/batch pause, resume, cancel, and safe retry/resume UX.
- Batch UI reports completed items, transferred bytes, throughput, and estimated remaining time.
- Single-file progress is displayed.
- Receiver-side offset reuse provides restart efficiency after pause/failure.
- Persistent queue authorization is intentionally not implemented with reusable pairing secrets; any future persisted queue must still obtain fresh authorization.

### Receive location and listener lifecycle

- Default receive storage is an application-private `Received` directory.
- Added `ReceiveLocationService` so receive root is resolved centrally instead of hardcoded at each call site.
- Added Windows native FolderPicker integration for a user-selected receive location.
- Unsupported platforms conservatively retain app-private folder rather than requesting broad storage permissions.
- Settings now publish validated change notifications.
- Main page listens specifically for receive-folder changes.
- Active receive root is tracked separately from configured text.
- When the receive root changes:
  - old listener is disposed;
  - listener cancellation propagates to active handlers;
  - active handlers are tracked/drained;
  - new root is resolved;
  - new listener is started;
  - UI displays the active destination.
- A semaphore serializes receive-server replacement/shutdown so multiple lifecycle/settings events do not mutate the listener simultaneously.
- Receive-root path confinement, collision handling, capacity reserve, staged partials, and checksum finalization apply regardless of selected root.

### Android share sheet and lifecycle integration

- Added Android `ACTION_SEND` and `ACTION_SEND_MULTIPLE` inbound handling for text and files.
- Content URIs are copied into app-cache staging with sanitized filenames before entering SwiftDrop transfer selection.
- Share ingestion is bounded and optional; malformed/unavailable shares do not crash startup.
- Staged share-cache files older than 24 hours are pruned at startup.
- Added Android foreground data-sync service lifetime while active user-initiated queued transfers are running.
- Foreground notification content is generic and does not include transferred filenames.
- Android manifest declares required foreground-service/data-sync and notification permissions in addition to local-network permissions.
- Android mDNS uses a reference-counted multicast-lock manager.
- Current source does not claim Android foreground execution removes all vendor battery/network restrictions.

### Windows drag-and-drop and activation

- Windows package manifest registers SwiftDrop protocol activation.
- Windows AppLifecycle routes protocol activations into the bounded external-input inbox.
- Added WinUI desktop drag-and-drop on the application surface.
- Accepted Windows drop formats:
  - storage files;
  - storage folders;
  - text;
  - `swiftdrop://pair` text links.
- Dropped files/folders are passed into `ExternalInputInbox` with the same 2,048-path bound used by other batch intake.
- Folder paths go through the same `BatchTransferSourceBuilder` validation/hashing pipeline as picker-selected folders.
- Dropped text is placed into the text editor for review.
- Dropped pairing links go through strict pairing decoding/fingerprint confirmation.
- No dropped content is automatically transferred.
- Windows drag-and-drop still requires target-platform compile and packaged runtime validation.

### Apple activation integration and remaining Apple platform gap

- iOS and Mac Catalyst handle `swiftdrop://pair` URL activation and pass it into the same bounded external-input inbox.
- Apple Bonjour/local-network declarations are present.
- A dedicated Apple inbound Share Extension target for arbitrary files/text is not currently included.
- Mac Catalyst first-class native file/folder/text drag-and-drop is not currently included.
- These remain genuine source gaps and are listed in `NEXT_STEPS.md` rather than being falsely described as complete.

### External input inbox

- Central external input inbox handles pairing links, shared text, and local file/folder paths.
- Pairing-link input is length/scheme bounded before use.
- Shared text is bounded.
- Shared path count is bounded.
- Shared paths must resolve to an existing file or directory.
- Duplicate paths are suppressed with platform-appropriate path comparison.
- Cached Android share staging has age-based cleanup.
- Main page drains inputs explicitly and presents them for review before transfer.

### MAUI lifecycle/API modernization

- Application startup now uses `CreateWindow(IActivationState?)` rather than assigning deprecated `Application.MainPage`.
- Main window owns a NavigationPage around the singleton MainPage.
- Window destruction unsubscribes external-input events and invokes MainPage async disposal.
- MainPage async disposal:
  - unsubscribes settings events;
  - cancels active single/batch send CTSs;
  - stops the receive server;
  - disposes send CTSs.
- About, Batch Approval, Devices, Diagnostics, History, Settings, and Trusted Devices use MAUI async dialog APIs.
- MainPage partial dialog helpers route legacy-shaped internal calls to async APIs so deprecation suppression is not required.
- Actual .NET MAUI 10 target compilation remains required to validate all platform APIs under warnings-as-errors.

### Settings and appearance

Settings cover:

- Device name.
- Device identity reset.
- Transfer concurrency.
- History retention.
- Privacy mode.
- Trusted-device auto-accept preference.
- Theme: system/light/dark.
- Notifications preference.
- Reduce-motion preference.
- Larger-interface preference.
- Default receive folder where supported.
- Language selection (`en` / `hi`).
- Developer-options toggle.

An `AppearanceService` applies theme, dynamic larger-interface resources, and selected culture. English/Hindi `.resx` catalogs and culture-aware text access exist. Not every page/dialog string is yet converted from hard-coded XAML/code-behind to resources, so full Hindi localization is not claimed complete.

### Accessibility

- Added semantic heading/description metadata to key transfer/navigation surfaces.
- Added dynamic body/control size resources and larger-interface preference.
- Added explicit accessibility validation checklist for TalkBack, VoiceOver, Narrator, keyboard/focus behavior, scaling, contrast, motion, localization, and touch targets.
- UI status/risk meaning is not intended to rely exclusively on color.
- Physical assistive-technology validation is still required before release-level accessibility claims.

### Transfer history and privacy mode

- Added strongly typed transfer-history rows and SQLite persistence.
- Records contain metadata only: direction, peer display name, filename/description, size, timestamp, status, integrity result.
- Privacy mode hides filenames from newly recorded history rows.
- Added retention pruning based on configured days; zero-day retention clears retained history.
- Added per-record history deletion and clear-all behavior.
- Added History view model and observable state.

### Privacy-aware diagnostics and developer self-tests

- Added local bounded diagnostic-event persistence.
- Diagnostic event validation limits IDs, levels, codes, message length, and multiline content.
- Privacy mode redacts email/path-like tokens from diagnostic messages.
- Safe diagnostic export explicitly excludes file/text contents, private keys, pairing nonces, and complete pairing invitations.
- Added clear-log behavior.
- Added developer-only synthetic self-tests using random temporary bytes:
  - known-good transfer round trip;
  - checksum mismatch rejection/cleanup;
  - interrupted receive leaving resumable partial file.
- Self-tests do not inspect user files or connect to external peers.

### SQLite schema management

- Added `DatabaseSchemaManager` and `PRAGMA user_version` schema versioning.
- Version 0 migrates transactionally to schema 1 containing trusted peers, transfer history, diagnostic events, and indexes.
- Unknown future database schema versions are rejected rather than silently corrupted.
- Trusted-device, transfer-history, and diagnostic stores route initialization through shared schema manager.
- Added database migration/future-version tests and schema documentation.
- Files themselves are never placed in SQLite; transfer payload bytes stream to filesystem destinations.

### Protocol/resource hardening

Central protocol safety limits include:

- 64 KiB JSON metadata frame maximum.
- JSON maximum depth.
- 256 KiB file streaming chunks.
- 100 GiB maximum single file.
- 2,048 maximum files in a batch.
- 1 TiB aggregate batch limit.
- bounded UTF-8 text snippet size.
- bounded pairing/text lifetimes.
- 45-second network idle timeout.

Additional validation/hardening includes:

- Frame lengths rejected before allocation when non-positive/oversized.
- Malformed protocol JSON wrapped as invalid protocol data.
- Unknown protocol versions/types rejected.
- Sender identity bounds/control-character checks.
- Strict 64-hex SHA-256 metadata validation.
- Timestamp validation.
- Path traversal/rooted-path rejection and filename sanitation.
- Local-address-only pairing policy.
- Bounded pairing/connection attempt rate limiting.
- Constant-time fingerprint/hash equality where identity/integrity comparisons require it.
- Manifest-bound outgoing file lengths.
- Receiver plan offset/completion validation.
- Aggregate batch validation on both sender and receiver.
- Atomic receive destination reservations.
- Active receive-handler lifetime tracking.

## Tests added or expanded

The portable test project now covers, among other areas:

### Pairing and identity

- Pairing codec round trips/canonicalization.
- Pairing expiry.
- Excessive pairing lifetime.
- Unknown protocol version.
- Public IPv4 rejection.
- Public IPv6 rejection.
- DNS hostname rejection.
- Private/local/link-local address acceptance.
- Strict SHA-256 fingerprint validation.
- Strict nonce validation.
- Duplicate pairing payload query rejection.
- Unexpected query parameter rejection.
- Unexpected outer pairing URI path rejection.
- Explicit unexpected outer authority-port rejection.
- Pairing nonce uniqueness/character bounds.
- One-time 8-digit pairing-code formatting, expiry, and replay rejection.
- Certificate fingerprint normalization/equality.
- Colon-separated fingerprint equality.
- P-256 identity certificate usability policy.
- Missing-private-key rejection.
- Near-expiry renewal classification.
- Expired-certificate rejection.
- Unsupported RSA private-key identity rejection.
- Certificate TLS server/client EKU profile.
- Oversized certificate device-ID rejection.

### Transport and transfer

- Real mutual-TLS loopback connection.
- Exact receiver-certificate pin success.
- Receiver pin mismatch rejection.
- Bootstrap observed-fingerprint capture.
- Full file-byte transfer over real loopback TLS.
- Final SHA-256 equality after TLS transfer.
- Resume from existing staged partial over TLS.
- Source-length mutation rejection.
- Sender offset behavior.
- Receiver staged-tail truncation to negotiated offset.
- Invalid resume offset beyond staged length.
- Integrity mismatch cleanup.
- Interrupted receive partial staging.

### Batch, paths, and collision handling

- Batch source/folder manifests.
- Duplicate top-level source-name deconfliction.
- Empty selection rejection.
- Missing source rejection.
- Cancellation before hashing.
- Shared batch file-count constant.
- Batch manifest count/path/declared-total/aggregate-size validation.
- Destination reservation collision deconfliction.
- Existing destination collision behavior.
- Reservation release/reuse.
- Traversal rejection.
- Portable invalid filename sanitation.
- Windows reserved-device filename sanitation.
- Unicode NFC filename normalization.
- Long filename bounding.

### Storage/application supporting behavior

- Discovery registry deduplication/expiry behavior.
- Transfer-history persistence, deletion, retention, and clearing.
- Trusted-device persistence lifecycle.
- Settings validation.
- File-risk classification.
- Pairing/source attempt rate limiting, independent keys, bounded key cardinality, and expiry.
- Frame protocol serialization plus oversized/zero/negative lengths and malformed JSON boundaries.
- Manifest validation.
- Database schema migration and future-version rejection.
- Diagnostic-event persistence and validation.
- Local/private/public address policy.
- Concurrency-gate queue/release/cancellation behavior.
- Synthetic successful transfer, interrupted receive, and checksum mismatch behavior.

## CI and repository engineering

- Existing CI restores/builds `SwiftDrop.Core` and runs portable unit tests on .NET 10.
- Platform compile workflows exist for Android, Windows, and Mac Catalyst.
- Platform validation workflow is configured on direct main pushes where applicable.
- CodeQL C# analysis is configured.
- Dependabot is configured for NuGet and GitHub Actions.
- Release-readiness workflow includes portable verification and release checks.
- Repository security-hygiene workflow exists.
- Added portable core verification scripts for Unix-like shells and Windows PowerShell.
- Added PR checklist covering tests, security/privacy, permissions, accessibility, and platform impact.
- Added focused issue templates, contributing guide, code of conduct, security policy, privacy policy, support policy, usage terms, third-party notice process, threat model, manual matrix, security test plan, accessibility checklist, release checklist, signing guide, store privacy declaration guide, wire-protocol docs, compatibility docs, clean architecture docs, platform integration status, and local database schema docs.
- `.github/FUNDING.yml` now exposes `https://buymeacoffee.com/sanskarIN` as optional project support.

## Documentation aligned in this continuation

- `README.md` now describes the latest transfer/certificate/pairing/drag-drop hardening and optional support link.
- `SUPPORT.md` includes the Buy Me a Coffee link and states that support is optional/non-privileged.
- `AboutPage` includes an optional Support Development section/button.
- `.github/FUNDING.yml` exposes the support link in GitHub-native funding metadata.
- `CHANGELOG.md` records all continuation hardening.
- `PROJECT_STATUS.md` distinguishes completed source work from external validation.
- `NEXT_STEPS.md` now separates recently completed source work from remaining P0/P1/P2 tasks.
- Old roadmap entries that incorrectly listed Windows drag/drop, receive-root restart, basic TLS loopback coverage, receiver aggregate capacity enforcement, or certificate lifecycle policy as wholly unimplemented were corrected.

## Commit policy used in this work

This project intentionally uses many focused commits rather than one giant commit. Commit messages follow conventional-style prefixes where practical and include:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

The GitHub connector used in this chat does not expose an independent author/committer-email field for Contents API writes. Therefore the requested email is preserved honestly in the Signed-off-by trailer. This ledger does not claim Git author/committer metadata was forcibly rewritten when the connector does not support that operation.

## Security and privacy properties intentionally preserved

- No account is required for current local-transfer workflow.
- SwiftDrop has no application-operated cloud upload path for transfer payloads.
- No advertising identifier collection or analytics pipeline has been added.
- No custom encryption algorithm has been invented.
- Private certificate keys are not committed or placed in pairing links.
- Pairing invitations/codes are temporary authorization factors rather than long-term passwords.
- File/text contents are not stored in SQLite history or diagnostics.
- Clipboard is never continuously monitored.
- Incoming files are never automatically executed/opened.
- Dropped/shared files/text are never automatically transferred.
- Extension-risk warnings are never described as malware detection.
- Public-internet peer targets and DNS hostnames are rejected by current protocol address policy.
- Network/firewall/OS restrictions are respected rather than bypassed.
- No signing keys, API secrets, passwords, tokens, real pairing invitations, or production credentials have been committed.
- Optional financial support does not change security/privacy/access behavior.

## Platform/release verification status

Source implementation is substantially more complete, but repository changes are not equivalent to physical release validation.

- Portable core/tests are covered by GitHub Actions configuration.
- Platform compile workflows are configured for Android, Windows, and Mac Catalyst.
- CodeQL and dependency/security workflows are configured.
- This current execution environment does not provide the full .NET MAUI SDK/workloads needed to compile/sign all targets locally.
- A missing GitHub combined-status result for a just-created Contents API commit is not evidence of success or failure.
- Android foreground-service behavior requires actual Android runtime validation.
- Android multicast-lock behavior requires actual Wi-Fi/device validation.
- Windows drag-and-drop requires WinUI packaged runtime validation.
- Windows receive-folder persistence requires packaged app/runtime validation.
- Windows protocol registration requires packaged install validation.
- Apple local-network/Bonjour behavior requires actual Apple-device validation.
- iOS/Mac Catalyst URL activation requires signed runtime validation.
- SecureStorage identity load/renewal/recovery requires real OS keystore/keychain validation.
- Real firewall, guest Wi-Fi, AP isolation, IPv4/IPv6, sleep/lock, low-storage, network-change, large-file, and interruption scenarios require physical/network testing.
- Store signing, provisioning, notarization, package identity, privacy declarations, screenshots, store metadata, and signed release artifact checks remain release-operations work.

## Genuine remaining source gaps before calling the product fully production-complete

The master prompt has been implemented aggressively, but the following source items remain genuine work or deliberate platform boundaries. They are not hidden behind marketing language.

1. **Apple inbound Share Extension**
   - Android share-sheet ingestion exists.
   - Windows drag-and-drop exists.
   - A dedicated iOS/Mac Catalyst Share Extension target for arbitrary inbound files/text is still not included.

2. **Mac Catalyst first-class drag-and-drop**
   - Windows file/folder/text/pair-link drag-and-drop is implemented in source.
   - Mac Catalyst native drag-and-drop still requires a platform-specific implementation compatible with sandbox/security-scoped URLs.

3. **Full localization wiring**
   - English/Hindi resource infrastructure exists.
   - Not every hard-coded XAML/dialog/status string has been moved into localization resources.
   - Full Hindi localization is therefore not claimed complete.

4. **App-wide MVVM/clean architecture completion**
   - History and Queue have view models.
   - Main transfer dashboard, Devices, Settings, Trusted Devices, Diagnostics, and About still contain meaningful UI orchestration in code-behind.

5. **Optional completion/failure notifications**
   - Android foreground-service notification exists because active foreground data-sync requires it.
   - Optional user-controlled transfer completion/failure notifications are not yet implemented consistently across every target.

6. **Apple/mobile background transfer policy**
   - SwiftDrop does not claim arbitrary background sockets will survive platform suspension.
   - Any additional behavior must follow supported Android/iOS/macOS lifecycle mechanisms and be verified physically.

7. **Application-protocol loopback integration tests**
   - Real TLS/pinning/file/resume loopback foundation now exists.
   - Full UI-independent app-protocol host coverage for authorization consumption, batch selective acceptance, text, cancellation, receive-root restart, and metadata privacy remains to be extracted/expanded.

8. **Additional malformed/fuzz/property testing**
   - Strong boundary tests exist.
   - More systematic truncation, Unicode/case collision, nested JSON, stream-boundary, and concurrent protocol-state testing remains valuable.

9. **Performance benchmarks**
   - Transfer implementation is bounded/streaming.
   - Representative-device hashing/throughput/CPU/memory/large-batch/discovery/SQLite benchmarks are not yet recorded.

10. **Real secure-storage/identity migration scenarios**
    - Certificate policy exists and is unit tested.
    - Real OS restore/migration/keychain/keystore/locked-device/upgrade behavior requires platform testing and may lead to source adjustments.

11. **Platform malware scanning**
    - No unsupported cross-platform malware-scanner claim is made.
    - Extension warnings and transport integrity are implemented; malware safety remains outside cryptographic transfer integrity.

12. **Final dependency/license artifact generation**
    - `THIRD_PARTY_NOTICES.md` documents process/direct dependencies.
    - Final binary notices must be generated/reviewed from the exact restored signed-release dependency graph.

## External validation still required before production-ready claims

These cannot be completed honestly by repository text/source edits alone:

1. Physical Android device testing.
2. Physical iPhone/iPad testing.
3. Physical macOS testing.
4. Physical Windows testing.
5. Cross-device directional transfer matrix.
6. Guest Wi-Fi/client isolation/multicast-blocked testing.
7. IPv4-only and IPv6-capable LAN testing.
8. Windows firewall blocked/allowed behavior.
9. Android vendor battery/background behavior.
10. iOS local-network permission denied/allowed behavior.
11. Network change during active transfer.
12. Device sleep/lock during active transfer.
13. Real low-storage behavior.
14. Multi-gigabyte file behavior.
15. Many-file/folder batch behavior.
16. TalkBack validation.
17. iOS/macOS VoiceOver validation.
18. Windows Narrator validation.
19. Keyboard-only desktop navigation.
20. High-contrast/reduced-motion/large-text validation.
21. Hindi layout/fallback validation.
22. Android release keystore/AAB/APK signing and install/upgrade testing.
23. Windows signing/MSIX install/update testing.
24. Apple Developer signing/provisioning/TestFlight/notarization/store testing.
25. Final store screenshots/metadata/privacy declarations against shipped binaries.

## Next engineering boundary

`NEXT_STEPS.md` is now the detailed prioritized roadmap.

Highest-value remaining source work before a production-tag decision is:

1. Finish Apple Share Extension design/implementation.
2. Add Mac Catalyst native drag-and-drop without weakening sandbox/file-access security.
3. Finish application-wide localization wiring.
4. Continue MVVM/service extraction so complete application-protocol loopback tests can exercise receiver request/authorization/batch/text flows without UI dependencies.
5. Implement optional completion/failure notifications in a permission-minimizing way where supported.
6. Expand malformed/fuzz/property tests.
7. Add synthetic performance benchmarks.
8. Keep release/security/privacy docs synchronized with actual binaries.

After source completion, the mandatory next boundary is not more source claims: it is platform compilation, signed packaging, real-device transfer/accessibility/network validation, exact dependency-license review, and store submission checks.

## Definition used by this ledger

“Implemented in source” means the repository contains the relevant code/tests/docs.

“Validated” means the relevant automated or platform-specific test has actually executed successfully in the correct environment.

“Production-ready” requires successful source gates, target-platform builds, signed package validation, physical-device transfer/network/accessibility tests, accurate privacy/security documentation, exact release dependency review, and store/release checks.

SwiftDrop should not be described as fully production-verified until those external gates are completed.

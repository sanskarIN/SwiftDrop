# What changed

Date: 2026-08-09
Repository: https://github.com/sanskarIN/SwiftDrop

This file is the detailed engineering ledger requested for SwiftDrop. Chat replies are intentionally kept short; implementation details, security notes, verification limits, and remaining release work are recorded here instead.

## Source prompt alignment

Work continues against `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md` and its local-first, account-free, cross-platform .NET MAUI/C# requirements. The repository preserves:

- Apache-2.0 licensing.
- `Made by the Sanskar` branding.
- Project/business email `sanskarin@outlook.in`.
- Support email `supportramsandesh@gmail.com`.
- GitHub repository `https://github.com/sanskarIN/SwiftDrop`.
- GitHub profile `https://www.github.com/sanskarIN` where referenced by the product/about documentation.

## Implementation completed across the project

### Application structure

- Built a .NET 10 solution containing a reusable `SwiftDrop.Core` library, a .NET MAUI `SwiftDrop.App`, and portable xUnit tests.
- Added Android, iOS, Mac Catalyst, and Windows target declarations and platform metadata.
- Added dependency-injection registration for identity, settings, appearance, receive location, queueing, transfer coordination, history, diagnostics, trust, discovery, pairing, self-tests, and application pages/view models.
- Added repository-wide `.editorconfig`, nullable/analysis rules, deterministic builds, CI, issue templates, PR template, contribution/security policies, Dependabot, CodeQL, platform compile workflows, and release/test documentation.
- Began an incremental MVVM refactor rather than leaving every surface entirely in page code-behind. History and Queue now use dedicated observable view models; additional secondary surfaces remain candidates for the same refactor.

### Device identity and secure storage

- Each installation creates a local device ID and self-signed P-256 ECDSA certificate.
- Certificate private-key material is stored through MAUI `SecureStorage`; it is not stored in SQLite, pairing links, diagnostics, or transfer history.
- Device name can be changed independently of cryptographic identity.
- Settings provide an explicit identity-reset workflow that creates a new device ID/certificate and clears local trust relationships rather than silently retaining stale trust.
- Certificate fingerprints are SHA-256 based and are displayed for user verification during pairing/consent flows.

### Discovery

- Added a reusable discovery registry with deduplication, last-seen tracking, expiry, self-filtering, and stable sorting.
- Added mDNS/Bonjour discovery through the Zeroconf library.
- Added bounded UDP IPv4 broadcast fallback with validation and automatic peer expiry.
- Added a MAUI Nearby Devices surface that consumes the discovery service.
- Added Apple Bonjour service declarations and Android multicast permission required by discovery.
- Automatic discovery can fail because of guest Wi-Fi, AP/client isolation, multicast filtering, firewall policy, local-network permissions, or OS lifecycle limits; SwiftDrop reports this rather than attempting to bypass network policy.

### Pairing

- Added QR/deep-link `swiftdrop://pair?...` invitations.
- Pairing invitations contain only the metadata needed to establish the local TLS connection; they do not contain the device private key.
- Pairing links are short-lived and use cryptographically random one-time nonces.
- Receiver nonces are atomically consumed and cannot silently authorize a second transfer.
- Added nearby pairing request/approval flow using an advertised certificate fingerprint.
- Added short-lived one-time 8-digit codes generated without modulo bias.
- Added manual local-IP + 8-digit-code fallback for networks where automatic discovery is blocked.
- Manual-IP bootstrap initially connects before the receiver fingerprint is known, but requires the fresh code and receiver approval, captures the exact certificate observed on that TLS connection, requires the returned pairing invitation to contain the same fingerprint, and then still presents the fingerprint for visual confirmation before transfer.
- Added strict current-protocol pairing metadata validation: version, port, device ID/name bounds, fingerprint format, nonce bounds, expiry/lifetime, and numeric local-address policy.
- Current local-only protocol rejects DNS hostnames and public Internet addresses. Allowed addresses are loopback, RFC1918/private IPv4, IPv4 link-local, IPv6 unique-local, and IPv6 link-local.
- Added `swiftdrop` protocol activation routing on Android, iOS, Mac Catalyst, and Windows.

### TLS and peer authentication

- Uses .NET/platform TLS rather than custom cryptographic algorithms.
- Sender pins the receiver certificate SHA-256 fingerprint learned from the pairing invitation/discovery bootstrap.
- Sender presents its own client certificate during TLS authentication.
- Receiver requires a sender certificate and derives its fingerprint from the authenticated TLS channel instead of trusting an application JSON field.
- TLS requests TLS 1.3/1.2 and disables revocation lookup for the deliberately self-signed local peer certificates; authorization instead relies on explicit fingerprint pinning/consent/trust.
- Added per-source-address inbound connection attempt limiting before expensive application work.
- Added per-sender-certificate pairing attempt limiting.
- Rate-limiter key cardinality is bounded and stale entries are pruned to avoid unlimited in-memory identity growth.

### Single-file transfer

- File selection uses the platform picker rather than broad storage access.
- Sender builds filename, size, last-write time, and SHA-256 manifest metadata before transfer.
- Receiver validates metadata, sanitizes filenames, constrains paths beneath the receive root, checks file-size limits, resolves collisions without silently overwriting existing completed files, and checks destination free space with a reserve.
- Payload bytes stream directly over TLS in bounded chunks instead of being loaded completely into memory.
- Incomplete receives are staged as `.swiftdrop.part` files.
- Receiver computes SHA-256 over the completed staged file and uses constant-time hash comparison before finalizing it.
- Invalid checksum staging is deleted and never promoted to a completed file.
- Network frame and payload read/write operations use an idle timeout so peers cannot hold operations open indefinitely without progress.
- Sender progress, cancellation, pause, resume, failure, and completion states are surfaced in the UI/history.
- Pause is implemented safely by cancelling the active stream and retaining resumable receiver staging; resume requires a fresh pairing authorization and reuses the receiver-reported partial offset.
- Retry after a network failure follows the same fresh-pairing/resume-offset model rather than replaying an old authorization token.

### Multi-file and folder transfer

- Added strongly typed batch source and response models.
- Added recursive file/folder manifest construction with SHA-256 per file.
- Added duplicate top-level/relative path deconfliction so two selected files with the same name do not collapse into one manifest path.
- Added central maximum batch file count and maximum aggregate batch byte limits on sender-side manifest building.
- Added reusable core batch manifest validation for count, per-file metadata, unique paths, declared total, and aggregate size.
- Added receiver batch metadata sanitation/path confinement before consent.
- Added receiver UI with Accept All, Accept Selected, and Reject decisions.
- Batch receive negotiates accepted source paths and per-file resume offsets before bytes are streamed.
- Each accepted file is transferred/staged/verified independently and recorded in local history.
- Existing destination names remain collision-safe.
- Interrupted batch items can leave resumable partial files; future resume requires fresh pairing.
- Windows can select a folder through the native system FolderPicker and send it recursively.
- Other platforms retain multi-file picker/share workflows rather than requesting broad filesystem access solely to imitate a folder picker.
- Empty directories are not represented in protocol version 1 because the wire format is file-content/relative-path based.

### Text transfer and clipboard behavior

- Added explicit encrypted text-snippet transfer over the same paired TLS path.
- Text snippet UTF-8 size and expiry are bounded.
- Receiver can reject, accept, or accept-and-copy.
- Clipboard is read only when the user explicitly presses the paste action; no continuous clipboard monitoring exists.
- Transfer history stores only metadata describing a text snippet, never the snippet content.

### Receive consent, trust, and file-risk warnings

- Incoming file consent displays sender name, sender certificate fingerprint, filename, declared size, and risk warnings where applicable.
- Added extension-based caution/high-risk classification for common executable, installer, script, archive, disk-image, package, and macro-enabled formats.
- This classifier is intentionally described as a warning aid only, not malware detection.
- SwiftDrop never automatically opens or executes a received file.
- Added local trusted-device SQLite persistence keyed by device ID with exact certificate fingerprint matching.
- Added Trust Device / Not Now flow after accepted transfers.
- Added Trusted Devices management page with revoke and clear-all actions.
- Optional auto-accept remains disabled by default and applies only to explicitly trusted certificate matches and normal-risk files/batches.

### Queue, concurrency, pause/resume, progress, speed, and ETA

- Added a cancellation-aware asynchronous concurrency gate.
- Added a local transfer queue service honoring the configured transfer-concurrency limit.
- Single files, batches, and text sends route through the queue.
- Added Queue page and Queue view model showing queued/running/completed/failed/cancelled work.
- Privacy mode hides queue labels and detailed error messages that could otherwise expose filenames/paths.
- Added single/batch pause, resume, cancel, and safe retry/resume UX.
- Batch UI reports completed items, transferred bytes, throughput, and estimated remaining time.
- Single-file progress is displayed; receiver-side offset reuse provides restart efficiency after pause/failure.

### Receive location and storage

- Default receive storage is an application-private `Received` directory.
- Added `ReceiveLocationService` so the receive root is resolved centrally instead of being hardcoded at each call site.
- Added Windows native FolderPicker integration for a user-selected receive/folder-transfer location.
- Unsupported platforms conservatively retain the app-private folder instead of requesting broad storage permissions.
- Receive-root path confinement, collision handling, free-space reserve, staged partial files, and checksum finalization continue to apply regardless of receive root.

### Android share sheet and lifecycle integration

- Added Android `ACTION_SEND` and `ACTION_SEND_MULTIPLE` inbound handling for text and files.
- Content URIs are copied into app-cache staging with sanitized filenames before entering SwiftDrop transfer selection.
- Share ingestion is bounded and optional; malformed/unavailable shares do not crash app startup.
- Staged share-cache files older than 24 hours are pruned at startup.
- Added Android foreground data-sync service lifetime while active user-initiated queued transfers are running.
- Foreground notification content is generic and does not include transferred filenames.
- Android manifest declares the required foreground-service/data-sync and notification permissions in addition to local-network permissions.
- Current source does not claim that Android foreground execution removes all vendor battery/network restrictions.

### Apple and Windows activation integration

- iOS and Mac Catalyst handle `swiftdrop://pair` URL activation and pass it into the same bounded external-input inbox.
- Windows package manifest registers the SwiftDrop protocol and Windows AppLifecycle routes protocol activations into the app.
- A dedicated Apple inbound Share Extension target is not currently included; this remains a genuine platform gap rather than being falsely marked complete.
- Desktop drag-and-drop is also not yet a first-class transfer surface.

### Settings and appearance

Settings now cover:

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

An `AppearanceService` applies theme, dynamic larger-interface resources, and selected culture. Key English/Hindi `.resx` catalogs and a culture-aware text accessor were added. Not every page string is yet converted from hard-coded XAML to resource bindings, so full Hindi localization is not claimed complete.

### Accessibility

- Added semantic heading/description metadata to key transfer/navigation surfaces.
- Added dynamic body/control size resources and a larger-interface preference.
- Added explicit accessibility validation checklist for TalkBack, VoiceOver, Narrator, keyboard/focus behavior, scaling, contrast, motion, localization, and touch targets.
- UI state does not rely exclusively on color for transfer/risk meaning.
- Physical assistive-technology validation is still required before claiming release-level accessibility conformance.

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
  - known-good transfer round-trip;
  - checksum mismatch rejection/cleanup;
  - interrupted receive leaving a resumable partial file.
- Self-tests do not inspect user files or connect to peers.

### SQLite schema management

- Added `DatabaseSchemaManager` and `PRAGMA user_version` schema versioning.
- Version 0 migrates transactionally to schema 1 containing trusted peers, transfer history, diagnostic events, and indexes.
- Unknown future database schema versions are rejected rather than silently corrupted.
- Trusted-device, transfer-history, and diagnostic stores now route initialization through the shared schema manager.
- Added database migration/future-version tests and schema documentation.

### Protocol/resource hardening

Central protocol safety limits now include:

- 64 KiB JSON metadata frame maximum.
- JSON maximum depth.
- 256 KiB file streaming chunks.
- 100 GiB maximum single file.
- 2,048 maximum files in a batch.
- 1 TiB aggregate batch limit.
- 256 KiB maximum UTF-8 text snippet.
- Five-minute pairing/text lifetimes.
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
- Constant-time fingerprint/hash equality where secrets/identity comparisons require it.

## Tests added or expanded

The portable test project now covers, among other areas:

- Pairing codec round trips, expiry, strict fingerprints, and public-address rejection.
- One-time 8-digit pairing-code formatting, expiry, and replay rejection.
- Certificate fingerprint equality behavior.
- Path traversal and filename-safety behavior.
- Discovery registry deduplication/expiry behavior.
- Transfer-history persistence, deletion, retention, and clearing.
- Trusted-device persistence lifecycle.
- Settings validation.
- File-risk classification.
- Pairing/source attempt rate limiting, independent keys, bounded key cardinality, and expiry.
- Frame protocol serialization plus oversized/zero/negative lengths and malformed JSON boundaries.
- Manifest validation.
- Batch source/folder manifests and duplicate source-name deconfliction.
- Batch manifest count/path/total validation.
- Database schema migration and future-version rejection.
- Diagnostic-event persistence and validation.
- Local/private/public address policy.
- Concurrency-gate queue/release/cancellation behavior.
- Synthetic successful transfer, interrupted receive, and checksum mismatch behavior.

## CI and repository engineering

- Existing CI restores/builds `SwiftDrop.Core` and runs portable unit tests on .NET 10.
- Added platform compile workflow for Android, Windows, and Mac Catalyst on pull requests/manual dispatch.
- Added CodeQL C# analysis on main pushes, pull requests, and weekly schedule.
- Added Dependabot configuration for NuGet and GitHub Actions.
- Added PR checklist covering tests, security/privacy, permissions, accessibility, and platform impact.
- Added focused issue templates, contributing guide, code of conduct, security policy, privacy policy, support policy, usage terms, third-party notice process, threat model, manual matrix, security test plan, accessibility checklist, release checklist, wire-protocol docs, platform integration status, and local database schema docs.

## Commit policy used in this work

This project intentionally uses many focused commits rather than one giant commit. Commit messages follow conventional-style prefixes where practical and include:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

The GitHub connector used in this chat does not expose an independent author/committer-email field for Contents API writes. Therefore the requested email is preserved honestly in the Signed-off-by trailer; this ledger does not claim that Git's author/committer metadata was forcibly rewritten when the connector does not support that operation.

## Security and privacy properties intentionally preserved

- No account is required for the current local-transfer workflow.
- SwiftDrop has no application-operated cloud upload path for transfer payloads.
- No advertising identifier collection or analytics pipeline has been added.
- No custom encryption algorithm has been invented.
- Private certificate keys are not committed or placed in pairing links.
- Pairing invitations/codes are temporary authorization factors rather than long-term passwords.
- File/text contents are not stored in SQLite history or diagnostics.
- Clipboard is never continuously monitored.
- Incoming files are never automatically executed/opened.
- Extension-risk warnings are never described as malware detection.
- Public-internet peer targets are rejected by current protocol address policy.
- Network/firewall/OS restrictions are respected rather than bypassed.
- No signing keys, API secrets, passwords, tokens, real pairing invitations, or production credentials have been committed.

## Platform/release verification status

Source implementation is substantially more complete, but repository changes are not equivalent to physical release validation.

- Portable core/tests are covered by GitHub Actions configuration.
- Platform compile workflows are configured for Android, Windows, and Mac Catalyst on PR/manual execution.
- CodeQL workflow is configured.
- A GitHub combined-status query during development may return no statuses for a just-written Contents API commit; absence of a returned status is not evidence of either success or failure.
- This chat environment cannot honestly sign or run Android/iOS/macOS/Windows store packages on physical devices.
- Android foreground-service behavior, Apple local-network permission behavior, Windows protocol/package activation, firewalls, vendor battery management, and background lifecycle behavior require actual target-device testing.
- Store signing, provisioning, notarization, package identity, privacy declarations, screenshots, store metadata, and release artifact checks remain release-operations work.

## Genuine remaining gaps before calling the product fully production-complete

The master prompt is being implemented aggressively, but the following items remain genuine work/verification boundaries and should not be hidden by marketing language:

1. **Receiver aggregate batch enforcement audit:** reusable core aggregate validation now exists and sender-side batch building enforces the total limit. The application receiver path should be kept explicitly wired to the shared validator and cumulative capacity planning during future refactors so malicious peers cannot rely only on sender behavior. Per-file size/free-space checks and declared-total equality are already present.
2. **Apple inbound share extension:** Android share-sheet ingestion exists; a dedicated iOS/Mac share-extension target is not yet included.
3. **Desktop drag-and-drop:** not yet implemented as a first-class SwiftDrop transfer surface.
4. **Full localization wiring:** English/Hindi resource catalogs and culture infrastructure exist, but not every hard-coded page/dialog string has been moved into resources.
5. **App-wide MVVM/clean architecture completion:** History and Queue have view models, while the main transfer dashboard and some settings/device/diagnostics UI still contain substantial code-behind orchestration.
6. **Platform background policy:** Android active user-initiated transfers use a foreground data-sync service. iOS arbitrary background socket transfer is intentionally not claimed; lifecycle-resilient behavior must follow Apple-supported mechanisms and be tested on devices.
7. **Notification preference behavior:** Android's foreground-service notification is required by platform policy when that service is active. Optional transfer completion/failure notifications controlled by the app preference are not yet fully implemented on every target.
8. **Platform malware scanning:** no unsupported cross-platform malware scanning claim is made. Extension warnings and integrity verification are implemented, but malware trust decisions remain outside transport integrity.
9. **Physical accessibility validation:** semantic/sizing work and a checklist exist, but TalkBack/VoiceOver/Narrator/manual keyboard testing is still required.
10. **Physical network matrix:** mDNS/UDP/QR/manual pairing and transfer paths need the documented real-device matrix across Android/iOS/macOS/Windows, routers, guest Wi-Fi, IPv4/IPv6, firewalls, low-storage, large-file, and interruption scenarios.
11. **Release signing/store artifacts:** not produced in this environment.
12. **Final third-party license audit:** `THIRD_PARTY_NOTICES.md` enumerates direct dependencies and the required process, but final binary-release notices must be generated from the exact restored release dependency graph.

## Next engineering boundary

Before a production-tag decision, the highest-value remaining engineering work is to complete the receiver-side aggregate batch/capacity audit, finish app-wide MVVM/localization migration, decide/implement the supported Apple share/background strategy, finish optional notifications without weakening permission minimization, and then execute the documented platform/security/accessibility/manual release matrices on real devices. Only after those checks should signed store artifacts be described as production-validated.

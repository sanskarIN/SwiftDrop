# Architecture

Updated: 2026-08-15

SwiftDrop separates platform/UI interaction from reusable transfer, protocol, security, path, integrity, staging, and metadata policy. Platform APIs remain in application/extension projects; reusable protocol/security/storage logic lives in `SwiftDrop.Core`.

## Projects

- `src/SwiftDrop.App` — .NET MAUI containing app: UI, platform manifests/entitlements, secure device identity integration, receiver lifecycle, pickers, Android share intake, Windows/Mac native drop, Apple App Group import, application services, and view models. Product targets are Android, iOS, Mac Catalyst, and Windows.
- `src/SwiftDrop.Core` — protocol models/factories/validators/authorization, pairing, certificate/fingerprint policy, TLS client/server, discovery, strict framed JSON, transfer engine, hashing, resume policy, path/collision/reparse safety, SQLite schema/storage, external-staging budget policy, source safety, and portable security rules.
- `src/SwiftDrop.ShareExtension` — dedicated **iOS-only** `net10.0-ios` Share Extension. It has no MAUI UI dependency and references `SwiftDrop.Core` for limits/sanitation/package validation policy. It stages user-selected content into the Apple App Group and never performs a peer transfer automatically.
- `tests/SwiftDrop.Core.Tests` — portable protocol/storage/security/transfer/TLS/migration/fuzz/boundary tests.
- `benchmarks/SwiftDrop.Benchmarks` — synthetic bounded benchmark harness using generated temporary data only.

Mac Catalyst external intake is implemented by the containing desktop app and native `UIDropInteraction`; there is no maintained Mac Catalyst Share Extension target.

Canonical solution: `SwiftDrop.slnx`.

## Main UI/MVVM boundary

Dedicated view models own presentation state for:

- Main dashboard → `MainViewModel`;
- History → `HistoryViewModel`;
- Queue → `QueueViewModel`;
- Nearby Devices → `DevicesViewModel`;
- Trusted Devices → `TrustedDevicesViewModel`;
- Diagnostics → `DiagnosticsViewModel`;
- Settings → `SettingsViewModel`;
- About → `AboutViewModel`.

Pages retain work that belongs to the UI/platform boundary: dialogs, navigation, native pickers, share/drop surfaces, clipboard calls, and native activation/lifecycle wiring.

`MainPage` uses partial classes to isolate large platform/interaction concerns, including:

- primary transfer orchestration;
- stable batch pause/resume state;
- external-input review handoff;
- Windows folder picking;
- Mac Catalyst native drop;
- identity recovery notice;
- application lifetime cleanup.

Networking, TLS, cryptography, hashing, SQLite, path policy, and transfer authorization are not implemented inside view models.

## Application services

Important app-layer services include:

- `DeviceIdentityService` — Preferences/SecureStorage integration for local identity.
- `TransferCoordinator` — outgoing protocol orchestration using Core typed wire records and transfer primitives.
- `ReceiveServerService` — incoming TLS/application protocol host plus UI consent callbacks.
- `BatchResumeStateService` — best-effort app bridge to completed-batch metadata introduced in schema v3 and retained in current schema v6, plus Core file revalidation.
- `NearbyDiscoveryService` — lifecycle/composition around Core mDNS/UDP discovery.
- `NearbyPairingService` — nearby/manual pairing using Core TLS/pairing/wire validation.
- `TrustedDevicesService` — certificate-bound trust metadata.
- `TransferHistoryService` — retention/privacy-aware history plus optional schema-v6 completed-transfer duration/actual-byte performance measurements normalized through portable Core policy.
- `TransferQueueService` — bounded concurrency plus privacy-minimal schema-v4 restart status/progress/item persistence; persisted queue context is never reusable transfer authorization.
- `DiagnosticLogService` — bounded privacy-aware diagnostics.
- `ReceiveLocationService` — platform-aware receive-root selection.
- `TransferNotificationService` — optional generic completion/failure notification behavior where implemented.
- `AppleShareContainerImporter` — strict App Group package import on Apple targets where pending iOS Share Extension packages can be surfaced to the containing app.

## Shared typed application protocol

Production sender, pairing client, receiver, and tests use the same Core wire records:

- `ProtocolRequest`;
- `TransferAcknowledgement`;
- `BatchItemStart`;
- `PairingResponse`;
- batch response/plan records.

Core protocol policy is split deliberately:

- `ProtocolRequestFactory` creates validated outgoing request shapes.
- `ProtocolRequestValidator` validates version/type and type-specific fields on incoming requests.
- `ProtocolSessionAuthorizer` validates and atomically consumes transfer authorization.
- `IncomingRequestPolicy` contains shared identity/nonce/code/transfer-ID/item-order rules.
- `TransferResponsePolicy` validates resume/completion/text acknowledgement offsets.
- `FrameProtocol` enforces bounded length, strict UTF-8/JSON, duplicate-property rejection, unknown-member rejection, truncation handling, cancellation, and idle timeouts.

This structure allows full application conversation tests without loading MAUI.

## Authorization and receive-session ordering

For file/batch/text requests:

1. TLS session is accepted.
2. A bounded typed request frame is read.
3. Request schema/type/metadata, including canonical file paths, is validated.
4. Authenticated TLS client certificate must exist.
5. Sender fingerprint is derived from that authenticated certificate.
6. One-time transfer nonce is consumed.
7. Receiver consent/trust policy is evaluated.
8. Transfer plan/data path begins.

Malformed requests, noncanonical manifest paths, and missing sender certificates do not consume authorization. Reused nonces are rejected.

Pair requests follow separate sender-certificate rate limiting, optional one-time code, receiver approval, and pairing-response flow; they do not consume file-transfer nonces.

`AsyncSessionTracker` owns portable active-session tracking/draining. `ReceiveServerService` uses it during listener shutdown/restart so in-flight handlers are cancelled/tracked and drained rather than abandoned.

## Single-file transfer flow

1. Sender validates the selected source as a regular non-link/non-reparse file.
2. Sender builds/validates a manifest with canonical name/path, length, timestamp, and SHA-256.
3. Sender creates a typed `file` request.
4. Receiver validates/authorizes/gets consent.
5. Receiver reserves a collision-safe destination and preflights storage.
6. Receiver negotiates a bounded partial-file resume offset.
7. Sender revalidates the regular source at the stream-open boundary and streams exactly the remaining manifest bytes.
8. Receiver stages to `.swiftdrop.part` in bounded chunks.
9. Receive-root confinement/reparse checks are repeated around directory creation/staging/promotion.
10. Receiver computes SHA-256 of complete staging and compares in constant time.
11. Staging is promoted with non-overwrite semantics only after successful integrity verification.
12. Optional final timestamp metadata is best-effort after verified promotion and cannot turn verified content into a false transfer failure.

## Batch transfer and idempotent resume

A new explicit batch gets a random stable `transferId`. Pause/failure retains that ID; resume/retry reuses it with **fresh pairing authorization**.

The sender rebuilds the source manifest from the current regular/link-safe sources using the same transfer ID. Folder traversal is explicit, bounded, link-safe, deterministic, and canonicalizes protocol relative paths to `/`. Portable case/Unicode/sanitation collisions are deconflicted before hashing. The receiver can negotiate per-file partial offsets.

### Completed-file resume state

The `completed_batch_items` table was introduced in SQLite schema v3 and remains part of current schema v6. After an item is fully verified/finalized, receiver records metadata **before** sending that item's completion acknowledgement:

- stable transfer ID;
- canonical source relative path;
- SHA-256 identity key of normalized receive root;
- effective destination relative path;
- length/hash;
- completion timestamp.

This is not authorization. On retry, `BatchCompletionVerifier` requires:

- same transfer ID;
- same receive-root key;
- same source path;
- same length/hash;
- destination path remains under current receive root;
- no existing symlink/reparse component;
- destination exists at expected length;
- fresh SHA-256 matches.

Only then can the receiver offer `ResumeOffset == Length`. Sender still emits the normal `BatchItemStart`; zero raw payload bytes are needed. **Immediately before** the zero-byte completion acknowledgement, the receiver verifies the completed destination again. Mutation/removal/reparse/root/hash mismatch during the retry-plan-to-ACK interval therefore fails closed.

A brand-new user send receives a new transfer ID, preserving intentional duplicate-send collision semantics.

Resume metadata persistence is best-effort. Failure of that optimization does not change the success of an already verified file transfer.

## Local History performance measurements

SQLite schema v5 adds nullable bounded `duration_ms`; schema v6 adds nullable non-negative `measured_bytes`. Older rows remain null rather than receiving inferred measurements.

The application measures elapsed transfer intervals with monotonic `Stopwatch` timing. Single-file sender history uses the sender-negotiated remaining byte count; receiver single-file and accepted batch-item history uses `length - resume offset`; completed text sends use UTF-8 byte length. Failed, paused, cancelled, rejected, skipped, legacy, zero-byte, or otherwise unattributable events do not become throughput samples.

`TransferPerformanceAnalyzer` is a portable Core policy boundary. It rejects impossible samples (`measured_bytes > size_bytes`, invalid duration, non-completed status), saturates extreme aggregate counters, and computes weighted throughput from total measured bytes divided by total measured duration. Invalid optional measurement input is dropped rather than being allowed to change a successful transfer result.

The History UI presents only valid per-row duration/rate values and a localized weighted summary. Performance metadata remains inside the same History retention/privacy boundary and does not persist peer endpoints, transfer contents, pairing capabilities/nonces, credentials, certificates/private keys, or reusable authorization.

## Restart-safe queue metadata

SQLite schema v4 extends queue persistence with bounded non-secret operation category, update timestamp, progress basis points, and optional total/completed item counts.

The queue architecture deliberately separates status/progress continuity from authorization:

- database labels remain generic `Transfer` rather than persisting source filenames/text;
- normal progress writes are coarsened to 5% buckets plus state/item-count transitions;
- progress is monotonic and bounded to `0..10000` basis points;
- stale `Queued`/`Running` entries become `Interrupted` on restart while retaining safe last-known context;
- interrupted entries are never automatically replayed;
- retry still requires fresh pairing/transfer authorization;
- pairing nonces, reusable session/transfer tokens, certificates/private keys, peer endpoints, source/destination paths, and transfer contents are excluded from queue persistence.

Caller cancellation of an initialization or best-effort metadata write is not treated as database corruption/unavailability; real persistence failures remain isolated so they cannot change the underlying transfer result.

## Canonical path and filesystem safety

Core owns portable path policy:

- `/` is the only protocol manifest separator;
- rooted path rejection, including Windows drive/UNC/device syntax on non-Windows hosts;
- empty/repeated/trailing separator rejection;
- `.` / `..` rejection;
- maximum 64 relative-path segments;
- incoming manifest path must already equal SwiftDrop's canonical sanitized representation before authorization;
- Unicode Form-C filename normalization during canonical source construction;
- invalid/control-character sanitation for locally constructed filenames;
- Windows reserved-device neutralization;
- filename segments bounded by UTF-16 code units and UTF-8 bytes;
- collision names retain bounded unique markers even at the segment limit;
- batch collision checks after portable normalization;
- receive-root lexical confinement;
- existing receive-root symlink/reparse component rejection;
- atomic destination reservations;
- collision-safe final path generation;
- non-overwrite final promotion;
- bounded staged partial resume.

## Cross-platform external-input architecture

All platform intake paths end at `ExternalInputInbox`, and no external input automatically sends.

Core `TransferStagingBudget` centralizes staged file-count, per-file, aggregate-byte, and commit-after-success accounting for Android shares, the iOS Share Extension, and Mac native drop.

### Android

`MainActivity` accepts Android share intents, stages content URIs into bounded per-share cache directories with provider-length/runtime-byte/capacity checks, shared staging-budget accounting, portable sanitation, exact staged-length validation, and cleanup on failure, then performs one atomic inbox handoff. Unknown/negative provider sizes are treated as unknown and runtime bytes are capped to remaining aggregate budget while storage reserve is rechecked during streaming.

### Windows

WinUI protocol activation and native drag/drop provide explicit paths/text/pairing links to the inbox. Actual files/folders still go through normal regular-source/link-safe manifest/hash validation before send. WinUI activation/drag types and WinRT data-package operations are explicitly qualified to avoid namespace collisions.

Focused hosted Windows compilation uses a single-TFM override and skips the iOS extension restore edge. It also uses `WindowsPackageType=None` so source/XAML/WinUI compilation is not conflated with signed MSIX packaging. Signed MSIX creation/install/update remains a separate release gate.

### Mac Catalyst drop

A `UIDropInteraction` is attached to the MAUI native host view. File/folder representations are copied while security-scoped access is valid, with symlink rejection, shared count/per-file/aggregate staging budget, bounded provider-response waits, capacity checks, and portable collision-safe staging.

### iOS Share Extension

`SwiftDrop.ShareExtension` processes bounded provider representations, copies them while access is valid, validates a Core `ExternalSharePackageManifest`, and atomically moves a package from `.staging-*` to `pending-*` inside App Group:

`group.in.sanskar.swiftdrop`

Provider response waits are bounded; once a provider responds and a legitimate local copy starts, the response timer is not misused as a file-copy timeout. Extension-lifetime cancellation still bounds active work.

The containing app later imports a package through `AppleShareContainerImporter`, using strict/unmapped-member-rejecting JSON validation, age/path/size/symlink checks, exact physical file-set validation, aggregate app-cache preflight, and app-cache re-staging before a single review-inbox event. One pending package is surfaced for review at a time; later packages are retained rather than silently merged/deleted.

The Share Extension never receives SwiftDrop private keys or reusable transfer authorization and never starts a peer transfer.

## Local database architecture

Current SQLite schema version: **6**.

Metadata tables cover:

- trusted peers;
- transfer history with optional bounded duration and attributable measured-byte metadata for valid completed-transfer performance samples;
- bounded diagnostics;
- privacy-minimal restart-safe queue status/progress/item metadata;
- verified completed-batch resume metadata.

Transfer file/text contents, pairing capabilities/nonces, reusable transfer authorization, queue peer endpoints, private keys, queue source/destination paths, source absolute paths, and receive-root absolute paths for completion reuse are not persisted in SQLite.

See `docs/storage/database-schema.md`.

## Localization/accessibility

English and Hindi use `.resx` catalogs. XAML resolves localized values through shared app text/localize infrastructure. `LocalizeExtension` is marked service-provider-independent for XAML compilation. Runtime strings used by major transfer/history/queue/devices/trust/diagnostics/settings/consent surfaces have catalog equivalents.

CI validates:

- XML well-formedness;
- non-empty values;
- duplicate keys;
- exact English/Hindi key parity;
- format-placeholder parity.

Physical layout/screen-reader validation remains a release step.

## Build/release architecture

The application currently uses .NET 10 with `Microsoft.Maui.Controls` 10.0.90. Portable verification runs Core tests, localization validation, Apple App Group/iOS Share Extension metadata validation, and benchmark compilation.

Apple integration validation statically checks:

- app/iOS extension App Group consistency;
- bundle IDs and version/build parity;
- iOS extension target and entitlements wiring;
- Mac Catalyst containing-app sandbox/App Group wiring;
- extension point/principal class/activation bounds;
- iOS project reference/`IsAppExtension` metadata;
- Core App Group constant;
- canonical solution inclusion.

Apple CI builds the Mac Catalyst containing app, then performs certificate-independent iOS Simulator restore/build of the iOS Share Extension and containing app. The project files retain their real entitlements for signed/device builds.

Android CI compiles the Release app target.

Windows CI compiles a focused unpackaged Windows target; signed MSIX packaging remains release-validation evidence rather than a hosted source-compile claim.

## Verification boundary

Source implementation and hosted compilation are not production certification. Release readiness still requires observed successful CI on the exact candidate, signed package/extension installation, Apple App Group provisioning, signed Windows MSIX validation, physical peer/network/provider/resume/filesystem tests, accessibility/localization validation, dependency-license review, and store-policy checks.


## History performance trend derivation and export

The performance trend is a derived read model, not a new persistence model. `TransferHistoryStore.GetPerformanceSamplesSinceAsync` selects all retained valid completed measurements at/after a UTC cutoff without the normal recent-row UI limit, but its SQL projection contains only `timestamp_utc`, `size_bytes`, `duration_ms`, and `measured_bytes`. History row IDs, direction, peer/device names, filenames, paths, endpoints, and authorization data are therefore never materialized into the trend pipeline.

`TransferPerformanceSample` is the identifier-free Core handoff model. `TransferPerformanceTrendAnalyzer` groups valid samples by UTC calendar date, excludes samples later than the exact UTC window end even when they share that calendar date, and uses actual `measured_bytes` plus `duration_ms` to compute weighted daily throughput.

`TransferPerformanceTrendCsvExporter` serializes only aggregate date/count/byte/duration/rate fields with invariant formatting. `TransferHistoryService` writes the derived CSV to app cache on explicit request, best-effort deletes older matching cached exports, and `HistoryPage` hands the file to the OS share sheet. Clearing History or configuring zero-day History retention also best-effort removes SwiftDrop-owned cached trend exports. No new SQLite table, cloud telemetry path, peer endpoint, row identifier, file/device metadata, or reusable authorization is introduced.

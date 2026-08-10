# Architecture

SwiftDrop separates platform/UI interaction from reusable transfer, protocol, security, path, integrity, and metadata policy. Platform APIs remain in application/extension projects; reusable protocol/security/storage logic lives in `SwiftDrop.Core`.

## Projects

- `src/SwiftDrop.App` — .NET MAUI containing app: UI, platform manifests/entitlements, secure device identity integration, receiver lifecycle, pickers, Android share intake, Windows/Mac native drop, App Group import, application services, and view models.
- `src/SwiftDrop.Core` — protocol models/factories/validators/authorization, pairing, certificate/fingerprint policy, TLS client/server, discovery, strict framed JSON, transfer engine, hashing, resume policy, path/collision/reparse safety, SQLite schema/storage, and portable security rules.
- `src/SwiftDrop.ShareExtension` — iOS/Mac Catalyst Share Extension. It has no MAUI UI dependency and references `SwiftDrop.Core` for limits/sanitation/package validation policy. It stages user-selected content into the Apple App Group; it never performs a peer transfer automatically.
- `tests/SwiftDrop.Core.Tests` — portable protocol/storage/security/transfer/TLS/migration/fuzz/boundary tests.
- `benchmarks/SwiftDrop.Benchmarks` — synthetic bounded benchmark harness using generated temporary data only.

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
- `BatchResumeStateService` — best-effort app bridge to schema-v3 completed-batch metadata and Core file revalidation.
- `NearbyDiscoveryService` — lifecycle/composition around Core mDNS/UDP discovery.
- `NearbyPairingService` — nearby/manual pairing using Core TLS/pairing/wire validation.
- `TrustedDevicesService` — certificate-bound trust metadata.
- `TransferHistoryService` — retention/privacy-aware history.
- `TransferQueueService` — bounded concurrency plus privacy-minimal restart status persistence.
- `DiagnosticLogService` — bounded privacy-aware diagnostics.
- `ReceiveLocationService` — platform-aware receive-root selection.
- `TransferNotificationService` — optional generic completion/failure notification behavior where implemented.
- `AppleShareContainerImporter` — strict App Group package import on Apple targets.

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
- `FrameProtocol` enforces bounded length, strict JSON, duplicate-property rejection, unknown-member rejection, truncation handling, cancellation, and idle timeouts.

This structure allows full application conversation tests without loading MAUI.

## Authorization and receive-session ordering

For file/batch/text requests:

1. TLS session is accepted.
2. a bounded typed request frame is read;
3. request schema/type/metadata is validated;
4. authenticated TLS client certificate must exist;
5. sender fingerprint is derived from that authenticated certificate;
6. one-time transfer nonce is consumed;
7. receiver consent/trust policy is evaluated;
8. transfer plan/data path begins.

Malformed requests and missing sender certificates do not consume authorization. Reused nonces are rejected.

Pair requests follow separate sender-certificate rate limiting, optional one-time code, receiver approval, and pairing-response flow; they do not consume file-transfer nonces.

`AsyncSessionTracker` owns portable active-session tracking/draining. `ReceiveServerService` uses it during listener shutdown/restart so in-flight handlers are cancelled/tracked and drained rather than abandoned.

## Single-file transfer flow

1. Sender builds/validates a manifest with canonical name/path, length, timestamp, and SHA-256.
2. Sender creates a typed `file` request.
3. Receiver validates/authorizes/gets consent.
4. Receiver reserves a collision-safe destination and preflights storage.
5. Receiver negotiates a bounded partial-file resume offset.
6. Sender streams exactly the remaining manifest bytes.
7. Receiver stages to `.swiftdrop.part` in bounded chunks.
8. Receive-root confinement/reparse checks are repeated around directory creation/staging/promotion.
9. Receiver computes SHA-256 of complete staging and compares in constant time.
10. Staging is promoted with non-overwrite semantics only after successful integrity verification.

## Batch transfer and idempotent resume

A new explicit batch gets a random stable `transferId`. Pause/failure retains that ID; resume/retry reuses it with **fresh pairing authorization**.

The sender rebuilds the source manifest from the current sources using the same transfer ID. The receiver can negotiate per-file partial offsets.

### Completed-file resume state

SQLite schema v3 contains `completed_batch_items`. After an item is fully verified/finalized, receiver records metadata **before** sending that item's completion acknowledgement:

- stable transfer ID;
- source relative path;
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

Only then can the receiver offer `ResumeOffset == Length`. Sender still emits the normal `BatchItemStart`; zero raw payload bytes are needed; receiver sends the normal full-length item acknowledgement. If verification fails, stale metadata is removed and the item follows normal transfer/collision behavior.

A brand-new user send receives a new transfer ID, preserving intentional duplicate-send collision semantics.

Resume metadata persistence is best-effort. Failure of that optimization does not change the success of an already verified file transfer.

## Filesystem safety

Core owns portable path policy:

- rooted path rejection, including Windows drive/UNC/device syntax on non-Windows hosts;
- `/` and `\\` traversal separator normalization;
- `.` / `..` rejection;
- Unicode Form-C filename normalization;
- invalid/control-character sanitation;
- Windows reserved-device neutralization;
- batch collision checks after portable normalization;
- receive-root lexical confinement;
- existing receive-root symlink/reparse component rejection;
- atomic destination reservations;
- collision-safe final path generation;
- non-overwrite final promotion;
- bounded staged partial resume.

## Cross-platform external-input architecture

All platform intake paths end at `ExternalInputInbox`, and no external input automatically sends.

### Android

`MainActivity` accepts Android share intents, stages content URIs into bounded cache with provider-length/runtime-byte/capacity checks and portable sanitation, then performs one atomic inbox handoff.

### Windows

WinUI protocol activation and native drag/drop provide explicit paths/text/pairing links to the inbox. Actual files/folders still go through normal source manifest/hash validation before send.

### Mac Catalyst drop

A `UIDropInteraction` is attached to the MAUI native host view. File/folder representations are copied while security-scoped access is valid, with symlink rejection, count/aggregate bounds, capacity checks, and portable collision-safe staging.

### Apple Share Extension

`SwiftDrop.ShareExtension` processes bounded provider representations, copies them while access is valid, validates a Core `ExternalSharePackageManifest`, and atomically moves a package from `.staging-*` to `pending-*` inside App Group:

`group.in.sanskar.swiftdrop`

The containing app later imports a package through `AppleShareContainerImporter`, using strict/unmapped-member-rejecting JSON validation, age/path/size/symlink checks, then re-stages accepted files into ordinary app cache before sending a single review-inbox event.

The Share Extension never receives SwiftDrop private keys or reusable transfer authorization and never starts a peer transfer.

## Local database architecture

SQLite schema v3 stores metadata-only tables for:

- trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal queue status;
- verified completed-batch resume metadata.

Transfer file/text contents, pairing capabilities, private keys, source absolute paths, and receive-root absolute paths for completion reuse are not persisted in SQLite.

See `docs/storage/database-schema.md`.

## Localization/accessibility

English and Hindi use `.resx` catalogs. XAML resolves localized values through shared app text/localize infrastructure. Runtime strings used by major transfer/history/queue/devices/trust/diagnostics/settings/consent surfaces have catalog equivalents.

CI validates:

- XML well-formedness;
- non-empty values;
- duplicate keys;
- exact English/Hindi key parity;
- format-placeholder parity.

Physical layout/screen-reader validation remains a release step.

## Build/release architecture

Portable verification runs Core tests, localization validation, Apple App Group/Share Extension metadata validation, and benchmark compilation.

Apple integration validation statically checks:

- app/extension App Group consistency;
- bundle IDs and version/build parity;
- sandbox/entitlements wiring;
- extension point/principal class/activation bounds;
- project reference/`IsAppExtension` metadata;
- Core App Group constant;
- canonical solution inclusion.

Apple CI/release jobs explicitly build both Share Extension and containing app for Mac Catalyst and unsigned iOS Simulator targets.

## Verification boundary

Source implementation is not production certification. Release readiness still requires observed successful CI on the exact candidate, signed package/extension installation, Apple App Group provisioning, physical peer/network/resume tests, accessibility/localization validation, dependency-license review, and store-policy checks.

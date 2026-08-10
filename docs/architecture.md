# Architecture

Updated: 2026-08-10

SwiftDrop separates platform interaction/presentation from local transfer, protocol, security, storage, and discovery logic. The repository favors reusable Core policies for security-sensitive validation and keeps platform-specific code as thin adapters where possible.

## Projects

- `src/SwiftDrop.App` — .NET MAUI application, pages/view models, platform manifests/entitlements, secure device identity service, QR/picker/share/document activation, receive-server lifecycle, Android foreground integration, Windows drag/drop, and Apple security-scoped file-URL staging adapter.
- `src/SwiftDrop.Core` — protocol models/policies, pairing codec, strict JSON validation, certificate fingerprinting, TLS client/server, discovery, transfer framing/engine, manifests, path/file safety, one-time authorization, portable external-file staging, and metadata stores.
- `tests/SwiftDrop.Core.Tests` — portable protocol/security/storage/discovery/transfer tests.
- `benchmarks/SwiftDrop.Benchmarks` — synthetic hashing/manifest/path benchmark harness using generated temporary data only.

The canonical solution file is `SwiftDrop.slnx`.

## Presentation and MVVM boundary

Dedicated view models currently back:

- Main transfer presentation state;
- History;
- Transfer Queue;
- Nearby Devices;
- Trusted Devices;
- Diagnostics;
- Settings;
- About.

`MainViewModel` owns only presentation state such as:

- local device name/ID/fingerprint text;
- active receive-folder text;
- selected peer display;
- selected file/batch display;
- transfer/batch/text status;
- single/batch progress;
- send/pause/resume/cancel enabled state.

The following deliberately remain outside view models:

- file/folder pickers;
- QR image generation/rendering;
- clipboard/share APIs;
- modal consent/confirmation dialogs;
- navigation;
- receive-server lifetime;
- TLS/socket operations;
- filesystem transfer operations;
- certificate/private-key operations.

This keeps MAUI/platform UI concerns out of Core while avoiding view models that directly own cryptography/networking/filesystem code.

## Application services

Key app services include:

- `DeviceIdentityService` — local device ID/name and secure-storage certificate lifecycle; uses the Core one-time authorization store for active pairing nonces.
- `TransferCoordinator` — outgoing file/batch/text orchestration over pinned TLS; delegates request/response validation to Core policies.
- `ReceiveServerService` — listener/session orchestration, authenticated sender extraction, consent callbacks, destination reservation, storage preflight, and transfer dispatch; delegates envelope/identity/transfer-ID/batch-order validation to Core.
- `TransferQueueService` — concurrency gating plus privacy-minimal restart metadata.
- `TransferHistoryService` — retention/privacy-aware history facade.
- `DiagnosticLogService` — bounded privacy-aware diagnostic persistence/export.
- `TrustedDevicesService` — app facade for exact device-ID + canonical certificate-fingerprint trust.
- `NearbyDiscoveryService` / `NearbyPairingService` — discovery and pairing orchestration.
- `ReceiveLocationService` — active receive-root selection/validation.
- `ExternalInputInbox` — bounded handoff point for platform share/drop/open activation.
- `AppleExternalFileStager` — thin Apple security-scoped adapter that delegates actual copying/sanitation/cleanup to Core `ExternalFileStager`.

## Core security/protocol policies

Reusable Core validation avoids parallel ad-hoc checks:

- `StrictJsonGuard` — rejects malformed/ambiguous JSON including duplicate property names before protocol deserialization.
- `PairingCodec` — strict local-only invitation decode/validate with strict decoded JSON guard.
- `IncomingRequestPolicy` — protocol version/type, sender identity, transfer ID and negotiated batch item ordering.
- `TransferResponsePolicy` — sender-side resume/completion/text acknowledgement contracts.
- `BatchManifestValidator` — incoming batch count/per-file/aggregate/path metadata rules.
- `BatchTransferPlanValidator` — sender-side validation of receiver selection/resume plans.
- `ManifestValidator` — per-file metadata bounds/hash/timestamp validation.
- `OneTimeAuthorizationStore` — bounded exact-expiry atomic consume/replay rejection.
- `AttemptRateLimiter` — bounded attempt windows/cardinality.
- `FileNameSanitizer` / `PathGuard` / `DestinationReservationSet` — portable filename sanitation, receive-root confinement and concurrent destination collision reservation.
- `DiagnosticPrivacyRedactor` — privacy-mode redaction of identifiers in diagnostic text.

## Transfer flow

### Pairing/authorization

1. Receiver creates a short-lived pairing payload containing LAN address, port, certificate SHA-256 fingerprint, expiration and cryptographically random nonce.
2. The nonce is registered in a bounded in-memory one-time authorization store with exact expiration precision.
3. Sender decodes/validates the invitation, including strict JSON duplicate-property protection and local/private numeric address policy.
4. Sender visually confirms/pins the receiver certificate fingerprint.
5. Sender connects through TLS and presents its local client certificate.
6. Receiver derives sender fingerprint from the authenticated TLS channel, not application JSON.
7. File/batch/text authorization atomically consumes the one-time nonce. Reuse is rejected.

### Single-file transfer

1. Sender validates the source and constructs a sanitized manifest with size/hash/timestamp.
2. Sender revalidates the pairing payload at the actual send boundary.
3. Sender opens pinned mutual TLS and submits the file request.
4. Receiver validates request envelope/sender identity/file metadata and authorization.
5. Receiver requests explicit consent unless conservative trusted-device policy applies.
6. Receiver reserves a collision-safe destination, validates capacity, and returns a bounded resume offset.
7. Sender validates that response through `TransferResponsePolicy` and streams exactly the manifest length from the negotiated offset.
8. Receiver writes `.swiftdrop.part`, validates exact length and SHA-256, then atomically promotes the completed file.
9. Receiver returns exact completion length; sender validates it.
10. Local history stores metadata only.

### Batch transfer

1. Sender performs complete source/count/size/path preflight before hashing.
2. Sender sends the bounded batch manifest.
3. Receiver sanitizes/validates manifest and presents accept-all/selective/reject consent.
4. Receiver reserves all selected destinations and performs aggregate remaining-capacity preflight.
5. Receiver returns one plan item for each source.
6. Sender validates the complete plan through `BatchTransferPlanValidator`.
7. Sender transmits accepted files in the negotiated order; receiver validates each item-start path.
8. Every item is staged/verified independently.
9. Final aggregate completion length is validated by the sender.

### Text transfer

1. Sender validates UTF-8 byte size and short expiration.
2. Sender uses the same pinned TLS + one-time authorization path.
3. Receiver shows explicit consent and optionally copies only after user choice.
4. Sender requires an accepted acknowledgement with zero offset.
5. Text content is never persisted in transfer history/SQLite.

## Data storage

SwiftDrop does not store user file/text payload contents in SQLite.

SQLite metadata currently includes:

- trusted peer metadata;
- transfer history metadata;
- bounded diagnostic events;
- restart-safe privacy-minimal queue metadata;
- schema version metadata.

Security/privacy properties:

- trusted fingerprints are canonicalized/validated at the Core storage boundary;
- malformed persisted trust/history/diagnostic rows are ignored rather than becoming implicit trust or crashing complete lists;
- privacy mode writes a language-neutral private marker for peer/file history labels and redacts older rows when read;
- diagnostic privacy mode structurally redacts IPs/endpoints, GUIDs, fingerprints, paths, emails and pairing URIs;
- queue persistence never stores source paths, text contents, peer addresses, pairing nonces/codes, credentials/private keys or free-form exception messages.

Device certificate/private-key material remains in MAUI platform secure storage, not SQLite.

## External platform input

All platform input should converge on normal review/authorization rather than direct-send shortcuts:

- Android share intents → bounded app cache → `ExternalInputInbox`;
- Windows native drag/drop → `ExternalInputInbox`;
- iOS/Mac Catalyst document/open-file URL → temporary security-scoped access → Core `ExternalFileStager` → `ExternalInputInbox`;
- `swiftdrop://pair` activation → `ExternalInputInbox` pairing link.

Shared/opened/dropped content is presented for review and is never automatically transmitted.

A dedicated Apple Share Extension and a first-class native Mac Catalyst drop surface are not currently part of the source and must not be conflated with document/open-file URL handling.

## Platform lifecycle

- Android may keep an active user-initiated transfer in a foreground data-sync service according to Android policy.
- iOS/Mac Catalyst do not claim unrestricted indefinite background socket continuation.
- Windows/macOS firewall/sandbox lifecycle is respected rather than bypassed.
- `Application.CreateWindow` owns app window creation; window destruction triggers active transfer/receiver cleanup.

## Validation boundary

Portable Core tests and configured GitHub Actions are not equivalent to production validation. The exact release candidate still requires:

- successful Actions/build/test evidence;
- physical cross-device transfer matrix;
- restricted-network/firewall/local-network-permission cases;
- low-storage/network-change/sleep-lock cases;
- accessibility validation;
- signed platform packaging/install/update;
- Apple security-scoped/sandbox document-provider validation;
- store policy/privacy review.

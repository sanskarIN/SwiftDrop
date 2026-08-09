# Architecture

SwiftDrop separates platform/UI interaction from reusable transfer, protocol, security, and metadata logic. Platform APIs remain in the MAUI application project; reusable protocol and storage policy lives in `SwiftDrop.Core`.

## Projects

- `src/SwiftDrop.App` — .NET MAUI UI, platform manifests, secure device identity integration, QR pairing presentation, receive-server lifecycle, system picker/share/drag-drop integration, application services, and view models.
- `src/SwiftDrop.Core` — protocol models, pairing codec, certificate/fingerprint policy, TLS client/server, discovery primitives, strict framed JSON, chunked transfer engine, hashing, path/collision safety, SQLite schema/storage, and portable security rules.
- `tests/SwiftDrop.Core.Tests` — portable protocol, storage, security, transfer, TLS-loopback, migration, and boundary tests.
- `benchmarks/SwiftDrop.Benchmarks` — bounded synthetic benchmark harness. It generates temporary data and does not inspect user files or contact peers.

The canonical repository solution is `SwiftDrop.slnx`.

## UI/MVVM boundary

The following surfaces use dedicated view models for state:

- History → `HistoryViewModel`.
- Queue → `QueueViewModel`.
- Nearby Devices → `DevicesViewModel`.
- Trusted Devices → `TrustedDevicesViewModel`.
- Diagnostics → `DiagnosticsViewModel`.
- Settings → `SettingsViewModel`.
- About → `AboutViewModel`.

Pages retain interaction that belongs to the UI/platform boundary, such as confirmation dialogs, navigation, system share sheets, file/folder pickers, and link launching. View models call application services rather than implementing TLS, storage, or cryptography themselves.

`MainPage` remains an incremental migration target because it coordinates several short-lived UI concerns at once: current pairing invitation, selected local sources, pause/resume/cancel controls, modal receiver consent, and receive-server lifecycle. Its transfer/network work is already delegated to services, but its presentation state is still largely named-control/code-behind state. That distinction is intentional and documented rather than calling the MVVM migration complete prematurely.

## Application services

Important app-layer services include:

- `DeviceIdentityService` — platform preferences/SecureStorage integration for local identity.
- `TransferCoordinator` — outgoing application-protocol orchestration using Core TLS/transfer primitives.
- `ReceiveServerService` — incoming application-protocol orchestration and user-consent callbacks.
- `NearbyDiscoveryService` — app lifecycle/composition around mDNS/DNS-SD and UDP discovery.
- `NearbyPairingService` — nearby/manual pairing flow using Core TLS/pairing validation.
- `TrustedDevicesService` — app access to certificate-bound trust metadata.
- `TransferHistoryService` — app access to local retention-aware history.
- `TransferQueueService` — bounded concurrency plus privacy-minimal restart status persistence.
- `DiagnosticLogService` — bounded privacy-aware diagnostic metadata.
- `ReceiveLocationService` — platform-aware receive-root selection.
- `TransferNotificationService` — optional privacy-safe completion/failure notification behavior where implemented.

Services are registered through MAUI dependency injection. Networking/storage/cryptography are not created ad hoc in view models.

## Pairing and transfer flow

1. Receiver creates a short-lived pairing payload containing numeric LAN address, port, certificate SHA-256 fingerprint, expiration, and random one-time nonce.
2. Sender validates the payload and connects through TLS 1.2/1.3 using the platform/.NET stack.
3. Sender pins the receiver certificate fingerprint from the pairing payload and presents its own local certificate.
4. Sender presents one-time authorization in the first bounded application request.
5. Receiver consumes authorization atomically and evaluates sender certificate identity plus explicit/trusted-device consent policy.
6. File/batch/text metadata is validated before payload bytes are accepted.
7. For files, receiver negotiates a bounded resume offset for compatible `.swiftdrop.part` staging.
8. File bytes stream in bounded chunks directly to disk.
9. Receiver computes SHA-256 over the complete staged file.
10. Only after successful integrity verification is the staged file finalized under the approved receive root.

Batch paths additionally pass portable normalization/collision checks, aggregate limits, and free-space preflight.

## Strict protocol metadata

Application protocol frames are length-prefixed and parsed under a shared strict JSON policy:

- bounded frame length before allocation;
- bounded JSON depth;
- invalid UTF-8/JSON rejection;
- comments/trailing commas rejected;
- duplicate object property names rejected case-insensitively, including nested values;
- truncated frames fail;
- idle timeouts and caller cancellation are enforced.

Pairing URI/payload validation is separately bounded. Reusing the shared duplicate-property guard inside the encoded pairing JSON remains a tracked defensive hardening item because that specific source replacement was blocked by the repository connector during the current implementation session; existing pairing field/URI validation remains active.

## Filesystem safety

Core owns portable filename/path policy:

- Unicode Form-C normalization;
- invalid/control-character removal;
- Windows reserved-device name neutralization;
- rooted/traversal path rejection;
- receive-root confinement;
- platform-aware case comparison;
- batch collision rejection after sanitation/normalization/case folding;
- active destination reservations for concurrent receives;
- collision-safe final names;
- bounded resume staging.

Platform intake paths such as pickers, Android share-sheet staging, and Windows drag/drop are routed into shared validation rather than bypassing it.

## Local data storage

SwiftDrop does not store transferred file/text contents in SQLite. Transfer bytes stream directly between network and filesystem. Current schema version 2 stores metadata-only tables for:

- trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal queue status.

Queue persistence stores generic state/timestamps and bounded machine-oriented error codes only. It does not persist filenames/source paths, text, peer IP addresses, pairing invitations/nonces, credentials, or reusable authorization. Stale queued/running rows become `Interrupted` after restart and are not silently retried.

Device certificate/private-key material is kept through platform secure storage rather than SQLite.

## Localization

English and Hindi resources use shared `.resx` catalogs. XAML resolves resource keys through `AppText`/`LocalizeExtension`. CI validates XML well-formedness, non-empty values, duplicate keys, and exact English/Hindi key parity. Saved culture is applied before `MainPage` is resolved at startup.

## Verification boundary

Portable tests and CI are source-level evidence, not a production certification. Release readiness additionally requires target-platform compilation, signed-package installation/upgrades, physical-device peer transfers, restricted-network/low-storage cases, accessibility validation, platform secure-storage lifecycle testing, and store-policy review.

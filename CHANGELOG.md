# Changelog

## Unreleased - 2026-08-10

### Apple platform integration

- Added a dedicated `SwiftDrop.ShareExtension` project targeting iOS and Mac Catalyst.
- Added shared App Group `group.in.sanskar.swiftdrop` to containing app and Share Extension entitlements.
- Added strict versioned App Group package manifests validated by `SwiftDrop.Core`.
- Added atomic Share Extension package publication from `.staging-*` to `pending-*`.
- Added containing-app App Group importer with strict JSON, unknown-member rejection, package-age bounds, canonical path/name checks, symlink/reparse rejection, exact-length validation, stale staging cleanup, and app-cache re-staging.
- Added bounded Share Extension provider intake for files/images/movies/text/web URLs with security-scoped access, storage preflight, exact-length staging, cancellation, and review-before-send behavior.
- Added native Mac Catalyst `UIDropInteraction` for files, folders, text, and pairing links.
- Added Mac drop count/per-file/aggregate bounds, security-scoped staging, symlink rejection, portable filename sanitation, and collision-safe directory/file deconfliction.
- Added Apple project/entitlement/version/App Group consistency validator to portable CI and release checks.
- Added explicit Share Extension compile gates for Mac Catalyst and unsigned iOS Simulator jobs.
- Added Apple Share Extension dependency inventories for both Apple target frameworks.

### Cross-platform external intake

- Hardened Android `ACTION_SEND` / `ACTION_SEND_MULTIPLE` staging with provider-count limits, provider-declared size checks, runtime byte caps, storage preflight, portable filename sanitation, exact staged length verification, partial cleanup, and atomic inbox handoff.
- Aligned Windows native drop with protocol constants and atomic text/path handoff.
- Added shared rune-safe UTF-8 truncation for external text so multi-byte characters/surrogate pairs are never split at the byte limit.
- Extended stale external-input cache cleanup to nested staging directories.

### Application protocol architecture and security

- Added shared Core wire records for protocol requests, transfer acknowledgements, batch item starts, and pairing responses.
- Added `ProtocolRequestFactory` for validated outgoing request construction.
- Added `ProtocolRequestValidator` for type-specific incoming shape validation and cross-type field-smuggling rejection.
- Added `ProtocolSessionAuthorizer` for testable one-time authorization consumption/replay behavior.
- Centralized sender identity, pairing nonce, pairing code, transfer ID, and batch-item ordering rules.
- Migrated transfer sender, nearby/manual pairing, and receive host to the same typed Core wire records.
- Changed framed protocol JSON deserialization to reject unknown/unmapped members in addition to duplicate members, malformed UTF-8/JSON, comments, trailing commas, excessive depth, and invalid frame lengths.
- Preserved authorization ordering so malformed requests and missing TLS client certificates do not consume a valid one-time transfer nonce.
- Added portable complete file/batch/text/pair conversation tests using the production wire records/policies.

### Receiver lifecycle and filesystem safety

- Added portable `AsyncSessionTracker` and migrated receive listener active-handler tracking/draining to it.
- Added session drain tests covering normal completion, faults, cancellation, and sessions added during drain.
- Added portable rooted-path rejection for Windows drive/UNC/device syntax even on non-Windows hosts.
- Added receive-root symlink/reparse component rejection before/after staging directory creation, before hashing, and before final promotion.
- Added reparse-safe completed-batch destination verification.
- Changed final receive promotion to non-overwrite semantics so a file created by another writer after reservation is preserved instead of replaced.
- Added deterministic final-promotion race and reparse/symlink tests.

### Idempotent interrupted-batch resume

- Added caller-supplied stable batch transfer IDs to `BatchTransferSourceBuilder` and `TransferCoordinator`.
- Routed actual MainPage batch Send/Pause/Resume/Cancel controls through stable-ID lifecycle handling.
- Preserved file **and folder** source selections across pause/failure retry where sources still exist.
- Added SQLite schema version 3 with `completed_batch_items` metadata.
- Added privacy-safe `ReceiveRootKey` using SHA-256 of normalized receive-root identity instead of storing absolute receive-root path.
- Added `BatchCompletionStore`, `BatchResumeStateService`, and `BatchCompletionVerifier`.
- Receiver now records verified finalized batch items before sending their item completion ACK.
- On retry with the same batch ID, receiver revalidates metadata, path confinement/reparse status, destination length, and fresh SHA-256 before offering full-length resume offset.
- Already-completed verified items use the existing protocol-v1 `ResumeOffset == Length` semantics and require zero additional payload bytes.
- Changed/missing destinations, changed source manifest, different root, or new transfer ID fall back to normal collision-safe transfer behavior.
- Completion metadata is bounded/pruned and best-effort; persistence failure cannot turn a successfully verified transfer into a failure.
- Added v2→v3 migration tests, completion-store corruption/pruning tests, stable-ID tests, and completed-file verification tests.

### Privacy and local metadata

- Updated schema documentation to version 3.
- Kept transfer contents, private keys, pairing invitations/nonces, source absolute paths, receive-root absolute paths, and reusable authorization out of SQLite.
- Existing history privacy mode redacts both peer/file identifiers; diagnostic privacy mode redacts common paths/emails/IPs/endpoints/GUIDs/fingerprints/pairing URIs at record/read/export time.
- Android application backup remains disabled.
- Windows protocol package remains private-network-only.

### CI, build, and release engineering

- Added `scripts/validate_apple_integration.py`.
- Integrated Apple metadata validation into Unix/PowerShell verification, regular CI, and release readiness.
- Platform build triggers now include Share Extension source changes.
- Apple jobs explicitly restore/build both Share Extension and containing app.
- Release readiness now requires Apple extension/app compile gates and captures extension dependency graphs for both iOS and Mac Catalyst.
- Added Share Extension-specific warning policy that keeps nullable regressions strict while leaving Apple SDK availability/obsolete diagnostics visible.
- Kept stable C# language mode (`latest`, not preview).

### Documentation

- Updated README, BUILDING, privacy, platform integration/permissions, architecture, wire/security protocol docs, compatibility matrix, SQLite schema docs, project status, roadmap, release checklist, and manual test matrix for the current source state.
- Reclassified the current master-prompt scope as source-complete while keeping signed package, App Group provisioning, real-device/network/accessibility, dependency-license, and store validation explicitly pending.

### Validation boundary

- The development chat runtime does not provide the full .NET MAUI workloads needed to compile/sign all targets locally.
- Recent direct-main GitHub Contents API commits have not exposed combined status contexts through the connector; missing status data is treated as unknown/unreported, never as a pass.
- Signed Apple App Group provisioning, Share Extension embedding/runtime behavior, Mac native drop under release sandbox, signed Android/Windows packages, physical cross-device transfers, accessibility/localization validation, real low-storage/network lifecycle cases, and final dependency-license review remain release gates.

## 1.0.0 - 2026-08-09

- Added the initial .NET MAUI app shell for Android, iOS, macOS (Mac Catalyst), and Windows.
- Added QR/deep-link pairing payloads with expiration and one-time nonce authorization.
- Added self-signed per-device certificate generation and certificate fingerprint pinning.
- Added TLS local-network transport and framed JSON protocol messages.
- Added chunked file streaming, resumable partial files, SHA-256 verification, size limits, and path traversal protection.
- Added local device identity storage with platform secure storage for the certificate.
- Added UDP discovery core service, SQLite trusted-peer store, project documentation, tests, and CI.
- Added Apache-2.0 open-source licensing and project contribution/security policies.

# Changelog

## Unreleased - 2026-08-10

### Transfer and pairing

- Added internal mDNS/DNS-SD nearby discovery with bounded UDP broadcast fallback.
- Added nearby pairing requests with receiver approval and certificate fingerprint binding.
- Added short-lived one-time 8-digit pairing codes and manual local-IP pairing fallback.
- Added strict local/private address enforcement for pairing links and nearby/manual pairing; public Internet addresses and DNS names are rejected by protocol version 1.
- Hardened `swiftdrop://pair` parsing with bounded link/payload sizes, strict protocol version, bounded device metadata, canonical SHA-256 fingerprints, bounded base64url nonces, expiry/lifetime limits, one payload query parameter only, and rejection of unexpected outer URI path/authority/fragment/user-info data.
- Added strict decoded pairing JSON validation before deserialization, including case-insensitive duplicate-property rejection, comments/trailing-comma rejection, bounded depth, and malformed JSON normalization.
- Replaced ad-hoc in-memory pairing nonce tracking with bounded `OneTimeAuthorizationStore` using exact tick-level expiry, atomic single-winner consume, replay rejection, pruning, capacity control, and identity-reset clearing.
- Added encrypted text-snippet transfer with explicit receiver decisions and user-triggered clipboard access only.
- Added multi-file and recursive folder transfer manifests.
- Added receiver accept-all, selective-accept, and reject flows for file batches.
- Added sender-side complete batch preflight for source existence, file count, per-file size, aggregate size, filename sanitation/deconfliction, and cancellation before expensive hashing.
- Added receiver-side shared batch manifest validation, aggregate byte limits, and aggregate remaining-capacity preflight before accepted batch bytes are streamed.
- Added portable post-sanitation batch collision rejection so case/Unicode/invalid-character variants cannot collapse onto one destination.
- Added strict receiver batch-plan validation in Core: unknown, duplicate, missing, contradictory, and out-of-range resume plans are rejected by the sender.
- Added defense-in-depth pairing payload revalidation and local identity initialization at the actual outgoing send boundary.
- Added shared Core `IncomingRequestPolicy` for request version/type, sender identity, transfer IDs, and negotiated batch item ordering; the receive host uses the shared policy.
- Added shared Core `TransferResponsePolicy` for resume offsets, exact file/item/batch completion lengths, and text acknowledgement offsets; the sender uses the shared policy.
- Added single-file resumed-progress initialization and exact receiver completion-length verification.
- Added batch receiver final-total verification.
- Added configurable queued transfer concurrency, queue status UI, cancellation, safe pause, and fresh-pairing resume behavior.
- Added progress, batch throughput, and ETA presentation.
- Added Windows system folder selection for receive/folder workflows while keeping conservative app-private storage on unsupported platforms.
- Added live receive-listener restart when the configured receive destination changes.
- Added active incoming-session tracking and shutdown drain so listener replacement/disposal does not leave untracked handlers.
- Added manifest-bound outgoing stream lengths so a source file that grows/shrinks after manifest creation cannot silently alter protocol framing.
- Added staged-resume validation and truncation of unexpected staged tails to the negotiated resume offset.

### Platform integration

- Added Android inbound share-sheet handling for files and text using app-cache staging.
- Added SwiftDrop pairing protocol activation on Android, iOS, Mac Catalyst, and Windows.
- Added Android foreground data-sync lifetime for active user-initiated transfers.
- Added optional Android completion/failure notifications with generic privacy-safe text and explicit Android 13+ permission gating.
- Kept optional notification preference disabled on targets where completion/failure notifications are not implemented.
- Added staged share-cache pruning.
- Consolidated Android mDNS multicast access onto a reference-counted Wi-Fi multicast-lock manager.
- Disabled Android application backup for local SwiftDrop metadata.
- Added Windows native drag-and-drop for files, folders, text, and SwiftDrop pairing links through the same bounded external-input pipeline used by other intake surfaces.
- Restricted Windows package networking to `privateNetworkClientServer`; removed the general `internetClient` capability for local-only protocol v1.
- Added portable `ExternalFileStager` for bounded exact-length sanitized staging with cancellation and failure cleanup.
- Added iOS and Mac Catalyst document/open-file declarations for `public.data`.
- Added iOS and Mac Catalyst external file-URL staging under temporary security-scoped access into SwiftDrop cache before the normal review/send workflow.
- Added explicit Mac Catalyst app-sandbox plus network client/server entitlements and wired them to the Mac Catalyst target.
- Migrated MAUI application startup from deprecated `Application.MainPage` assignment to `Application.CreateWindow`, with window-destruction cleanup for the receiver and active send cancellation tokens.
- Migrated secondary-page dialog calls to MAUI async dialog APIs and routed MainPage dialog helpers through async APIs without suppressing deprecation warnings.

### Security and privacy

- Added sender client certificates and receiver-certificate SHA-256 pinning.
- Added P-256 ECDSA local identity certificates with digital-signature key usage plus TLS server/client EKUs.
- Added explicit identity-certificate policy covering private-key presence, validity, near-expiry renewal, and supported ECDSA key type.
- Added secure-storage certificate recovery: corrupt/expired/unusable stored certificates generate a new device ID and certificate rather than silently preserving stale trusted identity.
- Added user-visible notice after automatic identity regeneration so other devices can be re-paired deliberately.
- Added canonical SHA-256 fingerprint normalization, colon-friendly parsing, constant-time comparison, and canonical trusted-device persistence.
- Enforced canonical SHA-256 fingerprints again at the Core trust-storage boundary; malformed persisted trust rows are ignored rather than silently trusted.
- Added serialized trusted-device store initialization to avoid concurrent first-use initialization races.
- Added connection-source and sender-certificate pairing attempt rate limits with bounded limiter cardinality.
- Added strict manifest validation, batch count/size limits, text-size limits, protocol JSON depth limits, and network idle timeouts.
- Added strict framed protocol JSON parsing with invalid UTF-8 rejection, no comments/trailing commas, and case-insensitive duplicate-property rejection including nested objects/arrays.
- Added local/private/link-local peer address policy.
- Added shared platform path-comparison policy for receive-root confinement, destination reservations, and external-input path de-duplication.
- Added atomic destination reservations across concurrent incoming sessions so two transfers cannot select the same not-yet-created final path.
- Added portable filename sanitation including Unicode NFC normalization and Windows reserved device-name handling (`CON`, `NUL`, `COM1`, etc.).
- Added collision-safe destination naming, receive-root traversal protection, free-space checks, and resumable `.swiftdrop.part` staging.
- Hardened mDNS TXT parsing so duplicate metadata keys are rejected rather than last-value-wins.
- Added extension-based received-file warnings without presenting them as malware scanning.
- Privacy mode now redacts both peer labels and file/description labels for newly stored transfer history and also redacts older history at read time.
- History privacy markers are language-neutral at rest and localized in presentation.
- Added structured diagnostic privacy redaction for IPs/endpoints, GUIDs, SHA-256 fingerprints, file paths, email-like tokens, and SwiftDrop pairing URIs at record/read/export time.
- Added explicit trusted-device management and device-identity reset behavior.
- Strengthened repository security hygiene to reject private signing/key material, committed local databases, production `.env` files, and embedded private-key blocks.

### Local metadata and persistence

- Advanced SQLite metadata schema to version 2.
- Added `transfer_queue_metadata` with generic label, state/timestamps, and bounded machine-oriented error code only.
- Queue metadata does not store filenames/source paths, transferred text, peer IP addresses, pairing invitations/nonces, credentials, private keys, or free-form exception messages.
- Stale persisted `Queued`/`Running` rows are marked `Interrupted` on app restart and are never silently retried with stale authorization.
- Added bounded queue metadata retention/clear operations.
- Added migration tests for version-zero → current, version 1 → version 2, idempotence, and future-schema rejection.
- Fixed transfer-history retention to call the implemented `PruneOlderThanAsync` store API and serialized history initialization.
- Added history metadata validation and corrupted-row tolerance so malformed local rows are skipped without breaking valid history.
- Added diagnostic metadata validation and corrupted-row tolerance.

### UI, localization, MVVM, support, and accessibility

- Expanded Settings with receive location, history retention, privacy/trust, optional notifications, reduced-motion preference, larger-interface preference, language selection, developer options, and identity management.
- Added Transfer Queue, Nearby Devices, Trusted Devices, Diagnostics, About, Batch Approval, and enhanced History surfaces.
- Expanded English/Hindi resource catalogs across Main, incoming batch consent, About, Queue, History, Nearby Devices, Trusted Devices, Diagnostics, Settings, runtime pairing/transfer dialogs, platform/share intake, identity recovery, and history presentation.
- Added shared XAML localization markup extension and multi-catalog resource lookup.
- Applied saved culture/theme before resolving MainPage during app startup so relaunch honors the selected language on the primary screen.
- Expanded CI localization validation to require XML well-formedness, non-empty values, duplicate-key rejection, exact English/Hindi key parity, and formatted placeholder-index parity.
- Added dynamic interface sizing resources and semantic accessibility labels on key controls.
- Added dedicated `MainViewModel` presentation state for identity, receive root, remote peer, selected sources, transfer status/progress, and send/pause/resume/cancel enabled state.
- Bound MainPage XAML to `MainViewModel` and migrated page/partial presentation updates away from removed named controls.
- Dedicated view-model state now backs Main presentation, History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About.
- Kept platform pickers, QR rendering, clipboard/share actions, modal consent, and navigation page-owned; networking, storage, protocol, TLS, certificate, and filesystem transfer remain in services/Core.
- History rows now localize privacy markers, direction, status, size, and timestamp instead of displaying raw persisted codes.
- Added active receive-root presentation and safe live receive-location updates.
- Added optional open-source development support link `https://buymeacoffee.com/sanskarIN` to README, `SUPPORT.md`, `.github/FUNDING.yml`, and the in-app About page.
- Support wording explicitly states that payment does not unlock transfer features, privileged support, security handling, or private user data.

### Testing, performance, CI, and engineering

- Added tests for pairing codes, discovery registries, trusted devices, history, settings, file-risk classification, rate limiting, path safety, protocol framing, batch manifests, duplicate source names, database schema migration, diagnostics, local-address policy, and transfer integrity/interruption self-tests.
- Expanded strict protocol tests for invalid/nonpositive/oversized frame lengths, malformed UTF-8/JSON, duplicate fields including nested/case variants, and every truncated prefix of a valid frame.
- Added strict pairing tests for decoded duplicate/case-variant JSON fields, comments/trailing commas, URI fields, public/DNS rejection, local addresses, fingerprint/nonce/version/lifetime bounds, and clock boundaries.
- Added one-time authorization tests for replay, expiry, sub-second expiry, concurrency, capacity, pruning, and malformed nonces.
- Added request-policy tests for supported types/version, sender identity, transfer IDs, and batch item ordering.
- Added response-policy tests for resume bounds, exact completion length, receiver rejection, and text acknowledgement offsets.
- Added mDNS tests for compression-pointer loops, duplicate TXT keys, impossible counts, every truncated prefix of a valid announcement, and deterministic random packet fuzzing.
- Added destination-reservation tests including 64-way concurrent same-path pressure.
- Added filename sanitation tests for reserved Windows device names, Unicode normalization, portable collision keys, and post-sanitation equivalence.
- Added manifest tests for timestamp lower/upper bounds, size bounds, control characters, and oversized path metadata.
- Added batch-builder tests for preflight cancellation, missing sources, empty selection, source-name deconfliction, and shared protocol limits.
- Added receiver batch-plan tests for unknown/duplicate/missing paths, invalid offsets, contradictory overall/item acceptance, and duplicate source manifests.
- Added transfer-engine tests for source-size mutation, staged-tail truncation, invalid resume offsets, and normal resume behavior.
- Added external-file staging tests for exact copying, size rejection, cancellation cleanup, and missing sources using cross-platform-safe fixtures.
- Added trust-store tests for canonical fingerprint persistence, malformed direct-database rows, certificate changes, revoke, and clear.
- Added history/diagnostic store tests for invalid writes and corrupted-row tolerance.
- Added diagnostic redaction tests for IPs/endpoints, GUIDs, paths, emails, pairing URIs, and compact/colon-separated SHA-256 fingerprints.
- Added identity certificate policy and certificate-profile tests.
- Added mutual-TLS loopback tests using real `TlsPeerServer`/`TlsPeerClient` streams for exact pin success, pin mismatch rejection, bootstrap fingerprint observation, full file transfer/integrity verification, and staged resume.
- Added bounded synthetic benchmark project for SHA-256 throughput, batch-manifest validation, and portable path sanitation using temporary generated data only.
- Added benchmark documentation and included the benchmark project in canonical `SwiftDrop.slnx`.
- Root build policy now uses latest stable C# language mode rather than preview mode.
- Updated Unix/PowerShell portable verification scripts to run localization validation, Core build/tests, and benchmark compilation.
- Configured platform compile workflows to run on direct `main` pushes and pull requests.
- Added Android, Windows, Mac Catalyst, and unsigned iOS Simulator compile jobs/configuration.
- Expanded release-readiness workflow with localization, benchmark compile, dependency inventory, Android/Windows/Mac Catalyst/iOS Simulator compile gates, and aggregate result enforcement.
- Added CodeQL analysis, Dependabot, repository security hygiene, pull request quality checklist, release checklist, threat model, wire-protocol documentation, database-schema documentation, support policy, usage terms, and third-party notice process.
- Updated privacy/platform/architecture/status/roadmap documentation to match the current implementation boundaries.

### Known validation boundary

- The active chat environment does not provide the .NET/MAUI SDK/workloads required to compile the repository locally.
- GitHub combined-status and direct-commit workflow-run lookups available during this work have returned no usable status contexts/runs; this is unknown/unreported, not success.
- A first-class Apple Share Extension target is still not implemented; Apple document/open-file URL intake is implemented and is deliberately described separately.
- First-class native Mac Catalyst file/folder/text drag-and-drop is still not implemented; Windows drag/drop is implemented.
- Physical-device transfer matrices, signed packages, store submission, accessibility validation, restricted-network behavior, real low-storage behavior, and platform SecureStorage restore/upgrade behavior remain external release requirements.

## 1.0.0 - 2026-08-09

- Added the initial .NET MAUI app shell for Android, iOS, macOS (Mac Catalyst), and Windows.
- Added QR/deep-link pairing payloads with expiration and one-time nonce authorization.
- Added self-signed per-device certificate generation and certificate fingerprint pinning.
- Added TLS local-network transport and framed JSON protocol messages.
- Added chunked file streaming, resumable partial files, SHA-256 verification, size limits, and path traversal protection.
- Added local device identity storage with platform secure storage for the certificate.
- Added UDP discovery core service, SQLite trusted-peer store, project documentation, tests, and CI.
- Added Apache-2.0 open-source licensing and project contribution/security policies.

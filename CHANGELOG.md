# Changelog

## Unreleased - 2026-08-09

### Transfer and pairing

- Added internal mDNS/DNS-SD nearby discovery with bounded UDP broadcast fallback.
- Added nearby pairing requests with receiver approval and certificate fingerprint binding.
- Added short-lived one-time 8-digit pairing codes and manual local-IP pairing fallback.
- Added strict local/private address enforcement for pairing links and nearby/manual pairing; public Internet addresses and DNS names are rejected by protocol version 1.
- Hardened `swiftdrop://pair` parsing with bounded link/payload sizes, strict protocol version, bounded device metadata, canonical SHA-256 fingerprints, bounded base64url nonces, expiry/lifetime limits, one payload query parameter only, and rejection of unexpected outer URI path/authority/fragment/user-info data.
- Added encrypted text-snippet transfer with explicit receiver decisions and user-triggered clipboard access only.
- Added multi-file and recursive folder transfer manifests.
- Added receiver accept-all, selective-accept, and reject flows for file batches.
- Added sender-side complete batch preflight for source existence, file count, per-file size, aggregate size, filename sanitation/deconfliction, and cancellation before expensive hashing.
- Added receiver-side shared batch manifest validation, aggregate byte limits, and aggregate remaining-capacity preflight before accepted batch bytes are streamed.
- Added portable post-sanitation batch collision rejection so case/Unicode/invalid-character variants cannot collapse onto one destination.
- Added strict receiver batch-plan validation in Core: unknown, duplicate, missing, contradictory, and out-of-range resume plans are rejected by the sender.
- Added defense-in-depth pairing payload revalidation and local identity initialization at the actual outgoing send boundary.
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
- Added Windows native drag-and-drop for files, folders, text, and SwiftDrop pairing links through the same bounded external-input pipeline used by other intake surfaces.
- Migrated MAUI application startup from deprecated `Application.MainPage` assignment to `Application.CreateWindow`, with window-destruction cleanup for the receiver and active send cancellation tokens.
- Migrated secondary-page dialog calls to MAUI async dialog APIs and routed MainPage dialog helpers through async APIs without suppressing deprecation warnings.

### Security and privacy

- Added sender client certificates and receiver-certificate SHA-256 pinning.
- Added P-256 ECDSA local identity certificates with digital-signature key usage plus TLS server/client EKUs.
- Added explicit identity-certificate policy covering private-key presence, validity, near-expiry renewal, and supported ECDSA key type.
- Added secure-storage certificate recovery: corrupt/expired/unusable stored certificates generate a new device ID and certificate rather than silently preserving stale trusted identity.
- Added user-visible notice after automatic identity regeneration so other devices can be re-paired deliberately.
- Added canonical SHA-256 fingerprint normalization, colon-friendly parsing, constant-time comparison, and canonical trusted-device persistence.
- Added serialized trusted-device store initialization to avoid concurrent first-use initialization races.
- Added connection-source and sender-certificate pairing attempt rate limits with bounded limiter cardinality; certificate-specific rate limiting applies to pairing requests while one-time nonces authorize file/text/batch transfers.
- Added strict manifest validation, batch count/size limits, text-size limits, protocol JSON depth limits, and network idle timeouts.
- Added strict framed protocol JSON parsing with invalid UTF-8 rejection, no comments/trailing commas, and case-insensitive duplicate-property rejection including nested objects/arrays.
- Added local/private/link-local peer address policy.
- Added shared platform path-comparison policy for receive-root confinement, destination reservations, and external-input path de-duplication.
- Added atomic destination reservations across concurrent incoming sessions so two transfers cannot select the same not-yet-created final path.
- Added portable filename sanitation including Unicode NFC normalization and Windows reserved device-name handling (`CON`, `NUL`, `COM1`, etc.).
- Added collision-safe destination naming, receive-root traversal protection, free-space checks, and resumable `.swiftdrop.part` staging.
- Added extension-based received-file warnings without presenting them as malware scanning.
- Added privacy-aware queue labels, history controls, diagnostic logs, and safe diagnostic export.
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

### UI, localization, MVVM, support, and accessibility

- Expanded Settings with receive location, history retention, privacy/trust, optional notifications, reduced-motion preference, larger-interface preference, language selection, developer options, and identity management.
- Added Transfer Queue, Nearby Devices, Trusted Devices, Diagnostics, About, Batch Approval, and enhanced History surfaces.
- Expanded English/Hindi resource catalogs across Main, incoming batch consent, About, Queue, History, Nearby Devices, Trusted Devices, Diagnostics, and Settings XAML.
- Added shared XAML localization markup extension and multi-catalog resource lookup.
- Applied saved culture/theme before resolving MainPage during app startup so relaunch honors the selected language on the primary screen.
- Added CI localization validation for XML well-formedness, non-empty values, duplicate keys, and exact English/Hindi key parity.
- Added dynamic interface sizing resources and semantic accessibility labels on key controls.
- Completed dedicated view-model state separation for History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About.
- Kept networking, storage, protocol, TLS, certificate, and filesystem logic in services/Core rather than view models.
- MainPage remains an explicit incremental presentation-state MVVM target because it coordinates pairing, selection, receiver consent, pause/resume/cancel, and active server lifecycle.
- Added active receive-root presentation and safe live receive-location updates.
- Added optional open-source development support link `https://buymeacoffee.com/sanskarIN` to README, `SUPPORT.md`, `.github/FUNDING.yml`, and the in-app About page.
- Support wording explicitly states that payment does not unlock transfer features, privileged support, security handling, or private user data.

### Testing, performance, CI, and engineering

- Added tests for pairing codes, discovery registries, trusted devices, history, settings, file-risk classification, rate limiting, path safety, protocol framing, batch manifests, duplicate source names, database schema migration, diagnostics, local-address policy, and transfer integrity/interruption self-tests.
- Expanded strict protocol tests for invalid/nonpositive/oversized frame lengths, malformed UTF-8/JSON, duplicate fields including nested/case variants, and every truncated prefix of a valid frame.
- Added `StrictJsonGuard` as the shared framed-protocol JSON ambiguity/depth validator.
- Added strict pairing-codec tests for public/DNS address rejection, local address acceptance, fingerprint/nonce/version/lifetime validation, duplicate/unexpected query data, and unexpected outer URI path/authority data.
- Added destination-reservation tests including 64-way concurrent same-path pressure.
- Added filename sanitation tests for reserved Windows device names, Unicode normalization, portable collision keys, and post-sanitation equivalence.
- Added manifest tests for timestamp lower/upper bounds, size bounds, control characters, and oversized path metadata.
- Added batch-builder tests for preflight cancellation, missing sources, empty selection, source-name deconfliction, and shared protocol limits.
- Added receiver batch-plan tests for unknown/duplicate/missing paths, invalid offsets, contradictory overall/item acceptance, and duplicate source manifests.
- Added transfer-engine tests for source-size mutation, staged-tail truncation, invalid resume offsets, and normal resume behavior.
- Added identity certificate policy and certificate-profile tests.
- Added mutual-TLS loopback tests using real `TlsPeerServer`/`TlsPeerClient` streams for exact pin success, pin mismatch rejection, bootstrap fingerprint observation, full file transfer/integrity verification, and staged resume.
- Added bounded synthetic benchmark project for SHA-256 throughput, batch-manifest validation, and portable path sanitation using temporary generated data only.
- Added benchmark documentation and included the benchmark project in the canonical `SwiftDrop.slnx`.
- Removed the misleading XML file carrying the legacy `.sln` extension; `SwiftDrop.slnx` is the only canonical solution format.
- Updated Unix/PowerShell portable verification scripts to run localization parity, Core build/tests, and benchmark compilation.
- Expanded regular CI with localization parity and benchmark compile validation.
- Configured platform compile workflows to run on direct `main` pushes and pull requests.
- Added Android, Windows, Mac Catalyst, and unsigned iOS Simulator compile jobs/configuration.
- Expanded release-readiness workflow with localization, benchmark compile, dependency inventory, Android/Windows/Mac Catalyst/iOS Simulator compile gates, and aggregate result enforcement.
- Added CodeQL analysis, Dependabot, repository security hygiene, pull request quality checklist, release checklist, threat model, wire-protocol documentation, database-schema documentation, support policy, usage terms, and third-party notice process.
- Updated security/protocol/architecture docs to match the implemented strict framing, portable path policy, queue persistence, certificate lifecycle, and MVVM boundaries.
- Added `NEXT_STEPS.md` with source work separated from physical-device/signing/store validation.

### Known validation boundary

- The active chat environment does not provide the .NET/MAUI SDK/workloads required to compile the repository locally.
- GitHub combined-status queries during this work have returned no status contexts; this is treated as unknown/unreported, not success.
- A defensive change to reuse `StrictJsonGuard` inside encoded pairing-payload JSON parsing was attempted but blocked by the repository connector. Existing strict pairing URI/field validation remains active; the additional duplicate-inner-property guard remains tracked.
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

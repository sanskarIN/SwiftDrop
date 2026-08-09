# Changelog

## Unreleased - 2026-08-09

### Transfer and pairing

- Added mDNS/Bonjour nearby discovery with bounded UDP broadcast fallback.
- Added nearby pairing requests with receiver approval and certificate fingerprint binding.
- Added short-lived one-time 8-digit pairing codes and manual local-IP pairing fallback.
- Added strict local/private address enforcement for pairing links and nearby/manual pairing; public Internet addresses and DNS names are rejected by protocol version 1.
- Hardened `swiftdrop://pair` parsing with bounded link/payload sizes, strict protocol version, bounded device metadata, canonical SHA-256 fingerprints, bounded base64url nonces, expiry/lifetime limits, one payload query parameter only, and rejection of unexpected outer URI path/authority/fragment/user-info data.
- Added encrypted text-snippet transfer with explicit receiver decisions and user-triggered clipboard access only.
- Added multi-file and recursive folder transfer manifests.
- Added receiver accept-all, selective-accept, and reject flows for file batches.
- Added sender-side complete batch preflight for source existence, file count, per-file size, aggregate size, filename sanitation/deconfliction, and cancellation before expensive hashing.
- Added receiver-side shared batch manifest validation, aggregate byte limits, and aggregate remaining-capacity preflight before accepted batch bytes are streamed.
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
- Added local/private/link-local peer address policy.
- Added atomic destination reservations across concurrent incoming sessions so two transfers cannot select the same not-yet-created final path.
- Added portable filename sanitation including Unicode NFC normalization and Windows reserved device-name handling (`CON`, `NUL`, `COM1`, etc.).
- Added collision-safe destination naming, receive-root traversal protection, free-space checks, and resumable `.swiftdrop.part` staging.
- Added extension-based received-file warnings without presenting them as malware scanning.
- Added privacy-aware queue labels, history controls, diagnostic logs, and safe diagnostic export.
- Added explicit trusted-device management and device-identity reset behavior.
- Added versioned SQLite metadata schema management.

### UI, settings, support, and accessibility

- Expanded Settings with receive location, history retention, privacy/trust, notifications preference, reduced-motion preference, larger-interface preference, language selection, developer options, and identity management.
- Added Transfer Queue, Nearby Devices, Trusted Devices, Diagnostics, About, Batch Approval, and enhanced History surfaces.
- Added English and Hindi resource catalogs plus culture-aware localization infrastructure.
- Added dynamic interface sizing resources and semantic accessibility labels on key controls.
- Began MVVM refactoring with History and Queue view models.
- Added active receive-root presentation and safe live receive-location updates.
- Added optional open-source development support link `https://buymeacoffee.com/sanskarIN` to README, `SUPPORT.md`, `.github/FUNDING.yml`, and the in-app About page.
- Support wording explicitly states that payment does not unlock transfer features, privileged support, security handling, or private user data.

### Testing and engineering

- Added tests for pairing codes, discovery registries, trusted devices, history, settings, file-risk classification, rate limiting, path safety, protocol framing, batch manifests, duplicate source names, database schema migration, diagnostics, local-address policy, and transfer integrity/interruption self-tests.
- Added protocol boundary/fuzz-style tests for malformed JSON and invalid frame lengths.
- Added strict pairing-codec tests for public/DNS address rejection, local address acceptance, fingerprint/nonce/version/lifetime validation, duplicate/unexpected query data, and unexpected outer URI path/authority data.
- Added destination-reservation tests for concurrent collision deconfliction and reservation release.
- Added filename sanitation tests for reserved Windows device names and Unicode normalization.
- Added batch-builder tests for preflight cancellation, missing sources, empty selection, source-name deconfliction, and shared protocol limits.
- Added transfer-engine tests for source-size mutation, staged-tail truncation, invalid resume offsets, and normal resume behavior.
- Added identity certificate policy and certificate-profile tests.
- Added mutual-TLS loopback tests using real `TlsPeerServer`/`TlsPeerClient` streams for exact pin success, pin mismatch rejection, bootstrap fingerprint observation, full file transfer/integrity verification, and staged resume.
- Added GitHub Actions platform compile workflows and CodeQL analysis.
- Added Dependabot configuration, pull request quality checklist, release checklist, threat model, wire-protocol documentation, database-schema documentation, support policy, usage terms, and third-party notice process.
- Added `NEXT_STEPS.md` with P0/P1/P2 release, platform, security, accessibility, performance, signing, and store-readiness work separated from already implemented source work.

## 1.0.0 - 2026-08-09

- Added the initial .NET MAUI app shell for Android, iOS, macOS (Mac Catalyst), and Windows.
- Added QR/deep-link pairing payloads with expiration and one-time nonce authorization.
- Added self-signed per-device certificate generation and certificate fingerprint pinning.
- Added TLS local-network transport and framed JSON protocol messages.
- Added chunked file streaming, resumable partial files, SHA-256 verification, size limits, and path traversal protection.
- Added local device identity storage with platform secure storage for the certificate.
- Added UDP discovery core service, SQLite trusted-peer store, project documentation, tests, and CI.
- Added Apache-2.0 open-source licensing and project contribution/security policies.

# Changelog

## Unreleased - 2026-08-09

### Transfer and pairing

- Added mDNS/Bonjour nearby discovery with bounded UDP broadcast fallback.
- Added nearby pairing requests with receiver approval and certificate fingerprint binding.
- Added short-lived one-time 8-digit pairing codes and manual local-IP pairing fallback.
- Added local/private address enforcement for pairing links and nearby/manual pairing.
- Added encrypted text-snippet transfer with explicit receiver decisions and user-triggered clipboard access only.
- Added multi-file and recursive folder transfer manifests.
- Added receiver accept-all, selective-accept, and reject flows for file batches.
- Added configurable queued transfer concurrency, queue status UI, cancellation, safe pause, and fresh-pairing resume behavior.
- Added progress, batch throughput, and ETA presentation.
- Added Windows system folder selection for receive/folder workflows while keeping conservative app-private storage on unsupported platforms.

### Platform integration

- Added Android inbound share-sheet handling for files and text using app-cache staging.
- Added SwiftDrop pairing protocol activation on Android, iOS, Mac Catalyst, and Windows.
- Added Android foreground data-sync lifetime for active user-initiated transfers.
- Added staged share-cache pruning.

### Security and privacy

- Added sender client certificates and receiver-certificate SHA-256 pinning.
- Added connection-source and sender-certificate pairing attempt rate limits with bounded limiter cardinality.
- Added strict manifest validation, batch count/size limits, text-size limits, protocol JSON depth limits, and network idle timeouts.
- Added local/private/link-local peer address policy.
- Added collision-safe destination naming, receive-root traversal protection, free-space checks, and resumable `.swiftdrop.part` staging.
- Added extension-based received-file warnings without presenting them as malware scanning.
- Added privacy-aware queue labels, history controls, diagnostic logs, and safe diagnostic export.
- Added explicit trusted-device management and device-identity reset behavior.
- Added versioned SQLite metadata schema management.

### UI, settings, and accessibility

- Expanded Settings with receive location, history retention, privacy/trust, notifications preference, reduced-motion preference, larger-interface preference, language selection, developer options, and identity management.
- Added Transfer Queue, Nearby Devices, Trusted Devices, Diagnostics, About, Batch Approval, and enhanced History surfaces.
- Added English and Hindi resource catalogs plus culture-aware localization infrastructure.
- Added dynamic interface sizing resources and semantic accessibility labels on key controls.
- Began MVVM refactoring with History and Queue view models.

### Testing and engineering

- Added tests for pairing codes, discovery registries, trusted devices, history, settings, file-risk classification, rate limiting, path safety, protocol framing, batch manifests, duplicate source names, database schema migration, diagnostics, local-address policy, and transfer integrity/interruption self-tests.
- Added protocol boundary/fuzz-style tests for malformed JSON and invalid frame lengths.
- Added GitHub Actions platform compile workflows and CodeQL analysis.
- Added Dependabot configuration, pull request quality checklist, release checklist, threat model, wire-protocol documentation, database-schema documentation, support policy, usage terms, and third-party notice process.

## 1.0.0 - 2026-08-09

- Added the initial .NET MAUI app shell for Android, iOS, macOS (Mac Catalyst), and Windows.
- Added QR/deep-link pairing payloads with expiration and one-time nonce authorization.
- Added self-signed per-device certificate generation and certificate fingerprint pinning.
- Added TLS local-network transport and framed JSON protocol messages.
- Added chunked file streaming, resumable partial files, SHA-256 verification, size limits, and path traversal protection.
- Added local device identity storage with platform secure storage for the certificate.
- Added UDP discovery core service, SQLite trusted-peer store, project documentation, tests, and CI.
- Added Apache-2.0 open-source licensing and project contribution/security policies.

# Changelog

## Unreleased

- Added transfer history persistence, history UI, privacy-aware filename handling, and trusted-device lifecycle persistence helpers.
- Added settings persistence and settings UI for transfer concurrency, history retention, privacy mode, trusted-device auto-accept preference, and theme.
- Added local network diagnostics.
- Added explicit incoming-transfer approval with sender name, sender certificate fingerprint, file metadata, and dangerous-file warnings.
- Added sender client-certificate presentation and receiver-side client-certificate requirement.
- Added filename collision avoidance and destination free-space checks before receiving file bytes.
- Added sender cancellation controls and sent/received transfer history recording.
- Added file-risk classification, pairing-attempt rate limiting primitives, and expanded automated tests.
- Added `.editorconfig`, privacy documentation, threat model, manual cross-platform test matrix, and release checklist.
- Expanded protocol security documentation and aligned the README with actual implemented behavior.

## 1.0.0 - 2026-08-09

- Added .NET MAUI app shell for Android, iOS, macOS (Mac Catalyst), and Windows.
- Added QR/deep-link pairing payloads with expiration and one-time nonce authorization.
- Added self-signed per-device certificate generation and certificate fingerprint pinning.
- Added TLS local-network transport and framed JSON protocol messages.
- Added chunked file streaming, resumable partial files, SHA-256 verification, size limits, and path traversal protection.
- Added local device identity storage with platform secure storage for the certificate.
- Added UDP discovery core service, SQLite trusted-peer store, project documentation, tests, and CI.
- Added Apache-2.0 open-source licensing and project contribution/security policies.

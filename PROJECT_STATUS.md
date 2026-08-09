# SwiftDrop Project Status

Updated: 2026-08-09

## Completed

- Open-source Apache-2.0 repository foundation.
- .NET MAUI app targeting Android, iOS, Mac Catalyst, and Windows.
- Local device identity with secure-storage-backed certificate material.
- TLS local-network transport with receiver certificate fingerprint pinning and sender client certificates.
- QR/deep-link pairing with short-lived one-time nonces.
- Nearby UDP discovery with device expiry and platform multicast handling.
- Bonjour/mDNS service declarations on Apple platforms plus UDP/QR fallback.
- Receiver-approved nearby pairing request flow with visual fingerprint verification.
- Trusted-device persistence, trust/revoke UI, and optional safe auto-accept for normal-risk files.
- Single-file transfer, resumable partial staging, SHA-256 integrity verification, collision-safe receive paths, filename/path validation, free-space checks, cancellation, and history.
- Explicit text snippet sending, one-time clipboard read, expiration, receive confirmation, and no continuous clipboard monitoring.
- Transfer history with privacy mode and retention pruning.
- Settings, diagnostics, history, nearby-devices, trusted-devices, and About screens.
- Threat model, privacy policy, security documentation, testing matrix, and release checklist.
- Portable unit tests and CI for the core project.

## Current engineering phase

Phase 3/4 continuation: transfer breadth, queueing, receive controls, diagnostics, accessibility, and production hardening.

## Remaining implementation

- Short human-entered pairing code flow layered on authenticated nearby discovery.
- Multi-file and folder manifest transfer with selective receive decisions.
- Explicit transfer queue with configurable concurrency, retry, pause/resume orchestration, speed, and remaining-size reporting.
- User-selectable receive destination with platform-supported picker/bookmark behavior.
- Desktop drag-and-drop and mobile share-sheet integration where supported by .NET MAUI/platform APIs.
- Platform-specific background/notification behavior with honest limitations.
- Structured privacy-aware diagnostic event persistence and safe export.
- Developer diagnostics: simulated interruption and checksum test controls.
- Localization resource architecture and accessibility preference wiring.
- Further integration, migration, compatibility, fuzz, and UI-flow tests.
- Packaging/signing/store validation on physical target platforms.

## Blocked by environment, not source implementation

- iOS/macOS signed builds require Xcode and Apple signing assets on macOS.
- Windows package signing requires a Windows signing environment/certificate.
- Android release signing requires release keystore configuration.
- Physical-device local-network, firewall, background execution, accessibility, share-sheet, drag-and-drop, and store validation cannot be honestly marked complete without the corresponding devices/SDK environments.

## Quality rule

No phase is described as production-verified until its build, automated tests, and documented manual smoke tests have been run on every available target. The source repository should never claim to be bug-free.

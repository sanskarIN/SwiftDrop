# SwiftDrop Project Status

Updated: 2026-08-09

## Implemented in source

- Open-source Apache-2.0 repository foundation.
- .NET MAUI application targeting Android, iOS, Mac Catalyst, and Windows.
- Reusable `SwiftDrop.Core` library and portable xUnit test project.
- Local device identity with secure-storage-backed P-256 ECDSA certificate/private-key material, TLS client/server EKUs, explicit validity policy, seven-day renewal window, and automatic new-identity recovery for unusable stored certificates.
- User-visible notice when automatic identity recovery changes the local device ID/certificate so stale remote trust is not silently implied.
- TLS local-network transport with receiver certificate SHA-256 pinning and sender client certificates.
- Strict QR/deep-link pairing validation with bounded local/private numeric IP addresses, canonical SHA-256 fingerprints, nonce/lifetime validation, and duplicate/unexpected-query rejection.
- Nearby discovery with mDNS/Bonjour plus bounded UDP fallback and peer expiry.
- Short-lived one-time 8-digit pairing-code flow.
- Manual numeric local-IP + one-time-code fallback.
- Explicit receiver approval and fingerprint presentation.
- Trusted-device SQLite persistence, canonical fingerprint matching, serialized initialization, revoke/clear UI, and conservative trusted-device auto-accept behavior.
- Single-file streaming with progress, cancel, pause/fresh-pair resume, staged partials, SHA-256 verification, manifest-bound source length, idle timeouts, free-space checks, and atomically reserved collision-safe receive names.
- Multi-file and recursive-folder manifests with sender/receiver aggregate limits, complete preflight before hashing, receiver selective acceptance, aggregate capacity preflight, per-file verification, and resumable partial staging.
- Concurrent receive-session tracking and graceful drain on receive-server shutdown/restart.
- Explicit text transfer with bounded UTF-8 content/expiry and user-triggered clipboard access only.
- Local transfer queue with configurable concurrency and privacy-mode redaction.
- Local transfer history with retention pruning, per-record deletion, and clear-all behavior.
- Privacy-aware bounded diagnostic persistence and safe export.
- Synthetic developer self-tests for successful transfer, interruption/resume staging, and checksum mismatch rejection.
- SQLite schema versioning/migration management.
- Windows receive-folder picker and centralized receive-location service; changing the configured receive root restarts the listener against the new resolved destination.
- Android inbound share-sheet staging for text/files and foreground transfer activity support.
- Windows desktop drag-and-drop for files, folders, text, and SwiftDrop pairing links through the bounded external-input pipeline.
- Android/iOS/Mac Catalyst/Windows `swiftdrop://` activation handling.
- Reference-counted Android multicast lock integration for mDNS discovery.
- Theme, language, privacy, transfer, notification, accessibility-oriented, and developer settings.
- English/Hindi resource catalogs and an incremental MVVM conversion.
- MAUI startup migrated to `Application.CreateWindow`; window destruction triggers transfer/receiver lifetime cleanup.
- MAUI dialog call sites migrated or routed to the current async dialog APIs rather than suppressing obsolete API warnings.
- Security/threat-model/protocol/privacy/release/testing/accessibility documentation.
- Core CI, platform compile workflows, CodeQL, Dependabot, security-hygiene checks, and release-readiness automation.
- Portable mutual-TLS loopback transfer/resume/pinning tests and expanded protocol/certificate/path/fingerprint tests.
- Optional project support link at `https://buymeacoffee.com/sanskarIN` in README, Support documentation, GitHub funding metadata, and the in-app About screen.

## Current engineering phase

Release-readiness and platform-completion phase.

The local transfer, pairing, trust, integrity, storage, Windows receive/drop integration, and portable TLS assurance paths are implemented substantially in source. Remaining source work is now concentrated primarily in Apple-specific sharing, Mac Catalyst drag-and-drop, complete localization/MVVM migration, optional notification behavior, performance/fuzz assurance, and platform packaging support. Physical-device validation remains a separate release requirement.

## Remaining source work

### Platform integration

- Add a first-class Apple Share Extension target for inbound arbitrary files/text where appropriate.
- Add first-class Mac Catalyst file/folder/text drag-and-drop while reusing the existing validation pipeline. Windows desktop drag-and-drop is implemented in source.
- Decide and implement any additional platform-specific background continuation behavior that can be supported honestly without implying OS-policy bypass.
- Complete optional transfer completion/failure notification behavior on targets where the user enables it; Android foreground-service notification remains a separate platform requirement.

### Architecture and UX

- Finish moving hard-coded user-facing strings into localization resources and validate Hindi layout/fallback behavior.
- Continue MVVM conversion for Main, Nearby Devices, Settings, Trusted Devices, Diagnostics, and About surfaces.
- Add restart-safe persistent queue metadata only if it materially improves UX; do not persist text contents, reusable pairing nonces, or credentials.
- Continue responsive desktop/layout polish, empty states, focus order, and accessible status presentation.

### Security, testing, and performance

- Expand malformed-protocol/property/fuzz-style tests beyond the existing frame, manifest, path, pairing, fingerprint, and TLS cases.
- Expand loopback integration from transport/integrity coverage into complete application-protocol request/approval/batch/text flows.
- Add performance/large-file/batch benchmarks using synthetic temporary data.
- Continue certificate lifecycle testing around platform secure-storage failures and real upgrade/restore scenarios.

## External validation still required

These cannot be honestly completed by source edits alone:

- Physical Android device testing.
- Physical iPhone/iPad testing.
- Physical macOS testing.
- Physical Windows testing across firewall/network configurations.
- Cross-device transfer matrix in both directions.
- Guest Wi-Fi/client-isolation/multicast-blocked behavior.
- Mobile background/sleep/lock behavior.
- Real low-storage behavior.
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, and high-contrast validation.
- Android production signing/AAB generation with a private release keystore.
- Windows production package signing.
- Apple Developer signing, provisioning, TestFlight/notarization/store packaging.
- Store privacy declarations and screenshots against the final signed binaries.
- Final dependency/license inventory from the exact restored signed-release dependency graph.

## Next-step source of truth

See `NEXT_STEPS.md` for the prioritized P0/P1/P2 roadmap and release definition.

See `what_changed.md` for the detailed engineering ledger.

## Quality rule

No phase is described as production-verified until its relevant builds, automated tests, signed package validation, and documented real-device smoke tests have been completed in the correct target environments. The repository should never claim to be bug-free merely because source implementation is extensive.

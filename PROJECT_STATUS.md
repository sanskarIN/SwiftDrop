# SwiftDrop Project Status

Updated: 2026-08-09

## Implemented in source

- Open-source Apache-2.0 repository foundation.
- .NET MAUI application targeting Android, iOS, Mac Catalyst, and Windows.
- Reusable `SwiftDrop.Core` library and portable xUnit test project.
- Local device identity with secure-storage-backed certificate/private-key material.
- TLS local-network transport with receiver certificate SHA-256 pinning and sender client certificates.
- QR/deep-link pairing with short-lived one-time nonces.
- Nearby discovery with mDNS/Bonjour plus bounded UDP fallback and peer expiry.
- Short-lived one-time 8-digit pairing-code flow.
- Manual numeric local-IP + one-time-code fallback.
- Explicit receiver approval and fingerprint presentation.
- Trusted-device SQLite persistence, certificate matching, revoke/clear UI, and conservative trusted-device auto-accept behavior.
- Single-file streaming with progress, cancel, pause/fresh-pair resume, staged partials, SHA-256 verification, free-space checks, and collision-safe receive names.
- Multi-file and recursive-folder manifests with aggregate limits, selective receiver acceptance, per-file verification, and resumable partial staging.
- Explicit text transfer with bounded UTF-8 content/expiry and user-triggered clipboard access only.
- Local transfer queue with configurable concurrency and privacy-mode redaction.
- Local transfer history with retention pruning, per-record deletion, and clear-all behavior.
- Privacy-aware bounded diagnostic persistence and safe export.
- Synthetic developer self-tests for successful transfer, interruption/resume staging, and checksum mismatch rejection.
- SQLite schema versioning/migration management.
- Windows receive-folder picker and centralized receive-location service.
- Android inbound share-sheet staging for text/files and foreground transfer activity support.
- Android/iOS/Mac Catalyst/Windows `swiftdrop://` activation handling.
- Theme, language, privacy, transfer, notification, accessibility-oriented, and developer settings.
- English/Hindi resource catalogs and an incremental MVVM conversion.
- Security/threat-model/protocol/privacy/release/testing/accessibility documentation.
- Core CI, platform compile workflows, CodeQL, Dependabot, security-hygiene checks, and release-readiness automation.
- Optional project support link at `https://buymeacoffee.com/sanskarIN` in README, Support documentation, GitHub funding metadata, and the in-app About screen.

## Current engineering phase

Release-readiness and platform-completion phase.

The core transfer/security model is implemented in source. Remaining work is concentrated in platform-specific integration, full localization/MVVM completion, automated end-to-end assurance, signed packaging, and physical-device validation.

## Remaining source work

### Platform integration

- Add a first-class Apple Share Extension target for inbound arbitrary files/text where appropriate.
- Add first-class desktop file/folder/text drag-and-drop on Windows and Mac Catalyst while reusing the existing validation pipeline.
- Improve receive-listener lifecycle when the configured destination changes while the app is running.
- Decide and implement any platform-specific background continuation behavior that can be supported honestly without implying policy bypass.

### Architecture and UX

- Finish moving hard-coded user-facing strings into localization resources.
- Continue MVVM conversion for Main, Nearby Devices, Settings, Trusted Devices, Diagnostics, and About surfaces.
- Add restart-safe persistent queue metadata if desired, without persisting text contents, reusable pairing nonces, or credentials.
- Continue responsive desktop/layout polish, empty states, focus order, and accessible status presentation.

### Security and testing

- Expand malformed-protocol/property/fuzz-style tests.
- Add deterministic loopback end-to-end TLS integration tests between two local SwiftDrop peers.
- Add explicit certificate-lifecycle/rotation tests and policy documentation.
- Add performance/large-file/batch benchmarks using synthetic temporary data.

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

## Next-step source of truth

See `NEXT_STEPS.md` for the prioritized P0/P1/P2 roadmap and release definition.

See `what_changed.md` for the detailed engineering ledger.

## Quality rule

No phase is described as production-verified until its relevant builds, automated tests, signed package validation, and documented real-device smoke tests have been completed in the correct target environments. The repository should never claim to be bug-free merely because source implementation is extensive.

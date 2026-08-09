# SwiftDrop Project Status

Updated: 2026-08-09

## Implemented in source

### Product and transfer foundation

- Open-source Apache-2.0 repository foundation.
- .NET MAUI application targeting Android, iOS, Mac Catalyst, and Windows.
- Reusable `SwiftDrop.Core` library, portable xUnit tests, and bounded synthetic benchmark project.
- Account-free local-network design with no SwiftDrop cloud upload/relay path.
- Automatic nearby discovery with internal mDNS/DNS-SD plus bounded UDP fallback and peer expiry.
- QR/deep-link pairing, nearby pairing requests, short-lived one-time 8-digit codes, and numeric local-IP fallback.
- Strict pairing-invitation validation for local/private/link-local numeric addresses, protocol version, metadata bounds, canonical SHA-256 fingerprint, nonce, expiry/lifetime, and unexpected outer URI/query data.
- Single-file, multi-file, recursive-folder, and explicit text-snippet transfer flows.
- User-triggered clipboard paste only; no continuous clipboard monitoring.
- Pause/cancel and fresh-pair resume behavior with `.swiftdrop.part` staging.
- Receiver accept-all/selective/reject behavior for file batches.
- Configurable transfer concurrency/queue status.
- Windows receive-folder picker and live receive-listener restart when the configured root changes.
- Windows desktop drag/drop for files, folders, text, and SwiftDrop pairing links through the bounded external-input pipeline.
- Android inbound share-sheet staging for files/text and Android foreground data-sync transfer lifetime.
- Android/iOS/Mac Catalyst/Windows `swiftdrop://` activation handling.

### Security and privacy

- Secure-storage-backed local P-256 ECDSA device certificate/private-key material with TLS client/server EKUs.
- Explicit identity certificate validity/renewal/recovery policy; unusable identities create a new device ID/certificate and surface a re-pair notice instead of silently inheriting old trust.
- TLS 1.2/1.3 local transport with receiver certificate SHA-256 pinning and sender client certificates.
- Explicit receiver approval, sender certificate presentation, certificate-bound trusted-device persistence/revocation, and opt-in normal-risk auto-accept for trusted devices.
- One-time pairing authorization plus bounded connection/pairing attempt rate limiting.
- Strict framed application JSON with bounded length/depth, invalid UTF-8 rejection, comments/trailing-comma rejection, case-insensitive duplicate-property rejection, truncation handling, idle timeouts, and cancellation.
- Sender source-length binding, exact receive-length accounting, SHA-256 completion verification, and invalid-partial cleanup.
- Sender and receiver batch count/per-file/aggregate limits plus receiver aggregate free-space preflight.
- Unicode-normalized/sanitized portable filenames, Windows reserved-name handling, portable post-sanitation batch collision rejection, receive-root confinement, and atomic concurrent destination reservations.
- Shared platform path-comparison policy for Core receive/path reservations and external-input de-duplication.
- Privacy-aware bounded diagnostic persistence/export.
- Received files are never automatically opened; extension warnings are not presented as malware scanning.

### Local metadata

- SQLite schema versioning/migration management at schema version 2.
- Certificate-bound trusted-peer metadata.
- Local transfer history with retention pruning, per-record deletion, clear-all behavior, and privacy-mode filename hiding.
- Bounded privacy-aware diagnostic events.
- Restart-safe privacy-minimal queue metadata containing only a generic transfer label, state/timestamps, and bounded machine-oriented error code.
- Stale persisted `Queued`/`Running` rows become `Interrupted` after restart and are not automatically retried.
- Queue persistence does not store filenames/source paths, transferred text, peer IP addresses, pairing invitations/nonces, credentials, private keys, or free-form exception messages.

### UI, localization, and architecture

- Settings for device name, receive location, transfer concurrency, history retention, privacy/trust, optional notifications, theme, language, accessibility preferences, developer diagnostics, and identity reset.
- Optional Android completion/failure notifications with generic privacy-safe text; permission is requested only on explicit enable where required and denial never changes transfer success/failure.
- English/Hindi `.resx` catalogs expanded across Main, incoming batch consent, About, Queue, History, Nearby Devices, Trusted Devices, Diagnostics, and Settings XAML surfaces.
- XAML localization markup extension and shared resource lookup.
- Saved culture/theme applied before `MainPage` is resolved at startup.
- CI validation for localization XML well-formedness, non-empty values, duplicate keys, and exact English/Hindi key parity.
- Dedicated view models now back History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About.
- Networking, storage, protocol, and cryptography remain in services/Core rather than view models.
- Main transfer orchestration remains code-behind/presentation-state heavy but delegates actual networking/storage/security work to services.
- MAUI startup uses `Application.CreateWindow`; window destruction triggers transfer/receiver cleanup.
- MAUI dialog calls are migrated/routed to current async APIs instead of hiding obsolete warnings.

### Testing, performance, CI, and documentation

- Portable tests for pairing, discovery, trust, settings, history, diagnostics, database migrations, rate limiting, path safety, portable filename normalization/collision behavior, manifests, transfer integrity, source mutation, resume staging, strict JSON framing, and certificate policy.
- Deterministic mutual-TLS loopback tests for certificate pinning, pin mismatch, bootstrap certificate observation, file-byte transfer, SHA-256 verification, and staged resume.
- Strict protocol tests for invalid frame lengths, malformed UTF-8/JSON, duplicate fields including nested/case variants, and every truncated prefix of a valid frame.
- Synthetic benchmark harness for SHA-256 throughput, batch-manifest validation, and portable path sanitation using generated temporary data only.
- Core CI builds/tests, localization parity, and benchmark compilation.
- Platform compile workflows configured for Android, Windows, Mac Catalyst, and unsigned iOS Simulator validation on direct `main` pushes and pull requests.
- Release-readiness workflow includes portable tests, localization, benchmark compile, dependency inventory, Android/Windows/Mac Catalyst/iOS Simulator compile gates, plus an aggregate automated gate.
- CodeQL, Dependabot, and strengthened repository hygiene checks for private signing/key material and local database artifacts.
- Canonical XML solution is `SwiftDrop.slnx`; the misleading XML file with a legacy `.sln` extension was removed.
- Security/threat-model/protocol/privacy/storage/build/release/testing/accessibility documentation.
- Optional project support link at `https://buymeacoffee.com/sanskarIN` in README, Support documentation, GitHub funding metadata, and the in-app About screen.

## Current engineering phase

**Release-readiness and remaining platform-integration phase.**

The core local transfer, pairing, trust, integrity, storage, Windows receive/drop integration, queue metadata, strict framed-protocol validation, localization foundation, and most secondary-screen MVVM separation are implemented in source. Remaining work is narrower and concentrated in MainPage presentation-state migration, Apple-specific share/drop packaging, deeper full-application protocol integration tests, remaining runtime-generated localization strings, and external platform/signing/device validation.

## Remaining source work

### P1 — Platform integration

- Add a first-class Apple Share Extension target for inbound arbitrary files/text where appropriate and validate its App Group/sandbox/privacy lifecycle.
- Add first-class Mac Catalyst file/folder/text drag-and-drop while reusing the existing bounded external-input validation pipeline.
- Decide whether any additional platform-specific background continuation can be supported honestly without implying bypass of OS/store policy.
- Optional completion/failure system notifications remain Android-only in source; unsupported targets deliberately disable the preference instead of pretending support exists.

### P1 — Architecture/localization

- Continue incremental MainPage presentation-state migration to a dedicated view model without moving TLS/filesystem/cryptography into the VM or destabilizing consent/pause/resume orchestration.
- Move remaining MainPage/secondary code-behind dialog/status strings into localization resources where practical; XAML coverage is now broad, but runtime-generated strings are not yet fully translated.
- Validate Hindi layout/wrapping, culture switching, large text, and technical-value readability on real targets.

### P1 — Protocol/testing

- Reuse `StrictJsonGuard` inside the encoded pairing JSON payload parser. This defensive source replacement was attempted in the current implementation session but was blocked by the repository connector; existing pairing URI/field validation remains active.
- Expand loopback integration from TLS/byte-transfer assurance into the complete application request/authorization/accept/reject/batch/text protocol around a UI-independent hostable receive service.
- Add more property/fuzz-style coverage for timestamp overflow, receiver-plan duplication/unknown paths, partial-file mutation between negotiation and send, and high-concurrency collision cases.
- Continue certificate lifecycle testing around real platform SecureStorage backup/restore/lock/upgrade behavior.

### P2 — Performance and polish

- Run and record the synthetic benchmark harness on representative target hardware; source harness exists but this environment cannot produce trustworthy target-device performance numbers.
- Add full peer-to-peer throughput/CPU/memory measurements on real devices because hashing benchmarks do not model Wi-Fi/TLS/receiver I/O.
- Continue responsive desktop layout, focus order, semantic announcements, empty states, trust indicators, and transfer-state polish.

## External validation still required

These cannot be honestly completed by repository source edits alone:

- Verify every configured GitHub Actions job actually completes successfully on the current commit. The connector status endpoint has not reported status contexts during this session; absence of a reported status is not a pass.
- Physical Android device testing.
- Physical iPhone/iPad testing.
- Physical macOS testing.
- Physical Windows testing across firewall/network configurations.
- Cross-device transfer matrix in both directions.
- Guest Wi-Fi/client-isolation/multicast-blocked behavior.
- Mobile background/sleep/lock behavior.
- Real low-storage and network-change behavior.
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, and high-contrast validation.
- Android production signing/AAB generation with a private release keystore.
- Windows production package signing/install/update validation.
- Apple Developer signing, provisioning, device/TestFlight/notarization/store packaging.
- Store privacy declarations/screenshots against final signed binaries.
- Final dependency/license inventory from the exact restored signed-release graph.

## Environment/connector limitations recorded

- The active chat runtime does not provide the .NET/MAUI SDK/workloads required to compile the repository locally here.
- GitHub combined status queries during this session have returned no status contexts; this is recorded as unknown/unreported, not success.
- The GitHub contents/commit connector does not expose a way to force Git author/committer email. Commits use `Signed-off-by: Sanskar <sanskarin@outlook.in>` trailers.
- The defensive `PairingCodec` change to invoke `StrictJsonGuard` for encoded pairing JSON was blocked by the repository connector. It was not bypassed or falsely marked implemented.

## Next-step source of truth

See `NEXT_STEPS.md` for prioritized remaining work and the release definition.

See `what_changed.md` for the detailed engineering ledger.

## Quality rule

No phase is described as production-verified until relevant source builds, automated tests, signed package validation, and documented real-device smoke tests have completed in the correct target environments. The repository must not claim to be bug-free merely because source implementation is extensive.

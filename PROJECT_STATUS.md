# SwiftDrop Project Status

Updated: 2026-08-10

## Implemented in source

### Product and transfer foundation

- Open-source Apache-2.0 repository foundation.
- .NET MAUI application targeting Android, iOS, Mac Catalyst, and Windows.
- Reusable `SwiftDrop.Core` library, portable xUnit test project, and bounded synthetic benchmark project.
- Account-free local-network design with no SwiftDrop-operated cloud upload/relay path.
- Automatic nearby discovery with internal mDNS/DNS-SD plus bounded UDP fallback and peer expiry.
- QR/deep-link pairing, nearby pairing requests, short-lived one-time 8-digit codes, and numeric local-IP fallback.
- Single-file, multi-file, recursive-folder, and explicit text-snippet transfer flows.
- User-triggered clipboard paste only; no continuous clipboard monitoring.
- Pause/cancel and fresh-pair resume behavior with `.swiftdrop.part` staging.
- Receiver accept-all/selective/reject behavior for file batches.
- Configurable transfer concurrency plus restart-safe privacy-minimal queue metadata.
- Windows receive-folder picker and live receive-listener restart when the configured root changes.
- Windows desktop drag/drop for files, folders, text, and SwiftDrop pairing links through the bounded external-input pipeline.
- Android inbound share-sheet staging for files/text and Android foreground data-sync transfer lifetime.
- Android/iOS/Mac Catalyst/Windows `swiftdrop://` activation handling.
- iOS and Mac Catalyst document/open-file URL handling with bounded security-scoped staging through the shared portable external-file stager.

### Security and privacy

- Secure-storage-backed local P-256 ECDSA device certificate/private-key material with TLS client/server EKUs.
- Explicit identity certificate validity/renewal/recovery policy; unusable identities create a new device ID/certificate and surface a re-pair notice instead of silently inheriting old trust.
- TLS 1.2/1.3 local transport with receiver certificate SHA-256 pinning and sender client certificates.
- Explicit receiver approval, authenticated sender certificate presentation, certificate-bound trusted-device persistence/revocation, and opt-in normal-risk auto-accept for trusted devices.
- Strict pairing-invitation validation for local/private/link-local numeric addresses, protocol version, metadata bounds, canonical SHA-256 fingerprint, nonce, expiry/lifetime, and unexpected URI/query data.
- Decoded pairing JSON passes the same strict duplicate-property/comment/trailing-comma/depth checks used by framed protocol JSON.
- Exact-precision bounded one-time authorization storage with atomic first-consume semantics, replay rejection, expiration pruning, capacity limits, and identity-reset clearing.
- Bounded connection/pairing attempt rate limiting.
- Strict framed application JSON with bounded length/depth, invalid UTF-8 rejection, comments/trailing-comma rejection, case-insensitive duplicate-property rejection, truncation handling, idle timeouts, and cancellation.
- Shared Core request policy validates request version/type, sender identity, transfer IDs, and negotiated batch item ordering in the receive host.
- Shared Core response policy validates resume offsets, file/item/batch completion lengths, and text acknowledgement offsets in the sender.
- Sender source-length binding, exact receive-length accounting, SHA-256 completion verification, and invalid-partial cleanup.
- Sender and receiver batch count/per-file/aggregate limits plus receiver aggregate free-space preflight.
- Unicode-normalized/sanitized portable filenames, Windows reserved-name handling, portable post-sanitation batch collision rejection, receive-root confinement, and atomic concurrent destination reservations.
- mDNS parsing rejects duplicate TXT metadata, compression loops, malformed/truncated packets, and safely handles deterministic random-packet fuzz inputs.
- Trusted-peer storage enforces canonical SHA-256 fingerprints itself and ignores malformed persisted trust rows.
- History storage validates metadata and skips malformed/corrupted rows without hiding valid records.
- Diagnostic storage validates metadata and skips malformed/corrupted rows safely.
- Privacy mode redacts both peer and file/description history values for newly stored records and at read time for older records.
- Diagnostic privacy redaction covers IPs/endpoints, GUIDs, fingerprints, paths, email-like tokens, and pairing URIs at record/read/export time.
- Android application backup is disabled for local SwiftDrop app metadata.
- Windows package networking is restricted to `privateNetworkClientServer`; no general `internetClient` capability is requested for protocol v1.
- Received files are never automatically opened; extension warnings are not presented as malware scanning.

### Local metadata

- SQLite schema versioning/migration management at schema version 2.
- Certificate-bound trusted-peer metadata.
- Local transfer history with retention pruning, per-record deletion, clear-all behavior, language-neutral privacy markers, and localized presentation rows.
- Bounded privacy-aware diagnostic events.
- Restart-safe privacy-minimal queue metadata containing only a generic transfer label, state/timestamps, and bounded machine-oriented error code.
- Stale persisted `Queued`/`Running` rows become `Interrupted` after restart and are not automatically retried.
- Queue persistence does not store filenames/source paths, transferred text, peer IP addresses, pairing invitations/nonces, credentials, private keys, or free-form exception messages.
- Transfer bytes and text contents are not stored in SQLite.

### UI, localization, and architecture

- Settings for device name, receive location, transfer concurrency, history retention, privacy/trust, optional notifications, theme, language, accessibility preferences, developer diagnostics, and identity reset.
- Optional Android completion/failure notifications with generic privacy-safe text; permission is requested only on explicit enable where required and denial never changes transfer success/failure.
- English/Hindi `.resx` catalogs cover primary/secondary XAML plus runtime dialogs, pairing, transfer statuses, platform/share intake, incoming batch consent, history presentation, diagnostics, settings, trusted devices, About, queue, and discovery surfaces.
- XAML localization markup extension and shared resource lookup.
- Saved culture/theme applied before `MainPage` is resolved at startup.
- CI validation checks localization XML well-formedness, non-empty values, duplicate keys, exact English/Hindi key parity, and formatted placeholder-index parity.
- Dedicated view models back Main presentation state, History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About.
- Main presentation state now includes identity/receive-root display, remote peer, selected files/batches, transfer status/progress, and send/pause/resume/cancel enabled state.
- Platform pickers, QR rendering, clipboard/share invocations, modal consent, and navigation remain page-owned while networking/storage/protocol/cryptography remain in services/Core.
- MAUI startup uses `Application.CreateWindow`; window destruction triggers transfer/receiver cleanup.
- MAUI dialog calls are migrated/routed to current async APIs instead of hiding obsolete warnings.

### Platform-specific source

- Android: share intents, bounded cache staging, foreground data-sync service, multicast lock, optional generic notifications, local network permissions, no broad storage permission, backup disabled.
- Windows: private-network-only capability, protocol activation, native folder picker, native file/folder/text/pairing-link drag/drop.
- iOS: local-network/Bonjour declarations, protocol activation, system picker, `public.data` document opening, bounded security-scoped external-file staging.
- Mac Catalyst: local-network/Bonjour declarations, protocol activation, `public.data` document opening, bounded security-scoped external-file staging, explicit app-sandbox plus network client/server entitlements.

### Testing, performance, CI, and documentation

- Portable tests for pairing, strict decoded pairing JSON, one-time authorization/replay/expiry/concurrency, discovery, mDNS fuzz/truncation/compression/duplicates, trust, settings, history, diagnostics, database migrations, rate limiting, path safety, portable filename normalization/collision behavior, manifests, transfer response/request policy, external file staging, transfer integrity, source mutation, resume staging, strict JSON framing, and certificate policy.
- Deterministic mutual-TLS loopback tests for certificate pinning, pin mismatch, bootstrap certificate observation, file-byte transfer, SHA-256 verification, and staged resume.
- Strict protocol tests for invalid frame lengths, malformed UTF-8/JSON, duplicate fields including nested/case variants, and every truncated prefix of a valid frame.
- Synthetic benchmark harness for SHA-256 throughput, batch-manifest validation, and portable path sanitation using generated temporary data only.
- Root compiler policy uses latest stable C# language mode rather than preview language mode.
- Core CI builds/tests, localization parity/placeholder validation, and benchmark compilation are configured.
- Platform compile workflows are configured for Android, Windows, Mac Catalyst, and unsigned iOS Simulator validation on direct `main` pushes and pull requests.
- Release-readiness workflow includes portable tests, localization, benchmark compile, dependency inventory, Android/Windows/Mac Catalyst/iOS Simulator compile gates, plus an aggregate automated gate.
- CodeQL, Dependabot, and repository hygiene checks cover private signing/key material and local database artifacts.
- Canonical XML solution is `SwiftDrop.slnx`.
- Security/threat-model/protocol/privacy/storage/build/release/testing/accessibility documentation exists.
- Optional project support link at `https://buymeacoffee.com/sanskarIN` is present in README, Support documentation, GitHub funding metadata, and the in-app About screen.

## Current engineering phase

**Release-readiness, Apple integration completion, and external validation phase.**

The core local transfer, pairing, trust, integrity, storage, request/response validation, Windows receive/drop integration, Android share/foreground path, Apple document/open-file staging, queue metadata, strict protocol handling, English/Hindi runtime localization, and Main/secondary-screen presentation separation are substantially implemented in source.

Remaining work is now concentrated primarily in Apple features that require dedicated extension/native-drop targets or careful signed sandbox validation, deeper full-application protocol integration tests, release packaging, accessibility/performance validation, and physical cross-device testing.

## Remaining source work

### P1 — Apple platform integration

- Add a first-class Apple Share Extension target for arbitrary inbound files/text if that product surface is required; the existing document/open-file URL path is not a Share Extension.
- Add first-class native Mac Catalyst file/folder/text drag-and-drop if required, reusing `ExternalInputInbox` and bounded staging rather than creating a direct-send bypass.
- Keep any additional Apple background behavior conservative and consistent with OS lifecycle/store policy; do not claim arbitrary indefinite socket continuation.
- Optional completion/failure system notifications remain Android-only in current source.

### P1 — Protocol/integration assurance

- Expand loopback integration from transport/byte transfer into the complete application request/authorization/accept/reject/batch/text flow around a UI-independent hostable protocol layer.
- Add automated receive-root restart/lifecycle tests once listener orchestration is isolated from MAUI lifecycle types.
- Continue certificate lifecycle testing around real SecureStorage/keychain/keystore backup/restore/locked-device/upgrade behavior.
- Continue deterministic property/fuzz cases for partial-file mutation between negotiation and stream start and protocol-transition disconnects.

### P2 — Accessibility/performance/polish

- Validate Hindi wrapping, culture switching, large text, technical fingerprint readability, and accessibility on real targets.
- Run/record synthetic benchmark results on representative hardware; source harness exists but this environment cannot produce target-device performance evidence.
- Measure full peer-to-peer throughput/CPU/memory on real devices because synthetic hashing/path benchmarks do not model Wi-Fi/TLS/storage I/O.
- Continue responsive desktop layout, focus order, semantic announcements, empty states, trust indicators, and transfer-state polish based on real accessibility/device testing.

## External validation still required

These cannot be honestly completed by repository source edits alone:

- Verify every configured GitHub Actions job actually completes successfully for the exact release candidate. Workflow/status lookups available in this session have returned no direct-main runs/status contexts; that is unknown/unreported, not a pass.
- Physical Android device testing.
- Physical iPhone/iPad testing.
- Physical macOS testing.
- Physical Windows testing across firewall/network configurations.
- Cross-device transfer matrix in both directions.
- Guest Wi-Fi/client-isolation/multicast-blocked behavior.
- Mobile background/sleep/lock behavior.
- Real low-storage and network-change behavior.
- Security-scoped Apple document-provider behavior under signed sandboxed builds.
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, and high-contrast validation.
- Android production signing/AAB generation with a private release keystore.
- Windows production package signing/install/update validation.
- Apple Developer signing, provisioning, physical-device/TestFlight/notarization/store packaging.
- Store privacy declarations/screenshots against final signed binaries.
- Final dependency/license inventory from the exact restored signed-release graph.

## Environment/connector limitations recorded

- The active chat runtime does not provide the .NET/MAUI SDK/workloads required to compile the repository locally here.
- GitHub combined status queries and direct-commit workflow-run lookups available in this session have returned no usable status contexts/runs. This is recorded as unknown/unreported, not success.
- The GitHub contents/commit connector does not expose a way to force Git author/committer email. Focused commits use `Signed-off-by: Sanskar <sanskarin@outlook.in>` trailers.

## Next-step source of truth

See `NEXT_STEPS.md` for prioritized remaining work and release definition.

See `what_changed.md` for the detailed engineering ledger.

## Quality rule

No phase is described as production-verified until relevant builds, automated tests, signed package validation, and documented real-device smoke tests have completed in the correct target environments. The repository must not claim to be bug-free merely because source implementation is extensive.

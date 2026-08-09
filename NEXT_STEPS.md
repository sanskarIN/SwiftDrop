# SwiftDrop Next Steps

Updated: 2026-08-09

This roadmap separates source work that can be completed in the repository from validation that requires real operating systems, devices, signing identities, stores, and network environments. It is intentionally strict: a feature is not called production-verified only because its source code exists.

## Recently completed in source

The following items were previously roadmap work and are now implemented in the repository. They remain subject to CI/platform/device validation where applicable:

- Optional project support link at `https://buymeacoffee.com/sanskarIN` in README, Support documentation, GitHub funding metadata, and the in-app About page.
- Shared receiver-side batch manifest validation and aggregate batch-byte limits.
- Aggregate free-space preflight for accepted batch remainder before bytes are accepted.
- Complete sender batch preflight for source existence/count/per-file-size/aggregate-size before expensive hashing.
- Manifest-bound outgoing file lengths so a source that grows/shrinks after hashing cannot silently change protocol framing.
- Resume staging validation and truncation of staged tails back to the negotiated offset.
- Atomic receive-destination reservations across concurrent incoming sessions.
- Portable Windows reserved-device filename handling and Unicode filename normalization.
- Live receive-listener restart when the configured receive root changes.
- Active incoming-session tracking and shutdown drain for the receive listener.
- Native Windows desktop drag-and-drop for files, folders, text, and SwiftDrop pairing links through the same bounded external-input path used by other intake surfaces.
- Reference-counted Android Wi-Fi multicast locking for mDNS discovery.
- MAUI startup migrated to `Application.CreateWindow` with window-lifetime cleanup.
- Current MAUI async dialog API usage on secondary screens and an async compatibility routing layer on the main transfer screen.
- Explicit P-256 ECDSA local identity certificate policy with TLS client/server EKUs, secure-storage recovery, renewal-before-expiry behavior, and user notice when identity regeneration changes the device certificate.
- Canonical SHA-256 certificate fingerprint normalization and constant-time trusted-device matching.
- Strict pairing invitation decoding for local/private numeric addresses, protocol version, device metadata, canonical fingerprint, nonce, expiry/lifetime, duplicate/unexpected query parameters, and unexpected outer URI authority/path data.
- Mutual-TLS loopback coverage for certificate pinning, successful file transfer, integrity verification, and staged resume behavior.
- Expanded certificate, fingerprint, path, filename, pairing, batch, collision, source-mutation, and resume tests.

## P0 — Release blockers

### 1. Make every automated build gate green

Required checks:

- `SwiftDrop.Core` restore/build.
- Full portable xUnit suite.
- Dependency vulnerability audit.
- CodeQL C# analysis.
- Android MAUI compile validation.
- Windows MAUI compile validation.
- Mac Catalyst MAUI compile validation.
- Repository security-hygiene checks.
- Release-readiness workflow.

If any target build fails, fix the source/workload/configuration issue before generating store packages. The current chat environment does not contain the required .NET MAUI SDK/workloads and cannot substitute for these gates.

Pay particular attention to target-platform compilation of the newest integrations:

- Windows WinUI drag-and-drop.
- MAUI `CreateWindow` lifecycle integration.
- MAUI async dialog APIs.
- Android foreground-service and multicast-lock integration.
- iOS/Mac Catalyst URL activation.
- SecureStorage certificate load/regeneration behavior.

### 2. Run the physical-device transfer matrix

At minimum validate these real-device pairs on the same normal LAN/Wi-Fi:

- Windows → Android.
- Android → Windows.
- Android → Android.
- Windows → Windows.
- macOS → Android.
- Android → macOS.
- iOS → Windows.
- Windows → iOS.
- iOS → macOS.
- macOS → iOS.

For each direction test:

- mDNS discovery.
- UDP fallback when mDNS is unavailable.
- QR/deep-link pairing.
- nearby pairing request.
- one-time 8-digit pairing code.
- manual local-IP fallback.
- single small file.
- zero-byte file.
- large file.
- duplicate filename collision.
- concurrent incoming transfers targeting the same filename.
- Windows reserved-device filename such as `CON.txt` from a non-Windows sender.
- high-risk-extension warning.
- multi-file batch.
- recursive folder transfer where source folder selection exists.
- receiver selective batch acceptance.
- aggregate batch free-space rejection.
- cancellation.
- pause then fresh-pair resume.
- interrupted network then fresh-pair resume.
- source-file mutation after manifest creation.
- checksum mismatch rejection through the developer self-test path.
- text transfer.
- explicit clipboard paste.
- Windows desktop file/folder/text drag-and-drop.
- trusted-device behavior.
- trust revocation.
- identity reset followed by re-pairing.
- automatic identity regeneration/renewal followed by re-pairing.
- privacy mode.
- history retention and deletion.
- receive-folder change while the listener is active.

### 3. Validate hostile and restricted local-network conditions

Run manual tests on:

- guest Wi-Fi with client isolation.
- multicast-blocked Wi-Fi.
- Windows firewall blocked/allowed inbound rules.
- Android background restrictions.
- iOS local-network permission denied then allowed.
- IPv4-only LAN.
- IPv6-capable LAN.
- network change during transfer.
- sleep/lock during a transfer.
- low-storage destination.
- very slow LAN.
- repeated pairing-code failures.
- rapid inbound connection attempts.
- stale/expired pairing links.
- public-IP and DNS pairing-link rejection.

Expected behavior is graceful failure and clear diagnostics. SwiftDrop must not attempt to bypass network, firewall, enterprise, or operating-system policy.

### 4. Complete release signing and store packaging

Android:

- Create/store the production keystore outside the repository.
- Configure secure CI/release secrets.
- Generate release AAB/APK.
- Verify signing certificate and package identifier.
- Test clean install and upgrade install.
- Validate foreground-service declarations against current Play requirements.
- Validate notification behavior and permission UX on supported Android versions.

Windows:

- Configure a production signing certificate outside the repository.
- Validate MSIX identity and protocol registration.
- Test install/uninstall/update.
- Validate WinUI drag-and-drop in the packaged build.
- Validate private-network capability and firewall behavior.

Apple:

- Configure Apple Developer signing outside the repository.
- Validate bundle identifiers, entitlements, local-network permission text, Bonjour service declarations, and URL scheme.
- Produce signed iOS and Mac Catalyst builds on macOS/Xcode.
- Test TestFlight/notarization/store submission as applicable.

No private signing keys, passwords, provisioning profiles containing secrets, or store credentials should be committed.

## P1 — Remaining platform integration work

### 5. Add first-class Apple Share Extension

Current Apple activation handles SwiftDrop pairing URLs, but inbound Share Extension packaging for arbitrary files/text is a separate target and should be implemented deliberately.

Design requirements:

- iOS/macOS share extension target where supported.
- shared app-group container only if required.
- bounded file/text intake.
- safe filename normalization.
- no background clipboard monitoring.
- no upload to a cloud service.
- clear handoff to the main SwiftDrop UI.
- independent extension lifecycle/error handling.
- App Store entitlement/privacy review.
- cleanup of staged extension data after handoff/failure.
- source-file size/count preflight before copying large extension payloads where platform APIs allow it.

### 6. Complete remaining desktop drag-and-drop on Mac Catalyst

**Windows source implementation is complete** and now accepts files, folders, text, and pairing links through the existing bounded external-input pipeline. It still needs target-platform build and packaged runtime validation.

Remaining source work:

- Mac Catalyst files.
- Mac Catalyst folders where allowed by sandbox/security-scoped access.
- Mac Catalyst text snippets.
- Mac Catalyst pairing links where the native drop surface exposes text/URLs.

The Mac drop path must reuse the existing picker/share validation pipeline instead of creating a weaker transfer path. Do not retain security-scoped access longer than required.

### 7. Finish localization coverage

The resource architecture exists, but all user-facing strings should move out of hard-coded XAML/code-behind before calling Hindi localization complete.

Work through every screen:

- Main.
- Nearby Devices.
- Queue.
- History.
- Settings.
- Trusted Devices.
- Diagnostics.
- About.
- batch consent.
- errors/warnings.
- pairing status.
- transfer status.
- identity-recovery notice.
- receive-location messages.
- drag-and-drop/shared-input messages.

Then validate clipping, wrapping, dynamic text size, RTL readiness, culture fallback, pluralization, and fingerprint/technical-value readability.

### 8. Complete the MVVM conversion

History and Queue already demonstrate observable view-model separation. Continue the same pattern for:

- Main transfer workflow.
- Nearby Devices.
- Settings.
- Trusted Devices.
- Diagnostics.
- About.

Keep networking, storage, protocol, and cryptography out of view models; they should depend on application/core services. The existing `MainPage` partial files are intentionally separated by responsibility, but that is not the same as completed MVVM separation.

### 9. Validate and refine receive-root lifecycle

The source now reacts to settings changes by stopping the active receive server, draining/cancelling its sessions through the server lifetime token, resolving the new destination, restarting the listener, and displaying the active destination.

Remaining work is validation/refinement:

- Confirm active sessions receive clear cancellation semantics on every target.
- Confirm no port-bind race occurs during rapid repeated receive-folder changes.
- Confirm staged partial files remain only in the root that originally accepted them.
- Confirm a fresh pairing invitation resumes only against the currently active root.
- Confirm Windows folder-access permissions remain usable after app restart/package update.
- Decide whether UI should prevent changing the receive root while active transfers exist instead of cancelling them.
- Add integration tests around listener restart when the app layer becomes test-hostable without MAUI UI dependencies.

### 10. Strengthen queue orchestration

Add optional persistent queue metadata while preserving privacy only if restart persistence materially improves the user experience:

- queued/running/paused/completed/failed states.
- retry policy with explicit user action.
- current peer identity/fingerprint binding.
- source availability validation before retry.
- no persistence of text contents.
- privacy-mode redaction.
- restart-safe cleanup of impossible/stale queue items.
- no silent replay of pairing authorization.

Do not persist reusable pairing nonces or credentials. A persisted queue item must still require fresh transfer authorization when authorization has expired/been consumed.

## P1 — Security and protocol assurance

### 11. Expand malformed-protocol and property-style testing

The current foundation already covers bounded frame lengths, malformed JSON, manifest validation, path traversal, filename sanitation, reserved Windows filenames, canonical fingerprints, strict pairing links, local/public address policy, rate limiting, destination collision reservations, staged resume validation, source-length mutation, and TLS pinning.

Continue with fuzz/property-style coverage for:

- truncated JSON frames at every byte position.
- invalid UTF-8 sequences.
- maximum-depth and over-depth JSON.
- duplicated JSON fields with conflicting values.
- invalid/overflow timestamps.
- malformed SHA-256 strings in every manifest context.
- duplicate batch paths after Unicode/case normalization on case-insensitive filesystems.
- rooted/UNC/device paths and alternate separators.
- extremely long nested paths near platform path limits.
- collision races under many concurrent reservations.
- resume offsets larger than partial/final size.
- partial file changes between offset negotiation and transfer start.
- replayed pairing nonces.
- repeated incorrect pairing codes.
- sender certificate changes during trusted-device flows.
- receiver plan duplication/unknown paths/out-of-range offsets.
- unexpected outer pairing URI path/authority/query fields.
- network stream truncation exactly at chunk/frame boundaries.

### 12. Expand end-to-end loopback integration tests

**Completed foundation:** deterministic loopback tests now start real `TlsPeerServer`/`TlsPeerClient` peers and cover exact certificate pinning, pin mismatch rejection, bootstrap fingerprint observation, mutual TLS, a full file-byte round trip, SHA-256 equality, and staged resume.

Remaining application-protocol loopback coverage:

- one-time authorization consumption/replay rejection.
- single-file request/accept/reject handshake around `ReceiveServerService` once the app protocol is moved into a UI-independent hostable service.
- resume-offset negotiation over the complete request protocol.
- batch transfer.
- selective batch acceptance.
- batch aggregate capacity failure.
- text transfer.
- cancellation.
- idle timeout.
- concurrent destination collisions.
- receive-root restart behavior.
- history/diagnostic metadata remains content-free.

These tests should use temporary random fixtures, never user files.

### 13. Validate certificate lifecycle and upgrade behavior

**Completed source policy:** SwiftDrop evaluates local identity certificates for private-key presence, validity window, near-expiry renewal, and ECDSA support. Unusable/corrupt stored certificates create a new device ID/certificate instead of silently preserving the old trusted identity. The app shows a notice that remote devices must pair again. New certificates declare TLS client/server EKUs.

Remaining validation:

- SecureStorage read/write/delete failure behavior on each OS.
- OS backup/restore behavior.
- device migration behavior.
- app uninstall/reinstall behavior.
- app upgrade across certificate-policy changes.
- keychain/keystore access while the device is locked.
- trust UX after automatic certificate renewal/regeneration.
- retention or cleanup of stale platform key material.
- clock-change behavior around NotBefore/NotAfter limits.

Any future rotation change must continue to avoid silently treating a new certificate as the old trusted identity.

## P2 — Performance and polish

### 14. Add performance benchmarks

Measure on representative devices:

- hashing throughput.
- transfer throughput.
- CPU usage.
- memory usage.
- large-batch source preflight cost.
- large-batch hashing cost.
- SQLite history/diagnostic cost.
- discovery traffic volume.
- resume efficiency.
- concurrent receive reservation/streaming behavior.
- Windows dropped-folder enumeration behavior.

Test small files and multi-gigabyte files without allocating whole-file buffers. Synthetic data should be used for benchmark automation.

### 15. Improve transfer statistics

Consider:

- smoothed transfer speed.
- stable ETA calculation.
- recent throughput graph.
- completed/remaining byte counts.
- per-file batch progress.
- receiver-side progress.
- queued vs active transfer counts.

Privacy mode should never expose filenames in notification/status surfaces when configured to hide them.

### 16. Accessibility release pass

Run the existing checklist with:

- Android TalkBack.
- iOS VoiceOver.
- macOS VoiceOver.
- Windows Narrator.
- keyboard-only Windows/macOS navigation.
- large text/font scaling.
- high contrast.
- reduced motion.
- touch target checks.
- pairing fingerprint reading/copying.
- batch selective-consent controls.
- drag-and-drop status announcements.
- identity regeneration notice.

Fix focus order, names, hints, contrast, and clipped content found during the pass.

### 17. UI/UX polish

Consider:

- responsive desktop width/layout.
- better empty states.
- clearer peer trust indicators.
- stronger separation of pairing vs transfer authorization.
- non-blocking status banners for recoverable network problems.
- clearer pause/resume explanation.
- explicit active receive-root indicator.
- optional onboarding that explains local-network privacy and permissions.
- clearer explanation when automatic identity renewal requires re-pairing.
- drag-and-drop affordance on desktop rather than relying only on accepting drops invisibly.

## P2 — Distribution and community readiness

### 18. Prepare store metadata

Prepare and verify:

- screenshots.
- short/long descriptions.
- privacy-policy URL.
- support URL/email.
- optional project-support URL.
- license attribution.
- local-network permission explanation.
- no-cloud/local-transfer description.
- version/release notes.
- age/content declarations appropriate to the app.
- Android foreground-service disclosure where store policy requires it.
- Apple local-network/Bonjour rationale.
- Windows private-network capability rationale.

### 19. Validate support and funding surfaces

The project support link is:

https://buymeacoffee.com/sanskarIN

It is currently present in the repository README, `SUPPORT.md`, GitHub funding metadata, and the in-app About page.

Keep it clearly optional. It must not imply payment is required for security fixes, open-source access, transfer features, privileged support, or access to private user data. Re-check this wording before every store submission if support copy changes.

### 20. Release candidate process

For each release candidate:

1. Freeze protocol-affecting changes.
2. Run portable CI/tests.
3. Run CodeQL/dependency review.
4. Run every available platform compile workflow.
5. Run physical-device smoke matrix.
6. Run the documented hostile/restricted-network cases.
7. Review permissions/entitlements.
8. Review privacy/security docs against actual behavior.
9. Review the exact restored dependency graph and third-party notices.
10. Update `CHANGELOG.md`.
11. Update `PROJECT_STATUS.md`.
12. Update `what_changed.md`.
13. Tag the release only after release gates are satisfied.
14. Produce signed packages in trusted release environments.
15. Re-test installed signed packages, clean installs, and upgrades.

## Definition of production-ready

SwiftDrop should be described as production-ready only when all of the following are true:

- Core tests pass.
- Security/static analysis gates pass.
- Each intended release platform compiles in its correct SDK environment.
- Signed packages install and upgrade correctly.
- Real-device peer-to-peer transfers pass in both directions for supported platform pairs.
- Permission-denied/network-blocked/low-storage/interrupted-transfer behavior has been validated.
- Identity renewal/recovery behavior has been validated on real platform secure storage.
- Privacy and security documentation matches the shipped binary.
- No credentials/signing secrets are committed.
- Accessibility and localization passes have been performed on target platforms.
- Store submissions/review requirements are satisfied.
- Exact release dependency/license notices are complete.

## Scope discipline

Do not add cloud relay, account login, analytics, advertising identifiers, remote Internet transfer, silent background clipboard monitoring, automatic opening/execution of received files, or custom cryptography merely to make the feature list larger. Those would materially change SwiftDrop's current privacy and threat model and require a separate design/security review.

# SwiftDrop Next Steps

Updated: 2026-08-09

This roadmap separates source work that can be completed in the repository from validation that requires real operating systems, devices, signing identities, stores, and network environments. It is intentionally strict: a feature is not called production-verified only because its source code exists.

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

If any target build fails, fix the source/workload/configuration issue before generating store packages.

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
- high-risk-extension warning.
- multi-file batch.
- recursive folder transfer where source folder selection exists.
- receiver selective batch acceptance.
- cancellation.
- pause then fresh-pair resume.
- interrupted network then fresh-pair resume.
- checksum mismatch rejection through the developer self-test path.
- text transfer.
- explicit clipboard paste.
- trusted-device behavior.
- trust revocation.
- privacy mode.
- history retention and deletion.

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

Expected behavior is graceful failure and clear diagnostics. SwiftDrop must not attempt to bypass network, firewall, enterprise, or operating-system policy.

### 4. Complete release signing and store packaging

Android:

- Create/store the production keystore outside the repository.
- Configure secure CI/release secrets.
- Generate release AAB/APK.
- Verify signing certificate and package identifier.
- Test clean install and upgrade install.

Windows:

- Configure a production signing certificate outside the repository.
- Validate MSIX identity and protocol registration.
- Test install/uninstall/update.

Apple:

- Configure Apple Developer signing outside the repository.
- Validate bundle identifiers, entitlements, local-network permission text, and URL scheme.
- Produce signed iOS and Mac Catalyst builds on macOS/Xcode.
- Test TestFlight/notarization/store submission as applicable.

No private signing keys, passwords, provisioning profiles containing secrets, or store credentials should be committed.

## P1 — Remaining platform integration work

### 5. Add first-class Apple share extension

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

### 6. Add first-class desktop drag-and-drop

Implement and validate native desktop drag/drop for:

- Windows files.
- Windows folders where allowed.
- Mac Catalyst files/folders where allowed.
- text snippets.

The drop path must reuse the existing picker/share validation pipeline instead of creating a weaker transfer path.

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
- consent dialogs.
- errors/warnings.
- pairing and transfer status text.

Then validate clipping, wrapping, dynamic text size, RTL readiness, and fallback culture behavior.

### 8. Complete the MVVM conversion

History and Queue already demonstrate observable view-model separation. Continue the same pattern for:

- Main transfer workflow.
- Nearby Devices.
- Settings.
- Trusted Devices.
- Diagnostics.
- About.

Keep networking, storage, protocol, and cryptography out of view models; they should depend on application/core services.

### 9. Improve receive-root lifecycle

The active receive listener should react safely when the user changes the receive destination:

- stop accepting new sessions.
- allow/cancel active sessions according to explicit policy.
- restart listener against the newly resolved destination.
- display the active destination, not only the configured destination.
- preserve path confinement and free-space checks.

### 10. Strengthen queue orchestration

Add optional persistent queue metadata while preserving privacy:

- queued/running/paused/completed/failed states.
- retry policy with explicit user action.
- current peer identity/fingerprint binding.
- source availability validation before retry.
- no persistence of text contents.
- privacy-mode redaction.
- restart-safe cleanup of impossible/stale queue items.

Do not persist reusable pairing nonces or credentials.

## P1 — Security and protocol assurance

### 11. Expand malformed-protocol testing

Add fuzz/property-style coverage for:

- truncated frames.
- oversized frames.
- deeply nested JSON.
- invalid UTF-8.
- invalid timestamps.
- invalid SHA-256 strings.
- duplicate batch paths after normalization.
- rooted/UNC/device paths.
- extremely long filenames.
- collision races.
- resume offsets larger than partial/final size.
- replayed pairing nonces.
- repeated incorrect pairing codes.
- sender certificate changes during trusted-device flows.

### 12. Add end-to-end loopback integration tests

Create deterministic local integration tests that start two TLS peers on loopback and verify:

- certificate pinning success.
- pinning mismatch failure.
- one-time authorization.
- single-file transfer.
- resume offset negotiation.
- batch transfer.
- selective batch acceptance.
- text transfer.
- cancellation.
- idle timeout.
- history/diagnostic metadata remains content-free.

These tests should use temporary random fixtures, never user files.

### 13. Review certificate lifecycle

Before release decide and document:

- certificate rotation policy.
- behavior after device restore/migration.
- secure-storage failure recovery.
- trust invalidation after identity reset.
- whether expired/old local certificates are rotated automatically or only by explicit reset.

Any rotation must avoid silently treating a new certificate as the old trusted identity.

## P2 — Performance and polish

### 14. Add performance benchmarks

Measure on representative devices:

- hashing throughput.
- transfer throughput.
- CPU usage.
- memory usage.
- large-batch manifest cost.
- SQLite history/diagnostic cost.
- discovery traffic volume.
- resume efficiency.

Test small files and multi-gigabyte files without allocating whole-file buffers.

### 15. Improve transfer statistics

Consider:

- smoothed transfer speed.
- stable ETA calculation.
- recent throughput graph.
- completed/remaining byte counts.
- per-file batch progress.
- receiver-side progress.

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

Fix focus order, names, hints, contrast, and clipped content found during the pass.

### 17. UI/UX polish

Consider:

- responsive desktop width/layout.
- better empty states.
- clearer peer trust indicators.
- stronger separation of pairing vs transfer authorization.
- non-blocking status banners for recoverable network problems.
- clearer pause/resume explanation.
- optional onboarding that explains local-network privacy and permissions.

## P2 — Distribution and community readiness

### 18. Prepare store metadata

Prepare and verify:

- screenshots.
- short/long descriptions.
- privacy-policy URL.
- support URL/email.
- license attribution.
- local-network permission explanation.
- no-cloud/local-transfer description.
- version/release notes.
- age/content declarations appropriate to the app.

### 19. Validate support and funding surfaces

The project support link is:

https://buymeacoffee.com/sanskarIN

Keep it clearly optional. It must not imply payment is required for security fixes, open-source access, transfer features, or access to private support data.

### 20. Release candidate process

For each release candidate:

1. Freeze protocol-affecting changes.
2. Run portable CI/tests.
3. Run CodeQL/dependency review.
4. Run every available platform compile workflow.
5. Run physical-device smoke matrix.
6. Review permissions/entitlements.
7. Review privacy/security docs against actual behavior.
8. Update `CHANGELOG.md`.
9. Update `PROJECT_STATUS.md`.
10. Update `what_changed.md`.
11. Tag the release only after release gates are satisfied.
12. Produce signed packages in trusted release environments.
13. Re-test installed signed packages.

## Definition of production-ready

SwiftDrop should be described as production-ready only when all of the following are true:

- Core tests pass.
- Security/static analysis gates pass.
- Each intended release platform compiles in its correct SDK environment.
- Signed packages install and upgrade correctly.
- Real-device peer-to-peer transfers pass in both directions for supported platform pairs.
- Permission-denied/network-blocked/low-storage/interrupted-transfer behavior has been validated.
- Privacy and security documentation matches the shipped binary.
- No credentials/signing secrets are committed.
- Accessibility and localization passes have been performed on target platforms.
- Store submissions/review requirements are satisfied.

## Scope discipline

Do not add cloud relay, account login, analytics, advertising identifiers, remote Internet transfer, silent background clipboard monitoring, automatic opening/execution of received files, or custom cryptography merely to make the feature list larger. Those would materially change SwiftDrop's current privacy and threat model and require a separate design/security review.

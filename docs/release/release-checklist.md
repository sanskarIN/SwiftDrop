# SwiftDrop Release Checklist

Updated: 2026-08-11

This checklist is a release gate, not a statement that the listed checks have already passed. A source implementation or configured workflow is not production validation.

## Source and dependency review

- [ ] The exact release-candidate commit is identified and frozen for validation.
- [ ] `main`/candidate CI is green for portable restore, build, tests, localization validation, Apple integration metadata validation, benchmark compile, platform compile jobs, CodeQL, repository hygiene, and release-readiness aggregation.
- [ ] `SwiftDrop.App`, `SwiftDrop.ShareExtension`, `SwiftDrop.Core`, tests, and benchmark dependency graphs are generated from the exact restored candidate.
- [ ] `dotnet list package --vulnerable` (or the current supported equivalent) is reviewed in a connected development environment for every shipped/runtime project and target framework.
- [ ] Dependency provenance, supported target frameworks, licenses, notice obligations, and security advisories are reviewed.
- [ ] No secrets, signing keys, PFX/P12 files, keystores, provisioning secrets, tokens, pairing invitations, local databases, or real transferred files are committed.
- [ ] `CHANGELOG.md`, `PROJECT_STATUS.md`, `PRIVACY.md`, `THIRD_PARTY_NOTICES.md`, `NEXT_STEPS.md`, and `what_changed.md` match the exact candidate.

## Protocol, identity, and transport security

- [ ] Pairing invitations are short-lived, local-address-only, strictly parsed, and one-time authorization cannot be replayed.
- [ ] Unknown protocol versions/types and unknown/unmapped JSON members are rejected.
- [ ] Duplicate JSON members, malformed UTF-8/JSON, comments, trailing commas, excessive depth, invalid lengths, and truncated frames are rejected.
- [ ] Receiver certificate SHA-256 pinning is verified for pairing/transfer connections where a pin is expected.
- [ ] Sender client certificate is required and receiver identity/trust decisions use the TLS-derived sender fingerprint rather than sender-supplied JSON.
- [ ] Local identity certificate lifecycle/recovery creates a new identity when old private material cannot be safely reused.
- [ ] Pairing/connection attempt limits are verified under repeated invalid requests.
- [ ] TLS configuration does not introduce custom cryptography or an undisclosed relay path.

## File and batch integrity

- [ ] Incoming untrusted transfers require explicit approval; trusted-device auto-accept remains opt-in, certificate-bound, and normal-risk only.
- [ ] Potentially dangerous extensions show a warning without claiming malware scanning.
- [ ] Received files are never automatically opened/executed.
- [ ] Rooted/traversal/portable-invalid paths are rejected.
- [ ] Existing symlink/reparse components under the receive root are rejected around staging/finalization.
- [ ] Concurrent destination reservations prevent same-destination races.
- [ ] Final promotion never silently overwrites an existing completed file.
- [ ] Sender streams exactly the manifest-declared length; source growth/shrinkage fails safely.
- [ ] Receiver writes exactly the expected remaining bytes and SHA-256 verifies before final promotion.
- [ ] Corrupted or mismatched payloads never become final files.
- [ ] Free-space checks reject unsafe single/batch transfers before consuming the remaining payload.
- [ ] Batch count/per-file/aggregate limits are enforced on both sender and receiver.
- [ ] Stable batch IDs are retained across pause/failure retry and changed for a new explicit send.
- [ ] Schema-v3 completed-batch metadata can skip only a still-present file matching the same transfer/root/source/length/SHA-256.
- [ ] Modifying/removing a previously completed batch destination before retry forces safe re-transfer/failure rather than a false completed acknowledgement.

## Privacy and local metadata

- [ ] No account is required for the local-transfer workflow.
- [ ] No transferred file/text content is uploaded to a SwiftDrop-operated service.
- [ ] Clipboard is read only after explicit user action.
- [ ] Transfer bytes/text contents, private keys, reusable pairing authorization, and absolute receive-root paths are not stored in SQLite.
- [ ] Schema-v3 `completed_batch_items` stores only bounded resume metadata and a hashed receive-root identity.
- [ ] Queue persistence remains metadata-only and does not replay authorization after restart.
- [ ] Privacy mode redacts peer/file history labels and privacy-sensitive diagnostic identifiers.
- [ ] Optional Android completion/failure notification text remains generic and content-free.
- [ ] `PRIVACY.md` and store privacy declarations match the final binaries.

## Android

- [ ] Build/install a signed release candidate on supported physical Android hardware.
- [ ] Verify picker/share workflows without broad legacy storage permission.
- [ ] Verify bounded `ACTION_SEND` / `ACTION_SEND_MULTIPLE` intake, cancellation/failure cleanup, and no auto-send.
- [ ] Verify mDNS multicast lock acquisition/release and UDP fallback behavior.
- [ ] Verify foreground data-sync service behavior against current Android/Play policy.
- [ ] Verify Android 13+ optional notification permission deny/allow transitions; transfer result must not depend on optional notification permission.
- [ ] Verify backup-disabled behavior matches release privacy expectations.
- [ ] Verify app icon, splash, theme, rotation, large text, TalkBack, and sleep/background behavior.

## iOS

- [ ] Build with the supported .NET MAUI/Xcode toolchain and Apple Developer signing.
- [ ] Provision the containing app and Share Extension with the same required App Group: `group.in.sanskar.swiftdrop`.
- [ ] Verify containing-app and extension bundle identifiers, versions/build numbers, entitlements, extension point, and activation rules.
- [ ] Verify local-network privacy prompt and Bonjour discovery on physical devices.
- [ ] Verify `swiftdrop://pair` activation in cold and warm starts.
- [ ] Verify document/open-file security-scoped staging.
- [ ] Verify Share Extension intake for supported file/text/web URL cases, provider cancellation/failure, package bounds, App Group handoff, containing-app import, and review-before-send behavior.
- [ ] Verify malformed/stale/symlinked App Group packages are rejected and abandoned staging is cleaned.
- [ ] Verify app transport does not depend on unsupported indefinite background socket execution.
- [ ] Verify VoiceOver, large text, Hindi wrapping, and lifecycle/sleep interruption behavior.

## macOS / Mac Catalyst

- [ ] Build/sign the Mac Catalyst app and Share Extension with the supported Xcode/.NET MAUI toolchain.
- [ ] Verify app-sandbox network client/server entitlements and shared App Group provisioning.
- [ ] Verify Bonjour/local-network behavior and macOS firewall allow/block cases.
- [ ] Verify document/open-file security-scoped staging under signed sandbox conditions.
- [ ] Verify Share Extension App Group handoff under signed sandbox conditions.
- [ ] Verify native `UIDropInteraction` for files, folders, text, and pairing links.
- [ ] Verify dropped file/folder security-scoped access lifetime, symlink rejection, count/aggregate limits, deconfliction, and no auto-send.
- [ ] Verify VoiceOver, keyboard-only navigation, window resizing, and large text.

## Windows

- [ ] Build/install a signed package on supported Windows versions.
- [ ] Verify the package requests `privateNetworkClientServer` and no unnecessary public-internet capability for protocol v1.
- [ ] Verify `swiftdrop://` package protocol activation.
- [ ] Verify Windows Defender Firewall blocked/allowed/private/public profile behavior and diagnostics guidance.
- [ ] Verify system receive-folder picker behavior after packaging/install/update.
- [ ] Verify native file/folder/text/pairing-link drag/drop and no auto-send.
- [ ] Verify keyboard navigation, Narrator, high DPI, high contrast, large text, and window resizing.
- [ ] Verify signed package clean-install/update/uninstall behavior.

## Physical transfer matrix

Complete `docs/testing/manual-test-matrix.md` for supported sender/receiver combinations. At minimum validate:

- [ ] mDNS discovery and UDP fallback;
- [ ] QR/deep-link, nearby request, one-time code, and manual local-IP pairing;
- [ ] expired/replayed invitation and wrong-code rejection;
- [ ] certificate pin mismatch rejection;
- [ ] small/zero-byte/large files;
- [ ] recursive folder and large multi-file batch;
- [ ] accept-all/selective/reject batch consent;
- [ ] pause/cancel/network interruption and fresh-pair resume;
- [ ] already-completed batch-item reuse without duplicate collision-renamed copies;
- [ ] source mutation and staged-partial corruption;
- [ ] filename collision and simultaneous same-name incoming transfers;
- [ ] Unicode/case/Windows-reserved path cases;
- [ ] low-storage rejection;
- [ ] dangerous extension warning;
- [ ] explicit text transfer and clipboard paste;
- [ ] trust, revoke, identity reset, and identity-regeneration re-pair behavior;
- [ ] receive-root change while listener is active;
- [ ] queue restart/interrupted metadata behavior.

## Restricted-network and lifecycle validation

- [ ] Guest Wi-Fi/client isolation.
- [ ] Multicast-blocked Wi-Fi.
- [ ] IPv4-only and IPv6-capable LANs.
- [ ] Network switch/change during transfer.
- [ ] Very slow LAN / idle-timeout behavior.
- [ ] Sleep/lock/background transitions on each platform.
- [ ] Real low-storage behavior.
- [ ] Repeated invalid pairing/connection pressure.
- [ ] SecureStorage/keychain/keystore locked/unavailable/upgrade/restore scenarios where reproducible.

## Accessibility and localization

- [ ] Light/dark/system themes are readable.
- [ ] TalkBack, VoiceOver, Narrator, and desktop keyboard-only navigation are exercised.
- [ ] Large text does not hide critical actions.
- [ ] Status/errors do not rely only on color.
- [ ] Pairing fingerprints remain readable and usable.
- [ ] Hindi layouts are checked for clipping/wrapping and long runtime messages.
- [ ] Reduced-motion/high-contrast preferences behave acceptably on supported targets.

## Packaging and publication

- [ ] App and Share Extension version/build numbers are consistent.
- [ ] Android signing material, Windows signing material, Apple signing/provisioning, and store credentials remain outside the repository.
- [ ] Final Android AAB/APK, Windows package/MSIX, iOS/TestFlight build, and Mac Catalyst distribution artifact are tested after signing/packaging.
- [ ] Apple App Group and extension entitlements are present in the signed artifacts, not only source plist files.
- [ ] Exact restored dependency/license/notice inventory is reviewed for the final signed binaries.
- [ ] Store privacy declarations, local-network/foreground-service/App Group explanations, screenshots, descriptions, support links, and release notes match actual behavior.
- [ ] Git tag/release notes identify the exact candidate commit and validation evidence.

## Production-ready rule

Do not describe a candidate as production-ready until the required automated gates, signed-package checks, physical cross-device/network matrix, accessibility/localization checks, and store/privacy review are complete for that exact commit.

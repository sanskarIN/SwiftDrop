# SwiftDrop Release Checklist

Updated: 2026-08-12

This checklist is a release gate, not a statement that the listed checks have already passed. A source implementation or configured workflow is not production validation.

## Source and dependency review

- [ ] The exact release-candidate commit is identified and frozen for validation.
- [ ] `main`/candidate CI is green for portable restore, build, tests, localization validation, Apple integration metadata validation, benchmark compile, platform compile jobs, CodeQL, repository hygiene, and release-readiness aggregation.
- [ ] `SwiftDrop.App`, `SwiftDrop.ShareExtension`, `SwiftDrop.Core`, tests, and benchmark dependency graphs are generated from the exact restored candidate.
- [ ] `dotnet list package --vulnerable` (or current supported equivalent) is reviewed in a connected development environment for every shipped/runtime project and target framework.
- [ ] Dependency provenance, supported target frameworks, licenses, notice obligations, and security advisories are reviewed.
- [ ] No secrets, signing keys, PFX/P12 files, keystores, provisioning secrets, tokens, pairing invitations, local databases, or real transferred files are committed.
- [ ] No obsolete/dead batch compatibility handler can bypass stable transfer IDs; XAML and app call sites use the stable-ID coordinator API.
- [ ] `CHANGELOG.md`, `PROJECT_STATUS.md`, `PRIVACY.md`, `THIRD_PARTY_NOTICES.md`, `NEXT_STEPS.md`, protocol/security/platform/testing docs, and `what_changed.md` match the exact candidate.

## Protocol, identity, and transport security

- [ ] Pairing invitations are short-lived, local-address-only, strictly parsed, and one-time authorization cannot be replayed.
- [ ] Pairing capability representation is canonical: no surrounding whitespace, exactly one raw `p=`, no empty/unknown/duplicate query fields, unpadded Base64URL only, and no standard Base64/percent-encoded aliases.
- [ ] Decoded pairing JSON rejects duplicate and unknown fields.
- [ ] Unknown protocol versions/types and unknown/unmapped framed JSON members are rejected.
- [ ] Duplicate JSON members, malformed UTF-8/JSON, comments, trailing commas, excessive depth, invalid lengths, and truncated frames are rejected.
- [ ] File/batch paths are fully canonical/structurally validated before transfer nonce consumption.
- [ ] A malformed path/request and a missing sender TLS certificate do not consume a valid one-time transfer nonce.
- [ ] Batch transfer IDs accept only bounded ASCII letters/digits/`-`/`_`.
- [ ] Receiver certificate SHA-256 pinning is verified for pairing/transfer connections where a pin is expected.
- [ ] Sender client certificate is required and receiver identity/trust decisions use the TLS-derived sender fingerprint rather than sender-supplied JSON.
- [ ] Local identity certificate lifecycle/recovery creates a new identity when old private material cannot be safely reused.
- [ ] Pairing/connection attempt limits are verified under repeated invalid requests.
- [ ] TLS configuration does not introduce custom cryptography or an undisclosed relay path.

## Canonical path, source, and destination safety

- [ ] Protocol manifest paths use `/` as the only wire separator across Windows/Android/iOS/Mac Catalyst senders.
- [ ] Rooted/drive/UNC/device syntax, empty/repeated/trailing separators, `.`/`..`, and more than 64 segments are rejected.
- [ ] A path that would change during sanitation/Unicode normalization/reserved-name handling is rejected as noncanonical rather than rewritten after authorization.
- [ ] Each filename segment is bounded to 180 UTF-16 code units and 180 UTF-8 bytes without broken Unicode scalars.
- [ ] The maximum sanitized filename plus `.swiftdrop.part` stays within the intended common component-byte headroom.
- [ ] Case/Unicode/sanitation-equivalent sender paths are deconflicted before hashing and remain receiver-valid.
- [ ] Collision-generated filenames stay distinct when the base is already at character/byte limits; uniqueness markers are not truncated away.
- [ ] Selected single-file source is a regular non-link/non-reparse source and is revalidated at stream open.
- [ ] Selected folder roots and descendants reject symbolic links/reparse points; recursion is bounded/deterministic.
- [ ] Paused resume state drops a source that has been replaced by a link/reparse point.
- [ ] Existing symlink/reparse components under the receive root are rejected around staging/finalization/resume verification.
- [ ] Concurrent destination reservations prevent same-destination races.
- [ ] Final promotion never silently overwrites an existing completed file.

## File and batch integrity

- [ ] Incoming untrusted transfers require explicit approval; trusted-device auto-accept remains opt-in, certificate-bound, and normal-risk only.
- [ ] Potentially dangerous extensions show a warning without claiming malware scanning.
- [ ] Received files are never automatically opened/executed.
- [ ] Sender streams exactly the manifest-declared length; source growth/shrinkage fails safely.
- [ ] Same-length source content change after hashing results in receiver SHA-256 failure rather than false success.
- [ ] Receiver writes exactly the expected remaining bytes and SHA-256 verifies before final promotion.
- [ ] Corrupted or mismatched payloads never become final files.
- [ ] Failure to apply optional last-write timestamp metadata after verified promotion does not falsely report content transfer failure.
- [ ] Free-space checks reject unsafe single/batch transfers before consuming the remaining payload.
- [ ] Batch count/per-file/aggregate/path-depth/path-length limits are enforced and source-known limits are preflighted before expensive hashing where practical.
- [ ] Folder manifest ordering is deterministic for an unchanged source tree.
- [ ] Stable batch IDs are retained across pause/failure retry and changed for a new explicit send.
- [ ] Active app batch controls use the stable-ID path; the removed compatibility overload cannot create a fresh ID per retry.
- [ ] Schema-v3 completed-batch metadata can skip only a still-present file matching the same transfer/root/source/length/SHA-256.
- [ ] Modifying/removing a previously completed destination before retry prevents false completed reuse.
- [ ] Modifying/removing a previously completed destination after the retry plan but before its item ACK is caught by the second completed-item verification.

## External-input staging safety

- [ ] Android, Apple Share Extension, and Mac drop use the shared file-count/per-file/aggregate staging budget policy.
- [ ] Failed file staging does not incorrectly consume budget for later items.
- [ ] Unicode/max-length external filenames stay byte-bounded and collision-safe.
- [ ] External shared/dropped/opened content always reaches review state before transfer and is never auto-sent.

## Privacy and local metadata

- [ ] No account is required for the local-transfer workflow.
- [ ] No transferred file/text content is uploaded to a SwiftDrop-operated service.
- [ ] Clipboard is read only after explicit user action.
- [ ] Transfer bytes/text contents, private keys, reusable pairing authorization, and absolute receive-root paths are not stored in SQLite.
- [ ] Schema-v3 `completed_batch_items` stores only bounded resume metadata and a hashed receive-root identity.
- [ ] Completed-batch source path is canonical protocol identity; local destination metadata is re-confined/re-hashed before reuse.
- [ ] Queue persistence remains metadata-only and does not replay authorization after restart.
- [ ] Privacy mode redacts peer/file history labels and privacy-sensitive diagnostic identifiers.
- [ ] Optional Android completion/failure notification text remains generic and content-free.
- [ ] `PRIVACY.md` and store privacy declarations match final binaries.

## Android

- [ ] Build/install a signed release candidate on supported physical Android hardware.
- [ ] Verify picker/share workflows without broad legacy storage permission.
- [ ] Verify bounded `ACTION_SEND` / `ACTION_SEND_MULTIPLE` intake, cancellation/failure cleanup, and no auto-send.
- [ ] Verify provider-declared normal size, null size, and negative size; negative size is treated as unknown.
- [ ] Verify unknown-length content is bounded by the remaining aggregate staging budget.
- [ ] Reduce cache free space during an unknown-length copy and verify repeated reserve checks stop/clean the copy rather than exhausting storage.
- [ ] Verify mDNS multicast lock acquisition/release and UDP fallback behavior.
- [ ] Verify foreground data-sync service behavior against current Android/Play policy.
- [ ] Verify Android 13+ optional notification permission deny/allow transitions; transfer result must not depend on optional notification permission.
- [ ] Verify backup-disabled behavior matches release privacy expectations.
- [ ] Verify app icon, splash, theme, rotation, large text, TalkBack, and sleep/background behavior.

## iOS

- [ ] Build with supported .NET MAUI/Xcode toolchain and Apple Developer signing.
- [ ] Provision containing app and Share Extension with App Group `group.in.sanskar.swiftdrop`.
- [ ] Verify app/extension bundle IDs, versions/build numbers, entitlements, extension point, and activation rules.
- [ ] Verify local-network privacy prompt and Bonjour discovery on physical devices.
- [ ] Verify canonical `swiftdrop://pair` activation in cold/warm starts.
- [ ] Verify document/open-file security-scoped staging.
- [ ] Verify Share Extension intake for supported file/text/web URL cases, common staging budgets, package bounds, App Group handoff, and review-before-send behavior.
- [ ] Delay provider response beyond the bounded response timeout and verify failure/cleanup.
- [ ] Return provider before timeout but allow a legitimate copy to continue longer; verify the response timer does not cancel that active copy.
- [ ] Verify malformed/stale/symlinked App Group packages are rejected and abandoned staging is cleaned.
- [ ] Add undeclared file/directory content under App Group package `files/` and verify exact-set rejection.
- [ ] Reduce app-cache free space and verify aggregate validated package bytes are preflighted before recopy starts.
- [ ] Verify app transport does not depend on unsupported indefinite background socket execution.
- [ ] Verify VoiceOver, large text, Hindi wrapping, and lifecycle/sleep interruption behavior.

## macOS / Mac Catalyst

- [ ] Build/sign Mac Catalyst app and Share Extension with supported Xcode/.NET MAUI toolchain.
- [ ] Verify app-sandbox network client/server entitlements and shared App Group provisioning.
- [ ] Verify Bonjour/local-network behavior and macOS firewall allow/block cases.
- [ ] Verify document/open-file security-scoped staging under signed sandbox conditions.
- [ ] Verify Share Extension App Group handoff under signed sandbox conditions.
- [ ] Verify native `UIDropInteraction` for files, folders, text, and pairing links.
- [ ] Verify dropped source security-scoped lifetime, link/reparse rejection, shared count/per-file/aggregate budget, bounded collision deconfliction, and no auto-send.
- [ ] Delay native-drop provider file/text callbacks beyond the bounded response wait and verify cleanup instead of a hang.
- [ ] Return provider before timeout but let copy continue longer and verify the response timer does not kill the active copy.
- [ ] Verify VoiceOver, keyboard-only navigation, window resizing, and large text.

## Windows

- [ ] Build/install a signed package on supported Windows versions.
- [ ] Verify package requests `privateNetworkClientServer` and no unnecessary public-Internet capability for protocol v1.
- [ ] Verify `swiftdrop://` package protocol activation.
- [ ] Verify Windows Defender Firewall blocked/allowed/private/public profile behavior and diagnostics guidance.
- [ ] Verify system receive-folder picker behavior after packaging/install/update.
- [ ] Verify native file/folder/text/pairing-link drag/drop and no auto-send.
- [ ] Verify Windows sender folder manifests use canonical `/` wire paths and interoperate with non-Windows receivers.
- [ ] Verify direct dropped/selected file/folder sources still pass regular-source/link checks before send.
- [ ] Verify keyboard navigation, Narrator, high DPI, high contrast, large text, and window resizing.
- [ ] Verify signed package clean-install/update/uninstall behavior.

## Physical transfer matrix

Complete `docs/testing/manual-test-matrix.md` for supported sender/receiver combinations. At minimum validate:

- [ ] mDNS discovery and UDP fallback;
- [ ] QR/deep-link, nearby request, one-time code, and manual local-IP pairing;
- [ ] pairing capability canonicality and expired/replayed invitation/wrong-code rejection;
- [ ] certificate pin mismatch rejection;
- [ ] small/zero-byte/large files;
- [ ] recursive folder and large multi-file batch;
- [ ] forward-slash canonical folder manifests across every sender platform;
- [ ] selected/source-tree link rejection;
- [ ] accept-all/selective/reject batch consent;
- [ ] pause/cancel/network interruption and fresh-pair stable-ID resume;
- [ ] already-completed batch-item reuse without duplicate collision-renamed copies;
- [ ] second completed-item verification after retry plan before zero-byte ACK;
- [ ] source mutation and staged-partial corruption;
- [ ] filename collision and simultaneous same-name incoming transfers including max-length Unicode names;
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
- [ ] Real low-storage behavior during transfer and external staging.
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
- [ ] Apple App Group and extension entitlements are present in signed artifacts, not only source plist files.
- [ ] Exact restored dependency/license/notice inventory is reviewed for final signed binaries.
- [ ] Store privacy declarations, local-network/foreground-service/App Group explanations, screenshots, descriptions, support links, and release notes match actual behavior.
- [ ] Git tag/release notes identify exact candidate commit and validation evidence.

## Production-ready rule

Do not describe a candidate as production-ready until required automated gates, signed-package checks, physical cross-device/network matrix, accessibility/localization checks, dependency/license review, and store/privacy review are complete for that exact commit.

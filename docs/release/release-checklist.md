# SwiftDrop Release Checklist

Updated: 2026-08-15

This checklist is a release gate, not a statement that the listed checks have already passed. A source implementation or configured workflow is not production validation.

## Source and dependency review

- [ ] The exact release-candidate commit is identified and frozen for validation.
- [ ] `main`/candidate CI is green for documentation integrity, Python validation-helper tests, portable restore/build/tests, localization validation, Apple integration metadata validation, benchmark compile, platform compile/audit jobs, CodeQL, repository hygiene, and release-readiness aggregation.
- [ ] `SwiftDrop.App` target graphs for Android, Windows, Mac Catalyst, and iOS, the iOS `SwiftDrop.ShareExtension`, `SwiftDrop.Core`, tests, and benchmark dependency graphs are generated from the exact restored candidate.
- [ ] Machine-readable package/vulnerability reports use explicit `--format json --output-version 1`; vulnerable views include transitive packages and are validated with `scripts/validate_nuget_vulnerability_report.py` rather than accepted solely because JSON was produced.
- [ ] Exact-candidate release-readiness artifacts `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit` are retained.
- [ ] Every retained dependency-audit bundle contains its expected JSON reports and deterministic `manifest.json`; file lengths and SHA-256 digests are independently checked before archival.
- [ ] Dependency provenance, supported target frameworks, licenses, notice obligations, and security advisories are manually reviewed; automation is not treated as complete license/provenance evidence.
- [ ] Restored/source dependency evidence is compared with the final signed/package artifacts so hosted simulator/unpackaged graphs are not silently substituted for shipped-binary evidence.
- [ ] `docs/release/dependency-evidence.md` matches the workflow artifact names, report format, validator behavior, manifest schema, and current release-review process.
- [ ] No secrets, signing keys, PFX/P12 files, keystores, provisioning secrets, tokens, pairing invitations, local databases, or real transferred files are committed.
- [ ] No obsolete/dead batch compatibility handler can bypass stable transfer IDs; XAML and app call sites use the stable-ID coordinator API.
- [ ] Current SQLite schema is v6 and documentation/tests/migrations agree on the v0/v1/v2/v3/v4/v5→v6 upgrade contract, including null-preserving legacy performance fields.
- [ ] `CHANGELOG.md`, `PROJECT_STATUS.md`, `PRIVACY.md`, `THIRD_PARTY_NOTICES.md`, `NEXT_STEPS.md`, protocol/security/platform/testing/release docs, and `what_changed.md` match the exact candidate.

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
- [ ] `completed_batch_items`, introduced in schema v3 and retained in current schema v6, can skip only a still-present file matching the same transfer/root/source/length/SHA-256.
- [ ] Modifying/removing a previously completed destination before retry prevents false completed reuse.
- [ ] Modifying/removing a previously completed destination after the retry plan but before its item ACK is caught by the second completed-item verification.

## External-input staging safety

- [ ] Android, iOS Share Extension, and Mac native drop use the shared file-count/per-file/aggregate staging budget policy.
- [ ] Failed file staging does not incorrectly consume budget for later items.
- [ ] Unicode/max-length external filenames stay byte-bounded and collision-safe.
- [ ] External shared/dropped/opened content always reaches review state before transfer and is never auto-sent.

## Privacy and local metadata

- [ ] No account is required for the local-transfer workflow.
- [ ] No transferred file/text content is uploaded to a SwiftDrop-operated service.
- [ ] Clipboard is read only after explicit user action.
- [ ] Transfer bytes/text contents, private keys, reusable pairing/transfer authorization, pairing nonces, queue peer endpoints, and queue source/destination paths are not stored in SQLite.
- [ ] `completed_batch_items`, introduced in schema v3 and retained in current schema v6, stores only bounded resume metadata and a hashed receive-root identity.
- [ ] Completed-batch source path is canonical protocol identity; local destination metadata is re-confined/re-hashed before reuse.
- [ ] The queue persistence contract introduced through schema v4 and retained in schema v6 stores only generic persisted labels, bounded state/error/operation/timestamp/progress/item metadata and never replays authorization after restart.
- [ ] Queue progress remains bounded to `0..10000`; completed item count does not exceed total count.
- [ ] Persisted `Queued`/`Running` rows become `Interrupted` on restart while retaining safe last-known progress/context and requiring fresh authorization for any new attempt.
- [ ] Caller cancellation during queue initialization/best-effort persistence does not permanently disable later metadata persistence in the same app session.
- [ ] Privacy mode redacts peer/file history labels and privacy-sensitive diagnostic identifiers.
- [ ] Schema-v6 History performance fields are optional/bounded; measured bytes never exceed logical size, resumed transfers count only post-offset bytes, legacy/unmeasured rows are not assigned rates, and invalid optional measurements cannot change the underlying transfer outcome.
- [ ] History retention/clear behavior removes performance metadata together with its owning history rows; numeric performance metadata adds no peer endpoint, content, credential, certificate, pairing capability, or reusable authorization.
- [ ] Optional Android/iOS/Mac Catalyst/Windows completion/failure notification text remains generic and content-free.
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
- [ ] Provision containing app and iOS Share Extension with App Group `group.in.sanskar.swiftdrop`.
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

- [ ] Build/sign the Mac Catalyst containing app with the supported Xcode/.NET MAUI toolchain.
- [ ] Verify app-sandbox network client/server entitlements and any containing-app App Group entitlement required by the shipped configuration.
- [ ] Verify Bonjour/local-network behavior and macOS firewall allow/block cases.
- [ ] Verify document/open-file security-scoped staging under signed sandbox conditions.
- [ ] Verify native `UIDropInteraction` for files, folders, text, and pairing links.
- [ ] Verify dropped source security-scoped lifetime, link/reparse rejection, shared count/per-file/aggregate budget, bounded collision deconfliction, and no auto-send.
- [ ] Delay native-drop provider file/text callbacks beyond the bounded response wait and verify cleanup instead of a hang.
- [ ] Return provider before timeout but let copy continue longer and verify the response timer does not kill the active copy.
- [ ] Verify VoiceOver, keyboard-only navigation, window resizing, and large text.
- [ ] Verify notarization/store packaging for the containing app; no Mac Catalyst Share Extension is expected in the maintained architecture.

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
- [ ] queue restart/interrupted metadata behavior, recovered progress/item display, and no stale authorization replay.
- [ ] normal completed file/text History samples display measured duration/rate where attributable;
- [ ] resumed-file History samples use only actual bytes transferred after the negotiated offset;
- [ ] zero-byte/failed/cancelled/paused/rejected/legacy rows do not display fabricated throughput;
- [ ] weighted History summary and English/Hindi presentation are validated on representative devices.

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
- [ ] Queue operation/progress/item/timing/interrupted-state information remains readable at large text sizes and in Hindi.
- [ ] Hindi layouts are checked for clipping/wrapping and long runtime messages.
- [ ] Reduced-motion/high-contrast preferences behave acceptably on supported targets.

## Packaging and publication

- [ ] iOS app and Share Extension version/build numbers are consistent.
- [ ] Android signing material, Windows signing material, Apple signing/provisioning, and store credentials remain outside the repository.
- [ ] Final Android AAB/APK, Windows package/MSIX, iOS/TestFlight build, and Mac Catalyst distribution artifact are tested after signing/packaging.
- [ ] Apple App Group and iOS extension entitlements are present in signed iOS artifacts, not only source plist files.
- [ ] Exact restored dependency/license/notice inventory is reviewed for final signed binaries and reconciled with the retained machine-readable evidence bundles.
- [ ] Store privacy declarations, local-network/foreground-service/App Group explanations, screenshots, descriptions, support links, and release notes match actual behavior.
- [ ] Git tag/release notes identify exact candidate commit and validation evidence.

## Production-ready rule

Do not describe a candidate as production-ready until required automated gates, signed-package checks, physical cross-device/network matrix, accessibility/localization checks, dependency/license review, and store/privacy review are complete for that exact commit.

## Native terminal notification release gate

- [ ] Notification preference defaults to Off and persists correctly when explicitly enabled/disabled.
- [ ] Android 13+ permission allow/deny and generic terminal delivery are verified on a signed build.
- [ ] Signed iOS verifies alert/sound authorization, foreground banner/sound, background/system delivery, system settings denial, generic English/Hindi text, and disabled preference behavior.
- [ ] Signed Mac Catalyst verifies the same local-notification behavior under the release sandbox/system settings.
- [ ] Windows signed package/MSIX verifies toast/COM registration, clean install/update, notification activation, startup registration for an already-enabled preference, system-settings suppression, and uninstall/update cleanup behavior.
- [ ] `scripts/validate_windows_integration.py` passes and confirms matching CLSIDs, exact activation arguments, handler-before-register ordering, local-only capability posture, and placeholder-free terminal messages.
- [ ] No platform notification contains filename/peer/path/content/pairing/transfer-ID/authorization data.
- [ ] Permission, registration, presentation, or activation failure cannot alter a transfer's success/failure state.

## Performance trend/export candidate checks

- [ ] Confirm the exact candidate passes the 26-helper/559-xUnit portable contract and target compile/audit matrix.
- [ ] Perform full/resumed measured transfers on representative physical devices and verify UTC daily trend math.
- [ ] Export the aggregate CSV and verify exact documented columns plus absence of file/device/path/network/auth/content identifiers.
- [ ] Verify OS share-sheet behavior, cancellation, repeated export/cache cleanup, History clear/prune behavior, English/Hindi presentation, large text, keyboard, and screen-reader access.
- [ ] Correlate representative-device/network trend evidence with synthetic benchmark results before making performance claims.

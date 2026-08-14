# SwiftDrop Next Steps

Updated: 2026-08-14

The current master-prompt source scope is implemented. This roadmap is intentionally about **verification, packaging, signed/provider/device/network evidence, defect closure, and optional post-v1 work** rather than listing already-completed iOS Share Extension, Mac drag/drop, canonical path, source-link, staging-budget, or schema-v3 resume work as missing.

## Source work completed through the August 14 continuation

### Two-OS portable and SQLite resource-lifetime hardening completed on August 14

- `ci.yml` now requires both Ubuntu portable verification and the Windows PowerShell verifier.
- Windows CI exposed and drove fixes for a PowerShell parser defect and SQLite native-handle/file-lock behavior that Ubuntu alone did not reveal.
- SQLite-backed test teardown clears idle pools before deleting isolated temp DB/`-wal`/`-shm` files; a direct regression protects the cleanup helper.
- Every Core SQLite storage component now disposes command objects deterministically, with readers/connections/transactions scoped around actual use.
- Current portable coverage is 517 xUnit tests plus 10 Python validation-helper tests; the full contract has passed on both Ubuntu and Windows.
- Platform compile/audit run `31786513898` validates the resulting source across Android, focused Windows, Mac Catalyst, iOS Share Extension, and iOS containing app.
- Same-ref CI/platform/CodeQL/security concurrency now prevents superseded intermediate commits from blocking the newest branch evidence.
- No additional source workaround is planned for SQLite file locking; future recurrence should be treated as a resource-lifetime regression and fixed rather than hidden with sleeps/retries.

### Release-evidence and verification automation completed on August 14

- Added strict machine-readable NuGet vulnerable-package report validation with direct/transitive finding detection and helper regression tests.
- Added deterministic SHA-256 dependency-evidence manifests with path confinement, exact byte lengths, stable ordering, and helper regression tests.
- Normal CI pins Python 3.13, runs the Python helper tests, and validates the Core machine-readable vulnerability report.
- Bash and PowerShell portable verification include documentation/localization/Apple/Core/test/benchmark/audit validation; Windows CI executes the PowerShell verifier directly.
- Added target-specific dependency/vulnerability evidence for Android, focused Windows, Mac Catalyst, iOS containing app, and iOS Share Extension.
- Added exact artifact contracts for `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit`.
- Platform run `31783405975` passed all maintained target builds/audits and produced hashed evidence bundles; downloaded Android/Windows/Apple internal manifests were independently verified against their report bytes.
- Release-readiness self-test run `31783537853` passed every portable/platform/audit job and its aggregate gate.
- Added deterministic pairing round-trip/canonical-alias property coverage and strict-JSON randomized robustness/duplicate-case coverage; portable xUnit count is now 517.
- The remaining dependency task is **manual exact-candidate provenance/license/final signed-artifact reconciliation**, not adding another source-level inventory command.

### Documentation completion through the August 14 continuation

- Added a canonical documentation index and complete end-user installation, user workflow, settings, FAQ, networking, troubleshooting, and privacy-safe diagnostic/reporting guides.
- Added repository structure, development workflow, CI-reference, versioning/compatibility, and release-process documentation.
- Expanded support, contribution, community conduct, security-disclosure, and usage-term documents.
- Synchronized public documentation navigation with the maintained iOS-only Share Extension, Mac native-drop architecture, .NET 10 `.slnx` workflow, explicit NuGet auditing, and current hosted compile/test evidence.
- Documentation completeness does not remove the remaining external release tasks below; the next engineering phase remains exact-candidate signing, packaging, physical cross-device/network/provider testing, accessibility/localization validation, dependency/license review, and store/privacy submission checks.

### Canonical pairing capability transport

- Pairing link decoder rejects surrounding whitespace.
- Raw outer query must contain exactly one `p=` field.
- Empty/unknown/duplicate query fields are rejected.
- Pairing payload text must be unpadded canonical Base64URL.
- Standard Base64 `+`, `/`, padding `=`, percent-encoded aliases, and non-canonical re-encodings are rejected.
- Decoded pairing JSON remains strict, duplicate-safe, and unknown-member rejecting.

### Canonical cross-platform manifest paths

- Added shared strict `PortableRelativePath` parser.
- Rooted/drive/UNC/device paths rejected portably.
- Empty/repeated/trailing separators and `.`/`..` rejected.
- Relative paths capped at 64 segments.
- Sender manifest paths canonicalized to `/` on every OS.
- Incoming wire manifest paths must already equal SwiftDrop's canonical sanitized form before authorization.
- Malformed/noncanonical file paths therefore do not consume valid one-time transfer authorization.
- Filename segments have both 180 UTF-16 code-unit and 180 UTF-8 byte caps.
- Collision names retain unique bounded markers even when the original name is already at the filename limit.

### Outgoing source and folder safety

- Added reusable regular-source/link validation.
- Single-file send rechecks source at actual stream-open boundary.
- Folder roots and descendants reject symlink/reparse sources.
- Folder recursion is explicit/bounded rather than unrestricted `AllDirectories` traversal.
- Folder source files are sorted deterministically for stable retry manifests.
- Portable case/Unicode/sanitation collisions are deconflicted before hashing.
- Count/per-file/aggregate/path-length limits are preflighted before expensive hashing where possible.
- Paused single/batch source lists retain files/folders only while they remain regular non-link/non-reparse sources.

### Stable batch UI/API cleanup

- Active XAML batch controls use the stable-ID workflow.
- Folder sources remain resumable.
- Obsolete duplicate batch handlers were removed from `MainPage`.
- Obsolete coordinator compatibility overload that implicitly generated a fresh ID was deleted.
- Batch transfer IDs use bounded ASCII token syntax.

### Completed-item retry race closure

- Completed destination is verified/re-hashed while the retry plan is built.
- After the sender returns the matching item-start frame, completed destination is verified **again immediately before zero-byte item ACK**.
- Mutation/removal/reparse/root/source/hash mismatch in that interval fails closed.

### External staging budgets and provider liveness

- Added reusable Core `TransferStagingBudget` for file count, per-file bytes, aggregate bytes, commit-after-success behavior.
- iOS Share Extension applies aggregate budget before copying an over-limit file.
- Android shares apply shared aggregate budget, treat negative provider size as unknown, cap unknown bytes to remaining aggregate budget, and preserve free-space reserve during unknown-length streaming.
- Mac native drop uses the same staging-budget policy.
- iOS Share Extension and Mac native drop have bounded provider-response waits.
- Provider-response timeout does not incorrectly cancel a legitimate copy that already began.
- Apple containing app validates exact physical App Group file set and preflights aggregate app-cache bytes before recopy.

### August 14 build/test/platform hardening

- Patched the bundled SQLite dependency path that blocked warnings-as-errors restore.
- Repaired hidden Core compile defects and test-project wiring exposed after restore became healthy.
- Restored and extended history maintenance behavior while keeping compatibility.
- Tightened strict UTF-8 decoding, filename canonicalization/idempotence, and staging-budget boundaries.
- Portable Core verification reached 511/511 passing tests and benchmark-project compilation.
- Repaired Android foreground-service, multicast-lock, intent/share staging, and nullable binding code until Android Release compilation passed in hosted CI.
- Removed unsupported `Entry.LineBreakMode` XAML usage.
- Corrected the Share Extension to its supported iOS-only target; Mac Catalyst remains the containing desktop app plus native drag/drop path.
- Removed stale Mac Catalyst extension entitlements and obsolete one-shot/duplicate workflows.
- Updated Apple metadata validation to the iOS-only extension architecture.
- Added hosted iOS simulator compilation that is certificate-independent while preserving real project entitlements for signed/device builds.
- Added focused Windows target-matrix controls so Windows CI does not traverse unrelated Android/iOS/Mac Catalyst workloads.
- Repaired WinUI activation/drag-event namespace ambiguities exposed by the first real Windows source compile.
- Marked the localization XAML extension service-provider independent to remove repeated XAML compiler service-provider warnings.

### Documentation/test alignment

- Protocol wire/security docs define canonical pairing/path/ID/resume representation.
- Threat model includes source-tree links, canonical aliases, staging budgets, collision-byte limits, and repeated completed-item verification.
- Security/manual/release test documents require the new invariants.
- Public/project/platform status documents are synchronized as source/platform evidence is verified.

## P0 — Observe automated gates on the exact final candidate

Before a production tag, confirm the **exact final commit** has successful runs for:

1. portable Core restore/build;
2. full portable xUnit suite;
3. localization key/value/placeholder validation;
4. Apple App Group/iOS Share Extension metadata validation;
5. synthetic benchmark-project compilation;
6. Android compile job;
7. Windows compile job;
8. Mac Catalyst containing-app compile job;
9. unsigned/certificate-independent iOS Simulator Share Extension compile job;
10. unsigned/certificate-independent iOS Simulator containing-app compile job;
11. CodeQL/security-hygiene jobs;
12. release-readiness aggregate gate;
13. exact dependency inventory artifacts;
14. zero-finding validation for direct/transitive vulnerable-package JSON;
15. deterministic evidence manifests for portable, Android, Windows, and Apple audit bundles;
16. exact-candidate verification that retained report bytes match their manifest SHA-256 digests.

Do not infer a pass from missing status contexts. If the connector/API reports no check contexts, record **unknown/unreported** and inspect the Actions UI/logs directly before release.

## P0 — Compile/test focus for the hardened Core boundaries

The exact candidate must compile/run tests covering:

- canonical pairing raw query/Base64URL/whitespace rejection;
- strict decoded pairing unknown/duplicate fields;
- portable path rooted/traversal/empty-segment/depth rejection;
- forward-slash canonical wire manifest paths;
- noncanonical sanitized path rejection;
- malformed path rejected before authorization callback/nonce consumption;
- transfer-ID token syntax;
- UTF-8 filename byte limits and rune/surrogate safety;
- max-length collision marker preservation;
- receive collision reservation uniqueness;
- regular source file/directory validation;
- single-file send-boundary symlink rejection;
- bounded deterministic source enumeration;
- symlinked source file/directory rejection;
- deterministic repeated folder manifests;
- portable sender path deconfliction;
- file/folder paused-source filtering;
- reusable staging budgets;
- exact Apple share package file sets;
- completed-file mutation between repeated verification passes.

## P0 — Signed Apple validation

The Apple source cannot be considered release-validated until the real provisioning/signing environment confirms:

- App Group `group.in.sanskar.swiftdrop` exists in Apple Developer configuration;
- containing iOS app and iOS Share Extension provisioning profiles include the same App Group;
- app ID `in.sanskar.swiftdrop` and extension ID `in.sanskar.swiftdrop.share` are valid;
- source entitlement/App Group metadata matches the signed artifact;
- iOS Share Extension appears for supported file/image/movie/text/web URL inputs;
- files/images/movies/text/URLs import into SwiftDrop without auto-sending;
- cold-start and warm-start App Group import work;
- stale/malformed/extra-file/nested-directory/symlinked packages fail safely;
- provider callbacks delayed beyond the configured response timeout fail/clean up without hanging;
- provider callbacks returned before the timeout can complete a legitimate copy that takes longer than the response timeout;
- extension dismissal/cancellation stops bounded staging safely;
- aggregate Share Extension staging budget prevents copying the file that exceeds aggregate limits;
- containing app preflights aggregate validated package bytes before App Group→cache recopy;
- multiple pending packages are not silently merged/deleted while one package is under review;
- security-scoped provider representations remain valid long enough for bounded staging;
- Mac Catalyst containing app works under release sandbox/App Group configuration;
- Mac native drop works for files, folders, text, and pairing links;
- Mac native-drop provider timeout behavior matches source semantics;
- symlink/reparse inputs are rejected as designed;
- notarization/TestFlight/store packaging signs/embeds the iOS extension correctly and signs the Mac Catalyst app correctly.

## P0 — Signed Android validation

- signed AAB/APK install and upgrade;
- ACTION_SEND/ACTION_SEND_MULTIPLE with provider URIs whose sizes are declared, null, negative, wrong, or changing;
- multiple files near/exceeding aggregate staging budget;
- unknown-length provider capped by remaining aggregate budget;
- reduce cache free space during unknown-length provider copy and verify storage-reserve checks stop/clean the copy;
- partial provider failure followed by valid item, confirming failed copy does not consume budget;
- foreground data-sync service behavior;
- Android 13+ notification permission behavior when optional notifications are enabled;
- multicast-lock/discovery behavior on physical Wi-Fi;
- canonical folder transfer paths to Windows/iOS/Mac receivers;
- backup remains disabled for app-local metadata;
- TalkBack, large text, Hindi wrapping, background/vendor battery restrictions.

## P0 — Signed Windows validation

- signed/package install and update;
- private-network capability/firewall behavior;
- protocol activation;
- receive-folder picker persistence;
- native files/folders/text/pair-link drop;
- direct source symlink/reparse rejection;
- Windows folder sender emits `/` wire manifest paths;
- Windows→Android, Windows→iOS, and Windows→Mac folder transfers negotiate exact matching paths;
- maximum-length/Unicode/collision filenames remain bounded/distinct on NTFS/package runtime;
- keyboard/Narrator/high-contrast/large-text behavior.

## P0 — Cross-device transfer matrix

Use synthetic files only and validate every supported direction across representative devices:

- Android ↔ Android;
- Android ↔ Windows;
- Android ↔ iOS;
- Android ↔ Mac Catalyst;
- Windows ↔ Windows;
- Windows ↔ iOS;
- Windows ↔ Mac Catalyst;
- iOS ↔ iOS;
- iOS ↔ Mac Catalyst;
- Mac Catalyst ↔ Mac Catalyst.

For each direction test:

- QR/deep-link pairing;
- canonical invitation rejection variants;
- nearby discovery/pairing;
- one-time code/manual local-IP fallback;
- single file;
- zero-byte file;
- multi-file;
- recursive folder where platform source selection allows it;
- canonical `/` relative paths;
- selected/source-tree link rejection where filesystem permits;
- text snippet;
- receiver reject;
- selective batch accept;
- pause/resume;
- cancel;
- network interruption/retry;
- already-completed batch item retry;
- mutation after retry plan before zero-byte completed-item ACK;
- collision handling including max-length Unicode names;
- low storage;
- large file/many-file batch;
- changed source during send;
- changed receiver partial/destination during resume;
- receiver final-path race.

## P0 — Idempotent batch-resume physical validation

1. start a batch containing several files and a selected folder in at least one run;
2. allow at least one file to finalize;
3. interrupt during a later file;
4. create fresh pairing authorization;
5. resume from the app Resume control;
6. confirm the same stable transfer ID is retained;
7. confirm folder source remains in the resume candidate list while it remains regular/non-link;
8. confirm the completed file is re-hashed while planning and negotiated at full offset;
9. confirm no `name (1)` duplicate is created for that already-completed item;
10. alter/delete the completed destination before retry and confirm it is not trusted as complete;
11. alter/delete the completed destination after plan creation but before item ACK and confirm the second verification fails closed;
12. replace a paused source with a symlink/reparse point and confirm it is removed/rejected;
13. start a brand-new explicit send of the same sources and confirm a fresh transfer ID preserves normal duplicate/collision semantics.

## P1 — Canonical path/filesystem validation

- Windows drive/UNC/device path rejection on every target;
- repeated/trailing separators and empty segments;
- `.`/`..` traversal;
- backslash incoming manifest path rejection;
- more than 64 path segments;
- decomposed Unicode / reserved Windows name / invalid-character wire aliases rejected rather than rewritten;
- max UTF-8 filename segment and `.swiftdrop.part` headroom;
- max-length collision marker preservation;
- case/Unicode/sanitation collision deconfliction;
- selected/source-tree symlink/reparse rejection;
- receive-root symlink/reparse rejection;
- concurrent same-name receive reservation pressure;
- final-path create-after-reservation race;
- filesystem/root semantics on NTFS/APFS and representative Android filesystem behavior.

## P1 — Network/lifecycle/security validation

- guest Wi-Fi/client isolation;
- multicast filtered but direct IP allowed;
- firewall blocked/allowed;
- IPv4-only and IPv6-capable LANs;
- device switches networks during transfer;
- mobile app foreground/background/sleep/lock transitions;
- local-network permission deny/allow on Apple;
- low-storage race during active receive/external staging;
- receiver destination modified by another process;
- real SecureStorage/keychain/keystore lock/restore/upgrade scenarios;
- database v1/v2 real upgrade to v3;
- corrupt local metadata recovery;
- repeated invalid pairing/request pressure.

## P1 — Accessibility/localization validation

- TalkBack;
- VoiceOver on iOS/Mac Catalyst;
- Narrator;
- keyboard-only desktop navigation;
- largest supported text scaling;
- high contrast;
- reduce motion;
- screen rotation/window resizing;
- Hindi layout, wrapping, fallback, runtime dialogs/statuses, and technical values.

Any untranslated, clipped, inaccessible, or focus-order issue found here should be fixed before release and then added to regression documentation/tests where feasible.

## P1 — Release dependency/license evidence

For the exact signed candidate:

- download `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit` from the exact release-readiness run;
- independently verify each retained bundle's report lengths/SHA-256 digests against `manifest.json`;
- inspect Core/App/test/benchmark and every shipped target graph, including the separate iOS Share Extension graph;
- confirm the machine-readable vulnerable-package reports contain no findings under the configured advisory data;
- manually review package provenance, licenses, notices, redistribution obligations, and platform/runtime components;
- compare hosted restored/simulator/unpackaged evidence with the final signed AAB/APK, MSIX/package, iOS archive/extension, and Mac Catalyst distribution artifacts;
- generate/review final third-party notices from the exact shipped graph;
- verify Apache-2.0 project license/NOTICE contents;
- verify no signing/private-key/local-database artifacts entered the repository;
- retain verified dependency/license evidence with release artifacts.

## P2 — Optional post-v1 enhancements

These are optional product improvements, not missing correctness work in the current master-prompt scope:

- native optional completion/failure system notifications on iOS/Mac Catalyst/Windows;
- additional OS-supported background continuation where store policy permits it;
- richer transfer queue persistence without persisting authorization;
- broader localization beyond English/Hindi;
- representative-device performance dashboard/history;
- trustworthy platform malware-scan integration only where a supported OS API exists;
- additional property/fuzz/state-machine testing beyond current coverage.

## Production-ready definition

Do not label SwiftDrop production-verified until all of the following are true for the exact release candidate:

- automated source gates pass;
- all target apps/extensions compile under release workloads;
- signed packages install/upgrade successfully;
- Apple App Group/iOS Share Extension provisioning is valid;
- real provider/ContentResolver/native-drop paths match the staged budget/timeout semantics;
- physical cross-device transfer/resume/path/link/network tests pass;
- accessibility/localization checks pass;
- privacy/security documentation matches the binary;
- final dependency/license review is complete;
- store metadata/declarations/screenshots match shipped behavior.

Until then, describe the repository as source-complete for the current scope but still undergoing release validation.

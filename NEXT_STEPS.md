# SwiftDrop Next Steps

Updated: 2026-08-11

The current master-prompt source scope is implemented. This roadmap now focuses on **verification, packaging, evidence, and optional enhancements** rather than listing already-completed Apple Share Extension, Mac drag/drop, typed protocol-hostability, pairing closed-schema, App Group package hardening, or batch-resume work as missing.

## Source work completed in the latest continuation

In addition to the previously completed Share Extension/Mac drop/typed protocol/schema-v3 work, the latest hardening pass now includes:

- closed-schema pairing payload JSON: unknown encoded fields are rejected rather than ignored;
- completed-batch destination re-verification immediately before zero-byte item ACK, not only while creating the retry plan;
- regression coverage for destination mutation between those two verification passes;
- Apple Share Extension provider waits bounded by a response timeout;
- extension lifetime cancellation propagated through provider waits and staged file copying;
- late timed-out/cancelled provider callbacks prevented from starting new copies;
- provider-response timeout separated from already-started large-copy duration;
- App Group importer requires the physical `files/` set to exactly match manifest-declared files;
- undeclared extra files and nested directories rejected;
- portable exact package file-set validator and tests;
- portable filename segments explicitly reject both `/` and `\\` as data;
- filename length cap made unconditional even for pathological extensions;
- UTF-16 surrogate pairs are not split at the filename boundary;
- release checklist, threat model, security test plan, protocol security document, third-party notice process, and project status aligned with the current source.

## P0 — Observe automated gates on the release candidate

Before a production tag, confirm the **exact candidate commit** has successful runs for:

1. portable Core build/tests;
2. localization parity/placeholder validation;
3. Apple App Group/Share Extension metadata validation;
4. synthetic benchmark-project compilation;
5. Android compile job;
6. Windows compile job;
7. Mac Catalyst **extension + containing app** compile jobs;
8. unsigned iOS Simulator **extension + containing app** compile jobs;
9. CodeQL/security-hygiene jobs;
10. release-readiness dependency inventory/audit jobs;
11. release-readiness aggregate gate.

Do not convert an absent connector status into success. Confirm the Actions UI/logs or equivalent CI evidence for the exact candidate.

## P0 — Signed Apple validation

The Apple source cannot be considered release-validated until the real provisioning/signing environment confirms:

- App Group `group.in.sanskar.swiftdrop` exists in the Apple Developer account;
- containing app and Share Extension provisioning profiles include the same App Group;
- app ID `in.sanskar.swiftdrop` and extension ID `in.sanskar.swiftdrop.share` are valid;
- app/extension version and build numbers match release expectations;
- iOS Share Extension appears in the system share sheet for supported content;
- files/images/movies/text/URLs import into SwiftDrop without auto-sending;
- cold-start and warm-start App Group import work;
- only one pending share bundle replaces the active review surface at a time; later pending packages remain available for later activation/import;
- stale/malformed/unknown-field packages are rejected;
- App Group packages containing undeclared extra files or nested directories are rejected;
- symlink/reparse package/file entries are rejected where the filesystem exposes them;
- an `NSItemProvider` that never answers fails within the bounded response wait;
- dismissing/cancelling the extension breaks pending waits and stops active copy loops;
- a provider that responds promptly can complete a legitimate longer local copy without being killed by the response timer alone;
- temporary provider/security-scoped access is released after staging;
- Mac Catalyst Share Extension works under release sandbox;
- Mac native drag/drop works for files, folders, text, and pairing links;
- notarization/TestFlight/store packaging embeds and signs the extension correctly.

## P0 — Signed Android and Windows validation

Android:

- signed AAB/APK install and upgrade;
- `ACTION_SEND`/`ACTION_SEND_MULTIPLE` with provider URIs whose sizes are known and unknown;
- oversized/provider-disappears/cancellation cleanup cases;
- foreground data-sync service behavior;
- Android 13+ notification permission behavior when optional notifications are enabled;
- multicast-lock/discovery behavior on physical Wi-Fi;
- backup remains disabled for app-local metadata.

Windows:

- signed/package install and update;
- private-network capability/firewall behavior;
- protocol activation;
- receive-folder picker persistence;
- native files/folders/text/pair-link drop;
- portable filenames containing foreign separator characters;
- keyboard/Narrator/high-contrast behavior.

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
- nearby discovery/pairing;
- one-time code/manual local IP fallback;
- unknown/duplicate/malformed pairing JSON rejection;
- single file;
- zero-byte file;
- multi-file;
- recursive folder where platform source selection allows it;
- text snippet;
- receiver reject;
- selective batch accept;
- pause/resume;
- cancel;
- network interruption/retry;
- already-completed batch item retry;
- collision handling;
- low storage;
- large file/many-file batch;
- changed source during send;
- changed receiver partial/destination during resume;
- receiver final-path race;
- filenames with Unicode, reserved names, `/`/`\\` separator characters, and pathological long-extension cases.

## P0 — Idempotent batch-resume physical validation

The source path is implemented and portable-tested. Validate the real flow:

1. start a batch containing several files;
2. allow at least one file to finalize;
3. interrupt during a later file;
4. create fresh pairing authorization;
5. resume;
6. confirm the completed file is re-hashed while the retry plan is created;
7. confirm no `name (1)` duplicate is created for that already-completed item;
8. after the plan arrives but before the completed item is acknowledged, mutate/delete/replace the completed destination if the test harness can reproduce that race;
9. confirm the receiver performs the second verification and does **not** falsely acknowledge completion;
10. retry again and confirm safe transfer behavior;
11. start a brand-new explicit send of the same sources and confirm a fresh transfer ID preserves normal duplicate/collision semantics.

## P1 — Network/lifecycle/security validation

- guest Wi-Fi/client isolation;
- multicast filtered but direct IP allowed;
- firewall blocked/allowed;
- IPv4-only and IPv6-capable LANs;
- device switches networks during transfer;
- mobile app foreground/background/sleep/lock transitions;
- local-network permission deny/allow on Apple;
- low-storage race during active receive;
- receiver destination modified by another process;
- real SecureStorage/keychain/keystore lock/restore/upgrade scenarios;
- database v1/v2 real upgrade to v3;
- corrupt local metadata recovery;
- repeated invalid pairing/connection pressure;
- connection close at each protocol transition.

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

- download dependency inventory artifacts from release-readiness;
- inspect Core/App/test/benchmark dependencies;
- inspect Share Extension iOS and Mac Catalyst restored dependency graphs;
- generate/review final third-party notices from the exact restored graph;
- verify Apache-2.0 project license/NOTICE contents;
- verify no signing/private-key/local-database artifacts entered the repository;
- retain license evidence with release artifacts.

A Share Extension with no direct NuGet `PackageReference` still has a restored dependency/runtime graph through Core and the Apple/.NET target packs; release review must include it.

## P2 — Optional post-v1 enhancements

These are optional product improvements, not missing correctness work in the current scope:

- native optional completion/failure system notifications on iOS/Mac Catalyst/Windows;
- additional OS-supported background continuation where store policy permits it;
- richer user-facing handling for multiple queued external share bundles without auto-merging/overwriting the active review selection;
- richer transfer queue persistence without persisting authorization;
- broader localization beyond English/Hindi;
- representative-device performance dashboard/history;
- trustworthy platform malware-scan integration only where a supported OS API exists;
- additional property/fuzz/state-machine testing beyond the current deterministic/fuzz coverage.

## Production-ready definition

Do not label SwiftDrop production-verified until all of the following are true for the exact release candidate:

- automated source/security/platform gates pass;
- all target apps/extensions compile under release workloads;
- signed packages install/upgrade successfully;
- Apple App Group/Share Extension provisioning and runtime behavior are valid;
- physical cross-device transfer/resume/network tests pass;
- provider timeout/cancellation and App Group tamper tests pass on Apple targets;
- accessibility/localization checks pass;
- privacy/security documentation matches the binary;
- final dependency/license review is complete;
- store metadata/declarations/screenshots match shipped behavior.

Until then, describe the repository as source-complete for the current scope but still undergoing release validation.

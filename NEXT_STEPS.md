# SwiftDrop Next Steps

Updated: 2026-08-10

The current master-prompt source scope is implemented. This roadmap now focuses on **verification, packaging, evidence, and optional enhancements** rather than listing already-completed Apple Share Extension, Mac drag/drop, protocol-hostability, or batch-resume work as missing.

## Source work completed in the latest continuation

- Added real iOS/Mac Catalyst Share Extension target.
- Added App Group entitlements to containing app and extension.
- Added strict versioned App Group package manifest and Core validator.
- Added atomic `.staging-*` → `pending-*` Apple share publication.
- Added containing-app App Group importer with strict JSON, unknown-field rejection, stale-package pruning, symlink/reparse rejection, exact length checks, and cache re-staging.
- Added native Mac Catalyst files/folders/text/pair-link drag/drop with temporary security-scoped access.
- Added Android share staging parity: count/size/capacity bounds, portable names, exact provider length where available, runtime byte cap, failure cleanup, atomic inbox handoff.
- Added atomic Windows drop handoff.
- Added rune-safe shared UTF-8 truncation.
- Added shared typed application wire records/factory/validator/authorizer.
- Production sender, pairing client, receiver, and portable tests now use the same wire records.
- Added strict unknown-member rejection in framed JSON.
- Added full file/batch/text/pair conversation tests using production wire models.
- Extracted active receive-session tracking/draining into portable Core with race tests.
- Added receive-root symlink/reparse rejection.
- Added stable batch IDs across pause/resume/retry.
- Added SQLite schema v3 completed-batch metadata.
- Added verified completed-file re-hashing so interrupted batches do not resend finalized files.
- Added Apple integration metadata validator to portable CI/release verification.
- Added explicit Share Extension compile/dependency gates for Mac Catalyst and unsigned iOS Simulator.

## P0 — Observe automated gates on the release candidate

Before a production tag, confirm the exact candidate commit has successful runs for:

1. portable Core build/tests;
2. localization parity/placeholder validation;
3. Apple App Group/Share Extension metadata validation;
4. synthetic benchmark-project compilation;
5. Android compile job;
6. Windows compile job;
7. Mac Catalyst **extension + containing app** compile jobs;
8. unsigned iOS Simulator **extension + containing app** compile jobs;
9. CodeQL/security-hygiene jobs;
10. release-readiness aggregate gate.

The GitHub connector used during development has returned no status contexts for recent direct-main commits, so absence of reported contexts must not be converted into a success claim.

## P0 — Signed Apple validation

The new Apple source cannot be considered release-validated until the real provisioning/signing environment confirms:

- App Group `group.in.sanskar.swiftdrop` exists in the Apple Developer account;
- containing app and Share Extension provisioning profiles include the same App Group;
- app ID `in.sanskar.swiftdrop` and extension ID `in.sanskar.swiftdrop.share` are valid;
- iOS Share Extension appears in the system share sheet for supported content;
- files/images/movies/text/URLs import into SwiftDrop without auto-sending;
- cold-start and warm-start App Group import work;
- stale/malformed package cleanup works;
- security-scoped provider representations remain valid long enough for bounded staging;
- Mac Catalyst Share Extension works under release sandbox;
- Mac native drag/drop works for files, folders, text, and pairing links;
- symlink/reparse inputs are rejected as designed;
- notarization/TestFlight/store packaging includes the extension correctly.

## P0 — Signed Android and Windows validation

Android:

- signed AAB/APK install and upgrade;
- ACTION_SEND/ACTION_SEND_MULTIPLE with provider URIs whose sizes are known/unknown;
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
- single file;
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
- receiver final-path race.

## P0 — Idempotent batch-resume physical validation

The source path is implemented and portable-tested. Validate the real flow:

1. start a batch containing several files;
2. allow at least one file to finalize;
3. interrupt during a later file;
4. create fresh pairing authorization;
5. resume;
6. confirm the completed file is re-hashed and negotiated at full offset;
7. confirm no `name (1)` duplicate is created for that already-completed item;
8. alter/delete the completed destination and repeat; confirm it is **not** trusted as complete;
9. start a brand-new explicit send of the same sources; confirm a fresh transfer ID preserves normal duplicate/collision semantics.

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
- corrupt local metadata recovery.

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
- inspect Share Extension iOS and Mac Catalyst dependency graphs;
- generate/review final third-party notices from the exact restored graph;
- verify Apache-2.0 project license/NOTICE contents;
- verify no signing/private-key/local-database artifacts entered the repository;
- retain license evidence with release artifacts.

## P2 — Optional post-v1 enhancements

These are optional product improvements, not missing correctness work in the current scope:

- native optional completion/failure system notifications on iOS/Mac Catalyst/Windows;
- additional OS-supported background continuation where store policy permits it;
- richer transfer queue persistence without persisting authorization;
- broader localization beyond English/Hindi;
- representative-device performance dashboard/history;
- trustworthy platform malware-scan integration only where a supported OS API exists;
- additional property/fuzz/state-machine testing beyond the current deterministic/fuzz coverage.

## Production-ready definition

Do not label SwiftDrop production-verified until all of the following are true for the exact release candidate:

- automated source gates pass;
- all target apps/extensions compile under release workloads;
- signed packages install/upgrade successfully;
- Apple App Group/Share Extension provisioning is valid;
- physical cross-device transfer/resume/network tests pass;
- accessibility/localization checks pass;
- privacy/security documentation matches the binary;
- final dependency/license review is complete;
- store metadata/declarations/screenshots match shipped behavior.

Until then, describe the repository as source-complete for the current scope but still undergoing release validation.

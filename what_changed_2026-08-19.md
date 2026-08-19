# SwiftDrop — What Changed (2026-08-19)

Repository: https://github.com/sanskarIN/SwiftDrop  
Merged hardening branch: `post-v1-hardening-20260819`  
Merged pull request: #26  
Current continuation branch: `release-evidence-hardening-20260819`

This addendum records the August 19 continuation work without rewriting the cumulative `what_changed.md` engineering ledger. It must be read together with that cumulative ledger.

## Scope decision

The maintained roadmap already classifies the current master-prompt source scope as source-complete and identifies the remaining P0/P1 work as signed-package, physical-device, provider, network, filesystem, accessibility/localization, dependency/license, and store validation.

No maintained source `TODO`, `FIXME`, or `NotImplementedException` marker was found during this continuation sweep.

Accordingly, this continuation implements optional post-v1 deterministic hardening and adds machine-readable support for the external release-validation phase. It does not manufacture source changes merely to increase commit count and does not claim external release validation that cannot be performed in hosted repository tooling.

## Added deterministic staging-budget state model

File:

- `tests/SwiftDrop.Core.Tests/TransferStagingBudgetStateMachineTests.cs`

Commit:

- `6bd17bceeefb14ea63aef8e2bd1de601b8a1c5e7` — `test(staging): add deterministic budget state model`

Coverage:

- 5,000 seeded operations;
- valid and invalid file lengths;
- zero-byte files;
- per-file overflow;
- aggregate overflow;
- file-count exhaustion;
- `EnsureCanStage` non-consuming behavior;
- `Commit` consuming behavior;
- committed/remaining counters;
- maximum-next-file calculation;
- reference-model comparison after every operation.

## Added destination-reservation filesystem state model

File:

- `tests/SwiftDrop.Core.Tests/DestinationReservationSetStateMachineTests.cs`

Commit:

- `cd43c288ad4a64ab7b7637f56554dc9bf69922b5` — `test(receive): add destination reservation state model`

Coverage:

- 3,000 seeded operations;
- repeated reservation pressure across a small destination-name set;
- reservation uniqueness under collision pressure;
- reservation visibility through `IsReserved`;
- randomized lease release;
- external completed-file creation/deletion between reservation operations;
- re-use only after reservation release and filesystem availability;
- path-comparison semantics inherited from `PathComparisonPolicy`.

## Added mixed-limit concurrency-gate queue regression

File:

- `tests/SwiftDrop.Core.Tests/AsyncConcurrencyGateQueueTests.cs`

Commit:

- `77d332d3a146c03c207d454e48b4464fba468367` — `test(concurrency): cover canceled head waiter dispatch`

Coverage:

- a restrictive queued head waiter;
- an otherwise eligible follower with a larger concurrency limit;
- cancellation of the restrictive head;
- immediate dispatch of the eligible follower without requiring an unrelated active lease to finish.

This protects cancellation cleanup and FIFO queue progress semantics.

## Added one-time pairing-code lifecycle state model

File:

- `tests/SwiftDrop.Core.Tests/OneTimePairingCodeManagerStateMachineTests.cs`

Commit:

- `097813e097ff2001f280b789efebd88c25d8cde8` — `test(pairing): add one-time code state model`

Coverage:

- 4,000 seeded lifecycle operations;
- create/replace;
- exact expiry timestamp;
- 8-digit decimal format;
- valid one-time consumption;
- replay prevention;
- wrong-code rejection;
- malformed-code rejection;
- explicit invalidation;
- exact-expiry rejection.

## Added mixed-outcome session-drain stress coverage

File:

- `tests/SwiftDrop.Core.Tests/AsyncSessionTrackerStressTests.cs`

Commit:

- `d3b94a78f788483de1efc4206d7bcaa7e24363c8` — `test(networking): stress mixed session drain outcomes`

Coverage:

- 128 simultaneously tracked sessions;
- deterministic shuffled completion order;
- successful and faulted sessions mixed together;
- drain begins while every session is active;
- drain completes only after the entire tracked set reaches terminal state;
- terminal tracker count returns to zero.

## Added one-time pairing-code concurrency coverage

File:

- `tests/SwiftDrop.Core.Tests/OneTimePairingCodeManagerConcurrencyTests.cs`

Commit:

- `040e3feb0cb2c2da9b61f9934f794a9009c0fb09` — `test(pairing): enforce single concurrent code consume`

Coverage:

- 64 simultaneous attempts against one valid code;
- exactly one successful consumer;
- all later replay attempts rejected.

## Added reservation-disposal concurrency coverage

File:

- `tests/SwiftDrop.Core.Tests/DestinationReservationSetDisposalTests.cs`

Commit:

- `31731b4d358dfca00756e075e3fdf6b37d09bdfb` — `test(receive): verify reservation disposal idempotency`

Coverage:

- 32 concurrent `Dispose` calls against one lease;
- reservation removal occurs safely once;
- original destination becomes reservable again after disposal.

## Added zero-byte-only staging-budget coverage

File:

- `tests/SwiftDrop.Core.Tests/TransferStagingBudgetZeroAggregateTests.cs`

Commit:

- `8d89ae52afcced0e6717227b33fef92252441a4d` — `test(staging): cover zero-byte-only aggregate budget`

Coverage:

- aggregate byte limit of zero;
- per-file byte limit of zero;
- configured zero-byte file count remains enforceable;
- positive-length staging remains rejected;
- file-count exhaustion remains closed.

## Deterministic testing documentation

Added:

- `docs/testing/deterministic-state-models.md`;
- canonical docs-index links to the deterministic testing guide and this dated ledger.

The guide records seed stability, bounded randomized inputs, deterministic synchronization, temporary-filesystem isolation, failure-handling rules, and the distinction between portable automated evidence and signed/device validation.

PR #26 was merged to `main` with merge commit:

- `f25f9ff65ddeb538f408bc9a1884ee141172e63c`.

The PR-head CI/CodeQL/security/release-readiness runs were queued at merge time and therefore were not recorded as passing evidence.

## Portable xUnit test-count effect

The pre-continuation portable xUnit baseline recorded by the repository was 572 tests.

The deterministic hardening tranche adds eight xUnit facts, so the expected suite size is **580 tests** once the applicable exact-head CI reports the suite. This document does not convert that expected count into passing evidence without a completed run.

## Added strict manual release-evidence validator

File:

- `scripts/validate_manual_release_evidence.py`.

Initial commit:

- `4c826c0a9eeb6a88f5ab69b202410793f4a38098` — `feat(release): add strict manual evidence validator`.

The validator uses only the Python standard library and enforces a closed schema for the exact candidate commit/version plus nine required validation groups:

- Android;
- Windows;
- iOS;
- Mac Catalyst;
- cross-device;
- filesystem;
- accessibility/localization;
- dependency/license;
- store.

It rejects missing/unknown/duplicate groups and cases, malformed candidate identifiers/timestamps, inconsistent aggregate group states, malformed terminal-case evidence, duplicate evidence references, and common private-key/pairing-capability leakage markers.

## Added structural release-evidence regression coverage

File:

- `scripts/tests/test_validate_manual_release_evidence.py`.

Commit:

- `109492dfa018104245dddd13c1657ca40f2ef215` — `test(release): cover manual evidence validator`.

This adds ten Python helper tests covering a complete not-run structure, terminal evidence requirements, aggregate status consistency, missing groups, duplicate cases, unknown fields, canonical commit syntax, blocked-case notes, pairing-capability leakage, and a valid mixed/in-progress group.

## Added canonical release-evidence template

File:

- `docs/release/manual-release-evidence.template.json`.

Commit:

- `c4abc3026d943e434495bab00fb68c850c7ef0b0` — `docs(release): add manual evidence manifest template`.

The template enumerates every currently required high-level external validation case with accurate initial `not-run` state rather than pre-marking release work as passed.

## Added checked-in template validation

File:

- `scripts/tests/test_manual_release_evidence_template.py`.

Commit:

- `f21b94a40533f95d9e753cec0a5fccedf2414817` — `test(release): validate checked-in evidence template`.

The Python helper suite now verifies that the committed template remains structurally valid whenever the validator contract changes.

## Added complete-candidate evidence mode

Commit:

- `95f5a0a3055742ca5e24fafc99dea7cf57437958` — `feat(release): add complete evidence gate mode`.

`--require-complete` additionally requires:

- a non-placeholder candidate commit SHA;
- every required group to be `passed`;
- therefore every required case to contain terminal timestamp, environment, and evidence.

Structural validation remains available for an in-progress candidate. This prevents a schema-valid but unexecuted template from being confused with a release-complete evidence record.

The same hardening pass applies sensitive-text rejection to environment strings and evidence references in addition to notes.

## Added complete-mode regression coverage

File:

- `scripts/tests/test_manual_release_evidence_complete_mode.py`.

Commit:

- `a4dd1117f64fd5fe86f620276637f0eb647b516f` — `test(release): cover complete evidence gate mode`.

Coverage includes:

- the checked-in placeholder template must fail complete mode;
- a fully populated all-passed candidate can pass complete mode;
- a partially completed group cannot pass complete mode;
- a pairing capability cannot be hidden inside an evidence reference.

## Added manual release-evidence documentation

Files/commits:

- `docs/release/manual-release-evidence.md` — `f9e5e88a0fbcb428e243aeea14682df0460b3313`;
- canonical documentation-index link — `ba06ba2e7e016597e1c2d49e1bb4c783ed195c08`.

The guide documents the two validation modes, status aggregation, terminal evidence requirements, privacy/secret boundaries, exact-candidate usage, and the relationship between automated CI and real signed/device/store validation.

## Canonical UTC timestamp hardening

Commits/files:

- `f3c7d765f726b1d44b7ea5bcc87bdcba0273eed3` — `fix(release): enforce canonical UTC evidence timestamps`;
- `e2cc7dd5b6f6fb4a5b078091a0bcbe9c6ef6d687` — `test(release): cover canonical evidence timestamps`;
- `scripts/tests/test_manual_release_evidence_timestamps.py`.

The validator now requires exact `YYYY-MM-DDTHH:MM:SS[.fraction]Z` timestamps rather than accepting broader aliases supported by Python's ISO parser. Dedicated regressions reject a space-separated timestamp and an explicit `+00:00` alias while preserving canonical fractional UTC timestamps.

## Python helper test-count effect

The repository baseline before this release-evidence tranche was 26 Python helper tests.

This tranche adds **18 Python helper tests**, so the expected helper-suite size is **44 tests** once the exact continuation head completes CI. This is an expected count, not a passing-evidence claim until CI completes.

## Runtime behavior

The August 19 deterministic-test and release-evidence tranches do not change production application runtime source. They strengthen regression coverage and make remaining external validation status machine-readable and harder to overstate.

## External release boundary remains unchanged

Still not verifiable from repository-only hosted tooling:

- signed Android AAB/APK install/upgrade and physical provider behavior;
- signed Windows package install/update, capabilities, toast/COM activation, and firewall behavior;
- Apple Developer App Group/provisioning, signed iOS Share Extension runtime, TestFlight/App Store embedding, and signed/notarized Mac Catalyst behavior;
- representative physical cross-device transfer matrix;
- real LAN/multicast/firewall/network-switching behavior;
- physical filesystem/symlink/reparse behavior across target filesystems;
- accessibility and localization validation on real target UI stacks;
- exact signed-candidate dependency/license/provenance reconciliation;
- store metadata, screenshots, declarations, and privacy review.

The new evidence manifest records those gates; it does not execute them.

Do not convert any unexecuted external gate into a pass.

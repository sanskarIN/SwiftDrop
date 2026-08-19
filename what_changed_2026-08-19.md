# SwiftDrop — What Changed (2026-08-19)

Repository: https://github.com/sanskarIN/SwiftDrop  
Working branch: `post-v1-hardening-20260819`  
Pull request: #26

This addendum records the August 19 continuation work without rewriting the cumulative `what_changed.md` engineering ledger. It must be read together with that cumulative ledger.

## Scope decision

The maintained roadmap already classifies the current master-prompt source scope as source-complete and identifies the remaining P0/P1 work as signed-package, physical-device, provider, network, filesystem, accessibility/localization, dependency/license, and store validation.

No maintained source `TODO`, `FIXME`, or `NotImplementedException` marker was found during this continuation sweep.

Accordingly, this continuation implements the roadmap's explicit optional post-v1 property/fuzz/state-machine hardening item. It does not manufacture source changes merely to increase commit count and does not claim external release validation that cannot be performed in hosted repository tooling.

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

## Portable test-count effect

The pre-continuation portable xUnit baseline recorded by the repository was 572 tests.

This tranche adds eight xUnit facts, so the expected suite size is 580 tests once the exact branch head completes CI. The number must not be recorded as passing evidence until the exact-head CI run reports success.

The existing Python helper-suite count is unchanged by these commits.

## Runtime behavior

These August 19 commits do not change production application runtime source. They strengthen deterministic regression coverage around existing security, transfer-staging, destination-reservation, concurrency, and networking lifecycle contracts.

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

Do not convert any of those external gates into a source-complete claim.

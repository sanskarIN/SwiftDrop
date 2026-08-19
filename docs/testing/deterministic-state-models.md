# Deterministic State-Model Testing

SwiftDrop uses deterministic seeded state-model and stress regressions to exercise long operation sequences without turning the portable test suite into nondeterministic fuzzing.

## Goals

The deterministic suites are intended to:

- exercise many state transitions in one reproducible test;
- compare stateful components with a small reference model where practical;
- cover exact boundary transitions that are easy to miss with isolated examples;
- preserve a stable seed so every CI failure can be reproduced locally;
- avoid wall-clock sleeps and uncontrolled randomness;
- keep failures attributable to a specific invariant rather than a probabilistic fuzz campaign.

They supplement focused unit, integration, concurrency, protocol, filesystem, and platform tests. They do not replace physical-device or signed-package release validation.

## Current deterministic suites

### Attempt rate limiter

`AttemptRateLimiterStateMachineTests` executes a seeded reference-model sequence covering expiry, admission, bounded state, reset, and pruning behavior.

### One-time authorization store

`OneTimeAuthorizationStoreStateMachineTests` executes a seeded reference-model sequence covering registration, duplicate rejection, one-time consumption, expiry, and bounded capacity behavior.

### Discovery registry

`DiscoveryRegistryStateMachineTests` executes a seeded model covering valid/invalid peer observations, self-exclusion, expiry, pruning, trusted-first ordering, and bounded registry behavior.

### Transfer staging budget

`TransferStagingBudgetStateMachineTests` executes **5,000 seeded operations** against an independent counter/limit model. It covers valid and invalid lengths, per-file limits, aggregate exhaustion, file-count exhaustion, non-consuming preflight, commit accounting, and next-file capacity.

`TransferStagingBudgetZeroAggregateTests` separately protects the intentional zero-byte-only aggregate configuration.

### Destination reservation set

`DestinationReservationSetStateMachineTests` executes **3,000 seeded filesystem/reservation operations**. It combines repeated reservations, releases, external completed-file creation/deletion, path-comparison semantics, and collision pressure while asserting reservation uniqueness and visibility after every step.

`DestinationReservationSetDisposalTests` separately exercises concurrent idempotent disposal.

### One-time pairing-code manager

`OneTimePairingCodeManagerStateMachineTests` executes **4,000 seeded lifecycle operations** covering create/replace, valid consumption, replay rejection, invalidation, malformed candidates, wrong candidates, time advancement, and exact expiry.

`OneTimePairingCodeManagerConcurrencyTests` separately verifies that 64 simultaneous consumers produce exactly one successful one-time-code use.

### Async concurrency and session tracking

`AsyncConcurrencyGateQueueTests` protects queue progress when a restrictive head waiter is canceled and a later waiter is already eligible under its own limit.

`AsyncSessionTrackerStressTests` tracks 128 sessions, completes them in a deterministic shuffled order with mixed successful/faulted outcomes, and verifies complete drain and terminal cleanup.

## Reproducibility rules

- Seeds are constants committed with each test.
- A regression should keep the seed that exposed the defect unless the test is deliberately redesigned.
- Do not use `DateTimeOffset.UtcNow` inside a state model when an explicit synthetic timestamp can be supplied.
- Do not add arbitrary `Task.Delay` calls to make concurrency tests pass.
- Prefer completion sources, cancellation tokens, and bounded `WaitAsync` guards for deterministic synchronization.
- Randomized values must remain within bounded ranges and must not allocate unbounded files, buffers, tasks, or protocol frames.
- Filesystem state models must use isolated temporary directories and best-effort cleanup.

## Failure handling

When a deterministic model fails:

1. reproduce with the committed seed;
2. identify the first invariant mismatch rather than only the final assertion;
3. determine whether the implementation or reference model is wrong;
4. fix the production contract if a real defect exists;
5. keep a focused regression for the minimal boundary in addition to the longer state model when practical;
6. update protocol/security/release documentation if the repaired behavior changes an owned contract.

Do not weaken or delete a model merely because it exposes a legitimate defect.

## CI and release evidence

A source file existing in the repository is not passing evidence. State-model coverage becomes portable-tested evidence only when the exact commit containing the test completes the maintained CI gate successfully.

Likewise, portable deterministic coverage does not prove:

- signed Android provider/lifecycle behavior;
- signed Windows package/capability behavior;
- Apple App Group/provisioning/Share Extension runtime behavior;
- representative physical LAN behavior;
- target filesystem semantics on every supported device;
- accessibility/localization behavior in real platform UI stacks;
- store acceptance.

Those remain separate release gates.

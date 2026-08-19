# SwiftDrop — Continuation Ledger (2026-08-19)

Repository: https://github.com/sanskarIN/SwiftDrop  
Branch: `continuation/post-v1-hardening-2026-08-19`  
Pull request: #32

This addendum records the post-v1 continuation that follows the existing August 19 completion and hardening ledgers. The maintained v1 transfer/runtime scope remains source-complete; this tranche concentrates on regression coverage, release-evidence usability, and defects found while reviewing those supporting surfaces.

## Core regression coverage

Added focused xUnit coverage for:

- receive-root key canonicalization, relative/absolute normalization, trailing separators, and platform case policy;
- path comparer/StringComparison alignment and ordinal Unicode semantics;
- storage-capacity input boundaries and checked arithmetic overflow;
- known SHA-256 file vectors, empty files, missing files, and cancellation;
- text-snippet exact-expiry, maximum clock-skew, and UTF-8 byte boundaries;
- local address selection safety and network diagnostic invariants;
- asynchronous concurrency-gate invalid limits, pre-cancellation, lease idempotency, mixed-limit FIFO fairness, and cancelled-head progress;
- asynchronous session tracking for null registration, successful/faulted/cancelled tasks, cancellation, and sessions added while draining.

## Canonical text-snippet validation

Removed the unused duplicate `SwiftDrop.Core.Security.TextSnippetValidator` implementation.

The repository now has one canonical text-snippet policy under `SwiftDrop.Core.Protocol`. This prevents two implementations with different exact-expiry and clock-skew semantics from silently diverging.

## Diagnostic privacy redaction fix

A continuation audit found that diagnostic redaction split input only on the literal space character. Identifiers separated by tabs, LF, or CRLF could therefore remain embedded in a larger token and avoid IP/email recognition.

Regression tests were added first, followed by a production fix that tokenizes on all whitespace. Existing output normalization remains space-separated, while IP addresses, endpoints, email-like identifiers, paths, pairing capabilities, GUIDs, and SHA-256 fingerprints continue to be replaced with `[redacted]`.

## Manual release-evidence status tooling

Added `scripts/summarize_manual_release_evidence.py`.

The helper validates the evidence document before reporting progress and supports:

- human-readable aggregate output;
- `--json` machine-readable output;
- exact case/group status counts;
- a `remaining` JSON collection containing every non-passed required case;
- `--remaining-only` output for an actionable `group/case: status` checklist;
- an explicit all-passed message when no required case remains.

The helper never changes evidence and never infers a pass from hosted CI or source compilation.

## Release workflow integration

Added and updated release documentation so the status helper is discoverable from:

- the manual release-evidence generator workflow;
- the dedicated manual release-evidence status guide;
- the canonical documentation index.

Repository-completion validation now requires the helper and its documentation, requires the docs-index link, and requires release-readiness workflow triggers for changes to the helper. The release-readiness workflow was updated for both push and pull-request path filters, with regression coverage protecting that integration.

## Commit strategy

The continuation intentionally uses small Conventional Commits so independent tests, fixes, release tooling, CI wiring, and documentation remain reviewable in history. Commits are signed off with:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

## Validation state

GitHub Actions for the latest pull-request head are the authoritative hosted validation. This ledger does not claim queued or unexecuted workflows as passed. Signed-package, physical-device, representative network/filesystem, accessibility/localization, Apple provisioning/notarization, and store checks remain external release evidence and must continue to be recorded honestly in the manual release-evidence manifest.

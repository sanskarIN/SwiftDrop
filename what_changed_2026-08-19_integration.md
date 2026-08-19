# What changed — final integration

Date: 2026-08-19  
Repository: `https://github.com/sanskarIN/SwiftDrop`  
Branch: `final/integrated-completion-20260819`

This ledger records the final repository-side integration of the two remaining independent completion branches. It does not replace or shorten their detailed ledgers.

## Integrated parents

The integration commit `747b054e1be362425f4eb1d505c2ffcdade955dd` has two parents:

- `eca8571c9703f0d04be55fa2862118c7c95e91f7` — PR #32 post-v1 Core/security/release-evidence hardening, **37 granular commits**;
- `cc84f819a2dd4f0ad02cee480c69de9a3ecd21b2` — PR #33 final UI/navigation/localization polish, **24 granular commits**.

The resulting integration branch is **62 commits ahead of `main`**: both parent histories plus the explicit two-parent integration commit. No squashing or history rewriting is used.

## Hardening/release-evidence work preserved

The integrated tree retains PR #32's unique work, including:

- receive-root/path/storage/hash/text/network/concurrency/session/settings regression coverage;
- removal of the duplicate security-namespace text-snippet validator so protocol validation has one canonical implementation;
- diagnostic privacy redaction across all whitespace token boundaries;
- explicit malformed null receive-folder rejection;
- manual release-evidence progress/status tooling;
- fail-closed handling for the all-zero template candidate commit;
- deriving release-evidence `complete` from the authoritative strict complete-mode validator;
- release-readiness path wiring, completion invariants, tests, and documentation.

## UI/navigation/localization work preserved

The integrated tree retains PR #33's unique work, including:

- direct home navigation to Queue, History, Settings, and About;
- English/Hindi localization of remaining primary runtime/accessibility/support surfaces;
- localized Settings display lists separated from canonical persisted values;
- localized Diagnostics status and stable-code self-test outcome summaries while preserving technical Core messages in safe logs;
- localized Queue state/operation presentation with typed-state counting;
- the `UiPolishStrings` English/Hindi resource pair and localization parity validation;
- the portable UI localization/navigation regression contract;
- the final UI audit and dedicated UI change ledger.

## Conflict resolution

The only intentional content-level overlap requiring manual reconciliation was `docs/README.md`.

The integrated documentation index keeps all unique links from both parents, including:

- final UI completion audit;
- final UI what-changed appendix;
- continuation hardening ledger;
- manual release-evidence status guide.

All other PR #33 changed files were overlaid on the PR #32 tree without dropping PR #32's independent Core/release files or its deletion of the duplicate security validator.

## Validation boundary

This integration ledger does not claim queued or unexecuted GitHub Actions as successful. The integration branch must be evaluated by the maintained exact-head CI, CodeQL, security-hygiene, release-readiness, and platform-build workflows.

Signed Android/Windows/Apple packages, real devices/providers/filesystems/networks, accessibility/Hindi behavior, Apple provisioning/App Group/notarization, exact signed-artifact dependency/license/provenance reconciliation, and store/privacy submission remain external release evidence rather than repository-source work.

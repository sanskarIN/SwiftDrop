# Manual Release Evidence

SwiftDrop keeps source/build automation separate from signed-device and store validation. This document defines the machine-readable companion to the manual release checklist so an unexecuted check cannot be represented as a pass merely because a source build succeeded.

The canonical template is:

- `docs/release/manual-release-evidence.template.json`

The validator is:

- `scripts/validate_manual_release_evidence.py`

The validator uses only the Python standard library.

## What this record is for

Create one evidence manifest for the **exact release-candidate commit** that is being signed and tested. The manifest records the current state of the major external release gates that hosted repository CI cannot prove by itself.

The required groups are:

1. `android`;
2. `windows`;
3. `ios`;
4. `maccatalyst`;
5. `cross-device`;
6. `filesystem`;
7. `accessibility-localization`;
8. `dependency-license`;
9. `store`.

Each group contains a fixed set of required case identifiers. Unknown groups, unknown cases, duplicate groups/cases, missing fields, and extra fields are rejected.

## Start a candidate record

Copy the template to a candidate-specific evidence location outside the source tree or into a controlled release-evidence location appropriate for the release process.

Replace:

- `candidate.commit` with the exact lowercase 40-hex Git commit SHA;
- `candidate.version` with the release-candidate version;
- `candidate.created_utc` with the UTC creation timestamp.

Do not reuse one evidence manifest across different candidate commits.

## Status values

Every case uses exactly one of:

- `not-run` — the case has not been executed;
- `in-progress` — execution has started but does not yet have a terminal result;
- `blocked` — execution cannot proceed and the reason is recorded;
- `passed` — the exact candidate passed the case in the recorded environment;
- `failed` — the exact candidate failed the case in the recorded environment.

Group status is derived from its case states and must match the validator's aggregate rule:

- any failed case → `failed`;
- all cases passed → `passed`;
- all cases not run → `not-run`;
- blocked cases with only passed/blocked/not-run neighbors → `blocked`;
- other mixed states → `in-progress`.

A group cannot be manually labeled `passed` while one of its required cases is still unexecuted.

## Terminal-case evidence requirements

A `passed` or `failed` case must include:

- `executed_utc` — canonical UTC timestamp ending in `Z`;
- `environment` — a bounded description of the actual device/package/environment used;
- at least one `evidence` reference.

A `blocked` case must include a nonempty `notes` explanation.

A `not-run` case must keep:

- `executed_utc` as `null`;
- `evidence` as an empty array.

Evidence references should identify retained screenshots, logs, videos, signed-package records, store reports, or other controlled release artifacts. Keep references short and stable enough for a reviewer to locate the evidence.

## Structural validation

During test execution and while a candidate is still incomplete, run:

```bash
python3 scripts/validate_manual_release_evidence.py path/to/manual-release-evidence.json
```

Structural mode verifies:

- schema version;
- exact closed field sets;
- canonical candidate commit syntax;
- candidate version/timestamp syntax;
- exact required groups;
- exact required cases;
- unique groups/cases/evidence references;
- valid statuses;
- case evidence requirements;
- group/case aggregate consistency;
- bounded text/reference sizes;
- common private-key/pairing-capability leakage markers.

A structurally valid record may still contain `not-run`, `blocked`, `in-progress`, or `failed` cases. Structural validity is **not release readiness**.

## Complete-candidate validation

Immediately before using the manifest as release evidence, run:

```bash
python3 scripts/validate_manual_release_evidence.py --require-complete path/to/manual-release-evidence.json
```

Complete mode additionally requires:

- a non-placeholder candidate commit;
- every required group to be `passed`;
- therefore every required case to be `passed` with timestamp, environment, and evidence.

The checked-in template intentionally fails complete mode because it contains the all-zero placeholder commit and `not-run` cases.

## Privacy and secret handling

The evidence manifest is metadata, not a place to copy sensitive transfer/session material.

Do not place in the manifest:

- private keys;
- pairing capability URLs;
- reusable authorization material;
- raw transferred file/text contents merely for proof;
- passwords, API tokens, signing secrets, provisioning secrets, or store credentials.

The validator rejects common private-key markers and `swiftdrop://pair` capability text in environment, evidence references, and notes. That check is deliberately narrow and is not a general secret scanner. Review evidence manually before sharing or committing it.

## Relationship to CI

Normal CI, security hygiene, CodeQL, hosted platform builds, and release-readiness workflows remain source/build evidence. They do not turn a manual evidence case into `passed` unless the case itself was actually executed in the required signed/device/store environment.

Likewise, a complete manual evidence manifest does not replace automated checks. Production release requires both the applicable automated gates and the real target-environment validation defined by the release checklist.

## Required group intent

### Android

Covers signed install/upgrade, real share-provider metadata behavior, foreground/background restrictions, and multicast discovery on physical Wi-Fi.

### Windows

Covers signed MSIX install/upgrade, packaged protocol activation, firewall/network behavior, and packaged picker/drop behavior.

### iOS

Covers signed-device build, Share Extension activation, App Group handoff, and real `NSItemProvider` behavior.

### Mac Catalyst

Covers signed/notarized containing-app validation, sandbox/network behavior, and native drop.

### Cross-device

Covers all pairing methods, file/folder/text transfers, pause/cancel/resume, network switching, and low-storage behavior across representative target pairs.

### Filesystem

Covers target filesystem symlink/reparse handling, destination collision pressure, and completed-destination mutation/race behavior.

### Accessibility and localization

Covers platform screen readers, large text/high contrast, and Hindi layout/runtime-message checks.

### Dependency and license

Covers exact candidate dependency graph audit, license/notice reconciliation, and package provenance.

### Store

Covers privacy declarations, metadata/screenshots, and final submission/signing requirements.

## Review rule

If a required case was not executed, keep it `not-run` or use another accurate non-pass state. Never convert missing evidence into a pass to make a candidate appear complete.

# Manual Release Evidence Status

SwiftDrop's manual release-evidence manifest is intentionally strict: hosted CI cannot prove signed-device, store, accessibility, filesystem-provider, or representative cross-network behavior. The status helper makes that external work easier to review without converting missing evidence into a pass.

## Command

Run the human-readable summary against a structurally valid candidate manifest:

```bash
python3 scripts/summarize_manual_release_evidence.py path/to/manual-release-evidence.json
```

For machine-readable output:

```bash
python3 scripts/summarize_manual_release_evidence.py path/to/manual-release-evidence.json --json
```

To print only checks or candidate conditions that still prevent completion:

```bash
python3 scripts/summarize_manual_release_evidence.py path/to/manual-release-evidence.json --remaining-only
```

Each unpassed case line uses `group/case: status`, so a release operator can work through the exact outstanding matrix without reading or rewriting the manifest. Candidate-level completion blockers are printed as explicit field messages after the case list. A fully passed manifest with an exact non-placeholder candidate commit prints a single all-passed message.

The helper validates the complete document structure before reporting anything. Invalid or inconsistent evidence fails instead of producing a potentially misleading progress report.

## Reported fields

The JSON form reports:

- exact candidate version, commit, and creation timestamp;
- total required groups and cases;
- passed and remaining case counts;
- counts for every valid case status;
- counts for every aggregate group status;
- each required group's aggregate status and local case-status counts;
- `remaining`, containing the group, case ID, and recorded status for every case that is not passed;
- `completion_blockers`, containing candidate-level conditions that still prevent completion;
- `complete`, derived from the same `validate_document(..., require_complete=True)` contract used by the strict validator.

The status helper deliberately does **not** maintain an independent definition of a complete release record. A structurally valid document is summarized first, then the authoritative complete-mode validator determines the `complete` flag. This means future complete-mode requirements fail closed automatically instead of silently drifting from the summary tool. If strict completion fails after every case is passed and no recognized candidate-specific blocker explains it, the helper emits a generic complete-validation blocker rather than an all-passed claim.

The all-zero template commit is structurally valid so a fresh evidence template can be inspected, but it is never a complete release candidate. If every manual case is marked passed while `candidate.commit` is still forty zeroes, the helper reports `complete: false`, includes an explicit `candidate.commit` blocker, and `--remaining-only` prints that blocker instead of claiming that all required evidence is complete.

The remaining case list deliberately omits evidence contents, environment text, and notes. It is a planning view, not a substitute for the underlying evidence record.

`complete: true` is still a progress summary, not independent release approval. Before release, run the strict complete validator directly against the exact candidate evidence:

```bash
python3 scripts/validate_manual_release_evidence.py --require-complete path/to/manual-release-evidence.json
```

## Recommended review loop

1. Create a fresh candidate evidence document for the exact candidate commit.
2. Execute only the physical/signed/store checks that were actually performed.
3. Record accurate case states, environments, timestamps, evidence references, and blocking notes.
4. Run structural validation.
5. Run the status summary to see aggregate progress and any candidate-level blocker.
6. Run `--remaining-only` to identify the exact next external checks or candidate correction.
7. Repeat until every required case is genuinely passed and no completion blocker remains.
8. Run complete validation immediately before release approval.

Do not use the summary helper to infer a pass from source builds, hosted workflows, missing devices, absent store access, unexecuted manual checks, or a template placeholder candidate commit.

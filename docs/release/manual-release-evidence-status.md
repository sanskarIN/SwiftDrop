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

The helper validates the complete document structure before reporting anything. Invalid or inconsistent evidence fails instead of producing a potentially misleading progress report.

## Reported fields

The JSON form reports:

- exact candidate version, commit, and creation timestamp;
- total required groups and cases;
- passed and remaining case counts;
- counts for every valid case status;
- counts for every aggregate group status;
- each required group's aggregate status and local case-status counts;
- `complete`, which is true only when every required case is recorded as passed.

`complete: true` is a progress summary, not an independent release-readiness decision. Before release, still run the strict complete validator:

```bash
python3 scripts/validate_manual_release_evidence.py --require-complete path/to/manual-release-evidence.json
```

## Recommended review loop

1. Create a fresh candidate evidence document for the exact candidate commit.
2. Execute only the physical/signed/store checks that were actually performed.
3. Record accurate case states, environments, timestamps, evidence references, and blocking notes.
4. Run structural validation.
5. Run the status summary to see remaining work.
6. Repeat until every required case is genuinely passed.
7. Run complete validation immediately before release approval.

Do not use the summary helper to infer a pass from source builds, hosted workflows, missing devices, absent store access, or unexecuted manual checks.

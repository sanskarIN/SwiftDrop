# Manual Release Evidence Generator

Use `scripts/create_manual_release_evidence.py` to create a fresh SwiftDrop manual release-evidence record for an exact candidate commit without hand-editing the canonical JSON template.

This generator complements [Manual Release Evidence](manual-release-evidence.md). It does **not** execute device/store tests and deliberately starts every required external case as `not-run`.

## Basic usage

```bash
python3 scripts/create_manual_release_evidence.py \
  --commit <40-hex-candidate-commit> \
  --version <release-candidate-version> \
  --output release-evidence/<candidate>.json
```

The generated record contains:

- schema version 1;
- the exact candidate commit;
- the supplied candidate version;
- a canonical UTC creation timestamp;
- all nine required release-validation groups;
- every required case in `not-run` state;
- no pre-filled device environment, evidence, notes, or pass claims.

## Deterministic timestamp override

For reproducible tooling tests or a controlled release process, provide an explicit canonical timestamp:

```bash
python3 scripts/create_manual_release_evidence.py \
  --commit <40-hex-candidate-commit> \
  --version <release-candidate-version> \
  --created-utc 2026-08-19T04:00:00Z \
  --output release-evidence/<candidate>.json
```

The timestamp must satisfy the same canonical UTC rules as the validator.

## Output safety

The generator is deliberately conservative:

- the all-zero template commit is rejected;
- malformed/noncanonical commit IDs are rejected by the validator contract;
- malformed/noncanonical timestamps are rejected;
- parent directories are created as needed;
- an existing output is not overwritten by default;
- non-force creation uses exclusive file creation to close the ordinary check/write race;
- symbolic-link outputs are rejected;
- directories and other non-regular output targets are rejected;
- `--force` may replace only an existing regular file.

To intentionally replace an existing regular manifest:

```bash
python3 scripts/create_manual_release_evidence.py \
  --commit <40-hex-candidate-commit> \
  --version <release-candidate-version> \
  --output release-evidence/<candidate>.json \
  --force
```

Review the destination before using `--force`.

## Validate immediately after creation

A newly generated manifest should pass structural mode:

```bash
python3 scripts/validate_manual_release_evidence.py release-evidence/<candidate>.json
```

It should **fail** complete mode because every case is intentionally still `not-run`:

```bash
python3 scripts/validate_manual_release_evidence.py \
  --require-complete \
  release-evidence/<candidate>.json
```

That failure is expected until real signed/device/store evidence has been recorded for every required case.

## Review progress without changing evidence

Use the status helper to validate the manifest and report how many external checks still remain:

```bash
python3 scripts/summarize_manual_release_evidence.py release-evidence/<candidate>.json
```

Use `--json` when another local release tool needs machine-readable totals. See [Manual Release Evidence Status](manual-release-evidence-status.md) for the output contract and review loop.

The helper is read-only and does not infer passes from CI, source builds, or missing evidence.

## Candidate workflow

1. Choose the exact source commit to sign.
2. Generate a fresh evidence manifest using that exact 40-hex commit.
3. Build/sign packages from the same commit.
4. Execute the applicable manual/device/store cases.
5. Record each case honestly as `passed`, `failed`, `blocked`, `in-progress`, or `not-run`.
6. Attach stable evidence references for terminal pass/fail cases.
7. Run structural validation during the process.
8. Run the status helper to review exact remaining case counts.
9. Run `--require-complete` only when every required external case has actually passed.
10. Keep automated CI/security/platform-build evidence separate from manual evidence; both are required where applicable.

## Privacy rule

The generated file contains no secrets. Keep it that way when editing it. Do not insert private keys, pairing capability URLs, reusable authorization material, signing credentials, store credentials, passwords, API tokens, or raw transfer content into the manifest.

The evidence validator contains narrow guards for common private-key and pairing-capability markers, but those guards are not a substitute for human review or repository secret scanning.

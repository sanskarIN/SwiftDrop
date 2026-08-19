# Repository Completion Validation

SwiftDrop treats **repository-side completion** as an enforceable quality contract rather than a one-time statement in a status document.

The canonical validator is:

```bash
python3 scripts/validate_repository_completion.py
```

It uses only the Python standard library and runs in normal CI plus both portable verification entry points.

## What the validator enforces

### Required open-source and community surface

The validator requires the maintained repository-level files needed for a healthy open-source project, including:

- README;
- license and notice files;
- security and support policies;
- contributing and Code of Conduct documents;
- privacy and terms documents;
- changelog, build guide, project status, next-steps record, and final continuation ledger;
- canonical solution/build configuration;
- Dependabot configuration;
- funding metadata;
- bug-report and feature-request forms;
- issue-template routing configuration;
- pull-request template.

Missing or empty required files fail validation.

### Required application/test/benchmark projects

The validator requires the maintained project set:

- `src/SwiftDrop.Core/SwiftDrop.Core.csproj`;
- `src/SwiftDrop.App/SwiftDrop.App.csproj`;
- `src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`;
- `tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj`;
- `benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj`.

This prevents accidental removal of a supported project from looking like a clean source tree.

### Required CI/release tooling

The validator requires the maintained CI/security/platform/release workflows and the canonical verification/release-evidence tools, including:

- CI;
- CodeQL;
- security hygiene;
- hosted platform builds;
- release readiness;
- Bash and PowerShell portable verification;
- repository-completion validation itself;
- documentation/localization/platform metadata validators;
- NuGet vulnerability-report validation;
- manual release-evidence validation;
- manual release-evidence generation;
- the associated release-process/checklist/signing/privacy/evidence documentation.

### No unfinished production implementation markers

Production source under `src/` must not contain the following unfinished implementation markers:

- `TODO`;
- `FIXME`;
- `NotImplementedException`;
- `#warning`.

The check is intentionally scoped to production source rather than historical documentation or tests so a document can discuss those words without breaking CI.

If future work is intentionally deferred, track it in a roadmap/issue or an explicit post-v1 design document rather than leaving an unfinished marker in production code.

### Production source must remain readable UTF-8 text

Maintained production source files covered by the completion scan must decode as UTF-8. This prevents malformed or accidentally binary source/configuration files from bypassing marker validation or creating platform-dependent repository behavior.

### No leaked placeholder release candidate

The all-zero 40-hex candidate commit is allowed only in:

- `docs/release/manual-release-evidence.template.json`.

A JSON evidence record elsewhere in the repository containing that placeholder fails validation. This prevents a copied but unstamped release-evidence template from being mistaken for a real candidate record.

## Where the validator runs

The contract is enforced by:

- `.github/workflows/ci.yml` on Ubuntu;
- `scripts/verify-core.sh`;
- `scripts/verify-core.ps1`, including the Windows portable CI job.

Release-readiness uses the maintained portable verification path, so the completion contract is part of the broader release evidence surface as well.

## What this validator does not prove

Repository completion validation is **not** a substitute for:

- compiling/running the test suite;
- CodeQL or secret/security hygiene checks;
- dependency vulnerability auditing;
- hosted Android/Windows/Apple target compilation;
- signed-package installation and upgrade;
- real device/provider/network/filesystem behavior;
- Apple Developer provisioning/App Group/notarization;
- accessibility/localization behavior on physical platform UI stacks;
- store privacy declarations, screenshots, signing, submission, or review.

Those remain separate automated or external evidence gates.

## Failure handling

If this validator fails:

1. do not remove the check simply to make CI green;
2. restore the missing required artifact or project when it is still maintained;
3. replace unfinished production markers with completed behavior or an explicit tracked post-v1 item;
4. repair malformed production text instead of excluding it from validation;
5. stamp real release-evidence records with the exact candidate commit rather than the placeholder;
6. if the project contract intentionally changes, update the validator, its tests, the documentation index, and the owning release/architecture documentation in the same change.

## Completion definition

For SwiftDrop, **repository-side complete** means that the maintained source scope, project structure, open-source/community surface, automated validation surface, release evidence tooling, and canonical documentation are present and free of known unfinished production markers.

It does **not** mean that external signed-device/store validation has magically occurred. External work must still be executed and recorded as evidence for the exact release candidate.

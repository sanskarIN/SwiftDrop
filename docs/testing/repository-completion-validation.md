# Repository Completion Validation

SwiftDrop treats **repository-side completion** as a continuously enforced quality contract rather than a one-time statement.

Run the canonical validator with:

```bash
python3 scripts/validate_repository_completion.py
```

It uses only the Python standard library plus the repository's own manual-release-evidence validator module.

## Required repository surface

The completion contract requires the maintained application/test/benchmark projects, core open-source/community/legal files, canonical final status/ledgers, CODEOWNERS/governance policy, Dependabot/funding/community templates, and the maintained CI/security/platform/release workflows and release-evidence tools.

A required file that is missing, unreadable, or empty fails validation.

## Maintained projects

The validator requires:

- `src/SwiftDrop.Core/SwiftDrop.Core.csproj`;
- `src/SwiftDrop.App/SwiftDrop.App.csproj`;
- `src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`;
- `tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj`;
- `benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj`.

This prevents accidental target/project removal from appearing as a clean repository.

## No unfinished production markers

Maintained production source/configuration under `src/` must not contain:

- `TODO`;
- `FIXME`;
- `TBD`;
- `NotImplementedException`;
- `#warning`.

Covered source/configuration files must also decode as UTF-8. Deferred work belongs in an issue, roadmap, or explicit post-v1 design record rather than as an unfinished production marker.

## Repository ownership integrity

The validator requires `.github/CODEOWNERS` and checks the source-side ownership contract rather than treating the file as an unvalidated decoration.

It requires:

- a repository-wide `*` fallback assigned to `@sanskarIN`;
- explicit maintainer ownership for GitHub automation, shared build properties, SDK selection, and verification scripts;
- explicit ownership for Core security, protocol, networking, transfer, and storage boundaries;
- explicit ownership for native platform integration and the iOS Share Extension;
- explicit ownership for security/privacy/third-party notice and security/protocol/release documentation.

This makes accidental removal or reassignment of a sensitive ownership boundary fail the portable quality contract.

CODEOWNERS is not, by itself, proof that GitHub requires Code Owner approval. The remote branch-protection/ruleset requirement is an external repository setting documented in `docs/repository-governance.md`.

## Release-readiness trigger integrity

The validator requires the release-readiness workflow to watch the release-critical helper surface for both `push` and `pull_request`, including:

- Bash/PowerShell portable verification;
- documentation/localization/Apple/Windows validators;
- NuGet vulnerability validation;
- dependency evidence manifest generation;
- manual release-evidence validation/generation;
- repository completion validation itself;
- helper tests.

This prevents release-tooling changes from bypassing the aggregate Android/Windows/Apple release gate.

## Portable verifier integration

The completion validator itself must be executed by:

- normal Ubuntu CI;
- `scripts/verify-core.sh`;
- `scripts/verify-core.ps1` and therefore the Windows portable verifier.

Release readiness calls the canonical portable verification path, so completion validation participates in that release gate too.

## Documentation index integrity

The canonical docs index must link the current final repository status, governance policy, completion record, closure/final continuation ledgers, manual release-evidence docs, and evidence generator guide. This prevents older historical status files from silently becoming the apparent current state or repository governance from becoming undiscoverable.

## Release template validity and placeholder safety

The checked-in `docs/release/manual-release-evidence.template.json` must continue to pass structural validation.

Its all-zero candidate commit is intentionally a template placeholder. That all-zero commit must not appear in another JSON evidence record in the repository; a copied-but-unstamped candidate record fails the completion contract.

## What this does not prove

Repository completion validation does not replace:

- compilation/tests;
- CodeQL/security hygiene;
- dependency vulnerability audits;
- hosted platform compilation;
- remote GitHub branch-protection/ruleset enforcement;
- signed package installation/upgrade;
- real devices/providers/networks/filesystems;
- Apple provisioning/App Group/notarization;
- accessibility/localization execution on real platform UI stacks;
- exact signed-candidate dependency/license/provenance review;
- store signing/submission/review.

Those are separate automated or external release evidence gates.

## Failure handling

If completion validation fails, repair the contract rather than deleting or weakening the check merely to make CI green. If the maintained project scope intentionally changes, update the validator, its regression tests, final status, docs index, and owning architecture/release documentation together.

If ownership boundaries intentionally change, update CODEOWNERS, the governance guide, this validation reference, and the validator/test expectations in the same reviewed change.

## Completion definition

For SwiftDrop, **repository-side complete** means the maintained implementation/project structure, community/open-source/governance surface, validation/release tooling, and canonical documentation are present and free of known unfinished production markers, while release-critical trigger and source-side ownership coverage remain enforced by automated validation.

It does not mean remote branch protection or unexecuted signed-device/store checks have passed. External evidence must still be recorded honestly for the exact repository/release candidate state.

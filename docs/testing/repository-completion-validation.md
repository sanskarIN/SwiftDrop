# Repository Completion Validation

SwiftDrop treats **repository-side completion** as a continuously enforced quality contract rather than a one-time statement.

Run the canonical validator with:

```bash
python3 scripts/validate_repository_completion.py
```

It uses only the Python standard library plus the repository's own manual-release-evidence validator module.

## Required repository surface

The completion contract requires the maintained application/test/benchmark projects, Linux packaging, core open-source/community/legal files, canonical current status/ledgers, CODEOWNERS/governance policy, Dependabot/funding/community templates, maintained CI/security/platform/release workflows, and release-evidence/version/platform validators.

A required file that is missing, unreadable, or empty fails validation.

## Maintained projects

The validator requires:

- `src/SwiftDrop.Core/SwiftDrop.Core.csproj`;
- `src/SwiftDrop.App/SwiftDrop.App.csproj`;
- `src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj`;
- `src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`;
- `tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj`;
- `benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj`.

This prevents accidental removal of the shared Core, MAUI host, maintained Avalonia Linux host, Share Extension, tests, or benchmarks from appearing as a clean repository.

## No unfinished production markers

Maintained production source/configuration under `src/` must not contain:

- `TODO`;
- `FIXME`;
- `TBD`;
- `NotImplementedException`;
- `#warning`.

Covered source/configuration files must also decode as UTF-8. Deferred work belongs in an issue, roadmap, or explicit later design record rather than as an unfinished production marker.

## Repository ownership integrity

The validator requires `.github/CODEOWNERS` and checks the source-side ownership contract rather than treating the file as unvalidated decoration.

It requires:

- a repository-wide `*` fallback assigned to `@sanskarIN`;
- explicit maintainer ownership for GitHub automation, shared build properties, SDK selection, and verification scripts;
- explicit ownership for Core Security, Discovery, Protocol, Networking, Transfer, and Storage boundaries;
- explicit ownership for native MAUI platform integration, the Avalonia desktop host, the iOS Share Extension, and Linux packaging;
- explicit ownership for security/privacy/third-party notice and security/protocol/release/platform documentation.

This makes accidental removal or reassignment of a sensitive ownership boundary—including Linux desktop/package paths—fail the portable quality contract.

CODEOWNERS is not, by itself, proof that GitHub requires Code Owner approval. The remote branch-protection/ruleset requirement is an external repository setting documented in `docs/repository-governance.md`.

## Release-readiness trigger integrity

The validator requires the release-readiness workflow to watch the release-critical helper surface for both `push` and `pull_request`, including:

- Linux packaging metadata;
- Bash/PowerShell portable verification;
- documentation/localization/Apple/Windows/Linux validators;
- cross-platform version alignment;
- Linux publishing/package assembly;
- NuGet vulnerability validation;
- dependency evidence manifest generation;
- manual release-evidence validation, generation, and status summarization;
- repository completion validation itself;
- helper tests.

This prevents release-tooling or maintained Linux changes from bypassing the aggregate Android/Windows/Apple/Linux release gate.

## Portable verifier integration

The completion contract checks that common validation executes the required portable validators from:

- normal Ubuntu CI;
- `scripts/verify-core.sh`;
- `scripts/verify-core.ps1` and therefore the Windows portable verifier.

Those paths must execute repository completion, Linux integration, and 2.5.18 version-alignment validation. Release readiness calls the maintained portable verification path and separately builds/audits the maintained platform targets.

## Cross-platform version integrity

`scripts/validate_version_alignment.py` is part of the required repository surface and common verification path.

For the prepared 2.5.18 candidate, it protects alignment between:

- MAUI display version `2.5.18`;
- MAUI build/version code `20518`;
- iOS Share Extension display/build versions;
- Windows four-part package identity `2.5.18.0`;
- Avalonia desktop version `2.5.18`.

The version validator has its own regression tests. Repository completion protects the validator's presence and integration so version alignment cannot be silently removed from the release path.

## Linux integration integrity

The repository-completion contract requires the maintained Linux project, platform documentation, desktop-entry metadata, publish helper, Linux validator, and release-trigger coverage.

`scripts/validate_linux_integration.py` is executed by common CI and both portable verification entry points. Aggregate release readiness additionally builds the desktop host and creates/audits `linux-x64` and `linux-arm64` packages.

These source/build checks do not replace representative physical Linux-distribution execution for a signed/final candidate.

## Documentation index integrity

The canonical docs index must link the current final repository status, 2.5.18 preparation and draft release notes, August 24 continuation ledger, governance policy/August 20 historical ledger, repository completion records, final continuation records, and manual release-evidence documentation/status/generator guidance.

This prevents an older historical status from silently becoming the apparent current state and prevents governance, release-evidence status, or the prepared release contract from becoming undiscoverable.

## Manual release-evidence integrity

The completion contract protects the manual release-evidence validator, generator, status summarizer, documentation, and template.

The checked-in `docs/release/manual-release-evidence.template.json` must continue to pass structural validation. Its all-zero candidate commit is intentionally a template placeholder. That all-zero commit must not appear in another JSON evidence record in the repository; a copied-but-unstamped candidate record fails the completion contract.

The status summarizer must remain part of the release-critical workflow trigger surface so changes to release-evidence interpretation cannot bypass aggregate validation.

## What this does not prove

Repository completion validation does not replace:

- compilation/tests;
- CodeQL/security hygiene;
- dependency vulnerability audits;
- hosted platform compilation/package creation;
- remote GitHub branch-protection/ruleset enforcement;
- signed package installation/upgrade;
- real devices/providers/networks/filesystems;
- representative Linux distribution execution;
- Apple provisioning/App Group/notarization;
- accessibility/localization execution on real platform UI stacks;
- exact signed-candidate dependency/license/provenance review;
- store/distribution signing, submission, and review.

Those are separate automated or external release-evidence gates.

## Failure handling

If completion validation fails, repair the contract rather than deleting or weakening the check merely to make CI green. If the maintained project scope intentionally changes, update the validator, its regression tests, final status, docs index, and owning architecture/platform/release documentation together.

If ownership boundaries intentionally change, update CODEOWNERS, the governance guide, this validation reference, and the validator/test expectations in the same reviewed change.

If release identifiers intentionally change, update every maintained package surface together and keep `scripts/validate_version_alignment.py` green rather than disabling the alignment check.

## Completion definition

For SwiftDrop, **repository-side complete** means the maintained implementation/project structure, community/open-source/governance surface, validation/release tooling, Linux/platform support contracts, and canonical documentation are present and free of known unfinished production markers, while release-critical triggers, source-side ownership, and coordinated versioning remain enforced by automated validation.

It does not mean remote branch protection or unexecuted signed-device/store/distribution checks have passed. External evidence must still be recorded honestly for the exact repository/release candidate state.

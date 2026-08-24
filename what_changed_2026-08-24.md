# SwiftDrop continuation ledger — 2026-08-24

Repository: https://github.com/sanskarIN/SwiftDrop
Release preparation target: **2.5.18**
Status: integrated source-candidate preparation and exact-head validation in progress; signed/device/distribution/store evidence not yet complete.

This ledger records the August 24 continuation work without representing queued workflow jobs or external validation as successful evidence.

## Work completed in this continuation

### PR #34 CI blocker correction and merge

The final hardening/UI integration branch had one failing xUnit analyzer rule in `NetworkDiagnosticsServiceTests.cs`: `xUnit2031` rejected filtering with LINQ `Where(...)` before `Assert.Single(...)`.

The assertion was changed to the predicate overload of `Assert.Single`, retaining the same behavioral requirement while satisfying the analyzer. CI, CodeQL, security hygiene, and maintained platform build jobs subsequently reported success for that head. PR #34 was merged into `main` with a normal merge so its granular history was preserved.

Main hardening merge: `c74eead16691ebd133d78c5fa8f279ba4c11acae`.

The aggregate Release Readiness wrapper was still runner-queued at the moment of that merge, so it is not retroactively represented as completed evidence.

### Coordinated 2.5.18 package versions

The maintained package surfaces were advanced from the former 1.0.0 baseline to the requested 2.5.18 preparation values:

- `src/SwiftDrop.App/SwiftDrop.App.csproj`
  - `ApplicationDisplayVersion`: `2.5.18`
  - `ApplicationVersion`: `20518`
- `src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`
  - `ApplicationDisplayVersion`: `2.5.18`
  - `ApplicationVersion`: `20518`
- `src/SwiftDrop.App/Platforms/Windows/Package.appxmanifest`
  - package identity: `2.5.18.0`
- `src/SwiftDrop.Desktop/SwiftDrop.Desktop.csproj`
  - `Version`: `2.5.18`

The build-code convention is `major * 10000 + minor * 100 + patch`, producing `20518` for 2.5.18.

### Version alignment automation

Added `scripts/validate_version_alignment.py` and regression coverage.

The validator protects:

- canonical `major.minor.patch` display version syntax;
- MAUI and iOS Share Extension display-version equality;
- MAUI and iOS Share Extension build-version equality;
- deterministic build-code calculation;
- Avalonia desktop version equality;
- Windows four-part package version equality;
- Android-compatible positive integer build-code bounds;
- optional explicit expected release/build values.

Version alignment now runs from common CI, Bash portable verification, and PowerShell portable verification.

### Maintained Linux desktop support and common-gate promotion

PR #35 contains the maintained Avalonia desktop host and its Linux packaging/integration surface while retaining shared `SwiftDrop.Core` security/protocol/transfer behavior.

The candidate includes:

- Avalonia `src/SwiftDrop.Desktop` host;
- desktop discovery, identity, pairing, receive-server, transfer-client, and batch-resume services;
- XDG-aware local data/identity handling;
- Linux desktop entry and `swiftdrop://` protocol handler;
- self-contained packaging helper `scripts/publish-linux.sh`;
- `linux-x64` and `linux-arm64` package paths;
- `scripts/validate_linux_integration.py` in common CI, Bash verification, and PowerShell verification;
- dedicated Desktop Linux workflow coverage;
- Linux package/dependency/vulnerability evidence in aggregate Release Readiness.

Aggregate Release Readiness now requires the Linux matrix in addition to Core/tests, Android, Windows, and Apple source/platform gates.

### Manual release-evidence protections retained

When Linux/2.5.18 work was reconciled against hardened `main`, the hardening-side release-evidence controls were deliberately preserved.

The combined candidate retains:

- manual release-evidence generator;
- strict validator;
- status summarizer;
- all-zero template candidate fail-closed behavior;
- completion state derived from strict complete-mode validation;
- release-readiness triggers for manual evidence validation/generation/status helpers;
- completion-validator and documentation protections for the evidence surface.

### PR #35 conflict resolution against hardened main

After PR #34 entered `main`, PR #35 had real conflicts in four shared quality surfaces:

1. `.github/workflows/release-readiness.yml`;
2. `docs/README.md`;
3. `scripts/validate_repository_completion.py`;
4. `scripts/tests/test_validate_repository_completion.py`.

The histories were joined without rebasing or squashing through true two-parent merge commit:

`de79916b52818a141d24429a5d0b51b354c69026`

Parents:

- hardened `main`: `c74eead16691ebd133d78c5fa8f279ba4c11acae`;
- previous PR #35 head: `fbc0f66682459aedf03dbd29cad1e88666f5ee55`.

The four overlapping files were then reconciled in explicit follow-up commits so Linux/version requirements and hardening/manual-evidence requirements coexist visibly and audibly.

### PR #36 governance history integrated without restoring stale versions

PR #36 contained useful repository-governance hardening but was authored while application metadata still reflected `1.0.0` / build `1`. Applying its overlapping status files directly would therefore have regressed the prepared 2.5.18 candidate.

The governance history was instead preserved through a second true two-parent integration merge:

`cfeb728682c11c0f20eaaf9c63de775ae3d26716`

Parents:

- reconciled 2.5.18/Linux candidate;
- PR #36 head `c42dcec09f68496229ca2afd74687dd807c37a81`.

Non-conflicting governance assets were brought in unchanged:

- `.github/CODEOWNERS`;
- `docs/repository-governance.md`;
- `docs/testing/repository-completion-validation.md`;
- `what_changed_2026-08-20.md`.

The overlapping current-status/index/completion files retained their newer 2.5.18 versions and received governance additions explicitly afterward.

### CODEOWNERS extended for the maintained Linux architecture

Because PR #36 predated the maintained Avalonia Linux host, its ownership map was expanded before enforcement.

The current CODEOWNERS contract explicitly protects:

- repository fallback `*`;
- GitHub automation/toolchain/scripts;
- Core Security, Discovery, Protocol, Networking, Transfer, and Storage;
- MAUI native platform integration;
- Avalonia `SwiftDrop.Desktop`;
- iOS Share Extension;
- `packaging/linux/`;
- security/privacy/third-party/release/protocol/platform documentation.

All protected entries currently retain `@sanskarIN` as owner.

### Governance made machine-enforced

`scripts/validate_repository_completion.py` now parses CODEOWNERS and fails if required sensitive paths are absent or reassigned.

The combined completion contract also continues to protect:

- Linux project/package/docs/validator surfaces;
- cross-platform version alignment;
- release-readiness triggers;
- portable validator integration;
- manual release-evidence validator/generator/status tooling;
- canonical 2.5.18/governance/current-status documentation.

`scripts/tests/test_validate_repository_completion.py` now includes regression cases for fallback ownership, Core sensitive paths, Discovery, Avalonia desktop, Linux packaging, platform documentation, governance index links, and the pre-existing Linux/version/manual-evidence invariants.

### Governance documentation aligned with 2.5.18

`docs/repository-governance.md` now documents the current maintained platform/security surfaces rather than the pre-Linux subset.

It covers:

- Discovery/Avalonia/Linux package ownership;
- single-maintainer review constraints;
- remote branch-protection evidence boundary;
- version-alignment review expectations;
- Linux package/release-gate review expectations;
- manual evidence and completion checks;
- the governance two-parent integration history.

The most recently inspected GitHub state reported `main` as not protected. Remote branch/ruleset configuration therefore remains an external repository-administration action and is not claimed as enabled.

### Canonical final status corrected

`FINAL_REPOSITORY_STATUS.md` was rewritten from its stale PR #34-pending state to the actual August 24 state.

It now records:

- PR #34 already merged into `main`;
- prepared identifiers `2.5.18`, `20518`, `2.5.18.0`;
- Android/iOS/macOS/Windows/Linux maintained source scope;
- Linux package/release gating;
- preserved hardening/UI work;
- governance/CODEOWNERS enforcement;
- both history-preserving integration merges;
- exact-head hosted validation requirements;
- external signed/device/distribution/store evidence still required;
- browser extension intentionally deferred beyond 2.5.18.

### Repository-completion documentation updated

`docs/testing/repository-completion-validation.md` now matches the actual validator contract, including:

- `SwiftDrop.Desktop` as a required maintained project;
- Linux packaging and integration validation;
- 2.5.18 version-alignment protection;
- CODEOWNERS governance integrity;
- manual release-evidence status tooling;
- Android/Windows/Apple/Linux aggregate release-readiness scope;
- explicit limits of source validation versus signed/device/distribution evidence.

### 2.5.18 preparation and draft release notes finalized for integrated source scope

`docs/release/2.5.18-preparation.md` now records hardening/Linux/governance as integrated candidate history rather than future intent. It also records the remote-governance boundary and both two-parent history-preservation merges.

`docs/release/2.5.18-release-notes.md` now describes the integrated candidate scope, including CODEOWNERS enforcement, Linux ownership, hardening provenance, governance provenance, and the remaining exact-head/external release evidence.

The notes remain explicitly **draft** and cannot be treated as published production release notes until the exact approved candidate is selected and validated.

### Canonical changelog synchronized

`CHANGELOG.md` now begins with a `2.5.18 candidate preparation - 2026-08-24` section that records coordinated versions, maintained Linux support, hardened integration, governance integration, and the release-evidence boundary.

The entry explicitly states that 2.5.18 is not yet a published production release. All pre-existing changelog history remains below the new candidate-preparation entry.

### PR #35 review surface updated

PR #35 is now titled:

`feat: finalize Linux, governance, and SwiftDrop 2.5.18 preparation`

Its body documents the integrated source scope, both preservation merges, version identifiers, Linux release gates, governance enforcement, external validation boundary, and browser-extension deferral.

At the metadata update it was mergeable and contained 157 commits across 83 changed files. Additional ledger/changelog commits increase the exact head afterward, so final counts must be read from GitHub for the frozen head rather than copied from this intermediate observation.

## Granular continuation commit sequence

The August 24 continuation intentionally used focused commits rather than collapsing unrelated changes. The sequence includes:

1. `test(core): satisfy xUnit single assertion analyzer`
2. `chore(release): set app version to 2.5.18`
3. `chore(release): align share extension version 2.5.18`
4. `chore(release): align Windows package version 2.5.18`
5. `chore(release): align desktop host version 2.5.18`
6. `feat(release): add cross-platform version alignment validator`
7. `test(release): cover version alignment contract`
8. `ci(release): validate versions in portable Bash gate`
9. `ci(release): validate versions in PowerShell gate`
10. `ci(release): enforce cross-platform version alignment`
11. `ci(linux): include Linux contract in portable verification`
12. `ci(linux): include Linux contract in PowerShell verification`
13. `ci(linux): enforce Linux metadata in common CI`
14. `ci(release): add Linux to aggregate release gate`
15. `feat(quality): protect Linux and version release contracts`
16. `test(quality): cover Linux and version completion invariants`
17. `docs(release): define 2.5.18 preparation contract`
18. initial August 24 continuation-ledger commit
19. `docs(index): expose 2.5.18 preparation records`
20. `docs(quality): protect 2.5.18 preparation records`
21. `docs(release): draft SwiftDrop 2.5.18 release notes`
22. `docs(index): link 2.5.18 draft release notes`
23. `docs(quality): protect 2.5.18 draft release notes`
24. first ledger refresh
25. `merge: integrate Linux and 2.5.18 preparation onto hardened main`
26. `docs(integration): preserve hardening records in 2.5.18 index`
27. `fix(integration): preserve manual evidence completion requirements`
28. `test(integration): cover combined completion invariants`
29. `ci(integration): preserve manual evidence trigger with Linux gate`
30. hardened-Linux integration ledger refresh
31. `merge: integrate repository governance into 2.5.18 candidate`
32. `docs(governance): integrate protected change policy into 2.5.18 index`
33. `chore(governance): extend ownership to Linux and discovery surfaces`
34. `feat(governance): enforce ownership on maintained 2.5.18 surfaces`
35. `test(governance): cover Linux ownership completion contract`
36. `docs(status): align canonical status with integrated 2.5.18 candidate`
37. `docs(governance): align policy with Linux and 2.5.18`
38. `docs(testing): document combined completion contract`
39. `docs(release): mark governance integrated in 2.5.18 preparation`
40. `docs(release): finalize integrated 2.5.18 draft scope`
41. first governance-complete August 24 ledger synchronization
42. `docs(changelog): add 2.5.18 candidate preparation entry`
43. this final August 24 ledger correction.

Every newly authored repository commit in this continuation uses:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

## Current integration queue

The source integration queue is now consolidated:

- PR #34 — **merged into `main`** as `c74eead16691ebd133d78c5fa8f279ba4c11acae`;
- PR #35 — **active integrated 2.5.18 candidate**, containing Linux, hardened-main reconciliation, and preserved PR #36 governance history; requires fresh exact-head validation before merge;
- PR #36 — its 13-commit governance history is already contained in the PR #35 candidate through `cfeb728682c11c0f20eaaf9c63de775ae3d26716`; the standalone PR should be closed as integrated/superseded after the combined candidate reaches `main`.

No earlier successful run is final evidence for a newer head. The head created by this ledger correction is the exact candidate to evaluate unless a real validator/build defect requires another source change.

## Evidence still external

The following remain external release work rather than repository-edit tasks:

- production signing keys/certificates/provisioning;
- signed Android/iOS/Mac Catalyst/Windows artifacts;
- representative Linux distribution/package execution;
- physical-device cross-platform transfer testing;
- real provider/share-extension behavior;
- real restricted-network/firewall/lifecycle/low-storage behavior;
- accessibility and Hindi runtime validation on target devices;
- exact signed-artifact dependency/license/provenance reconciliation;
- store metadata/screenshots/privacy declarations;
- notarization where applicable;
- store/distribution submission and review;
- remote GitHub branch/ruleset protection for `main`.

No queued or unexecuted item above is recorded as passed.

## Browser extension boundary

Browser-extension implementation remains intentionally outside the 2.5.18 stabilization scope. It should begin as a subsequent deliberately approved feature milestone after 2.5.18 has an exact candidate and the release path is stable.

**Made by the Sanskar**

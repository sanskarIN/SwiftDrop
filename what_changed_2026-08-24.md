# SwiftDrop continuation ledger — 2026-08-24

Repository: https://github.com/sanskarIN/SwiftDrop
Release preparation target: **2.5.18**
Status: repository integration and validation in progress; signed/device/store release evidence not yet complete.

This ledger records the August 24 continuation work without representing queued workflow jobs or external validation as successful evidence.

## Work completed in this continuation

### PR #34 CI blocker correction and integration

The final hardening/UI integration branch had one failing xUnit analyzer rule in `NetworkDiagnosticsServiceTests.cs`: `xUnit2031` rejected filtering with LINQ `Where(...)` before `Assert.Single(...)`.

The assertion was changed to the predicate overload of `Assert.Single`, retaining the same behavioral requirement while satisfying the analyzer. CI, CodeQL, security hygiene, and the maintained platform-build matrix subsequently reported success for that head. PR #34 was then merged into `main` with a normal merge so its granular history was preserved. The resulting main merge commit is `c74eead16691ebd133d78c5fa8f279ba4c11acae`.

The aggregate Release Readiness wrapper was still runner-queued at the moment of that merge; it is not represented here as completed evidence.

### Linux PR readiness

PR #35 was moved from draft to ready-for-review after an earlier exact head had successful CI, CodeQL, security hygiene, release readiness, and dedicated Desktop Linux workflow results. Subsequent August 24 commits invalidate that older head as final evidence, so the integrated head must pass again.

The PR title was updated to `feat: add maintained Linux support and prepare SwiftDrop 2.5.18` so the review surface reflects its expanded release-preparation scope.

### 2.5.18 coordinated package versions

The maintained package surfaces were changed from the former 1.0.0 baseline to the requested 2.5.18 preparation values:

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

Added `scripts/validate_version_alignment.py`.

It validates:

- canonical `major.minor.patch` display version syntax;
- MAUI and iOS Share Extension display-version equality;
- MAUI and iOS Share Extension build-version equality;
- deterministic build-code calculation;
- Avalonia desktop version equality;
- Windows four-part package version equality;
- Android-compatible positive integer build-code bounds;
- optional explicit expected release/build values.

Added `scripts/tests/test_validate_version_alignment.py` covering the valid 2.5.18 state and deliberate mismatch cases for the Share Extension, desktop host, build code, Windows package version, and explicit requested release version.

### Portable validation integration

Both portable verification entry points now execute the version alignment validator:

- `scripts/verify-core.sh`;
- `scripts/verify-core.ps1`.

Common `.github/workflows/ci.yml` also executes it explicitly.

### Linux common-gate promotion

The maintained Linux integration validator is no longer confined to the dedicated Linux workflow. It is now executed by:

- common Ubuntu CI;
- Bash portable verification;
- PowerShell portable verification.

This protects Linux project/solution/desktop-entry/launcher/packaging/workflow contracts across the common validation surface.

### Aggregate release-readiness Linux gate

`.github/workflows/release-readiness.yml` now treats Linux as a maintained release-gate platform.

Release readiness now:

- watches Linux packaging and validator changes;
- watches the new version-alignment validator;
- builds the Avalonia desktop host;
- creates self-contained `linux-x64` and `linux-arm64` packages;
- verifies expected package output;
- captures direct/transitive dependency reports;
- captures vulnerable-package reports;
- validates vulnerability reports;
- creates dependency-evidence manifests;
- uploads per-RID Linux dependency-audit artifacts;
- requires the Linux matrix to succeed before the aggregate release gate can pass.

The hardening-side manual release evidence summarizer trigger is also retained for both push and pull-request paths, so the integrated workflow does not weaken the evidence tooling already present on `main`.

### Repository completion protection

`scripts/validate_repository_completion.py` was expanded so future repository edits cannot silently remove the new release protections.

It now requires:

- `scripts/validate_version_alignment.py` as a repository-completion artifact;
- Linux packaging paths in release-readiness triggers;
- Linux integration, Linux publish, and version-alignment helper trigger coverage;
- Linux and version validator execution in CI, Bash verification, and PowerShell verification;
- the 2.5.18 preparation document, draft release notes, and dated continuation ledger;
- canonical documentation-index links to the 2.5.18 preparation and release-note records;
- the manual release evidence status document and summarizer inherited from the hardened `main` branch.

The corresponding completion-validator regression tests cover both the Linux/version invariants and the retained manual-evidence status/summarizer requirements.

### 2.5.18 release documentation

Added `docs/release/2.5.18-preparation.md` as the canonical release-preparation record for this version.

It defines:

- coordinated version identifiers;
- intended PR/source integration scope;
- exact-candidate rules;
- automated gate requirements;
- Android/iOS/Mac Catalyst/Windows/Linux signed and physical validation requirements;
- cross-device testing expectations;
- accessibility/localization/privacy/store evidence;
- the rule that `v2.5.18` must not be presented as a production release before complete exact-candidate evidence exists.

Added `docs/release/2.5.18-release-notes.md` as draft candidate release notes. The notes cover Linux support, coordinated versioning, stronger release gates, intended hardening/governance integration, platform scope, retained local-first security/privacy principles, the browser-extension deferral, and the signed/manual validation still required before publication.

`docs/README.md` links both 2.5.18 records and identifies `2.5.18` / `20518` as the prepared source identifiers without claiming a production release. The integrated index also retains the final hardening/UI ledgers and manual release evidence status document from `main`.

### PR #35 conflict resolution against hardened main

After PR #34 entered `main`, PR #35 had real conflicts in four shared quality surfaces:

1. `.github/workflows/release-readiness.yml`;
2. `docs/README.md`;
3. `scripts/validate_repository_completion.py`;
4. `scripts/tests/test_validate_repository_completion.py`.

The integration was resolved without rebasing or squashing away either history. A true two-parent merge commit, `de79916b52818a141d24429a5d0b51b354c69026`, joins hardened `main` (`c74eead16691ebd133d78c5fa8f279ba4c11acae`) with the previous PR #35 head (`fbc0f66682459aedf03dbd29cad1e88666f5ee55`).

The four overlapping files were then explicitly reconciled in granular follow-up commits so the stronger requirements from both sides remain visible and auditable. PR #35 is mergeable after this reconciliation.

## Commit sequence created during this continuation

The continuation intentionally used granular commits rather than collapsing unrelated changes:

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
18. initial continuation-ledger commit
19. `docs(index): expose 2.5.18 preparation records`
20. `docs(quality): protect 2.5.18 preparation records`
21. `docs(release): draft SwiftDrop 2.5.18 release notes`
22. `docs(index): link 2.5.18 draft release notes`
23. `docs(quality): protect 2.5.18 draft release notes`
24. first ledger-refresh commit
25. `merge: integrate Linux and 2.5.18 preparation onto hardened main`
26. `docs(integration): preserve hardening records in 2.5.18 index`
27. `fix(integration): preserve manual evidence completion requirements`
28. `test(integration): cover combined completion invariants`
29. `ci(integration): preserve manual evidence trigger with Linux gate`
30. this ledger-refresh commit.

Every newly authored repository commit in this continuation uses:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

## Current integration queue

The source integration queue has materially narrowed:

- PR #34 — **merged into `main`** as `c74eead16691ebd133d78c5fa8f279ba4c11acae`;
- PR #35 — **reconciled with hardened `main`, mergeable, and awaiting fresh exact-head validation**;
- PR #36 — CODEOWNERS/repository governance hardening still needs to be reconciled against the new 2.5.18/Linux/hardening source state.

The exact PR #35 head before this ledger refresh was `e2efe229165b9072d080d28d7c6d4627948e2878`. CI, Security hygiene, Platform builds, Desktop Linux, CodeQL, and Release readiness were all queued for that head. Because this ledger refresh creates another commit, those runs are historical rather than final evidence; the new head must be evaluated instead.

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
- store/distribution submission and review.

No queued or unexecuted item above is recorded as passed.

## Browser extension boundary

Browser-extension implementation remains intentionally outside the 2.5.18 stabilization scope. It should begin as the next deliberately approved feature milestone after 2.5.18 has an exact candidate and the release path is stable.

**Made by the Sanskar**

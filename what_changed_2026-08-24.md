# SwiftDrop continuation ledger — 2026-08-24

Repository: https://github.com/sanskarIN/SwiftDrop
Release preparation target: **2.5.18**
Status: repository work in progress; signed/device/store release evidence not yet complete.

This ledger records the August 24 continuation work without representing queued workflow jobs or external validation as successful evidence.

## Work completed in this continuation

### PR #34 CI blocker correction

The final hardening/UI integration branch had one failing xUnit analyzer rule in `NetworkDiagnosticsServiceTests.cs`: `xUnit2031` rejected filtering with LINQ `Where(...)` before `Assert.Single(...)`.

The assertion now uses the predicate overload of `Assert.Single`, retaining the same behavioral requirement while satisfying the analyzer. A new exact-head workflow matrix was triggered. CI, CodeQL, security hygiene, and the maintained platform-build matrix subsequently reported success; the aggregate Release Readiness final gate had not yet completed when this ledger was frozen.

### Linux PR readiness

PR #35 was moved from draft to ready-for-review after its earlier exact head had successful CI, CodeQL, security hygiene, release readiness, and dedicated Desktop Linux workflow results. New August 24 commits invalidate that older head as final evidence, so the updated head must pass again.

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

### Repository completion protection

`scripts/validate_repository_completion.py` was expanded so future repository edits cannot silently remove the new release protections.

It now requires:

- `scripts/validate_version_alignment.py` as a repository-completion artifact;
- Linux packaging paths in release-readiness triggers;
- Linux integration, Linux publish, and version-alignment helper trigger coverage;
- Linux and version validator execution in CI, Bash verification, and PowerShell verification;
- the 2.5.18 preparation document, draft release notes, and dated continuation ledger;
- canonical documentation-index links to the 2.5.18 preparation and release-note records.

The corresponding completion-validator regression test was expanded for those invariants.

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

`docs/README.md` links both 2.5.18 records and identifies `2.5.18` / `20518` as the prepared source identifiers without claiming a production release.

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
24. this ledger-refresh commit.

Every newly authored repository commit in this continuation uses:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

## Integration queue still to resolve

The repository currently has three post-baseline workstreams that must become one consistent `main` history before an exact 2.5.18 candidate can be frozen:

- PR #34 — final hardening/UI integration;
- PR #35 — Linux + 2.5.18 preparation and release-gate hardening;
- PR #36 — CODEOWNERS/repository governance hardening.

Their overlapping documentation/completion-validator changes must be reconciled without discarding the stronger checks from any branch.

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

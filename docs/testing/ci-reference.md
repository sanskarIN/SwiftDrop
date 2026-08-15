# SwiftDrop CI and Verification Reference

Updated: 2026-08-15

This document explains the maintained GitHub Actions gates and how their evidence should be interpreted.

## Maintained workflow set

SwiftDrop keeps five maintained workflows under `.github/workflows/`:

1. `ci.yml`
2. `platform-builds.yml`
3. `codeql.yml`
4. `security-hygiene.yml`
5. `release-readiness.yml`

Temporary one-shot repair/migration workflows are not part of the maintained set after they complete.

## Python validation toolchain

Maintained GitHub-hosted jobs that execute repository Python validators explicitly install Python 3.13 with `actions/setup-python@v7` instead of relying on an incidental runner PATH version.

Normal CI also executes the repository's Python helper regression tests:

```bash
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
```

The current helper suite contains **21 tests**. It covers:

- NuGet vulnerability-report interpretation;
- deterministic dependency-evidence manifest generation;
- Windows packaged app-notification integration validation, including matching toast/COM CLSIDs, activation arguments, local-only capability posture, placeholder-free generic notification text, startup registration, and handler-before-register ordering;
- the schema-v6 performance-history cross-layer contract, including separate duration/measured-byte persistence, weighted analyzer semantics, resume-safe sender/receiver byte attribution, and History UI/localization wiring.

A helper-script regression therefore fails the same normal CI gate that uses those helpers.

## `ci.yml` — two-OS portable core, documentation, and audit gate

Primary portable regression gate on pushes to `main` and pull requests.

The workflow has two jobs:

- `core` on Ubuntu, which runs the canonical portable build/test/audit sequence;
- `windows-portable-verifier` on Windows, which executes `scripts/verify-core.ps1` so Windows-native parsing, filesystem, SQLite, process-exit, and package-metadata validation behavior are exercised instead of being inferred from Linux success.

Current Ubuntu responsibilities:

- checkout source;
- install .NET 10 SDK;
- install pinned Python 3.13;
- run Python validation-helper unit tests;
- validate canonical documentation files and local Markdown links with `scripts/validate_documentation.py`;
- validate localization catalogs;
- validate Apple integration metadata;
- validate Windows protocol/private-network/app-notification package/source metadata with `scripts/validate_windows_integration.py`;
- restore `SwiftDrop.Core`;
- build `SwiftDrop.Core` Release;
- run `SwiftDrop.Core.Tests` Release;
- compile the benchmark project Release;
- generate a machine-readable direct/transitive Core vulnerable-package report using JSON output schema version 1;
- fail if `scripts/validate_nuget_vulnerability_report.py` finds any reported vulnerability entry or malformed report structure.

The Windows verifier performs the same helper/documentation/localization/Apple/Windows-integration/Core/test/benchmark/vulnerability checks through the PowerShell entry point and explicitly treats nonzero native command exit codes as failures.

The Windows integration validator checks source/package consistency that the intentionally unpackaged hosted Windows compile cannot prove by itself. It requires:

- the `swiftdrop` protocol registration;
- `privateNetworkClientServer` capability;
- absence of `internetClient`;
- exactly one packaged toast activation registration;
- exactly one COM server notification registration;
- matching valid toast/COM CLSIDs;
- exact Windows App SDK notification activation arguments;
- startup registration when the persisted notification preference is enabled;
- `NotificationInvoked` handler attachment before `Register()`;
- English/Hindi generic terminal notification resources that are non-empty and free of formatting placeholders.

This still does **not** prove that a signed installed MSIX successfully registers/activates notifications. That remains release evidence.

The Windows job is intentionally not redundant. It has already exposed defects invisible to Ubuntu-only execution, including a PowerShell interpolation parser error and SQLite native-handle/file-lock lifetime issues. Those failures were fixed in source/test resource ownership rather than bypassed or retried away.

The documentation validator requires the maintained user/developer/architecture/protocol/platform/storage/testing/release documents, confirms the canonical docs index links the principal guides, rejects broken local inline Markdown links, and ensures completed one-time documentation helper files are not left in the repository.

This gate is the fastest general proof that portable protocol/security/storage/path/transfer behavior, validation tooling, dependency-audit evidence, documentation integrity, Windows package metadata consistency, and the Windows portable execution path remain internally consistent.

It does **not** compile every MAUI target.

## `platform-builds.yml` — hosted target compile and dependency-audit matrix

Maintained jobs compile shipped target graphs and retain target-specific dependency evidence.

### Android

- install MAUI Android workload;
- restore Android containing-app target and Core;
- build Android containing-app source in Release configuration;
- generate `net10.0-android` direct/transitive package JSON;
- generate and validate its vulnerable-package JSON;
- generate a deterministic SHA-256 evidence manifest;
- upload `android-dependency-audit`.

### Windows

- install MAUI Windows workload;
- restore the focused Windows app runtime and Core runtime;
- build the Windows target in the maintained hosted compile configuration without claiming signed MSIX validation;
- generate focused `net10.0-windows10.0.19041.0` direct/transitive package JSON;
- generate and validate its vulnerable-package JSON;
- generate a deterministic SHA-256 evidence manifest;
- upload `windows-dependency-audit`.

The hosted Windows compile validates the Windows App SDK notification API source path but deliberately uses `WindowsPackageType=None`; packaged notification registration/activation must therefore be validated again from the signed installable artifact.

### Apple

- install MAUI iOS and Mac Catalyst workloads;
- select the hosted simulator/runtime identifiers appropriate for runner architecture;
- restore/build the Mac Catalyst containing app;
- generate and validate Mac Catalyst dependency/vulnerability evidence;
- restore iOS containing app + Share Extension graph;
- build the iOS Simulator Share Extension;
- build the iOS Simulator containing app;
- generate and validate separate iOS containing-app and Share Extension dependency/vulnerability reports;
- generate one deterministic SHA-256 manifest covering the Apple evidence JSON files;
- upload `apple-dependency-audit`.

The Apple job disables real code signing only for simulator compile scope. Source entitlements remain part of the project and must be validated again in signed device/distribution builds. The containing-app compile covers the Apple local-notification API source path, not signed notification authorization/presentation behavior.

`platform-builds.yml` includes the dependency-evidence helper scripts in its path triggers, so changes to audit interpretation or evidence manifest generation re-exercise the shipped target matrix.

## `codeql.yml` — static security analysis

Uses maintained CodeQL v4 actions with current checkout/setup-dotnet majors.

The workflow restores/builds relevant C# source and runs CodeQL analysis.

A green CodeQL workflow is useful static-analysis evidence, but it is not a replacement for protocol/security tests, dependency review, notification runtime policy testing, or device testing.

## `security-hygiene.yml` — repository hygiene

Repository-level hygiene checks include rejection of committed sensitive artifacts/patterns such as:

- private signing material and local databases under prohibited patterns;
- embedded private-key PEM/OpenSSH blocks;
- missing required security documentation.

This is a source-repository protection gate, not a malware scanner or secret-management system for external environments.

## `release-readiness.yml` — candidate aggregate evidence

Release readiness is the candidate-oriented aggregate gate.

It runs on:

- manual `workflow_dispatch`;
- `v*` tags;
- changes to the release workflow or its portable verification/audit/evidence/Windows-integration helper scripts on `main`;
- pull requests to `main` that change those release-gate inputs.

The explicit `scripts/validate_windows_integration.py` path trigger means a future Windows package-notification contract change cannot modify the validator without re-exercising the release-readiness self-test.

The main-branch/path self-test trigger means release-gate engineering changes are exercised before a production tag. Tag-triggered release candidates remain governed by the tag trigger rather than being treated as ordinary documentation-only changes.

The workflow currently requires and retains:

- canonical portable verification, including Apple and Windows integration validators;
- Core/test/benchmark dependency inventories and vulnerable-package reports;
- Android compile plus Android dependency evidence;
- focused Windows compile plus Windows dependency evidence;
- Mac Catalyst compile plus Mac Catalyst dependency evidence;
- iOS Simulator Share Extension compile plus extension dependency evidence;
- iOS Simulator containing-app compile plus iOS app dependency evidence;
- deterministic evidence manifests containing file sizes and SHA-256 digests;
- an aggregate release-gate job that fails unless every required compile/test/audit job succeeded.

The uploaded artifact names and evidence schema are defined in `docs/release/dependency-evidence.md`.

## SQLite resource-lifetime regression protection

SwiftDrop's SQLite stores use Microsoft.Data.Sqlite connection pooling. Windows keeps native database-file handles observable in ways that Linux cleanup often does not.

The portable test infrastructure therefore:

- clears SQLite connection pools before deleting isolated temporary database files;
- removes main, `-wal`, and `-shm` temp files;
- requires SQLite connections/readers/commands used in schema/storage code to be disposed deterministically;
- executes the database-backed test suite on the Windows PowerShell verifier.

Do not replace deterministic disposal with arbitrary sleeps/retries or disable Windows cleanup assertions. A Windows file-lock failure can indicate a real undisposed native SQLite resource.

## Repository-wide NuGet audit policy

`Directory.Build.props` explicitly enables:

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
<NuGetAuditLevel>low</NuGetAuditLevel>
```

Repository warnings are treated as errors. This makes qualifying NuGet audit warnings blocking unless a deliberate, reviewed repository policy change says otherwise.

Machine-readable evidence is an additional gate. It does not merely prove that JSON parsing succeeded: the validator rejects a report containing a non-empty package `vulnerabilities` collection.

## Stable machine-readable report format

Maintained evidence commands use:

```text
--format json --output-version 1
```

Vulnerable-package views use:

```text
--include-transitive --vulnerable
```

Where a target has already been restored for the job, report capture uses `--no-restore` so the evidence corresponds to that restored graph instead of silently initiating another restore at evidence-capture time.

## Local equivalents

### Validation-helper tests

```bash
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
```

### Documentation validation

```bash
python3 scripts/validate_documentation.py
```

Run this whenever documentation files, internal Markdown links, or canonical documentation navigation changes.

### Platform integration validators

```bash
python3 scripts/validate_apple_integration.py
python3 scripts/validate_windows_integration.py
```

The Apple validator checks App Group/Share Extension source metadata. The Windows validator checks protocol/private-network/packaged-notification source metadata. Neither replaces signed package/device validation.

### Portable verification

Linux/macOS shell:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

The local verification scripts include 21 helper tests, documentation/localization/Apple/Windows validators, Core restore/build, portable tests, benchmark compilation, Core vulnerable-package report generation, and explicit vulnerability-report validation.

The PowerShell verifier checks `$LASTEXITCODE` for native commands rather than assuming PowerShell exception behavior will convert every nonzero native exit into a terminating error.

### Core commands

```bash
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release --no-restore
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release
```

### Machine-readable vulnerability review

```bash
dotnet package list \
  --project src/SwiftDrop.Core/SwiftDrop.Core.csproj \
  --include-transitive \
  --vulnerable \
  --format json \
  --output-version 1 > core-vulnerabilities.json

python3 scripts/validate_nuget_vulnerability_report.py core-vulnerabilities.json
```

Use the corresponding restored target/framework command for every shipped runtime graph when preparing a release.

### Evidence manifest

```bash
python3 scripts/create_dependency_evidence_manifest.py artifacts artifacts/manifest.json
```

This writes a path-sorted manifest of evidence JSON file byte lengths and SHA-256 digests. See `docs/release/dependency-evidence.md` for the exact contract and limitations.

## Evidence interpretation

### Green portable CI proves

- all 16 Python validation helpers pass their regression tests;
- the canonical documentation set exists and its checked local Markdown links resolve;
- the current portable source restores under the workflow environment;
- Core compiles under the configured .NET SDK;
- portable tests pass on Ubuntu and through the Windows PowerShell verifier;
- localization/Apple/Windows integration validators pass;
- Windows packaged-notification source/manifest contracts are internally consistent, including startup handler ordering and generic placeholder-free terminal messages;
- benchmark source compiles on both portable verifier paths;
- the generated Core vulnerable-package report is structurally valid and contains no reported vulnerability entries under the configured command/advisory data on both verifier paths.

### Green platform compile/audit proves

- the target source/project graph can compile under the hosted runner/workload configuration used by the workflow;
- Android/iOS/Mac Catalyst/Windows notification API source compiles where it is part of the containing app;
- the configured target package/vulnerability reports were generated successfully;
- the vulnerable-package JSON contained no findings according to the repository validator;
- the uploaded evidence bundle was generated with its deterministic hash manifest when that workflow version includes the manifest step.

### Green CodeQL proves

- CodeQL analysis completed successfully under the configured query/action set.

### Green security hygiene proves

- the configured repository hygiene patterns/doc checks passed for that commit.

### Matching evidence manifest proves

- the retained JSON report bytes match the file lengths and SHA-256 digests recorded by the manifest.

It does **not** authenticate who produced the evidence, sign the bundle, or prove that a separately built signed artifact used the same graph.

### None of the above proves

- valid Android production signing/AAB publication;
- signed Android notification permission/delivery behavior;
- signed Windows MSIX install/update/protocol/app-notification activation behavior;
- real Apple Developer App Group provisioning;
- physical iOS Share Extension provider behavior;
- signed iOS/Mac Catalyst local-notification authorization/foreground/background/system-settings behavior;
- signed Mac Catalyst sandbox/notarization behavior;
- real LAN firewall/client-isolation/multicast behavior;
- device low-storage/lifecycle/background behavior;
- accessibility on actual platform assistive technologies;
- complete third-party license compliance by automation alone;
- final App Store/Play/Microsoft Store declaration acceptance.

## Required candidate discipline

For a release candidate:

1. identify the exact commit SHA/tag;
2. avoid mixing evidence from older commits without explicitly documenting it;
3. require relevant automated gates for that candidate;
4. retain all dependency inventory/vulnerability artifacts and their manifests;
5. verify the retained report bytes against each manifest;
6. manually review package provenance/licenses/notices and compare with final signed artifacts;
7. build/sign packages from the frozen candidate;
8. complete the manual test matrix and release checklist, including native-notification permission/activation/privacy checks;
9. record defects/fixes with a new candidate SHA when source changes;
10. repeat invalidated evidence after a candidate-changing fix.

## Historical versus exact-candidate evidence

`PROJECT_STATUS.md` and `what_changed.md` may record specific successful run IDs from earlier continuation checkpoints. Those are historical evidence for the source/workflow state they tested.

Always prefer the latest exact-candidate workflow results when preparing an actual release. Do not treat an earlier successful report, compile, or vulnerability view as permanent proof for a later candidate.

---

**Made by the Sanskar**

## Aggregate performance trend/export contract

Portable validation now includes **26 Python helper tests** and **559 xUnit tests**. `test_performance_trend_export_contract.py` protects UTC aggregation, aggregate-only invariant CSV schema, the untruncated storage cutoff query, cache/share-sheet export wiring, and English/Hindi UI resource completeness.

The Core suite additionally covers daily bucketing, resume-safe measured-byte math, UTC offset behavior, out-of-window/invalid sample exclusion, saturating aggregates, window bounds, deterministic CSV formatting, duplicate/inconsistent bucket rejection, and History store cutoff-query behavior.

## Aggregate performance trend/export contract

Portable validation now includes **26 Python helper tests** and **559 xUnit tests**. `test_performance_trend_export_contract.py` protects UTC aggregation, aggregate-only invariant CSV schema, the untruncated storage cutoff query, cache/share-sheet export wiring, and English/Hindi UI resource completeness.

The Core suite additionally covers daily bucketing, resume-safe measured-byte math, UTC offset behavior, out-of-window/invalid sample exclusion, saturating aggregates, window bounds, deterministic CSV formatting, duplicate/inconsistent bucket rejection, and History store cutoff-query behavior.

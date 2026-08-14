# SwiftDrop CI and Verification Reference

This document explains the maintained GitHub Actions gates and how their evidence should be interpreted.

## Maintained workflow set

SwiftDrop keeps five maintained workflows under `.github/workflows/`:

1. `ci.yml`
2. `platform-builds.yml`
3. `codeql.yml`
4. `security-hygiene.yml`
5. `release-readiness.yml`

Temporary one-shot repair/migration workflows are not part of the maintained set after they complete.

## `ci.yml` — portable core and documentation gate

Primary portable regression gate on pushes to `main` and pull requests.

Current responsibilities:

- checkout source;
- install .NET 10 SDK;
- validate canonical documentation files and local Markdown links with `scripts/validate_documentation.py`;
- validate localization catalogs;
- validate Apple integration metadata;
- restore `SwiftDrop.Core`;
- build `SwiftDrop.Core` Release;
- run `SwiftDrop.Core.Tests` Release;
- compile the benchmark project Release;
- exercise the machine-readable transitive vulnerable-package JSON command and parse its output as JSON.

The documentation validator requires the maintained user/developer/architecture/protocol/platform/storage/testing/release documents, confirms the canonical docs index links the principal guides, rejects broken local inline Markdown links, and ensures completed one-time documentation helper files are not left in the repository.

This gate is the fastest general proof that portable protocol/security/storage/path/transfer behavior and documentation integrity remain internally consistent.

It does **not** compile every MAUI target.

## `platform-builds.yml` — hosted target compile matrix

Maintained jobs cover:

### Android

- install MAUI Android workload;
- restore Android containing-app target and Core;
- build Android containing-app source in Release configuration.

### Windows

- install MAUI Windows workload;
- restore the focused Windows app runtime and Core runtime;
- build the Windows target in the maintained hosted compile configuration without claiming signed MSIX validation.

### Apple

- install MAUI iOS and Mac Catalyst workloads;
- select the hosted simulator runtime identifiers appropriate for runner architecture;
- restore/build the Mac Catalyst containing app;
- restore iOS containing app + Share Extension graph;
- build the iOS Simulator Share Extension;
- build the iOS Simulator containing app.

The Apple job disables real code signing only for simulator compile scope. Source entitlements remain part of the project and must be validated again in signed device/distribution builds.

## `codeql.yml` — static security analysis

Uses maintained CodeQL v4 actions with current checkout/setup-dotnet majors.

The workflow restores/builds relevant C# source and runs CodeQL analysis.

A green CodeQL workflow is useful static-analysis evidence, but it is not a replacement for protocol/security tests, dependency review, or runtime/device testing.

## `security-hygiene.yml` — repository hygiene

Repository-level hygiene checks include rejection of committed sensitive artifacts/patterns such as:

- private signing material and local databases under prohibited patterns;
- embedded private-key PEM/OpenSSH blocks;
- missing required security documentation.

This is a source-repository protection gate, not a malware scanner or secret-management system for external environments.

## `release-readiness.yml` — candidate aggregate evidence

Release readiness is a broader candidate-oriented gate. It mirrors/aggregates important build/test/platform checks and emits dependency inventory/audit evidence.

Maintained dependency evidence uses .NET 10 noun-first package commands and machine-readable JSON where configured, including complete transitive graph and vulnerable-package views for relevant projects/targets.

The resulting evidence should be retained/reviewed with the candidate rather than relying on memory of an earlier run.

## Repository-wide NuGet audit policy

`Directory.Build.props` explicitly enables:

```xml
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
<NuGetAuditLevel>low</NuGetAuditLevel>
```

Repository warnings are treated as errors. This makes qualifying NuGet audit warnings blocking unless a deliberate, reviewed repository policy change says otherwise.

## Local equivalents

### Documentation validation

```bash
python3 scripts/validate_documentation.py
```

Run this whenever documentation files, internal Markdown links, or canonical documentation navigation changes.

### Portable verification

Linux/macOS shell:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

### Core commands

```bash
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release --no-restore
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release
```

### Machine-readable vulnerability review

```bash
dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --vulnerable --format json
```

Use the equivalent command for every relevant shipped/runtime/test/benchmark project when preparing a release.

## Evidence interpretation

### Green portable CI proves

- the canonical documentation set exists and its checked local Markdown links resolve;
- the current portable source restores under the workflow environment;
- Core compiles under the configured .NET SDK;
- portable tests pass;
- localization/Apple metadata validators pass;
- benchmark source compiles;
- audit command syntax/output remains usable.

### Green platform compile proves

- the target source/project graph can compile under the hosted runner/workload configuration used by the workflow.

### Green CodeQL proves

- CodeQL analysis completed successfully under the configured query/action set.

### Green security hygiene proves

- the configured repository hygiene patterns/doc checks passed for that commit.

### None of the above proves

- valid Android production signing/AAB publication;
- signed Windows MSIX install/update/protocol behavior;
- real Apple Developer App Group provisioning;
- physical iOS Share Extension provider behavior;
- signed Mac Catalyst sandbox/notarization behavior;
- real LAN firewall/client-isolation/multicast behavior;
- device low-storage/lifecycle/background behavior;
- accessibility on actual platform assistive technologies;
- final App Store/Play/Microsoft Store declaration acceptance.

## Required candidate discipline

For a release candidate:

1. identify the exact commit SHA;
2. avoid mixing evidence from older commits without explicitly documenting it;
3. require relevant automated gates for that candidate;
4. retain dependency inventory/audit outputs;
5. build/sign packages from the frozen candidate;
6. complete the manual test matrix and release checklist;
7. record defects/fixes with a new candidate SHA when source changes;
8. repeat invalidated evidence after a candidate-changing fix.

## Latest verified continuation evidence

The August 14 continuation recorded in `what_changed.md` includes successful evidence for:

- 511/511 portable Core tests after test-toolchain modernization;
- Core and benchmark Release builds;
- localization and Apple metadata validators;
- machine-readable vulnerability-audit validation;
- CodeQL v4;
- repository security hygiene;
- Android hosted compile;
- focused Windows hosted compile;
- Mac Catalyst hosted compile;
- iOS Simulator Share Extension compile;
- iOS Simulator containing-app compile.

The documentation-completion continuation additionally added an exact CI-enforced documentation integrity validator. Always prefer the latest exact-candidate workflow results when preparing an actual release rather than treating historical snapshots as permanent proof.

---

**Made by the Sanskar**

# Building SwiftDrop

Updated: 2026-08-18

SwiftDrop targets .NET 10 and .NET MAUI.

## Canonical solution

Use `SwiftDrop.slnx`:

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release
```

The solution contains:

- `SwiftDrop.Core`;
- `SwiftDrop.App`;
- the iOS-only `SwiftDrop.ShareExtension`;
- portable tests;
- synthetic benchmarks.

## Portable verification

Linux/macOS:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

These verify:

- .NET environment;
- Python validation-helper regression tests;
- canonical documentation files and local Markdown link integrity;
- English/Hindi localization catalogs and placeholder parity;
- Apple App Group/iOS Share Extension project/entitlement/version invariants;
- Windows package protocol/private-network/app-notification registration/source invariants;
- Core restore/build;
- portable tests;
- benchmark-project compilation;
- machine-readable Core direct/transitive vulnerable-package JSON using schema version 1;
- explicit rejection of any vulnerability entries or malformed vulnerability-report structure.

The helper suite currently contains **26 Python tests**, covering the machine-readable NuGet vulnerability validator, deterministic dependency-evidence manifests, Windows packaged-notification integration validation, and the cross-layer schema-v6 performance-history contract (storage/analyzer/resume-byte attribution/UI-localization wiring). The Windows validator checks matching notification toast/COM CLSIDs, activation arguments, handler-before-registration ordering, startup registration for an already-enabled preference, placeholder-free English/Hindi terminal notification messages, preservation of `privateNetworkClientServer`, and rejection of `internetClient`.

Normal `ci.yml` executes this portable contract on both Ubuntu and Windows: the Ubuntu job runs the individual canonical gates directly and the Windows job executes `scripts/verify-core.ps1`. Windows execution is required because it exercises PowerShell parsing/native exit handling, Windows filesystem semantics, and Microsoft.Data.Sqlite native-handle behavior that Linux execution alone cannot prove.

The Unix verifier uses a temporary audit file with automatic cleanup. The PowerShell verifier explicitly checks native-command exit codes so a nonzero `dotnet`/Python result cannot be mistaken for success merely because the host shell did not convert it into a terminating exception.

SQLite-backed tests use deterministic resource ownership and a test-only pooled-database cleanup helper. Production SQLite connections, readers, commands, and schema transactions must be disposed deterministically; Windows file-lock failures are treated as resource-lifetime defects, not hidden with sleeps/retries.

Individual validation helpers can also be run directly:

```bash
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
python3 scripts/validate_documentation.py
python3 scripts/validate_localization.py
python3 scripts/validate_apple_integration.py
python3 scripts/validate_windows_integration.py
```

## Stable compiler policy

Repository-wide `Directory.Build.props` uses stable `LangVersion=latest`, nullable reference types, current analyzers, deterministic builds, and warnings-as-errors for portable projects.

MAUI/Apple platform projects keep platform SDK availability/obsolete warnings visible while still failing common nullable-safety warnings.

## Dependency security policy

Repository-wide restore explicitly enables NuGet auditing for direct and transitive dependencies with `NuGetAudit=true`, `NuGetAuditMode=all`, and `NuGetAuditLevel=low`. Because warnings are treated as errors, qualifying NuGet vulnerability audit warnings block normal verification rather than being silently accepted.

Machine-readable evidence uses the .NET 10 package-list command with an explicit JSON schema version:

```bash
dotnet package list \
  --project src/SwiftDrop.Core/SwiftDrop.Core.csproj \
  --include-transitive \
  --format json \
  --output-version 1 > core-packages.json

dotnet package list \
  --project src/SwiftDrop.Core/SwiftDrop.Core.csproj \
  --include-transitive \
  --vulnerable \
  --format json \
  --output-version 1 > core-vulnerabilities.json

python3 scripts/validate_nuget_vulnerability_report.py core-vulnerabilities.json
```

`validate_nuget_vulnerability_report.py` fails when the report contains vulnerability entries and also fails for malformed/unexpected report structure. A successfully generated JSON file is not treated as sufficient evidence by itself.

## Dependency evidence manifests

Audit bundles can be given a deterministic file manifest:

```bash
python3 scripts/create_dependency_evidence_manifest.py artifacts artifacts/manifest.json
```

The generated manifest records the relative path, exact byte count, and SHA-256 digest of each dependency-evidence JSON report under the selected root. It is an integrity aid for retained evidence, not a digital signature or proof that a separately built signed binary used the same graph.

See `docs/release/dependency-evidence.md` for artifact names, target coverage, manifest schema, review steps, and evidence limitations.

## Android

```bash
dotnet workload install maui-android
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Release --no-restore
```

The maintained platform/release workflows additionally capture and validate the `net10.0-android` containing-app direct/transitive dependency graph and upload its package/vulnerability JSON plus hash manifest.

Android production release still requires a private release keystore, signing configuration, AAB/APK generation, install/upgrade testing, Play Console/store checks, and physical notification permission/delivery validation when optional terminal notifications are enabled.

## Windows

Run on Windows with the current .NET MAUI Windows workload and Windows App SDK prerequisites. For a focused Windows-only **source compile** of this multi-target project, use the same target isolation and unpackaged build boundary used by maintained CI:

```powershell
dotnet workload install maui-windows

dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj `
  -p:SwiftDropTargetFrameworksOverride=net10.0-windows10.0.19041.0 `
  -p:TargetFramework=net10.0-windows10.0.19041.0 `
  -p:RuntimeIdentifier=win-x64 `
  -p:RuntimeIdentifierOverride=win-x64 `
  -p:SkipIosShareExtensionProjectReference=true `
  -p:WindowsPackageType=None `
  -p:GenerateAppxPackageOnBuild=false

dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj -p:RuntimeIdentifier=win-x64

dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -c Release `
  -p:SwiftDropTargetFrameworksOverride=net10.0-windows10.0.19041.0 `
  -p:TargetFramework=net10.0-windows10.0.19041.0 `
  -p:RuntimeIdentifierOverride=win-x64 `
  -p:SkipIosShareExtensionProjectReference=true `
  -p:WindowsPackageType=None `
  -p:GenerateAppxPackageOnBuild=false
```

The override properties narrow this validation command to the Windows TFM; normal product builds still retain the full target matrix. `WindowsPackageType=None` and `GenerateAppxPackageOnBuild=false` intentionally validate source/XAML/WinUI compilation without claiming MSIX readiness.

Before a packaged Windows release, also run the portable package/source validator:

```powershell
python scripts/validate_windows_integration.py
```

It proves the repository's `Package.appxmanifest`, local-only capability posture, notification activation CLSIDs/arguments, notification registration source contract, and generic localized message resources are internally consistent. It does **not** exercise actual COM/toast registration from an installed signed package.

Maintained hosted validation also captures/validates the focused Windows target dependency graph and uploads its JSON reports plus SHA-256 evidence manifest. Production packaging still requires the real signing certificate/package identity, signed MSIX generation, install/update validation, `swiftdrop://` protocol activation, app-notification registration/activation, Windows notification settings/deny behavior, capability checks, and final dependency review of the actual signed package.

## Apple prerequisites

Run Apple builds on macOS with current Xcode and .NET MAUI iOS/Mac Catalyst workloads:

```bash
dotnet workload install maui-ios maui-maccatalyst
```

SwiftDrop contains a dedicated **iOS-only** Share Extension:

`src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`

The iOS containing app and extension must share App Group:

`group.in.sanskar.swiftdrop`

Bundle IDs:

- containing app: `in.sanskar.swiftdrop`;
- iOS Share Extension: `in.sanskar.swiftdrop.share`.

The repository validator checks that source metadata stays synchronized:

```bash
python3 scripts/validate_apple_integration.py
```

That source check cannot create Apple Developer App Group capabilities or provisioning profiles. The real iOS signing environment must configure the same App Group for both identifiers.

## Mac Catalyst containing app

Select a Mac Catalyst RID matching the host and build the containing desktop app. There is no Mac Catalyst Share Extension target in the maintained architecture.

```bash
RID=maccatalyst-arm64   # use maccatalyst-x64 on x64 hosts

dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-maccatalyst -p:RuntimeIdentifier=$RID
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj -p:RuntimeIdentifier=$RID
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-maccatalyst -c Release --no-restore -p:RuntimeIdentifier=$RID
```

Maintained hosted validation captures/validates Mac Catalyst containing-app dependency/vulnerability JSON as part of the Apple dependency evidence bundle.

Mac Catalyst external intake uses the containing app's normal document/file flows and native `UIDropInteraction`. Release validation must confirm sandbox/network/App Group entitlements used by the containing app, native drop, local notification authorization/presentation, signing, notarization, and store packaging.

## iOS Simulator app + Share Extension

Select the simulator RID matching the macOS runner. Hosted CI deliberately clears signing/provisioning inputs only for simulator compilation; the project files retain their real entitlements for signed/device builds.

```bash
RID=iossimulator-arm64   # use iossimulator-x64 on x64 hosts

COMMON_SIM_SIGNING="-p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false -p:CodesignEntitlements="

dotnet restore src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj -p:RuntimeIdentifier=$RID $COMMON_SIM_SIGNING
dotnet build src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj -f net10.0-ios -c Release --no-restore -p:RuntimeIdentifier=$RID $COMMON_SIM_SIGNING

dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-ios -p:RuntimeIdentifier=$RID $COMMON_SIM_SIGNING
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj -p:RuntimeIdentifier=$RID
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-ios -c Release --no-restore -p:RuntimeIdentifier=$RID $COMMON_SIM_SIGNING
```

The maintained Apple job generates separate direct/transitive package and vulnerable-package JSON reports for the iOS containing app and iOS Share Extension, validates both vulnerable-package reports, and includes them with Mac Catalyst reports beneath one hashed Apple evidence manifest.

Simulator compilation checks source/API compatibility only. Physical iOS devices, archives, TestFlight and App Store distribution require Apple signing, provisioning, App Group configuration, extension runtime validation, local notification authorization/presentation validation, and final dependency comparison against the actual signed archive/package.

## Apple runtime validation

Before a signed release verify:

1. the iOS Share Extension appears for its declared file/text/image/movie/web-URL activation types;
2. provider representations stage while their access is valid;
3. the extension publishes only bounded validated packages;
4. the iOS containing app imports packages on cold/warm activation;
5. imported content is shown for review and never automatically sent;
6. stale/malformed/symlinked App Group packages are rejected/pruned;
7. the App Group works in the signed iOS app/extension configuration;
8. opt-in local completion/failure notification permission, disabled state, foreground presentation, background/system delivery, and generic content are correct on iOS;
9. Mac Catalyst native drag/drop remains independent of the iOS Share Extension and works under signed sandbox rules;
10. Mac Catalyst local notification authorization/presentation works under the signed sandbox/system notification configuration.

## Synthetic performance harness

Uses generated temporary data only:

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

See `docs/testing/performance-benchmarks.md`.

## CI

GitHub Actions is configured for:

- 26 validation-helper regression tests;
- documentation integrity validation;
- localization validation;
- Apple integration metadata validation;
- Windows packaged notification/protocol/capability metadata validation;
- two-OS portable Core/test/benchmark/audit verification (Ubuntu and Windows PowerShell);
- CodeQL/security hygiene;
- direct/transitive NuGet vulnerability auditing on restore;
- explicit machine-readable vulnerability-report finding validation;
- Android app compile plus Android dependency evidence;
- focused Windows app compile plus Windows dependency evidence;
- Mac Catalyst containing-app compile plus Mac Catalyst dependency evidence;
- certificate-independent iOS Simulator Share Extension + containing-app compile plus separate iOS app/extension dependency evidence;
- deterministic SHA-256 manifests for retained dependency-report bundles;
- concurrency controls that cancel superseded same-ref CI/platform/security analysis runs while preserving the newest branch evidence;
- release-readiness validation for source, tests, project/benchmark inputs, workflow changes, and portable/audit/evidence/Windows-integration helpers;
- aggregate release-readiness gate.

A configured workflow is not proof it passed. Confirm the exact release-candidate run before publishing.

## Release boundary

Successful source compilation and dependency-audit evidence do not replace:

- signed install/upgrade checks;
- Apple App Group provisioning;
- physical-device iOS Share Extension/native-drop behavior;
- signed Android/iOS/Mac Catalyst/Windows optional notification permission, registration, presentation, and system-settings behavior;
- real peer-to-peer transfer/network/resume tests;
- accessibility/localization validation;
- exact final signed-artifact dependency/license/provenance review;
- store declarations/screenshots/metadata.

Follow `NEXT_STEPS.md`, `docs/testing/manual-test-matrix.md`, `docs/release/release-checklist.md`, and `docs/release/dependency-evidence.md`.

## Current portable final-audit contract

The maintained portable verifier currently runs **26 Python helper tests** and **572 xUnit tests**. In addition to the aggregate History performance-trend/export contract, the final regression set covers resume side-effect boundaries, regular-file staging enforcement, exact one-time credential expiry, bounded concurrent security-state admission, discovery expiry, and strict mDNS RDATA isolation. The August 18 continuation adds deterministic seeded reference-model state machines for the attempt rate limiter, one-time authorization store, and discovery registry without adding a new test dependency or changing runtime source. The August 18 continuation adds deterministic seeded reference-model state machines for the attempt rate limiter, one-time authorization store, and discovery registry without adding a new test dependency or changing runtime source. The August 18 continuation adds deterministic seeded reference-model state machines for the attempt rate limiter, one-time authorization store, and discovery registry without adding a new test dependency or changing runtime source.

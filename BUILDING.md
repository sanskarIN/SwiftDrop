# Building SwiftDrop

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
- canonical documentation files and local Markdown link integrity;
- English/Hindi localization catalogs and placeholder parity;
- Apple App Group/iOS Share Extension project/entitlement/version invariants;
- Core restore/build;
- portable tests;
- benchmark-project compilation.

The documentation check can also be run independently:

```bash
python3 scripts/validate_documentation.py
```

## Stable compiler policy

Repository-wide `Directory.Build.props` uses stable `LangVersion=latest`, nullable reference types, current analyzers, deterministic builds, and warnings-as-errors for portable projects.

MAUI/Apple platform projects keep platform SDK availability/obsolete warnings visible while still failing common nullable-safety warnings.

## Dependency security policy

Repository-wide restore explicitly enables NuGet auditing for direct and transitive dependencies with `NuGetAudit=true`, `NuGetAuditMode=all`, and `NuGetAuditLevel=low`. Because warnings are treated as errors, a known low/moderate/high/critical NuGet vulnerability blocks normal verification rather than being silently accepted.

For a machine-readable local dependency review with the .NET 10 SDK:

```bash
dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --format json
dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --vulnerable --format json
```

The release-readiness workflow captures equivalent JSON dependency and vulnerability reports for Core, tests, benchmarks, and the iOS Share Extension as release evidence. These reports supplement restore-time audit enforcement; they do not replace license/provenance review of the exact signed candidate.

## Android

```bash
dotnet workload install maui-android
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Release --no-restore
```

Android production release still requires a private release keystore, signing configuration, AAB/APK generation, install/upgrade testing, and Play Console/store checks.

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

The override properties narrow this validation command to the Windows TFM; normal product builds still retain the full target matrix. `WindowsPackageType=None` and `GenerateAppxPackageOnBuild=false` intentionally validate source/XAML/WinUI compilation without claiming MSIX readiness. Production packaging requires the real signing certificate/package identity, signed MSIX generation, install/update validation, protocol activation, and capability checks.

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

Mac Catalyst external intake uses the containing app's normal document/file flows and native `UIDropInteraction`. Release validation must confirm sandbox/network/App Group entitlements used by the containing app, native drop, signing, notarization, and store packaging.

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

Simulator compilation checks source/API compatibility only. Physical iOS devices, archives, TestFlight and App Store distribution require Apple signing, provisioning, App Group configuration and extension runtime validation.

## Apple runtime validation

Before a signed release verify:

1. the iOS Share Extension appears for its declared file/text/image/movie/web-URL activation types;
2. provider representations stage while their access is valid;
3. the extension publishes only bounded validated packages;
4. the iOS containing app imports packages on cold/warm activation;
5. imported content is shown for review and never automatically sent;
6. stale/malformed/symlinked App Group packages are rejected/pruned;
7. the App Group works in the signed iOS app/extension configuration;
8. Mac Catalyst native drag/drop remains independent of the iOS Share Extension and works under signed sandbox rules.

## Synthetic performance harness

Uses generated temporary data only:

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

See `docs/testing/performance-benchmarks.md`.

## CI

GitHub Actions is configured for:

- documentation integrity validation;
- localization validation;
- Apple integration metadata validation;
- portable Core build/tests;
- benchmark compile;
- CodeQL/security hygiene;
- direct/transitive NuGet vulnerability auditing on restore;
- Android app compile;
- focused Windows app compile;
- Mac Catalyst containing-app compile;
- certificate-independent iOS Simulator Share Extension + containing-app compile;
- machine-readable release dependency/vulnerability inventories, including the iOS Share Extension graph;
- aggregate release-readiness gate.

A configured workflow is not proof it passed. Confirm the exact release-candidate run before publishing.

## Release boundary

Successful source compilation does not replace:

- signed install/upgrade checks;
- Apple App Group provisioning;
- physical-device iOS Share Extension/native-drop behavior;
- real peer-to-peer transfer/network/resume tests;
- accessibility/localization validation;
- exact release dependency/license review;
- store declarations/screenshots/metadata.

Follow `NEXT_STEPS.md`, `docs/testing/manual-test-matrix.md`, and `docs/release/release-checklist.md`.

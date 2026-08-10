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
- `SwiftDrop.ShareExtension`;
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
- English/Hindi localization catalogs and placeholder parity;
- Apple App Group/Share Extension project/entitlement/version invariants;
- Core restore/build;
- portable tests;
- benchmark-project compilation.

## Stable compiler policy

Repository-wide `Directory.Build.props` uses stable `LangVersion=latest`, nullable reference types, current analyzers, deterministic builds, and warnings-as-errors for portable projects.

MAUI/Apple platform projects keep platform SDK availability/obsolete warnings visible while still failing common nullable-safety warnings.

## Android

```bash
dotnet workload install maui-android
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Release --no-restore
```

Android production release still requires a private release keystore, signing configuration, AAB/APK generation, install/upgrade testing, and Play Console/store checks.

## Windows

Run on Windows with current .NET MAUI Windows workload and Windows App SDK prerequisites:

```powershell
dotnet workload install maui-windows
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-windows10.0.19041.0 -c Release --no-restore
```

Production packaging requires the real signing certificate/package identity and install/update validation.

## Apple prerequisites

Run Apple builds on macOS with current Xcode and .NET MAUI iOS/Mac Catalyst workloads:

```bash
dotnet workload install maui-ios maui-maccatalyst
```

SwiftDrop contains a dedicated Share Extension:

`src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`

The containing app and extension must share App Group:

`group.in.sanskar.swiftdrop`

Bundle IDs:

- containing app: `in.sanskar.swiftdrop`;
- Share Extension: `in.sanskar.swiftdrop.share`.

The repository validator checks that source metadata stays synchronized:

```bash
python3 scripts/validate_apple_integration.py
```

That source check cannot create Apple Developer App Group capabilities or provisioning profiles. The real signing environment must configure the same App Group for both identifiers.

## Mac Catalyst app + Share Extension

```bash
dotnet restore src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj -p:TargetFramework=net10.0-maccatalyst
dotnet build src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj -f net10.0-maccatalyst -c Release --no-restore

dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-maccatalyst
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-maccatalyst -c Release --no-restore
```

The app project references the extension as `IsAppExtension=true`; build/package validation must confirm the extension is embedded in the signed app and the sandbox/App Group entitlements are accepted.

## iOS Simulator app + Share Extension

Select the simulator RID matching the macOS runner:

```bash
RID=iossimulator-arm64   # use iossimulator-x64 on x64 hosts

dotnet restore src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj -p:TargetFramework=net10.0-ios -p:RuntimeIdentifier=$RID
dotnet build src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj -f net10.0-ios -c Release --no-restore -p:RuntimeIdentifier=$RID

dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-ios -p:RuntimeIdentifier=$RID
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-ios -c Release --no-restore -p:RuntimeIdentifier=$RID
```

Unsigned simulator compilation checks source/API compatibility only. Physical iOS devices, archives, TestFlight and App Store distribution require Apple signing, provisioning, App Group configuration and extension runtime validation.

## Apple Share Extension runtime validation

Before a signed release verify:

1. extension appears for its declared file/text/image/movie/web-URL activation types;
2. provider representations stage while their access is valid;
3. extension publishes only bounded validated packages;
4. containing app imports packages on cold/warm activation;
5. imported content is shown for review and never automatically sent;
6. stale/malformed/symlinked App Group packages are rejected/pruned;
7. App Group works in iOS and Mac Catalyst release sandboxes;
8. Mac native drag/drop remains independent of the Share Extension and works under sandbox rules.

## Synthetic performance harness

Uses generated temporary data only:

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

See `docs/testing/performance-benchmarks.md`.

## CI

GitHub Actions is configured for:

- localization validation;
- Apple integration metadata validation;
- portable Core build/tests;
- benchmark compile;
- CodeQL/security hygiene;
- Android app compile;
- Windows app compile;
- Mac Catalyst **Share Extension + app** compile;
- unsigned iOS Simulator **Share Extension + app** compile;
- release dependency inventories, including both Apple extension frameworks;
- aggregate release-readiness gate.

A configured workflow is not proof it passed. Confirm the exact release-candidate run before publishing.

## Release boundary

Successful source compilation does not replace:

- signed install/upgrade checks;
- Apple App Group provisioning;
- physical-device Share Extension/native-drop behavior;
- real peer-to-peer transfer/network/resume tests;
- accessibility/localization validation;
- exact release dependency/license review;
- store declarations/screenshots/metadata.

Follow `NEXT_STEPS.md`, `docs/testing/manual-test-matrix.md`, and `docs/release/release-checklist.md`.

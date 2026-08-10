# Building SwiftDrop

Updated: 2026-08-10

SwiftDrop targets .NET 10 and .NET MAUI. Repository-wide compiler policy uses the latest **stable** C# language mode (`LangVersion=latest`), nullable reference types, latest analyzer level, deterministic builds, and warnings-as-errors. Preview language mode is not required by policy.

## Recommended solution file

Use `SwiftDrop.slnx`, the canonical XML solution file used by this repository:

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release
```

## Portable verification

Linux/macOS:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

Portable verification includes:

- localization catalog validation;
- Core restore/build;
- portable xUnit tests;
- synthetic benchmark project compilation.

Localization validation checks XML well-formedness, non-empty entries, duplicate keys, exact English/Hindi key parity, and formatted placeholder-index parity across the app/runtime catalogs.

## Android

```bash
dotnet workload install maui-android
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Debug --no-restore
```

For a production Android build, use release signing material supplied outside the repository. Do not commit keystores/passwords. The current manifest disables app backup for SwiftDrop-local metadata and uses foreground data-sync/notification permissions required by the implemented Android transfer path.

## Windows

Run on Windows with the .NET MAUI Windows workload and Windows App SDK prerequisites:

```powershell
dotnet workload install maui-windows
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-windows10.0.19041.0 -c Debug
```

The package manifest requests `privateNetworkClientServer` for the local peer protocol and does not request general `internetClient` in protocol version 1. Production package signing remains external release configuration.

## iOS and Mac Catalyst

Run on macOS with current Xcode and relevant workloads:

```bash
dotnet workload install maui-ios maui-maccatalyst
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-maccatalyst
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-maccatalyst -c Debug
```

For an unsigned simulator smoke build, use the target/runtime combination appropriate to the runner architecture. The repository platform workflow selects an unsigned iOS Simulator runtime rather than claiming a signed device build.

Mac Catalyst uses the committed `Platforms/MacCatalyst/Entitlements.plist` through conditional `CodesignEntitlements` configuration. The entitlements declare:

- app sandbox;
- network client;
- network server.

The committed iOS/Mac Catalyst Info.plist files declare:

- local-network usage rationale;
- `_swiftdrop._tcp` Bonjour service;
- `swiftdrop` URL scheme;
- `public.data` document/open-file support;
- opening documents in place.

Real device/archive builds additionally require Apple Developer signing/provisioning appropriate to the app, Xcode version, target and distribution channel. Document/open-file URL staging must be tested under the signed sandbox/security-scoped file-provider environments used for release.

A dedicated Apple Share Extension is **not** part of the current source. Do not add extension signing/App Group assumptions to the normal app build unless a real extension target is implemented and reviewed.

## CI

GitHub Actions configuration includes:

- portable Core build/tests;
- localization validation;
- benchmark compilation;
- CodeQL analysis;
- repository security hygiene;
- Android MAUI compile smoke;
- Windows MAUI compile smoke;
- Mac Catalyst compile smoke;
- unsigned iOS Simulator compile smoke;
- release-readiness dependency inventory/aggregate gates.

Workflow configuration alone is not evidence that the exact release candidate passed. During the current implementation session, available connector workflow/status lookups returned no usable direct-main runs/status contexts. Verify the Actions UI/logs for the exact candidate before release.

## Physical/release validation

Successful compilation is not equivalent to production validation. Follow:

- `docs/testing/manual-test-matrix.md`;
- `docs/testing/accessibility-checklist.md`;
- `docs/release/release-checklist.md`;
- `docs/release/signing-configuration.md`;
- `docs/release/store-privacy-declarations.md`.

The release matrix must include actual cross-device LAN transfers, restricted network conditions, sleep/lock/background behavior, low storage, platform permissions, Apple security-scoped document activation, signed package install/update, and accessibility checks.

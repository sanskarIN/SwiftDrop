# Building SwiftDrop

SwiftDrop targets .NET 10 and .NET MAUI.

## Recommended solution file

Use `SwiftDrop.slnx`, the official XML solution format used by this repository:

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

The repository also contains the earlier `SwiftDrop.sln` bootstrap file for compatibility/history. Tooling that expects the modern XML solution format should open `SwiftDrop.slnx` explicitly.

## Portable verification

Linux/macOS:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

## Android

```bash
dotnet workload install maui-android
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-android
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-android -c Debug --no-restore
```

## Windows

Run on Windows with the .NET MAUI Windows workload and Windows App SDK prerequisites:

```powershell
dotnet workload install maui-windows
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-windows10.0.19041.0 -c Debug
```

## iOS and Mac Catalyst

Run on macOS with current Xcode and relevant workloads:

```bash
dotnet workload install maui-ios maui-maccatalyst
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-maccatalyst -c Debug
```

An iOS build additionally requires the Apple toolchain/signing configuration appropriate to simulator/device/archive usage.

## CI

GitHub Actions includes:

- portable core build/tests;
- CodeQL analysis;
- MAUI target compile smoke jobs;
- release-readiness compile/test/dependency inventory gates.

Successful compilation is not equivalent to physical-device or store-release validation. Follow `docs/testing/manual-test-matrix.md` and `docs/release/release-checklist.md` before publishing.

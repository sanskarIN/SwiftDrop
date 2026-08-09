# Building SwiftDrop

SwiftDrop targets .NET 10 and .NET MAUI.

## Canonical solution file

Use `SwiftDrop.slnx`, the official XML solution format used by this repository:

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release
```

The earlier XML content with a `.sln` extension was removed because that filename suggested the legacy text solution format while containing `.slnx` XML. Use `SwiftDrop.slnx` consistently.

## Portable verification

Linux/macOS:

```bash
bash scripts/verify-core.sh
python3 scripts/validate_localization.py
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
python scripts/validate_localization.py
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
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-windows10.0.19041.0
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-windows10.0.19041.0 -c Debug --no-restore
```

## iOS and Mac Catalyst

Run on macOS with current Xcode and relevant workloads:

```bash
dotnet workload install maui-ios maui-maccatalyst
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-maccatalyst
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-maccatalyst -c Debug --no-restore
```

For unsigned simulator compile validation, choose the runtime identifier that matches the macOS runner/host architecture:

```bash
RID=iossimulator-arm64   # use iossimulator-x64 on x64 hosts
dotnet restore src/SwiftDrop.App/SwiftDrop.App.csproj -p:TargetFramework=net10.0-ios -p:RuntimeIdentifier=$RID
dotnet build src/SwiftDrop.App/SwiftDrop.App.csproj -f net10.0-ios -c Release --no-restore -p:RuntimeIdentifier=$RID
```

Physical iOS devices, archives, TestFlight, and App Store distribution still require the Apple toolchain, signing identities, provisioning, and release configuration.

## Synthetic performance harness

The benchmark harness uses generated temporary data only:

```bash
dotnet run --project benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release -- --size-mib 128 --iterations 3
```

See `docs/testing/performance-benchmarks.md` for limits and interpretation.

## CI

GitHub Actions includes:

- localization resource parity validation;
- portable core build/tests;
- benchmark-project compile validation;
- CodeQL analysis;
- Android compile smoke job;
- Windows compile smoke job;
- Mac Catalyst compile smoke job;
- unsigned iOS Simulator compile smoke job;
- release-readiness compile/test/dependency inventory gates.

Successful compilation is not equivalent to physical-device or store-release validation. Follow `docs/testing/manual-test-matrix.md` and `docs/release/release-checklist.md` before publishing.

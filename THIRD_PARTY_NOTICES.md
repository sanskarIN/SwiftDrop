# Third-Party Notices

Updated: 2026-08-20

SwiftDrop is licensed under Apache-2.0. It also depends on third-party packages and platform SDK components that remain governed by their own licenses.

This file describes direct source references for review. It is **not** a substitute for the exact restored transitive dependency/license inventory of a signed or otherwise distributable release candidate.

## Shipped/runtime projects

### `SwiftDrop.Core`

Direct NuGet package references:

- `Microsoft.Data.Sqlite` — 10.0.11 in the current project file.
- `Microsoft.Extensions.Logging.Abstractions` — 10.0.11 in the current project file.
- `SQLitePCLRaw.bundle_e_sqlite3` — 3.0.5 explicitly pinned in the current project file.

The SQLite dependency surface includes transitive native/runtime components. Release review must inspect the exact restored graph and advisories for the candidate rather than treating the direct package list as the complete redistribution inventory.

### `SwiftDrop.App`

Direct NuGet package references:

- `Microsoft.Maui.Controls` — 10.0.90 in the current project file.
- `Microsoft.Extensions.Logging.Debug` — 10.0.11 in the current project file.
- `QRCoder` — 1.8.0 in the current project file.

Direct project references:

- `SwiftDrop.Core` on all app targets.
- `SwiftDrop.ShareExtension` on the iOS target as an app-extension project reference.

Maintained hosted audit jobs capture separate restored containing-app dependency/vulnerability evidence for Android, the focused Windows target, Mac Catalyst, and iOS. Those reports improve review coverage, but they still describe the hosted restored graph rather than proving the exact contents of a separately produced signed store artifact.

The Windows hosted compile gate deliberately uses `WindowsPackageType=None` and does not generate a signed MSIX; the final signed/package dependency and notice review must be performed against the real release package rather than the unpackaged CI compile output.

### `SwiftDrop.Desktop`

The maintained Avalonia desktop host is used for Linux packaging and directly references:

- `Avalonia` — 12.1.1 in the current project file.
- `Avalonia.Desktop` — 12.1.1 in the current project file.
- `Avalonia.Fonts.Inter` — 12.1.1 in the current project file.
- `Avalonia.Themes.Fluent` — 12.1.1 in the current project file.
- `SwiftDrop.Core` as a direct project reference.

Avalonia UI's open-source framework is licensed under the MIT License by AvaloniaUI OÜ. The repository/package release process must preserve any license/notice material required by the exact Avalonia packages and their transitive dependencies. Do not infer the complete Linux redistribution notice set from the four direct package names alone.

The dedicated Linux workflow captures direct/transitive package and vulnerable-package evidence for the desktop project while generating the self-contained `linux-x64` and `linux-arm64` packages. A self-contained publish includes runtime/native assets beyond the four direct Avalonia references, so the final package notice/license review must inspect the exact generated archive and restored graph.

### `SwiftDrop.ShareExtension`

The dedicated **iOS-only** Share Extension currently declares no direct NuGet `PackageReference` of its own. It references:

- `SwiftDrop.Core`.

Its restored runtime graph therefore still includes dependencies pulled by the referenced Core project and the iOS/.NET target packs. Maintained Apple audit evidence captures the restored iOS extension graph separately from the iOS containing app. Release review must inspect the **restored iOS extension target graph** and final signed extension package, not infer that “no direct PackageReference” means “no third-party/runtime dependencies.”

Mac Catalyst uses the containing desktop app/native-drop path and does not have a maintained Share Extension target.

## Test-only dependencies

`tests/SwiftDrop.Core.Tests` directly references:

- `Microsoft.NET.Test.Sdk` — 18.9.0 in the current project file.
- `xunit` — 2.9.3 in the current project file.
- `xunit.runner.visualstudio` — 3.1.5 in the current project file.
- `coverlet.collector` — 10.0.1 in the current project file.

These test dependencies are not automatically part of a shipped application binary merely because they are used by CI/development tests.

## Benchmark/development dependencies

The synthetic benchmark project and repository build tooling may restore additional .NET SDK/framework assets according to their project/SDK declarations. They are development/release-engineering inputs unless the exact distributed binary dependency graph demonstrates otherwise.

## Platform SDKs and framework components

Building SwiftDrop can use:

- .NET 10 SDK/runtime/target packs;
- .NET MAUI workloads;
- Avalonia desktop packages/runtime assets for the Linux host;
- Android SDK/platform tooling;
- Apple Xcode, iOS SDK, and Mac Catalyst/macOS SDK components;
- Windows App SDK / Windows platform tooling;
- Linux desktop/runtime libraries required by the selected Avalonia backend and distribution.

Those SDKs/framework/runtime components are governed by their respective licenses. They are not automatically redistributed by this repository merely because SwiftDrop targets them; self-contained Linux archives must be reviewed for the assets they actually redistribute.

## Machine-readable dependency evidence

Maintained platform/release workflows can produce these evidence bundles:

- `dependency-audit` — Core, portable tests, and benchmark reports;
- `android-dependency-audit` — Android containing-app graph;
- `windows-dependency-audit` — focused Windows containing-app graph;
- `apple-dependency-audit` — Mac Catalyst containing app, iOS containing app, and iOS Share Extension graphs;
- `swiftdrop-linux-x64` — self-contained Linux x64 archive plus desktop dependency-audit evidence;
- `swiftdrop-linux-arm64` — self-contained Linux ARM64 archive plus desktop dependency-audit evidence.

Reports use an explicit machine-readable JSON schema version, include direct/transitive package views, and include vulnerable-package views validated by repository tooling. Each dependency-evidence bundle also carries a deterministic evidence manifest recording report byte lengths and SHA-256 digests.

These manifests are integrity aids for retained report files. They are not digital signatures, SBOM attestations, or proof that an independently produced signed/distributed artifact contains exactly the same graph.

See `docs/release/dependency-evidence.md` for the canonical evidence contract.

## Release dependency inventory

Before publishing any binary release:

1. Restore the **exact release-candidate commit** on the supported target environments.
2. Generate/retrieve target-specific dependency inventories for `SwiftDrop.Core`, Android/Windows/Mac Catalyst/iOS `SwiftDrop.App`, Linux `SwiftDrop.Desktop`, and the iOS `SwiftDrop.ShareExtension` target.
3. Retain the exact-candidate `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, `apple-dependency-audit`, and applicable Linux desktop workflow artifacts.
4. Verify each retained dependency-evidence bundle against its SHA-256 `manifest.json` before archival/review.
5. Confirm the vulnerable-package reports contain no findings under the configured advisory data and command semantics.
6. Include transitive packages/framework/native components where redistribution or notice obligations apply.
7. Review package provenance, versions, licenses, notices, vulnerabilities/security advisories, and redistribution terms manually.
8. Confirm the restored graph does not reintroduce a vulnerable or otherwise disallowed SQLite native dependency path.
9. For Linux, inspect the actual self-contained archive contents and the runtime/native assets introduced by the selected RID.
10. Compare the source/restored inventories against the final signed/package/distribution outputs; do not rely only on project files or hosted simulator/unpackaged reports.
11. Include every required attribution/license text in the app/package/release materials.
12. Repeat the review whenever package versions, workloads, SDKs, target frameworks, runtime identifiers, or shipped projects change.

Workflow-generated evidence substantially improves reproducibility, but workflow configuration or a historical green run alone is not proof that the exact distributed candidate was reviewed.

## User content

No third-party dependency license grants SwiftDrop or its dependencies ownership rights over files/text a user chooses to transfer. User content remains governed by the user and applicable law/agreements.

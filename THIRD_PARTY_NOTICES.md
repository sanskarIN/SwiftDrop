# Third-Party Notices

Updated: 2026-08-14

SwiftDrop is licensed under Apache-2.0. It also depends on third-party packages and platform SDK components that remain governed by their own licenses.

This file describes direct source references for review. It is **not** a substitute for the exact restored transitive dependency/license inventory of a signed release candidate.

## Shipped/runtime projects

### `SwiftDrop.Core`

Direct NuGet package references:

- `Microsoft.Data.Sqlite` — 10.0.10 in the current project file.
- `Microsoft.Extensions.Logging.Abstractions` — 10.0.0 in the current project file.
- `SQLitePCLRaw.bundle_e_sqlite3` — 2.1.12 explicitly pinned in the current project file so restore does not select the previously blocked vulnerable native SQLite bundle path.

The SQLite dependency surface includes transitive native/runtime components. Release review must inspect the exact restored graph and advisories for the candidate rather than treating the direct package list as the complete redistribution inventory.

### `SwiftDrop.App`

Direct NuGet package references:

- `Microsoft.Maui.Controls` — 10.0.90 in the current project file.
- `Microsoft.Extensions.Logging.Debug` — 10.0.0 in the current project file.
- `QRCoder` — 1.8.0 in the current project file.

Direct project references:

- `SwiftDrop.Core` on all app targets.
- `SwiftDrop.ShareExtension` on the iOS target as an app-extension project reference.

The Windows hosted compile gate deliberately uses `WindowsPackageType=None` and does not generate a signed MSIX; the final signed/package dependency and notice review must be performed against the real release package rather than the unpackaged CI compile output.

### `SwiftDrop.ShareExtension`

The dedicated **iOS-only** Share Extension currently declares no direct NuGet `PackageReference` of its own. It references:

- `SwiftDrop.Core`.

Its restored runtime graph therefore still includes dependencies pulled by the referenced Core project and the iOS/.NET target packs. Release review must inspect the **restored iOS extension target graph**, not infer that “no direct PackageReference” means “no third-party/runtime dependencies.”

Mac Catalyst uses the containing desktop app/native-drop path and does not have a maintained Share Extension target.

## Test-only dependencies

`tests/SwiftDrop.Core.Tests` directly references:

- `Microsoft.NET.Test.Sdk` — 18.8.1 in the current project file.
- `xunit` — 2.9.3 in the current project file.
- `xunit.runner.visualstudio` — 3.1.5 in the current project file.
- `coverlet.collector` — 10.0.1 in the current project file.

These test dependencies are not automatically part of a shipped application binary merely because they are used by CI/development tests.

## Benchmark/development dependencies

The synthetic benchmark project and repository build tooling may restore additional .NET SDK/framework assets according to their project/SDK declarations. They are development/release-engineering inputs unless the exact signed binary dependency graph demonstrates otherwise.

## Platform SDKs and framework components

Building SwiftDrop can use:

- .NET 10 SDK/runtime/target packs;
- .NET MAUI workloads;
- Android SDK/platform tooling;
- Apple Xcode, iOS SDK, and Mac Catalyst/macOS SDK components;
- Windows App SDK / Windows platform tooling.

Those SDKs are governed by their respective licenses. They are not automatically redistributed by this repository merely because SwiftDrop targets them.

## Release dependency inventory

Before publishing any binary release:

1. Restore the **exact release-candidate commit** on the supported target environments.
2. Generate dependency inventories for `SwiftDrop.Core`, `SwiftDrop.App`, and the iOS `SwiftDrop.ShareExtension` target.
3. Include transitive packages/framework/native components where redistribution or notice obligations apply.
4. Review package provenance, versions, licenses, notices, vulnerabilities/security advisories, and redistribution terms.
5. Confirm the restored graph does not reintroduce the previously blocked vulnerable SQLite native dependency version/path.
6. Compare the inventory against the final signed/package outputs; do not rely only on source project files.
7. Include every required attribution/license text in the app/package/release materials.
8. Repeat the review whenever package versions, workloads, SDKs, target frameworks, or shipped projects change.

The release-readiness workflow can generate dependency inventory evidence, but workflow configuration alone is not proof that the exact signed candidate was reviewed.

## User content

No third-party dependency license grants SwiftDrop or its dependencies ownership rights over files/text a user chooses to transfer. User content remains governed by the user and applicable law/agreements.

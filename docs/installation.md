# SwiftDrop Installation and Source-Run Guide

SwiftDrop's repository currently documents and validates source builds. A hosted compile is not the same as an officially signed end-user package. Do not install unsigned/untrusted packages from unknown sources merely because they use the SwiftDrop name.

## Current distribution boundary

The repository contains source for Android, iOS, Mac Catalyst, and Windows. Final production distribution still requires the applicable signing, packaging, physical-device, store/privacy, and release-checklist gates for an exact candidate.

Until a release explicitly publishes verified signed artifacts, the supported developer path is to build from source.

## Clone source

```bash
git clone https://github.com/sanskarIN/SwiftDrop.git
cd SwiftDrop
git checkout main
```

For a reproducible release-candidate test, check out the exact candidate commit SHA instead of a moving branch.

## Required base tools

- Git
- .NET 10 SDK
- the .NET MAUI workload(s) for the target being built
- target platform SDK/tooling

See `BUILDING.md` for exact maintained commands and environment notes.

## Validate portable source first

Linux/macOS:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

This verifies portable Core/tests and metadata validators before spending time on a platform package.

## Android developer build

Install the MAUI Android workload and Android SDK components required by the current .NET MAUI toolchain.

Then restore/build the `net10.0-android` target according to `BUILDING.md` or `.github/workflows/platform-builds.yml`.

For physical-device installation:

- use a developer/debug-signed build for development;
- enable normal Android developer deployment using Android Studio/.NET tooling;
- grant only permissions required by the app flow you are testing;
- do not treat a debug-signed APK as the production Play artifact.

A production release requires external signing material and final AAB/APK validation outside the repository.

## iOS developer build

Requirements include macOS, compatible Xcode, .NET 10, MAUI iOS workload, and Apple Developer signing/provisioning for physical-device operation.

The containing app ID is:

`in.sanskar.swiftdrop`

The Share Extension ID is:

`in.sanskar.swiftdrop.share`

The App Group is:

`group.in.sanskar.swiftdrop`

Simulator compilation can run without real signing under the repository's CI-specific simulator code-signing configuration. Physical-device/TestFlight builds must use correct real provisioning and App Group entitlements.

## Mac Catalyst developer build

Requirements include macOS, compatible Xcode, .NET 10, and the MAUI Mac Catalyst workload.

The maintained Mac Catalyst app is the containing desktop app. It uses native drag/drop/document intake and does not embed a maintained Mac Catalyst Share Extension.

For distribution, validate:

- signing identity;
- sandbox entitlements;
- local network client/server behavior;
- security-scoped resources;
- notarization/store packaging when applicable.

## Windows developer build

Requirements include Windows, .NET 10, MAUI Windows workload, Windows SDK/App SDK dependencies used by .NET MAUI, and appropriate packaging tooling for signed-package validation.

The hosted CI compile intentionally does not claim signed MSIX installation validation.

For real package validation, create/install the signed Windows package and test:

- protocol activation;
- private-network capability;
- firewall profile behavior;
- custom receive-folder picker;
- drag/drop;
- install/update/uninstall.

## Installing a future official release

When the project publishes a verified release, prefer artifacts linked from the official repository/release/distribution channel and verify that the release notes identify the expected version/candidate.

Do not download packages from unrelated mirrors unless the project explicitly documents them.

## First-run expectations

Depending on platform and the feature used, the OS may request or enforce:

- local-network access;
- notifications on Android if explicitly enabled;
- firewall access on Windows/macOS;
- document/provider access for selected/shared files;
- Apple App Group/provisioning internally for the iOS Share Extension.

SwiftDrop does not need broad public-Internet transfer permissions for its protocol-v1 local peer path.

## Updating a development build

When switching source commits:

1. record the previous/current commit SHA;
2. restore packages/workloads as needed;
3. run portable verification;
4. rebuild the target;
5. retest migrations/settings/trust/identity behavior if the changed source affects persistence or security state.

Do not assume app-local state created by an older development build is compatible without migration coverage.

## Uninstalling

Operating-system uninstall behavior controls removal of application-private storage. Do not assume external/custom receive files should be deleted by uninstall; user-owned received files should be treated according to platform storage semantics.

For privacy-sensitive testing, verify actual uninstall/backup/restore behavior on the target as part of release validation.

## Build problems

Read:

- `BUILDING.md`;
- `docs/development-guide.md`;
- `docs/troubleshooting.md`;
- `.github/workflows/platform-builds.yml` for the maintained hosted compile commands.

## Release/production installation

For release engineering rather than development installation, use:

- `docs/release/release-process.md`;
- `docs/release/signing-configuration.md`;
- `docs/release/release-checklist.md`.

---

**Made by the Sanskar**

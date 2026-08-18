# SwiftDrop Final In-Repository Audit — 2026-08-18

This document records the final repository-level audit performed after the deterministic state-machine hardening and August dependency-maintenance sequence.

## Scope

The audit covered the maintained repository surface that can be validated from source and GitHub-hosted tooling:

- Core protocol, discovery, pairing, authorization, transfer, resume, path/storage, diagnostics, queue, history, and persistence code;
- Android, iOS, Mac Catalyst, Windows, and iOS Share Extension project/configuration surfaces;
- portable tests and helper validators;
- dependency declarations and direct third-party notices;
- maintained GitHub Actions workflows;
- current release, CI, security, platform, support, privacy, and contributor documentation;
- open issue/pull-request queue state.

It does not claim to replace signed-package, physical-device, store, provisioning, notarization, accessibility, or real-network validation.

## Final dependency correction

A final audit found that `tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj` had been moved from `xunit.runner.visualstudio` 3.1.5 to `4.0.0` even though the xUnit project and NuGet currently list **3.1.5 as the latest stable Visual Studio adapter** and `4.0.0-pre.5` as the current prerelease line.

The test adapter was therefore restored to the stable `3.1.5` version in PR #23. This is a test/development dependency correction and does not alter SwiftDrop application/runtime behavior.

Official references used for the version check:

- https://xunit.net/releases/
- https://www.nuget.org/packages/xunit.runner.visualstudio

## Current direct dependency baseline

### SwiftDrop.Core

- `Microsoft.Data.Sqlite` 10.0.11
- `Microsoft.Extensions.Logging.Abstractions` 10.0.11
- `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5

### SwiftDrop.App

- `Microsoft.Maui.Controls` 10.0.90
- `Microsoft.Extensions.Logging.Debug` 10.0.11
- `QRCoder` 1.8.0

### SwiftDrop.Core.Tests

- `Microsoft.NET.Test.Sdk` 18.9.0
- `xunit` 2.9.3
- `xunit.runner.visualstudio` 3.1.5
- `coverlet.collector` 10.0.1

`THIRD_PARTY_NOTICES.md` was synchronized to this direct dependency baseline on 2026-08-18. Exact release review must still use the restored target-specific transitive graphs and the final signed artifacts rather than only this direct list.

## GitHub Actions maintenance

All seven maintained dependency-evidence upload steps now use `actions/upload-artifact@v7`:

- three upload steps in `.github/workflows/platform-builds.yml`;
- four upload steps in `.github/workflows/release-readiness.yml`.

The repository continues to maintain five permanent workflow files:

1. `ci.yml`
2. `codeql.yml`
3. `platform-builds.yml`
4. `release-readiness.yml`
5. `security-hygiene.yml`

No new permanent one-shot migration/finalizer workflow was added by this audit.

## Regression/test baseline retained

The last fully recorded pre-dependency-maintenance source/test evidence remains the deterministic state-machine test head `898f17a3157ab7af14d7aeb958b315dde1e1c2af`:

- 572 portable xUnit tests;
- 26 Python helper tests;
- normal CI success;
- CodeQL success;
- security-hygiene success;
- release-readiness success, including Android, focused Windows, Mac Catalyst, iOS Share Extension, and iOS containing-app hosted compile/audit jobs.

The August 18 dependency/workflow maintenance after that point changes package/tooling/workflow inputs, not SwiftDrop runtime application source.

## Current candidate and queued-check caveat

PR #23 merged as commit `387de3c1960b3781dfc4763d944b55ee8df39297` after restoring the stable xUnit adapter and synchronizing direct dependency notices.

At merge time, the newly scheduled GitHub-hosted CI, CodeQL, security-hygiene, and release-readiness runs were still **queued**. They are therefore not recorded as passed evidence in this document. The correction was merged because it returns the test adapter to the published stable version and to the previously validated adapter line; queued workflow status must not be relabeled as success.

A production candidate must still be observed through all maintained exact-candidate gates before release.

## Final repository queue check

After PR #23 merged:

- open pull requests: none;
- open GitHub issues: none found by the final repository queue search;
- maintained-source search found no `TODO`, `FIXME`, or `NotImplementedException` implementation marker requiring closure.

The absence of open issues is not proof that no undiscovered defect exists; future source work should be driven by a reproducible defect, platform/dependency change, or deliberately scoped post-v1 feature.

## Current source-completion assessment

For the current project scope, SwiftDrop remains **source-complete and in release-validation phase**. The repository contains the planned local-transfer, security, resume, history, queue, diagnostics, platform-integration, localization, notification, CI, audit, and documentation surfaces already recorded by `PROJECT_STATUS.md`, `NEXT_STEPS.md`, the protocol/security documents, and `what_changed.md`.

No additional mandatory in-repository product feature was identified by this final audit.

## External production gates that remain mandatory

The following cannot be truthfully completed from repository source/hosted compilation alone and remain required for a production release:

- signed Android AAB/APK creation, install/upgrade, permission/notification, real-provider, lifecycle, low-storage, and Play policy validation;
- signed Windows MSIX creation, install/update/uninstall, protocol registration, firewall behavior, packaged FolderPicker/native drop, toast/COM activation, and package-identity validation;
- Apple Developer App Group/provisioning configuration, signed iOS device/TestFlight Share Extension/provider testing, iOS notification behavior, and App Store checks;
- signed/notarized Mac Catalyst sandbox, App Group, native-drop, networking/firewall, and notification validation;
- physical Android/Windows/iOS/Mac cross-device transfers over representative IPv4/IPv6, multicast-limited, isolated/guest, switching, sleep/lock/background, interruption/resume, and low-storage scenarios;
- SecureStorage/keychain/keystore and real filesystem semantics on representative targets;
- TalkBack, VoiceOver, Narrator, keyboard-only, high-contrast, large-text, reduced-motion, resize/rotation, focus-order, and Hindi localization checks on real environments;
- exact signed-artifact dependency/license/provenance reconciliation and final privacy/store declarations.

Hosted source checks are evidence for source correctness and compileability; they are not substitutes for these signed/device/store gates.

## Final maintenance rule

Do not merge a dependency/tooling update merely because its diff is small or an older branch previously passed. Before a release, require the exact candidate to finish the maintained automated checks and keep the direct dependency notices synchronized with the project files.

---

**Made by the Sanskar**

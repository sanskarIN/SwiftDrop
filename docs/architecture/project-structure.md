# SwiftDrop Project Structure

This document explains the maintained repository layout and the responsibility of each major project or directory.

## Repository root

### `SwiftDrop.slnx`

Canonical .NET solution file. Use this for repository-wide restore/navigation where a solution is appropriate.

### `Directory.Build.props`

Repository-wide MSBuild quality/security defaults, including warnings-as-errors and explicit NuGet vulnerability-audit policy.

### `README.md`

Public project overview, capabilities, platform scope, privacy model, current build/test commands, support links, and production-readiness boundary.

### `BUILDING.md`

Detailed SDK/workload and target-specific build instructions.

### `PROJECT_STATUS.md`

Current implementation and verification status. It distinguishes source implementation/hosted compile evidence from external signed-device/store validation.

### `NEXT_STEPS.md`

Remaining release-candidate and external validation work.

### `CHANGELOG.md`

User/developer-facing change history for the current unreleased line and prior repository work.

### `what_changed.md`

Detailed engineering continuation ledger. This is intentionally more exhaustive than the public changelog.

### Legal/community files

- `LICENSE` — Apache-2.0 license text.
- `NOTICE` — project notice.
- `THIRD_PARTY_NOTICES.md` — dependency/license notice inventory maintained in source.
- `PRIVACY.md` — privacy behavior and local metadata policy.
- `SECURITY.md` — vulnerability-reporting/security policy.
- `SUPPORT.md` — support channels.
- `TERMS.md` — project/application terms.
- `CONTRIBUTING.md` — contributor workflow and quality expectations.
- `CODE_OF_CONDUCT.md` — community conduct expectations.
- `DECISIONS.md` — architecture/engineering decision record.

## `src/SwiftDrop.Core`

Portable application/domain/infrastructure logic that should not depend on MAUI UI types or target-specific platform APIs.

Major responsibilities include:

- protocol records, framing, serialization, strict JSON guards, and request validation;
- pairing models/canonical parsing and authorization policy;
- certificate/fingerprint and transport-security helpers;
- transfer manifest/path/filename validation;
- file/batch/text transfer coordination primitives;
- collision-safe destination/path safety;
- hashing/integrity/resume metadata rules;
- local discovery protocol helpers;
- SQLite persistence and schema migration logic;
- privacy/redaction and reusable policy utilities.

Portable security/correctness behavior belongs here whenever it can be expressed without platform APIs.

## `src/SwiftDrop.App`

The .NET MAUI containing application.

Maintained target frameworks:

- `net10.0-android`
- `net10.0-ios`
- `net10.0-maccatalyst`
- `net10.0-windows10.0.19041.0` when building on Windows

Minimum supported platform declarations in the current project include Android 24, iOS 15, Mac Catalyst 15, and Windows target platform minimum 10.0.17763.0.

Major responsibilities include:

- MAUI application lifecycle and dependency registration;
- pages and view models;
- user-driven send/receive/review flows;
- platform picker/dialog/provider boundaries;
- application settings and appearance;
- queue/history/trust/diagnostics presentation;
- platform activation/share/drop integration;
- app-local receive/cache locations;
- local transport/discovery hosting using Core policy.

### Pages and view models

The app contains the primary dashboard plus dedicated pages/view models for areas such as:

- nearby devices;
- history;
- queue;
- trusted devices;
- diagnostics;
- settings;
- about;
- incoming batch approval.

View models own presentation state. Portable protocol/security/path/storage policy remains in Core/services rather than XAML event handlers.

### `Platforms/`

Target-specific integration code belongs under platform directories where practical:

- Android: intents/share, multicast lock, foreground transfer service, notifications and Android lifecycle integration.
- iOS: URL/document activation, local-network and containing-app integration, App Group import path.
- Mac Catalyst: native drop/document activation, security-scoped resources, sandbox-related integration.
- Windows: protocol activation, folder picker, drag/drop, WinUI/WinRT integration, private-network packaging capability.

### `Resources/`

MAUI app icons, splash assets, images, raw assets, styling, and localization resources.

## `src/SwiftDrop.ShareExtension`

Dedicated **iOS-only** Share Extension target (`net10.0-ios`).

Responsibilities include:

- accepting supported iOS share-sheet provider input;
- copying/normalizing input under bounded staging budgets;
- publishing a strict versioned App Group package;
- respecting provider-response timeout and extension lifetime;
- ensuring the containing app reviews imported content rather than auto-sending it.

The maintained architecture does **not** include a Mac Catalyst Share Extension target.

App Group:

`group.in.sanskar.swiftdrop`

Containing app ID:

`in.sanskar.swiftdrop`

Share Extension ID:

`in.sanskar.swiftdrop.share`

## `tests/SwiftDrop.Core.Tests`

Portable xUnit regression/security/integration test project.

Coverage areas include:

- strict/canonical pairing representation;
- one-time authorization and replay rejection;
- certificate/fingerprint policy;
- protocol framing and strict JSON behavior;
- canonical path/filename/transfer-ID validation;
- source/destination symlink/reparse handling;
- collision behavior;
- transfer integrity, partial resume, and batch resume;
- SQLite migrations/corruption behavior;
- discovery parsing/fuzz cases;
- privacy redaction;
- shared external staging-budget behavior;
- Apple package exact-set validation;
- race/failure boundary cases.

The test project is intended to stay portable so core security/correctness gates can run quickly on standard CI.

## `benchmarks/SwiftDrop.Benchmarks`

Synthetic benchmark/measurement project for important protocol/path/hash/transfer-oriented code paths. CI currently compiles it as part of the portable quality gate; performance documentation describes how to interpret/run measurements.

## `scripts/`

Repository verification/maintenance scripts. Important maintained scripts include portable Core verification and metadata validators.

Examples of responsibilities:

- run portable restore/build/test gates;
- validate English/Hindi localization catalogs;
- validate Apple App Group/entitlement/iOS Share Extension metadata consistency.

Scripts should fail closed when required invariants are missing rather than silently rewriting security/release configuration.

## `.github/`

### Workflows

Maintained workflow set:

- `ci.yml` — portable build/test/validators/benchmark compile and machine-readable vulnerability-audit command validation.
- `platform-builds.yml` — Android, Windows, Mac Catalyst, and iOS Simulator compile matrix.
- `codeql.yml` — CodeQL analysis.
- `security-hygiene.yml` — repository secret/signing/private-key/security-document hygiene.
- `release-readiness.yml` — broader release-candidate aggregation and dependency evidence.

See [CI reference](../testing/ci-reference.md).

### Templates/configuration

- issue templates;
- pull request template;
- Dependabot configuration;
- funding configuration.

## `docs/`

Canonical technical/user documentation hierarchy. Start at [`docs/README.md`](../README.md) from inside this directory or the root documentation link from the repository README.

Major groups:

- architecture;
- platform integration/permissions;
- protocol/security;
- storage;
- testing;
- release;
- user/configuration/FAQ/troubleshooting/development guides.

## Dependency direction

The intended dependency direction is:

`Platform/UI -> App services/view models -> SwiftDrop.Core`

Core must not depend on the MAUI application project or an OS-specific platform project.

The iOS Share Extension may reuse appropriately portable/shared logic but remains a separately built app-extension target and does not turn Core into an extension/UI-aware project.

## Where new code should go

Use these rules:

- portable protocol/security/path/integrity/storage policy -> `SwiftDrop.Core`;
- app-level workflow/presentation -> `SwiftDrop.App` service/view model/page boundary;
- OS APIs -> the relevant `Platforms/` implementation;
- iOS share-sheet host code -> `SwiftDrop.ShareExtension`;
- portable regression coverage -> `SwiftDrop.Core.Tests`;
- performance probes -> benchmark project;
- release/operational explanation -> `docs/` or the appropriate root policy file.

Avoid duplicating business/security policy separately inside Android/iOS/Windows/Mac handlers when it can be centralized and tested in Core.

## Source truth and documentation truth

If documentation and code diverge, do not simply update documentation to make a broken implementation look correct. Determine the intended contract, fix the source or the docs as appropriate, add/adjust tests, and keep `PROJECT_STATUS.md`, `CHANGELOG.md`, and `what_changed.md` synchronized with the evidence.

---

**Made by the Sanskar**

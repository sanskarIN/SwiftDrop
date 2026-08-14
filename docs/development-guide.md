# SwiftDrop Development Guide

This guide is for contributors working on SwiftDrop source. It complements `BUILDING.md`, `CONTRIBUTING.md`, architecture documentation, protocol/security documents, and the test plans.

## 1. Development principles

SwiftDrop is a security-sensitive local-transfer application. Changes should preserve these core principles:

- no SwiftDrop-operated transfer-content relay;
- explicit user pairing/consent boundaries;
- platform cryptography/.NET TLS rather than custom cryptography;
- strict/canonical protocol representation;
- portable path and filename safety;
- metadata-minimal persistence;
- fail-closed integrity/resume behavior;
- target-specific APIs isolated at platform boundaries;
- regression coverage for security/correctness changes;
- documentation that distinguishes compile evidence from signed-device validation.

## 2. Prerequisites

Install:

- .NET 10 SDK;
- Git;
- the .NET MAUI workload(s) required for the target you are changing;
- platform SDK/tooling required by that target.

Typical target tooling includes Android SDK tooling, Xcode on macOS for iOS/Mac Catalyst, and Windows SDK/WinUI tooling on Windows.

See `BUILDING.md` for maintained commands and current platform-specific details.

## 3. Clone and inspect

```bash
git clone https://github.com/sanskarIN/SwiftDrop.git
cd SwiftDrop
git checkout main
```

Canonical solution:

```text
SwiftDrop.slnx
```

Before making changes, read:

- `README.md`;
- `PROJECT_STATUS.md`;
- `NEXT_STEPS.md`;
- `docs/README.md`;
- the architecture/protocol/security document relevant to your change.

## 4. Portable verification baseline

Linux/macOS shell:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

The maintained portable verification path covers Core restore/build/tests and repository metadata validators such as localization and Apple integration consistency.

Individual commands can also be useful while iterating:

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release
```

## 5. NuGet security audit policy

Repository MSBuild policy explicitly enables direct/transitive NuGet vulnerability auditing at low-or-higher severity. Warnings-as-errors makes qualifying audit findings verification-blocking.

For machine-readable review:

```bash
dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --vulnerable --format json
```

Repeat for affected shipped/runtime/test/benchmark projects as appropriate. Do not suppress an advisory merely to make CI green; evaluate provenance, exploitability, fixed versions, compatibility, and the release checklist.

## 6. Choosing the correct source layer

### Change belongs in Core when it is portable policy

Examples:

- pairing parser/canonicalization;
- protocol records and frame parsing;
- path/filename validation;
- authorization/replay logic;
- hashing/integrity/resume policy;
- SQLite schema/persistence logic;
- discovery message validation;
- reusable privacy/redaction policy.

### Change belongs in App when it is application workflow/presentation

Examples:

- page/view-model state;
- application settings;
- navigation;
- queue/history/trust/diagnostics UI;
- orchestration between UI and Core services.

### Change belongs under a platform boundary when it requires OS APIs

Examples:

- Android intents/services/notifications/multicast lock;
- iOS activation/provider/App Group containing-app integration;
- Mac Catalyst native drop/security-scoped access;
- Windows protocol activation/folder picker/drag-drop/WinRT APIs.

### Change belongs in the iOS Share Extension when it handles share-sheet hosting

The maintained Share Extension target is iOS-only. Do not add Mac Catalyst extension behavior unless the architecture and product scope are deliberately changed and separately validated.

## 7. Protocol changes

Protocol changes require special care.

Before editing protocol code, read:

- `docs/protocol/wire-format.md`;
- `docs/protocol/security.md`;
- `docs/protocol/compatibility.md`;
- `docs/protocol/compatibility-matrix.md`;
- `docs/security/THREAT_MODEL.md`.

For any externally observable wire change:

1. define canonical representation;
2. define strict parser/rejection behavior;
3. determine compatibility/versioning impact;
4. update factories/validators/host/client paths together;
5. add positive, negative, malformed, boundary, and replay/security tests;
6. update the wire/security/compatibility docs;
7. update changelog/status/ledger as appropriate.

Do not create a second parser/validator path for one platform when the portable protocol can remain centralized.

## 8. File/path changes

SwiftDrop treats peer paths as untrusted.

Preserve:

- `/` as the only protocol path separator;
- rejection of rooted/drive/UNC/device paths;
- rejection of empty/repeated/trailing separators and `.`/`..`;
- bounded segment/path depth/length;
- canonical filename sanitation/Unicode behavior;
- UTF-8 and UTF-16 filename limits;
- link/reparse rejection at source and destination boundaries;
- containment checks before staging/finalization/resume reuse;
- non-overwrite final promotion;
- collision-marker uniqueness under length limits.

Path changes require adversarial tests, not only happy-path tests.

## 9. Transfer/resume changes

For file or batch resume changes, preserve the authorization and integrity boundaries:

- never trust a partial/final destination solely by filename;
- validate receive-root confinement;
- use expected length/hash metadata;
- re-hash where the current contract requires it;
- preserve stable batch transfer IDs across an interrupted batch retry;
- use a fresh transfer ID for a new explicit batch send;
- do not replay stale pairing/transfer authorization after app restart;
- keep completed-item reuse fail-closed when destination state changes.

Add tests for interruption, mutation, disappearance, collision, low storage, and unsafe link/reparse states.

## 10. Persistence changes

SQLite changes require:

- explicit schema-version handling;
- migration tests from supported prior schemas;
- corruption/failure behavior review;
- privacy review of every new column/table;
- retention/deletion behavior where relevant;
- documentation update in `docs/storage/database-schema.md` and `PRIVACY.md` when data categories change.

Do not persist transferred content, private keys, reusable authorization, or unnecessary absolute paths merely for implementation convenience.

## 11. UI and localization changes

For user-facing text:

- add/update English and Hindi resource entries;
- keep resource keys in parity;
- preserve formatted placeholder indexes;
- avoid embedding new operational text directly in code/XAML when it belongs in localization resources;
- run localization validation.

For XAML:

- use MAUI-supported properties across every maintained target;
- prefer semantic headings/descriptions where useful;
- check large text, wrapping, keyboard/navigation, screen readers, light/dark/system themes, and reduced-motion expectations.

See `docs/testing/accessibility-checklist.md`.

## 12. Platform integration changes

Every platform integration change needs both source validation and real-target validation planning.

### Android

Review intent URI/provider behavior, storage bounds, notification permission, foreground-service policy, multicast lock, lifecycle, and device/OEM behavior.

### iOS

Review local-network privacy, deep-link activation, security-scoped document access, App Group provisioning, extension lifetime/provider callbacks, and device/TestFlight behavior.

### Mac Catalyst

Review sandbox entitlements, network client/server permissions, security-scoped resources, native drop, firewall behavior, signing, and notarization/distribution.

### Windows

Review package capabilities, protocol activation, WinRT/WinUI API behavior, private-network firewall profile behavior, picker permissions, drag/drop, and signed package install/update.

## 13. Testing strategy

Use multiple levels:

### Unit/portable regression tests

Fast, deterministic tests for validators, parsers, policy, persistence, and transfer primitives.

### Loopback/integration tests

Exercise framed protocol, mutual TLS, sequencing, transfer/resume, and persistence boundaries without requiring physical devices.

### Hosted-platform compile gates

Confirm platform source and target references compile under current hosted workload/tooling.

### Physical/signed validation

Required for OS integration that compile tests cannot prove: permissions, providers, background/lifecycle, firewall, App Group, package protocol registration, sandbox, signing/notarization, accessibility, low-storage, network isolation, and store behavior.

## 14. CI workflows

See `docs/testing/ci-reference.md`.

A change is not complete because one workflow is green. Evaluate which gates are relevant to the affected layer.

## 15. Commit style

Use focused conventional-style messages, for example:

```text
fix(protocol): reject noncanonical capability encoding
feat(settings): add privacy-safe transfer preference
test(storage): cover schema migration failure
docs(release): document signed package gate
```

When using the project owner's requested identity in automated/project maintenance work, the maintained sign-off format is:

```text
Signed-off-by: Sanskar <sanskarin@outlook.in>
```

Do not falsely claim a cryptographic Git commit signature if only a sign-off trailer is present.

## 16. Pull request expectations

A good PR explains:

- behavior changed;
- security/privacy impact;
- affected platforms;
- tests added/changed;
- exact verification performed;
- docs updated;
- manual/signed-device validation still required.

Do not label a PR or commit production-ready solely because hosted CI compiles.

## 17. Documentation update rule

Update documentation in the same change set when the contract changes.

Common mappings:

- public feature -> `README.md`, user guide, changelog;
- settings -> `docs/configuration.md`;
- architecture -> `docs/architecture*`, `DECISIONS.md` if decision-worthy;
- protocol -> `docs/protocol/*`;
- permissions/entitlements -> `docs/platform-permissions.md`;
- local data -> `PRIVACY.md`, database schema;
- tests/gates -> testing docs/CI reference;
- release behavior -> release checklist/process/status;
- substantial continuation work -> `what_changed.md`.

## 18. Definition of done for source work

Before considering a code change complete:

- code builds at the relevant layer;
- portable tests pass where applicable;
- new security/correctness paths have regression coverage;
- analyzers/warnings are not ignored without reason;
- dependency audit remains clean or documented/approved;
- relevant platform compile gate passes;
- docs are synchronized;
- external validation requirements are explicitly recorded rather than implied away.

---

**Made by the Sanskar**

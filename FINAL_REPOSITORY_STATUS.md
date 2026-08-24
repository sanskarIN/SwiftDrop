# SwiftDrop — Final Repository Status

Updated: 2026-08-24

This file is the canonical **current repository-side status** for SwiftDrop. Older dated sections in `PROJECT_STATUS.md`, `NEXT_STEPS.md`, `CHANGELOG.md`, and the engineering ledgers are historical evidence and must not be read as newer than this record.

## Current repository-side status

**SwiftDrop is source-complete for the currently maintained application scope and is being stabilized as the 2.5.18 release candidate.**

The final August 19 hardening/UI integration was merged through PR #34 into `main` as `c74eead16691ebd133d78c5fa8f279ba4c11acae` after the substantive Core/test, Android, Windows, Apple, CI, CodeQL, and security checks succeeded.

The active 2.5.18 integration branch then preserved and combined:

- the hardened `main` history;
- PR #35 Linux/Avalonia cross-platform work and 2.5.18 release preparation;
- PR #36 repository-governance/CODEOWNERS history;
- explicit conflict-resolution commits that retain the stricter requirements from all workstreams.

No history was intentionally collapsed merely to simplify integration. The Linux/hardening union uses two-parent merge commit `de79916b52818a141d24429a5d0b51b354c69026`. The governance history is joined by two-parent merge commit `cfeb728682c11c0f20eaaf9c63de775ae3d26716`.

The active candidate must still pass fresh exact-head hosted validation after the final integration/documentation commits. Queued, superseded, or earlier successful workflow runs are not treated as evidence for a newer head.

## Prepared 2.5.18 identifiers

The maintained package surfaces are coordinated for the requested 2.5.18 candidate:

- MAUI application display version: `2.5.18`;
- MAUI application build/version code: `20518`;
- iOS Share Extension display version: `2.5.18`;
- iOS Share Extension build/version code: `20518`;
- Windows package identity version: `2.5.18.0`;
- Avalonia desktop version: `2.5.18`.

`scripts/validate_version_alignment.py` treats this alignment as a machine-enforced invariant. The prepared identifiers do **not** mean `v2.5.18` has been released. A production tag/release must wait for the exact candidate to satisfy the automated and external signed/manual evidence described below.

## Maintained application scope

SwiftDrop currently maintains:

- Android through the .NET MAUI application host;
- iOS containing app through .NET MAUI;
- iOS Share Extension;
- macOS through Mac Catalyst, including native drop integration;
- Windows through .NET MAUI/Windows integration;
- Linux through the Avalonia `SwiftDrop.Desktop` host;
- shared `SwiftDrop.Core` security, discovery, protocol, networking, transfer, storage, and diagnostics logic;
- portable Core tests;
- benchmark tooling;
- release, dependency, documentation, localization, platform-integration, version-alignment, and repository-completion validators.

Linux release readiness covers both `linux-x64` and `linux-arm64` self-contained package paths. Linux is a maintained source/build target, while representative real-distribution execution remains an external release-evidence requirement.

## Integrated Core/security/release hardening

The integrated source preserves the post-v1 hardening work, including:

- receive-root/path/storage/hash/text/network/concurrency/session/settings regression coverage;
- canonical protocol text validation without the former duplicate security-namespace implementation;
- diagnostic privacy redaction across whitespace token boundaries;
- explicit malformed receive-folder validation;
- manual release-evidence generation, strict validation, and status summarization;
- fail-closed handling of the all-zero template release-candidate commit;
- completion state derived from the authoritative strict manual-evidence validator;
- release-readiness path filters and completion invariants for release-evidence tooling.

## Integrated UI/navigation/localization closure

The candidate also preserves the final UI/navigation/localization work, including:

- localized direct home navigation to Transfer Queue, Transfer History, Settings, and About;
- localized pairing-QR accessibility description;
- localized Buy Me a Coffee support copy/accessibility text while retaining the canonical support URL;
- Settings display text separated from canonical persisted values;
- localized certificate-fingerprint, retention, receive-folder, diagnostics, discovery, self-test, queue-state, and operation presentation;
- English/Hindi resource parity validation;
- focused UI-localization contract regression coverage.

## Maintained Linux desktop integration

The 2.5.18 candidate includes the dedicated Avalonia `src/SwiftDrop.Desktop` host while continuing to share `SwiftDrop.Core` protocol/security/transfer behavior.

Repository-side Linux support includes:

- desktop discovery, pairing, identity, receive-server, transfer-client, and batch-resume services;
- Linux desktop entry/protocol-handler metadata;
- self-contained packaging helper `scripts/publish-linux.sh`;
- `linux-x64` and `linux-arm64` package verification in aggregate release readiness;
- dependency/vulnerability evidence for the Linux desktop host;
- `scripts/validate_linux_integration.py` in common CI, Bash verification, PowerShell verification, and dedicated Linux workflow coverage;
- Linux build/install/security documentation.

This establishes maintained Linux source/build support without falsely claiming every target distribution has been physically exercised.

## Repository governance hardening

The candidate includes `.github/CODEOWNERS` with `@sanskarIN` as the repository fallback owner and explicit ownership for sensitive boundaries.

Machine-protected ownership includes:

- GitHub automation, toolchain configuration, and verification scripts;
- Core Security, Discovery, Protocol, Networking, Transfer, and Storage code;
- native MAUI platform integration;
- the Avalonia desktop host;
- the iOS Share Extension;
- Linux packaging;
- security/privacy/third-party/release/protocol/platform documentation surfaces.

`scripts/validate_repository_completion.py` parses CODEOWNERS and rejects missing or reassigned protected entries. The regression suite deliberately tests ownership erosion, including Linux desktop/package paths.

`docs/repository-governance.md` defines the protected-change policy and the safe review model for a repository whose current CODEOWNER set has one maintainer.

### Remote protection boundary

CODEOWNERS is source-side ownership metadata. It does not by itself prove GitHub is enforcing Code Owner approval or protected-branch rules.

The most recently inspected remote state reported `main` as **not protected**. Enabling feasible pull-request/check/conversation/force-push/deletion protections is therefore still a GitHub repository-administration action. An approval rule that requires an independent approval should not be configured until a trusted independent reviewer exists, because the pull-request author cannot provide an independent self-review.

Remote branch/ruleset enforcement must not be represented as enabled until it is actually configured and rechecked.

## Permanent repository-completion contract

`scripts/validate_repository_completion.py` protects the repository-side completion state instead of relying only on status prose. Its current contract verifies, among other things:

- required application/test/benchmark/community/governance/release files exist and are non-empty;
- maintained production source/configuration text is readable UTF-8;
- unfinished production markers remain absent;
- CODEOWNERS retains fallback and sensitive ownership entries;
- release-readiness watches release-critical helpers on both push and pull requests;
- Linux packaging, Linux integration, version alignment, manual-evidence validation/generation/status tooling, and repository completion remain release-critical;
- common CI, Bash verification, and PowerShell verification execute required portable validators;
- the canonical documentation index exposes current 2.5.18, governance, completion, and manual-evidence records;
- the manual release-evidence template remains structurally valid;
- the all-zero release-candidate placeholder does not leak into another JSON evidence record.

Additional dedicated validators protect documentation, English/Hindi localization parity, Apple integration metadata, Windows integration metadata, Linux integration, package-version alignment, NuGet vulnerability reports, and release evidence.

## Hosted validation boundary

The exact current 2.5.18 candidate must complete the maintained hosted validation surface after its final source/documentation commit. The relevant automated surface includes:

- common CI and portable verification;
- Core/xUnit and Python helper tests;
- CodeQL;
- Security hygiene;
- Android Release build/dependency audit;
- Windows Release build/dependency audit;
- Mac Catalyst Release build/dependency audit;
- iOS Simulator Share Extension build/dependency audit;
- iOS Simulator containing-app build/dependency audit;
- dedicated Desktop Linux validation;
- `linux-x64` and `linux-arm64` packaging/dependency audit;
- aggregate Release Readiness.

Earlier green heads demonstrate useful historical behavior but are not final proof for a later commit. This document intentionally does **not** upgrade queued, in-progress, cancelled, skipped, superseded, or unexecuted jobs to successful evidence.

## Source audit result

The maintained completion sweep rejects production-source `TODO`, `FIXME`, `TBD`, `NotImplementedException`, and `#warning` markers through the completion validator. Earlier audits also found no maintained `HACK` or `NotSupportedException` placeholder path in the source scope.

The current continuation work is driven by real integration, Linux cross-platform support, version coordination, release validation, and governance requirements rather than artificial feature churn.

## Documentation status

Canonical documentation covers:

- project/build/contribution/security/privacy/support/legal/community material;
- repository governance and protected-change policy;
- architecture/networking/protocol/platform/storage behavior;
- Linux build/install/security behavior;
- user/FAQ/troubleshooting/diagnostics/glossary/development guidance;
- CI/deterministic/security/manual/accessibility/performance/completion testing;
- release process/checklist/signing/store privacy/dependency/manual-evidence status tooling;
- 2.5.18 preparation and draft release notes;
- dated audit, hardening, UI, governance, integration, and continuation ledgers.

`scripts/validate_documentation.py` validates maintained documentation and local Markdown links. The completion validator separately protects the broader required documentation/governance/release surface.

## Current repository integration queue

- PR #34 — merged into `main` as `c74eead16691ebd133d78c5fa8f279ba4c11acae`.
- PR #35 — active integrated 2.5.18 candidate containing Linux support, hardened-main reconciliation, and the preserved governance history; fresh exact-head checks are required before merge.
- PR #36 — its 13-commit governance history is already preserved inside the PR #35 candidate through merge commit `cfeb728682c11c0f20eaaf9c63de775ae3d26716`; the standalone PR should be closed as integrated/superseded after the combined candidate reaches `main`.

There is no known open application-runtime feature gap that should be invented merely to create more source activity.

## What is still external release work

The following are **not** completed by repository edits or unsigned hosted compilation:

- production signing keys/certificates/provisioning profiles;
- signed Android AAB/APK installation, upgrade, share-provider, background, notification, and LAN behavior;
- signed Windows MSIX install/update, protocol/app-notification activation, firewall/network/picker/drop behavior;
- Apple Developer provisioning/App Group configuration, signed iOS containing app + Share Extension behavior, real `NSItemProvider`, and signed/notarized Mac Catalyst behavior;
- representative Linux distribution install/run/desktop/protocol-handler behavior for the actual packaged artifacts;
- representative physical cross-device pairing and file/folder/text transfers;
- physical pause/cancel/resume, network switching, low-storage, lifecycle, and target-filesystem behavior;
- real screen-reader, large-text, high-contrast, and Hindi UI/runtime validation;
- exact signed-candidate dependency/license/notice/provenance reconciliation;
- final store/distribution metadata, screenshots, privacy declarations, signing/notarization, submission, and review;
- remote GitHub branch-protection/ruleset enforcement for `main`.

These must be recorded against the exact candidate with the checked-in release-evidence tooling or the relevant external administration/store evidence. They must not be inferred from source code.

## Browser extension boundary

Browser-extension work remains deliberately outside the 2.5.18 stabilization scope. It is a subsequent feature milestone, not a reason to destabilize the current release candidate.

## Current rule

The next repository-side milestone is to finish exact-head automated validation of the integrated PR #35 candidate and merge that validated history into `main` without losing the granular Linux, hardening, or governance histories.

Do **not** create or publish `v2.5.18` merely because the source metadata says 2.5.18. Tagging/release publication belongs after the exact merged candidate and the required signed/manual evidence satisfy the release contract.

from pathlib import Path


def replace_once(path: Path, old: str, new: str, required: bool = True) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        if required:
            raise SystemExit(f"Expected marker not found in {path}: {old}")
        return
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


checklist_path = Path("docs/release/release-checklist.md")
old_check = "- [ ] `dotnet list package --vulnerable` (or current supported equivalent) is reviewed in a connected development environment for every shipped/runtime project and target framework."
new_check = "- [ ] `dotnet package list --project <project> --include-transitive --vulnerable --format json` is run/reviewed for every shipped/runtime project and required target framework, and the release-readiness JSON audit artifacts are retained with the release evidence."
checklist = checklist_path.read_text(encoding="utf-8")
if old_check in checklist:
    checklist_path.write_text(checklist.replace(old_check, new_check, 1), encoding="utf-8")
elif new_check not in checklist:
    raise SystemExit("Release checklist audit-command marker was not found.")

status_path = Path("PROJECT_STATUS.md")
status = status_path.read_text(encoding="utf-8")
status_marker = "## August 14 continuation hardening snapshot"
if status_marker not in status:
    pivot = "Updated: 2026-08-14\n"
    if pivot not in status:
        raise SystemExit("PROJECT_STATUS.md date marker was not found.")
    insert = """

## August 14 continuation hardening snapshot

- Maintained GitHub Actions use `actions/checkout@v7`, `actions/setup-dotnet@v6`, and `github/codeql-action@v4`.
- Repository-wide NuGet auditing is explicitly enabled for direct/transitive dependencies at low-or-higher severity, with warnings-as-errors retaining audit findings as blockers.
- Release readiness emits machine-readable full/vulnerable dependency JSON evidence; portable CI continuously validates the JSON vulnerability-report command.
- The .NET 10 test toolchain uses `Microsoft.NET.Test.Sdk` 18.8.1, `xunit.runner.visualstudio` 3.1.5, and `coverlet.collector` 10.0.1; 511/511 tests passed after the update.
- Platform run `31773145276` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app compilation after the action/audit hardening.
- Signed/package/device/network/accessibility/store validation remains required exactly as described below.
"""
    status_path.write_text(status.replace(pivot, pivot + insert, 1), encoding="utf-8")

changelog_path = Path("CHANGELOG.md")
changelog = changelog_path.read_text(encoding="utf-8")
changelog_marker = "### Workflow/runtime and dependency-audit hardening"
if changelog_marker not in changelog:
    pivot = "## Unreleased - 2026-08-14\n"
    if pivot not in changelog:
        raise SystemExit("CHANGELOG.md Unreleased marker was not found.")
    insert = """

### Workflow/runtime and dependency-audit hardening

- Upgraded maintained GitHub Actions to checkout v7, setup-dotnet v6, and CodeQL v4.
- Made repository-wide direct/transitive NuGet auditing explicit at low-or-higher severity under warnings-as-errors.
- Added machine-readable dependency/vulnerability JSON evidence to release readiness and continuously validate the vulnerable-package JSON command in portable CI.
- Updated the .NET 10 test runner/tooling stack to Microsoft.NET.Test.Sdk 18.8.1, xunit.runner.visualstudio 3.1.5, and coverlet.collector 10.0.1.
- Revalidated 511/511 Core tests, benchmark compilation, CodeQL, security hygiene, and the Android/Windows/Mac Catalyst/iOS Simulator compile matrix after the continuation hardening.
- Synchronized third-party notices, release-audit instructions, and contributor guidance with the maintained `.slnx`, audit, and release-validation gates.
"""
    changelog_path.write_text(changelog.replace(pivot, pivot + insert, 1), encoding="utf-8")

ledger_path = Path("what_changed.md")
ledger = ledger_path.read_text(encoding="utf-8")
ledger_marker = "# 112. August 14 continuation resumed after ledger freeze"
if ledger_marker not in ledger:
    footer = "**Made by the Sanskar**"
    footer_index = ledger.rfind(footer)
    if footer_index < 0:
        raise SystemExit("Expected what_changed.md footer was not found; refusing to alter the ledger.")
    base = ledger[:footer_index].rstrip()
    appendix = r"""

---

# 112. August 14 continuation resumed after ledger freeze

Section 111 recorded the then-intended final repository write. The user explicitly instructed SwiftDrop work to continue, requested complete code, requested maximum practical focused commits, and explicitly required `what_changed.md` to be updated again. This section supersedes only section 111's “final planned write” statement; all earlier ledger history remains preserved.

# 113. Maintained GitHub Actions runtime modernization

All maintained workflows were audited. Deprecated action majors were replaced without changing product behavior:

- Core CI: `actions/checkout@v7`, `actions/setup-dotnet@v6`.
- CodeQL: checkout v7, setup-dotnet v6, `github/codeql-action@v4`.
- Security hygiene: checkout v7.
- Platform matrix: checkout v7/setup-dotnet v6 on Android, Windows, and Apple jobs.
- Release readiness: checkout v7/setup-dotnet v6 on all compile/test jobs.

Focused commits:
- `f5a31f68` — core workflow action runtime refresh.
- `7594cfa5` — CodeQL maintained-major refresh.
- `dc1ef7cb` — security-hygiene checkout refresh.
- `8bcb4e04` — platform workflow action refresh.
- `a2cd424f` — release-readiness action refresh.

Repository searches confirmed no remaining checkout v4, setup-dotnet v4, or CodeQL v3 references in maintained source.

# 114. Explicit direct/transitive NuGet vulnerability policy

Commit `78fc3d68` made the restore policy explicit in `Directory.Build.props`:

- `NuGetAudit=true`
- `NuGetAuditMode=all`
- `NuGetAuditLevel=low`

`TreatWarningsAsErrors=true` remains repository-wide, so low/moderate/high/critical NuGet audit warnings stay verification-blocking unless an intentional reviewed exception is separately documented rather than silently suppressed.

# 115. Machine-readable release dependency and vulnerability evidence

Commit `edb545b3` extended release readiness to emit JSON package evidence for SwiftDrop.Core, portable tests, synthetic benchmarks, and the iOS Share Extension target. For each relevant project, the workflow captures the complete transitive package graph and vulnerable-package view where applicable.

Commit `04cb5a11` additionally exercises the Core vulnerable-package JSON command on every regular CI run and parses the output as JSON, preventing release-only audit command drift. CI run `31773580594` passed this validation.

# 116. Build, release, and contributor documentation synchronization

Commit `2e85b166` documented the enforced NuGet audit boundary, local JSON audit commands, and release-readiness evidence in `BUILDING.md`.

Commit `6526a701` corrected the stale contributor restore target from `SwiftDrop.sln` to the canonical `SwiftDrop.slnx` and aligned contribution guidance with maintained portable verification scripts, platform-specific build commands, dependency-audit review, localization/Apple metadata invariants, CodeQL/repository hygiene, and honest compile-versus-signed-release boundaries.

The release checklist was also moved from the legacy `dotnet list package --vulnerable` spelling to the .NET 10 noun-first `dotnet package list --project <project> --include-transitive --vulnerable --format json` evidence command.

# 117. .NET 10 test-toolchain modernization

Focused test-only dependency commits:

- `551be910` — `xunit.runner.visualstudio` 3.0.2 -> 3.1.5.
- `734f75ce` — `coverlet.collector` 6.0.4 -> 10.0.1.
- `c3b18381` — `Microsoft.NET.Test.Sdk` 17.13.0 -> 18.8.1.

`xunit` remains 2.9.3, preserving the existing test API while modernizing host/runner/coverage tooling. `THIRD_PARTY_NOTICES.md` was synchronized in commit `b2d2506c`. Equivalent Dependabot PRs #10, #3, and #8 were closed after the signed updates were applied and validated on `main`.

# 118. Fresh portable verification after test-tool updates

CI run `31773452371` completed successfully after the combined test-tool modernization.

Exact test result:
- Failed: 0
- Passed: 511
- Skipped: 0
- Total: 511

The same run also completed Core Release build with zero warnings/errors, localization validation, Apple integration metadata validation, and benchmark Release build.

# 119. Fresh CodeQL and security-hygiene evidence

CodeQL run `31773251979` completed successfully using checkout v7, setup-dotnet v6, and CodeQL v4.

Security-hygiene run `31773251972` completed successfully using checkout v7 and retained private signing/local database artifact rejection, embedded private-key block rejection, and required security-document checks.

# 120. Fresh platform evidence after action/audit hardening

Platform build run `31773145276` completed successfully after the explicit NuGet audit/action-runtime hardening.

Verified successful targets:
- Android Release app compile.
- focused Windows Release app compile without MSIX packaging.
- Mac Catalyst containing-app Release compile.
- iOS Simulator Share Extension Release compile.
- iOS Simulator containing-app Release compile.

The Apple simulator builds remain certificate-independent only at CI command scope; real project entitlements remain in source for signed/device builds.

# 121. Current completion boundary after this continuation

Source/hosted verification is current for 511/511 portable tests, benchmark compile, localization and Apple metadata validators, CodeQL v4, repository security hygiene, direct/transitive NuGet audit enforcement, machine-readable vulnerability-report validation, and Android/Windows/Mac Catalyst/iOS Simulator compile coverage.

SwiftDrop must still not be called production-ready until the existing external gates are completed for an exact release candidate: signed Android/Windows/Apple artifacts; physical Android/iOS/device-to-device transfer matrix; Apple App Group provisioning and Share Extension runtime behavior; signed Windows MSIX install/update/protocol/capability behavior; signed Mac Catalyst sandbox/notarization behavior; real-network discovery/resume/firewall/low-storage tests; accessibility/localization checks on actual target devices; final dependency/license/provenance review; and store/privacy metadata/publication checks.

The temporary continuation ledger helper is intentionally removed after a successful write so no stale self-edit workflow or script remains.
"""
    ledger_path.write_text(base + appendix.rstrip() + "\n\n" + footer + "\n", encoding="utf-8")

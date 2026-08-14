from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8")


# README public navigation.
path = "README.md"
text = read(path)
readme_marker = "- Complete documentation index: `docs/README.md`"
if readme_marker not in text:
    pivot = "## Engineering/release documents\n\n"
    if pivot not in text:
        raise SystemExit("README engineering documents marker not found")
    insert = (
        "- Complete documentation index: `docs/README.md`\n"
        "- Installation/source run: `docs/installation.md`\n"
        "- User guide: `docs/user-guide.md`\n"
        "- Settings reference: `docs/configuration.md`\n"
        "- FAQ: `docs/faq.md`\n"
        "- Troubleshooting: `docs/troubleshooting.md`\n"
        "- Networking/firewall guide: `docs/networking.md`\n"
        "- Development guide: `docs/development-guide.md`\n"
        "- Project structure: `docs/architecture/project-structure.md`\n"
        "- CI reference: `docs/testing/ci-reference.md`\n"
        "- Release process: `docs/release/release-process.md`\n"
        "- Versioning/compatibility: `docs/versioning-and-compatibility.md`\n"
        "- Diagnostics/bug reports: `docs/diagnostics-and-bug-reports.md`\n"
    )
    text = text.replace(pivot, pivot + insert, 1)
    write(path, text)


# Project status documentation snapshot.
path = "PROJECT_STATUS.md"
text = read(path)
status_marker = "## August 14 documentation completion snapshot"
if status_marker not in text:
    pivot = "## Implemented in source\n"
    if pivot not in text:
        raise SystemExit("PROJECT_STATUS implemented marker not found")
    insert = """## August 14 documentation completion snapshot

- Added a canonical `docs/README.md` index covering user, developer, architecture, protocol/security, platform, storage, testing, release, support, and legal documentation.
- Added complete user-facing guides for installation/source-run boundaries, pairing/sending/receiving/resume, settings/defaults, networking/firewalls, FAQ, troubleshooting, and privacy-safe diagnostics/bug reports.
- Added developer/repository documentation for project structure, development workflow, CI evidence interpretation, versioning/compatibility, and the end-to-end signed release process.
- Expanded `CONTRIBUTING.md`, `SUPPORT.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, and `TERMS.md` so community, support, security-disclosure, and source-vs-release boundaries are explicit.
- Public README navigation now links the complete documentation surface; documentation maintenance rules identify the canonical owner for user/settings/protocol/storage/platform/testing/release changes.
- The documentation pass does not change the existing production-ready rule: signed/device/network/provider/accessibility/store validation remains required for an exact release candidate.

"""
    text = text.replace(pivot, insert + pivot, 1)
    write(path, text)


# Next-steps roadmap should reflect docs completion instead of treating docs as missing.
path = "NEXT_STEPS.md"
text = read(path)
next_marker = "### Documentation completion through the August 14 continuation"
if next_marker not in text:
    pivot = "### Canonical pairing capability transport\n"
    if pivot not in text:
        raise SystemExit("NEXT_STEPS source-work marker not found")
    insert = """### Documentation completion through the August 14 continuation

- Added a canonical documentation index and complete end-user installation, user workflow, settings, FAQ, networking, troubleshooting, and privacy-safe diagnostic/reporting guides.
- Added repository structure, development workflow, CI-reference, versioning/compatibility, and release-process documentation.
- Expanded support, contribution, community conduct, security-disclosure, and usage-term documents.
- Synchronized public documentation navigation with the maintained iOS-only Share Extension, Mac native-drop architecture, .NET 10 `.slnx` workflow, explicit NuGet auditing, and current hosted compile/test evidence.
- Documentation completeness does not remove the remaining external release tasks below; the next engineering phase remains exact-candidate signing, packaging, physical cross-device/network/provider testing, accessibility/localization validation, dependency/license review, and store/privacy submission checks.

"""
    text = text.replace(pivot, insert + pivot, 1)
    write(path, text)


# Changelog documentation section.
path = "CHANGELOG.md"
text = read(path)
change_marker = "### Complete documentation and contributor/support reference"
if change_marker not in text:
    pivot = "### Workflow/runtime and dependency-audit hardening\n"
    if pivot not in text:
        raise SystemExit("CHANGELOG workflow marker not found")
    insert = """### Complete documentation and contributor/support reference

- Added `docs/README.md` as the canonical documentation index.
- Added installation/source-run, end-user workflow, settings, FAQ, networking/firewall, development, project-structure, CI-reference, release-process, versioning/compatibility, and diagnostics/bug-report guides.
- Expanded troubleshooting across discovery/pairing/firewall/integrity/resume/storage and Android/iOS/Mac/Windows external-intake/build failure cases.
- Expanded contribution, support, community conduct, security disclosure, and usage-term documents while preserving the source-compile versus signed/device/store readiness boundary.
- Public documentation now maps each contract area to a canonical document and records how documentation must stay synchronized with source/tests/release evidence.

"""
    text = text.replace(pivot, insert + pivot, 1)
    write(path, text)


# Detailed engineering ledger, preserving every historical section.
path = "what_changed.md"
text = read(path)
ledger_marker = "# 122. Complete documentation pass requested and executed"
if ledger_marker not in text:
    footer = "**Made by the Sanskar**"
    idx = text.rfind(footer)
    if idx < 0:
        raise SystemExit("what_changed footer not found; refusing to alter history")
    base = text[:idx].rstrip()
    appendix = r"""

---

# 122. Complete documentation pass requested and executed

The user explicitly requested complete project documentation, all repository work pushed to the `main` branch, focused commit messages, and use of `sanskarin@outlook.in` if commit identity configuration was needed. The continuation therefore audited the existing documentation surface against current source/platform/CI state before adding missing canonical guides.

No source feature was invented solely to make documentation appear complete. Documentation continues to distinguish implemented source, portable-tested behavior, hosted-platform compilation, and signed/device/store validation.

# 123. Canonical documentation index

Commit `f275573c` added `docs/README.md` as the canonical documentation navigation point.

Commit `ff4229d1` expanded that index after the rest of the documentation set landed so installation, user workflow, settings, diagnostics, networking, project structure, CI, versioning, and release-process material is discoverable rather than orphaned.

# 124. End-user documentation

Focused commits added:

- `a97a1e70` — complete end-user guide covering discovery, pairing, single/multi/folder/text transfer, approval, resume, Android sharing, iOS Share Extension, Mac native drop, Windows drag/drop/receive folder, queue/history/trust/diagnostics, privacy, and safety boundaries.
- `2a289046` — settings reference derived from the maintained Settings view model/XAML and `AppSettings.Default`, including concurrency 1-8, retention 0-3650, platform notification/receive-folder differences, identity reset, privacy, trust, themes, languages, and developer diagnostics.
- `efbbe97d` — comprehensive FAQ aligned to the maintained local-only protocol, platform targets, iOS-only Share Extension, integrity/resume, settings, CI/release boundaries, and support channels.
- `c8cc520c` — installation/source-run guide that explicitly avoids presenting hosted unsigned compile artifacts as official signed releases.
- `86b66a09` — networking/firewall guide covering mDNS/DNS-SD, bounded UDP fallback, TCP 47821, UDP 47822, guest/client isolation, local address scope, Windows/macOS firewall, Apple local-network privacy, Android multicast behavior, VPNs, IPv4/IPv6, and diagnostic boundaries.
- `b99745c0` — expanded troubleshooting guide covering local discovery/connection, strict pairing, fingerprint mismatch, trust, integrity/resume, collisions/path safety, storage, Android/iOS/Mac/Windows intake, App Group, localization validators, NuGet audit, target builds, and CI-versus-device failures.
- `0a6aa886` — privacy-safe diagnostics and bug-report guide.

# 125. Developer and architecture documentation

Focused commits added:

- `7f6f9d54` — repository/project-structure guide for `SwiftDrop.Core`, `SwiftDrop.App`, iOS-only `SwiftDrop.ShareExtension`, tests, benchmarks, scripts, workflows, docs, resources, platform boundaries, and dependency direction.
- `61d989d7` — development workflow guide covering prerequisites, portable verification, NuGet audit, layer selection, protocol/path/resume/persistence/UI/platform changes, testing levels, CI, commit style, PR expectations, documentation ownership, and definition of done.
- `8bd3d849` — CI/verification reference documenting the five maintained workflows, their exact evidence boundaries, repository-wide NuGet audit policy, local equivalents, candidate discipline, and August 14 verified hosted evidence.
- `091ad581` — versioning/compatibility policy covering application/protocol/schema/trust/batch-resume/platform/settings/dependency/localization compatibility and fail-closed legacy-state handling.
- `da3897fa` — end-to-end release process from exact candidate freeze through automated gates, dependency/license review, signing, signed artifacts, physical matrix, platform provider intake, accessibility/localization, privacy/store review, tagging/submission, and post-release verification.

# 126. Community, support, security, and legal documentation

Focused commits expanded:

- `09ed02a8` — `SUPPORT.md`, linking complete user/developer troubleshooting and safe report guidance.
- `d7d2e4f3` — `CONTRIBUTING.md`, adding security/privacy/layer/dependency/protocol/persistence/platform/test/docs/PR requirements and the requested sign-off format.
- `f524247d` — original SwiftDrop Code of Conduct expanded with expected/unacceptable behavior, security/privacy handling, technical disagreement rules, maintainer responsibilities, reporting, scope, and good-faith enforcement.
- `3192df73` — `SECURITY.md`, removing stale pre-1.0 wording and adding current source/release boundary, private reporting scope, security-sensitive examples, cryptography/endpoint/secret/dependency policies, responsible testing, and regression expectations.
- `3397e85c` — `TERMS.md`, clarifying local-transfer responsibility, authorization, received-file trust, source/unofficial package boundaries, privacy, support diagnostics, third-party services, downstream forks, and Apache-2.0 precedence.

# 127. Documentation source-truth audit

Repository searches during this pass found no remaining indexed `TODO` documentation placeholders, old `469/469` portable-test marker, legacy `dotnet list package` audit spelling, obsolete `SwiftDrop.sln` solution reference, or maintained Mac Catalyst Share Extension wording.

The audit also found `SECURITY.md` still referred to “pre-1.0 development” even though the current project source declares display version `1.0.0`; that stale wording was corrected without falsely claiming that a signed 1.0.0 production release has already passed the release process.

# 128. Documentation ownership map

The completed documentation set now has canonical ownership:

- public overview -> `README.md`;
- navigation -> `docs/README.md`;
- installation/source-run -> `docs/installation.md`;
- end-user workflow -> `docs/user-guide.md`;
- settings -> `docs/configuration.md`;
- FAQ -> `docs/faq.md`;
- troubleshooting -> `docs/troubleshooting.md`;
- safe diagnostics/bug reporting -> `docs/diagnostics-and-bug-reports.md`;
- network/firewall -> `docs/networking.md`;
- build -> `BUILDING.md`;
- development/contribution -> `docs/development-guide.md` + `CONTRIBUTING.md`;
- architecture/project boundaries -> `docs/architecture.md`, `docs/architecture/*`, `DECISIONS.md`;
- protocol/security/compatibility -> `docs/protocol/*`, `docs/security/THREAT_MODEL.md`, `SECURITY.md`;
- platform permissions/status -> `docs/platform-permissions.md`, `docs/platform/integration-status.md`;
- local data/privacy -> `docs/storage/database-schema.md`, `PRIVACY.md`;
- CI/testing -> `docs/testing/*`;
- release/signing/store -> `docs/release/*`;
- version compatibility -> `docs/versioning-and-compatibility.md`;
- support/community/legal -> `SUPPORT.md`, `CODE_OF_CONDUCT.md`, `TERMS.md`, `LICENSE`, `NOTICE`, `THIRD_PARTY_NOTICES.md`;
- current engineering evidence -> `PROJECT_STATUS.md`, `NEXT_STEPS.md`, `CHANGELOG.md`, `what_changed.md`.

# 129. Completion boundary after documentation pass

The repository now contains a complete maintained documentation surface for the implemented source and release process. This does not remove external release gates.

The next required production work remains exact-candidate signed Android/Windows/Apple packaging, Apple App Group provisioning, physical cross-device/network/provider/storage/lifecycle testing, Windows protocol/package/firewall validation, Mac sandbox/notarization validation, accessibility/localization checks, exact dependency/license provenance review, and store/privacy submission checks.

Any source changes made while closing those external gates must update the affected canonical documentation and create a new exact candidate before production readiness is claimed.
"""
    write(path, base + appendix.rstrip() + "\n\n" + footer + "\n")

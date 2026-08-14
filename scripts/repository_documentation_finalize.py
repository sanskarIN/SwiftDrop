from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8")


# Public README: describe the permanent docs gate and glossary.
path = "README.md"
text = read(path)
old = "The verification scripts also validate localization and Apple integration metadata."
new = "The verification scripts also validate documentation integrity, localization, and Apple integration metadata."
if old in text:
    text = text.replace(old, new, 1)
elif new not in text:
    raise SystemExit("README verification sentence marker not found")

bullet = "- documentation integrity validation;\n"
pivot = "- portable Core build/tests;\n"
if bullet not in text:
    if pivot not in text:
        raise SystemExit("README CI bullet marker not found")
    text = text.replace(pivot, bullet + pivot, 1)

glossary = "- Technical glossary: `docs/glossary.md`\n"
pivot = "- Troubleshooting: `docs/troubleshooting.md`\n"
if glossary not in text:
    if pivot not in text:
        raise SystemExit("README troubleshooting docs marker not found")
    text = text.replace(pivot, glossary + pivot, 1)
write(path, text)


# Release checklist: documentation integrity is part of exact-candidate automation.
path = "docs/release/release-checklist.md"
text = read(path)
old = "- [ ] `main`/candidate CI is green for portable restore, build, tests, localization validation, Apple integration metadata validation, benchmark compile, platform compile jobs, CodeQL, repository hygiene, and release-readiness aggregation."
new = "- [ ] `main`/candidate CI is green for documentation integrity, portable restore, build, tests, localization validation, Apple integration metadata validation, benchmark compile, platform compile jobs, CodeQL, repository hygiene, and release-readiness aggregation."
if old in text:
    text = text.replace(old, new, 1)
elif new not in text:
    raise SystemExit("Release checklist candidate CI marker not found")
write(path, text)


# Current project status snapshot.
path = "PROJECT_STATUS.md"
text = read(path)
marker = "## August 14 documentation enforcement and dependency completion"
if marker not in text:
    pivot = "## August 14 documentation completion snapshot\n"
    if pivot not in text:
        raise SystemExit("PROJECT_STATUS documentation snapshot marker not found")
    insert = """## August 14 documentation enforcement and dependency completion

- `scripts/validate_documentation.py` now makes the canonical documentation surface testable: required documents must exist and be nonempty, principal guides must be indexed, checked local Markdown links must resolve, and completed one-time documentation helpers must not remain.
- Documentation validation runs in regular CI and both local `verify-core` entry points; release-readiness uses the same canonical portable verification path.
- CI run `31778543950` proved the new documentation gate together with localization, Apple metadata validation, Core build, 511/511 tests, benchmark compilation, and machine-readable vulnerability auditing; run `31778749428` revalidated the integrated build documentation state.
- Added a technical glossary and aligned pull-request, bug-report, feature-request, and issue-contact routing with the maintained security/privacy/compatibility/release evidence rules.
- `QRCoder` is updated from 1.6.0 to 1.8.0 and `THIRD_PARTY_NOTICES.md` matches the direct dependency version.
- QRCoder update evidence: CI `31778661754`, CodeQL `31778661766`, security hygiene `31778661731`, and platform matrix `31778661776` all succeeded; Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app compilation are green.
- Dependabot PR #9 was closed after the equivalent signed update was applied directly to `main`; no open pull requests or issues remained at the completion check.
- These source/hosted checks do not replace signed Android/Windows/Apple packaging, physical-device/network/provider/accessibility validation, Apple provisioning/notarization, exact release dependency/license review, or store/privacy submission checks.

"""
    text = text.replace(pivot, insert + pivot, 1)
write(path, text)


# Changelog current continuation.
path = "CHANGELOG.md"
text = read(path)
marker = "### Documentation enforcement, community workflow, and dependency completion"
if marker not in text:
    pivot = "### Complete documentation and contributor/support reference\n"
    if pivot not in text:
        raise SystemExit("CHANGELOG documentation section marker not found")
    insert = """### Documentation enforcement, community workflow, and dependency completion

- Added a permanent documentation integrity validator and integrated it into regular CI, Linux/macOS and Windows portable verification, and the canonical release-readiness verification path.
- Added a technical glossary and made it part of the required/indexed documentation contract.
- Strengthened pull-request, bug-report, feature-request, and issue-contact templates around reproducibility, security/privacy, compatibility, dependencies/licenses, accessibility/localization, documentation, and signed-device/manual validation.
- Updated QRCoder 1.6.0 -> 1.8.0, synchronized third-party notices, and revalidated Core CI, CodeQL, security hygiene, Android, Windows, Mac Catalyst, iOS Share Extension, and iOS containing-app hosted compilation.
- Closed the superseded QRCoder Dependabot PR after the signed direct-to-main update passed; the final queue check found no open pull requests or issues.

"""
    text = text.replace(pivot, insert + pivot, 1)
write(path, text)


# Preserve the full engineering history and append the final completion evidence.
path = "what_changed.md"
text = read(path)
marker = "# 130. Permanent documentation integrity gate"
if marker not in text:
    footer = "**Made by the Sanskar**"
    idx = text.rfind(footer)
    if idx < 0:
        raise SystemExit("what_changed footer not found; refusing to rewrite history")
    base = text[:idx].rstrip()
    appendix = r"""

---

# 130. Permanent documentation integrity gate

The documentation completion pass was converted from a one-time editorial exercise into a maintained automated contract.

Focused commits:

- `14b6c980` — added `scripts/validate_documentation.py`.
- `aa4cc015` — regular CI runs documentation integrity before localization/Apple/Core checks.
- `7f13b4a6` — documented the new gate in the CI reference.
- `c838ac52` — Linux/macOS `verify-core.sh` runs the documentation validator.
- `1ffeee38` — Windows PowerShell `verify-core.ps1` runs the documentation validator.
- `efa24732` — `BUILDING.md` documents the validator/local verification contract.
- `08040d5d` — release readiness now uses the canonical portable verification entry point, so release-candidate portable validation includes the same documentation check instead of duplicating drift-prone validator commands.

The validator requires the canonical user/developer/architecture/protocol/platform/storage/testing/release documents, checks that principal guides are indexed, resolves local inline Markdown links/images, requires the public README to link the canonical docs index, and rejects completed one-time documentation helpers that are explicitly forbidden.

CI run `31778543950` completed successfully with the new documentation gate plus localization, Apple integration metadata, Core Release build, 511/511 portable tests, benchmark Release build, and machine-readable vulnerability-audit validation. CI run `31778749428` revalidated the integrated build-documentation state.

# 131. Community contribution and issue workflow alignment

The repository's GitHub community templates were brought to the same standard as the completed documentation set.

Focused commits:

- `b76e26e2` — expanded pull-request template with compatibility, exact verification, security/privacy, dependency/license, accessibility/localization, platform, documentation, and remaining signed-device validation sections.
- `fae0c2ec` — expanded non-security bug report template with exact version/commit, sender/receiver, affected area, network/pairing context, reproducible steps, expected/actual result, sanitized diagnostics, and security-data confirmations.
- `30e860c5` — expanded feature-request template with product problem, platform/area, security/privacy, compatibility, alternatives, and validation requirements.
- `78594321` / `57d9d5ea` — added and refined issue contact routing so documentation, general support, and security disclosure point to the canonical repository policies instead of encouraging unsuitable blank/public issues.

# 132. Technical glossary added to the canonical documentation contract

Commit `4aa85e49` added `docs/glossary.md` covering project-specific terms such as App Group, canonical representation, pairing capability, certificate fingerprint, discovery, external staging, completed-item reuse, receive root, resume metadata, signed/device validation, stable transfer ID, strict JSON, trusted device, and production-ready.

Commit `809cb2b8` linked the glossary from the canonical docs index and documented terminology maintenance ownership.

Commit `7042345b` made the glossary a required/indexed file in `validate_documentation.py` and extended the temporary-helper absence checks used during this finalization sequence.

# 133. QRCoder 1.8.0 dependency completion

The only remaining open Dependabot update at this stage was QRCoder 1.6.0 -> 1.8.0.

Focused commits:

- `9f4d6018` — updated `SwiftDrop.App` to QRCoder 1.8.0.
- `6a9e8b09` — synchronized `THIRD_PARTY_NOTICES.md`.

Verification for the source-changing dependency commit:

- CI run `31778661754` — success.
- CodeQL run `31778661766` — success.
- Security hygiene run `31778661731` — success.
- Platform run `31778661776` — success across Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing app.

Dependabot PR #9 was then closed without merging because the equivalent signed update had already been applied directly to `main`. A repository queue check returned no open pull requests and no open issues.

# 134. Release-readiness verification path simplified

Commit `08040d5d` changed `release-readiness.yml` so its portable job calls `./scripts/verify-core.sh` as the canonical portable source/documentation verification entry point instead of separately repeating localization/Apple validators and then re-running them through the script.

The release workflow still captures machine-readable direct/transitive dependency and vulnerability reports, compiles the synthetic benchmark harness, compiles Android/Windows/Apple target paths, and keeps the explicit final message that signed Windows MSIX, physical-device testing, Apple signing/notarization/App Group provisioning, Share Extension runtime behavior, and store checks remain mandatory.

# 135. Documentation and source completion boundary after this continuation

The repository now has:

- a complete navigable documentation surface for users, contributors, architecture, protocol/security, platform integration, networking, settings, storage/privacy, diagnostics, CI/testing, versioning, signing, release process/checklist, support/community/legal policies, and the detailed engineering ledger;
- automated documentation integrity enforcement in normal/local/release portable verification;
- strengthened GitHub contribution/issue routing templates;
- no open pull requests or issues at the completion check;
- the current QRCoder dependency update validated across the maintained hosted target matrix;
- the existing 511-test portable correctness/security suite and NuGet/CodeQL/security-hygiene gates retained.

This is the end of source/documentation completion work that can be truthfully proven from the repository and hosted CI alone. SwiftDrop must still not be described as production-ready until an exact release candidate passes the already documented external gates: real signing and distribution packaging; physical Android/iOS/device-to-device transfers; Apple App Group and iOS Share Extension runtime validation; Windows MSIX install/update/protocol/firewall validation; Mac Catalyst signed sandbox/notarization validation; real restricted-network/lifecycle/low-storage/provider tests; accessibility/localization checks on actual targets; final dependency/license/provenance review of signed artifacts; and store/privacy publication checks.
"""
    write(path, base + appendix.rstrip() + "\n\n" + footer + "\n")

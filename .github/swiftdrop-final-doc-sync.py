from pathlib import Path
import subprocess


def commit(path: str, message: str) -> None:
    subprocess.run(["git", "add", path], check=True)
    if subprocess.run(["git", "diff", "--cached", "--quiet"]).returncode == 0:
        return
    subprocess.run([
        "git", "commit", "-m", message,
        "-m", "Signed-off-by: Sanskar <sanskarin@outlook.in>"
    ], check=True)


def replace_required(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"Expected documentation fragment not found: {label}")
    return text.replace(old, new)


# README: current test count + duplicate aggregate-export privacy sentence + final regression coverage.
path = Path("README.md")
text = path.read_text(encoding="utf-8")
dup = (
    "The optional performance-trend CSV is derived on demand into app cache, contains aggregate UTC buckets only, "
    "and is shared only after explicit user action. The optional performance-trend CSV is derived on demand into app cache, "
    "contains aggregate UTC buckets only, and is shared only after explicit user action."
)
single = (
    "The optional performance-trend CSV is derived on demand into app cache, contains aggregate UTC buckets only, "
    "and is shared only after explicit user action."
)
text = replace_required(text, dup, single, "README duplicate performance-trend privacy sentence")
text = replace_required(text, "currently covering **559 xUnit tests**", "currently covering **569 xUnit tests**", "README test count")
needle = "- discovery fuzz/truncation/pointer-loop/duplicate metadata;\n"
insert = (
    needle
    + "- mDNS record-RDATA boundary isolation, including rejection of names that would read into a following record;\n"
    + "- exact-expiry behavior for one-time pairing/transfer authorizations and discovered-peer presence;\n"
    + "- concurrent bounded-state admission for rate-limiter peer keys and one-time authorization nonces;\n"
    + "- resume failure paths that reject invalid/missing staged state without creating destination directories or partial files;\n"
    + "- external staging symlink/reparse rejection through the same regular-source safety policy used by direct sends;\n"
)
text = replace_required(text, needle, insert, "README final audit regression list")
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(readme): align final bug-audit contract")

# BUILDING: remove stale helper counts/duplicate footer and describe the expanded release trigger.
path = Path("BUILDING.md")
text = path.read_text(encoding="utf-8")
text = replace_required(text, "The helper suite currently contains **21 Python tests**", "The helper suite currently contains **26 Python tests**", "BUILDING helper count")
text = replace_required(text, "- 16 validation-helper regression tests;", "- 26 validation-helper regression tests;", "BUILDING CI helper bullet")
text = replace_required(
    text,
    "- release-readiness self-validation when its workflow/portable-verifier/audit/evidence/Windows-integration helper inputs change;",
    "- release-readiness validation for source, tests, project/benchmark inputs, workflow changes, and portable/audit/evidence/Windows-integration helpers;",
    "BUILDING release trigger description",
)
old_block = (
    "## Current portable performance-trend contract\n\n"
    "The maintained portable verifier currently runs **26 Python helper tests** and **559 xUnit tests**. "
    "The helper suite includes the aggregate History performance-trend/export contract in addition to documentation, "
    "localization, platform-integration, NuGet evidence, and prior performance-history checks."
)
new_block = (
    "## Current portable final-audit contract\n\n"
    "The maintained portable verifier currently runs **26 Python helper tests** and **569 xUnit tests**. "
    "In addition to the aggregate History performance-trend/export contract, the final regression set covers resume side-effect boundaries, "
    "regular-file staging enforcement, exact one-time credential expiry, bounded concurrent security-state admission, discovery expiry, "
    "and strict mDNS RDATA isolation."
)
if text.count(old_block) != 2:
    raise RuntimeError(f"Expected two duplicate BUILDING footer blocks, found {text.count(old_block)}")
text = text.replace(old_block + "\n\n" + old_block, new_block)
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(build): remove stale final verification text")

# CI reference: current helper/test counts, release trigger coverage, and duplicate footer.
path = Path("docs/testing/ci-reference.md")
text = path.read_text(encoding="utf-8")
text = replace_required(text, "The current helper suite contains **21 tests**.", "The current helper suite contains **26 tests**.", "CI reference helper count")
text = replace_required(text, "The local verification scripts include 21 helper tests", "The local verification scripts include 26 helper tests", "CI reference local helper count")
text = replace_required(text, "- all 16 Python validation helpers pass their regression tests;", "- all 26 Python validation helpers pass their regression tests;", "CI reference green helper count")
old_triggers = (
    "It runs on:\n\n"
    "- manual `workflow_dispatch`;\n"
    "- `v*` tags;\n"
    "- changes to the release workflow or its portable verification/audit/evidence/Windows-integration helper scripts on `main`;\n"
    "- pull requests to `main` that change those release-gate inputs."
)
new_triggers = (
    "It runs on:\n\n"
    "- manual `workflow_dispatch`;\n"
    "- `v*` tags;\n"
    "- production source under `src/**`;\n"
    "- portable tests under `tests/**` and benchmark inputs under `benchmarks/**`;\n"
    "- solution/build inputs (`SwiftDrop.slnx`, `Directory.Build.props`, `global.json`, `NuGet.config`);\n"
    "- changes to the release workflow or its portable verification/audit/evidence/Windows-integration helper scripts on `main`;\n"
    "- pull requests to `main` that change the same candidate-affecting inputs."
)
text = replace_required(text, old_triggers, new_triggers, "CI reference release trigger list")
old_footer = (
    "## Aggregate performance trend/export contract\n\n"
    "Portable validation now includes **26 Python helper tests** and **559 xUnit tests**. `test_performance_trend_export_contract.py` protects UTC aggregation, "
    "aggregate-only invariant CSV schema, the untruncated storage cutoff query, cache/share-sheet export wiring, and English/Hindi UI resource completeness.\n\n"
    "The Core suite additionally covers daily bucketing, resume-safe measured-byte math, UTC offset behavior, out-of-window/invalid sample exclusion, "
    "saturating aggregates, window bounds, deterministic CSV formatting, duplicate/inconsistent bucket rejection, and History store cutoff-query behavior."
)
new_footer = (
    "## Current aggregate and final-hardening contract\n\n"
    "Portable validation now includes **26 Python helper tests** and **569 xUnit tests**. `test_performance_trend_export_contract.py` protects UTC aggregation, "
    "aggregate-only invariant CSV schema, the untruncated storage cutoff query, cache/share-sheet export wiring, and English/Hindi UI resource completeness.\n\n"
    "The Core suite additionally covers daily bucketing/resume-safe measured-byte math plus the final hardening regressions for resume filesystem side effects, "
    "external source-link staging, exact one-time credential/discovery expiry, bounded concurrent security-state admission, and mDNS record-boundary parsing."
)
if text.count(old_footer) != 2:
    raise RuntimeError(f"Expected two duplicate CI footer blocks, found {text.count(old_footer)}")
text = text.replace(old_footer + "\n\n" + old_footer, new_footer)
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(ci): align final release and regression gates")

# Release process: source/test changes now automatically exercise readiness.
path = Path("docs/release/release-process.md")
text = path.read_text(encoding="utf-8")
text = replace_required(
    text,
    "The release-readiness workflow also self-tests when its verification/audit/evidence helper inputs change on `main` or in a pull request. That reduces the chance of discovering a broken release gate only after a version tag is created.",
    "The release-readiness workflow runs for production source, portable tests, benchmark/build inputs, and its verification/audit/evidence helper inputs on `main` and matching pull requests. This prevents normal candidate-affecting source changes from bypassing the aggregate Android/Windows/Apple release gate before a version tag is created.",
    "release-process automatic readiness coverage",
)
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(release): document source-triggered readiness")

# NEXT_STEPS: make the final in-repo completion boundary explicit while retaining external signed/device work.
path = Path("NEXT_STEPS.md")
text = path.read_text(encoding="utf-8")
marker = "Updated: 2026-08-15\n"
section = """

## August 15 final in-repository defect-closure pass

- The final source-level bug/error audit is complete across transfer/resume, external staging, one-time credentials, bounded security state, discovery/mDNS parsing, persistence, platform integration, and release automation.
- Fixed invalid/missing resume-state filesystem side effects, external symlink/reparse staging, exact-expiry inconsistencies, concurrent rate-limiter/authorization capacity races, exact discovery expiry, and cross-record mDNS RDATA reads.
- Release readiness now runs automatically for production source, tests, benchmark/build inputs, and its verification/audit/evidence inputs instead of only a narrow helper-script set.
- Portable regression coverage is **569 xUnit tests** plus **26 Python helper tests**.
- No additional mandatory source feature is intentionally left on the repository roadmap. New source work should be driven by a reproducible defect, dependency/platform change, or deliberately scoped post-v1 feature.
- Production release still requires the signed/package/device/network/provider/accessibility/store evidence listed below; hosted source validation cannot substitute for those external gates.
"""
if section.strip() not in text:
    text = replace_required(text, marker, marker + section, "NEXT_STEPS insertion marker")
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(roadmap): mark final in-repo defect closure")

# PROJECT_STATUS: prepend a current snapshot without rewriting historical evidence.
path = Path("PROJECT_STATUS.md")
text = path.read_text(encoding="utf-8")
section = """

## August 15 final in-repository bug/error audit snapshot

- Final runtime source-changing head: `406c2cfb48c45e04cc34662776e67a68f167745d`; final source/test/release-trigger candidate before documentation synchronization: `6b1544b3a91ecfef2937a909f58a7e9faee31cff`.
- The audit fixed resume filesystem mutation before validation, missing-positive-resume partial creation, external symlink/reparse staging, exact-expiry one-time pairing/authorization behavior, concurrent bounded-state admission races, exact discovered-peer expiry, and mDNS known-record RDATA over-read.
- Release readiness now triggers for `src/**`, `tests/**`, `benchmarks/**`, the canonical solution/build inputs, and existing verification/audit/evidence helpers.
- Portable verification is **569/569 xUnit tests** plus **26/26 Python helper tests**, with documentation/localization/Apple/Windows integration validators, Core/benchmark builds, and machine-readable vulnerability validation retained.
- The final hosted platform/release evidence is recorded in `what_changed.md`; signed Android/Windows/Apple packages, real Apple provisioning/App Group, physical devices/networks/providers/storage, accessibility/localization, exact signed-artifact dependency/license/provenance, and store/privacy submission remain external release gates.
"""
marker = "Updated: 2026-08-15\n"
if section.strip() not in text:
    text = replace_required(text, marker, marker + section, "PROJECT_STATUS insertion marker")
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(status): record final in-repo audit snapshot")

# CHANGELOG: add final bug-fix section at the top of the current Unreleased entry.
path = Path("CHANGELOG.md")
text = path.read_text(encoding="utf-8")
section = """
### Final in-repository bug/error audit

- Reordered receive/resume validation so invalid offsets and missing positive-resume partials fail without creating destination directories or empty staging files.
- Unified external staging with the regular-file source policy so symlink/reparse inputs are rejected consistently.
- Made numeric pairing codes and one-time transfer authorizations expire at the exact declared boundary, matching pairing-link semantics.
- Made rate-limiter key admission and one-time authorization capacity enforcement atomic under concurrent first-seen keys/nonces.
- Expired discovered peers exactly at their configured lifetime boundary.
- Hardened mDNS known-record parsing so PTR/SRV/TXT/A payloads cannot consume bytes beyond their declared RDATA boundaries.
- Expanded portable coverage to **569 xUnit tests** while retaining **26 Python helper tests**.
- Expanded release-readiness path triggers so production source/tests/build inputs automatically exercise the aggregate candidate gate.

"""
marker = "## Unreleased - 2026-08-15\n\n"
if section.strip() not in text:
    text = replace_required(text, marker, marker + section, "CHANGELOG insertion marker")
path.write_text(text, encoding="utf-8")
commit(str(path), "docs(changelog): record final defect fixes")

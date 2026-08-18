from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8")


def replace_current(text: str, old: str, new: str, label: str, *, count: int = 1) -> str:
    old_count = text.count(old)
    new_count = text.count(new)
    if old_count == count:
        return text.replace(old, new, count)
    if old_count == 0 and new_count >= count:
        return text
    raise SystemExit(f"{label}: expected {count} old occurrence(s) or an already-updated value; old={old_count}, new={new_count}")


def insert_after_once(text: str, marker: str, section: str, section_marker: str, label: str) -> str:
    if section_marker in text:
        return text
    if text.count(marker) != 1:
        raise SystemExit(f"{label}: expected one insertion marker, found {text.count(marker)}")
    return text.replace(marker, marker + section, 1)


# README current test contract and coverage description.
path = "README.md"
text = read(path)
old_bullet = "- concurrent bounded-state admission for rate-limiter peer keys and one-time authorization nonces;\n"
new_bullet = old_bullet + "- deterministic seeded reference-model state machines for rate-limiter window/reset/capacity behavior, one-time authorization register/consume/prune/clear behavior, and discovery upsert/expiry/snapshot/clear behavior;\n"
text = replace_current(text, old_bullet, new_bullet, path)
text = replace_current(text, "currently covering **569 xUnit tests**", "currently covering **572 xUnit tests**", path)
write(path, text)

# BUILDING current portable contract.
path = "BUILDING.md"
text = read(path)
text = replace_current(text, "Updated: 2026-08-15", "Updated: 2026-08-18", path)
text = replace_current(text, "**569 xUnit tests**", "**572 xUnit tests**", path)
old_sentence = "The maintained portable verifier currently runs **26 Python helper tests** and **572 xUnit tests**. In addition to the aggregate History performance-trend/export contract, the final regression set covers resume side-effect boundaries, regular-file staging enforcement, exact one-time credential expiry, bounded concurrent security-state admission, discovery expiry, and strict mDNS RDATA isolation."
new_sentence = old_sentence + " The August 18 continuation adds deterministic seeded reference-model state machines for the attempt rate limiter, one-time authorization store, and discovery registry without adding a new test dependency or changing runtime source."
text = replace_current(text, old_sentence, new_sentence, path)
write(path, text)

# NEXT_STEPS current roadmap.
path = "NEXT_STEPS.md"
text = read(path)
text = replace_current(text, "Updated: 2026-08-15", "Updated: 2026-08-18", path)
text = replace_current(text, "Portable regression coverage is **569 xUnit tests** plus **26 Python helper tests**.", "Portable regression coverage is **572 xUnit tests** plus **26 Python helper tests**.", path)
next_section = "\n".join([
    "## August 18 deterministic state-machine hardening",
    "",
    "- Added deterministic reference-model state-machine regression suites for `AttemptRateLimiter`, `OneTimeAuthorizationStore`, and `DiscoveryRegistry`.",
    "- The suites execute **12,000 seeded generated operations** in total: 5,000 rate-limiter operations, 4,000 authorization-store operations, and 3,000 discovery-registry operations.",
    "- The reference models cover expiry boundaries, bounded admission, duplicate rejection, reset/prune/clear behavior, one-time consumption, invalid discovery inputs, trusted-first snapshot ordering, and self-exclusion.",
    "- Portable xUnit coverage is now **572 tests** while the Python helper suite remains **26 tests**.",
    "- Exact test head `898f17a3157ab7af14d7aeb958b315dde1e1c2af` passed normal CI, CodeQL, security hygiene, and the complete release-readiness aggregate including Android, Windows, Mac Catalyst, iOS Share Extension, and iOS containing-app hosted compile/audit jobs.",
    "- This tranche changes test/documentation coverage only; no application/runtime source behavior is changed.",
    "- Signed-package, real-device/provider/network/filesystem, accessibility/localization, exact signed-artifact dependency/license/provenance, Apple provisioning/notarization, Windows packaged activation, and store/privacy validation remain external release work.",
    "",
    "",
])
text = insert_after_once(text, "Updated: 2026-08-18\n\n", next_section, "## August 18 deterministic state-machine hardening", path)
text = replace_current(
    text,
    "- additional property/fuzz/state-machine testing beyond current coverage.",
    "- further property/fuzz/state-machine hardening can continue opportunistically; the August 18 tranche added deterministic reference-model coverage for the attempt rate limiter, one-time authorization store, and discovery registry.",
    path,
)
write(path, text)

# PROJECT_STATUS current snapshot while preserving historical sections below.
path = "PROJECT_STATUS.md"
text = read(path)
text = replace_current(text, "Updated: 2026-08-15", "Updated: 2026-08-18", path)
text = replace_current(text, "Portable verification is **569/569 xUnit tests** plus **26/26 Python helper tests**", "Portable verification is **572/572 xUnit tests** plus **26/26 Python helper tests**", path)
status_section = "\n".join([
    "## August 18 deterministic state-machine hardening snapshot",
    "",
    "- Added three seeded reference-model state-machine regressions covering the bounded attempt rate limiter, one-time authorization store, and discovery registry.",
    "- The combined generated sequence count is **12,000 operations**, with every subject result compared against an intentionally simple reference model after each relevant transition.",
    "- Release-readiness run `32126274097` on test head `898f17a3157ab7af14d7aeb958b315dde1e1c2af` completed successfully, including **572/572 xUnit tests**, **26/26 Python helper tests**, documentation/localization/Apple/Windows integration validation, Core and benchmark Release builds, Android/Windows/Mac Catalyst/iOS Share Extension/iOS containing-app hosted compile-audit jobs, and the final aggregate release gate.",
    "- Normal CI run `32126274113`, CodeQL run `32126274127`, and security-hygiene run `32126274092` also completed successfully for the same test head.",
    "- No runtime application source changed in this tranche; remaining production work is still the signed/package/device/network/provider/accessibility/store evidence documented below.",
    "",
    "",
])
text = insert_after_once(text, "Updated: 2026-08-18\n\n", status_section, "## August 18 deterministic state-machine hardening snapshot", path)
write(path, text)

# CHANGELOG new current entry without rewriting August 15 history.
path = "CHANGELOG.md"
text = read(path)
changelog_section = "\n".join([
    "## Unreleased - 2026-08-18",
    "",
    "### Deterministic state-machine regression hardening",
    "",
    "- Added a 5,000-operation seeded reference-model state machine for `AttemptRateLimiter`, covering sliding-window expiry, bounded first-seen key admission, independent keys, rejection, and reset behavior.",
    "- Added a 4,000-operation seeded reference-model state machine for `OneTimeAuthorizationStore`, covering register/consume/prune/clear transitions, exact expiry, duplicate/capacity rejection, and count invariants.",
    "- Added a 3,000-operation seeded reference-model state machine for `DiscoveryRegistry`, covering valid/invalid upserts, exact expiry, snapshots, exclusion, ordering, and clear behavior.",
    "- Expanded the portable xUnit contract from **569 to 572 tests** without adding a third-party test dependency or changing application/runtime source.",
    "- Exact test head `898f17a3157ab7af14d7aeb958b315dde1e1c2af` passed normal CI, CodeQL, security hygiene, and complete release readiness, including all hosted Android/Windows/Apple compile-audit jobs.",
    "- Synchronized current-state documentation to the 572/26 contract and removed a duplicated aggregate-performance-evidence section plus obsolete notification-era test counts from the release process.",
    "",
    "",
])
text = insert_after_once(text, "# Changelog\n\n", changelog_section, "## Unreleased - 2026-08-18", path)
write(path, text)

# CI reference current contract.
path = "docs/testing/ci-reference.md"
text = read(path)
text = replace_current(text, "Updated: 2026-08-15", "Updated: 2026-08-18", path)
text = replace_current(text, "**569 xUnit tests**", "**572 xUnit tests**", path)
old_ci = "The Core suite additionally covers daily bucketing/resume-safe measured-byte math plus the final hardening regressions for resume filesystem side effects, external source-link staging, exact one-time credential/discovery expiry, bounded concurrent security-state admission, and mDNS record-boundary parsing."
new_ci = old_ci + " It also includes deterministic seeded reference-model state machines for rate-limiter, one-time authorization, and discovery-registry transitions."
text = replace_current(text, old_ci, new_ci, path)
write(path, text)

# Release process current counts and duplicate aggregate section.
path = "docs/release/release-process.md"
text = read(path)
text = replace_current(text, "Updated: 2026-08-15", "Updated: 2026-08-18", path)
text = replace_current(text, "confirm the 16 Python helper tests and 522 xUnit tests pass;", "confirm the 26 Python helper tests and 572 xUnit tests pass;", path)
duplicate = "\n".join([
    "## Aggregate performance evidence",
    "",
    "For a release candidate, treat the local trend CSV as reproducible **device evidence**, not as hosted telemetry. Generate it only from the exact signed candidate while exercising representative devices/networks. Retain the exported aggregate CSV with the candidate test record if project policy permits, and correlate it with the synthetic benchmark harness.",
    "",
    "Before retaining or sharing any trend export, verify its schema is the aggregate-only five-column contract and contains no file/device/path/endpoint/authentication/content data. Hosted compile/test success validates implementation structure but does not substitute for physical measurement or store/privacy review.",
    "",
    "",
])
duplicate_count = text.count(duplicate)
if duplicate_count == 2:
    first = text.find(duplicate)
    second = text.find(duplicate, first + len(duplicate))
    text = text[:second] + text[second + len(duplicate):]
elif duplicate_count != 1:
    raise SystemExit(f"{path}: expected one or two aggregate-performance sections, found {duplicate_count}")
write(path, text)

# Engineering ledger append only; historical content is never rewritten.
path = "what_changed.md"
text = read(path)
ledger_marker = "## 198. Deterministic reference-model state-machine hardening"
if ledger_marker not in text:
    ledger = "\n".join([
        "",
        "",
        "## 198. Deterministic reference-model state-machine hardening",
        "",
        "The next repository-executable post-v1 hardening item was additional property/fuzz/state-machine coverage. This continuation deliberately uses deterministic seeded reference models rather than non-reproducible smoke randomness.",
        "",
        "Focused signed commits:",
        "",
        "- `ab73209a94abcff4553cb1fccbec998672bd691c` — 5,000-operation `AttemptRateLimiter` reference-model state machine;",
        "- `d355d6c526593d1b78439d3168606707d0186247` — 4,000-operation `OneTimeAuthorizationStore` reference-model state machine;",
        "- `898f17a3157ab7af14d7aeb958b315dde1e1c2af` — 3,000-operation `DiscoveryRegistry` reference-model state machine.",
        "",
        "Together the suites execute **12,000 deterministic generated operations**. They compare the production object's externally visible result against an intentionally simple model across expiry, bounded capacity, duplicate/replay rejection, reset/prune/clear transitions, one-time consumption, invalid discovery inputs, trusted-first/name ordering, and snapshot self-exclusion. Fixed seeds keep every failure reproducible.",
        "",
        "No application/runtime source file changes in this tranche; it is regression-only hardening.",
        "",
        "## 199. Exact-head automated evidence and current-contract synchronization",
        "",
        "Exact test head `898f17a3157ab7af14d7aeb958b315dde1e1c2af` passed all maintained PR gates:",
        "",
        "- normal CI run `32126274113`: success;",
        "- CodeQL run `32126274127`: success;",
        "- security-hygiene run `32126274092`: success;",
        "- release-readiness run `32126274097`: success, including the final aggregate release gate.",
        "",
        "Release-readiness portable evidence included **26/26 Python helper tests**, documentation integrity (47 required files and 85 checked local Markdown links), English/Hindi localization validation, Apple/Windows integration metadata validation, Core Release build with zero warnings/errors, **572/572 xUnit tests**, benchmark Release build with zero warnings/errors, and machine-readable vulnerable-package validation with zero findings.",
        "",
        "The same release-readiness run also completed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app hosted compile/audit jobs successfully and retained the configured dependency-audit artifacts.",
        "",
        "Current-state README/build/CI/roadmap/status documentation is synchronized from the prior 569-test contract to **572 xUnit tests** while older historical evidence sections remain unchanged.",
        "",
        "## 200. Release-process documentation defect closure and remaining boundary",
        "",
        "During synchronization, the canonical release process was found to contain the aggregate-performance-evidence section twice and to retain a notification-era instruction requiring only 16 Python helper tests / 522 xUnit tests. The duplicated section was reduced to one canonical copy and the current candidate instruction was aligned to **26 Python helper tests / 572 xUnit tests**.",
        "",
        "The August 18 state-machine tranche advances the optional property/fuzz/state-machine roadmap item but does not claim that adversarial testing is ever permanently exhausted. Further focused property testing remains appropriate when new defects, stateful components, protocol changes, or platform changes justify it.",
        "",
        "The production-readiness boundary is unchanged: signed Android/Windows/Apple packaging, Apple provisioning/App Group/notarization, real device/provider/network/filesystem/lifecycle/low-storage testing, accessibility/localization on actual assistive technologies, exact signed-artifact dependency/license/provenance reconciliation, and final store/privacy publication checks remain external or exact-candidate evidence. Hosted source tests do not substitute for those gates.",
        "",
    ])
    text += ledger
write(path, text)

print("Documentation synchronization completed successfully.")

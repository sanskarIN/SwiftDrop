from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8", newline="\n")


def insert_after_once(text: str, marker: str, insertion: str, *, guard: str) -> str:
    if guard in text:
        return text
    if marker not in text:
        raise SystemExit(f"Required marker not found: {marker!r}")
    return text.replace(marker, marker + insertion, 1)


def insert_before_once(text: str, marker: str, insertion: str, *, guard: str) -> str:
    if guard in text:
        return text
    if marker not in text:
        raise SystemExit(f"Required marker not found: {marker!r}")
    return text.replace(marker, insertion + marker, 1)


# README
path = "README.md"
text = read(path)
text = insert_after_once(
    text,
    "- documentation integrity validation;\n",
    "- Python validation-helper regression tests;\n",
    guard="- Python validation-helper regression tests;",
)
old = "- release-readiness aggregate gates and dependency inventories."
new = """- explicit machine-readable direct/transitive vulnerability-report validation;
- target-specific Android, Windows, Mac Catalyst, iOS app, and iOS Share Extension dependency-audit artifacts;
- deterministic SHA-256 manifests for retained dependency-evidence JSON bundles;
- release-readiness aggregate compile/test/audit gates."""
if old in text:
    text = text.replace(old, new, 1)
elif "- deterministic SHA-256 manifests for retained dependency-evidence JSON bundles;" not in text:
    raise SystemExit("README CI dependency marker not found")
text = insert_after_once(
    text,
    "- Release process: `docs/release/release-process.md`\n",
    "- Dependency evidence: `docs/release/dependency-evidence.md`\n",
    guard="- Dependency evidence: `docs/release/dependency-evidence.md`",
)
old = "The verification scripts also validate documentation integrity, localization, and Apple integration metadata."
new = "The verification scripts run Python helper tests; validate documentation integrity, localization, and Apple integration metadata; compile/test Core and benchmarks; and reject machine-readable Core vulnerability reports containing findings."
if old in text:
    text = text.replace(old, new, 1)
elif new not in text:
    raise SystemExit("README verification summary marker not found")
write(path, text)


# PROJECT_STATUS
path = "PROJECT_STATUS.md"
text = read(path)
section = """## August 14 release-evidence, verifier, and adversarial-test continuation

- Added `scripts/validate_nuget_vulnerability_report.py` plus regression tests so a machine-readable NuGet report is not treated as clean merely because it is valid JSON; non-empty vulnerability collections now fail explicitly.
- Added `scripts/create_dependency_evidence_manifest.py` plus regression tests; audit bundles now contain a deterministic schema-v1 manifest of report paths, exact byte lengths, and SHA-256 digests.
- Normal CI pins Python 3.13, runs 10 Python helper tests, validates documentation/localization/Apple metadata, builds Core, runs the portable xUnit suite, builds benchmarks, and validates the Core vulnerable-package report.
- Local Bash and PowerShell verification run the same helper/documentation/Core/audit gates. A dedicated Windows CI job now executes the PowerShell verifier so Windows-only parser/native-exit behavior is continuously exercised.
- The first Windows-verifier execution exposed a PowerShell parser bug in `$LASTEXITCODE:` interpolation. Commit `080126a0` fixes it by explicitly delimiting the variable; the gate was kept rather than weakened.
- Added deterministic randomized pairing canonicalization and strict-JSON fuzz/property regression tests. CI run `31784196373` passed **516/516** xUnit tests, 10 Python helper tests, documentation/localization/Apple validators, Core/benchmark builds, and a zero-finding machine-readable Core vulnerability audit.
- Platform run `31783405975` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app builds; each target graph produced/validated vulnerable-package JSON and uploaded hashed dependency evidence.
- The retained platform artifacts are `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit`. Their internal manifests were independently recomputed after download; all listed report byte lengths and SHA-256 digests matched. The Apple manifest covers six reports across Mac Catalyst, iOS app, and iOS Share Extension graphs.
- Release-readiness self-test run `31783537853` passed portable verification, Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator app, target dependency-audit uploads, and the final aggregate `release-gate`.
- Release-readiness now also self-tests on `main`/pull-request changes to its verification/audit/evidence helpers while all `v*` tag pushes remain release-candidate triggers.
- Added canonical `docs/release/dependency-evidence.md`; release process/checklist, CI/build documentation, docs index, and third-party notices now define stable JSON output version 1, exact artifact names, vulnerability validation, evidence manifests, and final signed-artifact comparison requirements.
- These improvements strengthen reproducible source/restored-graph evidence. They still do not replace real signing, final package dependency/provenance/license review, physical device/network/provider/accessibility testing, Apple App Group/notarization, signed Windows MSIX behavior, or store/privacy checks.

"""
text = insert_after_once(
    text,
    "Updated: 2026-08-14\n\n",
    section,
    guard="## August 14 release-evidence, verifier, and adversarial-test continuation",
)
text = text.replace("- **511/511 portable tests passed**;", "- **516/516 portable tests passed**;", 1)
write(path, text)


# NEXT_STEPS
path = "NEXT_STEPS.md"
text = read(path)
section = """### Release-evidence and verification automation completed on August 14

- Added strict machine-readable NuGet vulnerable-package report validation with direct/transitive finding detection and helper regression tests.
- Added deterministic SHA-256 dependency-evidence manifests with path confinement, exact byte lengths, stable ordering, and helper regression tests.
- Normal CI pins Python 3.13, runs the Python helper tests, and validates the Core machine-readable vulnerability report.
- Bash and PowerShell portable verification include documentation/localization/Apple/Core/test/benchmark/audit validation; Windows CI executes the PowerShell verifier directly.
- Added target-specific dependency/vulnerability evidence for Android, focused Windows, Mac Catalyst, iOS containing app, and iOS Share Extension.
- Added exact artifact contracts for `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit`.
- Platform run `31783405975` passed all maintained target builds/audits and produced hashed evidence bundles; downloaded Android/Windows/Apple internal manifests were independently verified against their report bytes.
- Release-readiness self-test run `31783537853` passed every portable/platform/audit job and its aggregate gate.
- Added deterministic pairing round-trip/canonical-alias property coverage and strict-JSON randomized robustness/duplicate-case coverage; portable xUnit count is now 516.
- The remaining dependency task is **manual exact-candidate provenance/license/final signed-artifact reconciliation**, not adding another source-level inventory command.

"""
text = insert_after_once(
    text,
    "## Source work completed through the August 14 continuation\n\n",
    section,
    guard="### Release-evidence and verification automation completed on August 14",
)
old = "13. exact dependency inventory artifacts."
new = """13. exact dependency inventory artifacts;
14. zero-finding validation for direct/transitive vulnerable-package JSON;
15. deterministic evidence manifests for portable, Android, Windows, and Apple audit bundles;
16. exact-candidate verification that retained report bytes match their manifest SHA-256 digests."""
if old in text:
    text = text.replace(old, new, 1)
elif "16. exact-candidate verification that retained report bytes match their manifest SHA-256 digests." not in text:
    raise SystemExit("NEXT_STEPS exact-candidate dependency marker not found")
old = """For the exact signed candidate:

- download dependency inventory artifacts from release-readiness;
- inspect Core/App/test/benchmark dependencies;
- inspect the iOS Share Extension dependency graph;
- generate/review final third-party notices from the exact restored graph;
- verify Apache-2.0 project license/NOTICE contents;
- verify no signing/private-key/local-database artifacts entered the repository;
- retain license evidence with release artifacts."""
new = """For the exact signed candidate:

- download `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit` from the exact release-readiness run;
- independently verify each retained bundle's report lengths/SHA-256 digests against `manifest.json`;
- inspect Core/App/test/benchmark and every shipped target graph, including the separate iOS Share Extension graph;
- confirm the machine-readable vulnerable-package reports contain no findings under the configured advisory data;
- manually review package provenance, licenses, notices, redistribution obligations, and platform/runtime components;
- compare hosted restored/simulator/unpackaged evidence with the final signed AAB/APK, MSIX/package, iOS archive/extension, and Mac Catalyst distribution artifacts;
- generate/review final third-party notices from the exact shipped graph;
- verify Apache-2.0 project license/NOTICE contents;
- verify no signing/private-key/local-database artifacts entered the repository;
- retain verified dependency/license evidence with release artifacts."""
if old in text:
    text = text.replace(old, new, 1)
elif "- independently verify each retained bundle's report lengths/SHA-256 digests against `manifest.json`;" not in text:
    raise SystemExit("NEXT_STEPS P1 dependency section marker not found")
write(path, text)


# CHANGELOG
path = "CHANGELOG.md"
text = read(path)
section = """### Release evidence, audit enforcement, and adversarial regression expansion

- Added a reusable NuGet vulnerability-report validator that rejects actual direct/transitive vulnerability findings and malformed report structure instead of treating any valid JSON file as clean evidence.
- Added 10 Python regression tests covering vulnerability-report interpretation and deterministic dependency-evidence manifest generation.
- Added deterministic dependency-evidence manifests containing path, exact byte length, and SHA-256 for retained audit JSON files.
- Platform/release workflows now emit and validate separate dependency evidence for Android, focused Windows, Mac Catalyst, iOS containing app, and iOS Share Extension, using explicit JSON output schema version 1.
- Platform run `31783405975` passed the complete target compile/audit matrix and uploaded hashed Android/Windows/Apple evidence bundles; downloaded bundle manifests were independently verified against the retained report bytes.
- Release-readiness self-test run `31783537853` passed portable, Android, Windows, Mac Catalyst, iOS Share Extension, iOS containing-app, dependency-audit, artifact-upload, and aggregate-gate jobs.
- Normal CI pins Python 3.13 and validates helper scripts; Bash/PowerShell portable verification now includes explicit vulnerability-report validation.
- Added a Windows CI job for the PowerShell portable verifier. Its first run exposed a PowerShell interpolation parser error, fixed in `080126a0`, proving the value of executing the Windows path instead of only reviewing it statically.
- Added deterministic randomized pairing round-trip/canonical-alias and strict-JSON fuzz/duplicate-property invariants; portable xUnit coverage increased from 511 to **516 passing tests** in CI run `31784196373`.
- Added the canonical dependency-evidence reference and synchronized release process/checklist, CI/build docs, docs index, and third-party notices while preserving the signed-artifact/device/store production boundary.

"""
text = insert_after_once(
    text,
    "## Unreleased - 2026-08-14\n\n",
    section,
    guard="### Release evidence, audit enforcement, and adversarial regression expansion",
)
write(path, text)


# what_changed.md — preserve every prior section and append before the single footer.
path = "what_changed.md"
text = read(path)
if "# 136. Machine-readable NuGet vulnerability evidence is enforced" not in text:
    footer = "**Made by the Sanskar**"
    idx = text.rfind(footer)
    if idx < 0:
        raise SystemExit("what_changed footer not found; refusing to truncate history")
    base = text[:idx].rstrip()
    appendix = r"""

---

# 136. Machine-readable NuGet vulnerability evidence is enforced

This continuation replaced a weak evidence assumption — “the JSON command ran and produced parseable JSON” — with explicit finding validation.

Focused commits:

- `562fc0d7` — added `scripts/validate_nuget_vulnerability_report.py`.
- `003151ef` — added vulnerability-report validator regression tests.
- `874238b0` — wired explicit finding validation into normal CI.
- `1940907d` — pinned Python 3.13 for the normal validation gate.

The validator accepts UTF-8/UTF-8-BOM reports, requires a top-level JSON object, recursively examines direct/transitive package structures for non-empty `vulnerabilities` arrays, reports package/version/severity/advisory fields when available, and fails malformed vulnerability shapes. Exit status distinguishes clean reports, reported vulnerabilities, and malformed/report failures.

Machine-readable commands now explicitly request `--format json --output-version 1`; vulnerable views include transitive packages. Repository-wide NuGet restore auditing in `Directory.Build.props` remains a separate warnings-as-errors gate.

# 137. Local portable verification now enforces audit evidence on Bash and PowerShell

Focused commits:

- `b551fa97` — Unix `verify-core.sh` now runs helper tests and validates a temporary Core vulnerable-package report with automatic cleanup.
- `f9d5e80c` — PowerShell verification gained explicit native-command exit checking, helper tests, and Core vulnerable-package report validation.
- `e858fc4a` — normal CI gained a Windows runner job that executes the PowerShell verifier.
- `080126a0` — fixed the PowerShell parser defect exposed by that new Windows job by delimiting `${LASTEXITCODE}` before a colon.

The initial Windows verifier run `31784473076` failed at parse time on the original `$LASTEXITCODE:` string. The gate was retained and the script was fixed rather than weakening/removing the Windows validation path.

# 138. Shipped target dependency graphs are now audited by maintained platform CI

Commit `e364d402` extended `platform-builds.yml` so hosted target jobs generate direct/transitive package JSON plus vulnerable-package JSON for:

- Android `SwiftDrop.App`;
- focused Windows `SwiftDrop.App`;
- Mac Catalyst `SwiftDrop.App`;
- iOS Simulator `SwiftDrop.App`;
- iOS Simulator `SwiftDrop.ShareExtension`.

The target vulnerable-package reports are passed through the same explicit finding validator used by portable verification. This closes the earlier evidence gap where release tooling had stronger portable/extension inventory coverage than ordinary shipped app target graphs.

# 139. Dependency evidence bundles now have deterministic SHA-256 manifests

Focused commits:

- `6e3f8e98` — added `scripts/create_dependency_evidence_manifest.py`.
- `22476296` — added manifest generator regression tests.
- `335686ac` — platform audit bundles now include deterministic manifests.
- `9901c7c0` — release-readiness audit bundles now include deterministic manifests.

Schema version 1 records each evidence JSON file's relative POSIX path, exact byte length, and lowercase SHA-256 digest in stable path order. The generator rejects an output outside the evidence root, excludes the manifest from its own file list, and fails an empty evidence root.

Platform run `31783405975` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app compile/audit jobs. It uploaded:

- `android-dependency-audit`;
- `windows-dependency-audit`;
- `apple-dependency-audit`.

The downloaded Android bundle contained `packages.json`, `vulnerabilities.json`, and `manifest.json`; every listed byte length and SHA-256 was independently recomputed and matched. The Windows bundle passed the same independent check. The Apple bundle contained six report files under `maccatalyst/`, `ios-app/`, and `ios-share-extension/`; all six independently matched its root manifest.

These manifests are integrity aids, not signatures or provenance attestations.

# 140. Release-readiness now self-validates audit/evidence changes

Focused commits:

- `462c4ae3` — extended release readiness with shipped-platform dependency evidence.
- `9901c7c0` — added hashed evidence manifests to release artifacts.
- `b050fcf5` — added main/pull-request self-test triggers for release workflow/verification/audit/evidence helper changes while keeping all `v*` tag pushes as candidate triggers.

Release-readiness self-test run `31783537853` completed successfully. It passed:

- canonical portable verification;
- portable Core/test/benchmark dependency reports and manifest;
- Android compile/audit/upload;
- focused Windows compile/audit/upload;
- Mac Catalyst compile/audit;
- iOS Simulator Share Extension compile;
- iOS Simulator containing-app compile;
- iOS app/extension dependency audits and Apple evidence upload;
- final aggregate `release-gate`.

The aggregate gate still states that signed Windows MSIX, physical-device testing, Apple signing/notarization/App Group provisioning, Share Extension behavior, and store checks remain mandatory.

# 141. Dependency evidence has a canonical release contract

Focused documentation commits:

- `24e39417` — added `docs/release/dependency-evidence.md`.
- `b1d9224e` — linked it from the canonical documentation index.
- `084ec25c` — made the dependency-evidence document required by documentation validation.
- `85ef1535` — expanded the CI reference with helper tests, target audits, manifests, stable JSON schema, local equivalents, and evidence limitations.
- `e33d7b42` — synchronized `BUILDING.md` with the audited portable/target workflows.
- `6017b169` — release checklist now requires all four exact-candidate audit artifacts, manifest verification, and final signed-artifact comparison.
- `d82f4671` — synchronized third-party notices with target audit evidence and final provenance/license obligations.
- `116c56cf` — release process now explicitly retrieves/verifies evidence bundles before manual provenance/license and signed-artifact reconciliation.

The documentation intentionally distinguishes restored/source graph evidence from final signed binary/package evidence.

# 142. Deterministic adversarial pairing and strict-JSON regression coverage expanded

Focused commits:

- `48825620` — added deterministic randomized pairing payload round-trip/canonical re-encoding tests and repeated canonical outer/query alias rejection.
- `dcfb40a2` — added deterministic bounded-byte strict-JSON fuzzing, case-variant duplicate-property generation, and distinct-property strict-validation invariants.

CI run `31784196373` passed:

- 10 Python helper tests;
- 47 required documentation files and 85 checked local Markdown links at that commit;
- localization validation;
- Apple integration metadata validation;
- Core Release build with zero warnings/errors;
- **516/516** xUnit tests with zero failures/skips;
- benchmark Release build with zero warnings/errors;
- Core vulnerable-package report validation with zero findings.

This increases the portable xUnit suite from 511 to 516 tests while keeping the randomized cases deterministic/reproducible and dependency-free.

# 143. Source/release boundary after this continuation

Source-level work now additionally proves:

- machine-readable vulnerable-package findings are explicitly rejected rather than inferred from command success;
- audit helper behavior has its own regression suite;
- target-specific Android/Windows/Mac/iOS app/iOS extension restored graphs have maintained audit evidence;
- retained report bundles have deterministic internal SHA-256 manifests;
- release workflow/audit helper changes self-test before a candidate tag;
- both Bash and Windows PowerShell portable verification paths are executable CI contracts;
- pairing and strict-JSON boundaries have additional deterministic randomized regression coverage.

The remaining P0/P1 work is deliberately external or candidate-specific: production signing and packaging; exact final package/runtime dependency and license/provenance reconciliation; physical cross-device/provider/network/lifecycle/low-storage testing; Apple App Group/provisioning/notarization; Windows signed MSIX install/update/protocol/firewall behavior; accessibility/localization on actual assistive technologies; and final store/privacy submission checks.
"""
    write(path, base + appendix.rstrip() + "\n\n" + footer + "\n")

print("Continuation release-evidence documentation synchronization complete.")

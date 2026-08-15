from pathlib import Path
import subprocess


RUNTIME_HEAD = "9e637b909550ea433bf0c453774d6ab20ba7f605"
CONTRACT_HEAD = "3df4a50836a64655fbf1fb990d0946198f32b52b"
PLATFORM_RUN = "31876069688"
RELEASE_RUN = "31876116068"

PLATFORM_ANDROID = "__PLATFORM_ANDROID__"
PLATFORM_WINDOWS = "__PLATFORM_WINDOWS__"
PLATFORM_APPLE = "__PLATFORM_APPLE__"
RELEASE_PORTABLE = "__RELEASE_PORTABLE__"
RELEASE_ANDROID = "__RELEASE_ANDROID__"
RELEASE_WINDOWS = "__RELEASE_WINDOWS__"
RELEASE_APPLE = "__RELEASE_APPLE__"
FINAL_CI = "__FINAL_CI__"
FINAL_CODEQL = "__FINAL_CODEQL__"
FINAL_SECURITY = "__FINAL_SECURITY__"


def commit(paths: list[str], message: str) -> None:
    subprocess.run(["git", "add", *paths], check=True)
    if subprocess.run(["git", "diff", "--cached", "--quiet"]).returncode == 0:
        return
    subprocess.run([
        "git", "commit", "-m", message,
        "-m", "Signed-off-by: Sanskar <sanskarin@outlook.in>"
    ], check=True)


ledger = Path("what_changed.md")
text = ledger.read_text(encoding="utf-8")
if "## 178." not in text:
    addition = f"""

## 178. Reusable daily performance trend

- Added a portable rolling History performance trend with a default **30-day** window and a guarded maximum of **3650 days**.
- Daily buckets use UTC calendar dates and accept only valid completed measurements; legacy, failed, cancelled, rejected, paused, zero-byte, missing-duration, missing-measured-byte, and otherwise invalid rows do not create fabricated trend points.
- Throughput uses actual attributable `measured_bytes`, not logical file size, so resumed transfers remain mathematically correct.
- Daily throughput is weighted from aggregate measured bytes divided by aggregate measured duration, with saturating counters protecting extreme valid totals from integer overflow.
- The analyzer rejects timestamps after the exact UTC window-end instant, including clock-skewed samples later on the same UTC date.

## 179. Identifier-free trend projection

- Added `TransferPerformanceSample`, containing only `TimestampUtc`, `LogicalSizeBytes`, `DurationMilliseconds`, and `MeasuredBytes`.
- `TransferHistoryStore.GetPerformanceSamplesSinceAsync` projects exactly `timestamp_utc`, `size_bytes`, `duration_ms`, and `measured_bytes` from SQLite.
- The trend query is cutoff-based and intentionally has no `LIMIT`, so it is not truncated by the normal recent-History UI cap.
- SQL eligibility requires completed status, non-negative logical size, bounded positive duration, positive measured bytes, and `measured_bytes <= size_bytes`.
- History row IDs, direction, peer/device names, filenames, paths, endpoints, hashes, transfer identifiers, pairing material, credentials, and content are never materialized into the trend pipeline.
- SQLite remains schema **v6**; no trend/export table or cloud telemetry persistence was added.

## 180. Deterministic aggregate-only CSV export

- Added `TransferPerformanceTrendCsvExporter` with the exact five-column contract: `date_utc,measured_transfers,measured_bytes,measured_duration_ms,weighted_bytes_per_second`.
- Export order, UTC dates, integer values, and floating-point rates use deterministic invariant formatting.
- The exporter accepts aggregate trend points rather than raw History rows and rejects duplicate UTC buckets, non-positive aggregate measurements, and throughput values inconsistent with measured bytes/duration.
- The CSV contains no filename, peer/device name, direction, row ID, path, endpoint, hash, token, nonce, certificate/private key, transferred text/content, or reusable authorization.

## 181. Local History trend preview and export lifecycle

- History now shows a localized rolling 30-day trend and up to the seven most recent measured UTC days, newest first.
- `Export aggregate CSV` is enabled only when measured trend data exists and opens the operating-system share sheet after explicit user action.
- SwiftDrop writes the aggregate CSV to app cache as UTF-8 without BOM and best-effort deletes older matching SwiftDrop trend exports before creating a new one.
- Clearing History and configuring zero-day History retention also best-effort remove SwiftDrop-owned cached trend exports, so derived local export files follow the History privacy lifecycle.
- The export path performs no HTTP request, analytics upload, background telemetry, account synchronization, or remote persistence.

## 182. Localization, documentation, and roadmap synchronization

- Added English and Hindi strings for trend title/description, no-measurement guidance, recent-day preview lines, aggregate export action, share title, and export failure handling with localization key/placeholder parity.
- Updated `README.md`, `PRIVACY.md`, `NEXT_STEPS.md`, `PROJECT_STATUS.md`, `CHANGELOG.md`, `BUILDING.md`, `docs/architecture.md`, `docs/user-guide.md`, `docs/storage/database-schema.md`, `docs/testing/performance-benchmarks.md`, `docs/testing/manual-test-matrix.md`, `docs/testing/security-test-plan.md`, `docs/testing/ci-reference.md`, `docs/release/release-checklist.md`, and `docs/release/release-process.md`.
- Removed a duplicated/stale architecture trend section and replaced the obsolete full-History-row query reference with the identifier-free projection contract.
- The source-level aggregate trend/export P2 item is complete; representative-device/cross-network evidence and synthetic-vs-real benchmark correlation remain real external performance-validation work.

## 183. Regression coverage and defect closure

- Added Core tests for UTC grouping, offset handling, resumed measured-byte math, exact end-instant exclusion, invalid/out-of-window sample exclusion, saturation, window bounds, deterministic CSV formatting, aggregate privacy columns, duplicate/inconsistent/non-positive point rejection, and identifier-free storage cutoff behavior.
- Added permanent `scripts/tests/test_performance_trend_export_contract.py` covering identifier-free aggregation, exact UTC end filtering, aggregate-only CSV, untruncated numeric-only SQL projection, cache/History-clear lifecycle, absence of HTTP/telemetry wiring, OS share-sheet integration, and English/Hindi UI resources.
- The first verifier caught a test-only named-parameter casing error (`integrityVerified` vs `IntegrityVerified`); it was fixed rather than weakening the verifier.
- A later review found and fixed future same-day clock-skew inclusion, and a privacy review replaced full History-row materialization with the numeric/timestamp-only `TransferPerformanceSample` projection.
- Portable coverage is now **26/26 Python helper tests** and **559/559 xUnit tests**.

## 184. Exact final runtime platform evidence

- Exact final application/runtime source head: `{RUNTIME_HEAD}`.
- Maintained platform run **{PLATFORM_RUN}** completed successfully across Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing app, including target dependency vulnerability audits and evidence uploads.
- Platform dependency-evidence artifact digests recorded by GitHub:
  - `android-dependency-audit`: `{PLATFORM_ANDROID}`
  - `windows-dependency-audit`: `{PLATFORM_WINDOWS}`
  - `apple-dependency-audit`: `{PLATFORM_APPLE}`
- No application/runtime source file changed after `{RUNTIME_HEAD}`; later commits in this continuation are regression-contract, documentation, ledger, and temporary-helper cleanup only.

## 185. Release-readiness and cleaned-branch evidence

- Final source+portable-contract candidate: `{CONTRACT_HEAD}`.
- Release-readiness run **{RELEASE_RUN}** completed successfully across Core/tests, Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator containing app, dependency audits/uploads, and final `release-gate`.
- Its portable contract passed **26/26 Python helper tests**, **559/559 xUnit tests**, documentation/localization/Apple/Windows integration validation, Core/benchmark compilation, and zero vulnerable-package findings in maintained portable reports.
- Release-readiness artifact digests recorded by GitHub:
  - `dependency-audit`: `{RELEASE_PORTABLE}`
  - `android-dependency-audit`: `{RELEASE_ANDROID}`
  - `windows-dependency-audit`: `{RELEASE_WINDOWS}`
  - `apple-dependency-audit`: `{RELEASE_APPLE}`
- Final cleaned-main CI run **{FINAL_CI}**, CodeQL run **{FINAL_CODEQL}**, and security-hygiene run **{FINAL_SECURITY}** are the post-ledger/helper-removal branch evidence.
- Production readiness still requires signed-package, physical-device/provider/network/filesystem/accessibility/localization, representative-device performance correlation, exact signed-artifact dependency/license/provenance, Apple provisioning/notarization, Windows signed MSIX activation, and store/privacy submission validation. Hosted CI does not prove those external gates.
"""
    ledger.write_text(text.rstrip() + addition + "\n", encoding="utf-8")

status = Path("PROJECT_STATUS.md")
s = status.read_text(encoding="utf-8")
marker = "## August 15 aggregate performance-trend export"
if marker in s and "Exact runtime platform evidence for this continuation" not in s:
    start = s.index(marker)
    next_header = s.find("\n## ", start + len(marker))
    if next_header == -1:
        next_header = len(s)
    block = s[start:next_header].rstrip()
    evidence = f"""

- Exact runtime platform evidence for this continuation: run `{PLATFORM_RUN}` on runtime head `{RUNTIME_HEAD}`.
- Release-readiness evidence: run `{RELEASE_RUN}` on source+contract head `{CONTRACT_HEAD}`, with **26/26 Python helper tests** and **559/559 xUnit tests** plus Android/Windows/Apple compile/audit gates and final release gate.
- Cleaned-branch evidence after ledger/helper removal: CI `{FINAL_CI}`, CodeQL `{FINAL_CODEQL}`, security hygiene `{FINAL_SECURITY}`.
"""
    s = s[:start] + block + evidence + s[next_header:]
    status.write_text(s, encoding="utf-8")

commit(["what_changed.md", "PROJECT_STATUS.md"], "docs(status): finalize aggregate performance trend evidence")
subprocess.run(["git", "push", "origin", "HEAD:main"], check=True)

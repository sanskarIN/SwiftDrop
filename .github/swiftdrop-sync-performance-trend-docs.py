from pathlib import Path
import subprocess


def commit_file(path: str, message: str) -> None:
    subprocess.run(["git", "add", path], check=True)
    diff = subprocess.run(["git", "diff", "--cached", "--quiet"])
    if diff.returncode == 0:
        return
    subprocess.run([
        "git", "commit", "-m", message,
        "-m", "Signed-off-by: Sanskar <sanskarin@outlook.in>"
    ], check=True)


def append_section(path: str, marker: str, title: str, body: str) -> None:
    p = Path(path)
    text = p.read_text(encoding="utf-8")
    if marker in text:
        return
    section = f"\n\n## {title}\n\n{body.strip()}\n"
    p.write_text(text.rstrip() + section, encoding="utf-8")


p = Path("README.md")
text = p.read_text(encoding="utf-8")
old = "- Local History performance dashboard with measured completed-transfer duration, resume-safe actual-byte throughput, and weighted average throughput; legacy/unmeasured rows are never given invented rates."
new = old + "\n- Local 30-day UTC performance trend derived from completed measured History samples, with an explicit aggregate-only CSV export through the OS share sheet; the export contains UTC date, measured-count, measured-byte, measured-duration, and weighted-rate columns only."
if old in text and "aggregate-only CSV export through the OS share sheet" not in text:
    text = text.replace(old, new, 1)
text = text.replace(
    "Numeric performance metadata follows the same local history-retention policy and contains no peer endpoint, transfer content, credential, or reusable authorization.",
    "Numeric performance metadata follows the same local history-retention policy and contains no peer endpoint, transfer content, credential, or reusable authorization. The optional performance-trend CSV is derived on demand into app cache, contains aggregate UTC buckets only, and is shared only after explicit user action."
)
text = text.replace(
    "- **16 Python validation-helper regression tests**, including NuGet evidence helpers and the Windows packaged-notification integration validator;",
    "- **26 Python validation-helper regression tests**, including NuGet evidence helpers, packaged-integration validators, performance-history measurement contracts, and aggregate performance-trend export contracts;"
)
text = text.replace(
    "- two-OS portable verification on Ubuntu and Windows PowerShell, currently covering **522 xUnit tests**;",
    "- two-OS portable verification on Ubuntu and Windows PowerShell, currently covering **559 xUnit tests**;"
)
p.write_text(text, encoding="utf-8")
commit_file("README.md", "docs(readme): document aggregate performance trend export")

append_section(
    "PRIVACY.md",
    "### Aggregate performance-trend export (August 15)",
    "Aggregate performance-trend export (August 15)",
    """
The History screen can derive a rolling 30-day performance trend from already-retained, valid completed-transfer measurements. Trend calculation uses UTC calendar dates, actual attributable measured bytes, measured elapsed duration, measured transfer count, and weighted throughput.

The optional CSV is created only after an explicit export action, written to app cache, and handed to the operating-system share sheet. Before creating a new trend file SwiftDrop best-effort removes older matching trend exports from its cache.

The CSV schema is deliberately aggregate-only: `date_utc`, `measured_transfers`, `measured_bytes`, `measured_duration_ms`, and `weighted_bytes_per_second`. It does not contain history row IDs, direction, filenames, peer/device names, source/destination paths, endpoints/IPs/ports, hashes, transfer IDs, pairing invitations/capabilities/nonces, tokens/session credentials, certificates/private keys, or transferred text/content.

This feature adds no telemetry upload, analytics endpoint, cloud account, or new SQLite table. It derives data from the existing local History retention boundary; clearing/pruning History removes the source measurements for future trend generation.
"""
)
commit_file("PRIVACY.md", "docs(privacy): define aggregate trend export boundary")

p = Path("NEXT_STEPS.md")
text = p.read_text(encoding="utf-8")
if "## August 15 aggregate performance-trend export continuation" not in text:
    anchor = "Updated: 2026-08-15"
    addition = """

## August 15 aggregate performance-trend export continuation

- The source-level History trend/export P2 item is complete: SwiftDrop derives rolling 30-day UTC buckets from valid completed measurements and exports a deterministic aggregate-only CSV on explicit user action.
- The dedicated storage query is cutoff-based and untruncated, so trend generation is not limited by the normal recent-History UI cap.
- Resume-safe `measured_bytes` remains the rate numerator; logical file size is never substituted for bytes actually transferred during a resumed interval.
- Export is local/cache-based, best-effort cleans older matching cache files, and contains only UTC date, measured transfer count, measured bytes, measured duration, and weighted bytes/second.
- The remaining performance P2 work is **representative-device/cross-network evidence and synthetic-vs-real benchmark correlation using this aggregate export**, not additional source telemetry or cloud collection.
"""
    if anchor in text:
        text = text.replace(anchor, anchor + addition, 1)
    else:
        text = text.rstrip() + addition + "\n"
text = text.replace(
    "representative-device performance trend capture/export and synthetic-vs-real benchmark correlation using the implemented local History measurements;",
    "representative-device/cross-network trend evidence and synthetic-vs-real benchmark correlation using the implemented aggregate local History trend export;"
)
p.write_text(text, encoding="utf-8")
commit_file("NEXT_STEPS.md", "docs(roadmap): mark aggregate trend export source work complete")

append_section(
    "PROJECT_STATUS.md",
    "## August 15 aggregate performance-trend export",
    "August 15 aggregate performance-trend export",
    """
SwiftDrop now derives a rolling 30-day UTC performance trend from valid completed History measurements and can export it as a deterministic aggregate-only CSV through the operating-system share sheet. The query path is cutoff-based rather than UI-limit based, so all retained valid measurements in the selected window can contribute.

The export contains only UTC date, measured transfer count, measured bytes, measured duration, and weighted bytes/second. It does not expose file/device/path/network/authentication/content fields and introduces no new database schema or remote telemetry.

Portable coverage for the corrected source is **559/559 xUnit tests** plus **26/26 Python helper tests**, including a permanent cross-layer trend/export contract. Exact final platform/release run IDs are recorded in `what_changed.md` after hosted jobs complete. Representative-device and cross-network benchmark correlation remains external evidence.
"""
)
commit_file("PROJECT_STATUS.md", "docs(status): record aggregate performance trend source state")

append_section(
    "CHANGELOG.md",
    "## Aggregate History performance trend and CSV export",
    "Aggregate History performance trend and CSV export",
    """
- Added rolling 30-day UTC performance trend aggregation based only on valid completed History measurements.
- Added an untruncated cutoff query for retained performance samples.
- Added deterministic aggregate CSV export with invariant UTC/date/rate formatting.
- Added explicit OS share-sheet export from History and best-effort cleanup of prior matching cache exports.
- Added English/Hindi trend/export UI strings and a seven-day recent measured-bucket preview.
- Added aggregate-only privacy guarantees and cross-layer regression coverage.
- Portable regression coverage is now 559 xUnit tests and 26 Python helper tests.
"""
)
commit_file("CHANGELOG.md", "docs(changelog): record aggregate trend export")

append_section(
    "docs/architecture.md",
    "### History performance trend derivation and export",
    "History performance trend derivation and export",
    """
The performance trend is a derived read model, not a new persistence model. `TransferHistoryStore.GetPerformanceEntriesSinceAsync` selects all retained valid completed measurements at/after a UTC cutoff without the normal recent-row UI limit. `TransferPerformanceTrendAnalyzer` groups these records by UTC calendar date and uses actual `measured_bytes` plus `duration_ms` to compute weighted daily throughput.

`TransferPerformanceTrendCsvExporter` serializes only aggregate date/count/byte/duration/rate fields with invariant formatting. `TransferHistoryService` writes the derived CSV to app cache on explicit request, best-effort deletes older matching cached exports, and `HistoryPage` hands the file to the OS share sheet. No new SQLite table, cloud telemetry path, peer endpoint, row identifier, file/device metadata, or reusable authorization is introduced.
"""
)
commit_file("docs/architecture.md", "docs(architecture): describe aggregate trend derivation")

append_section(
    "docs/user-guide.md",
    "### Export the local performance trend",
    "Export the local performance trend",
    """
Open **History** to view the rolling 30-day performance trend. SwiftDrop shows up to the seven most recent UTC days that have valid measured completed transfers. A day is omitted when it has no valid measured sample; SwiftDrop does not invent a zero-speed sample for legacy, failed, cancelled, rejected, paused, zero-byte, or otherwise unmeasured operations.

When measured trend data exists, choose **Export aggregate CSV**. SwiftDrop creates a local cache file and opens the operating-system share sheet. The CSV contains only aggregate UTC date/count/bytes/duration/weighted-rate fields and intentionally excludes filenames, peer/device names, directions, paths, addresses, transfer content, pairing material, and credentials.

The export reflects retained History. History retention, pruning, clearing, and privacy behavior therefore remain the source-of-truth controls; the export does not enable background or cloud analytics.
"""
)
commit_file("docs/user-guide.md", "docs(user): explain performance trend export")

append_section(
    "docs/storage/database-schema.md",
    "### Derived performance trend (no schema change)",
    "Derived performance trend (no schema change)",
    """
Schema version remains **v6**. Performance trends are calculated on demand from valid `transfer_history.duration_ms` and `transfer_history.measured_bytes` values already introduced by v5/v6; no trend table or export table is persisted.

The cutoff query selects retained completed rows with positive bounded duration, positive measured bytes, and `measured_bytes <= size_bytes`, then the Core analyzer groups them into UTC daily buckets. CSV files are derived cache artifacts created only by an explicit export action and are not authoritative database state.
"""
)
commit_file("docs/storage/database-schema.md", "docs(storage): document derived trend without schema change")

append_section(
    "docs/testing/performance-benchmarks.md",
    "### Local trend/export evidence",
    "Local trend/export evidence",
    """
The local History trend is an observational aid, not a benchmark guarantee. It groups valid completed measurements by UTC date and computes weighted throughput from actual measured bytes divided by measured elapsed time. Resumed transfers contribute only bytes transferred after the negotiated resume offset.

Automated tests verify UTC bucketing, window boundaries, resumed-byte attribution, saturation behavior, invariant CSV output, aggregate-only columns, duplicate/inconsistent bucket rejection, and the untruncated cutoff query.

For release/post-v1 performance claims, collect representative-device and representative-network samples, export the aggregate trend, and correlate those results with the synthetic benchmark harness. Do not present hosted CI or the local trend as proof of universal transfer speed.
"""
)
commit_file("docs/testing/performance-benchmarks.md", "docs(performance): add trend export evidence workflow")

append_section(
    "docs/testing/manual-test-matrix.md",
    "### Performance trend and export",
    "Performance trend and export",
    """
- Complete several measured transfers across at least two UTC dates; verify History groups measured days correctly and shows newest measured days first in the preview.
- Resume a partially transferred file; verify the daily rate reflects only post-resume transferred bytes rather than the full logical file size.
- Include legacy/unmeasured, failed, cancelled, rejected, paused, and zero-byte History records; verify they do not create fabricated trend buckets.
- Export the 30-day aggregate CSV; verify the OS share sheet opens only after explicit action and the file contains exactly the documented aggregate columns.
- Inspect the CSV for absence of row IDs, direction, filenames, peer/device names, paths, endpoints/IPs/ports, hashes, transfer IDs, pairing material, credentials, certificates/private keys, and transferred content.
- Export twice and verify a new file can be shared while prior matching app-cache exports are best-effort cleaned.
- Clear/prune History and verify future trend generation reflects the remaining retained measurements.
- Repeat in English/Hindi, larger-interface mode, keyboard/screen-reader navigation, and high text scaling.
"""
)
commit_file("docs/testing/manual-test-matrix.md", "docs(testing): add performance trend export matrix")

append_section(
    "docs/testing/security-test-plan.md",
    "### Aggregate performance export privacy checks",
    "Aggregate performance export privacy checks",
    """
Verify the trend CSV header is exactly `date_utc,measured_transfers,measured_bytes,measured_duration_ms,weighted_bytes_per_second` and that generated rows contain only those aggregate values.

Seed History with conspicuous filenames, peer names, paths, transfer IDs, endpoint-like strings, and unrelated failed/unmeasured rows. Export the trend and assert none of those identifiers appear. Confirm trend generation has no network request/telemetry dependency, creates no new SQLite table, and uses only the existing History retention source.

Verify malformed or impossible in-memory trend points are rejected by the CSV exporter, duplicate UTC buckets are rejected, and the storage query excludes non-completed/nonpositive/out-of-bound measurement rows.
"""
)
commit_file("docs/testing/security-test-plan.md", "docs(security): add aggregate trend export leak checks")

append_section(
    "docs/testing/ci-reference.md",
    "### Aggregate performance trend/export contract",
    "Aggregate performance trend/export contract",
    """
Portable validation now includes **26 Python helper tests** and **559 xUnit tests**. `test_performance_trend_export_contract.py` protects UTC aggregation, aggregate-only invariant CSV schema, the untruncated storage cutoff query, cache/share-sheet export wiring, and English/Hindi UI resource completeness.

The Core suite additionally covers daily bucketing, resume-safe measured-byte math, UTC offset behavior, out-of-window/invalid sample exclusion, saturating aggregates, window bounds, deterministic CSV formatting, duplicate/inconsistent bucket rejection, and History store cutoff-query behavior.
"""
)
commit_file("docs/testing/ci-reference.md", "docs(ci): record performance trend export contract")

append_section(
    "docs/release/release-checklist.md",
    "### Performance trend/export candidate checks",
    "Performance trend/export candidate checks",
    """
- [ ] Confirm the exact candidate passes the 26-helper/559-xUnit portable contract and target compile/audit matrix.
- [ ] Perform full/resumed measured transfers on representative physical devices and verify UTC daily trend math.
- [ ] Export the aggregate CSV and verify exact documented columns plus absence of file/device/path/network/auth/content identifiers.
- [ ] Verify OS share-sheet behavior, cancellation, repeated export/cache cleanup, History clear/prune behavior, English/Hindi presentation, large text, keyboard, and screen-reader access.
- [ ] Correlate representative-device/network trend evidence with synthetic benchmark results before making performance claims.
"""
)
commit_file("docs/release/release-checklist.md", "docs(release): add performance trend candidate checks")

append_section(
    "docs/release/release-process.md",
    "### Aggregate performance evidence",
    "Aggregate performance evidence",
    """
For a release candidate, treat the local trend CSV as reproducible **device evidence**, not as hosted telemetry. Generate it only from the exact signed candidate while exercising representative devices/networks. Retain the exported aggregate CSV with the candidate test record if project policy permits, and correlate it with the synthetic benchmark harness.

Before retaining or sharing any trend export, verify its schema is the aggregate-only five-column contract and contains no file/device/path/endpoint/authentication/content data. Hosted compile/test success validates implementation structure but does not substitute for physical measurement or store/privacy review.
"""
)
commit_file("docs/release/release-process.md", "docs(release): define aggregate performance evidence")

append_section(
    "BUILDING.md",
    "### Current portable performance-trend contract",
    "Current portable performance-trend contract",
    """
The maintained portable verifier currently runs **26 Python helper tests** and **559 xUnit tests**. The helper suite includes the aggregate History performance-trend/export contract in addition to documentation, localization, platform-integration, NuGet evidence, and prior performance-history checks.
"""
)
commit_file("BUILDING.md", "docs(build): update portable trend test contract")

subprocess.run(["git", "push", "origin", "HEAD:main"], check=True)

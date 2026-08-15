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


architecture = Path("docs/architecture.md")
text = architecture.read_text(encoding="utf-8")
header = "## History performance trend derivation and export"
first = text.find(header)
if first < 0:
    raise SystemExit("architecture trend section missing")
text = text[:first].rstrip() + """


## History performance trend derivation and export

The performance trend is a derived read model, not a new persistence model. `TransferHistoryStore.GetPerformanceSamplesSinceAsync` selects all retained valid completed measurements at/after a UTC cutoff without the normal recent-row UI limit, but its SQL projection contains only `timestamp_utc`, `size_bytes`, `duration_ms`, and `measured_bytes`. History row IDs, direction, peer/device names, filenames, paths, endpoints, and authorization data are therefore never materialized into the trend pipeline.

`TransferPerformanceSample` is the identifier-free Core handoff model. `TransferPerformanceTrendAnalyzer` groups valid samples by UTC calendar date, excludes samples later than the exact UTC window end even when they share that calendar date, and uses actual `measured_bytes` plus `duration_ms` to compute weighted daily throughput.

`TransferPerformanceTrendCsvExporter` serializes only aggregate date/count/byte/duration/rate fields with invariant formatting. `TransferHistoryService` writes the derived CSV to app cache on explicit request, best-effort deletes older matching cached exports, and `HistoryPage` hands the file to the OS share sheet. Clearing History or configuring zero-day History retention also best-effort removes SwiftDrop-owned cached trend exports. No new SQLite table, cloud telemetry path, peer endpoint, row identifier, file/device metadata, or reusable authorization is introduced.
"""
architecture.write_text(text, encoding="utf-8")
commit("docs/architecture.md", "docs(architecture): enforce identifier-free trend boundary")

privacy = Path("PRIVACY.md")
text = privacy.read_text(encoding="utf-8")
needle = "The History screen can derive a rolling 30-day performance trend from already-retained, valid completed-transfer measurements. Trend calculation uses UTC calendar dates, actual attributable measured bytes, measured elapsed duration, measured transfer count, and weighted throughput."
replacement = needle + " The dedicated SQLite trend query projects only timestamp, logical size, measured duration, and measured bytes into an identifier-free Core sample; it does not materialize History row IDs, direction, peer/device names, filenames, or paths into the trend pipeline."
if needle in text and "identifier-free Core sample" not in text:
    text = text.replace(needle, replacement, 1)
clear_needle = "Before creating a new trend file SwiftDrop best-effort removes older matching trend exports from its cache."
clear_replacement = clear_needle + " Clearing History or setting History retention to zero also best-effort removes SwiftDrop-owned matching trend exports from app cache."
if clear_needle in text and "setting History retention to zero" not in text:
    text = text.replace(clear_needle, clear_replacement, 1)
privacy.write_text(text, encoding="utf-8")
commit("PRIVACY.md", "docs(privacy): harden identifier-free trend lifecycle")

storage = Path("docs/storage/database-schema.md")
text = storage.read_text(encoding="utf-8")
needle = "The cutoff query selects retained completed rows with positive bounded duration, positive measured bytes, and `measured_bytes <= size_bytes`, then the Core analyzer groups them into UTC daily buckets."
replacement = "The cutoff query selects retained completed rows with positive bounded duration, positive measured bytes, and `measured_bytes <= size_bytes`, while projecting only `timestamp_utc`, `size_bytes`, `duration_ms`, and `measured_bytes` into an identifier-free performance sample. The Core analyzer then groups those samples into UTC daily buckets and excludes timestamps later than the exact window end."
if needle in text:
    text = text.replace(needle, replacement, 1)
storage.write_text(text, encoding="utf-8")
commit("docs/storage/database-schema.md", "docs(storage): document identifier-free trend projection")

security = Path("docs/testing/security-test-plan.md")
text = security.read_text(encoding="utf-8")
needle = "Verify malformed or impossible in-memory trend points are rejected by the CSV exporter, duplicate UTC buckets are rejected, and the storage query excludes non-completed/nonpositive/out-of-bound measurement rows."
replacement = needle + " Also verify the storage trend SELECT projects only `timestamp_utc`, `size_bytes`, `duration_ms`, and `measured_bytes`, with no History ID, direction, peer/device, filename, or path column."
if needle in text and "storage trend SELECT projects only" not in text:
    text = text.replace(needle, replacement, 1)
security.write_text(text, encoding="utf-8")
commit("docs/testing/security-test-plan.md", "docs(security): require identifier-free trend query")

status = Path("PROJECT_STATUS.md")
text = status.read_text(encoding="utf-8")
needle = "The export contains only UTC date, measured transfer count, measured bytes, measured duration, and weighted bytes/second. It does not expose file/device/path/network/authentication/content fields and introduces no new database schema or remote telemetry."
replacement = needle + " The trend storage query itself is identifier-free and projects only timestamp/size/duration/measured-byte fields; exact-window filtering also excludes clock-skewed samples later than the requested UTC end instant."
if needle in text and "trend storage query itself is identifier-free" not in text:
    text = text.replace(needle, replacement, 1)
status.write_text(text, encoding="utf-8")
commit("PROJECT_STATUS.md", "docs(status): record identifier-free trend projection")

subprocess.run(["git", "push", "origin", "HEAD:main"], check=True)

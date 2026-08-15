import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


class PerformanceTrendExportContractTests(unittest.TestCase):
    def read(self, relative: str) -> str:
        return (ROOT / relative).read_text(encoding="utf-8")

    def catalog_keys(self, relative: str) -> set[str]:
        root = ET.parse(ROOT / relative).getroot()
        return {node.attrib["name"] for node in root.findall("data")}

    def test_core_trend_uses_identifier_free_samples_and_exact_utc_window(self) -> None:
        analyzer = self.read("src/SwiftDrop.Core/Diagnostics/TransferPerformanceTrendAnalyzer.cs")
        sample = self.read("src/SwiftDrop.Core/Diagnostics/TransferPerformanceSample.cs")
        self.assertIn("TransferPerformanceSample", analyzer)
        self.assertIn("TransferPerformanceAnalyzer.IsValidMeasurement(entry)", analyzer)
        self.assertIn("sample.TimestampUtc.ToUniversalTime()", analyzer)
        self.assertIn("timestampUtc > windowEndUtc", analyzer)
        self.assertIn("sample.MeasuredBytes", analyzer)
        self.assertIn("sample.DurationMilliseconds", analyzer)
        self.assertIn("SaturatingAdd", analyzer)
        self.assertIn("MaxWindowDays = 3650", analyzer)
        for forbidden in ("PeerDeviceName", "FileName", "Direction", "Id"):
            self.assertNotIn(forbidden, sample)

    def test_csv_contract_is_aggregate_only_and_deterministic(self) -> None:
        exporter = self.read("src/SwiftDrop.Core/Diagnostics/TransferPerformanceTrendCsvExporter.cs")
        expected_header = (
            "date_utc,measured_transfers,measured_bytes,"
            "measured_duration_ms,weighted_bytes_per_second"
        )
        self.assertIn(expected_header, exporter)
        self.assertIn('ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)', exporter)
        self.assertIn('ToString("R", CultureInfo.InvariantCulture)', exporter)
        self.assertIn("TransferPerformanceTrendPoint", exporter)
        self.assertNotIn("TransferHistoryEntry", exporter)
        for forbidden in (
            "peer_device_name",
            "file_name",
            "direction",
            "endpoint",
            "nonce",
            "token",
            "certificate",
            "private_key",
        ):
            self.assertNotIn(forbidden, expected_header)

    def test_store_projects_only_numeric_timestamp_performance_fields_without_limit(self) -> None:
        store = self.read("src/SwiftDrop.Core/Storage/TransferHistoryStore.cs")
        self.assertIn("GetPerformanceSamplesSinceAsync", store)
        method = store[
            store.index("GetPerformanceSamplesSinceAsync") :
            store.index("public async Task DeleteAsync")
        ]
        self.assertIn("SELECT timestamp_utc, size_bytes, duration_ms, measured_bytes", method)
        self.assertIn("timestamp_utc >= $cutoff", method)
        self.assertIn("status = 'completed'", method)
        self.assertIn("duration_ms > 0", method)
        self.assertIn("measured_bytes > 0", method)
        self.assertIn("measured_bytes <= size_bytes", method)
        self.assertNotIn("SELECT id", method)
        self.assertNotIn("peer_device_name", method)
        self.assertNotIn("file_name", method)
        self.assertNotIn("direction,", method)
        self.assertNotIn("LIMIT", method)

    def test_app_export_uses_identifier_free_query_cache_cleanup_and_os_share_sheet(self) -> None:
        history_service = self.read("src/SwiftDrop.App/Services/TransferHistoryService.cs")
        page = self.read("src/SwiftDrop.App/HistoryPage.xaml.cs")
        self.assertIn("GetPerformanceSamplesSinceAsync", history_service)
        self.assertNotIn("GetPerformanceEntriesSinceAsync", history_service)
        self.assertIn("ExportPerformanceTrendCsvAsync", history_service)
        self.assertIn("FileSystem.CacheDirectory", history_service)
        self.assertIn("CleanupPreviousPerformanceTrendExports", history_service)
        self.assertIn("encoderShouldEmitUTF8Identifier: false", history_service)
        clear_method = history_service[
            history_service.index("public async Task ClearAsync") :
            history_service.index("private static void ValidateTrendWindow")
        ]
        self.assertIn("CleanupPreviousPerformanceTrendExports(FileSystem.CacheDirectory)", clear_method)
        zero_retention = history_service[
            history_service.index("if (days == 0)") :
            history_service.index("await _store.PruneOlderThanAsync")
        ]
        self.assertIn("CleanupPreviousPerformanceTrendExports(FileSystem.CacheDirectory)", zero_retention)
        self.assertNotIn("HttpClient", history_service)
        self.assertNotIn("TelemetryClient", history_service)
        self.assertIn("Share.Default.RequestAsync", page)
        self.assertIn("new ShareFileRequest", page)
        self.assertIn("HistoryPerformanceTrendShareTitle", page)

    def test_history_trend_ui_and_localizations_are_complete(self) -> None:
        view_model = self.read("src/SwiftDrop.App/ViewModels/HistoryViewModel.cs")
        page = self.read("src/SwiftDrop.App/HistoryPage.xaml")
        english = self.catalog_keys("src/SwiftDrop.App/Resources/Strings/HistoryRuntimeStrings.resx")
        hindi = self.catalog_keys("src/SwiftDrop.App/Resources/Strings/HistoryRuntimeStrings.hi.resx")
        required = {
            "HistoryPerformanceTrendTitle",
            "HistoryPerformanceTrendDescription",
            "HistoryPerformanceTrendNoMeasurements",
            "HistoryPerformanceTrendSummaryFormat",
            "HistoryPerformanceTrendLineFormat",
            "ExportPerformanceTrend",
            "HistoryPerformanceTrendShareTitle",
            "HistoryPerformanceTrendExportFailed",
            "HistoryPerformanceTrendExportFailedFormat",
        }
        self.assertIn("GetPerformanceTrendAsync", view_model)
        self.assertIn("ExportPerformanceTrendAsync", view_model)
        self.assertIn('Text="{Binding PerformanceTrendStatus}"', page)
        self.assertIn('Text="{Binding PerformanceTrendPreview}"', page)
        self.assertIn('IsEnabled="{Binding CanExportPerformanceTrend}"', page)
        self.assertTrue(required.issubset(english))
        self.assertTrue(required.issubset(hindi))


if __name__ == "__main__":
    unittest.main()

import unittest
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


class PerformanceHistoryContractTests(unittest.TestCase):
    def read(self, relative: str) -> str:
        return (ROOT / relative).read_text(encoding="utf-8")

    def catalog_keys(self, relative: str) -> set[str]:
        root = ET.parse(ROOT / relative).getroot()
        return {node.attrib["name"] for node in root.findall("data")}

    def test_schema_v6_keeps_duration_and_measured_bytes_separate(self) -> None:
        schema = self.read("src/SwiftDrop.Core/Storage/DatabaseSchemaManager.cs")
        self.assertIn("public const int CurrentVersion = 6;", schema)
        self.assertIn("ADD COLUMN duration_ms", schema)
        self.assertIn("PRAGMA user_version = 5;", schema)
        self.assertIn("ADD COLUMN measured_bytes", schema)
        self.assertIn("PRAGMA user_version = 6;", schema)
        self.assertLess(schema.index("ADD COLUMN duration_ms"), schema.index("ADD COLUMN measured_bytes"))

    def test_store_persists_and_bounds_optional_measurement_fields(self) -> None:
        store = self.read("src/SwiftDrop.Core/Storage/TransferHistoryStore.cs")
        self.assertIn("duration_ms, measured_bytes", store)
        self.assertIn("entry.DurationMilliseconds is < 0 or > MaxDurationMilliseconds", store)
        self.assertIn("entry.MeasuredBytes is < 0", store)
        self.assertIn("entry.MeasuredBytes > entry.SizeBytes", store)
        self.assertIn("reader.IsDBNull(8) ? null : reader.GetInt64(8)", store)
        self.assertIn("reader.IsDBNull(9) ? null : reader.GetInt64(9)", store)

    def test_analyzer_uses_weighted_actual_byte_measurements(self) -> None:
        analyzer = self.read("src/SwiftDrop.Core/Diagnostics/TransferPerformanceAnalyzer.cs")
        self.assertIn("measuredBytes * 1000d / measuredDurationMilliseconds", analyzer)
        self.assertIn("entry.MeasuredBytes <= entry.SizeBytes", analyzer)
        self.assertIn("NormalizeOptionalMeasurement", analyzer)
        self.assertIn("TransferPerformanceMeasurement", analyzer)
        self.assertIn("SaturatingAdd", analyzer)

    def test_sender_and_receiver_attribute_only_bytes_transferred_in_interval(self) -> None:
        coordinator = self.read("src/SwiftDrop.App/Services/TransferCoordinator.cs")
        page = self.read("src/SwiftDrop.App/MainPage.xaml.cs")
        receiver = self.read("src/SwiftDrop.App/Services/ReceiveServerService.cs")

        self.assertIn("return new FileSendResult(entry.Length, entry.Length - resumeOffset);", coordinator)
        self.assertIn("measuredBytes: sendResult.TransferredBytes", page)
        self.assertIn("measuredBytes: Encoding.UTF8.GetByteCount(text)", page)
        self.assertIn("effectiveEntry.Length - offset", receiver)
        self.assertIn("item.EffectiveEntry.Length - item.ResumeOffset", receiver)

    def test_history_ui_and_localization_are_wired_to_analyzer(self) -> None:
        view_model = self.read("src/SwiftDrop.App/ViewModels/HistoryViewModel.cs")
        page = self.read("src/SwiftDrop.App/HistoryPage.xaml")
        english = self.catalog_keys("src/SwiftDrop.App/Resources/Strings/HistoryRuntimeStrings.resx")
        hindi = self.catalog_keys("src/SwiftDrop.App/Resources/Strings/HistoryRuntimeStrings.hi.resx")
        required = {
            "HistoryPerformanceNoMeasurements",
            "HistoryPerformanceSummaryFormat",
            "HistoryDurationFormat",
            "HistoryThroughputFormat",
        }

        self.assertIn("TransferPerformanceAnalyzer.Summarize", view_model)
        self.assertIn("TransferPerformanceAnalyzer.BytesPerSecond", view_model)
        self.assertIn('Text="{Binding PerformanceStatus}"', page)
        self.assertIn('Text="{Binding DurationText}"', page)
        self.assertIn('Text="{Binding ThroughputText}"', page)
        self.assertTrue(required.issubset(english))
        self.assertTrue(required.issubset(hindi))


if __name__ == "__main__":
    unittest.main()

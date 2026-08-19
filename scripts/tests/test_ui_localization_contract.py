from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


class UiLocalizationContractTests(unittest.TestCase):
    def test_home_exposes_completed_destinations(self) -> None:
        xaml = read("src/SwiftDrop.App/MainPage.xaml")
        navigation = read("src/SwiftDrop.App/MainPage.Navigation.cs")

        for handler in (
            "OpenQueueClicked",
            "OpenHistoryClicked",
            "OpenSettingsClicked",
            "OpenAboutClicked",
        ):
            self.assertIn(f'Clicked="{handler}"', xaml)
        self.assertIn("OpenAboutClicked", navigation)
        self.assertIn("GetRequiredService<AboutPage>()", navigation)
        self.assertIn("{services:Localize PairingQrCodeDescription}", xaml)
        self.assertNotIn('SemanticProperties.Description="SwiftDrop pairing QR code"', xaml)

    def test_settings_localizes_display_without_persisting_translations(self) -> None:
        xaml = read("src/SwiftDrop.App/SettingsPage.xaml")
        view_model = read("src/SwiftDrop.App/ViewModels/SettingsViewModel.cs")

        self.assertIn('ItemsSource="{Binding ThemeOptions}"', xaml)
        self.assertIn('SelectedIndex="{Binding ThemeIndex, Mode=TwoWay}"', xaml)
        self.assertIn('ItemsSource="{Binding LanguageOptions}"', xaml)
        self.assertIn('SelectedIndex="{Binding LanguageIndex, Mode=TwoWay}"', xaml)
        self.assertNotIn("<x:String>System</x:String>", xaml)
        self.assertNotIn("<x:String>English</x:String>", xaml)

        for stable_value in ('1 => "Light"', '2 => "Dark"', '_ => "System"'):
            self.assertIn(stable_value, view_model)
        self.assertIn('LanguageIndex == 1 ? "hi" : "en"', view_model)
        self.assertIn('AppText.Get("DoNotRetainHistory")', view_model)
        self.assertIn('AppText.Format("RetentionDaysFormat"', view_model)
        self.assertIn('"CertificateFingerprintFormat"', view_model)
        self.assertIn('AppText.Get("ReceiveFolderSupportWindows")', view_model)
        self.assertIn('AppText.Get("ReceiveFolderSupportPrivate")', view_model)

    def test_support_surfaces_do_not_reintroduce_english_literals(self) -> None:
        about = read("src/SwiftDrop.App/AboutPage.xaml")
        settings = read("src/SwiftDrop.App/SettingsPage.xaml")

        for source in (about, settings):
            self.assertNotIn('SemanticProperties.Description="Buy Me a Coffee support logo"', source)
        self.assertNotIn('Text="Support SwiftDrop on Buy Me a Coffee"', about)
        self.assertNotIn('Text="☕  Buy Me a Coffee"', about)
        self.assertNotIn('Text="Support SwiftDrop"', settings)
        self.assertNotIn('Text="Optional support for continued open-source development."', settings)
        self.assertNotIn('Text="☕ Buy Me a Coffee"', settings)

    def test_runtime_status_and_queue_presentation_use_localization(self) -> None:
        diagnostics = read("src/SwiftDrop.App/ViewModels/DiagnosticsViewModel.cs")
        queue = read("src/SwiftDrop.App/ViewModels/QueueViewModel.cs")
        formatter = read("src/SwiftDrop.App/Services/LocalizedStatusFormatter.cs")

        for key in (
            "ProtocolVersionFormat",
            "MdnsDiscoveryStatusFormat",
            "UdpFallbackStatusFormat",
            "AutomaticDiscoveryUnavailableTitle",
            "AutomaticDiscoveryUnavailableMessage",
            "DeveloperOptionsDisabled",
            "RunningSyntheticSelfTest",
            "SelfTestResultFormat",
            "SelfTestFailedFormat",
        ):
            self.assertIn(key, diagnostics)

        self.assertNotIn("entry.OperationKind.ToString()", queue)
        self.assertNotIn("entry.State.ToString()", queue)
        self.assertIn("LocalizedStatusFormatter.QueueOperation(entry.OperationKind)", queue)
        self.assertIn("LocalizedStatusFormatter.QueueState(entry.State)", queue)
        self.assertIn("LocalizedStatusFormatter.QueueCounts", queue)
        self.assertIn("QueueStateQueued", formatter)
        self.assertIn("QueueOperationReceive", formatter)

    def test_final_polish_catalog_is_loaded_and_validated(self) -> None:
        app_text = read("src/SwiftDrop.App/Services/AppText.cs")
        validator = read("scripts/validate_localization.py")
        english = read("src/SwiftDrop.App/Resources/Strings/UiPolishStrings.resx")
        hindi = read("src/SwiftDrop.App/Resources/Strings/UiPolishStrings.hi.resx")

        self.assertIn("UiPolishStrings", app_text)
        self.assertIn("UiPolishStrings.resx", validator)
        self.assertIn("UiPolishStrings.hi.resx", validator)
        for key in (
            "PairingQrCodeDescription",
            "CertificateFingerprintFormat",
            "QueueStateInterrupted",
            "QueueOperationReceive",
        ):
            self.assertIn(f'name="{key}"', english)
            self.assertIn(f'name="{key}"', hindi)


if __name__ == "__main__":
    unittest.main()

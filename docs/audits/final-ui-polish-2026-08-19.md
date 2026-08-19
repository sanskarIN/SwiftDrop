# Final UI completion audit — 2026-08-19

This audit records the final repository-side defect pass performed after the earlier completion merge. It is intentionally defect-driven: it does not invent post-v1 features merely to create activity in a source-complete repository.

## Scope

The review focused on maintained .NET MAUI presentation/runtime surfaces that can still hide completion defects after Core protocol/storage/security work is complete:

- home-screen navigation reachability;
- English/Hindi presentation parity;
- accessibility descriptions;
- Settings display values versus persisted canonical values;
- Diagnostics runtime status text;
- Queue state/operation presentation;
- portable regression enforcement for those boundaries.

## Reproducible defects found

### Completed pages were not all reachable from the home screen

`MainPage.xaml.cs` already contained navigation handlers for Queue, History, Settings, Nearby Devices, and Diagnostics, but the home XAML exposed only Nearby Devices and Diagnostics. Queue, History, and Settings were therefore implemented yet not directly reachable from the primary dashboard, and About had no dashboard handler.

The home screen now exposes localized buttons for:

- Transfer Queue;
- Transfer History;
- Settings;
- About.

The About destination is implemented in a small `MainPage` partial so the large transfer code-behind is not expanded unnecessarily.

### Remaining English-only accessibility/support strings

The pairing QR semantic description and Buy Me a Coffee support surfaces still contained English literals. Those strings now flow through localization resources, including accessibility descriptions.

The canonical support URL remains unchanged and is intentionally not localized.

### Settings localized display values could not safely double as stored values

Theme and language are persisted with canonical machine values:

- theme: `System`, `Light`, `Dark`;
- language: `en`, `hi`.

The previous Picker items were literal English display strings and `SettingsViewModel` used displayed language names to decide which language code to save. Translating those display values directly would have risked persisting presentation text instead of a validator-approved canonical value.

The corrected Settings design binds Pickers to localized display lists and binds selection through stable indexes. Saving maps the selected index back to the canonical values above. This keeps UI localization independent from configuration serialization.

The same pass localized:

- certificate fingerprint label;
- history-retention text;
- platform receive-folder guidance;
- support-card text and accessibility description.

### Diagnostics runtime statuses leaked English

User-facing protocol/discovery/self-test statuses in `DiagnosticsViewModel` were constructed with English literals. These now resolve through `AppText` while diagnostic event codes and safe technical log records remain stable machine-oriented values.

A second pass found that `TransferSelfTestService` correctly returns stable English technical messages from Core for logging/debugging, but `DiagnosticsViewModel` was also displaying that raw `SelfTestResult.Message` in the localized UI. The UI now maps the stable self-test code plus pass/fail state to a localized outcome summary. The original Core message is still written unchanged to the safe diagnostic log, preserving useful technical evidence without leaking English presentation text into Hindi UI.

Localized runtime presentation now covers:

- protocol version label;
- mDNS/Bonjour status;
- UDP fallback status;
- discovery-unavailable title/message;
- developer-options-disabled message;
- self-test running/pass/fail/failure presentation;
- successful round-trip outcome detail;
- checksum-mismatch protection outcome detail;
- interrupted-receive recovery outcome detail.

### Queue enum names leaked English

`QueueViewModel` displayed `TransferQueueState` and `TransferQueueOperationKind` by calling `ToString()`. In Hindi UI this exposed English enum identifiers.

`LocalizedStatusFormatter` now owns queue state and operation presentation. Queue counts are calculated from typed entries before localization, avoiding a subtle bug where translated state labels could break running/queued/interrupted counts.

## Localization catalog

A focused resource pair was added:

- `src/SwiftDrop.App/Resources/Strings/UiPolishStrings.resx`;
- `src/SwiftDrop.App/Resources/Strings/UiPolishStrings.hi.resx`.

`AppText` loads the catalog in the normal fallback chain. `scripts/validate_localization.py` now requires English/Hindi key equality and matching format placeholders for this pair just like the established catalogs.

## Regression enforcement

`scripts/tests/test_ui_localization_contract.py` is executed automatically by the existing Python unittest discovery in CI. It protects:

- Queue/History/Settings/About home navigation wiring;
- localized QR semantic text;
- stable-index Settings Pickers;
- canonical theme/language save mapping;
- localized Settings runtime labels;
- removal of English-only support-card literals;
- localized Diagnostics runtime keys;
- localized self-test outcome details while preserving Core technical messages in safe logs;
- removal of queue enum `ToString()` presentation;
- final-polish resource loading and validation wiring.

This test is intentionally a narrow source contract. Platform workflows still provide the real MAUI/XAML compilation boundary.

## Hosted validation boundary

Pull requests that touch `src/SwiftDrop.App/**` trigger the maintained platform-build workflow for:

- Android Release compilation/audit;
- focused Windows Release compilation/audit;
- Mac Catalyst Release compilation/audit;
- iOS Simulator Share Extension compilation/audit;
- iOS Simulator containing-app compilation/audit.

Normal CI also runs Python helper tests, repository-completion validation, documentation validation, localization validation, Apple/Windows metadata validation, Core build/tests/benchmarks, and vulnerability-report validation.

The final PR checks for this audit must be read from GitHub Actions after the branch head is complete; this document does not pre-claim a passing result.

## External release work remains external

This repository-side pass does not turn hosted source checks into signed-device evidence. Production release still requires the exact signed candidate to complete the maintained external/manual gates, including representative physical cross-device transfer behavior, signed Android/Windows/Apple packaging, Apple provisioning/App Group/notarization, Windows packaged activation/firewall/notification behavior, real providers/filesystems/network conditions, assistive-technology and Hindi UI validation, exact signed-artifact dependency/license/provenance reconciliation, and final store/privacy review.

Those are evidence tasks, not missing source implementations.

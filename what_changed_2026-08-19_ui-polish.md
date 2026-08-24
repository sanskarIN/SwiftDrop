# What changed — final UI polish

Date: 2026-08-19  
Repository: `https://github.com/sanskarIN/SwiftDrop`  
Branch: `final-polish-20260819`

This file is the dated `what_changed` appendix for the final defect-driven SwiftDrop UI completion pass. The cumulative `what_changed.md` remains intact as historical engineering evidence and is not shortened or rewritten.

## Reproducible defects fixed

- Home navigation did not directly expose the already-implemented Transfer Queue, Transfer History, Settings, and About destinations.
- The pairing QR semantic description was hard-coded in English.
- About and Settings retained English-only Buy Me a Coffee support/accessibility literals.
- Settings Pickers coupled English display labels too closely to persisted theme/language values, making direct translation unsafe for the canonical configuration contract.
- Settings retention, certificate-fingerprint, and receive-folder guidance text still contained English runtime literals.
- Diagnostics protocol/discovery/self-test runtime presentation still contained English literals.
- Core self-tests returned intentionally technical English messages for diagnostic logging, and Diagnostics was also displaying those raw messages in the localized UI.
- Queue state and operation labels were displayed from enum `ToString()` values, leaking English identifiers in Hindi UI.

## Source changes

### Localization resources

Added a focused English/Hindi resource pair:

- `src/SwiftDrop.App/Resources/Strings/UiPolishStrings.resx`;
- `src/SwiftDrop.App/Resources/Strings/UiPolishStrings.hi.resx`.

`AppText` now loads that pair and `scripts/validate_localization.py` validates key and placeholder parity.

The catalog also contains localized outcome summaries for all three maintained synthetic self-tests in pass/fail form. This lets presentation remain localized without changing technical Core diagnostics.

### Home navigation

`MainPage.xaml` now exposes localized direct buttons for:

- Transfer Queue;
- Transfer History;
- Settings;
- About.

`MainPage.Navigation.cs` supplies the About handler as a focused partial-class addition. The pairing QR accessibility description now resolves through localization.

### Settings correctness

`SettingsViewModel` now maintains localized Picker display lists separately from canonical persisted values. Selection uses stable indexes and maps back to:

- `System`, `Light`, `Dark` for theme;
- `en`, `hi` for language.

This prevents translated presentation strings from becoming configuration values that `SettingsValidator` would reject.

The Settings view model also localizes retention, certificate-fingerprint, and receive-folder support labels. Settings support-card UI/accessibility text is localized.

### Diagnostics

User-facing protocol version, discovery availability, UDP fallback, discovery failure, developer-options, and synthetic self-test status text now flows through `AppText`. Stable safe-log codes remain machine-oriented.

Self-test outcome details now map the stable Core `SelfTestResult.Code` plus `Passed` state to localized UI resource keys. The original `SelfTestResult.Message` remains unchanged in the safe diagnostic log so troubleshooting evidence is preserved without leaking English technical text into Hindi presentation.

### Queue

`LocalizedStatusFormatter` now owns queue state and operation presentation. Queue counts are calculated from typed `TransferQueueEntry` values before translation, so translating the displayed state cannot break running/queued/interrupted counts.

## Regression coverage

Added `scripts/tests/test_ui_localization_contract.py` to the existing Python unittest discovery. Its focused tests enforce:

- home destination wiring;
- localized QR accessibility text;
- localized Settings Picker lists with canonical save mapping;
- localized Settings runtime labels;
- removal of English-only support literals;
- localized Diagnostics status keys;
- localized self-test outcome summaries rather than raw Core messages;
- preservation of raw Core self-test messages in safe diagnostic logs;
- no queue enum `ToString()` display leakage;
- final-polish catalog loading/validator wiring.

The existing pull-request `platform-builds.yml` path rules compile the maintained Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app targets when `src/SwiftDrop.App/**` changes.

## Documentation

Added:

- `docs/audits/final-ui-polish-2026-08-19.md`;
- this dated what-changed appendix.

Updated:

- `docs/README.md` canonical index;
- `FINAL_REPOSITORY_STATUS.md` current repository-side status.

## Focused commit trail before hosted validation

- `d7ac0d66683cb317e646d9bd323077598f7be079` — English final-polish resources;
- `a3b32a82da6e27346653b9b5f53b52413be085f3` — Hindi final-polish resources;
- `a67c933b42ac1b8997664aafe4436c1414f7c2be` — AppText catalog loading;
- `e9f3f4cd7c710381587d56940b7ca96e47cdb4cf` — localization validator coverage;
- `5eb457c7c717760fd4514687a8f31458b0d280f6` — home destination navigation and QR accessibility localization;
- `840e7fcadd4955bcf09349ef0f79fd8e57738b41` — About navigation handler partial;
- `2dad33cb9df261907568f334355e4cd3f6bb6256` — About support localization;
- `474621c8d93c237265e40263d109851e46bf44c8` — canonical Settings value/display separation;
- `9925f29680cf0c10b350a806a5787bbb5f4d3974` — localized Settings Picker/support UI;
- `ebc987a3cc02ee865f155b41812082e2766080d6` — localized Diagnostics runtime statuses;
- `af6d5423710bc17817ef8b908a675312571dd33c` — centralized queue label formatting;
- `778273611ad0db2e3ec20d71ae39d70af7a14c5e` — localized queue state/operation presentation;
- `6b23a799f7f963b8ec27752dce06f0776c20eb80` — final UI localization/navigation regression contract;
- `80a0f7d427c897158881b35c84d22dea89e52019` — dated UI completion audit;
- `83fed596b0e03208b8296d0df3c2b9cc01bec5c2` — documentation index;
- `0410d40e97ab0ff22e218fc623ec5a9409a1ef59` — canonical final repository status synchronization;
- `053b58b4885e6dd1598460426def1c4f97315357` — final UI what-changed appendix indexing;
- `6441c5e757fec248344fc9734fdbd0c2ee697dd3` — English self-test outcome resources;
- `8b6220260968491695ef6feb907d2c93c876e99b` — Hindi self-test outcome resources;
- `fe825a02213bfae92f904cc1f2987398f7f02cdb` — localized self-test outcome presentation with technical logging preserved;
- `844fad3becff4bddc165780f0d2fcb224dd1765f` — self-test message-leak regression contract;
- `13408fd7506c5a8336abed4da13587e111fc9021` — audit clarification for the presentation/logging boundary.

## Validation boundary

This appendix does not pre-claim hosted checks. The final branch head must be evaluated through its pull-request CI/platform/security/release workflows. Signed-device/store validation remains external release evidence and is not represented as completed by source edits.

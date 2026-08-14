# SwiftDrop Clean Architecture Boundary

Updated: 2026-08-14

SwiftDrop separates portable protocol/security/storage code from .NET MAUI platform/UI code. This boundary is intentionally explicit rather than hiding platform dependencies behind misleading abstractions.

## `SwiftDrop.Core`

Portable responsibilities:

- protocol constants and strict wire framing;
- pairing payload/canonical capability validation;
- local/private address policy;
- certificate creation/fingerprint utilities;
- canonical relative-path and filename safety primitives;
- regular source/link/reparse safety;
- bounded deterministic folder enumeration and sender-path deconfliction;
- discovery data models and UDP discovery primitive;
- transfer manifest/batch models and validators;
- streaming transfer engine, hashing, resume policy, resource limits, and idle timeouts;
- shared external staging-budget policy;
- SQLite schema manager and metadata stores;
- completed-batch verification primitives;
- network diagnostics models/services that do not depend on MAUI UI;
- synthetic transfer self-tests;
- reusable concurrency/rate-limit/session-tracking primitives.

Core must not depend on MAUI pages, Clipboard, FilePicker, SecureStorage, platform permission prompts, or UI dialogs.

## `SwiftDrop.App/Services`

Application/platform coordination responsibilities:

- MAUI SecureStorage/Preferences integration;
- device identity lifetime;
- receive-root resolution and platform folder picker integration;
- mDNS/Bonjour service integration;
- nearby/manual pairing orchestration;
- receive-server lifecycle and user-consent callbacks;
- stable transfer/batch resume orchestration;
- queue/background activity lifetime;
- transfer-history/privacy-mode application policy;
- trusted-device application policy;
- diagnostic export/share integration;
- external share/deep-link/App Group inbox coordination;
- appearance/culture application;
- optional platform notification coordination where implemented.

Services pass portable models to Core and do not move cryptographic, path, source, staging-budget, or file-integrity validation into page event handlers.

## Pages and view models

Pages own visual composition, focus/navigation, native picker/dialog/share/drop surfaces, and platform-sensitive lifecycle presentation. View models own observable page state and presentation-oriented user state.

Current dedicated view-model coverage includes:

- `MainViewModel`;
- `HistoryViewModel`;
- `QueueViewModel`;
- `DevicesViewModel`;
- `TrustedDevicesViewModel`;
- `DiagnosticsViewModel`;
- `SettingsViewModel`;
- `AboutViewModel`.

`MainPage` still coordinates platform UI primitives that cannot be moved into portable view models without creating false abstractions, including dialogs/navigation, clipboard, system file pickers, QR image generation, and lifecycle-sensitive receive-server/external-input presentation. Transfer/security correctness remains below that UI layer.

Architecture rules:

- one-time pairing authorization remains consumed atomically below the UI layer;
- UI display names never become cryptographic identity;
- filesystem/network/path/source validation remains below the UI layer;
- page/view-model state never stores private certificate keys;
- logging never receives payload content or reusable pairing secrets;
- cancellation tokens flow through long-running network/file operations;
- stable batch transfer IDs are caller-visible application state but verification/authorization remains Core/service policy;
- externally shared/dropped content is review input and never an automatic-send command.

## Platform projects and folders

`SwiftDrop.App/Platforms/Android`, `Platforms/iOS`, `Platforms/MacCatalyst`, and `Platforms/Windows` contain declarations/activation/background/native integrations that genuinely require the target SDK. Shared code must not pretend these platform policies are identical.

`SwiftDrop.ShareExtension` is a separate **iOS-only** app-extension target. It stages bounded user-selected content into the configured App Group and references Core policy; it does not perform peer transfer or own the user's SwiftDrop private identity.

Mac Catalyst external intake remains in the containing desktop app through native `UIDropInteraction` and normal file/document flows; there is no maintained Mac Catalyst Share Extension target.

## Focused build-validation boundary

The multi-target application remains a normal Android/iOS/Mac Catalyst/Windows MAUI product project. CI may narrow that matrix for one platform without changing product architecture:

- Windows uses `SwiftDropTargetFrameworksOverride` and skips the iOS extension project-reference edge only for focused Windows validation;
- hosted Windows compilation uses `WindowsPackageType=None`, so signed MSIX packaging remains a separate release gate;
- hosted iOS Simulator compilation clears signing/provisioning requirements only at command scope; real source entitlements remain intact for signed/device builds.

These switches are build-validation boundaries, not runtime feature flags.

## Dependency direction

Desired dependency direction:

`Platform UI / iOS Share Extension -> Application services/view models -> SwiftDrop.Core`

Core does not reference the application or extension projects. Platform-specific types are translated into portable primitives at the application/extension boundary.

## Security review rule

Any change that moves authentication, authorization, canonical-path validation, source-link safety, staging budgets, resource limits, integrity verification, or cryptographic comparison upward into the UI layer should be treated as a regression unless there is a documented reason and equivalent lower-level validation remains present.

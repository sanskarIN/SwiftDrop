# SwiftDrop Clean Architecture Boundary

SwiftDrop separates portable protocol/security/storage code from .NET MAUI platform/UI code. This boundary is intentionally enforced incrementally rather than hiding platform dependencies behind misleading abstractions.

## `SwiftDrop.Core`

Portable responsibilities:

- protocol constants and wire framing;
- pairing payload validation;
- local/private address policy;
- certificate creation/fingerprint utilities;
- path and filename safety primitives;
- discovery data models and UDP discovery primitive;
- transfer manifest/batch models and validators;
- streaming transfer engine, hashing, resource limits, and idle timeouts;
- SQLite schema manager and metadata stores;
- network diagnostics models/services that do not depend on MAUI UI;
- synthetic transfer self-tests;
- reusable concurrency/rate-limit primitives.

Core must not depend on MAUI pages, Clipboard, FilePicker, SecureStorage, platform permission prompts, or UI dialogs.

## `SwiftDrop.App/Services`

Application/platform coordination responsibilities:

- MAUI SecureStorage/Preferences integration;
- device identity lifetime;
- receive-root resolution and platform folder picker integration;
- mDNS/Bonjour service integration;
- nearby/manual pairing orchestration;
- receive-server lifecycle and user-consent callbacks;
- queue/background activity lifetime;
- transfer-history/privacy-mode application policy;
- trusted-device application policy;
- diagnostic export/share integration;
- external share/deep-link inbox;
- appearance/culture application.

Services should pass portable models to Core and avoid moving cryptographic/file validation into page event handlers.

## Pages and view models

Pages own visual composition, focus/navigation, and platform user-dialog presentation. View models own observable page state and simple user intents where doing so does not require platform UI primitives.

Current MVVM migration includes dedicated History and Queue view models. The main transfer dashboard still coordinates multiple platform dialogs and lifecycle-sensitive operations in code-behind; this is a known refactor boundary, not a hidden architectural claim.

A future extraction should introduce dedicated transfer-session view models/application commands while preserving these rules:

- one-time pairing authorization remains consumed atomically;
- UI display names never become cryptographic identity;
- filesystem/network validation remains below the UI layer;
- page/view-model state never stores private certificate keys;
- logging never receives payload content or pairing secrets;
- cancellation tokens flow through every long-running network/file operation.

## Platform folders

`Platforms/Android`, `Platforms/iOS`, `Platforms/MacCatalyst`, and `Platforms/Windows` contain declarations/activation/background integrations that genuinely require the target SDK. Shared code must not pretend these platform policies are identical.

## Dependency direction

Desired dependency direction:

`Platform UI -> Application services/view models -> SwiftDrop.Core`

Core does not reference the application project. Platform-specific types should be translated into portable primitives at the application boundary.

## Security review rule

Any change that moves authentication, authorization, path validation, resource limits, or cryptographic comparison upward into the UI layer should be treated as a regression unless there is a documented reason and equivalent lower-level validation remains present.

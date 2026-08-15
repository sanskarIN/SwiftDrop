# SwiftDrop Settings Reference

Updated: 2026-08-15

This document maps the maintained Settings UI to its source-backed behavior and defaults.

## Defaults

Current `AppSettings.Default` values:

| Setting | Default |
|---|---|
| Transfer concurrency | 2 |
| History retention | 30 days |
| Privacy mode | Off |
| Auto-accept trusted devices | Off |
| Theme | System |
| Notifications | Off |
| Reduce motion | Off |
| Custom receive folder | App-private default |
| Larger interface | Off |
| Language | English (`en`) |
| Developer diagnostics | Off |

## Device name

The device name is the human-readable local identity label shown to peers and in SwiftDrop UI.

- Maximum UI input length: 64 characters.
- Saving renames the local device identity label; it does not replace the certificate by itself.

## Certificate fingerprint

Settings displays the SHA-256 fingerprint of the local device certificate. Use it when manually comparing device identities during pairing.

The fingerprint is derived from the current local certificate; it is not an editable setting.

## Reset device identity

Resetting identity:

1. clears locally stored trusted-device relationships;
2. creates/resets the local cryptographic device identity;
3. changes the displayed certificate fingerprint;
4. requires previous peers to establish trust/pairing again where applicable.

Use this only when intentionally rotating the device identity or recovering from identity problems.

## Receive folder

### Windows

Windows supports a system folder picker. SwiftDrop requests access to the folder explicitly chosen by the user.

### Android, iOS, and Mac Catalyst

These targets currently use SwiftDrop's app-private Received folder. Custom external-folder selection is intentionally disabled rather than requesting broad filesystem access.

The **Use app folder** action returns the setting to the platform default app-private receive location.

## Concurrent transfers

- Minimum: 1
- Maximum: 8
- Step: 1
- Default: 2

This controls bounded transfer concurrency. Higher values can increase simultaneous work but may increase CPU, storage, battery, memory, and local-network contention.

## History retention

- Minimum: 0 days
- Maximum: 3650 days
- Step: 1 day
- Default: 30 days

`0` means do not retain transfer history. Changing and saving the value applies the retention policy to stored history.

History is metadata-only; transferred file bytes and transferred text are not stored in the history database.

## Privacy mode

Default: Off.

Privacy mode hides/redacts peer/file identifiers in privacy-sensitive history and diagnostics surfaces. It is a presentation/metadata-minimization feature; it does not move, encrypt, or delete the actual files the user has chosen to receive.

See [PRIVACY.md](../PRIVACY.md) for exact local-data behavior.

## Auto-accept trusted devices

Default: Off.

When enabled, SwiftDrop may auto-accept only content from a device whose stored trust relationship matches the expected device ID and certificate fingerprint, and only for normal-risk content under the maintained policy.

Higher-risk content warnings/consent are not bypassed by this switch.

Trust can be managed/revoked from the Trusted Devices UI.

## Optional notifications

Default: Off on every target.

Completion/failure system notifications are implemented as an explicit opt-in setting on Android, iOS, Mac Catalyst, and Windows. Notification text is intentionally generic: it does not include filenames, peer names, paths, pairing information, transfer IDs, or transferred text/content.

A denied/unavailable notification capability does not determine transfer success or failure. The transfer result remains visible inside SwiftDrop.

### Android

Android uses the existing transfer notification path. On Android 13+, notification permission is requested only when the user enables notifications and saves the setting. Older supported Android versions do not require the Android 13 runtime notification permission.

The foreground-service notification required while Android keeps an active user-initiated transfer alive is a separate lifecycle requirement from the optional terminal completion/failure notification preference.

### iOS

SwiftDrop requests local notification `Alert` and `Sound` authorization only after the user enables notifications and saves Settings. The app uses the system User Notifications framework and installs a notification-center delegate so an enabled generic terminal notification can also be presented while SwiftDrop is foregrounded.

No push-notification service, remote notification token, cloud notification relay, or notification-server account is required by this feature.

### Mac Catalyst

Mac Catalyst uses the same local User Notifications framework path and opt-in authorization model as the iOS containing app. Signed sandbox/runtime behavior still requires physical/release validation.

### Windows

Windows uses Windows App SDK app notifications. SwiftDrop registers the app notification manager lazily when the user enables notifications or a terminal notification needs to be shown. The packaged manifest carries the notification activation/COM registration required by the packaged app model.

Hosted Windows CI intentionally performs an unpackaged source compile, so the release process must still verify the signed/package manifest registration and real notification activation behavior from the final MSIX/package.

## Reduce motion

Default: Off.

Requests a lower-motion SwiftDrop presentation where supported by current UI behavior. Release validation still requires checking the app together with operating-system accessibility preferences on actual devices.

## Larger interface

Default: Off.

Enables SwiftDrop's larger-interface preference. It does not replace operating-system text scaling/accessibility settings; both should be tested during release validation.

## Theme

Allowed values:

- System
- Light
- Dark

Default: System.

System follows the current platform appearance where supported by MAUI/platform behavior.

## Language

Maintained UI language choices:

- English (`en`)
- Hindi (`hi`)

Default: English.

Localization catalogs are validated in CI for XML validity, nonempty values, duplicate keys, exact English/Hindi key parity, and formatted placeholder parity. Generic notification completion/failure messages are present in both maintained language catalogs.

A language change may require UI refresh/navigation lifecycle behavior according to the current app implementation.

## Developer diagnostics

Default: Off.

When enabled, the Diagnostics UI exposes safe developer-oriented diagnostic information. The diagnostics design remains bounded and privacy-aware.

Safe diagnostic export must not contain:

- transferred file contents;
- transferred text contents;
- private keys;
- reusable pairing authorization;
- pairing nonces/capabilities;
- other secret signing material.

## Reset settings to defaults

Resetting settings restores `AppSettings.Default`, applies history retention, reapplies appearance settings, and reloads the Settings page state.

Resetting settings is separate from **Reset device identity**. A normal settings reset is not intended to rotate the cryptographic identity.

## Saving settings

Saving settings currently applies:

- device-name update;
- transfer concurrency;
- history retention;
- privacy mode;
- trusted-device auto-accept preference;
- theme;
- optional native notification preference/authorization on supported Android, iOS, Mac Catalyst, and Windows targets;
- reduce motion;
- receive-folder choice where supported;
- larger interface;
- language;
- developer diagnostics.

## Settings and release validation

Before release, verify settings on each applicable real target, including:

- persistence across restart/update;
- invalid/extreme values handled by UI bounds;
- custom Windows receive-folder behavior;
- Android notification permission deny/allow transitions;
- iOS/Mac Catalyst notification authorization, foreground presentation, disabled state, and generic content;
- Windows packaged notification registration, enable/disable behavior, activation, and generic content;
- notification denial/failure never changing the underlying transfer result;
- identity reset and re-pair flow;
- trusted-device revoke/auto-accept behavior;
- history-retention cleanup;
- English/Hindi layout and notification strings;
- dark/light/system appearance;
- large text and accessibility behavior.

See [release checklist](release/release-checklist.md) and [manual test matrix](testing/manual-test-matrix.md).

---

**Made by the Sanskar**

# SwiftDrop Next Steps

Updated: 2026-08-09

This roadmap is intentionally strict. It separates remaining **source work** from validation that requires target operating systems, physical devices, signing identities, stores, and real networks. Source implementation is not described as production-verified merely because the code exists.

## Recently completed in source

The following items were earlier roadmap work and are now implemented in the repository, subject to CI/target-device validation where applicable:

- Optional project support link at `https://buymeacoffee.com/sanskarIN` in README, `SUPPORT.md`, GitHub funding metadata, and the in-app About page.
- Shared receiver batch manifest validation and aggregate batch-byte limits.
- Aggregate free-space preflight for the accepted batch remainder.
- Complete sender batch source/count/per-file/aggregate preflight before hashing.
- Manifest-bound outgoing source length and receiver completion-length checks.
- Strict Core validation for receiver batch plans: unknown, duplicate, missing, contradictory, and out-of-range plans are rejected.
- Defense-in-depth revalidation of the pairing payload at the actual send boundary.
- Resume staging validation/truncation and immediate resumed-progress reporting.
- Atomic destination reservations across concurrent incoming sessions.
- Portable Unicode/filename sanitation, Windows reserved-device-name handling, and post-sanitation batch collision rejection.
- Shared platform path-comparison policy for Core receive confinement/reservations and external input de-duplication.
- 64-way concurrent destination reservation regression coverage.
- Live receive-listener restart when receive location changes plus active session tracking/drain.
- Native Windows drag/drop for files, folders, text, and SwiftDrop pairing links through the bounded external-input pipeline.
- Reference-counted Android Wi-Fi multicast lock for mDNS discovery.
- MAUI `Application.CreateWindow` startup and window-lifetime transfer/receiver cleanup.
- Current MAUI async dialog APIs on secondary screens plus MainPage async dialog compatibility wrappers.
- Explicit P-256 ECDSA identity certificate policy, TLS client/server EKUs, renewal/recovery, and user-visible re-pair notice.
- Canonical SHA-256 fingerprint normalization and certificate-bound trusted-device matching.
- Strict pairing invitation validation for local/private numeric addresses, metadata, fingerprint, nonce, lifetime, duplicate/unexpected query data, and unexpected outer URI fields.
- Strict framed application JSON with bounded length/depth, invalid UTF-8 rejection, no comments/trailing commas, and case-insensitive duplicate-property rejection.
- Every truncated prefix of a valid framed JSON message covered by boundary tests.
- Mutual-TLS loopback coverage for pin success/failure, bootstrap fingerprint observation, real encrypted file transfer, integrity verification, and staged resume.
- Manifest timestamp/path/size boundary tests.
- Restart-safe privacy-minimal queue metadata with schema version 2; stale active rows become `Interrupted` without replaying authorization.
- Transfer-history retention API mismatch fixed and history initialization serialized.
- English/Hindi resource catalogs expanded across primary and secondary XAML surfaces.
- XAML localization extension plus CI key-parity/well-formedness validation.
- Saved culture/theme applied before MainPage construction at startup.
- MVVM-backed History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About surfaces.
- Synthetic benchmark harness for hashing, batch-manifest validation, and portable path sanitation using generated temporary data only.
- Canonical `SwiftDrop.slnx` solution including app/core/tests/benchmarks; misleading XML `.sln` bootstrap removed.
- Portable verification scripts aligned with CI.
- Platform compile workflow configured for Android, Windows, Mac Catalyst, and unsigned iOS Simulator on `main` pushes/PRs.
- Release-readiness workflow expanded with localization, benchmark compile, dependency inventory, Android/Windows/Mac Catalyst/iOS Simulator compile gates, and aggregate result enforcement.
- Repository security-hygiene workflow strengthened against private keys/signing material/local databases/production `.env` artifacts.
- Security, protocol, storage, architecture, build, README, status, and changelog documentation synchronized with current source behavior.

## P0 — Release blockers

### 1. Get every automated gate green and retain evidence

Required automated evidence:

- localization parity validation;
- `SwiftDrop.Core` restore/build;
- full portable xUnit suite;
- synthetic benchmark project compile;
- dependency vulnerability audit/inventory;
- CodeQL C# analysis;
- repository security-hygiene checks;
- Android MAUI Release compile;
- Windows MAUI Release compile;
- Mac Catalyst MAUI Release compile;
- unsigned iOS Simulator Release compile;
- release-readiness aggregate gate.

The workflows are configured, but the GitHub combined-status endpoint available in this session has repeatedly returned **no status contexts**. That is unknown/unreported state, not a pass. Do not tag a release until the actual Actions UI/logs show the required jobs completed successfully for the release candidate.

Pay special attention to newer compile-sensitive areas:

- XAML localization markup extension/resource lookup;
- Settings/Devices/Diagnostics/Trusted/Queue/About DI view-model constructors;
- Android notification permission/foreground-service code;
- Windows WinUI drag/drop;
- Mac Catalyst/iOS URL activation;
- SecureStorage certificate load/regeneration;
- iOS Simulator runtime selection in Actions;
- SQLite schema v1→v2 queue migration.

### 2. Run the physical cross-device transfer matrix

At minimum validate these peer directions on a normal LAN/Wi-Fi:

- Windows → Android.
- Android → Windows.
- Android → Android.
- Windows → Windows.
- macOS → Android.
- Android → macOS.
- iOS → Windows.
- Windows → iOS.
- iOS → macOS.
- macOS → iOS.

For each supported direction exercise:

- mDNS discovery;
- UDP fallback with multicast discovery unavailable;
- QR/deep-link pairing;
- nearby pairing request;
- one-time 8-digit pairing code;
- manual numeric local-IP fallback;
- stale/expired pairing invitation rejection;
- public-IP/DNS pairing target rejection;
- fingerprint mismatch rejection;
- single small file;
- zero-byte file;
- large file;
- duplicate completed filename collision;
- many concurrent incoming transfers targeting the same name;
- Windows reserved-device filenames received from another OS;
- Unicode composed/decomposed filename collision;
- high-risk extension warning;
- multi-file batch;
- recursive folder transfer where source folder selection exists;
- receiver accept-all/selective/reject;
- aggregate batch free-space rejection;
- cancellation;
- pause then fresh-pair resume;
- interrupted network then fresh-pair resume;
- source-file mutation after manifest creation;
- checksum mismatch self-test path;
- text snippet transfer;
- explicit clipboard paste;
- trusted-device normal-risk behavior;
- high-risk content from a trusted device still requiring consent;
- trust revocation;
- identity reset then re-pair;
- automatic identity regeneration/renewal then re-pair;
- privacy mode;
- history retention/delete/clear;
- queue restart showing stale active metadata as `Interrupted` without auto-retry;
- receive-folder change while listener is active;
- Android optional notification enable/deny/allow behavior;
- Windows desktop file/folder/text/pairing-link drag/drop.

### 3. Validate hostile/restricted network and device conditions

Run manual cases on:

- guest Wi-Fi/client isolation;
- multicast-blocked Wi-Fi;
- Windows firewall blocked then allowed;
- Android background restrictions;
- iOS local-network permission denied then allowed;
- IPv4-only LAN;
- IPv6-capable LAN;
- network switch/change during transfer;
- sleep/lock during transfer;
- low-storage destination;
- very slow LAN;
- repeated wrong pairing codes;
- rapid inbound connection attempts;
- app restart during queued/running transfer;
- SecureStorage unavailable/locked/error conditions where reproducible.

Expected behavior is bounded/graceful failure with privacy-safe diagnostics. SwiftDrop must never attempt to bypass firewall, Wi-Fi isolation, MDM, enterprise, or operating-system policy.

### 4. Complete signed package validation

Android:

- create/store production keystore outside the repository;
- configure secrets only in trusted release infrastructure;
- generate signed AAB/APK;
- verify package ID/version/signature;
- clean-install and upgrade-test;
- validate foreground data-sync declarations against current Play policy;
- validate notification permission copy/behavior on supported Android versions;
- complete Play Data Safety declarations from the shipped binary.

Windows:

- configure production signing certificate outside the repository;
- generate signed MSIX/package;
- verify private-network capability and `swiftdrop://` registration;
- install/uninstall/upgrade-test;
- validate firewall behavior and packaged WinUI drag/drop.

Apple:

- configure Apple Developer signing/provisioning outside the repository;
- verify bundle identifiers, local-network permission text, Bonjour service declarations, URL scheme, sandbox/entitlements;
- build signed iOS and Mac Catalyst artifacts on macOS/Xcode;
- physical-device install/test;
- TestFlight/notarization/store submission as applicable.

Never commit signing private keys, passwords, provisioning secrets, store credentials, or private certificates.

## P1 — Remaining source work

### 5. MainPage presentation-state MVVM migration

Most secondary screens now have dedicated view models. `MainPage` remains the primary architecture gap because it coordinates several interactive states in one place.

Migrate incrementally rather than with a single high-risk rewrite:

- device identity display state;
- active receive-root display;
- current pairing link/code/fingerprint state;
- selected remote peer state;
- selected single-file state;
- selected batch state;
- text draft/status state;
- single/batch progress/status state;
- send/pause/resume/cancel enabled state;
- external-input presentation state.

Keep these in page/platform interaction or services rather than the view model:

- file/folder/system pickers;
- clipboard invocation;
- share sheets;
- modal consent/confirmation dialogs;
- navigation;
- TLS/networking;
- filesystem transfer operations;
- certificate/private-key operations;
- receive-server lifecycle/service implementation.

Do not destabilize current pause/resume/cancel/consent behavior just to claim a complete MVVM conversion.

### 6. Finish runtime-generated localization

Broad XAML coverage is now resource-backed and English/Hindi catalog parity is enforced in CI. Remaining localization work is mainly code-generated dialog/status text:

- MainPage pairing/send/receive status strings;
- MainPage consent/fingerprint dialogs;
- Nearby Devices pairing prompts/status details;
- Settings save/reset/permission dialogs;
- Trusted Devices revoke/clear dialogs;
- Diagnostics export/clear/self-test errors;
- dynamic queue/history status labels;
- identity-recovery notice;
- receive-location/platform capability messages;
- drag/drop/share-input status text.

Then validate on actual target UI:

- Hindi clipping/wrapping;
- large text/font scaling;
- fingerprint/technical values remaining readable;
- culture fallback;
- relaunch after language change;
- pluralization/dynamic count wording;
- future RTL readiness even though current Hindi/English are LTR.

### 7. Pairing JSON duplicate-property hardening

`StrictJsonGuard` now protects framed application JSON. A defensive change to invoke the same guard on the decoded JSON bytes inside `PairingCodec` was attempted during this implementation session, but the repository connector blocked that source replacement. Existing pairing protections remain active:

- URI structure/length;
- exactly one payload query parameter;
- local numeric address;
- protocol version;
- device metadata bounds;
- fingerprint format;
- nonce format;
- expiry/lifetime bounds.

When repository tooling permits a normal reviewed source update, apply `StrictJsonGuard.Validate(decoded, 16)` before `JsonSerializer.Deserialize<PairingPayload>` and retain the existing exception normalization/tests. Do not bypass repository safety controls to make this change.

### 8. Apple Share Extension

Current Apple code handles SwiftDrop pairing URL activation, but inbound arbitrary file/text sharing requires a first-class extension target and Apple lifecycle validation.

Design/implementation requirements:

- iOS/macOS share extension target where appropriate;
- App Group/shared container only if required;
- bounded item count/size before staging where APIs permit;
- safe filename normalization;
- explicit text handoff only;
- no clipboard monitoring;
- no cloud upload;
- clear handoff to main SwiftDrop UI;
- independent extension cancellation/error handling;
- cleanup of staged extension data after handoff/failure;
- minimal security-scoped/resource access lifetime;
- App Store entitlement/privacy review.

Do not add a placeholder extension target that compiles but cannot safely hand off data.

### 9. Mac Catalyst desktop drag/drop

Windows drag/drop is implemented. Remaining Mac Catalyst source work should accept, where sandbox APIs allow:

- files;
- folders;
- text snippets;
- SwiftDrop pairing links/URLs.

The path must feed `ExternalInputInbox`/existing source validation rather than creating a weaker direct-send path. Security-scoped access must be acquired/released for the shortest necessary lifetime and validated in a signed/notarized sandboxed build.

### 10. Full application-protocol loopback tests

The Core transport foundation already uses real TLS loopback streams. Expand into a UI-independent hostable application-protocol layer covering:

- one-time authorization consume/replay rejection;
- file request accept/reject;
- resume-offset negotiation;
- receiver completion-length reply;
- batch offer/plan/item/final flow;
- selective batch acceptance;
- unknown/duplicate/missing receiver plan items;
- text offer/accept/reject;
- cancellation;
- idle timeout;
- concurrent destination reservation;
- batch capacity rejection;
- metadata-history/diagnostic privacy guarantees.

Prefer extracting pure/request-host logic from `ReceiveServerService` into reusable application services instead of making portable tests depend on MAUI UI types.

### 11. Receive lifecycle integration tests

Once receive-host orchestration is UI-independent, automate:

- rapid receive-root changes;
- active session cancellation/drain semantics;
- no port-bind race during restart;
- staged partial remains in original accepted root;
- fresh authorization resumes only against current root;
- server dispose with multiple active clients;
- app-restart queue metadata and receive partial coexistence.

### 12. Additional property/fuzz tests

Already covered: frame length/depth/UTF-8/duplicates/truncation, pairing URI fields, path traversal, filename sanitation, Unicode/case collisions, reserved names, manifest timestamp/size/path bounds, receiver plan validation, resume offsets, source mutation, 64-way destination reservations, rate limits, TLS pinning.

Useful remaining cases:

- very high-count randomized legal/illegal relative paths;
- alternate path separators and UNC/device path variants across target-specific parsing;
- partial-file mutation between resume negotiation and stream start;
- repeated/reordered batch item-start frames;
- receiver final-total mismatch;
- connection close exactly at every protocol transition;
- high-concurrency rate-limiter eviction behavior;
- certificate change during a trusted-device flow;
- extreme but legal device/display names;
- clock-change behavior around certificate/pairing expiry boundaries.

Use deterministic generated fixtures; never use real user files/secrets in automated fuzz tests.

## P2 — Performance, accessibility, and polish

### 13. Run and archive performance measurements

The synthetic harness is implemented. Run comparable Release builds on representative hardware and archive JSON results for:

- Windows laptop/desktop;
- Android mid-range/high-end device;
- iPhone/iPad;
- Mac.

The harness measures hashing, manifest validation, and path sanitation only. Separately measure full peer-to-peer transfers for:

- small-file latency;
- large-file throughput;
- many-small-file batch overhead;
- TLS CPU cost;
- sender/receiver CPU usage;
- memory peak;
- receiver storage throughput;
- resume efficiency;
- discovery traffic.

Do not publish one CI runner number as a universal transfer-speed promise.

### 14. Accessibility release pass

Validate with:

- Android TalkBack;
- iOS VoiceOver;
- macOS VoiceOver;
- Windows Narrator;
- keyboard-only Windows/macOS navigation;
- large text/font scaling;
- high contrast;
- reduced motion;
- touch target checks;
- pairing fingerprint reading/copying;
- batch selective-consent controls;
- drag/drop status announcement;
- identity-regeneration notice;
- queue interrupted-state announcement.

Fix focus order, names, hints, contrast, clipping, and dynamic-status announcements discovered during this pass.

### 15. UI/UX polish

Consider after release blockers are green:

- responsive desktop max-width/layout improvements;
- clearer peer trust indicators;
- better empty states;
- stronger visual separation of pairing vs transfer authorization;
- non-blocking status banners for recoverable network errors;
- clearer fresh-pair resume explanation;
- visible drag/drop affordance on desktop;
- optional first-run explanation of local-network privacy/permissions;
- clearer identity-renewal/re-pair explanation;
- smoothed speed/ETA and per-file batch progress where measurements justify it.

Privacy mode must continue to avoid filename/text disclosure in status/notification surfaces.

## P2 — Distribution/community readiness

### 16. Store metadata and policy review

Prepare/verify per platform:

- screenshots from final signed binaries;
- short/long description;
- privacy-policy URL;
- support URL/email;
- optional support-development URL;
- Apache-2.0 attribution;
- exact dependency/third-party notice inventory;
- local-network permission rationale;
- no-cloud/local-transfer description;
- foreground-service disclosure where required;
- Bonjour/local-network rationale on Apple;
- private-network capability rationale on Windows;
- version/release notes;
- appropriate age/content declarations.

### 17. Validate support/funding surfaces

Current project support URL:

https://buymeacoffee.com/sanskarIN

Keep it optional. It must never imply that payment is required for:

- open-source source access;
- transfer features;
- security fixes;
- vulnerability handling;
- privileged access to private user data;
- bypassing normal support/security procedures.

### 18. Release candidate process

For every RC:

1. Freeze protocol-affecting changes.
2. Run localization parity.
3. Run portable Core build/tests.
4. Compile benchmark harness.
5. Run CodeQL/security hygiene/dependency checks.
6. Run Android/Windows/Mac Catalyst/iOS Simulator compile gates.
7. Review all Actions logs, not only a summary badge.
8. Run physical-device peer matrix.
9. Run restricted-network/low-storage/background cases.
10. Run accessibility pass.
11. Run target performance measurements.
12. Review permissions/entitlements against actual shipped behavior.
13. Review privacy/security docs against shipped binary.
14. Generate dependency/license inventory from exact release restore graph.
15. Update `CHANGELOG.md`, `PROJECT_STATUS.md`, and `what_changed.md`.
16. Tag only after source and device gates pass.
17. Build signed packages in trusted release environments.
18. Clean-install and upgrade-test signed packages.
19. Produce final store screenshots/metadata from signed RC.
20. Submit/review stores as applicable.

## Definition of production-ready

SwiftDrop should be described as production-ready only when all of these are true:

- portable Core tests pass;
- localization and repository hygiene gates pass;
- CodeQL/dependency checks pass;
- every intended platform compiles in its correct SDK environment;
- signed packages install/upgrade correctly;
- real-device peer transfers pass in both directions for supported platform pairs;
- permission-denied/network-blocked/low-storage/interrupted-transfer behavior is validated;
- certificate/SecureStorage lifecycle behavior is validated on targets;
- accessibility and localization passes are completed;
- privacy/security/docs match the shipped binary;
- no credentials/signing secrets are committed;
- store requirements are satisfied.

## Scope discipline

Do not add cloud relay, account login, analytics, advertising identifiers, remote Internet transfer, silent clipboard monitoring, automatic opening/execution of received files, or custom cryptography merely to make the feature list larger. Any of those would materially change SwiftDrop's privacy/threat model and require a separate product/security design review.

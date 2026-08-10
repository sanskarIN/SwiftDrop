# SwiftDrop Next Steps

Updated: 2026-08-10

This roadmap is intentionally strict. It separates remaining repository work from validation that requires real target operating systems, physical devices, signing identities, stores, and networks. Source implementation is not production verification.

## Recently completed in source

The following earlier roadmap items are now implemented in the repository, subject to automated/platform/device validation where relevant:

- Decoded pairing JSON now passes `StrictJsonGuard` before deserialization, including duplicate/case-variant property, comment, trailing-comma, UTF-8, and depth protections.
- Pairing nonces now use a reusable bounded `OneTimeAuthorizationStore` with atomic first-use consume, replay rejection, exact tick-level expiration, pruning, capacity control, and identity-reset clearing.
- Pairing invitation clock-boundary/replay/concurrency tests were expanded.
- Incoming request envelope/version/type, sender identity, transfer ID, and batch-item ordering validation were moved into reusable Core policy and are used by the receive host.
- Sender acknowledgement validation for resume offsets, file/item/batch completion lengths, and text acknowledgement offsets was moved into reusable Core policy.
- Receiver batch plan validation rejects unknown, duplicate, missing, contradictory, and out-of-range items.
- Main transfer presentation state now has a dedicated `MainViewModel` for identity/receive-root/peer/selection/status/progress/control-enabled state.
- Main XAML is bound to that view model; platform pickers, modal consent, navigation, QR rendering, clipboard/share calls, TLS, filesystem and cryptography remain outside the view model.
- Main partial files were updated to use the new view-model state without retaining references to removed XAML controls.
- English/Hindi localization now covers XAML plus runtime pairing, transfer, queue, device, trust, diagnostics, settings, history, platform/share, identity-recovery, and batch-consent text.
- Localization CI now validates XML well-formedness, non-empty values, duplicate keys, exact English/Hindi key parity, and formatted placeholder-index parity.
- History uses language-neutral private markers at rest and localized presentation rows for direction/status/size/time/private values.
- Privacy mode redacts peer and filename/description metadata for new history entries and redacts older history at read time.
- Diagnostic privacy redaction covers IPs/endpoints, GUIDs, SHA-256 fingerprints, file paths, email-like tokens, and SwiftDrop pairing URIs at record/read/export time.
- Trusted-device storage enforces canonical SHA-256 fingerprints at the Core storage boundary and ignores malformed persisted trust rows.
- History and diagnostic stores validate new metadata and skip malformed/corrupted persisted rows rather than failing the complete list.
- mDNS parsing rejects duplicate TXT metadata keys, compression-pointer loops, malformed/truncated input, and deterministic random packet fuzz input.
- Portable external-file staging implements bounded exact-length copying, sanitized destination names, cancellation, and failure cleanup.
- iOS and Mac Catalyst document/open-file URL handling stages external files through temporary security-scoped access into the existing bounded `ExternalInputInbox` workflow.
- iOS/Mac Catalyst Info.plist files declare `public.data` document opening and opening documents in place.
- Mac Catalyst has explicit app-sandbox plus network client/server entitlements wired to the target.
- Android application backup is disabled for local SwiftDrop metadata.
- Windows package networking now requests only `privateNetworkClientServer`, not general `internetClient`.
- Root compiler policy uses latest stable C# mode instead of preview mode.
- External-file staging tests are portable across Windows/Linux/macOS runners.
- Existing mutual-TLS/file/resume, strict JSON, batch/path/collision, database migration, queue, discovery, certificate and settings tests remain in place.

## P0 — Release blockers

### 1. Obtain successful automated build/test evidence for the release candidate

Required automated evidence:

- localization validation;
- `SwiftDrop.Core` restore/build with warnings treated as errors;
- full portable xUnit suite;
- synthetic benchmark project compile;
- dependency inventory/vulnerability audit;
- CodeQL C# analysis;
- repository security-hygiene workflow;
- Android MAUI Release compile;
- Windows MAUI Release compile;
- Mac Catalyst MAUI Release compile using the committed entitlements;
- unsigned iOS Simulator Release compile;
- release-readiness aggregate gate.

The GitHub connector available in this implementation session has returned no usable direct-main workflow runs/status contexts for queried commits. That is unknown/unreported state, not a pass.

When Actions evidence is available, pay special attention to the newest compile-sensitive source:

- `MainViewModel`/MainPage DI constructor and XAML bindings;
- `AppText` resource-manager additions;
- runtime resource catalogs and placeholder parity script;
- `AppleExternalFileStager` conditional compilation;
- iOS/Mac Catalyst AppDelegate file-URL activation;
- Apple Info.plist document declarations;
- Mac Catalyst `CodesignEntitlements` configuration;
- Core `IncomingRequestPolicy`/`TransferResponsePolicy` integration;
- storage corruption-tolerance code;
- latest stable C# language mode.

Do not tag or describe a release candidate as automated-green until the Actions UI/logs for that exact commit confirm it.

### 2. Run the physical cross-device transfer matrix

Minimum peer directions:

- Windows → Android;
- Android → Windows;
- Android → Android;
- Windows → Windows;
- macOS → Android;
- Android → macOS;
- iOS → Windows;
- Windows → iOS;
- iOS → macOS;
- macOS → iOS.

For each supported direction exercise as applicable:

- mDNS discovery;
- UDP fallback when multicast discovery is unavailable;
- QR/deep-link pairing;
- nearby pairing request;
- one-time 8-digit pairing code;
- manual numeric local-IP fallback;
- stale/expired pairing invitation rejection;
- duplicate/conflicting pairing JSON rejection;
- public-IP/DNS pairing-target rejection;
- certificate fingerprint mismatch rejection;
- one-time nonce replay rejection;
- single small file;
- zero-byte file;
- large file;
- duplicate destination collision;
- many concurrent incoming transfers targeting the same filename;
- portable Unicode composed/decomposed filename collision;
- Windows reserved-device filename received from another OS;
- high-risk-extension warning;
- multi-file batch;
- recursive folder transfer where folder selection exists;
- receiver accept-all/selective/reject;
- malformed/unknown receiver plan rejection;
- aggregate batch free-space rejection;
- cancellation;
- pause then fresh-pair resume;
- interrupted network then fresh-pair resume;
- source mutation after manifest generation;
- text snippet transfer;
- explicit clipboard paste;
- trusted-device behavior;
- high-risk content from a trusted device still requiring consent;
- trust revocation;
- local identity reset/re-pair;
- automatic identity regeneration/re-pair;
- privacy mode history/diagnostic behavior;
- history retention/delete/clear;
- queue restart showing stale active metadata as `Interrupted` with no automatic replay;
- receive-folder change while listener is active;
- Android optional notification enable/deny/allow;
- Windows file/folder/text/pairing-link drag/drop;
- Apple document/open-file URL staging where supported.

### 3. Validate hostile/restricted local environments

Required manual environments/cases:

- guest Wi-Fi/client isolation;
- multicast-blocked Wi-Fi;
- Windows Firewall blocked/allowed/private/public profile changes;
- Android background restrictions and vendor battery controls;
- iOS local-network permission denied then allowed;
- IPv4-only LAN;
- IPv6-capable LAN;
- network switch/change during transfer;
- sleep/lock during transfer;
- destination low-storage condition;
- very slow LAN;
- repeated wrong pairing codes;
- rapid inbound connection attempts;
- app restart during queued/running transfer;
- SecureStorage/keychain/keystore unavailable/locked/error conditions where reproducible;
- Apple security-scoped document-provider files;
- stale/corrupted SQLite metadata created by prior/broken builds.

Expected behavior is bounded/graceful failure with privacy-safe diagnostics. SwiftDrop must not bypass firewall, guest-network isolation, MDM, sandbox, or OS lifecycle policy.

### 4. Complete signed package validation

Android:

- create/store the production keystore outside the repository;
- configure release secrets only in trusted release infrastructure;
- generate signed AAB/APK;
- verify package ID/version/signature;
- clean-install and upgrade-test;
- validate foreground data-sync declarations against current Play policy;
- validate notification behavior/permission UX;
- verify Data Safety declarations from the final binary;
- verify backup-disabled behavior is appropriate for release expectations.

Windows:

- configure production signing certificate outside the repository;
- generate signed package/MSIX;
- verify `privateNetworkClientServer` behavior and no unnecessary internet capability;
- verify `swiftdrop://` registration;
- clean-install/update/uninstall-test;
- validate packaged native drag/drop;
- validate firewall prompts/private-network behavior.

Apple:

- configure Apple Developer signing/provisioning outside the repository;
- verify bundle identifiers, local-network copy, Bonjour service, URL scheme, document declarations, and Mac entitlements;
- produce signed iOS and Mac Catalyst builds on a current macOS/Xcode environment;
- verify document/open-file activation and security-scoped staging;
- physical-device install/test;
- complete TestFlight/notarization/store flow as applicable.

Never commit signing private keys, passwords, store credentials, provisioning secrets, or private certificates.

## P1 — Remaining repository/source work

### 5. First-class Apple Share Extension, if the product requires it

Current source supports Apple document/open-file URL intake. It does **not** contain a dedicated Share Extension target and must not claim one.

If a first-class Share Extension is required, implement it deliberately with:

- a real iOS/Mac-compatible extension target;
- correct bundle IDs/signing/provisioning;
- App Group/shared-container use only if required;
- bounded item count/size before staging where platform APIs permit;
- explicit text handoff;
- safe filename normalization;
- no background clipboard monitoring;
- no cloud upload/relay;
- independent extension cancellation/error handling;
- cleanup of staged extension files;
- short security-scoped/resource-access lifetime;
- handoff into the same bounded `ExternalInputInbox` review path;
- App Store entitlement/privacy review.

Do not add a placeholder extension that compiles but cannot safely hand off content.

### 6. Native Mac Catalyst drag-and-drop, if required

Windows native drag/drop is implemented. Mac Catalyst currently has document/open-file URL staging, not a first-class native drop surface.

A Mac drop implementation should support only what sandbox APIs safely expose:

- files;
- folders when security-scoped access can be handled correctly;
- text snippets;
- SwiftDrop pairing links/URLs.

Requirements:

- feed the existing `ExternalInputInbox`/bounded staging path;
- never direct-send dropped input;
- acquire/release security-scoped access for the shortest necessary period;
- enforce file count/size/source existence before expensive copy/hashing;
- clean staged files on cancellation/failure;
- validate in a signed sandboxed build.

### 7. Complete application-protocol loopback integration coverage

Core policy/transport tests are strong, but the complete application flow should eventually be hostable without MAUI UI types so it can be tested end to end.

Target scenarios:

- one-time authorization consume/replay rejection across a full request;
- file offer/accept/reject handshake;
- resume-offset negotiation;
- exact completion response;
- batch offer/plan/item/final sequence;
- selective batch acceptance;
- unknown/duplicate/missing plan items;
- reordered/unknown batch item-start frame;
- text offer/accept/reject;
- invalid text acknowledgement;
- cancellation;
- idle timeout;
- connection close at each protocol transition;
- concurrent destination collision;
- aggregate capacity rejection;
- history/diagnostic metadata remains content-free.

Prefer extracting UI-independent request-host orchestration rather than making portable tests depend on MAUI dialogs/pages.

### 8. Receive lifecycle integration tests

Once receive-host lifecycle is independently hostable, automate:

- rapid receive-root changes;
- active session cancellation/drain semantics;
- no port-bind race during listener restart;
- staged partial remains in the root that accepted it;
- fresh authorization resumes only against the current active root;
- server dispose while multiple clients are active;
- receive-root change with staged partials;
- queue metadata and partial staging across app restart.

### 9. Additional deterministic property/fuzz tests

Already covered:

- framed JSON bounds/depth/UTF-8/comments/trailing commas/duplicates/truncation;
- encoded pairing JSON strictness and URI fields;
- pairing lifetime/clock boundaries;
- one-time authorization replay/expiry/concurrency/capacity;
- mDNS compression loops/duplicate TXT/truncation/random packets;
- filename sanitation/path traversal/Unicode/case collisions;
- reserved Windows names;
- batch manifest/plan limits;
- source length mutation;
- resume offsets/staged tails;
- destination collision concurrency;
- rate limits;
- TLS pinning/mutual TLS;
- external-file staging/cancellation/size limits;
- corrupted trust/history/diagnostic rows.

Useful remaining deterministic cases:

- partial-file mutation between resume negotiation and receiver file-open;
- connection close exactly at every request/response transition;
- certificate replacement during an active trusted-device interaction;
- extreme but legal identity/display names through all storage/UI boundaries;
- very high-count randomized legal/illegal relative paths;
- platform-specific alternate separator/device-path cases;
- receive-root replacement while multiple staged transfers exist.

Use generated temporary fixtures only; never use real user files/secrets.

## P2 — Performance, accessibility, and UX

### 10. Run and archive performance measurements

The synthetic harness exists for:

- SHA-256 throughput;
- batch-manifest validation;
- portable path sanitation.

Run comparable Release builds on representative Windows, Android, iPhone/iPad, and Mac hardware and archive machine-readable results.

Separately measure full peer-to-peer behavior for:

- small-file latency;
- large-file throughput;
- many-small-file batch overhead;
- TLS CPU cost;
- sender/receiver CPU usage;
- memory peak;
- receiver storage throughput;
- resume efficiency;
- discovery traffic.

Do not advertise one CI-runner number as a universal transfer-speed promise.

### 11. Accessibility release pass

Validate with:

- Android TalkBack;
- iOS VoiceOver;
- macOS VoiceOver;
- Windows Narrator;
- keyboard-only Windows/macOS navigation;
- large text/font scaling;
- high contrast;
- reduced motion;
- touch target sizing;
- pairing fingerprint reading/copying;
- batch selective-consent controls;
- drag/drop/share/open-file status announcements;
- identity-regeneration notice;
- queue interrupted-state announcement.

Fix focus order, labels/hints, clipping, contrast, semantic announcements, and control reachability discovered by those real tests.

### 12. Localization visual validation

Source resources and CI parity are now broad. Real-device validation still needs:

- Hindi clipping/wrapping on every page;
- large font scaling;
- technical fingerprint/IP/path readability;
- culture fallback;
- relaunch after language change;
- plural/count wording;
- layout behavior for long translated buttons/statuses;
- future RTL readiness for any future RTL locale.

### 13. UI/UX polish after release blockers are green

Potential improvements driven by real testing:

- responsive desktop max-width/layout tuning;
- clearer trust indicators;
- richer empty states;
- stronger visual separation of pairing versus transfer authorization;
- non-blocking banners for recoverable local-network failures;
- clearer fresh-pair resume explanation;
- visible desktop drag/drop affordance;
- first-run local-network privacy/permission explanation;
- smoother speed/ETA if measurements justify it;
- improved per-file batch progress.

Privacy mode must continue to avoid peer/file/text disclosure in history, diagnostics, queue persistence, and notification surfaces.

## P2 — Distribution/community readiness

### 14. Store metadata and policy review

Prepare from final signed binaries:

- screenshots;
- short/long descriptions;
- privacy-policy URL;
- support URL/email;
- optional development-support URL;
- Apache-2.0 attribution;
- exact restored dependency/third-party notice inventory;
- local-network permission rationale;
- account-free/no-cloud description;
- Android foreground-service rationale;
- Bonjour/local-network rationale on Apple;
- Apple document/open-file behavior disclosure if relevant;
- Windows private-network capability rationale;
- version/release notes;
- age/content declarations.

### 15. Verify funding/support surfaces

Keep the optional support URL `https://buymeacoffee.com/sanskarIN` clearly separate from product capability, security support priority, and private user data. Payment must not unlock hidden transfer features or privileged access.

## Release definition

SwiftDrop is ready for a production release only when all of the following are true:

1. Required automated compile/test/security gates are green for the exact candidate.
2. The complete physical cross-device/network matrix is recorded and acceptable.
3. No known unresolved high-severity security/integrity/privacy defect remains.
4. Signed platform packages install/update/uninstall correctly.
5. Store declarations and privacy documentation match the final binaries.
6. Accessibility/manual permission/network lifecycle validation is complete enough for supported targets.
7. Performance has been measured rather than guessed.
8. `PROJECT_STATUS.md`, `CHANGELOG.md`, `PRIVACY.md`, platform docs, and `what_changed.md` match the release candidate.

Until those conditions are met, describe SwiftDrop as implemented substantially in source and under release validation—not as bug-free or production-verified.

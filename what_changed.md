# What changed

Date: 2026-08-14
Repository: https://github.com/sanskarIN/SwiftDrop
Branch: `main`
Master prompt: `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md`

This file is the detailed SwiftDrop engineering ledger for the current repository source scope. It records the cumulative implementation, the August 12 protocol/filesystem hardening, the August 14 dependency/build/platform completion pass, source and CI evidence, release boundaries, and the work that still requires signing credentials, physical devices, real providers, real networks, or store infrastructure.

It deliberately distinguishes:

- **implemented in source**;
- **portable-tested**;
- **hosted-platform compiled**;
- **signed/device validated**;
- **production verified**.

A successful hosted source compile is not treated as a signed-package or physical-device validation result.

---

# 1. Product and repository alignment

SwiftDrop is an open-source, account-free, local-network file and text transfer application built with .NET 10, .NET MAUI, and C#.

Current product/repository identity:

- application name: **SwiftDrop**;
- repository: `https://github.com/sanskarIN/SwiftDrop`;
- GitHub profile: `https://www.github.com/sanskarIN`;
- license: Apache-2.0;
- business/security email: `sanskarin@outlook.in`;
- support email: `supportramsandesh@gmail.com`;
- optional development support: `https://buymeacoffee.com/sanskarIN`;
- watermark/branding: `Made by the Sanskar`.

Transfer payloads are designed to stay on the local peer-to-peer path. The current product does not depend on a SwiftDrop-operated cloud upload or relay service for file/text transfer.

Optional financial support does not unlock transfer features, privileged security handling, hidden capabilities, private data access, or special transfer limits.

---

# 2. Canonical repository projects

Canonical solution:

- `SwiftDrop.slnx`

Projects:

- `src/SwiftDrop.Core`
  - portable protocol, security, pairing, path, filesystem, transfer, staging, hashing, TLS, discovery, SQLite, and metadata policy;
- `src/SwiftDrop.App`
  - .NET MAUI containing application and platform-specific integration for Android, iOS, Mac Catalyst, and Windows;
- `src/SwiftDrop.ShareExtension`
  - dedicated **iOS-only** Share Extension targeting `net10.0-ios`;
- `tests/SwiftDrop.Core.Tests`
  - portable xUnit regression/security/integration coverage;
- `benchmarks/SwiftDrop.Benchmarks`
  - synthetic bounded benchmark harness.

Mac Catalyst does **not** use a Share Extension in the maintained architecture. Mac external intake is implemented by the containing desktop application, normal file/document flows, and native `UIDropInteraction`.

---

# 3. Compiler and repository policy

Repository-wide source policy includes:

- .NET 10 target baseline;
- stable C# `LangVersion=latest`, not preview;
- nullable reference types;
- deterministic builds;
- current analyzers;
- warnings-as-errors on portable code;
- platform SDK availability/obsolete diagnostics retained where the MAUI/Apple SDK requires platform-specific warning handling.

The August 14 continuation did not weaken analyzers to make CI green. Compiler, analyzer, nullable, test, XAML, and platform errors were corrected at source/workflow level.

---

# 4. UI and MVVM architecture

Dedicated presentation view models currently back:

- Main dashboard → `MainViewModel`;
- History → `HistoryViewModel`;
- Queue → `QueueViewModel`;
- Nearby Devices → `DevicesViewModel`;
- Trusted Devices → `TrustedDevicesViewModel`;
- Diagnostics → `DiagnosticsViewModel`;
- Settings → `SettingsViewModel`;
- About → `AboutViewModel`.

The UI/platform boundary intentionally retains:

- user consent dialogs;
- navigation;
- file/folder pickers;
- clipboard operations;
- Android share intent handling;
- Windows native activation/drop;
- Mac native drop;
- iOS Share Extension/App Group handoff;
- platform lifecycle integration.

Reusable networking, TLS, cryptographic identity policy, hashing, SQLite, path validation, transfer authorization, source validation, resume validation, staging budgets, and protocol logic remain outside the view models in services/Core.

---

# 5. Local identity

SwiftDrop creates a local identity consisting of:

- random local device ID;
- user-visible local device name;
- self-signed P-256 ECDSA certificate;
- private key stored through platform `SecureStorage`.

Certificate profile includes:

- non-CA basic constraints;
- digital-signature key usage;
- TLS server-auth EKU;
- TLS client-auth EKU;
- subject-key identifier;
- bounded validity.

Identity policy checks:

- private-key presence;
- expected key type;
- certificate timing;
- expiration;
- renewal window;
- corrupt/unusable identity recovery.

When identity cannot safely be reused, SwiftDrop generates a fresh identity and requires deliberate re-pairing instead of silently continuing old trust.

Private keys are not stored in SQLite, pairing links, diagnostics, history, App Group share packages, or source code.

---

# 6. Certificate fingerprint policy

Peer certificate identity uses SHA-256 fingerprints.

Current behavior:

- exactly 32 SHA-256 bytes;
- canonical uppercase 64-hex internal representation where textual storage is needed;
- normalized compact/colon input where appropriate;
- constant-time equality for security comparisons;
- malformed persisted trust fingerprints ignored/rejected rather than treated as valid;
- human-readable colon formatting validates input first.

Trusted-device matching requires both:

- device ID; and
- exact certificate fingerprint.

A display name alone never establishes cryptographic trust.

---

# 7. Discovery

SwiftDrop includes:

- internal mDNS/DNS-SD discovery;
- bounded UDP IPv4 fallback;
- peer registry deduplication;
- expiry;
- self-filtering;
- stable peer sorting;
- Android multicast-lock integration;
- Apple Bonjour declarations;
- Nearby Devices UI/service integration.

Discovery parsing is tested against truncation, malformed DNS structures, compression pointer loops, impossible record counts, duplicate metadata, deterministic random packets, and bounded registry behavior.

Discovery metadata is not authentication. Transfer trust still depends on pairing, TLS, certificate identity, and one-time authorization.

---

# 8. Pairing methods

Supported local pairing methods:

- QR/deep-link pairing;
- Nearby pairing request;
- short-lived one-time 8-digit code;
- manual numeric local-IP + code fallback.

Pairing invitations contain short-lived connection metadata/capability information only and never contain private-key material.

Manual bootstrap observes the remote certificate, validates returned address/port/fingerprint against the actual connection, and still requires explicit user fingerprint verification.

---

# 9. Canonical pairing capability representation

Pairing capability text has one accepted protocol-v1 representation.

Decoder rejects:

- leading/trailing whitespace;
- wrong scheme or host;
- unexpected user info;
- unexpected outer authority port;
- unexpected path;
- fragment;
- missing query;
- malformed query segments;
- missing `=` after `p`;
- empty query segments;
- unknown query keys;
- duplicate `p` fields;
- standard Base64 `+` or `/`;
- Base64 padding `=`;
- percent-encoded payload aliases;
- invalid Base64URL length;
- decoded payloads that do not re-encode to the exact same canonical unpadded Base64URL text.

Decoded pairing JSON also rejects:

- malformed JSON;
- comments;
- trailing commas;
- case-insensitive duplicate members;
- unknown members;
- wrong protocol version;
- invalid device identity fields;
- invalid local address;
- invalid port;
- malformed fingerprint;
- malformed nonce;
- invalid expiry/lifetime.

---

# 10. TLS peer authentication

SwiftDrop uses .NET/platform TLS 1.2/1.3 rather than a custom cryptosystem.

Sender:

- pins the receiver certificate fingerprint learned during pairing;
- presents its own client certificate.

Receiver:

- requires a TLS client certificate;
- derives sender identity/fingerprint from the authenticated TLS connection;
- does not trust a sender fingerprint supplied inside ordinary application JSON.

Pairing/connection attempts are bounded/rate-limited.

---

# 11. One-time authorization

`OneTimeAuthorizationStore` provides bounded, expiring, thread-safe, one-use authorization tokens.

Behavior includes:

- syntax validation;
- exact expiration;
- expired-entry pruning;
- bounded store capacity;
- duplicate-active token rejection;
- atomic consume;
- exactly one winner under concurrent consume;
- replay rejection;
- clear/reset behavior.

Malformed requests and invalid canonical paths are rejected before valid transfer authorization is consumed.

---

# 12. Shared typed protocol

Production sender, receiver, pairing paths, and portable tests share Core wire records and validation policies.

Important records/policies include:

- `ProtocolRequest`;
- `TransferAcknowledgement`;
- `BatchItemStart`;
- `PairingResponse`;
- `BatchTransferResponse`;
- `BatchItemPlan`;
- `ProtocolRequestFactory`;
- `ProtocolRequestValidator`;
- `ProtocolSessionAuthorizer`;
- `IncomingRequestPolicy`;
- `TransferResponsePolicy`;
- `BatchTransferPlanValidator`.

This prevents anonymous app-private DTO drift and allows full protocol conversations to be portable-tested without MAUI UI dependencies.

---

# 13. Strict framed JSON

`FrameProtocol` uses a bounded four-byte signed big-endian frame length followed by UTF-8 JSON metadata.

Current protections:

- frame length must be positive and bounded before allocation;
- strict UTF-8 decoding occurs before JSON deserialization;
- malformed UTF-8 is rejected rather than replacement-decoded;
- JSON depth is bounded;
- comments/trailing commas rejected;
- duplicate members rejected case-insensitively at every object depth;
- unknown/unmapped members rejected;
- truncated headers/payloads fail;
- read/write/flush paths use cancellation and idle timeout handling.

Protocol v1 is therefore intentionally closed-schema.

---

# 14. Type-specific request validation

Protocol request types cannot smuggle fields from another request type.

Examples:

- file requests cannot carry batch/text/pair-only fields;
- batch requests require transfer ID, manifest, totals, and transfer authorization;
- text requests require bounded text + expiry and cannot carry file/batch/pair fields;
- pair requests cannot carry transfer authorization.

Malformed identity, nonce/code, file metadata, text expiry, batch totals, transfer IDs, and cross-type fields are rejected before transfer negotiation.

---

# 15. Canonical portable relative paths

Shared `PortableRelativePath` defines one path grammar across all sender/receiver operating systems.

Rejected:

- rooted OS paths;
- leading `/`;
- leading `\\`;
- Windows drive prefixes;
- UNC/device path syntax;
- repeated separators;
- empty path segments;
- trailing separators;
- `.` segments;
- `..` segments;
- more than 64 segments.

The canonical wire separator is `/` on every platform.

Windows local paths are converted into canonical wire paths before transfer. Incoming backslash aliases are rejected instead of rewritten after authorization.

---

# 16. Canonical manifest validation before nonce consumption

`ManifestValidator` checks canonical path identity before one-time transfer authorization can be consumed.

Checks include:

- nonempty path;
- maximum metadata length;
- no control characters;
- strict portable structure;
- maximum depth;
- exact equality with SwiftDrop canonical sanitation;
- file length bounds;
- valid SHA-256;
- timestamp bounds.

Paths that would change through sanitation/Unicode normalization/reserved-name handling are rejected as noncanonical wire input.

---

# 17. Filename sanitation

`FileNameSanitizer` applies:

- Unicode NFC;
- outer whitespace cleanup;
- portable invalid-character filtering;
- control-character removal;
- Windows reserved-device-name neutralization;
- unsafe trailing dot/space cleanup;
- deterministic fallback name;
- post-filter normalization;
- post-filter whitespace canonicalization.

August 14 fixed an idempotence boundary where removing an invalid/control character could expose new leading/trailing whitespace after the initial trim.

---

# 18. UTF-8 and UTF-16 filename limits

Canonical filename segments are bounded by both:

- 180 UTF-16 code units; and
- 180 UTF-8 bytes.

Truncation does not split Unicode scalar values/surrogate pairs.

The byte cap leaves headroom for `.swiftdrop.part` on common byte-limited filesystems.

---

# 19. Collision naming

`FileNameSanitizer.CreateCollisionSegment` ensures a collision marker survives even when the original filename is already at the segment limit.

Used by:

- destination reservations;
- generic collision-safe path construction;
- sender batch deconfliction;
- Apple external staging/package naming.

Conventional `name (n).ext` is used when possible; a bounded prefix strategy is available when appending a suffix would be truncated away.

---

# 20. Receive path and filesystem safety

Receive path policy includes:

- strict canonical wire path validation;
- lexical confinement to receive root;
- existing receive-root symlink/reparse component rejection;
- repeated reparse checks around staging/hash/promotion;
- atomic concurrent destination reservation;
- collision-safe paths;
- non-overwrite final promotion.

If another process creates a final destination after reservation and before promotion, SwiftDrop preserves that external file and fails closed rather than overwriting it.

---

# 21. Single-file source safety

`TransferSourceSafety` validates regular non-link/non-reparse source files/directories.

Single-file flow validates the selected file before manifest creation and repeats regular-source validation at the actual stream-open boundary.

This narrows a race where a regular source path could be replaced with a symlink/reparse object between selection/hashing and streaming.

---

# 22. Single-file transfer integrity

Sender:

- validates regular source;
- validates size bounds;
- canonicalizes name/path;
- hashes SHA-256;
- validates manifest;
- sends typed request;
- revalidates source at stream open;
- streams exactly declared bytes;
- confirms length remains compatible with manifest.

Receiver:

- validates request/manifest before authorization;
- obtains consent;
- reserves destination;
- preflights storage;
- negotiates bounded resume offset;
- stages `.swiftdrop.part`;
- receives exactly expected bytes;
- hashes complete staging;
- constant-time compares expected/actual hash;
- removes invalid staging;
- promotes only after verification.

Optional last-write timestamp is best-effort metadata after verified promotion and cannot convert verified content into a false transfer failure.

---

# 23. Bounded deterministic folder traversal

`TransferSourceEnumerator` replaces unrestricted recursive enumeration.

It:

- validates selected root as regular non-link directory;
- rejects linked/reparse roots;
- explicitly walks directories;
- rejects linked/reparse descendants;
- bounds directory count;
- bounds file count;
- gathers regular files;
- sorts by normalized relative path for deterministic manifests.

This prevents traversal outside the intended source tree through linked directories and stabilizes retry manifest identity.

---

# 24. Batch source building

`BatchTransferSourceBuilder` supports:

- selected files;
- selected folders;
- recursive folder contents;
- caller-supplied stable transfer ID for retry;
- fresh transfer ID for a new explicit send;
- file-count/per-file/aggregate preflight;
- canonical `/` relative paths;
- deterministic order;
- portable path collision deconfliction;
- hash construction;
- final manifest validation.

Known source/path/size constraints are preflighted before expensive hashing where practical.

---

# 25. Stable batch transfer IDs

A new explicit batch gets a new random transfer ID.

Pause/failure retains the same ID.

Retry uses:

- same stable transfer ID;
- fresh pairing invitation/authorization.

Transfer IDs accept bounded ASCII letters, digits, `-`, and `_` only.

Obsolete duplicate UI handlers and the compatibility overload that could silently generate a fresh ID for retry have been removed.

---

# 26. Paused source retention

`TransferSourcePathPolicy` keeps only still-existing regular non-link/non-reparse files/folders.

It provides:

- normalized full paths;
- platform-aware deduplication;
- missing-source removal;
- symlink/reparse replacement rejection;
- safe history metadata for files and folders.

Single-file resume applies this validation before consuming a fresh remote pairing capability.

---

# 27. Batch receive decisions

Receiver supports:

- reject batch;
- accept all;
- accept selected items.

Sender validates receiver plan against source manifest, including:

- no unknown paths;
- no duplicate paths;
- valid accepted/rejected semantics;
- resume offsets within source length;
- item ordering;
- final batch totals.

Accepted aggregate remaining bytes are preflighted before payload transfer.

---

# 28. SQLite schema v3

Current SQLite schema version: **3**.

Tables cover:

- trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal queue state;
- completed-batch resume metadata.

`completed_batch_items` stores metadata only:

- stable transfer ID;
- canonical source relative path;
- hashed receive-root identity;
- effective local destination relative path;
- expected length;
- expected SHA-256;
- completion timestamp.

It does not store transfer contents or reusable authorization.

---

# 29. Idempotent completed-file batch resume

After a batch item is fully verified/finalized, completion metadata may be recorded before normal item completion acknowledgement.

On retry, `BatchCompletionVerifier` requires:

- same stable transfer ID;
- same receive-root key;
- same canonical source path;
- same length/hash;
- destination still confined beneath current receive root;
- no symlink/reparse redirection;
- destination still exists at expected length;
- freshly computed SHA-256 still matches.

Only then can receiver offer `ResumeOffset == Length`.

---

# 30. Second completed-item verification

A retry-plan-to-ACK TOCTOU window was closed.

After sender returns the matching `BatchItemStart`, receiver verifies the completed destination **again immediately before** the zero-byte completion ACK.

Mutation, deletion, changed length/hash, root mismatch, completion-record mismatch, or reparse redirection fails closed.

A new explicit send has a new transfer ID and therefore preserves normal collision-safe duplicate-send semantics.

---

# 31. Text transfer

Text transfer uses the same paired TLS path.

Behavior includes:

- UTF-8 byte bounds;
- expiry validation;
- receiver Reject / Accept / Accept-and-Copy;
- zero-offset acknowledgement rule;
- clipboard read only after explicit user action;
- no continuous clipboard monitoring;
- history stores text metadata, not text contents.

External text truncation is rune-safe and does not split Unicode scalar values.

---

# 32. External input inbox

`ExternalInputInbox` provides atomic review handoff for external pairing/text/path input.

Controls include:

- bounded pairing-link storage;
- UTF-8-bounded text;
- path-count limit;
- full local-path normalization;
- existence checks;
- platform-aware duplicate suppression;
- recursive stale cache cleanup;
- one review inbox event per accepted batch handoff.

External content is never automatically sent.

---

# 33. Shared external staging budget

`TransferStagingBudget` centralizes:

- maximum staged file count;
- maximum per-file bytes;
- maximum aggregate bytes;
- remaining count/bytes;
- maximum allowed next-file bytes;
- preflight without consumption;
- commit only after successful exact staging.

Zero-byte items still consume file-count budget.

August 14 fixed an exhaustion boundary so a fully consumed positive aggregate byte budget is closed to further files instead of incorrectly accepting an extra zero-byte item.

Used by:

- Android share intake;
- iOS Share Extension;
- Mac native drop.

---

# 34. Android share intake

Android supports:

- `ACTION_SEND`;
- `ACTION_SEND_MULTIPLE`;
- shared text;
- shared content URIs.

Staging protections include:

- URI deduplication;
- protocol attachment count limits;
- provider display-name lookup;
- provider declared-size handling;
- negative size treated as unknown;
- portable UTF-8-bounded filename sanitation;
- bounded app-cache staging;
- aggregate staging budget;
- runtime byte cap for unknown-size providers;
- repeated free-space reserve checks during unknown-size copy;
- exact staged length verification;
- cleanup on failure;
- atomic review-inbox handoff.

---

# 35. Android lifecycle integration

Android active user-initiated transfers use a foreground data-sync service.

The August 14 compile pass hardened:

- API-level service/channel handling;
- Android application-context qualification;
- nullable notification builder bindings;
- nullable notification instances;
- launch `PendingIntent` creation;
- optional notification failure isolation.

Notification permission/platform policy cannot cause the underlying transfer itself to fail.

---

# 36. Android multicast integration

`AndroidMulticastLockManager` is reference-counted.

August 14 fixed nullable binding/service conditions so:

- missing application context is handled;
- unavailable Wi-Fi service is handled;
- nullable multicast-lock creation is handled;
- failed acquisition does not leave the reference count falsely elevated;
- release/dispose remains safe.

---

# 37. Android intent/binding update

August 14 updated Android share/activation code to current bindings:

- current Activity `Intent` property;
- current openable-column binding use;
- bounded shared URI collection;
- per-share staging directory;
- sanitized original filename retained inside that directory;
- declared/unknown length handling;
- failed file/directory cleanup;
- shared staging budget commit only after successful exact copy.

This removed Android compiler errors previously hidden behind the restore gate.

---

# 38. iOS Share Extension architecture

`SwiftDrop.ShareExtension` is now intentionally **iOS-only**:

- target framework: `net10.0-ios`;
- application ID: `in.sanskar.swiftdrop.share`;
- containing application ID: `in.sanskar.swiftdrop`;
- shared App Group: `group.in.sanskar.swiftdrop`.

The extension handles bounded user-provided:

- files;
- images;
- movies;
- text;
- web URLs.

It stages a package for later containing-app review and does not perform a peer transfer itself.

---

# 39. Mac Catalyst architecture correction

The original source history included an attempted Mac Catalyst Share Extension target.

August 14 corrected the maintained architecture because the current .NET 10 Mac Catalyst SDK does not provide the required app-extension CSharp target used by `IsAppExtension` in that configuration.

Current Mac Catalyst path:

- containing MAUI desktop app;
- normal file/document intake;
- native `UIDropInteraction`;
- security-scoped source access;
- shared staging budget;
- bounded provider waits;
- link/reparse rejection;
- review-before-send.

There is no maintained Mac Catalyst Share Extension target.

The stale Mac Catalyst Share Extension entitlement file was removed.

---

# 40. Apple App Group package model

The iOS Share Extension publishes into App Group storage using:

- `.staging-<id>` temporary directory;
- `files/` payload directory;
- strict versioned manifest;
- atomic rename to `pending-<id>` only after complete staging/validation.

Manifest contains bounded metadata only:

- version;
- package ID;
- creation time;
- optional text;
- file names/lengths.

No private keys, pairing nonces, pairing codes, reusable transfer authorization, or trust secrets are included.

---

# 41. Apple provider lifecycle

The iOS extension provider callbacks are bounded by a provider-response timeout and extension-lifetime cancellation.

Late callbacks after response timeout/cancellation cannot begin a new copy.

The response timeout is separated from local copy duration: once a provider responds in time and a legitimate local copy starts, the response timer does not incorrectly terminate that active copy.

Security-scoped access is released in `finally` where provided.

---

# 42. Apple containing-app importer

`AppleShareContainerImporter` treats App Group packages as untrusted input.

It validates:

- strict JSON and unknown fields;
- version/package ID/time bounds;
- canonical filenames;
- item count/per-file/aggregate limits;
- package/manifest/files/file symlink/reparse state;
- exact declared file lengths;
- exact physical file set;
- no undeclared top-level files;
- no nested undeclared directories;
- aggregate app-cache capacity before recopy.

Import is serialized and exposes one pending package for review at a time rather than silently merging/deleting later pending packages.

---

# 43. Mac native drop

Mac Catalyst native drop supports:

- files;
- folders;
- text;
- pairing links.

Controls include:

- temporary security-scoped access;
- regular-source/link validation;
- shared count/per-file/aggregate budget;
- bounded provider-response waits;
- capacity checks;
- portable collision-safe bounded staging;
- review-inbox handoff;
- no automatic send.

---

# 44. Windows integration

Windows source includes:

- `swiftdrop` protocol registration/activation;
- private-network client/server capability;
- native system receive-folder picker;
- files/folders/text/pair-link drag/drop;
- local path → canonical `/` wire path conversion;
- regular-source/link checks before send;
- atomic external-input handoff.

August 14 compiler fixes explicitly qualified WinUI/WinRT types to remove namespace ambiguity between MAUI, WinUI, and legacy Windows APIs.

---

# 45. Windows focused target isolation

The multi-target MAUI project normally contains Android, iOS, Mac Catalyst, and Windows targets.

Hosted Windows validation must not accidentally traverse unrelated mobile workloads or the iOS extension graph.

August 14 added:

- `SwiftDropTargetFrameworksOverride` for focused single-TFM validation;
- `SkipIosShareExtensionProjectReference` for the Windows validation path only;
- explicit Windows `TargetFramework` values in CI;
- explicit `win-x64` runtime restore/override where required.

Normal product configuration still retains the full platform target matrix and iOS extension reference on the iOS build.

---

# 46. Windows compile versus MSIX boundary

Hosted Windows CI now intentionally compiles with:

- `WindowsPackageType=None`;
- `GenerateAppxPackageOnBuild=false`.

This validates source/XAML/WinUI compilation without pretending an unsigned hosted compile is a signed MSIX release test.

Signed MSIX generation, certificate signing, install, update, package identity, protocol registration, and capability behavior remain explicit release gates.

`BUILDING.md` now mirrors this maintained green CI command boundary.

---

# 47. Local metadata/privacy

SQLite schema v3 remains metadata-only.

It does not store:

- transferred file bytes;
- transferred text contents;
- private keys;
- reusable transfer authorization;
- pairing invitations/nonces;
- absolute receive-root path for completed-item reuse.

Privacy mode redacts peer/file identifiers in history and common privacy-sensitive identifiers in diagnostics.

Android app-local backup is disabled.

---

# 48. History maintenance

August 14 restored/extended history pruning behavior through a compatibility-safe API:

- `PruneBeforeAsync` returns the number of removed rows for maintenance callers;
- existing `PruneOlderThanAsync` compatibility behavior remains available.

This fixed a compile/test mismatch without breaking existing callers.

---

# 49. Diagnostics

Diagnostic redaction covers common:

- paths;
- email addresses;
- IP addresses;
- endpoints;
- GUIDs;
- fingerprints;
- pairing URIs.

Diagnostic persistence is bounded and corruption-tolerant where intended.

Safe export excludes transfer contents, private keys, nonces, and reusable pairing capabilities.

---

# 50. Trusted devices

Trust persistence is certificate-bound and validates canonical SHA-256 fingerprints at storage boundaries.

Trusted auto-accept:

- disabled by default;
- opt-in;
- requires exact device ID + certificate fingerprint;
- limited to normal-risk content;
- does not bypass warning/consent for higher-risk content.

---

# 51. Queue/restart behavior

Queue uses bounded cancellation-aware concurrency.

Persisted restart metadata contains only privacy-minimal transfer state, not file paths, content, pairing authorization, or peer credentials.

Previously `Queued`/`Running` records become interrupted after restart and are not automatically replayed with stale authorization.

---

# 52. Settings

Settings include:

- device name;
- identity reset;
- receive location;
- transfer concurrency;
- history retention;
- privacy mode;
- trusted-device auto-accept;
- theme;
- notifications preference;
- reduce motion;
- larger interface;
- English/Hindi language;
- developer diagnostics.

Receive-folder changes cause the receiver path/listener state to be re-resolved rather than silently continuing to the old location.

---

# 53. Buy Me a Coffee support UI

August 14 source also includes the optional support-card work that preceded the platform hardening pass:

- custom support logo asset;
- highlighted support card;
- localization-safe UI treatment;
- settings support link/action;
- README/support documentation alignment.

Relevant commits include:

- `54b8257ecec89d4cc552430e8a2854935fc3b7e6` — custom support logo asset;
- `f49c5bec238cbbeccdb52cc0104ed6e158af36d0` — highlighted support card;
- `de0f539936360dd8060e1db4be36538c723b254e` — localization-safe highlight;
- `b956bb039c2803201af359d5ee16b243358167dc` — settings support link;
- `19f6c706d2e4040703219d4c1188fa8b3d899bcd` — support action wiring;
- `9b91d828632d3a0738cac1e5c90b39b974175a02` — support documentation.

---

# 54. Localization

English/Hindi resource catalogs cover primary UI and major runtime/dialog/status surfaces.

CI validates:

- XML well-formedness;
- nonempty values;
- duplicate keys;
- exact English/Hindi key parity;
- formatted placeholder index parity.

August 14 marked `LocalizeExtension` service-provider-independent for XAML compilation, removing repeated `XC0103` warnings without changing localization behavior.

---

# 55. XAML portability repair

A real cross-platform XAML compiler error was exposed only after platform builds reached the application project.

`SettingsPage.xaml` used `Entry.LineBreakMode`, but MAUI `Entry` is a single-line control and does not expose that property.

Commit:

- `ebb94f8fc427a417cba788b51027439940e64773` — removed unsupported `Entry.LineBreakMode`.

The same XAML source now compiles across Android, Windows, iOS, and Mac Catalyst gates.

---

# 56. Multi-file picker nullability

The serviced MAUI API exposes a nullable/cancelled multi-file result contract.

The app now treats a cancelled/null result as an empty selection and filters to non-null `FileResult` values before reading paths.

Relevant commits:

- `3d5ce81fae53f52313c4579b6fd006ea0dc2d82d` — normalize nullable picker results;
- `addb068add8b18d4ea2ec05b2eb695c3c632e39d` — handle null/cancelled collection itself under the serviced API contract.

---

# 57. .NET MAUI servicing refresh

Application dependency was updated:

- `Microsoft.Maui.Controls` from 10.0.0 → **10.0.90**.

Commit:

- `21a7b68a9a5de8ff41c49acd3b3b0e7807d42be4`.

The source was then recompiled through Android, Windows, Mac Catalyst, and iOS Simulator gates rather than assuming the servicing update was harmless.

---

# 58. SQLite security dependency recovery

The August 14 continuation began with CI restore blocked by security auditing/warnings-as-errors because the dependency graph selected vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.11.

Fix:

- `Microsoft.Data.Sqlite` → 10.0.10;
- explicit `SQLitePCLRaw.bundle_e_sqlite3` → 2.1.12.

Commit:

- `2114c0f8a3d6a52dc705c576984b34e401406cd5` — `security(deps): patch bundled SQLite dependency`.

This allowed restore to proceed and expose previously hidden compile/test defects rather than suppressing the advisory.

---

# 59. Core compile defects exposed after restore recovery

Once dependency restore was healthy, real compile errors became visible.

Focused fixes included:

- `595c814ac70b6131f616e7eb00b3532dc1150587` — use canonical text snippet byte limit;
- `508957cc029fadead36447a9e2b6749e7345086d` — import transfer manifest validators in request factory;
- `03bd8ae7e00ec7268e077a714df2ceec12fc72c5` — import transfer manifest validators in request validation;
- `c07f41a804e46abd462da3600d11510961b92fcb` — supported SHA-256 hex parsing API in security code;
- `47b592e8577de3826442a877d53f65bb67761ada` — supported completion-hash hex parsing.

No duplicate validator implementations were introduced; the missing namespace wiring was repaired instead.

---

# 60. Test-project recovery

The test source intentionally relied on project/global xUnit namespace wiring.

August 14 restored that project-level import rather than adding repetitive `using Xunit` lines to every test file.

Commit:

- `dedfab99afd085ac3922edba60bee30da5752030`.

Additional focused test/analyzer repairs:

- `2757f21cdb4e16eae3fa1683d8fcdb796deff4d3` — text snippet test uses protocol source-of-truth constant;
- `43687ae8a20ab0757b47bf1203cd0167e2e7f3d6` — xUnit suffix assertion in reservation tests;
- `405d560c8c993115b4ff4d76ae4dbaf2f25d0d86` — xUnit suffix assertion in filename tests;
- `4c6af0950d291eccf879028d159de011e5965ee2` — tests assert `JsonException` contract rather than exact subclass implementation detail.

Analyzer enforcement was retained.

---

# 61. Strict UTF-8 repair

Runtime tests exposed that protocol frame decoding needed an explicit strict UTF-8 boundary before JSON parsing.

Commit:

- `46428536f80469e63645009e934976b285af4d4e` — reject malformed UTF-8 before JSON parsing.

Invalid byte sequences can no longer be replacement-decoded into a JSON string before deserialization.

---

# 62. Filename idempotence repair

Runtime tests exposed sanitation stability issues.

Commits:

- `99c35c6c2ffd03307da74a9435f00ab6a2855f22` — normalize again after invalid-character removal and allow deterministic `unnamed` fallback;
- `28a551247c172d2cbc0bcf7cc6992fcb72113531` — trim whitespace exposed after invalid-character filtering/bounding.

The sanitation function is now stable across repeated application for these boundary cases.

---

# 63. Staging-budget exhaustion repair

Runtime testing exposed a boundary after all positive aggregate bytes had already been consumed.

Commit:

- `665bd051d52b1bfd29f7cca2fd32234c08ac2118`.

A consumed positive aggregate budget is now closed to additional files, while an intentionally configured zero-byte aggregate budget can still represent zero-byte-only staging under its own semantics.

---

# 64. Portable test result

After dependency, compile, analyzer, UTF-8, filename, and staging repairs:

- Core Release restore/build succeeded;
- **511/511 portable tests passed**;
- synthetic benchmark project compiled;
- localization validation passed;
- Apple integration metadata validation passed.

The benchmark compile is a source/build gate, not a claim of representative real-device throughput.

---

# 65. Android compile closure

After portable code was green, maintained platform CI exposed Android-specific compiler errors that had been hidden by earlier failures.

Focused commits:

- `128a0e218c26cba5d00b14f37387e1d811f6aa09` — multicast lock nullability;
- `16ef00079a6a1636388a1dce8669648b57fd0374` — foreground-service API/nullability;
- `40bb8b712cf461a528db0bf0f7a5bb2e3380cba8` — current Android intent/openable-column bindings and bounded shared-content staging;
- `3d5ce81fae53f52313c4579b6fd006ea0dc2d82d` / `addb068...` — multi-file picker nullable contract;
- `ebb94f8...` — shared XAML compile fix.

Android Release compile subsequently passed in the maintained platform matrix.

---

# 66. Apple target correction commits

Key Apple architecture commits:

- `8a3eb2ae23576c602709993c2162ca45f8021f06` — scope Share Extension to iOS;
- `9d2b209918cb2fe233f4732b6a972865bca393ed` — embed extension only in iOS app;
- `c96ceb8eca84efdb518a8b79256997b7148986d9` — remove stale Mac Catalyst extension entitlements;
- `af23c301c23b87f62e4e002aa88c8c9626e1a6e2` — align Apple validator with iOS-only extension.

Mac Catalyst remains a first-class application target, but its external intake is implemented through the containing desktop app/native drop rather than an unsupported extension target.

---

# 67. Apple simulator signing isolation

Hosted iOS simulator compilation must not require a private Apple Developer certificate/provisioning profile.

At the same time, real project entitlements must remain present for signed/device builds and static validation.

The final maintained approach confines signing/provisioning overrides to simulator CI command scope.

Relevant commits include:

- `e52593619eb2104ea83225c15166df4f21e0bfd7` — simulator extension signing handling;
- `5d5d982110b0ae532c3753ecbd2cca6e828ef48b` — simulator containing-app signing handling;
- `29375df8b7718e0ef8b7b77bb0b1883e3ee64dc3` — certificate-independent hosted simulator gate;
- `84e6b3e0c6f9fc4bf871e11d6691866ce1ca9a9d` — same release-readiness boundary.

Real App Group entitlements remain in source.

---

# 68. Windows compile closure

Windows required several layers of correction after the first true Windows source compile:

- runtime restore consistency;
- target-matrix isolation;
- iOS extension graph isolation;
- WinUI namespace disambiguation;
- WinRT data-package enum disambiguation;
- compile-versus-MSIX boundary.

Key commits:

- `7c1db757f8cb1d8daf9a9d06739f0881ddfc0a7b` — runtime identifier override mapping;
- `255451f8195116ae47bd73aecd742582679cca3b` — explicit `win-x64` restore/build;
- `786f2904b4e6f070d99616509dcac30fd29aefa0` — allow matching Windows runtime restore at final MAUI build;
- `46bc975f9832f748725436f16ab6fa5dbc8edff0` — optional iOS extension graph skip for non-Apple focused restore;
- `a5ee2e496664e284a7aecdab78c569c549923d23` — apply graph skip in Windows platform job;
- `efac3b490ba1beab1a0f49e72ee9bea1c3a31ca3` — mirror in release readiness;
- `f8a062b0b7d1ecf7cacb196a30a999f3f623f674` — single-TFM override hook;
- `d6ff6ff611147ab5bc1a5c1c23e0ab9f5fa4affc` / `41e20eb...` — force Windows-only target matrix;
- `18a41cd9e10adf23a2daf1057c39e738fcc4f9d3` — qualify WinUI activation/drag event types;
- `af4dfda0e5629a9c5ad9d52e33152102cd7a283e` — qualify WinRT data package operations;
- `a40aec014e9bad900ec0667293401f8478fb25d2` — separate hosted compile from MSIX packaging;
- `50c34fd445c10a283ddbc69eb6fbc04d1e2a912c` — mirror that boundary in release readiness.

Focused Windows Release compilation is green.

---

# 69. Platform workflow runtime isolation

Earlier platform-specific restores could overwrite shared Core assets with incompatible platform target state.

Commit:

- `7a4449fc2e517de05dc6a6a88dfe1f14b9af5718` — restore platform runtimes/shared Core explicitly.

Later commits refined Windows and Apple RIDs until the final platform matrix was stable.

---

# 70. Maintained platform workflow

The maintained `.github/workflows/platform-builds.yml` covers:

- Android Release app compile;
- focused unpackaged Windows Release app compile;
- Mac Catalyst containing-app Release compile;
- certificate-independent iOS Simulator Share Extension compile;
- certificate-independent iOS Simulator containing-app compile.

Mac Catalyst Share Extension compile is intentionally absent because there is no maintained Mac extension target.

---

# 71. Obsolete workflow retirement

The repository contained one-shot/self-modifying migration workflows that had already completed their purpose and could continue producing duplicate or invalid checks.

They were removed in focused commits:

- `8f2fd845a88bb98bfa67a053caef48c4707bc02d` — retire completed platform hardening workflow;
- `b28c24d16958745af0c18aa8e8f6149f6be331f7` — retire completed hardening workflow;
- `b275c0bb41737ff7c92fc2b04853ac49b1cbcfa7` — retire continuation finalizer;
- `ed67533dc65cc1cc9205215ac70b97005aeb4618` — retire script-mode migration workflow;
- `0c9166b2cc402361fd58a78e2017da7aa1bfe732` — remove duplicate platform smoke workflow.

Platform compilation now has one maintained workflow rather than duplicated stale variants.

---

# 72. Release-readiness workflow alignment

`.github/workflows/release-readiness.yml` mirrors maintained platform compile boundaries:

- portable Core/tests/localization/Apple metadata/benchmark;
- dependency inventories;
- Android compile;
- focused unpackaged Windows compile;
- Mac Catalyst containing-app compile;
- iOS Simulator extension/app compile with signing disabled at command scope;
- iOS Share Extension dependency graph;
- aggregate gate requiring each compile/test job to succeed.

Signed MSIX, signed Android packages, Apple device signing/App Group provisioning, notarization, and physical-device tests are deliberately not mislabeled as hosted CI passes.

---

# 73. Apple metadata validator

`scripts/validate_apple_integration.py` now validates the maintained architecture:

- exact App Group values;
- containing/extension bundle IDs;
- version/build parity;
- extension target exactly `net10.0-ios`;
- `IsAppExtension=true`;
- extension `Info.plist`;
- iOS extension entitlements;
- containing iOS entitlements;
- Mac Catalyst containing-app sandbox/App Group entitlement wiring;
- extension point/principal class;
- activation rule;
- iOS project reference;
- Core App Group constant;
- solution inclusion.

It does not pretend to create or validate Apple Developer provisioning profiles.

---

# 74. Automated platform evidence

Maintained Platform builds run:

- workflow run ID: **31768420086**;
- source/workflow head: `a40aec014e9bad900ec0667293401f8478fb25d2`;
- conclusion: **success**.

That run completed:

- Android job: success;
- focused Windows job: success;
- Apple job: success, including Mac Catalyst containing app, iOS Simulator Share Extension, and iOS Simulator containing app.

Later commits through the current ledger write are documentation-only; exact final release-candidate validation must still be run/frozen before a production tag.

---

# 75. Portable CI evidence

A later normal CI run on documentation/architecture head `b7eca57bf4020c35d500512028aa17863d528ccc` completed successfully:

- workflow run ID: **31769091180**;
- conclusion: **success**.

That gate includes:

- localization validation;
- Apple integration metadata validation;
- Core restore;
- Core Release build;
- full portable xUnit suite;
- benchmark-project compile.

The portable suite evidence remains **511/511 passing tests**.

---

# 76. CodeQL/security-hygiene evidence

On head `b7eca57bf4020c35d500512028aa17863d528ccc`:

- CodeQL workflow run **31769091211** — **success**;
- repository security-hygiene workflow run **31769091218** — **success**.

These are automated source/security checks, not a substitute for signed-binary review or physical platform testing.

---

# 77. Current dependencies and notices

Current direct runtime dependency surface documented in `THIRD_PARTY_NOTICES.md` includes:

`SwiftDrop.Core`:

- `Microsoft.Data.Sqlite` 10.0.10;
- `Microsoft.Extensions.Logging.Abstractions` 10.0.0;
- `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12.

`SwiftDrop.App`:

- `Microsoft.Maui.Controls` 10.0.90;
- `Microsoft.Extensions.Logging.Debug` 10.0.0;
- `QRCoder` 1.6.0.

The iOS Share Extension declares no arbitrary direct NuGet package of its own but references Core and uses the .NET/iOS target/runtime graph.

Exact transitive licenses/notices must still be reviewed against the restored signed release candidate.

---

# 78. Portable test coverage

Portable tests cover, among other areas:

- strict/canonical pairing query/Base64URL/whitespace behavior;
- identity/fingerprint/certificate policy;
- one-time authorization consume/replay/concurrency;
- strict UTF-8 and strict JSON;
- duplicate/unknown framed JSON members;
- typed request construction/validation;
- canonical manifest path validation;
- malformed path rejection before authorization consumption;
- transfer ID token syntax;
- complete file/batch/text/pair conversations;
- mutual-TLS loopback pinning/file/resume;
- source length/content mutation;
- staged corruption/integrity cleanup;
- regular source/symlink rejection;
- deterministic bounded folder enumeration;
- portable sender path deconfliction;
- UTF-8 filename bounds;
- collision marker preservation;
- stable batch IDs;
- completed-file retry validation;
- repeated completed-file verification after mutation;
- SQLite v0/v1/v2→v3 migrations;
- receive-root link/reparse rejection;
- destination reservation/final-promotion races;
- staging count/per-file/aggregate budgets;
- exact Apple package physical file sets;
- discovery fuzz/truncation/pointer loops;
- session drain/fault/cancellation races;
- privacy redaction;
- rune-safe UTF-8 text truncation.

---

# 79. Manual/security validation documents

Current release/testing documents include:

- `docs/testing/security-test-plan.md`;
- `docs/testing/manual-test-matrix.md`;
- `docs/release/release-checklist.md`;
- `docs/testing/performance-benchmarks.md`.

The manual matrix explicitly covers all supported cross-device directions and separately covers:

- pairing aliases/replay;
- canonical paths;
- source links;
- single-file integrity;
- stable batch resume;
- completed-item retry race;
- Android provider metadata/staging budgets;
- iOS Share Extension/App Group;
- Mac native drop;
- Windows activation/drop/picker;
- low storage;
- lifecycle/network changes;
- accessibility/localization.

---

# 80. Platform documentation synchronization

Current `docs/platform/integration-status.md` correctly states:

- Android share and foreground service are implemented in source;
- iOS has the dedicated Share Extension;
- Mac Catalyst uses containing-app/native drop and has no Share Extension target;
- Windows uses focused CI target isolation and native platform integration;
- release validation still requires signed/device testing.

No stale current-state Mac Catalyst Share Extension claim remains in this platform document.

---

# 81. Build documentation synchronization

`BUILDING.md` now documents:

- canonical solution and portable verification;
- Android build commands;
- focused Windows source-compile commands matching green CI;
- exact `WindowsPackageType=None` / `GenerateAppxPackageOnBuild=false` boundary;
- iOS-only Share Extension;
- Mac Catalyst containing app build;
- certificate-independent iOS simulator commands;
- Apple signed-device/App Group requirements;
- production validation boundary.

Commit:

- `7f8004c8446c689d9bf5cae53c7b49e9c3318ad0`.

---

# 82. Project-status synchronization

`PROJECT_STATUS.md` is updated to the fully green maintained platform matrix rather than the earlier temporary “Apple revalidation in progress” wording.

Commit:

- `33680cefa5a75acc28e62a8b8cb4e9d5cd9ffbec`.

It continues to classify the repository as:

**source-complete release-validation phase for the current master-prompt scope**.

---

# 83. README synchronization

Public README was aligned with:

- iOS-only Share Extension;
- Mac Catalyst native drop/no Mac extension;
- canonical pairing/path behavior;
- stable completed-item resume;
- staging budgets;
- current target compile gates;
- Apple provisioning boundary;
- source-complete versus production-validated distinction.

Relevant August 14 commit:

- `90e42d523a08d86d28769c4076306bd2c7b33432`.

---

# 84. Architecture/privacy/permissions synchronization

Focused documentation commits include:

- `45912e63af7808a509b44f336f20e452ece8b7ff` — architecture aligned to iOS-only extension and focused platform builds;
- `3c8cdbf39aa1518b731ac9fc619bfbcd76a474c6` — permissions/entitlements corrected for retired Mac extension;
- `f72bd0d6f13339a417488d4734fbd440b40070d1` — privacy aligned to external staging and iOS-only extension;
- `c5d10c701699cdb2e9f38edefc706270d77b3478` — clean architecture/MVVM boundaries refreshed;
- `b7eca57bf4020c35d500512028aa17863d528ccc` — architectural decisions recorded.

---

# 85. Security/release/license documentation synchronization

Focused commits include:

- `75ed1d6b77b4a83448aae522d8a920dc45fa36f8` — security test plan aligned with current platform architecture;
- `a60f2cb2fa174014468a34b3003103acc934e5c7` — stale Mac extension release gates removed;
- `54d3c857afad7d7003fdfa995b753653f1ae9b02` — manual matrix aligned with iOS-only extension;
- `45f08237601b089e1ae9b90814766d331e779b68` — third-party notice/dependency inventory refreshed;
- `f9a82a3962ee261936e2b28c1c53435e25805a65` — roadmap aligned with current release-validation architecture;
- `439c05ac64436306a076cff8ed316c5baa22de58` — August 14 changelog section added.

Historical changelog entries describing the earlier Mac Catalyst extension attempt remain historical records; the August 14 entry explicitly records the maintained iOS-only correction.

---

# 86. Threat/protocol documentation retained

August 12 protocol/security hardening documentation remains current and is not superseded by the August 14 build/platform pass.

Important documents:

- `docs/protocol/wire-format.md`;
- `docs/protocol/security.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/protocol/compatibility-matrix.md`.

They define/cover:

- canonical pairing representation;
- strict typed JSON;
- one-time authorization order;
- canonical `/` paths;
- filename bounds;
- source link/reparse handling;
- staging budgets;
- stable batch IDs;
- repeated completed-item verification.

---

# 87. Repository completion sweep

The maintained repository was swept for unfinished implementation markers.

No maintained-source implementation remained for:

- `TODO`;
- `FIXME`;
- `NotImplementedException`;
- placeholder implementation markers;
- stub implementation markers.

The Share Extension tree was also inspected directly and now contains only its iOS platform entitlement directory under `src/SwiftDrop.ShareExtension/Platforms`.

No maintained Mac Catalyst Share Extension target/entitlement file remains.

---

# 88. Repository workflow sweep

The workflow directory was reviewed after retiring one-shot helpers.

Maintained responsibility is now separated into normal CI/security/platform/release workflows rather than migration workflows that edit the repository themselves.

This prevents stale self-edit workflows from producing duplicate failures/check noise after their migration purpose has completed.

---

# 89. Current source-completion assessment

For the current master-prompt scope, repository source contains implementations for:

- local device identity/trust;
- discovery;
- QR/deep-link/nearby/code/manual pairing;
- canonical pairing capability encoding;
- mutual TLS and certificate pinning;
- strict closed-schema typed protocol;
- one-time authorization;
- single-file transfer;
- multi-file transfer;
- recursive folder transfer;
- explicit text transfer;
- selective receive;
- pause/cancel/resume;
- stable batch IDs;
- idempotent completed-file retry;
- second completed-item verification;
- source link/reparse safety;
- deterministic folder manifests;
- canonical cross-platform paths;
- bounded UTF-8 filenames/collisions;
- receive path/collision/capacity/integrity safety;
- SQLite trust/history/diagnostics/queue/resume metadata;
- Android share/lifecycle integration;
- Windows activation/folder/drop integration;
- iOS document intake;
- iOS Share Extension/App Group handoff;
- Mac Catalyst containing-app native drop;
- shared external staging budgets;
- settings/trust/history/queue/diagnostics/About UI;
- English/Hindi localization;
- portable regression/security tests;
- maintained multi-platform compile CI;
- release/security/build documentation.

The accurate classification is:

**source-complete for the current scope and undergoing release validation**.

---

# 90. Deliberate non-claims

SwiftDrop does not falsely claim that:

- arbitrary iOS/Android sockets survive OS suspension;
- extension-risk warnings are malware scanning;
- source App Group entitlements prove provisioning works;
- hosted iOS Simulator compile proves device signing/extension runtime works;
- unpackaged Windows compile proves signed MSIX behavior;
- source compile proves store readiness;
- portable provider tests prove every real provider behaves identically;
- application-level filesystem checks protect against a fully compromised OS/kernel;
- optional completion/failure system notifications exist on every target;
- synthetic benchmark compile equals representative real-network performance.

---

# 91. Optional post-v1 enhancements

Optional future work may include:

- native completion/failure notifications on Apple/Windows;
- additional supported/store-compliant background continuation;
- broader localization;
- representative-device performance dashboards/evidence;
- additional property/state-machine fuzzing;
- trustworthy platform malware-scanning integration only where a supported OS API exists.

These are optional enhancements rather than hidden missing correctness work for the current master-prompt source scope.

---

# 92. External Android release validation still required

Repository source/hosted compile cannot complete:

- private release keystore setup;
- signed AAB/APK generation;
- clean install/upgrade testing;
- Play Console policy/store validation;
- physical API/device range;
- real providers with normal/null/negative/wrong/changing size metadata;
- low-storage pressure during unknown-size provider copy;
- foreground-service behavior under real Android restrictions;
- notification permission transitions;
- multicast discovery on physical Wi-Fi;
- vendor battery-management behavior;
- TalkBack/large-text/Hindi/lifecycle testing.

---

# 93. External Windows release validation still required

Repository source/hosted compile cannot complete:

- real signing certificate configuration;
- signed MSIX/package generation;
- package install/update/uninstall;
- packaged protocol registration behavior;
- package capability behavior;
- Windows Firewall behavior;
- packaged receive FolderPicker persistence;
- packaged native drop;
- physical Windows→Android/iOS/Mac canonical folder interoperability;
- Narrator/keyboard/high-DPI/high-contrast testing.

---

# 94. External Apple release validation still required

Repository source/hosted simulator compile cannot complete:

- Apple Developer App Group creation/configuration;
- real provisioning profiles for `in.sanskar.swiftdrop` and `in.sanskar.swiftdrop.share`;
- signed iOS device build;
- Share Extension appearance/activation on physical devices;
- TestFlight/App Store embedding;
- real `NSItemProvider` timeout/cancellation/security-scoped behavior;
- App Group cold/warm handoff under signed sandbox;
- signed/notarized Mac Catalyst sandbox/network/App Group/native-drop behavior;
- macOS firewall/notarization/store checks;
- VoiceOver/large-text/Hindi behavior.

Mac Catalyst validation concerns the containing app/native drop; no Mac Catalyst Share Extension is expected.

---

# 95. Cross-device/network/filesystem validation still required

Physical matrix still must exercise supported directions between:

- Android;
- Windows;
- iOS;
- Mac Catalyst.

Cases include:

- QR/nearby/code/manual pairing;
- expired/replayed/canonical-alias rejection;
- certificate pin mismatch;
- small/zero-byte/large files;
- large multi/folder batches;
- canonical `/` paths;
- source symlink/reparse cases;
- receive-root symlink/reparse cases;
- selective receive/reject;
- pause/cancel/network interruption/resume;
- completed-item retry without duplicate copy;
- completed destination mutation between plan and zero-byte ACK;
- source mutation;
- staged corruption;
- collision pressure;
- low storage;
- guest Wi-Fi/client isolation;
- multicast-filtered LAN;
- IPv4/IPv6 combinations;
- network switching;
- sleep/lock/background transitions.

---

# 96. Secure storage/database physical validation still required

Real target environments still need validation for:

- SecureStorage/keychain/keystore locked/unavailable cases;
- operating-system upgrade/restore behavior;
- identity recovery behavior;
- real schema-v1/v2 database upgrade to v3;
- local metadata corruption/recovery;
- filesystem-specific symlink/reparse semantics on representative platforms.

---

# 97. Accessibility/localization physical validation still required

Manual release validation still needs:

- TalkBack;
- VoiceOver on iOS;
- VoiceOver on Mac Catalyst;
- Narrator;
- keyboard-only desktop operation;
- largest supported text scaling;
- high contrast;
- reduced motion;
- rotation/window resizing;
- Hindi clipping/wrapping/runtime messages;
- focus order and semantic label behavior.

Source support and catalogs do not replace these checks.

---

# 98. Dependency/legal/store validation still required

For the exact signed candidate:

- restore exact dependency graphs;
- inspect transitive packages/native components;
- review current advisories/vulnerabilities;
- verify package provenance;
- verify license/notice obligations;
- compare notices against actual signed binaries/packages;
- verify privacy declarations;
- verify local-network/foreground-service/App Group explanations;
- verify screenshots/descriptions/support links;
- verify signing/notarization;
- freeze exact commit/tag/release notes.

---

# 99. Exact-candidate release-readiness rule

The maintained source/platform-changing head has green portable/platform/security evidence.

However, documentation commits occurred after the latest platform-changing commit. Before a production release, identify the exact final candidate and run/observe all configured candidate gates, including `release-readiness`, against that candidate.

Do not infer success from a missing status context.

Missing/unreported status means:

**unknown/unreported**

not pass.

---

# 100. August 14 focused commit trail — dependency/Core/test recovery

Key commits:

- `2114c0f8a3d6a52dc705c576984b34e401406cd5` — patch SQLite dependency path;
- `595c814ac70b6131f616e7eb00b3532dc1150587` — canonical text limit;
- `508957cc029fadead36447a9e2b6749e7345086d` — request-factory manifest validator import;
- `03bd8ae7e00ec7268e077a714df2ceec12fc72c5` — request-validator manifest validator import;
- `c07f41a804e46abd462da3600d11510961b92fcb` — security SHA-256 hex parsing;
- `47b592e8577de3826442a877d53f65bb67761ada` — completion SHA-256 hex parsing;
- `dedfab99afd085ac3922edba60bee30da5752030` — xUnit global import;
- `2757f21cdb4e16eae3fa1683d8fcdb796deff4d3` — test constant alignment;
- `010bed418a8216e388f25547ccd63c0ce47829c1` — history pruning API;
- `43687ae8a20ab0757b47bf1203cd0167e2e7f3d6` — reservation analyzer assertion;
- `405d560c8c993115b4ff4d76ae4dbaf2f25d0d86` — filename analyzer assertion;
- `99c35c6c2ffd03307da74a9435f00ab6a2855f22` — filename sanitation idempotence;
- `46428536f80469e63645009e934976b285af4d4e` — strict UTF-8;
- `4c6af0950d291eccf879028d159de011e5965ee2` — JSON exception contract test;
- `665bd051d52b1bfd29f7cca2fd32234c08ac2118` — staging byte-budget exhaustion;
- `28a551247c172d2cbc0bcf7cc6992fcb72113531` — post-filter whitespace canonicalization.

---

# 101. August 14 focused commit trail — Apple/platform workflow repair

Key commits:

- `8a3eb2ae23576c602709993c2162ca45f8021f06` — iOS-only Share Extension;
- `9d2b209918cb2fe233f4732b6a972865bca393ed` — iOS-only app extension reference;
- `7a4449fc2e517de05dc6a6a88dfe1f14b9af5718` — explicit platform/shared-Core restores;
- `8f2fd845a88bb98bfa67a053caef48c4707bc02d` — remove completed platform hardening workflow;
- `b28c24d16958745af0c18aa8e8f6149f6be331f7` — remove completed hardening workflow;
- `b275c0bb41737ff7c92fc2b04853ac49b1cbcfa7` — remove finalizer workflow;
- `ed67533dc65cc1cc9205215ac70b97005aeb4618` — remove script-mode migration workflow;
- `0c9166b2cc402361fd58a78e2017da7aa1bfe732` — remove duplicate platform smoke workflow;
- `7deb57efd63761907833470a064741f7e678ce28` — release gate aligned to maintained platforms;
- `af23c301c23b87f62e4e002aa88c8c9626e1a6e2` — Apple metadata validator corrected;
- `c96ceb8eca84efdb518a8b79256997b7148986d9` — stale Mac extension entitlement removed.

---

# 102. August 14 focused commit trail — Android/UI

Key commits:

- `128a0e218c26cba5d00b14f37387e1d811f6aa09` — Android multicast lock nullability;
- `16ef00079a6a1636388a1dce8669648b57fd0374` — foreground service API/nullability;
- `40bb8b712cf461a528db0bf0f7a5bb2e3380cba8` — Android intent/share bindings and staging;
- `3d5ce81fae53f52313c4579b6fd006ea0dc2d82d` — nullable picker result filtering;
- `ebb94f8fc427a417cba788b51027439940e64773` — remove invalid Entry line-break property;
- `d345a9bc49ee1555f996984eb68965e38303ab51` — localization XAML service-provider warning removal;
- `addb068add8b18d4ea2ec05b2eb695c3c632e39d` — cancelled/null multi-file picker result.

---

# 103. August 14 focused commit trail — Windows

Key commits:

- `7c1db757f8cb1d8daf9a9d06739f0881ddfc0a7b` — Windows RID override mapping;
- `4cc52b2766d2e7d83f76f8c3aa09e04dfc336906` — exact Windows/Apple runtime targets;
- `255451f8195116ae47bd73aecd742582679cca3b` — explicit `win-x64` restore/build;
- `786f2904b4e6f070d99616509dcac30fd29aefa0` — allow final Windows build runtime restore;
- `46bc975f9832f748725436f16ab6fa5dbc8edff0` — optional iOS extension graph skip;
- `a5ee2e496664e284a7aecdab78c569c549923d23` — Windows workflow graph skip;
- `efac3b490ba1beab1a0f49e72ee9bea1c3a31ca3` — release workflow graph skip;
- `f8a062b0b7d1ecf7cacb196a30a999f3f623f674` — focused target override;
- `d6ff6ff611147ab5bc1a5c1c23e0ab9f5fa4affc` — Windows-only platform matrix;
- `41e20eb1bfc20bc8e3ca634bd60d3f6e1f20db5a` — Windows-only release matrix;
- `18a41cd9e10adf23a2daf1057c39e738fcc4f9d3` — WinUI event type disambiguation;
- `af4dfda0e5629a9c5ad9d52e33152102cd7a283e` — WinRT data operation disambiguation;
- `a40aec014e9bad900ec0667293401f8478fb25d2` — unpackaged hosted compile boundary;
- `50c34fd445c10a283ddbc69eb6fbc04d1e2a912c` — same release-readiness boundary.

---

# 104. August 14 focused commit trail — servicing/docs

Key commits:

- `21a7b68a9a5de8ff41c49acd3b3b0e7807d42be4` — MAUI Controls 10.0.90;
- `90e42d523a08d86d28769c4076306bd2c7b33432` — public README platform alignment;
- `358072d9f893473523c8f41450c9046a710a4b85` — build docs platform alignment;
- `54d3c857afad7d7003fdfa995b753653f1ae9b02` — manual matrix platform alignment;
- `ffbf4c0260f5107fd8506c9e2db8a8dae706099b` — platform status correction;
- `a60f2cb2fa174014468a34b3003103acc934e5c7` — release checklist correction;
- `f9a82a3962ee261936e2b28c1c53435e25805a65` — roadmap correction;
- `439c05ac64436306a076cff8ed316c5baa22de58` — changelog update;
- `c07c3a00cce52b47dab27da63b235b45fa8ddc1f` — project status synchronization;
- `75ed1d6b77b4a83448aae522d8a920dc45fa36f8` — security test documentation;
- `45f08237601b089e1ae9b90814766d331e779b68` — dependency/license documentation;
- `45912e63af7808a509b44f336f20e452ece8b7ff` — architecture synchronization;
- `3c8cdbf39aa1518b731ac9fc619bfbcd76a474c6` — permission/entitlement synchronization;
- `f72bd0d6f13339a417488d4734fbd440b40070d1` — privacy synchronization;
- `c5d10c701699cdb2e9f38edefc706270d77b3478` — clean architecture/MVVM documentation;
- `b7eca57bf4020c35d500512028aa17863d528ccc` — architecture decision record;
- `33680cefa5a75acc28e62a8b8cb4e9d5cd9ffbec` — replace stale platform status with fully green matrix evidence;
- `7f8004c8446c689d9bf5cae53c7b49e9c3318ad0` — exact green Windows local-build boundary.

---

# 105. August 12 hardening retained

The August 14 continuation builds on, and does not replace, the August 12 hardening already recorded in history.

Retained major areas include:

- canonical raw pairing query/Base64URL representation;
- whitespace alias rejection;
- strict portable relative paths;
- `/` wire separator on all platforms;
- path validation before authorization consumption;
- canonical filename sanitation;
- UTF-8 filename byte bound;
- collision marker preservation;
- regular-source/link rejection;
- deterministic bounded folder traversal;
- portable sender deconfliction before hashing;
- batch path preflight;
- stable transfer ID syntax;
- paused file/folder source filtering;
- obsolete fresh-ID handler/compatibility removal;
- shared external staging budget;
- Android unknown-size storage reserve;
- Apple provider timeout semantics;
- exact Apple package file-set validation;
- aggregate Apple import capacity preflight;
- Mac provider timeout/budget handling;
- second completed-item verification.

The cumulative repository state must be evaluated, not only the latest commits.

---

# 106. Representative August 12 commit trail retained

Examples of important August 12 commits still forming the current security/correctness baseline:

- `27161b7e57f24b3b61205347ef474150a33da582` — canonical Base64URL pairing query;
- `f8120cfc2858745bd1a79450cf07f18f3416bbda` — whitespace-wrapped pairing rejection;
- `fec05363e41ed6e7938bbfc90b0b92955132953c` — strict portable relative path parser;
- `54914b48287397a581617f94007773746ad4c0d3` — path nesting bound;
- `5586d70ba574f9637fd4e600cc083789cec8cc07` — manifest path validation before authorization;
- `6fa77d75a24431b94d28c2c40d58232d0edc7320` — canonical sanitized wire paths;
- `d10835f9617698647dd90e4cb6530c579f946081` — forward-slash canonical relative paths;
- `23eb74e0832e027b357c68788fc59d32cd2a04d1` — canonical batch deconfliction;
- `629b5f0fadb4d78fd2fb7e144de1bbdbacb1265a` — shared staging budget;
- `1615480b9c6231c97be4133b08a873d61438b31d` — Apple aggregate pre-copy budget;
- `7c860aec2b4684a01a55ea8fa97339c01ad6f4c0` — Android aggregate staging budget;
- `5a4649ce157b28851b38e2dc362a3ea734d57b30` — Mac shared staging budget;
- `1edaa69dd5fa40c1112b04af7c25e35f5eaa5632` — bounded link-safe deterministic folder enumeration;
- `413d2bee68ab8cbfa0ab803df53db9a260e42508` — regular source validation;
- `5304acf5121cff72a151acd05e479e88520c072b` — stream-open source revalidation;
- `86253c4e64ac8bc63c5574771454234c3500719e` — sender portable path deconfliction;
- `445992173c56cd684ad5f8ce7128639924cb9cb8` — UTF-8 filename bounds;
- `5b3610824787ea08aaa6ae454b3d18e3d01afc96` — collision marker preservation;
- `bbf59e2a122e768debbbcb2ba3c6e55755c1537c` — canonical batch transfer ID syntax;
- `e41fb03eac2f57fdfc7caf156dff5104430c287c` — remove obsolete batch handlers/harden single resume;
- `078aa9a718881bb61985f8ad4cc5f2a50fcc9059` — remove implicit fresh-ID compatibility overload;
- `0fc8b50c97600574f10db33dbdf0333fd92a1022` — preserve Android unknown-size storage reserve.

---

# 107. Git commit identity handling

The GitHub Contents API available in this session allows commit messages but does not expose an independent author/committer-email override field for these focused file writes.

Where commit-message control is available, commits use:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

This records the requested mailbox in the commit message trailer without falsely claiming the connector changed GitHub account-level commit authorship metadata.

---

# 108. Current documentation set reviewed

Current-state repository documents reviewed/aligned during the August 14 completion pass include:

- `README.md`;
- `BUILDING.md`;
- `PROJECT_STATUS.md`;
- `NEXT_STEPS.md`;
- `CHANGELOG.md`;
- `PRIVACY.md`;
- `THIRD_PARTY_NOTICES.md`;
- `DECISIONS.md`;
- `docs/architecture.md`;
- `docs/architecture/clean-architecture.md`;
- `docs/platform-permissions.md`;
- `docs/platform/integration-status.md`;
- `docs/protocol/wire-format.md`;
- `docs/protocol/security.md`;
- `docs/protocol/compatibility-matrix.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/testing/security-test-plan.md`;
- `docs/testing/manual-test-matrix.md`;
- `docs/testing/performance-benchmarks.md`;
- `docs/storage/database-schema.md`;
- `docs/release/release-checklist.md`;
- this `what_changed.md` ledger.

The current-state documentation uses the iOS-only extension architecture and keeps signed/device/store gates explicit.

---

# 109. Definition of completion used by this ledger

**Implemented in source** means the repository contains the behavior/policy and associated integration code.

**Portable-tested** means portable regression/integration tests exist and, for the stated August 14 evidence, the full portable suite executed successfully.

**Hosted-platform compiled** means the appropriate GitHub hosted runner/workload compiled the maintained target source successfully.

**Signed/device validated** requires a real signed package/application on the relevant target environment with its real entitlements/capabilities/providers/filesystem/network behavior.

**Production verified** additionally requires the full cross-device, release, accessibility, localization, dependency/license, privacy/store, signing/notarization, and exact-candidate evidence.

---

# 110. Current engineering phase

The repository has moved from broad feature implementation to:

**release-candidate validation and defect closure**.

The highest-value next work is not inventing duplicate feature implementations. It is:

1. freeze exact release candidate;
2. run/observe all candidate workflows including release readiness;
3. sign/package each target;
4. configure/verify Apple App Group provisioning;
5. execute physical cross-device matrix;
6. exercise real Android/Apple providers;
7. exercise low-storage/network/lifecycle/filesystem races;
8. validate accessibility/localization;
9. review exact signed dependency/license graph;
10. align store declarations and publish only after those gates pass.

---

# 111. Final repository-write rule for this continuation

This August 14 `what_changed.md` update is intentionally the final planned repository content write of the current continuation after source/platform/workflow/documentation reconciliation.

After this ledger write, the next operation is status/evidence inspection only.

If a newly triggered automated check exposes an actual defect, that defect must be fixed and the ledger updated again afterward; otherwise this file represents the frozen current repository engineering state.

---

**Made by the Sanskar**

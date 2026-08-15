# What changed

Date: 2026-08-15
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

---

# 112. August 14 continuation resumed after ledger freeze

Section 111 recorded the then-intended final repository write. The user explicitly instructed SwiftDrop work to continue, requested complete code, requested maximum practical focused commits, and explicitly required `what_changed.md` to be updated again. This section supersedes only section 111's “final planned write” statement; all earlier ledger history remains preserved.

# 113. Maintained GitHub Actions runtime modernization

All maintained workflows were audited. Deprecated action majors were replaced without changing product behavior:

- Core CI: `actions/checkout@v7`, `actions/setup-dotnet@v6`.
- CodeQL: checkout v7, setup-dotnet v6, `github/codeql-action@v4`.
- Security hygiene: checkout v7.
- Platform matrix: checkout v7/setup-dotnet v6 on Android, Windows, and Apple jobs.
- Release readiness: checkout v7/setup-dotnet v6 on all compile/test jobs.

Focused commits:
- `f5a31f68` — core workflow action runtime refresh.
- `7594cfa5` — CodeQL maintained-major refresh.
- `dc1ef7cb` — security-hygiene checkout refresh.
- `8bcb4e04` — platform workflow action refresh.
- `a2cd424f` — release-readiness action refresh.

Repository searches confirmed no remaining checkout v4, setup-dotnet v4, or CodeQL v3 references in maintained source.

# 114. Explicit direct/transitive NuGet vulnerability policy

Commit `78fc3d68` made the restore policy explicit in `Directory.Build.props`:

- `NuGetAudit=true`
- `NuGetAuditMode=all`
- `NuGetAuditLevel=low`

`TreatWarningsAsErrors=true` remains repository-wide, so low/moderate/high/critical NuGet audit warnings stay verification-blocking unless an intentional reviewed exception is separately documented rather than silently suppressed.

# 115. Machine-readable release dependency and vulnerability evidence

Commit `edb545b3` extended release readiness to emit JSON package evidence for SwiftDrop.Core, portable tests, synthetic benchmarks, and the iOS Share Extension target. For each relevant project, the workflow captures the complete transitive package graph and vulnerable-package view where applicable.

Commit `04cb5a11` additionally exercises the Core vulnerable-package JSON command on every regular CI run and parses the output as JSON, preventing release-only audit command drift. CI run `31773580594` passed this validation.

# 116. Build, release, and contributor documentation synchronization

Commit `2e85b166` documented the enforced NuGet audit boundary, local JSON audit commands, and release-readiness evidence in `BUILDING.md`.

Commit `6526a701` corrected the stale contributor restore target from `SwiftDrop.sln` to the canonical `SwiftDrop.slnx` and aligned contribution guidance with maintained portable verification scripts, platform-specific build commands, dependency-audit review, localization/Apple metadata invariants, CodeQL/repository hygiene, and honest compile-versus-signed-release boundaries.

The release checklist was also moved from the legacy `dotnet list package --vulnerable` spelling to the .NET 10 noun-first `dotnet package list --project <project> --include-transitive --vulnerable --format json` evidence command.

# 117. .NET 10 test-toolchain modernization

Focused test-only dependency commits:

- `551be910` — `xunit.runner.visualstudio` 3.0.2 -> 3.1.5.
- `734f75ce` — `coverlet.collector` 6.0.4 -> 10.0.1.
- `c3b18381` — `Microsoft.NET.Test.Sdk` 17.13.0 -> 18.8.1.

`xunit` remains 2.9.3, preserving the existing test API while modernizing host/runner/coverage tooling. `THIRD_PARTY_NOTICES.md` was synchronized in commit `b2d2506c`. Equivalent Dependabot PRs #10, #3, and #8 were closed after the signed updates were applied and validated on `main`.

# 118. Fresh portable verification after test-tool updates

CI run `31773452371` completed successfully after the combined test-tool modernization.

Exact test result:
- Failed: 0
- Passed: 511
- Skipped: 0
- Total: 511

The same run also completed Core Release build with zero warnings/errors, localization validation, Apple integration metadata validation, and benchmark Release build.

# 119. Fresh CodeQL and security-hygiene evidence

CodeQL run `31773251979` completed successfully using checkout v7, setup-dotnet v6, and CodeQL v4.

Security-hygiene run `31773251972` completed successfully using checkout v7 and retained private signing/local database artifact rejection, embedded private-key block rejection, and required security-document checks.

# 120. Fresh platform evidence after action/audit hardening

Platform build run `31773145276` completed successfully after the explicit NuGet audit/action-runtime hardening.

Verified successful targets:
- Android Release app compile.
- focused Windows Release app compile without MSIX packaging.
- Mac Catalyst containing-app Release compile.
- iOS Simulator Share Extension Release compile.
- iOS Simulator containing-app Release compile.

The Apple simulator builds remain certificate-independent only at CI command scope; real project entitlements remain in source for signed/device builds.

# 121. Current completion boundary after this continuation

Source/hosted verification is current for 511/511 portable tests, benchmark compile, localization and Apple metadata validators, CodeQL v4, repository security hygiene, direct/transitive NuGet audit enforcement, machine-readable vulnerability-report validation, and Android/Windows/Mac Catalyst/iOS Simulator compile coverage.

SwiftDrop must still not be called production-ready until the existing external gates are completed for an exact release candidate: signed Android/Windows/Apple artifacts; physical Android/iOS/device-to-device transfer matrix; Apple App Group provisioning and Share Extension runtime behavior; signed Windows MSIX install/update/protocol/capability behavior; signed Mac Catalyst sandbox/notarization behavior; real-network discovery/resume/firewall/low-storage tests; accessibility/localization checks on actual target devices; final dependency/license/provenance review; and store/privacy metadata/publication checks.

The temporary continuation ledger helper is intentionally removed after a successful write so no stale self-edit workflow or script remains.

---

# 122. Complete documentation pass requested and executed

The user explicitly requested complete project documentation, all repository work pushed to the `main` branch, focused commit messages, and use of `sanskarin@outlook.in` if commit identity configuration was needed. The continuation therefore audited the existing documentation surface against current source/platform/CI state before adding missing canonical guides.

No source feature was invented solely to make documentation appear complete. Documentation continues to distinguish implemented source, portable-tested behavior, hosted-platform compilation, and signed/device/store validation.

# 123. Canonical documentation index

Commit `f275573c` added `docs/README.md` as the canonical documentation navigation point.

Commit `ff4229d1` expanded that index after the rest of the documentation set landed so installation, user workflow, settings, diagnostics, networking, project structure, CI, versioning, and release-process material is discoverable rather than orphaned.

# 124. End-user documentation

Focused commits added:

- `a97a1e70` — complete end-user guide covering discovery, pairing, single/multi/folder/text transfer, approval, resume, Android sharing, iOS Share Extension, Mac native drop, Windows drag/drop/receive folder, queue/history/trust/diagnostics, privacy, and safety boundaries.
- `2a289046` — settings reference derived from the maintained Settings view model/XAML and `AppSettings.Default`, including concurrency 1-8, retention 0-3650, platform notification/receive-folder differences, identity reset, privacy, trust, themes, languages, and developer diagnostics.
- `efbbe97d` — comprehensive FAQ aligned to the maintained local-only protocol, platform targets, iOS-only Share Extension, integrity/resume, settings, CI/release boundaries, and support channels.
- `c8cc520c` — installation/source-run guide that explicitly avoids presenting hosted unsigned compile artifacts as official signed releases.
- `86b66a09` — networking/firewall guide covering mDNS/DNS-SD, bounded UDP fallback, TCP 47821, UDP 47822, guest/client isolation, local address scope, Windows/macOS firewall, Apple local-network privacy, Android multicast behavior, VPNs, IPv4/IPv6, and diagnostic boundaries.
- `b99745c0` — expanded troubleshooting guide covering local discovery/connection, strict pairing, fingerprint mismatch, trust, integrity/resume, collisions/path safety, storage, Android/iOS/Mac/Windows intake, App Group, localization validators, NuGet audit, target builds, and CI-versus-device failures.
- `0a6aa886` — privacy-safe diagnostics and bug-report guide.

# 125. Developer and architecture documentation

Focused commits added:

- `7f6f9d54` — repository/project-structure guide for `SwiftDrop.Core`, `SwiftDrop.App`, iOS-only `SwiftDrop.ShareExtension`, tests, benchmarks, scripts, workflows, docs, resources, platform boundaries, and dependency direction.
- `61d989d7` — development workflow guide covering prerequisites, portable verification, NuGet audit, layer selection, protocol/path/resume/persistence/UI/platform changes, testing levels, CI, commit style, PR expectations, documentation ownership, and definition of done.
- `8bd3d849` — CI/verification reference documenting the five maintained workflows, their exact evidence boundaries, repository-wide NuGet audit policy, local equivalents, candidate discipline, and August 14 verified hosted evidence.
- `091ad581` — versioning/compatibility policy covering application/protocol/schema/trust/batch-resume/platform/settings/dependency/localization compatibility and fail-closed legacy-state handling.
- `da3897fa` — end-to-end release process from exact candidate freeze through automated gates, dependency/license review, signing, signed artifacts, physical matrix, platform provider intake, accessibility/localization, privacy/store review, tagging/submission, and post-release verification.

# 126. Community, support, security, and legal documentation

Focused commits expanded:

- `09ed02a8` — `SUPPORT.md`, linking complete user/developer troubleshooting and safe report guidance.
- `d7d2e4f3` — `CONTRIBUTING.md`, adding security/privacy/layer/dependency/protocol/persistence/platform/test/docs/PR requirements and the requested sign-off format.
- `f524247d` — original SwiftDrop Code of Conduct expanded with expected/unacceptable behavior, security/privacy handling, technical disagreement rules, maintainer responsibilities, reporting, scope, and good-faith enforcement.
- `3192df73` — `SECURITY.md`, removing stale pre-1.0 wording and adding current source/release boundary, private reporting scope, security-sensitive examples, cryptography/endpoint/secret/dependency policies, responsible testing, and regression expectations.
- `3397e85c` — `TERMS.md`, clarifying local-transfer responsibility, authorization, received-file trust, source/unofficial package boundaries, privacy, support diagnostics, third-party services, downstream forks, and Apache-2.0 precedence.

# 127. Documentation source-truth audit

Repository searches during this pass found no remaining indexed `TODO` documentation placeholders, old `469/469` portable-test marker, legacy `dotnet list package` audit spelling, obsolete `SwiftDrop.sln` solution reference, or maintained Mac Catalyst Share Extension wording.

The audit also found `SECURITY.md` still referred to “pre-1.0 development” even though the current project source declares display version `1.0.0`; that stale wording was corrected without falsely claiming that a signed 1.0.0 production release has already passed the release process.

# 128. Documentation ownership map

The completed documentation set now has canonical ownership:

- public overview -> `README.md`;
- navigation -> `docs/README.md`;
- installation/source-run -> `docs/installation.md`;
- end-user workflow -> `docs/user-guide.md`;
- settings -> `docs/configuration.md`;
- FAQ -> `docs/faq.md`;
- troubleshooting -> `docs/troubleshooting.md`;
- safe diagnostics/bug reporting -> `docs/diagnostics-and-bug-reports.md`;
- network/firewall -> `docs/networking.md`;
- build -> `BUILDING.md`;
- development/contribution -> `docs/development-guide.md` + `CONTRIBUTING.md`;
- architecture/project boundaries -> `docs/architecture.md`, `docs/architecture/*`, `DECISIONS.md`;
- protocol/security/compatibility -> `docs/protocol/*`, `docs/security/THREAT_MODEL.md`, `SECURITY.md`;
- platform permissions/status -> `docs/platform-permissions.md`, `docs/platform/integration-status.md`;
- local data/privacy -> `docs/storage/database-schema.md`, `PRIVACY.md`;
- CI/testing -> `docs/testing/*`;
- release/signing/store -> `docs/release/*`;
- version compatibility -> `docs/versioning-and-compatibility.md`;
- support/community/legal -> `SUPPORT.md`, `CODE_OF_CONDUCT.md`, `TERMS.md`, `LICENSE`, `NOTICE`, `THIRD_PARTY_NOTICES.md`;
- current engineering evidence -> `PROJECT_STATUS.md`, `NEXT_STEPS.md`, `CHANGELOG.md`, `what_changed.md`.

# 129. Completion boundary after documentation pass

The repository now contains a complete maintained documentation surface for the implemented source and release process. This does not remove external release gates.

The next required production work remains exact-candidate signed Android/Windows/Apple packaging, Apple App Group provisioning, physical cross-device/network/provider/storage/lifecycle testing, Windows protocol/package/firewall validation, Mac sandbox/notarization validation, accessibility/localization checks, exact dependency/license provenance review, and store/privacy submission checks.

Any source changes made while closing those external gates must update the affected canonical documentation and create a new exact candidate before production readiness is claimed.

---

# 130. Permanent documentation integrity gate

The documentation completion pass was converted from a one-time editorial exercise into a maintained automated contract.

Focused commits:

- `14b6c980` — added `scripts/validate_documentation.py`.
- `aa4cc015` — regular CI runs documentation integrity before localization/Apple/Core checks.
- `7f13b4a6` — documented the new gate in the CI reference.
- `c838ac52` — Linux/macOS `verify-core.sh` runs the documentation validator.
- `1ffeee38` — Windows PowerShell `verify-core.ps1` runs the documentation validator.
- `efa24732` — `BUILDING.md` documents the validator/local verification contract.
- `08040d5d` — release readiness now uses the canonical portable verification entry point, so release-candidate portable validation includes the same documentation check instead of duplicating drift-prone validator commands.

The validator requires the canonical user/developer/architecture/protocol/platform/storage/testing/release documents, checks that principal guides are indexed, resolves local inline Markdown links/images, requires the public README to link the canonical docs index, and rejects completed one-time documentation helpers that are explicitly forbidden.

CI run `31778543950` completed successfully with the new documentation gate plus localization, Apple integration metadata, Core Release build, 511/511 portable tests, benchmark Release build, and machine-readable vulnerability-audit validation. CI run `31778749428` revalidated the integrated build-documentation state.

# 131. Community contribution and issue workflow alignment

The repository's GitHub community templates were brought to the same standard as the completed documentation set.

Focused commits:

- `b76e26e2` — expanded pull-request template with compatibility, exact verification, security/privacy, dependency/license, accessibility/localization, platform, documentation, and remaining signed-device validation sections.
- `fae0c2ec` — expanded non-security bug report template with exact version/commit, sender/receiver, affected area, network/pairing context, reproducible steps, expected/actual result, sanitized diagnostics, and security-data confirmations.
- `30e860c5` — expanded feature-request template with product problem, platform/area, security/privacy, compatibility, alternatives, and validation requirements.
- `78594321` / `57d9d5ea` — added and refined issue contact routing so documentation, general support, and security disclosure point to the canonical repository policies instead of encouraging unsuitable blank/public issues.

# 132. Technical glossary added to the canonical documentation contract

Commit `4aa85e49` added `docs/glossary.md` covering project-specific terms such as App Group, canonical representation, pairing capability, certificate fingerprint, discovery, external staging, completed-item reuse, receive root, resume metadata, signed/device validation, stable transfer ID, strict JSON, trusted device, and production-ready.

Commit `809cb2b8` linked the glossary from the canonical docs index and documented terminology maintenance ownership.

Commit `7042345b` made the glossary a required/indexed file in `validate_documentation.py` and extended the temporary-helper absence checks used during this finalization sequence.

# 133. QRCoder 1.8.0 dependency completion

The only remaining open Dependabot update at this stage was QRCoder 1.6.0 -> 1.8.0.

Focused commits:

- `9f4d6018` — updated `SwiftDrop.App` to QRCoder 1.8.0.
- `6a9e8b09` — synchronized `THIRD_PARTY_NOTICES.md`.

Verification for the source-changing dependency commit:

- CI run `31778661754` — success.
- CodeQL run `31778661766` — success.
- Security hygiene run `31778661731` — success.
- Platform run `31778661776` — success across Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing app.

Dependabot PR #9 was then closed without merging because the equivalent signed update had already been applied directly to `main`. A repository queue check returned no open pull requests and no open issues.

# 134. Release-readiness verification path simplified

Commit `08040d5d` changed `release-readiness.yml` so its portable job calls `./scripts/verify-core.sh` as the canonical portable source/documentation verification entry point instead of separately repeating localization/Apple validators and then re-running them through the script.

The release workflow still captures machine-readable direct/transitive dependency and vulnerability reports, compiles the synthetic benchmark harness, compiles Android/Windows/Apple target paths, and keeps the explicit final message that signed Windows MSIX, physical-device testing, Apple signing/notarization/App Group provisioning, Share Extension runtime behavior, and store checks remain mandatory.

# 135. Documentation and source completion boundary after this continuation

The repository now has:

- a complete navigable documentation surface for users, contributors, architecture, protocol/security, platform integration, networking, settings, storage/privacy, diagnostics, CI/testing, versioning, signing, release process/checklist, support/community/legal policies, and the detailed engineering ledger;
- automated documentation integrity enforcement in normal/local/release portable verification;
- strengthened GitHub contribution/issue routing templates;
- no open pull requests or issues at the completion check;
- the current QRCoder dependency update validated across the maintained hosted target matrix;
- the existing 511-test portable correctness/security suite and NuGet/CodeQL/security-hygiene gates retained.

This is the end of source/documentation completion work that can be truthfully proven from the repository and hosted CI alone. SwiftDrop must still not be described as production-ready until an exact release candidate passes the already documented external gates: real signing and distribution packaging; physical Android/iOS/device-to-device transfers; Apple App Group and iOS Share Extension runtime validation; Windows MSIX install/update/protocol/firewall validation; Mac Catalyst signed sandbox/notarization validation; real restricted-network/lifecycle/low-storage/provider tests; accessibility/localization checks on actual targets; final dependency/license/provenance review of signed artifacts; and store/privacy publication checks.

---

# 136. Machine-readable NuGet vulnerability evidence is enforced

This continuation replaced a weak evidence assumption — “the JSON command ran and produced parseable JSON” — with explicit finding validation.

Focused commits:

- `562fc0d7` — added `scripts/validate_nuget_vulnerability_report.py`.
- `003151ef` — added vulnerability-report validator regression tests.
- `874238b0` — wired explicit finding validation into normal CI.
- `1940907d` — pinned Python 3.13 for the normal validation gate.

The validator accepts UTF-8/UTF-8-BOM reports, requires a top-level JSON object, recursively examines direct/transitive package structures for non-empty `vulnerabilities` arrays, reports package/version/severity/advisory fields when available, and fails malformed vulnerability shapes. Exit status distinguishes clean reports, reported vulnerabilities, and malformed/report failures.

Machine-readable commands now explicitly request `--format json --output-version 1`; vulnerable views include transitive packages. Repository-wide NuGet restore auditing in `Directory.Build.props` remains a separate warnings-as-errors gate.

# 137. Local portable verification now enforces audit evidence on Bash and PowerShell

Focused commits:

- `b551fa97` — Unix `verify-core.sh` now runs helper tests and validates a temporary Core vulnerable-package report with automatic cleanup.
- `f9d5e80c` — PowerShell verification gained explicit native-command exit checking, helper tests, and Core vulnerable-package report validation.
- `e858fc4a` — normal CI gained a Windows runner job that executes the PowerShell verifier.
- `080126a0` — fixed the PowerShell parser defect exposed by that new Windows job by delimiting `${LASTEXITCODE}` before a colon.

The initial Windows verifier run `31784473076` failed at parse time on the original `$LASTEXITCODE:` string. The gate was retained and the script was fixed rather than weakening/removing the Windows validation path.

# 138. Shipped target dependency graphs are now audited by maintained platform CI

Commit `e364d402` extended `platform-builds.yml` so hosted target jobs generate direct/transitive package JSON plus vulnerable-package JSON for:

- Android `SwiftDrop.App`;
- focused Windows `SwiftDrop.App`;
- Mac Catalyst `SwiftDrop.App`;
- iOS Simulator `SwiftDrop.App`;
- iOS Simulator `SwiftDrop.ShareExtension`.

The target vulnerable-package reports are passed through the same explicit finding validator used by portable verification. This closes the earlier evidence gap where release tooling had stronger portable/extension inventory coverage than ordinary shipped app target graphs.

# 139. Dependency evidence bundles now have deterministic SHA-256 manifests

Focused commits:

- `6e3f8e98` — added `scripts/create_dependency_evidence_manifest.py`.
- `22476296` — added manifest generator regression tests.
- `335686ac` — platform audit bundles now include deterministic manifests.
- `9901c7c0` — release-readiness audit bundles now include deterministic manifests.

Schema version 1 records each evidence JSON file's relative POSIX path, exact byte length, and lowercase SHA-256 digest in stable path order. The generator rejects an output outside the evidence root, excludes the manifest from its own file list, and fails an empty evidence root.

Platform run `31783405975` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app compile/audit jobs. It uploaded:

- `android-dependency-audit`;
- `windows-dependency-audit`;
- `apple-dependency-audit`.

The downloaded Android bundle contained `packages.json`, `vulnerabilities.json`, and `manifest.json`; every listed byte length and SHA-256 was independently recomputed and matched. The Windows bundle passed the same independent check. The Apple bundle contained six report files under `maccatalyst/`, `ios-app/`, and `ios-share-extension/`; all six independently matched its root manifest.

These manifests are integrity aids, not signatures or provenance attestations.

# 140. Release-readiness now self-validates audit/evidence changes

Focused commits:

- `462c4ae3` — extended release readiness with shipped-platform dependency evidence.
- `9901c7c0` — added hashed evidence manifests to release artifacts.
- `b050fcf5` — added main/pull-request self-test triggers for release workflow/verification/audit/evidence helper changes while keeping all `v*` tag pushes as candidate triggers.

Release-readiness self-test run `31783537853` completed successfully. It passed:

- canonical portable verification;
- portable Core/test/benchmark dependency reports and manifest;
- Android compile/audit/upload;
- focused Windows compile/audit/upload;
- Mac Catalyst compile/audit;
- iOS Simulator Share Extension compile;
- iOS Simulator containing-app compile;
- iOS app/extension dependency audits and Apple evidence upload;
- final aggregate `release-gate`.

The aggregate gate still states that signed Windows MSIX, physical-device testing, Apple signing/notarization/App Group provisioning, Share Extension behavior, and store checks remain mandatory.

# 141. Dependency evidence has a canonical release contract

Focused documentation commits:

- `24e39417` — added `docs/release/dependency-evidence.md`.
- `b1d9224e` — linked it from the canonical documentation index.
- `084ec25c` — made the dependency-evidence document required by documentation validation.
- `85ef1535` — expanded the CI reference with helper tests, target audits, manifests, stable JSON schema, local equivalents, and evidence limitations.
- `e33d7b42` — synchronized `BUILDING.md` with the audited portable/target workflows.
- `6017b169` — release checklist now requires all four exact-candidate audit artifacts, manifest verification, and final signed-artifact comparison.
- `d82f4671` — synchronized third-party notices with target audit evidence and final provenance/license obligations.
- `116c56cf` — release process now explicitly retrieves/verifies evidence bundles before manual provenance/license and signed-artifact reconciliation.

The documentation intentionally distinguishes restored/source graph evidence from final signed binary/package evidence.

# 142. Deterministic adversarial pairing and strict-JSON regression coverage expanded

Focused commits:

- `48825620` — added deterministic randomized pairing payload round-trip/canonical re-encoding tests and repeated canonical outer/query alias rejection.
- `dcfb40a2` — added deterministic bounded-byte strict-JSON fuzzing, case-variant duplicate-property generation, and distinct-property strict-validation invariants.

CI run `31784196373` passed:

- 10 Python helper tests;
- 47 required documentation files and 85 checked local Markdown links at that commit;
- localization validation;
- Apple integration metadata validation;
- Core Release build with zero warnings/errors;
- **516/516** xUnit tests with zero failures/skips;
- benchmark Release build with zero warnings/errors;
- Core vulnerable-package report validation with zero findings.

This increases the portable xUnit suite from 511 to 516 tests while keeping the randomized cases deterministic/reproducible and dependency-free.

# 143. Source/release boundary after this continuation

Source-level work now additionally proves:

- machine-readable vulnerable-package findings are explicitly rejected rather than inferred from command success;
- audit helper behavior has its own regression suite;
- target-specific Android/Windows/Mac/iOS app/iOS extension restored graphs have maintained audit evidence;
- retained report bundles have deterministic internal SHA-256 manifests;
- release workflow/audit helper changes self-test before a candidate tag;
- both Bash and Windows PowerShell portable verification paths are executable CI contracts;
- pairing and strict-JSON boundaries have additional deterministic randomized regression coverage.

The remaining P0/P1 work is deliberately external or candidate-specific: production signing and packaging; exact final package/runtime dependency and license/provenance reconciliation; physical cross-device/provider/network/lifecycle/low-storage testing; Apple App Group/provisioning/notarization; Windows signed MSIX install/update/protocol/firewall behavior; accessibility/localization on actual assistive technologies; and final store/privacy submission checks.

---

# 144. Windows portable verification became a required CI contract

Commit `e858fc4a` added the `windows-portable-verifier` job to normal CI and made `scripts/verify-core.ps1` execute on a real Windows hosted runner.

The first Windows execution, run `31784473076`, immediately found a PowerShell parser error in the native-command error message: `$LASTEXITCODE:` was parsed as an invalid variable reference. Commit `080126a0` changed the interpolation to `${LASTEXITCODE}:`.

The Windows gate was deliberately retained. It exists because platform-specific process, filesystem, and native-library behavior cannot be proven by Ubuntu-only execution.

# 145. SQLite test teardown was made portable instead of retry-based

Windows then exposed SQLite temp-database file locks after otherwise-successful tests.

Focused test commits introduced `SqliteTestDatabaseCleanup` and applied it across transfer history, history maintenance, schema migration, diagnostic events, completed-batch metadata, trusted peers, and transfer-queue metadata tests. The helper calls `SqliteConnection.ClearAllPools()` before deleting the isolated database plus `-wal` and `-shm` companions.

A direct `SqliteTestDatabaseCleanupTests` regression creates a pooled temporary database, disposes the connection, invokes cleanup, and verifies that the main/WAL/SHM files are gone. This raised the portable xUnit suite to 517 tests.

Schema migration tests were also changed from broad method-lifetime `await using var` connections to explicit `await using (...)` scopes so connection disposal necessarily occurs before the outer cleanup `finally`.

Arbitrary sleeps/retries were not introduced. A locked test database remains a signal of incorrect SQLite resource ownership.

# 146. Windows testing exposed a production SQLite resource-lifetime defect

Even after pool-aware test cleanup and explicit schema-test connection scopes, the version-zero migration path still retained the database on Windows. Inspection found the real cause in production storage code: SQLite commands were created without deterministic disposal.

Focused signed production fixes:

- `ef8d9deb` — `DatabaseSchemaManager` disposes version/migration commands;
- `a87b486e` — `BatchCompletionStore` disposes all commands;
- `c6be1c1a` — `DiagnosticEventStore` disposes all commands;
- `07616b2a` — `TransferHistoryStore` disposes all commands;
- `ab8a6605` — `TransferQueueMetadataStore` disposes all commands;
- `13af8507` — `TrustStore` disposes all commands.

Readers/connections and migration transactions remain scoped to their actual operation. The Core storage directory was audited after these changes; every Microsoft.Data.Sqlite command-owning storage component is covered by deterministic command disposal.

# 147. Two-OS portable evidence reached 517 tests

Exact source-head CI run `31785808946` completed successfully after the production disposal fixes.

Ubuntu `core` passed:

- 10 Python helper tests;
- documentation validation;
- localization validation;
- Apple integration metadata validation;
- Core Release build;
- **517/517 xUnit tests**;
- benchmark Release build;
- Core machine-readable vulnerable-package validation with zero findings.

Windows `windows-portable-verifier` passed the same PowerShell verification contract, including **517/517 xUnit tests**, benchmark compilation, and zero-finding vulnerable-package validation on Windows.

The Windows success proves that the earlier schema/database-lock failures are closed under the maintained hosted verifier instead of merely passing on Linux.

Source-head CodeQL run `31785808918` and security-hygiene run `31785808999` also succeeded.

# 148. Superseded branch runs no longer block the newest evidence

This continuation produced many intentionally focused commits, which exposed another engineering issue: older platform runs could occupy hosted runner capacity while a newer source head waited.

Focused workflow commits added same-ref concurrency cancellation:

- `7ef7b354` — platform build/audit matrices;
- `a870ff73` — core/two-OS CI;
- `9d9934f3` — CodeQL analysis;
- `51f94cc0` — repository security hygiene.

The platform concurrency change immediately allowed the newest Android, Windows, and Apple jobs to run together rather than waiting behind superseded intermediate matrices. This changes only CI scheduling; it does not skip or downgrade checks on the newest branch run.

# 149. Latest maintained platform matrix is green after the SQLite fixes

Platform run `31786513898` uses commit `7ef7b354`, which contains the complete SQLite production/test fixes plus the maintained platform workflow with concurrency control.

The run succeeded for:

- Android Release compile and dependency audit/upload;
- focused Windows Release compile and dependency audit/upload;
- Mac Catalyst containing-app Release compile and dependency audit;
- iOS Simulator Share Extension Release compile;
- iOS Simulator containing-app Release compile;
- separate iOS app/extension vulnerable-package validation;
- deterministic Apple dependency-evidence manifest generation and artifact upload.

This is hosted source/restored-graph evidence. It is not a signed AAB/APK, MSIX, iOS archive/TestFlight build, or notarized Mac distribution result.

# 150. Current-main portable/security evidence after workflow and documentation alignment

After the source fixes, normal CI/workflow documentation was aligned with the two-OS contract and concurrency behavior.

CI run `31786693757` passed both Ubuntu and Windows jobs on the aligned main state, including 517/517 xUnit tests on both paths. CodeQL run `31786693816` also completed successfully.

The final documentation synchronization and helper cleanup that follow this entry are documentation/repository-maintenance-only changes; they do not alter SwiftDrop transfer/storage runtime source.

# 151. Source/release boundary after Windows and SQLite hardening

The repository now additionally proves:

- PowerShell verification is actually executable on Windows;
- native-command failures are propagated reliably by the Windows verifier;
- SQLite test teardown handles connection pooling without hiding failures;
- production SQLite command objects have deterministic lifetimes across every Core SQLite store;
- the 517-test portable contract passes on both Ubuntu and Windows;
- CodeQL/security hygiene remain green after the resource-lifetime fixes;
- Android/Windows/Mac Catalyst/iOS hosted compilation and target dependency audits remain green after those fixes;
- obsolete intermediate CI runs no longer block newest same-ref platform/core/security evidence.

Still external/candidate-specific: production signing and packaging; physical Android/iOS/device-to-device transfer testing; signed App Group/Share Extension behavior; Windows MSIX install/update/protocol/firewall validation; Mac sandbox/notarization; real restricted-network/provider/lifecycle/low-storage testing; accessibility/localization on actual assistive technologies; exact final signed-artifact dependency/license/provenance reconciliation; and store/privacy publication checks.

**Made by the Sanskar**

---

# 152. August 15 post-v1 queue persistence continuation

The next source-level post-v1 enhancement was selected from the existing optional roadmap rather than reimplementing already-completed master-prompt scope. The target was richer restart-safe transfer queue persistence **without persisting reusable transfer authorization**.

Implemented behavior:

- queue metadata now retains a bounded non-secret operation category;
- queue metadata now retains the most recent safe update timestamp;
- progress is represented as integer basis points in the inclusive range `0..10000`;
- optional total/completed item counts can be retained;
- file, batch, and text sender flows report progress into the shared queue service;
- ordinary progress persistence is deliberately coarsened to 5% buckets, while state/item-count transitions remain persistable;
- stale `Queued`/`Running` work is still converted to `Interrupted` at application restart;
- recovered progress/context remains visible, but stale work is never auto-replayed.

This closes the former `NEXT_STEPS.md` P2 item for richer transfer queue persistence.

# 153. SQLite schema v4 and migration work

`DatabaseSchemaManager.CurrentVersion` was advanced from 3 to **4**.

The `transfer_queue_metadata` table now includes:

- `operation_kind`;
- `updated_utc`;
- `progress_basis_points`;
- `item_count`;
- `completed_item_count`.

The v3→v4 migration preserves existing queue rows and supplies safe defaults (`operation_kind='Transfer'`, progress `0`, nullable counts). The migration remains sequential and transactional and keeps a self-contained base queue-table creation path for artificial/partial prior-version databases used by compatibility tests.

Validation now enforces:

- allowed queue states;
- allowed bounded operation categories (`Transfer`, `File`, `Batch`, `Text`, `Receive`);
- bounded identifiers/labels/error codes;
- creation/update timestamp relationships;
- progress in `0..10000`;
- non-negative item counts;
- completed count not greater than total count when both are known.

Relevant focused commits:

- `3db15d1afd8b880b545271e92fcd68233730d657` — enrich restart-safe queue metadata model;
- `c1eb55c86f15ae48c7a9ffb8bb85c7df6200fd70` — add schema-v4 migration;
- `ccef2188cc2782a22d86cc1c78da7a88fd1837fe` — persist queue progress/operation context;
- `287dca8fde5526dd9dee19d7cbdb74837081d09a` — make the v4 migration self-contained.

# 154. Restart safety and authorization boundary

The richer queue persistence was intentionally designed **not** to become a transfer-resume authorization store.

Persisted queue labels remain the generic value `Transfer`, even when the non-private in-memory UI can display a filename. The queue table does not store:

- transferred file bytes;
- transferred text;
- source paths;
- destination paths;
- peer IP/host values;
- peer ports;
- pairing invitation text;
- pairing nonces;
- reusable bearer/session/transfer tokens;
- peer certificates;
- private keys;
- reusable credentials.

Automated schema coverage explicitly checks that queue columns do not introduce names containing `nonce`, `token`, `certificate`, `host`, or `port`.

On restart, stale active entries are marked `Interrupted` and retain only their safe last-known context/progress. A retry still requires fresh pairing/authorization through the normal product workflow.

# 155. Sender integration and queue UI

`TransferQueueService` now supports progress-aware execution while keeping existing execution overloads compatible.

Implementation details include:

- serialized persistence through a dedicated semaphore;
- monotonic in-memory progress (`Math.Max` against prior progress);
- coarse persistence buckets of 500 basis points (5%);
- state transition persistence for queued/running/completed/failed/cancelled/interrupted states;
- terminal completion normalized to 100%;
- best-effort persistence that does not change transfer success/failure semantics;
- progress callbacks that never overwrite a newer terminal entry with stale state.

`TransferCoordinator` integration now categorizes and reports:

- single file → `File`, one item;
- multi-file/folder batch → `Batch`, batch byte fraction plus completed/total items;
- text snippet → `Text`, one item.

`QueueViewModel` and `QueuePage.xaml` now expose:

- operation category;
- queue state;
- persisted/recovered progress fraction;
- percentage plus completed/total item counts where available;
- a visual progress bar;
- timing and bounded error context.

Focused commits:

- `1b7a45d3a38d08f43143bb46b79cb8e29d394439` — implement progress-aware queue recovery/persistence;
- `37de59a381b59670424379fabd345d89038d74b6` — feed sender progress into the queue;
- `9d0b4b88041fc884a5df65b134dd1f14c8279838` — close an overload-recursion/compile-risk found during review;
- `0d27f9f93f2ef69b0b619801512b2b02f435605d` — expose queue progress from the view model;
- `0d580d2a5847af2e258998faaf829e6981ef5f14` — render operation/progress in queue XAML.

# 156. Schema and metadata regression coverage

Storage regression coverage was expanded for the v4 contract.

`DatabaseSchemaManagerTests` now covers:

- version-zero database creation through current schema;
- v1→current migration;
- v2→current migration;
- legacy v3 queue-row migration without data loss;
- current-version idempotence;
- future-version rejection;
- presence of all new v4 queue columns.

`TransferQueueMetadataStoreTests` now covers:

- rich metadata round-trip;
- operation kind;
- progress fraction;
- total/completed item counts;
- interrupted-state conversion while preserving progress;
- deletion of terminal rows;
- invalid state/error/operation/progress/item relationships;
- explicit absence of authorization/endpoint-oriented queue schema fields.

Focused test commits:

- `834ee66c07d3e1a2ccc729d1af1529ac794edb12` — schema-v4 migration coverage;
- `2742c43534716d28d29350d0a9c23af62892e9d3` — rich metadata and privacy-boundary coverage.

# 157. Documentation synchronization for schema v4

The current-state documentation was corrected so it no longer describes schema v3 as the latest schema after the queue enhancement.

Updated references include:

- `docs/storage/database-schema.md` — current schema 4, queue fields, migrations 0→1→2→3→4, completed-batch table history, and no-authorization rules;
- `NEXT_STEPS.md` — moved rich queue persistence from optional P2 into completed August 15 work and added v4 validation requirements;
- `PRIVACY.md` — documented restart-safe queue metadata, coarse progress persistence, and excluded authorization/content/endpoints;
- `docs/versioning-and-compatibility.md` — current schema 4 and queue restart compatibility contract;
- `CHANGELOG.md` — new August 15 unreleased section;
- `PROJECT_STATUS.md` — current schema/status/UI/testing/release-boundary synchronization.

Focused documentation commits:

- `cb0065b83d63ab4cb10fc6e981424aa44151587b` — storage schema documentation;
- `6d4868b983e44d8dc9ee9ed3c2a130a603e48025` — roadmap synchronization;
- `8dd52e8ce30aa835303b4e5f55de5f5af0e71250` — privacy contract;
- `af6a26335f94a08266287b6184d00423bf48cccf` — compatibility contract;
- `d374bfeefa0ce014e4828feb9c06430369cca4b0` — changelog synchronization;
- `d1d9a2c3b9200b4e8c5b35eff1f4c32a244f185a` — project-status synchronization.

# 158. Validation evidence boundary for this continuation

The August 14 exact-head baseline remains historically valid evidence: portable tests were green on both Ubuntu and Windows and the maintained Android/focused-Windows/Mac-Catalyst/iOS platform matrix was green before this new queue source work.

The August 15 source-changing platform run `31866920059` was launched from source head `0d580d2a5847af2e258998faaf829e6981ef5f14`. At the time this ledger append was prepared, its focused Windows build/audit had already completed successfully while Android and Apple jobs were still running. Newer documentation commits intentionally do not relabel older source evidence as an exact-final-head pass.

Regular CI, CodeQL, and security-hygiene runs for the newest main-head documentation/source state were queued/running while this ledger append was prepared. Their final conclusions must be recorded from GitHub Actions evidence rather than guessed.

A temporary one-run ledger helper was attempted only to preserve the long file by appending in place, but GitHub rejected that workflow before creating a job. It made no ledger modification and was removed immediately in commit `4c49d1ced68d9c99d02654c3832b81d3b8063db5`. The ledger was then updated directly without deleting or shortening any earlier numbered section.

The project therefore remains classified as **source-complete for the current master-prompt scope plus this implemented optional queue enhancement, with release validation still external/candidate-specific**. Signed packaging, real devices/providers/networks/filesystems, accessibility/localization validation, final dependency/license/provenance reconciliation, App Group/notarization, and store/privacy checks remain outside what source-only edits can honestly complete.

---

# 159. Caller-cancellation queue persistence hardening

Focused source commit `67fc3feaa506b16d11307afa9da8ca9d151f6d22` closes a lifecycle edge discovered during final review of the richer queue persistence implementation.

Before this fix, a caller-cancelled persistence wait/write could flow through the generic best-effort storage catch path and set `_persistenceAvailable=false`, incorrectly treating ordinary transfer/request cancellation as evidence that SQLite was unusable for the remainder of the application session.

The corrected behavior is:

- queue initialization rethrows caller cancellation instead of marking persistence unavailable or completing initialization with a false degraded state;
- caller-cancelled best-effort queue metadata writes are ignored as cancelled writes but do **not** disable later persistence;
- `ClearFinishedAsync` preserves caller cancellation semantics instead of turning it into a storage-health failure;
- real SQLite/storage failures continue to disable only the best-effort persistence path and never change the transfer result;
- exception type names are normalized through a bounded error-code sanitizer before persistence, retaining only ASCII letters/digits/`-`/`_`/`.` up to 64 characters with `transfer-error` fallback.

# 160. Current-state schema-v4 documentation sweep

A final repository-wide current-state sweep found several documents that still described schema v3 as current even though the queue enhancement had advanced the database to v4. Historical dated ledger/changelog references were preserved as history; current public/release instructions were corrected.

Focused commits:

- `8209b13c84d481dcd8b9fa7934e9f5772d384658` — public README aligned with schema v4, restart-safe queue progress, and the 522-test current contract;
- `bc04419ac2c0c90929fb5ad2deb3da2f31e43a1b` — architecture/local metadata description aligned with schema v4 and cancellation semantics;
- `f1e6f9593aae3a598e36f91b181b21066a5e9565` — manual test matrix expanded for v0/v1/v2/v3→v4 migration, queue progress/restart/privacy/cancellation behavior;
- `701ca698aa1a22beb3cb64bd6a4f5dc3277efe9e` — security test plan aligned to the schema-v4 non-authorizing queue boundary;
- `234371b7d7d35844a07b400f32ff6e5b9c60168a` — release checklist requires schema-v4 migration/privacy/restart/cancellation checks;
- `fe0b73022ccc8a66585c5a07b34c6ebef9556b7f` — release process made the same v4 queue requirements part of candidate freeze, physical validation, accessibility, and privacy review.

The documentation intentionally continues to say that `completed_batch_items` was **introduced in schema v3** while correctly stating that the **current schema is v4**.

# 161. Final automated evidence for the August 15 source continuation

The exact runtime/source-changing head is `67fc3feaa506b16d11307afa9da8ca9d151f6d22`.

Portable/source evidence:

- Ubuntu normal CI on that source completed the full **522/522 xUnit** suite with zero failures/skips;
- 10 Python validation-helper tests passed;
- documentation integrity, English/Hindi localization validation, and Apple integration metadata validation passed;
- Core and benchmark Release builds completed with zero reported build errors;
- the machine-readable Core vulnerable-package report contained zero findings;
- security-hygiene on the exact source succeeded.

Current documentation/source-state evidence:

- CI run `31867674137` completed successfully on both Ubuntu and the Windows PowerShell portable verifier using the same runtime source;
- CodeQL run `31867674094` completed successfully;
- security-hygiene run `31867674078` completed successfully.

Exact-source platform evidence:

- platform run `31867418650` completed successfully;
- Android Release compile/audit succeeded;
- focused Windows Release compile/audit succeeded;
- Mac Catalyst containing-app Release compile/audit succeeded;
- iOS Simulator Share Extension Release compile succeeded;
- iOS Simulator containing-app Release compile succeeded;
- iOS containing-app and Share Extension vulnerable-package audits succeeded;
- Android, Windows, and Apple dependency-evidence artifacts uploaded successfully.

The final open-issue search returned no open GitHub issues.

# 162. Final source/release boundary for this continuation

SwiftDrop is source-complete for the current master-prompt scope plus the implemented restart-safe queue enhancement. The current hosted source contract includes schema-v4 queue migration/persistence, sender progress integration, queue UI progress/context, caller-cancellation resilience, 522 portable xUnit tests, two-OS portable CI, CodeQL/security hygiene, and the maintained Android/Windows/Mac Catalyst/iOS hosted platform matrix.

This does **not** convert hosted/source evidence into a production-ready claim. The remaining release work is deliberately external or exact-candidate distribution evidence:

- signed Android AAB/APK and install/upgrade/policy validation;
- signed Windows MSIX/package install/update/protocol/capability/firewall validation;
- Apple Developer App Group/provisioning for the iOS app and Share Extension;
- signed iOS/TestFlight Share Extension/provider/App Group behavior;
- signed/notarized Mac Catalyst sandbox/network/native-drop validation;
- physical cross-device transfer/resume/network/filesystem/low-storage/provider testing;
- real SecureStorage/keychain/keystore and supported schema-upgrade scenarios;
- TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast/Hindi validation;
- exact final signed-artifact dependency/license/provenance reconciliation;
- store/privacy metadata and submission checks.

---

# 163. Native terminal notifications across maintained targets

The next post-v1 source enhancement moved optional completion/failure system notifications from Android-only support to every maintained SwiftDrop product target while preserving the existing privacy-first opt-in model.

- Android retains the existing local terminal-notification path.
- iOS and Mac Catalyst use the platform User Notifications framework.
- Windows uses Windows App SDK app notifications.
- The preference remains off by default.
- Permission/registration/presentation failure is best-effort and cannot change the underlying transfer outcome.

# 164. Apple authorization and foreground presentation

Apple containing-app behavior now requests only alert/sound local-notification authorization after explicit user enable. A strongly retained `UNUserNotificationCenterDelegate` returns banner/sound presentation options so an enabled terminal notification can be visible while SwiftDrop is foregrounded. The delegate is removed/disposed during application shutdown.

The implementation does not register a remote-push token, add a relay service, or place transfer-specific data in the notification body. Signed iOS/Mac Catalyst authorization, foreground/background/system-settings behavior remains a release gate.

# 165. Windows packaged app-notification lifecycle

Windows uses `AppNotificationManager.Default` and `AppNotificationBuilder`. The package manifest now contains a `windows.toastNotificationActivation` extension and matching COM server/class using CLSID `A630B8B4-6522-4EA0-9BBE-A2C7C40BB839`, executable `$targetnametoken$.exe`, and activation arguments `----AppNotificationActivated:`.

Registration behavior is lifecycle-safe:

- `NotificationInvoked` is attached before `Register()`;
- an already-enabled saved notification preference registers during service startup;
- showing a terminal notification reuses/ensures registration;
- shutdown removes the handler and calls `Unregister()`;
- registration/show failures remain isolated from transfer correctness.

The package retains `privateNetworkClientServer` and does not add `internetClient`. Signed MSIX install/update/activation remains external release evidence.

# 166. Notification privacy and localization

English/Hindi resource catalogs now contain placeholder-free generic completion/failure notification bodies plus localized platform support guidance. The former Android-specific permission-denial explanation was replaced with platform-neutral English/Hindi wording.

Terminal notification text intentionally excludes filenames, peer/device names, source/destination paths, transferred text/file content, pairing invitations/nonces/codes/fingerprints, transfer IDs, and reusable authorization/credentials.

# 167. Portable Windows integration validator

Added `scripts/validate_windows_integration.py` and dedicated regression coverage. The validator checks:

- one `swiftdrop` protocol registration;
- `privateNetworkClientServer` is retained and `internetClient` is absent;
- exactly one packaged toast activation extension and one COM notification server;
- valid/matching toast and COM CLSIDs;
- exact notification activation arguments;
- generic nonempty placeholder-free English/Hindi terminal messages;
- Windows notification source contains startup registration;
- `NotificationInvoked` is attached before `Register()`;
- notification show uses the generic resource keys.

Six Windows validator tests now cover a valid contract, CLSID mismatch, forbidden Internet capability, placeholder injection, incorrect handler/register order, and missing startup registration. Together with the existing audit/evidence helper tests, the Python helper suite is now **16 tests**. Bash, PowerShell, normal CI, and release readiness execute the Windows validator.

# 168. Focused notification and verification commit trail

Key commits:

- `308ecc0e110e707c9a4feb53b77dd9cce9d75e5a` — native Apple/Windows notification service support;
- `16273054ea8b5540921a6df458f2a749ecd5c4e1` — enable native notification opt-in across targets;
- `e7c5d9e15e9326acea9a8cae8d06a2b99ec05b5a` / `18df19940ce693225d23465b698e11675fe56ec6` — generic English/Hindi terminal messages;
- `3af5636624fdfceab03be024dced3b2ef05121c0` — Windows packaged notification activation metadata;
- `d2f2c5b867d451f300eeb41d7030cdcbfee7f526` — native notification registration cleanup;
- `2315d4d5fa34242a05018a5bd3493bb424004b70` — Apple foreground presentation delegate;
- `5d6f215c57f23d8ec657e41183c64f77976e0d6b` / `0221630bd57434054f9a0125c06bb0cc2e655c07` — Windows validator and initial tests;
- `f48e9721761a2345cffcbd5a0e1c18177988b225` / `d0458b20cbc672cc13f6458d558f8817e5ccc648` — platform-neutral English/Hindi denial wording;
- `12c64e44e1de55dfa0421a67b35ee25a2774a0bd` / `ef54cfa7bd269ba2c69f753d7f91f2b4193efe79` / `23104e4d1bf8810a39f56d7dda7f4584dc018583` — localized notification support guidance;
- `c3bd4d9fd5389a56fd203a5e4edb31033631181a` — Windows startup registration when preference is already enabled;
- `b3eab15d2f04462ff60f5bd9014acfb7ef490353` / `335eb067a3799e15ec96a3b1b7bee12614332ff6` — startup/order validator hardening and tests;
- `bca13792b41b85464ac357b267c32667b79798c2` — normal CI integration;
- `594e586dcda99d75b4d79da0ce9362813e28d4f5` — release-readiness Apple shared-Core restore correction discovered by the new self-test sequence.


**Made by the Sanskar**

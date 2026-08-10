# What changed

Date: 2026-08-10
Repository: https://github.com/sanskarIN/SwiftDrop
Branch: `main`
Master prompt: `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md`

This file is the detailed engineering ledger requested for SwiftDrop. Chat replies are intentionally kept short. Implementation details, security/privacy decisions, tests, platform integration, build/release gates, validation limits, and remaining external release work are recorded here instead.

---

# 1. Product/source alignment

SwiftDrop remains:

- an open-source local-network file/text transfer application;
- .NET 10 / .NET MAUI / C#;
- Apache-2.0 licensed;
- account-free for the current local-transfer workflow;
- designed without a SwiftDrop-operated cloud relay/upload path for transfer payloads;
- branded `Made by the Sanskar`;
- repository: `https://github.com/sanskarIN/SwiftDrop`;
- creator profile: `https://www.github.com/sanskarIN`;
- business/security email: `sanskarin@outlook.in`;
- support email: `supportramsandesh@gmail.com`;
- optional development support: `https://buymeacoffee.com/sanskarIN`.

Optional financial support does not unlock transfer features, privileged support, priority security handling, private data access, or hidden application capabilities.

---

# 2. Repository/project architecture

The canonical solution is `SwiftDrop.slnx`.

Current projects:

- `src/SwiftDrop.Core`
  - reusable protocol/security/networking/transfer/storage policy;
- `src/SwiftDrop.App`
  - .NET MAUI containing application and platform integration;
- `src/SwiftDrop.ShareExtension`
  - dedicated iOS/Mac Catalyst Share Extension;
- `tests/SwiftDrop.Core.Tests`
  - portable xUnit regression/security/integration tests;
- `benchmarks/SwiftDrop.Benchmarks`
  - bounded synthetic benchmark harness.

Repository-wide compiler policy uses stable `LangVersion=latest`, nullable reference types, deterministic builds, current analyzers, and warnings-as-errors for portable projects. Platform projects keep SDK availability/obsolete diagnostics visible while still treating common nullable regressions as errors.

---

# 3. UI/MVVM separation

Dedicated view models now back:

- Main dashboard → `MainViewModel`;
- History → `HistoryViewModel`;
- Queue → `QueueViewModel`;
- Nearby Devices → `DevicesViewModel`;
- Trusted Devices → `TrustedDevicesViewModel`;
- Diagnostics → `DiagnosticsViewModel`;
- Settings → `SettingsViewModel`;
- About → `AboutViewModel`.

The UI/platform boundary still owns work that belongs there:

- confirmation/consent dialogs;
- navigation;
- file/folder pickers;
- clipboard calls;
- native activation;
- Android share intents;
- Windows/Mac native drag/drop;
- Apple Share Extension/App Group handoff;
- window/page lifecycle.

Networking, TLS, certificates, hashing, SQLite, path policy, integrity, protocol validation, and authorization remain in services/Core rather than view models.

Main transfer work is split into partials to keep platform/lifecycle/batch-resume/external-input concerns separated from the primary page.

---

# 4. Local identity and certificate lifecycle

SwiftDrop creates a local device identity consisting of:

- random local device ID;
- user-visible device name;
- self-signed P-256 ECDSA certificate;
- private key stored through platform `SecureStorage`.

Certificate profile includes:

- non-CA basic constraints;
- digital-signature key usage;
- TLS server-auth EKU;
- TLS client-auth EKU;
- subject-key identifier;
- bounded validity.

`IdentityCertificatePolicy` verifies:

- private key presence;
- supported ECDSA key type;
- not-before tolerance;
- expiry;
- renewal window.

Corrupt/expired/unusable identity material is not silently reused. SwiftDrop creates a new device ID/certificate, invalidates active pairing capabilities, and shows a re-pair notice. Identity reset explicitly clears local trust relationships.

Certificate/private-key material is not stored in SQLite, pairing links, diagnostics, transfer history, GitHub source, or App Group share packages.

---

# 5. Certificate fingerprint handling

SHA-256 fingerprint policy is centralized.

Implemented behavior:

- exactly 32 SHA-256 bytes;
- canonical uppercase 64-hex storage form;
- compact/colon-separated parsing where appropriate;
- constant-time equality for trust/pinning comparisons;
- malformed persisted trust fingerprints ignored;
- human-friendly colon formatting rejects malformed values rather than formatting arbitrary input.

Trusted-device matching uses device ID plus exact certificate fingerprint. Device display name alone never establishes trust.

---

# 6. Discovery

SwiftDrop includes:

- internal mDNS/DNS-SD codec/service;
- bounded UDP IPv4 fallback;
- discovery registry with deduplication/expiry/self-filtering/stable sort;
- Android multicast-lock manager;
- Apple Bonjour declarations;
- Nearby Devices UI/service integration.

Discovery parser hardening/tests cover:

- truncated packets;
- random packets;
- DNS compression pointer loops;
- impossible record counts;
- duplicate TXT metadata keys;
- every truncated prefix of a valid announcement;
- deterministic random/fuzz-style packet inputs.

Discovery metadata is not authentication. Pairing/TLS/certificate verification remains required.

SwiftDrop respects guest-Wi-Fi/client-isolation, multicast filtering, firewall, local-network permission, and managed-device policy rather than bypassing them.

---

# 7. Pairing

Supported local pairing methods:

- QR/deep-link pairing;
- Nearby request pairing;
- short-lived one-time 8-digit code;
- manual numeric local-IP + code fallback.

Pairing invitations are short-lived capabilities and contain connection metadata only; they do not contain private keys.

`PairingCodec` now strictly validates:

- overall/encoded length;
- bounded JSON depth;
- strict encoded JSON;
- comments/trailing commas rejected;
- case-insensitive duplicate-property rejection;
- exact `swiftdrop://pair` URI form;
- one `p` query field only;
- no unknown outer query/path/fragment/user-info/authority-port data;
- exact protocol version;
- bounded device ID/name;
- numeric loopback/private/link-local/unique-local address only;
- port range;
- canonical SHA-256 fingerprint;
- bounded base64url-style nonce;
- valid future expiration within maximum invitation lifetime.

Protocol v1 rejects public Internet addresses and DNS peer hostnames.

Manual-IP bootstrap:

- uses fresh 8-digit code;
- requires receiver approval;
- captures server certificate from bootstrap TLS;
- returned invitation must match observed certificate/address/port;
- user still confirms fingerprint before transfer.

---

# 8. TLS and peer authentication

SwiftDrop uses .NET/platform TLS 1.3/1.2 and does not implement custom cryptographic algorithms.

Sender:

- pins receiver SHA-256 certificate fingerprint from pairing;
- presents its own local client certificate.

Receiver:

- requires a TLS client certificate;
- derives sender fingerprint from the authenticated TLS channel;
- never trusts a JSON-supplied sender fingerprint;
- applies receiver consent/trust after authenticated identity is available.

Inbound connection/pairing attempts are rate-limited with bounded key cardinality and stale-key pruning.

---

# 9. Shared typed application protocol

Production sender, nearby/manual pairing client, receiver, and portable tests now use the same Core wire records:

- `ProtocolRequest`;
- `TransferAcknowledgement`;
- `BatchItemStart`;
- `PairingResponse`;
- `BatchTransferResponse` / `BatchItemPlan`.

Added Core policy:

- `ProtocolRequestFactory` — validated outgoing construction;
- `ProtocolRequestValidator` — type-specific incoming shape validation;
- `ProtocolSessionAuthorizer` — one-time transfer authorization validation/consumption;
- `IncomingRequestPolicy` — shared envelope/identity/nonce/code/transfer-ID/item-order rules;
- `TransferResponsePolicy` — resume/completion/text acknowledgement validation.

This removes anonymous/app-private wire DTO drift and makes full application conversations portable-testable without MAUI.

---

# 10. Strict framed JSON

`FrameProtocol` is a 4-byte signed big-endian length followed by UTF-8 JSON.

Current strict rules:

- positive frame length;
- frame size bounded before allocation;
- bounded JSON depth;
- invalid UTF-8 rejected;
- malformed JSON rejected;
- comments rejected;
- trailing commas rejected;
- duplicate members rejected case-insensitively at every nested level;
- **unknown/unmapped JSON members rejected** using `JsonUnmappedMemberHandling.Disallow`;
- truncated headers/payloads fail;
- reads/writes/flushes use idle timeout and caller cancellation.

Protocol v1 therefore behaves as a closed JSON schema. Additive wire fields require an explicit compatibility/version decision rather than assuming older peers will ignore them.

---

# 11. Type-specific request safety

`ProtocolRequestValidator` rejects cross-type field smuggling.

Examples:

- file request cannot carry text/batch/pair fields;
- batch request requires transfer ID, manifests, and declared total;
- text request requires text + expiration and cannot carry batch/pair fields;
- pair request cannot carry transfer authorization.

Malformed text expiration, invalid batch declared total, missing file metadata, bad sender identity, invalid transfer ID, bad nonce/code, and unexpected fields are rejected before receiver consent/data handling.

---

# 12. One-time authorization ordering

For file/batch/text requests production receive ordering is:

1. read strict typed request frame;
2. validate protocol/type/metadata shape;
3. require authenticated TLS sender certificate;
4. derive sender fingerprint;
5. consume one-time transfer nonce;
6. apply consent/trust policy;
7. negotiate/stream payload.

Consequences:

- malformed requests do not consume authorization;
- missing TLS client certificate does not consume authorization;
- consumed nonce cannot authorize a replay;
- pause/retry/resume requires fresh pairing authorization even when receiver partial/completion metadata is reused.

Pair requests do not consume transfer nonces. They use certificate-based rate limiting, optional code, receiver approval, and a pairing response.

---

# 13. Portable receive-session lifetime tracking

Added `AsyncSessionTracker` in Core.

It tracks active async sessions, removes completed/faulted tasks, supports cancellation-aware draining, and handles sessions added while a drain is already active.

`ReceiveServerService` now uses it instead of a private task dictionary. Receiver listener shutdown/restart cancels the listener/session token and drains tracked handlers rather than abandoning them.

Tests cover:

- completed session;
- multiple active sessions;
- faulted session;
- cancellation during drain;
- session added while drain is running.

---

# 14. Single-file transfer

Sender:

- chooses source through platform/user input;
- validates source exists;
- checks file-size bound;
- sanitizes filename;
- hashes SHA-256;
- creates validated manifest;
- binds stream to manifest-declared length.

Receiver:

- validates metadata/path;
- requests explicit/trusted consent;
- reserves collision-safe destination;
- preflights capacity;
- returns bounded resume offset;
- stages to `.swiftdrop.part`;
- receives exactly the remaining bytes;
- verifies complete SHA-256 using constant-time digest comparison;
- promotes only after integrity verification;
- uses non-overwrite final promotion;
- applies last-write timestamp where supported.

Source growth/shrink after manifest creation is detected rather than silently altering protocol framing.

---

# 15. Receive path/filesystem safety

Core path policy now includes:

- rooted path rejection;
- Windows drive/UNC/device rooted syntax rejected even on non-Windows hosts;
- portable `/` and `\\` separator normalization;
- `.` / `..` traversal rejection;
- invalid/control filename sanitation;
- Unicode NFC normalization;
- Windows reserved-device-name neutralization;
- bounded filename/path segments;
- portable batch collision checks;
- receive-root lexical confinement;
- **existing symlink/reparse components beneath receive root rejected**;
- active destination reservation across concurrent sessions;
- deterministic collision suffixing;
- non-overwrite final promotion.

Reparse/symlink checks are repeated around parent directory creation/staging/hash/final promotion, and completed-batch destination revalidation uses the same policy.

A malicious/fully compromised local OS/filesystem remains outside the security boundary; these checks are defense in depth against ordinary local path redirection/races.

---

# 16. Final-destination race protection

A previous final promotion used overwrite semantics.

It now uses non-overwrite `File.Move`. If another process creates the final path after SwiftDrop reserved the destination but before final promotion, SwiftDrop preserves the external file and fails rather than replacing it.

A deterministic regression test covers this race.

---

# 17. Multi-file/folder transfer

Implemented:

- recursive folder enumeration;
- per-file manifest/hash;
- stable portable relative paths;
- duplicate top-level/path deconfliction;
- source existence checks;
- preflight file-count/per-file/aggregate limits before expensive hashing;
- maximum 2,048 files;
- maximum aggregate 1 TiB;
- cancellation;
- receiver Accept All / Accept Selected / Reject;
- per-file negotiated resume offset;
- per-file integrity verification/history;
- aggregate accepted remainder capacity preflight;
- sender validation of receiver plan paths/offsets;
- ordered `BatchItemStart` validation;
- final batch total acknowledgement validation.

Windows folder picker and Windows/Mac native folder drops feed the same source builder rather than bypassing validation.

Protocol v1 transfers file-content paths; empty directories are not independently represented.

---

# 18. Stable batch IDs across pause/resume/retry

The previous interrupted-batch behavior rebuilt a new transfer ID, which prevented the receiver from knowing that finalized files belonged to the same interrupted batch.

Implemented fix:

- `BatchTransferSourceBuilder` supports caller-provided validated transfer ID;
- `TransferCoordinator` accepts/preserves that ID;
- new explicit batch send creates a fresh random ID;
- pause preserves ID and source list;
- transfer failure preserves ID and source list when sources still exist;
- resume reuses the same ID;
- cancel clears the resume ID/state;
- success clears resume ID/state;
- folder sources remain resumable (`File.Exists || Directory.Exists`).

MainPage batch buttons now route through the stable-ID workflow.

A compatibility extension keeps older internal call shape compiling while actual UI controls use the stable workflow.

---

# 19. SQLite schema version 3

`DatabaseSchemaManager.CurrentVersion = 3`.

Migrations:

- 0→1: trust/history/diagnostics;
- 1→2: privacy-minimal queue metadata;
- 2→3: verified completed-batch metadata.

New table: `completed_batch_items`.

Metadata fields:

- `transfer_id`;
- `source_relative_path`;
- `receive_root_key`;
- `destination_relative_path`;
- `length`;
- `sha256`;
- `completed_utc`.

Primary key:

`(transfer_id, source_relative_path, receive_root_key)`.

`receive_root_key` is SHA-256 of normalized receive-root identity. The absolute receive-root path is not stored in this table.

Completion rows are bounded/pruned.

---

# 20. Idempotent completed-file batch resume

This continuation closes the known interrupted-batch duplicate-file gap.

After a batch item is fully verified/finalized, receiver records completion metadata **before** sending that item's normal completion acknowledgement.

On retry with the same stable batch ID, `BatchCompletionVerifier` requires:

- same transfer ID;
- same hashed receive-root identity;
- same source relative path;
- same expected length;
- same expected SHA-256;
- effective destination remains beneath current receive root;
- destination path has no detected symlink/reparse component;
- destination still exists;
- destination length matches;
- **fresh SHA-256 of destination matches current manifest**.

Only then may receiver use existing protocol-v1 semantics:

`BatchItemPlan.ResumeOffset == FileManifestEntry.Length`

Sender still writes the normal `BatchItemStart`, but sends zero additional raw bytes for that item. Receiver sends the normal full-length item acknowledgement.

If destination changed/missing, source manifest changed, receive root changed, metadata is malformed, or a new transfer ID is used, completed-item reuse does not qualify and normal collision-safe transfer behavior is used.

A new user-initiated batch always gets a new transfer ID, so intentional duplicate sends remain intentional duplicates rather than being mistaken for a retry.

Resume metadata is an optimization, never authorization. Fresh pairing is still required. Resume metadata persistence failure is best-effort and cannot convert an already verified file transfer into a failure.

---

# 21. Completed-batch storage/verifier tests

Added tests for:

- v0→v3 migration;
- v1→v3 migration;
- v2→v3 migration;
- current-version idempotence;
- future schema rejection;
- completion row round-trip;
- upsert replacement;
- scoped removal;
- corrupted completion row ignored;
- pruning;
- receive-root hash privacy;
- exact completed-file reuse;
- modified same-length destination rejection;
- different root/source/hash rejection;
- traversal destination rejection;
- stable caller transfer ID across source rebuilds;
- invalid caller transfer ID rejection before hashing.

---

# 22. Text snippet and clipboard behavior

Implemented encrypted text transfer over the paired TLS path.

Controls:

- UTF-8 byte limit;
- expiration/lifetime validation;
- explicit receiver Reject / Accept / Accept-and-Copy;
- accepted text acknowledgement requires offset `0`;
- clipboard read only after explicit user action;
- no continuous clipboard monitoring;
- transfer history stores text metadata only, never snippet contents.

External text handling uses shared rune-safe UTF-8 truncation so a byte limit never splits a Unicode scalar/surrogate pair.

---

# 23. Rune-safe external text limiter

Added:

- `Utf8TextLimiter`;
- portable tests for ASCII boundaries, 3-byte runes, surrogate-pair emoji, zero limit, invalid negative limit.

Used by:

- common external inbox;
- Apple Share Extension;
- Mac Catalyst drop.

This eliminates duplicated substring-by-character/byte-count logic.

---

# 24. External input inbox

`ExternalInputInbox` now supports atomic `AddSharedBatch(text, paths)` handoff.

Behavior:

- bounded pairing links;
- UTF-8-bounded text;
- protocol maximum shared path count;
- path normalization/existence checks;
- platform-aware duplicate path suppression;
- one Changed event for batch handoff;
- recursive stale shared-input cache pruning;
- explicit drain into MainPage review state.

Warm/cold activation paths feed the same review flow. External content is never automatically transferred.

---

# 25. Android share-sheet hardening

Android `MainActivity` now handles `ACTION_SEND` / `ACTION_SEND_MULTIPLE` with stricter parity to other platforms.

Implemented:

- URI deduplication;
- protocol max attachment count;
- provider display-name lookup;
- provider declared-length lookup where available;
- per-file protocol size check;
- runtime byte cap when declared length unavailable;
- Core `FileNameSanitizer`;
- app-cache staging;
- storage capacity preflight;
- bounded 128 KiB streaming;
- exact declared-length check;
- final staged-length check;
- failed partial cleanup;
- atomic text+file inbox handoff;
- failure cannot crash app startup.

Android app-local backup is disabled.

---

# 26. Windows activation/drop integration

Windows package/source includes:

- `swiftdrop` protocol activation;
- `privateNetworkClientServer` capability;
- no general `internetClient` capability for current protocol-v1 local-only design;
- native FolderPicker for receive/folder workflows;
- native files/folders/text/pair-link drag/drop;
- protocol max dropped path count;
- atomic shared text/path handoff.

Dropped paths remain explicit user sources and still pass through normal batch/source manifest hashing and receiver authorization.

---

# 27. Apple containing app document/file intake

Apple file URLs can be staged into SwiftDrop app cache with:

- temporary security-scoped access where available;
- per-file protocol limit;
- portable filename sanitation;
- exact source length checks;
- storage capacity preflight;
- cancellation;
- cleanup on failure;
- normal external review-inbox handoff.

The portable byte-copy implementation is shared/tested in Core rather than duplicating unsafe platform copy loops.

---

# 28. Dedicated iOS/Mac Catalyst Share Extension

This previously documented source gap is now implemented.

Project:

`src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`

Targets:

- `net10.0-ios`;
- `net10.0-maccatalyst`.

Bundle ID:

`in.sanskar.swiftdrop.share`

Containing app bundle ID:

`in.sanskar.swiftdrop`

Shared App Group:

`group.in.sanskar.swiftdrop`

Extension supports bounded activation for:

- text;
- files;
- images;
- movies;
- web URLs.

Extension processing:

- bounded provider/item count;
- security-scoped access where supplied;
- copy provider temp representations while valid;
- per-file/count/aggregate limits;
- portable filename sanitation;
- capacity preflight;
- exact staging length validation;
- shared UTF-8 text limit;
- cancellation tied to extension view lifetime;
- no peer transfer inside extension;
- no private key/reusable pairing authorization in extension handoff.

---

# 29. Strict Apple App Group package handoff

Added Core model/constants/validator for Share Extension package manifests.

Share Extension writes into App Group using:

- temporary `.staging-*` directory;
- strict package manifest;
- `files/` staging;
- atomic publication to `pending-*` only after complete validation/staging.

Containing app importer treats App Group package as untrusted:

- strict JSON;
- unknown JSON fields rejected;
- package version/ID/age/future-skew validation;
- item count/per-file/aggregate/text limits;
- canonical filename/collision checks;
- package/manifest/files/file reparse/symlink rejection;
- exact file length validation;
- re-stage accepted files into ordinary app cache;
- one review-inbox handoff;
- stale staging cleanup;
- malformed packages discarded;
- transient IO failure can remain for retry.

Shared content is presented for review and is never auto-sent.

---

# 30. Native Mac Catalyst drag/drop

This previously documented source gap is now implemented.

A native `UIDropInteraction` is attached to the MAUI host view.

Supports:

- Finder files;
- folders;
- text;
- SwiftDrop pairing links.

Controls:

- temporary security-scoped access;
- bounded provider processing;
- per-file/count/aggregate limits;
- storage preflight;
- exact staging lengths;
- file/folder symlink/reparse rejection;
- portable filename sanitation;
- collision-safe file/directory deconfliction;
- UTF-8 text bound;
- review-inbox handoff;
- integration detached on MainPage disposal;
- no auto-send.

---

# 31. Apple entitlements and source invariants

Containing iOS app and Share Extension share App Group:

`group.in.sanskar.swiftdrop`

Mac Catalyst containing app has:

- App Sandbox;
- network client;
- network server;
- App Group.

Mac Catalyst Share Extension has:

- App Sandbox;
- App Group.

Added `scripts/validate_apple_integration.py`.

It verifies:

- exact App Group across four entitlement files;
- containing/extension bundle IDs;
- containing/extension display/build version parity;
- extension iOS/Mac target frameworks;
- `IsAppExtension=true`;
- app project references extension exactly once as an app extension;
- all correct entitlements files are wired by csproj;
- Mac sandbox entitlements;
- extension point/principal class;
- activation rule text/file bound;
- Core App Group constant;
- canonical solution includes extension.

This is a source-consistency check. It cannot create Apple Developer capabilities/provisioning profiles.

---

# 32. Transfer history and privacy

Local transfer history stores metadata only.

Privacy mode now protects both:

- peer device name;
- filename/description.

New private rows store a language-neutral marker. Older rows are also redacted at read time without destructive rewrite.

History storage validates writes and skips malformed local rows so a corrupt row cannot break valid history.

Retention supports pruning and zero-day clear behavior.

History UI runtime/status/dialog fields are localized.

---

# 33. Diagnostic privacy and resilience

Added/expanded `DiagnosticPrivacyRedactor`.

Privacy mode redacts common:

- paths;
- email addresses;
- IP addresses;
- IP endpoints;
- GUIDs;
- SHA-256 certificate fingerprints;
- SwiftDrop pairing URIs.

Redaction is applied at record/read/export time.

Diagnostic persistence validates bounded single-line events and skips malformed local rows rather than breaking all diagnostic history.

Safe export intentionally excludes transfer contents/private keys/nonces/full pairing invitations.

---

# 34. Trusted-device store hardening

Core trust storage now enforces canonical valid SHA-256 fingerprints at its own persistence boundary rather than relying only on the application wrapper.

Implemented/tested:

- canonicalization;
- malformed direct write rejection;
- malformed persisted row ignored;
- same device ID with certificate change behavior;
- revoke;
- clear-all.

Trusted auto-accept remains disabled by default and applies only to explicit certificate matches and normal-risk content.

---

# 35. Queue/concurrency/restart metadata

Queue uses cancellation-aware concurrency gate and configurable parallelism.

SQLite queue persistence remains privacy-minimal:

- generic `Transfer` label only;
- state;
- created/started/finished timestamps;
- bounded machine-oriented error code.

It does not persist:

- filenames/source paths;
- transferred text;
- peer IP addresses;
- pairing invitations/nonces;
- credentials/private keys;
- free-form exception messages.

Stale persisted `Queued`/`Running` becomes `Interrupted` after restart and is not automatically retried with stale authorization.

---

# 36. Settings

Settings cover:

- device name;
- identity reset;
- receive location;
- transfer concurrency;
- history retention;
- privacy mode;
- trusted-device auto-accept preference;
- theme system/light/dark;
- notifications preference;
- reduce motion;
- larger interface;
- English/Hindi language;
- developer options.

Receive-folder changes restart/re-resolve the listener rather than silently continuing to write to the previous path.

---

# 37. Notifications/background boundaries

Android includes:

- foreground data-sync lifecycle during active user-initiated queued transfers;
- required generic foreground notification;
- optional generic completion/failure notifications where supported.

Unsupported targets do not pretend the optional completion/failure preference works; the preference is disabled rather than silently ignored.

SwiftDrop does not claim arbitrary mobile sockets can survive OS suspension. Additional background continuation remains a platform/store-policy feature boundary, not something SwiftDrop attempts to bypass.

---

# 38. Localization

English/Hindi resource catalogs cover primary and secondary XAML plus major runtime/dialog/status surfaces.

Added CI validator checks:

- XML well-formedness;
- no empty localization values;
- no duplicate keys;
- exact English/Hindi key parity;
- formatted-placeholder index parity.

Saved culture/theme are applied before resolving MainPage.

Physical Hindi layout/wrapping and assistive-technology validation remains a release gate.

---

# 39. Accessibility

Source includes:

- semantic heading/description metadata on important UI;
- dynamic larger-interface resources;
- theme/accessibility settings;
- reduced-motion preference;
- keyboard/focus-aware desktop surfaces;
- accessibility/manual-test checklist.

Release still requires real:

- TalkBack;
- VoiceOver iOS/Mac;
- Narrator;
- keyboard-only navigation;
- large text;
- high contrast;
- reduced motion;
- Hindi layout validation.

---

# 40. Portable protocol conversation tests

Added end-to-end framed conversation tests using the exact production wire records/policies for:

- file request authorization;
- file resume offset;
- file completion acknowledgement;
- authorization replay rejection;
- selective batch plan;
- batch item ordering;
- per-item completion;
- final batch total;
- reordered item-start rejection;
- text request/zero-offset acknowledgement;
- pair request/response with no transfer nonce consumption.

This materially improves hostability/testing without requiring MAUI dialogs/platform APIs.

---

# 41. Strict JSON tests

Coverage now includes:

- invalid frame sizes;
- malformed UTF-8/JSON;
- comments/trailing commas;
- case-insensitive duplicate top-level members;
- nested duplicates;
- unknown top-level members;
- unknown nested members;
- exact known nested manifest accepted;
- every truncated prefix of valid framed JSON.

---

# 42. Transfer integrity/resume tests

Coverage includes:

- source length mutation;
- source shorter during send;
- invalid resume offset;
- staged partial shorter than resume offset;
- staged unexpected tail truncation;
- staged same-length corruption;
- interrupted receive leaves partial;
- SHA-256 mismatch cleanup;
- successful full transfer;
- real TLS file transfer;
- real TLS staged resume;
- final promotion race/non-overwrite.

---

# 43. Path/fuzz/collision tests

Coverage includes:

- traversal/rooted path rejection;
- portable Windows-root syntax on non-Windows hosts;
- randomized portable path fuzzing;
- Windows reserved names;
- Unicode normalization;
- duplicate/collision equivalence;
- destination reservation pressure;
- symlink/reparse directory/file component rejection;
- completed-file traversal rejection.

---

# 44. TLS tests

Portable loopback tests use real `TlsPeerServer` / `TlsPeerClient` streams and cover:

- mutual TLS;
- exact server pin success;
- pin mismatch failure;
- bootstrap observed fingerprint;
- complete file-byte transfer;
- final hash equality;
- staged resume.

These are protocol/transport evidence, not physical-platform release evidence.

---

# 45. CI/build verification

Regular CI includes:

- localization validator;
- Apple integration metadata validator;
- Core restore/build;
- portable tests;
- benchmark-project compile.

Platform workflows include:

- Android app compile;
- Windows app compile;
- Mac Catalyst Share Extension compile;
- Mac Catalyst containing-app compile;
- unsigned iOS Simulator Share Extension compile;
- unsigned iOS Simulator containing-app compile.

Release-readiness includes:

- portable verification;
- Apple metadata validation;
- dependency inventories;
- Android/Windows/Apple compile gates;
- Apple extension dependency graph for iOS;
- Apple extension dependency graph for Mac Catalyst;
- aggregate result gate.

Portable verification scripts (`verify-core.sh` / `verify-core.ps1`) include localization and Apple metadata checks.

Configured jobs are not proof that the exact final commit passed. The final status check is recorded below after the ledger commit.

---

# 46. Release/dependency/license engineering

`THIRD_PARTY_NOTICES.md` now explicitly requires exact release-candidate review of:

- Core/test/benchmark dependency artifacts;
- Share Extension iOS dependency graph;
- Share Extension Mac Catalyst dependency graph;
- containing-app platform restore graphs where needed;
- native/runtime redistribution obligations.

The Share Extension intentionally references Core/platform SDKs rather than introducing a new arbitrary third-party runtime package.

Final binary notices remain a release-candidate task because only the exact signed/published restore graph determines final obligations.

---

# 47. Documentation synchronized in this continuation

Updated current-state documentation includes:

- `README.md`;
- `BUILDING.md`;
- `PRIVACY.md`;
- `PROJECT_STATUS.md`;
- `NEXT_STEPS.md`;
- `CHANGELOG.md`;
- `THIRD_PARTY_NOTICES.md`;
- `docs/architecture.md`;
- `docs/platform/integration-status.md`;
- `docs/platform-permissions.md`;
- `docs/storage/database-schema.md`;
- `docs/protocol/wire-format.md`;
- `docs/protocol/security.md`;
- `docs/protocol/compatibility-matrix.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/testing/manual-test-matrix.md`;
- `docs/release/release-checklist.md`.

Previously stale statements that Apple Share Extension or Mac native drag/drop were missing have been removed from the authoritative current-status docs.

---

# 48. Current source-completion assessment

For the current master-prompt scope, the repository now contains source implementations for:

- local identity/trust;
- mDNS + UDP + QR + code + manual pairing;
- mutual TLS and certificate pinning;
- strict typed protocol;
- one-time authorization;
- single/multi/folder/text transfer;
- selective receive;
- pause/resume/cancel/retry;
- idempotent completed-file batch retry;
- receive path/collision/capacity/integrity protection;
- local metadata/history/privacy/diagnostics/queue;
- Android share intake/background transfer lifetime;
- Windows protocol/folder/drop integration;
- iOS/Mac document URL intake;
- iOS/Mac Share Extension/App Group source;
- Mac native drag/drop source;
- settings/about/trust/history/diagnostics/queue UI;
- English/Hindi localization infrastructure and runtime coverage;
- portable tests/CI/release docs.

The current phase is therefore **source-complete for the current scope and awaiting release validation**, not “production-verified.”

---

# 49. Deliberate non-claims / optional future enhancements

SwiftDrop does not falsely claim:

- arbitrary iOS/Android background sockets survive suspension;
- extension-based file warnings are malware scanning;
- platform signing/provisioning is validated by source metadata alone;
- configured CI jobs passed when status evidence is absent;
- source compile equals store-ready signed package;
- local metadata is immune to a fully compromised OS/kernel/filesystem;
- optional completion/failure native notifications exist on every target.

Optional post-v1 enhancements may include:

- native optional completion/failure notifications on Apple/Windows;
- additional store-compliant background continuation mechanisms;
- broader localization;
- more performance telemetry/benchmarks using synthetic data;
- additional property/state-machine fuzzing;
- trustworthy platform malware-scan integration only where a supported OS API exists.

---

# 50. External validation still required

Repository source changes cannot honestly complete these release gates:

## Automated candidate evidence

- Observe all configured jobs successfully complete for the exact candidate commit.

## Android

- release keystore/AAB/APK signing;
- clean install/upgrade;
- provider shares with known/unknown sizes;
- foreground-service/notification behavior;
- vendor background restrictions;
- physical multicast/discovery.

## Windows

- signed package identity/certificate;
- install/update;
- firewall behavior;
- packaged protocol activation;
- FolderPicker persistence;
- packaged native drop.

## Apple

- create/verify App Group in Apple Developer configuration;
- provisioning profile for containing app + Share Extension;
- signed iOS physical-device build;
- TestFlight/App Store extension embedding;
- signed Mac Catalyst sandbox/App Group/Share Extension;
- notarization/store package;
- Share Extension runtime behavior for declared content types;
- cold/warm App Group import;
- native Mac security-scoped drop behavior.

## Cross-device/network

- physical Android/iOS/Mac/Windows directional matrix;
- guest Wi-Fi/client isolation;
- multicast filtered/direct IP;
- IPv4/IPv6 LANs;
- firewalls/local-network permission;
- network switching;
- sleep/lock/background;
- real low storage;
- multi-gigabyte files;
- many-file/folder batches;
- physical idempotent completed-file resume behavior.

## Accessibility/localization

- TalkBack;
- VoiceOver;
- Narrator;
- keyboard-only;
- largest text;
- high contrast;
- reduced motion;
- responsive layouts;
- Hindi layout/wrapping/runtime-dialog validation.

## Release/legal/store

- exact signed dependency graph review;
- final third-party notices;
- store privacy/data declarations;
- screenshots/metadata;
- final security/privacy review against shipped binaries.

---

# 51. Connector/environment limitations

During this implementation session:

- the active chat runtime did not provide full .NET MAUI workloads needed to compile/sign all target apps/extensions locally;
- GitHub Contents API writes were used for focused commits;
- the connector does not expose an independent author/committer-email override for Contents API writes;
- commits therefore use:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

- recent direct-main combined-status queries through the connector have often returned no status contexts; absence of status is recorded as **unknown/unreported**, never success.

---

# 52. Commit policy

This continuation intentionally used many focused commits rather than one giant commit.

Examples of commit areas include:

- Core protocol models/factories/validators/authorizer;
- protocol strictness tests;
- Apple package model/validator/writer/importer;
- Share Extension project/entitlements/UI controller;
- Mac native drop;
- Android share hardening;
- Windows drop handoff;
- UTF-8 limiter/tests;
- session tracker/tests;
- receive path/reparse/final-promotion safety;
- stable batch ID workflow;
- schema v3 migration/store/verifier/tests;
- CI Apple validation;
- release/platform/privacy/protocol documentation.

Each focused commit uses the requested Signed-off-by trailer where the connector permits commit-message control.

---

# 53. Definition used by this ledger

**Implemented in source** means repository source/tests/docs contain the behavior/policy.

**Automated-validated** means the relevant test/build actually executed successfully for the exact commit in the correct environment.

**Platform-validated** means a signed/packaged target build actually ran successfully on the relevant OS/device/runtime.

**Production-ready** requires:

- current automated source gates pass;
- target apps/extensions compile in release environments;
- signed packages install/upgrade;
- Apple App Group/provisioning works;
- real cross-device/network/resume tests pass;
- accessibility/localization checks pass;
- privacy/security documentation matches binaries;
- dependency/license review is complete;
- store declarations/metadata match shipped behavior.

SwiftDrop must not be described as production-verified until those external gates are completed.

---

# 54. Final engineering boundary

The highest-value next work is no longer broad feature source generation. It is release-candidate evidence:

1. observe the exact final CI/release-readiness runs;
2. configure Apple Developer App Group/provisioning and validate app + Share Extension;
3. build/sign/install Android, Windows, iOS, and Mac Catalyst packages;
4. execute the cross-device manual matrix including idempotent batch retry;
5. run accessibility/localization/network/low-storage lifecycle tests;
6. review exact dependency/license artifacts;
7. align store metadata/privacy declarations with the signed binaries;
8. fix any defects found and add regression tests before tagging production release.

See `NEXT_STEPS.md` and `docs/release/release-checklist.md` for the release-validation sequence.

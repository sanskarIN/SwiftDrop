# What changed

Date: 2026-08-11
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
- malformed UTF-8/JSON rejected;
- comments/trailing commas rejected;
- case-insensitive duplicate-property rejection;
- **unknown/unmapped encoded JSON members rejected** using closed-schema typed deserialization;
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

Pairing JSON now follows the same closed-schema principle as framed application JSON: adding an encoded field requires an explicit compatibility/version decision rather than relying on older peers to ignore it.

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

# 15. Receive path/filesystem and portable filename safety

Core path policy now includes:

- rooted path rejection;
- Windows drive/UNC/device rooted syntax rejected even on non-Windows hosts;
- portable `/` and `\\` separator normalization;
- `/` and `\\` explicitly treated as invalid **single-segment filename data** on every host OS;
- `.` / `..` traversal rejection;
- invalid/control filename sanitation;
- Unicode NFC normalization;
- Windows reserved-device-name neutralization;
- portable batch collision checks;
- receive-root lexical confinement;
- **existing symlink/reparse components beneath receive root rejected**;
- active destination reservation across concurrent sessions;
- deterministic collision suffixing;
- non-overwrite final promotion.

Portable filename segment maximum is now centralized as:

`FileNameSanitizer.MaximumSegmentLength = 180`.

The cap is unconditional even when a filename has a pathological extremely long extension. Truncation avoids splitting a UTF-16 surrogate pair at the boundary. Reasonable extensions remain preserved where possible; pathological extensions are still bounded.

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

This closes the interrupted-batch duplicate-file gap and adds another fail-closed race check.

After a batch item is fully verified/finalized, receiver records completion metadata **before** sending that item's normal completion acknowledgement.

When creating a retry plan with the same stable batch ID, `BatchCompletionVerifier` requires:

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

Only then may receiver initially plan existing protocol-v1 semantics:

`BatchItemPlan.ResumeOffset == FileManifestEntry.Length`.

Sender still writes the normal `BatchItemStart`, but sends zero additional raw bytes for that item.

The August 11 hardening adds a **second completed-file verification after the sender's matching `BatchItemStart` and immediately before the receiver sends the zero-byte full-length completion acknowledgement**.

At that second verification SwiftDrop again requires the same transfer/root/source/destination metadata and a still-valid matching completed file. If the destination was removed, replaced, changed without changing length, redirected, or no longer matches the stored completion record between planning and acknowledgement, the receiver fails closed instead of falsely acknowledging stale bytes. Failed verification invalidates the stored completion optimization so a later retry can safely transfer data again.

If destination changed/missing, source manifest changed, receive root changed, metadata is malformed, or a new transfer ID is used, completed-item reuse does not qualify and normal collision-safe transfer behavior is used.

A new user-initiated batch always gets a new transfer ID, so intentional duplicate sends remain intentional duplicates rather than being mistaken for a retry.

Resume metadata is an optimization, never authorization. Fresh pairing is still required. Resume metadata persistence failure is best-effort and cannot convert an already verified file transfer into a failure.

A fully compromised local process/OS can still race filesystem operations at finer granularity; repeated verification narrows the ordinary TOCTOU window but is not claimed as protection from a hostile kernel/filesystem.

---

# 21. Completed-batch storage/verifier tests

Added/retained tests for:

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
- **first verification succeeds, destination mutates with same length, second verification rejects it**;
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

`ExternalInputInbox` supports atomic `AddSharedBatch(text, paths)` handoff.

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

For Apple App Group imports, one pending share bundle is surfaced for review at a time. Additional pending packages remain in the App Group inbox for a later activation/import rather than being silently merged into or overwriting the currently reviewed selection.

---

# 25. Android share-sheet hardening

Android `MainActivity` handles `ACTION_SEND` / `ACTION_SEND_MULTIPLE` with parity to other platforms.

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

Portable filename sanitation now explicitly removes both `/` and `\\` if they appear as filename data from a foreign platform, independent of the current host's native invalid-character list.

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

The dedicated source target is implemented.

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

- bounded input/provider/item count;
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

August 11 provider-lifecycle hardening adds:

- a bounded 20-second **provider response wait**;
- extension-lifetime cancellation registration around provider waits;
- late timed-out/cancelled callbacks cannot begin a new copy;
- active file-copy loops check extension/user cancellation between chunks;
- timeout is scoped only to waiting for the provider to answer;
- once a provider responds, a legitimate large local copy is governed by extension/user lifetime rather than being killed only because it takes longer than the provider-response window;
- security-scoped resource access is released in `finally`.

Real `NSItemProvider` timing/resource behavior remains an Apple signed-runtime validation gate.

---

# 29. Strict Apple App Group package handoff

Added Core model/constants/validator for Share Extension package manifests.

Share Extension writes into App Group using:

- temporary `.staging-*` directory;
- strict package manifest;
- `files/` staging;
- atomic publication to `pending-*` only after complete validation/staging.

Containing app importer treats App Group package as untrusted:

- serialized import gate;
- strict JSON;
- duplicate and unknown JSON fields rejected;
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

August 11 exact-file-set hardening adds `ExternalSharePackageFileSetValidator`.

It requires:

- every declared package filename is a canonical single path segment;
- physical package filename comparison is host-independent and case-insensitive for portable Apple package semantics;
- duplicate portable names are rejected;
- the physical top-level files under `files/` must **exactly** equal manifest-declared files;
- undeclared extra files are rejected;
- missing declared files are rejected;
- nested directories under `files/` are rejected;
- non-canonical names are rejected;
- symlink/reparse entries remain rejected.

Shared content is presented for review and is never auto-sent.

---

# 30. Native Mac Catalyst drag/drop

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

Privacy mode protects both:

- peer device name;
- filename/description.

New private rows store a language-neutral marker. Older rows are also redacted at read time without destructive rewrite.

History storage validates writes and skips malformed local rows so a corrupt row cannot break valid history.

Retention supports pruning and zero-day clear behavior.

History UI runtime/status/dialog fields are localized.

---

# 33. Diagnostic privacy and resilience

`DiagnosticPrivacyRedactor` covers common:

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

Core trust storage enforces canonical valid SHA-256 fingerprints at its own persistence boundary rather than relying only on the application wrapper.

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

CI validator checks:

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

End-to-end framed conversation tests use the exact production wire records/policies for:

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

# 41. Strict JSON and pairing tests

Coverage includes:

- invalid frame sizes;
- malformed UTF-8/JSON;
- comments/trailing commas;
- case-insensitive duplicate top-level members;
- nested duplicates;
- unknown top-level framed members;
- unknown nested framed members;
- exact known nested manifest accepted;
- every truncated prefix of valid framed JSON;
- encoded pairing duplicate JSON property rejection;
- pairing case-variant duplicate rejection;
- **unknown encoded pairing JSON property rejection**;
- pairing comments/trailing commas rejected;
- wrong scheme/path/authority port/query/version rejected;
- local/private addresses accepted and public/DNS targets rejected;
- nonce/fingerprint/lifetime bounds.

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
- final promotion race/non-overwrite;
- completed-batch destination revalidation;
- **repeated completed-batch verification with same-length mutation between passes**.

---

# 43. Path/fuzz/collision/filename tests

Coverage includes:

- traversal/rooted path rejection;
- portable Windows-root syntax on non-Windows hosts;
- randomized portable path fuzzing;
- Windows reserved names;
- Unicode normalization;
- duplicate/collision equivalence;
- destination reservation pressure;
- symlink/reparse directory/file component rejection;
- completed-file traversal rejection;
- `/` and `\\` removed from single portable filename segments;
- pathological extension still obeys 180-character cap;
- truncation does not split a UTF-16 surrogate pair;
- exact App Group physical file-set validation for exact/missing/extra/nested/non-canonical/duplicate/empty cases.

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

Configured jobs are not proof that the exact final commit passed. The final status check is performed after this ledger commit and missing contexts are not treated as success.

---

# 46. Release/dependency/license engineering

`THIRD_PARTY_NOTICES.md` now describes the current project graph accurately.

Direct shipped/runtime source references include:

- Core: `Microsoft.Data.Sqlite`, `Microsoft.Extensions.Logging.Abstractions`;
- App: `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug`, `QRCoder`;
- Share Extension: no direct NuGet `PackageReference`, but direct project reference to Core and Apple/.NET target-pack/runtime dependency graph.

Test-only dependencies are listed separately.

The notice process explicitly requires exact release-candidate review of:

- Core dependency graph;
- containing-app target graphs;
- Share Extension iOS dependency graph;
- Share Extension Mac Catalyst dependency graph;
- transitive/native/runtime redistribution obligations;
- license/notice/security advisory status from the exact restored signed candidate.

A project having no direct package reference is not treated as having no runtime/dependency obligations.

Final binary notices remain a release-candidate task because only the exact signed/published restore graph determines final obligations.

---

# 47. Documentation synchronized in the current continuation

Current-state documentation includes/was refreshed across:

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
- `docs/testing/security-test-plan.md`;
- `docs/release/release-checklist.md`.

Previously stale statements that Apple Share Extension or Mac native drag/drop were missing have been removed from authoritative current-status docs.

August 11 documentation additionally reflects:

- pairing unknown-member rejection;
- second completed-item verification immediately before zero-byte ACK;
- provider response timeout versus active-copy lifetime distinction;
- exact App Group physical file-set validation;
- portable separator and pathological-extension filename boundaries;
- current shipped-project dependency graph;
- current signed/physical validation requirements.

---

# 48. Current source-completion assessment

For the current master-prompt scope, the repository contains source implementations for:

- local identity/trust;
- mDNS + UDP + QR + code + manual pairing;
- mutual TLS and certificate pinning;
- strict typed protocol;
- closed-schema framed and pairing JSON;
- one-time authorization;
- single/multi/folder/text transfer;
- selective receive;
- pause/resume/cancel/retry;
- idempotent completed-file batch retry with plan-time and pre-ACK revalidation;
- receive path/collision/capacity/integrity protection;
- portable filename/path safety;
- local metadata/history/privacy/diagnostics/queue;
- Android share intake/background transfer lifetime;
- Windows protocol/folder/drop integration;
- iOS/Mac document URL intake;
- iOS/Mac Share Extension/App Group source;
- exact App Group package contents validation;
- Apple provider response/cancellation hardening;
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
- optional completion/failure native notifications exist on every target;
- source-level provider timeout/cancellation logic has been physically proven with every Apple file provider;
- repeated filesystem verification eliminates every possible race against a compromised local OS.

Optional post-v1 enhancements may include:

- native optional completion/failure notifications on Apple/Windows;
- additional store-compliant background continuation mechanisms;
- broader localization;
- richer user-facing handling for multiple queued external Apple share bundles without auto-merging or overwriting the active review selection;
- more performance telemetry/benchmarks using synthetic data;
- additional property/state-machine fuzzing;
- trustworthy platform malware-scan integration only where a supported OS API exists.

---

# 50. External validation still required

Repository source changes cannot honestly complete these release gates.

## Automated candidate evidence

- Observe all configured jobs successfully complete for the exact candidate commit.
- Confirm portable tests, localization, Apple metadata validation, platform compiles, CodeQL/hygiene, dependency inventory, and release aggregate gate.

## Android

- release keystore/AAB/APK signing;
- clean install/upgrade;
- provider shares with known/unknown sizes;
- oversized/disappearing provider cases;
- foreground-service/notification behavior;
- vendor background restrictions;
- physical multicast/discovery.

## Windows

- signed package identity/certificate;
- install/update;
- firewall behavior;
- packaged protocol activation;
- FolderPicker persistence;
- packaged native drop;
- foreign-platform filename separator/path cases.

## Apple

- create/verify App Group in Apple Developer configuration;
- provisioning profile for containing app + Share Extension;
- signed iOS physical-device build;
- TestFlight/App Store extension embedding;
- signed Mac Catalyst sandbox/App Group/Share Extension;
- notarization/store package;
- Share Extension runtime behavior for declared content types;
- cold/warm App Group import;
- **real provider response timeout/cancellation behavior**;
- verify a prompt provider can complete a legitimate longer copy without the response timer aborting it;
- Files/iCloud Drive/Photos/representative third-party provider cases;
- malformed App Group package tests including unknown fields, undeclared extra files, missing files, nested directories, symlink/reparse entries, stale packages, and changed lengths;
- one-pending-bundle-at-a-time review behavior;
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
- physical idempotent completed-file resume behavior;
- mutate/remove completed batch file after plan and before ACK where reproducible, confirming fail-closed behavior.

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

- recent direct-main combined-status queries through the connector may return no status contexts; absence of status is recorded as **unknown/unreported**, never success.

---

# 52. Commit policy

This continuation intentionally used many focused commits rather than one giant commit.

Earlier continuation commit areas included:

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

August 11 continued that policy with separate commits for each security fix, test, and document synchronization step. Section 55 lists the focused commit sequence.

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
- real cross-device/network/resume/provider tests pass;
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
4. execute the cross-device manual matrix including idempotent batch retry and plan→ACK mutation cases;
5. run Apple provider timeout/cancellation/App Group tamper tests;
6. run accessibility/localization/network/low-storage lifecycle tests;
7. review exact dependency/license artifacts;
8. align store metadata/privacy declarations with the signed binaries;
9. fix any defects found and add regression tests before tagging production release.

See `NEXT_STEPS.md`, `docs/testing/security-test-plan.md`, and `docs/release/release-checklist.md` for the release-validation sequence.

---

# 55. 2026-08-11 final hardening continuation and focused commits

This section records the continuation after the previous final ledger commit `89c9dc9dc437da62c0dd05ff85bc49f5ccaeed9a`.

## Release/security documentation conflicts closed first

- `32771ce0e86d70f1af9be0a98a9392e3e254397c`
  - `docs: harden release checklist for current Apple and schema-v3 scope`
  - replaced the pre-Share-Extension/schema-v3 release checklist with exact current app/extension/App Group/resume/security gates.

- `fc4a9bbbe2aa8afd990bd599f7cccd8dadf4c6ef`
  - `docs: align threat model with current protocol Apple intake and resume security`
  - removed stale claims and aligned threat boundaries to typed protocol, schema v3, App Group handoff, and symlink/reparse protections.

- `1b4feda0e264c5a95e40018a1e44925ca767c425`
  - `docs: align third-party notices with shipped project graph`
  - documented Core/App/Share Extension dependency boundaries and exact restored-release inventory requirements.

- `35c7995d2a5d8a4063b00bd62bb5b1244a5e4ca3`
  - `docs: expand security tests for typed protocol Apple handoff and resume metadata`
  - expanded security test plan for closed-schema protocol, App Group tampering, resume metadata, platform shares, and release evidence.

## Completed-batch retry race hardening

- `e5ea6c8d582bae25124cf167af29017465b95264`
  - `test: cover completed batch mutation between repeated verification passes`
  - proves a file can pass first verification, change with the same length, and fail a second SHA-256 verification.

- `d2a195ea77e0a5cdccdf2abeb8032948567c628a`
  - `security: reverify completed batch item before zero-byte resume acknowledgement`
  - production receiver now repeats completed-item verification after `BatchItemStart` and immediately before zero-byte completion ACK.

## Apple provider lifecycle hardening

- `17797faa5738827cea6be6d8b94fd47870d16021`
  - `security: bound Apple share provider callbacks by cancellation and timeout`
  - introduced bounded callback wait and extension lifetime cancellation.

- `31751df77b3e6703196621f1dcd7f4f622fd374d`
  - `fix: limit Apple provider response wait without timing out active copies`
  - separated provider response timeout from already-started copy duration.

- `8e4ab05db2fdb98d13d51e8b8910962f118ac651`
  - `refactor: keep Apple provider wait registrations scoped to awaits`
  - simplified cancellation/timeout registration lifetime for compile/reliability clarity.

## Exact App Group package file set

- `33781260c24b037dc8027625ced105c239c19a04`
  - `security: add exact external share package file-set validation`
  - introduced reusable Core exact-set validation.

- `7b31d13fc5d81d4d541cc0a12c5385ed6cddfd5b`
  - `test: cover exact external share package file sets`
  - covers exact/missing/extra/nested/non-canonical/duplicate/empty package sets.

- `8a458b2a932807c5aed50b736eb572e32e6cc2ba`
  - `fix: make external share package filename matching host-independent`
  - ensures Apple package case/collision semantics do not depend on the CI runner OS.

- `a193bba5853350a9f508905f10634ae4307ef46f`
  - `security: reject undeclared Apple share package files and directories`
  - containing app importer now rejects undeclared extra files/nested directories and enforces exact physical manifest correspondence.

## Protocol security documentation after source changes

- `3898c5c0d75e37a3ac154a40f1688b4cc8bcb8ac`
  - `docs: record final retry and Apple share intake hardening`
  - protocol security documentation updated for repeated resume verification and App Group/provider boundaries.

## Portable filename boundary hardening

- `76571df8d5b91fde85ddcd3938d8aacfee4a8cea`
  - `security: treat both separator characters as invalid portable filename data`
  - `/` and `\\` can no longer be treated as legal filename data merely because a Unix-like host allows one of them.

- `b77a516db48b7ae280f29b703f85793ffd69b406`
  - `test: lock portable separator removal in filename segments`
  - explicit regression coverage for both separators.

- `2c06ffcbf71af8b86526b3e34ac9886353884b17`
  - `security: enforce filename length cap for extreme extensions`
  - closes a previous edge where preserving an extremely long extension could exceed the 180-character segment cap; truncation is surrogate-safe.

- `71420880d6c799efb7933c1af9ad43eda3a7e6ad`
  - `test: cover extreme extension and surrogate-safe filename bounds`
  - verifies pathological extension bounding and emoji/surrogate boundary behavior.

## Pairing closed-schema hardening

- `ceebaf8360a89e0a6953fdbb77889f6d33b3cc16`
  - `security: reject unknown encoded pairing payload members`
  - `PairingCodec` now uses `JsonUnmappedMemberHandling.Disallow` so pairing payload JSON is closed-schema like framed protocol JSON.

- `5d2e06bd2b1719f4dcf515a158f757d79193f2ec`
  - `test: reject unknown encoded pairing payload fields`
  - valid pairing payload plus an extra `debug` field is rejected.

## Final status/document synchronization

- `a45087935275aa35c22ee1979945e4b82702e386`
  - `docs: refresh project status after final source hardening pass`

- `bced8f03376c959099270c96a203e8c746b0017c`
  - `docs: refresh next steps after final security and intake hardening`

- `dc38a861b47b9213a5d56a3f0651b0811ea8e0f6`
  - `docs: add August 11 final hardening changes`

- `98e41a8ded6000ab8cb78fea860c513b3f5eee02`
  - `docs: finalize threat model after pairing and Apple intake hardening`

- `8536378ab159f7a176d95e4af1e94216a0917b2a`
  - `docs: align README with final pairing resume and App Group hardening`

- `c8ca60cce01f0b68003883596c87c879455b3d3c`
  - `docs: align platform status with final Apple intake hardening`

## Final repository hygiene observation before this ledger

A source search for:

`TODO FIXME HACK NotImplementedException`

returned no matching repository results during the continuation. This is a source-hygiene observation, not proof that the software contains no defects.

## Validation truth after this continuation

The current source scope is substantially complete and the known source gaps found during the continuation were closed with focused code/test/document commits. The repository still must not be called production-verified until:

- exact candidate CI evidence is green;
- all platform apps/extensions compile in current release workloads;
- signed packages install/upgrade;
- Apple App Group/Share Extension provisioning/runtime tests pass;
- provider timeout/cancellation and App Group tamper cases pass on real Apple targets;
- physical cross-device/network/resume/path/storage tests pass;
- accessibility/localization checks pass;
- exact signed dependency/license review is complete;
- store declarations and release metadata match the final binaries.

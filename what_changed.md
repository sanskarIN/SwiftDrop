# What changed

Date: 2026-08-12
Repository: https://github.com/sanskarIN/SwiftDrop
Branch: `main`
Master prompt: `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md`

This is the complete SwiftDrop engineering ledger for the current repository source scope. It intentionally records implementation boundaries, security/privacy decisions, platform integration, tests, CI/release gates, focused continuation work, and external validation that cannot honestly be completed from repository source alone.

---

# 1. Product and repository alignment

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

Canonical solution:

- `SwiftDrop.slnx`

Projects:

- `src/SwiftDrop.Core`
  - reusable protocol, networking, cryptography-adjacent policy, path safety, transfer logic, storage policy, source validation, external-staging policy;
- `src/SwiftDrop.App`
  - .NET MAUI containing app and platform integration;
- `src/SwiftDrop.ShareExtension`
  - dedicated iOS/Mac Catalyst Share Extension;
- `tests/SwiftDrop.Core.Tests`
  - portable xUnit regression/security/integration tests;
- `benchmarks/SwiftDrop.Benchmarks`
  - bounded synthetic benchmark harness.

Repository-wide compiler policy uses stable `LangVersion=latest`, nullable reference types, deterministic builds, analyzers, and warnings-as-errors for portable projects. Platform projects keep SDK availability/obsolete diagnostics visible while retaining strict nullable/common compiler behavior.

---

# 3. UI/MVVM architecture

Dedicated presentation view models back:

- Main dashboard → `MainViewModel`;
- History → `HistoryViewModel`;
- Queue → `QueueViewModel`;
- Nearby Devices → `DevicesViewModel`;
- Trusted Devices → `TrustedDevicesViewModel`;
- Diagnostics → `DiagnosticsViewModel`;
- Settings → `SettingsViewModel`;
- About → `AboutViewModel`.

The UI/platform boundary intentionally retains:

- confirmation/consent dialogs;
- navigation;
- file/folder pickers;
- clipboard calls;
- native activation;
- Android share intents;
- Windows/Mac native drag/drop;
- Apple Share Extension/App Group handoff;
- window/page lifecycle.

Networking, TLS, certificates, hashing, SQLite, path policy, source safety, integrity, protocol validation, authorization, and transfer engines remain in services/Core instead of view models.

Main transfer work is split into partials so stable batch resume, external input, folder selection, platform integration, and lifecycle do not accumulate into one monolithic source file.

---

# 4. Local device identity and certificate lifecycle

SwiftDrop creates a local identity consisting of:

- random local device ID;
- user-visible device name;
- self-signed P-256 ECDSA certificate;
- private key persisted through platform `SecureStorage`.

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

Corrupt, expired, unusable, or renewal-required identity material is not silently reused. SwiftDrop generates a new device ID/certificate, clears active one-time authorization, and surfaces a re-pair notice. Explicit identity reset also clears local trusted-device state.

Certificate private-key material is not stored in:

- SQLite;
- pairing links;
- transfer history;
- diagnostics;
- App Group share packages;
- source code.

---

# 5. Certificate fingerprint handling

SHA-256 fingerprint policy is centralized.

Implemented behavior:

- exactly 32 SHA-256 bytes;
- canonical uppercase 64-hex storage representation;
- compact/colon-separated input normalization where appropriate;
- constant-time equality for trust/pinning comparisons;
- malformed persisted trust fingerprints ignored;
- human-readable colon formatting rejects malformed values instead of formatting arbitrary strings.

Trusted-device matching requires:

- device ID; and
- exact canonical certificate fingerprint.

A display name never establishes cryptographic trust.

---

# 6. Discovery

SwiftDrop includes:

- internal mDNS/DNS-SD codec/service;
- bounded UDP IPv4 fallback;
- discovery registry with deduplication, expiry, self-filtering, and stable sorting;
- Android Wi-Fi multicast-lock manager;
- Apple Bonjour declarations;
- Nearby Devices UI/service integration.

Discovery parser hardening/tests cover:

- truncated packets;
- deterministic random packets;
- DNS compression pointer loops;
- impossible record counts;
- duplicate TXT metadata keys;
- every truncated prefix of a valid announcement;
- bounded peer registry behavior.

Discovery metadata is not authentication. Pairing/TLS/certificate verification remains required.

SwiftDrop does not attempt to bypass:

- guest Wi-Fi/client isolation;
- multicast filtering;
- host firewall policy;
- Apple local-network permission;
- enterprise/MDM policy.

---

# 7. Pairing methods

Supported local pairing methods:

- QR/deep-link pairing;
- Nearby request pairing;
- short-lived one-time 8-digit code;
- manual numeric local-IP + code fallback.

Pairing invitations are short-lived capabilities containing connection metadata only; they never contain the private key.

Manual-IP bootstrap:

- uses a fresh 8-digit code;
- requires receiver approval;
- observes the server certificate during bootstrap TLS;
- requires the returned invitation fingerprint/address/port to match the observed bootstrap connection;
- still asks the user to verify the fingerprint before transfer.

---

# 8. Canonical pairing capability representation — August 12 hardening

Pairing transport now accepts one strict textual representation.

`PairingCodec.Decode` rejects:

- surrounding whitespace instead of trimming it;
- wrong scheme/host;
- unexpected user-info;
- unexpected authority port;
- unexpected path;
- fragment;
- missing query;
- missing `=` after the query key;
- empty query segments;
- unknown query keys;
- duplicate `p` fields;
- standard Base64 `+`;
- standard Base64 `/`;
- padding `=`;
- percent-encoded aliases;
- invalid Base64URL length;
- any decoded payload whose bytes do not re-encode to exactly the same unpadded Base64URL text.

The raw `p` value must contain only:

- ASCII letters;
- ASCII digits;
- `-`;
- `_`.

Decoded pairing JSON remains strict:

- bounded depth;
- invalid JSON rejected;
- comments rejected;
- trailing commas rejected;
- duplicate properties rejected case-insensitively;
- unknown/unmapped members rejected;
- exact protocol version;
- bounded device ID/name;
- numeric loopback/private/link-local/unique-local address only;
- valid port;
- canonical SHA-256 fingerprint;
- bounded nonce syntax;
- future expiration within protocol lifetime.

This removes alternate textual aliases for the same short-lived authorization capability and makes logs/tests/comparisons reason about one accepted representation.

---

# 9. TLS and peer authentication

SwiftDrop uses .NET/platform TLS 1.2/1.3 and does not implement custom encryption/key exchange.

Sender:

- pins receiver SHA-256 certificate fingerprint learned from validated pairing;
- presents its own local client certificate.

Receiver:

- requires a TLS client certificate;
- derives sender fingerprint from the authenticated TLS channel;
- never trusts a sender fingerprint supplied in application JSON;
- applies consent/trust only after authenticated identity is available.

Inbound connection/pairing attempts are bounded/rate-limited with stale-key pruning and bounded cardinality.

---

# 10. One-time authorization store

`OneTimeAuthorizationStore` provides reusable bounded one-time authorization behavior:

- bounded token count;
- syntax validation;
- exact expiration;
- expired-entry pruning;
- duplicate-active token rejection;
- thread-safe atomic consume;
- only one concurrent consumer can win;
- replay fails;
- clear support for identity reset/regeneration/disposal.

Tests cover:

- one successful consume followed by replay rejection;
- expiry including sub-second boundary behavior;
- concurrent consumers with exactly one winner;
- duplicate registration;
- capacity bounds;
- pruning reclaiming capacity;
- malformed tokens;
- clear behavior.

---

# 11. Shared typed application protocol

Production sender, nearby/manual pairing client, receiver, and portable tests use shared Core wire records including:

- `ProtocolRequest`;
- `TransferAcknowledgement`;
- `BatchItemStart`;
- `PairingResponse`;
- `BatchTransferResponse`;
- `BatchItemPlan`.

Shared Core policy includes:

- `ProtocolRequestFactory` — validated outgoing construction;
- `ProtocolRequestValidator` — type-specific incoming validation;
- `ProtocolSessionAuthorizer` — request validation + one-time authorization consumption;
- `IncomingRequestPolicy` — envelope, sender identity, nonce/code, transfer-ID, item-order rules;
- `TransferResponsePolicy` — receiver rejection/resume/completion/text-ack rules;
- `BatchTransferPlanValidator` — sender validation of receiver batch plan.

This prevents app-private anonymous DTO drift and makes complete protocol conversations portable-testable without MAUI UI code.

---

# 12. Strict framed JSON

`FrameProtocol` uses:

- four-byte signed big-endian length;
- UTF-8 JSON metadata payload.

Current strict rules:

- positive frame length;
- size bounded before allocation;
- bounded JSON depth;
- invalid UTF-8 rejected;
- malformed JSON rejected;
- comments rejected;
- trailing commas rejected;
- duplicate members rejected case-insensitively at every nested level;
- unknown/unmapped members rejected with `JsonUnmappedMemberHandling.Disallow`;
- truncated headers/payloads fail;
- reads/writes/flushes use idle timeout plus caller cancellation.

Protocol v1 is therefore closed-schema. Adding fields is a compatibility/versioning decision, not an assumption that old peers will ignore them.

---

# 13. Type-specific request safety

`ProtocolRequestValidator` rejects cross-type field smuggling.

Examples:

- file request cannot carry text/batch/pair fields;
- batch request requires transfer ID, files, declared total, and transfer authorization;
- text request requires bounded text + expiry and cannot carry file/batch/pair fields;
- pair request cannot carry transfer authorization.

Malformed identity, nonce/code, file metadata, text expiry, batch total, transfer ID, and cross-type fields are rejected before transfer negotiation.

---

# 14. Canonical portable relative paths — August 12 hardening

New shared `PortableRelativePath` policy provides one path grammar across operating systems.

Protocol path rules now reject:

- OS-rooted paths;
- leading `/`;
- leading `\\`;
- Windows drive prefixes such as `C:`;
- UNC/device-style roots;
- empty segments;
- repeated separators;
- trailing separators;
- `.` segments;
- `..` segments;
- more than 64 segments.

This policy is used by:

- `FileNameSanitizer.SanitizeRelativePath`;
- `PathGuard.ResolveUnderRoot`;
- `ManifestValidator`;
- sender batch construction;
- receiver validation paths.

---

# 15. Canonical `/` wire path representation — cross-platform bug fix

A significant cross-platform bug was closed.

Previously, `FileNameSanitizer.SanitizeRelativePath` returned `Path.Combine(...)`, meaning Windows could advertise:

`folder\\file.txt`

while an Android/iOS/Mac receiver would sanitize the same logical path to:

`folder/file.txt`

Because receiver batch plans and sender manifests use exact relative-path identity, this could break Windows→non-Windows batch/folder negotiation/resume.

Now:

- `/` is the only wire separator;
- sender manifests always use `/` regardless of local OS;
- incoming peer paths containing `\\` are rejected as noncanonical rather than rewritten after authorization;
- local filesystem conversion occurs only after canonical protocol validation.

Batch sender deconfliction also operates directly on canonical `/` strings instead of host-dependent `Path.GetDirectoryName` output.

---

# 16. Canonical manifest validation before authorization

`ManifestValidator.ValidateEntry` now validates full path identity before one-time authorization can be consumed.

It enforces:

- nonempty path;
- maximum 1,024-character relative path metadata;
- no control characters;
- strict portable structure from `PortableRelativePath`;
- maximum 64 segments;
- path must equal `FileNameSanitizer.SanitizeRelativePath(path)` exactly;
- file length bounds;
- valid SHA-256 text;
- bounded timestamp policy.

Therefore incoming paths are rejected if they would change during sanitation, including:

- backslash aliases;
- invalid filename characters;
- reserved Windows device-name aliases;
- unsafe trailing dot/space representations;
- decomposed Unicode aliases whose normalized form differs;
- traversal/rooted/empty-segment forms.

`ProtocolSessionAuthorizer` validation runs before nonce consumption, so malformed/noncanonical path metadata does not burn a valid one-time capability.

Direct tests prove the authorization callback is not invoked for malformed paths.

---

# 17. Filename sanitation

`FileNameSanitizer` provides portable segment safety:

- Unicode NFC normalization;
- trim outer whitespace;
- control-character removal;
- invalid filename-character removal;
- explicit portable invalid characters `/`, `\\`, `<`, `>`, `:`, `"`, `|`, `?`, `*`;
- unsafe trailing dot/space cleanup;
- Windows reserved device-name neutralization (`CON`, `PRN`, `AUX`, `NUL`, `CLOCK$`, `COM1..9`, `LPT1..9`);
- deterministic `unnamed` fallback.

---

# 18. UTF-8 filename byte bounds — August 12 hardening

The previous 180-character cap alone was not enough for byte-limited filesystems because a Unicode-heavy filename can exceed common 255-byte component limits.

Now each SwiftDrop sanitized segment is bounded by both:

- `MaximumSegmentLength = 180` UTF-16 code units; and
- `MaximumSegmentUtf8Bytes = 180` UTF-8 bytes.

Truncation:

- does not split a UTF-16 surrogate pair;
- does not split a Unicode scalar/rune in UTF-8;
- preserves the extension where practical;
- handles pathological very-long extensions safely;
- reapplies reserved-name safety after truncation.

The 180-byte cap intentionally leaves headroom for receive staging suffix:

`.swiftdrop.part`

on common 255-byte component filesystems.

Tests cover:

- ASCII long names;
- extreme extensions;
- surrogate boundary;
- CJK-heavy names;
- emoji-heavy names;
- staging suffix byte headroom.

---

# 19. Bounded collision naming — August 12 hardening

A subtle filename-collision bug was closed.

Simply appending ` (1)` to an already maximum-sized filename and then sanitizing can truncate the suffix away, collapsing the candidate back to the original name.

New `FileNameSanitizer.CreateCollisionSegment` ensures the uniqueness marker survives limits.

Behavior:

- use conventional `name (n).ext` when it fits;
- if the suffix would be lost, use prefix fallback such as `(n) name...`;
- enforce UTF-16 and UTF-8 segment caps;
- preserve distinct markers across repeated attempts.

The shared collision helper is now used by:

- `DestinationReservationSet`;
- `PathGuard.GetCollisionFreePath`;
- batch sender portable deconfliction;
- Apple Share package filename deconfliction.

Tests cover:

- conventional suffix;
- maximum-length ASCII base;
- Unicode byte-bound base;
- repeated markers;
- concurrent/max-length receive reservations.

---

# 20. Receive path/filesystem safety

Receive path policy includes:

- strict portable rooted/path grammar rejection;
- canonical wire path validation;
- receive-root lexical confinement;
- existing symlink/reparse components beneath receive root rejected;
- reparse checks repeated around parent creation/staging/hash/final promotion;
- concurrent destination reservation;
- deterministic bounded collision naming;
- non-overwrite final promotion.

A malicious/fully compromised OS remains outside the application security boundary, but these checks provide defense-in-depth against ordinary local redirection/race cases.

---

# 21. Final destination race protection

Final promotion uses non-overwrite `File.Move` semantics.

If another process creates the final destination after SwiftDrop reserved it but before promotion:

- the external file is preserved;
- SwiftDrop fails safely;
- SwiftDrop does not overwrite that completed destination.

A deterministic regression test covers this race.

---

# 22. Destination reservations

`DestinationReservationSet` atomically reserves candidate paths across concurrent incoming sessions.

Coverage includes:

- first reservation uses requested path when available;
- second reservation gets collision path;
- 64 concurrent reservations remain unique;
- existing completed destination is skipped;
- disposal releases reservation;
- max-length base names still produce distinct byte-bounded collision candidates.

---

# 23. Single-file sender source safety — August 12 hardening

New reusable `TransferSourceSafety` centralizes regular source validation.

`GetRegularFile`:

- resolves full path;
- requires file to exist;
- refreshes filesystem metadata;
- rejects reparse/symlink source.

`TransferCoordinator` uses this before single-file manifest construction.

`TransferEngine.SendFileAsync` repeats the same regular-source validation at the actual stream-open boundary.

This narrows a race where a selected regular path could be replaced with a symbolic link between manifest construction and streaming.

Tests cover:

- normal regular file;
- symbolic-link file rejection where platform permits link creation;
- send-boundary link rejection with zero payload written.

---

# 24. Single-file transfer integrity

Sender:

- validates regular source;
- checks file-size bound;
- creates canonical safe filename;
- hashes SHA-256;
- validates manifest;
- sends strict typed request;
- revalidates source at stream open;
- requires current length to equal manifest length;
- streams exactly declared remaining bytes;
- checks source length again after streaming.

Receiver:

- validates canonical manifest before authorization;
- obtains user/trusted consent;
- reserves destination;
- preflights free space;
- returns bounded resume offset;
- stages `.swiftdrop.part`;
- receives exact remaining bytes;
- SHA-256 hashes complete staging;
- constant-time compares expected/actual hash bytes;
- deletes invalid staging on integrity failure;
- promotes only after verification;
- uses non-overwrite final promotion.

Same-length source content changes after hashing cannot become false success because receiver SHA-256 must still match the manifest hash.

---

# 25. Best-effort final timestamp metadata — August 12 reliability fix

After verified promotion, SwiftDrop attempts to apply the manifest last-write timestamp.

This is optional metadata, not payload integrity.

`File.SetLastWriteTimeUtc` failures caused by supported filesystem/permission/platform exceptions are now best-effort and do not falsely convert already-verified/promoted file content into a transfer failure.

Payload verification/final promotion remain strict.

---

# 26. Bounded link-safe deterministic folder enumeration — August 12 hardening

The previous recursive source enumeration used `Directory.EnumerateFiles(..., SearchOption.AllDirectories)`.

That can be problematic because platform/filesystem behavior around linked/reparse directories can cause traversal outside the selected logical source tree and filesystem enumeration order is not a stable resume identity.

New `TransferSourceEnumerator` performs explicit bounded traversal.

It:

- validates selected root as regular non-link directory;
- rejects linked/reparse root;
- walks directories explicitly;
- rejects linked/reparse descendant directories;
- rejects linked/reparse descendant files;
- bounds traversed directory count;
- bounds traversed file count;
- gathers regular source files;
- sorts by normalized relative path using ordinal ordering.

Tests cover:

- deterministic relative-path order;
- file count overflow;
- directory count overflow;
- symlinked file rejection;
- symlinked directory rejection.

---

# 27. Batch/folder source builder

`BatchTransferSourceBuilder` supports:

- directly selected files;
- selected folders;
- recursive folder files;
- caller-provided stable transfer ID;
- new random transfer ID for new explicit send convenience overload;
- count/per-file/aggregate preflight;
- canonical relative paths;
- deterministic source ordering;
- hash construction;
- final `BatchManifestValidator` validation.

Current maximums are sourced from protocol constants, including 2,048 files and 1 TiB aggregate transfer limit.

---

# 28. Sender pre-hash portable deconfliction — August 12 hardening

Sender destination naming now deconflicts portable collisions before expensive hashing instead of waiting for request construction to reject them later.

The builder uses case-insensitive canonical path identity and canonical sanitation.

It handles:

- duplicate top-level filenames;
- case-only collisions;
- Unicode normalization-equivalent collisions;
- sanitation-equivalent names such as portable invalid-character aliases;
- duplicate folder-root names;
- nested collisions.

After construction, `BatchManifestValidator` still revalidates the complete final manifest.

Tests cover:

- duplicate top-level names;
- case-only names;
- sanitation-equivalent names;
- deterministic retry manifests.

---

# 29. Batch source preflight before hashing — August 12 reliability hardening

Known limits are checked before expensive SHA-256 work where possible:

- transfer ID syntax;
- source existence;
- regular source/link policy;
- file count;
- per-file length;
- aggregate bytes;
- canonical sanitized relative path;
- maximum relative-path length.

If selecting a filesystem root yields an empty `DirectoryInfo.Name`, the source builder uses safe fallback root label:

`Folder`

rather than constructing an unusable transfer root name.

---

# 30. Stable batch IDs

A new explicit batch send creates a fresh random transfer ID.

Pause/failure retry preserves the same ID so receiver-side staged/completed metadata belongs to one interrupted batch lineage.

Success/cancel clears the paused resume lineage.

Transfer ID is validated by `IncomingRequestPolicy`.

---

# 31. Canonical batch transfer ID syntax — August 12 hardening

Batch transfer ID is now a bounded ASCII token:

Allowed:

- `A-Z`;
- `a-z`;
- `0-9`;
- `-`;
- `_`.

Maximum length:

- 128 characters.

Rejected:

- whitespace;
- control characters;
- punctuation outside `-`/`_`;
- slash/path syntax;
- non-ASCII text;
- oversized values.

This avoids treating a persistent resume/database key as arbitrary free-form text.

---

# 32. Active batch UI stable-ID cleanup — August 12

XAML already used the stable batch handlers in `MainPage.BatchResume.cs`.

However, `MainPage.xaml.cs` still contained a second obsolete unbound implementation:

- `SendBatchClicked`;
- `ResumeBatchClicked`;
- `RunBatchSendAsync`;
- `PauseBatchClicked`;
- `CancelBatchClicked`.

That dead path called a compatibility overload that generated a fresh batch ID implicitly.

This was removed to prevent future UI rewiring from accidentally reintroducing non-idempotent retries.

Also deleted:

- `src/SwiftDrop.App/Services/TransferCoordinatorCompatibilityExtensions.cs`

The active app batch workflow now has one coordinator API path:

- caller supplies the stable transfer ID explicitly.

---

# 33. File/folder resume source retention — August 12

New `TransferSourcePathPolicy` centralizes local paused-source retention.

Behavior:

- preserve existing regular files;
- preserve existing regular directories;
- use platform-aware local path comparison for deduplication;
- drop missing sources;
- drop symlink/reparse sources;
- return normalized full local paths;
- provide safe history metadata for both files and folders.

This fixed a folder-resume UI/history bug where a paused folder source could previously be treated like a `FileInfo` or discarded by file-only checks.

Tests cover:

- file + folder retention;
- missing-source removal;
- duplicate removal;
- history metadata;
- source replaced with symlink after pause.

---

# 34. Single-file resume source hardening — August 12

Single-file Send and Resume now filter through regular-source policy before taking the fresh remote pairing capability.

Consequences:

- missing source fails before consuming pairing invitation;
- source replaced by symlink/reparse point is rejected/dropped;
- paused source is retained only if still regular;
- Resume button availability corresponds to a currently valid source candidate.

`RunSingleSendAsync` also obtains `TransferSourceSafety.GetRegularFile` metadata before starting transfer UI state.

---

# 35. Batch receive consent and selective receive

Receiver supports:

- reject whole batch;
- accept all;
- accept selected files.

Sender validates receiver plan:

- every path must exist in source manifest;
- duplicate plan paths rejected;
- accepted overall response cannot omit required plan semantics;
- rejected plan item cannot advertise nonzero resume offset;
- resume offset must be within source length;
- reordered/unknown item-start path fails.

Receiver preflights accepted aggregate remaining bytes before payload streaming.

---

# 36. SQLite schema version 3

`DatabaseSchemaManager.CurrentVersion = 3`.

Migrations:

- 0→1: trust/history/diagnostics;
- 1→2: privacy-minimal queue metadata;
- 2→3: completed-batch resume metadata.

`completed_batch_items` stores:

- `transfer_id`;
- canonical source relative path;
- `receive_root_key`;
- effective local destination relative path;
- length;
- SHA-256;
- completion time.

Primary identity includes:

- transfer ID;
- source path;
- receive-root key.

`receive_root_key` is SHA-256 of normalized receive-root identity rather than the absolute root path.

Rows are bounded/pruned.

---

# 37. Completed-batch verifier

`BatchCompletionVerifier` requires:

- matching transfer lineage metadata;
- matching receive-root key;
- exact canonical source relative path;
- expected length;
- expected SHA-256;
- destination path confinement;
- no reparse/symlink destination path;
- destination exists;
- destination length matches;
- fresh SHA-256 of destination matches.

Completion metadata is an optimization, never authorization.

---

# 38. Idempotent completed-file retry

After a batch item is fully verified/finalized, receiver records completion metadata before sending the normal item completion ACK.

On retry with the same stable ID:

- completion record is located;
- final destination is re-confined;
- reparse/link policy is applied;
- length is checked;
- SHA-256 is freshly computed;
- only then can receiver offer protocol-v1 full-length resume offset.

Sender still emits normal `BatchItemStart` for the item and sends zero additional raw bytes when resume offset equals source length.

A new explicit send has a fresh transfer ID, preserving intentional duplicate-send collision semantics.

---

# 39. Second completed-file verification before zero-byte ACK

A remaining TOCTOU window was closed.

Previously the completed destination was re-hashed while the receiver constructed the retry plan. A local process could potentially modify/delete the destination after the plan but before the zero-byte item ACK.

Now, after the sender returns the matching `BatchItemStart`, receiver invokes completed-file verification again immediately before sending the item completion acknowledgement.

If the destination is:

- missing;
- changed;
- different length;
- different SHA-256;
- redirected through reparse/link state;
- outside expected root;
- no longer matching completion record;

then the shortcut fails closed instead of falsely acknowledging completion.

Regression test covers:

1. verify completion;
2. mutate destination at same length;
3. verify again;
4. expect rejection.

A fully compromised local OS can still race application-level checks at finer granularity; repeated checks narrow but do not eliminate that external trust boundary.

---

# 40. Text snippet transfer and clipboard behavior

Text transfer uses paired TLS path.

Controls:

- UTF-8 byte limit;
- expiration/lifetime validation;
- receiver Reject / Accept / Accept-and-Copy;
- text acknowledgement requires offset exactly zero;
- clipboard read only after explicit user action;
- no continuous clipboard monitoring;
- history stores text metadata only, not snippet contents.

External text intake uses rune-safe UTF-8 truncation.

---

# 41. Rune-safe UTF-8 text limiter

Shared `Utf8TextLimiter` is used by external text paths.

Coverage includes:

- ASCII boundary;
- multi-byte runes;
- surrogate-pair emoji;
- zero limit;
- invalid negative limit.

This prevents a byte-bound truncation from splitting a Unicode scalar.

---

# 42. External input inbox

`ExternalInputInbox` supports atomic `AddSharedBatch(text, paths)` handoff.

Behavior:

- bounded pairing link storage;
- UTF-8-bounded text;
- maximum shared path count;
- local full-path normalization;
- existence checks;
- platform-aware duplicate suppression;
- one Changed event per batch handoff;
- recursive stale cache pruning;
- explicit drain into MainPage review state.

External content is never automatically transferred.

---

# 43. Shared TransferStagingBudget — August 12

New Core `TransferStagingBudget` centralizes external-input copy limits.

Tracks:

- maximum files;
- maximum aggregate bytes;
- maximum per-file bytes;
- committed file count;
- committed bytes;
- remaining files;
- remaining aggregate bytes;
- maximum bytes allowed for next file.

Important semantics:

- `EnsureCanStage(length)` validates without consuming budget;
- `Commit(length)` consumes budget only after a file was copied/verified successfully;
- zero-byte files still consume file-count budget;
- checked arithmetic prevents overflow;
- failed copies do not falsely consume count/bytes.

Tests cover:

- committed accounting;
- per-file limit;
- aggregate limit;
- file count including zero-byte files;
- non-consuming preflight;
- invalid constructor limits.

Used by:

- Apple Share Extension;
- Android share intake;
- Mac Catalyst native drop.

---

# 44. Android share-sheet intake

Android handles:

- `ACTION_SEND`;
- `ACTION_SEND_MULTIPLE`;
- shared text;
- shared content URIs.

Baseline hardening includes:

- URI deduplication;
- protocol max attachment count;
- provider display-name lookup;
- declared-length lookup where available;
- portable filename sanitation;
- app-cache staging;
- storage preflight;
- bounded streaming;
- exact declared/staged length checks;
- cleanup on failure;
- atomic inbox handoff;
- no startup crash if external share fails.

Android application backup is disabled.

---

# 45. Android aggregate budget + unknown-size hardening — August 12

Android share intake now uses `TransferStagingBudget` across the whole share action.

If provider declares size:

- declared size must fit remaining file/count/aggregate budget before copy.

If provider reports negative size:

- it is treated as unknown rather than trusted negative metadata.

If provider size is unknown:

- runtime bytes are capped to `stagingBudget.MaximumBytesForNextFile`;
- this includes remaining aggregate budget, not only per-file limit;
- storage safety reserve is rechecked while streaming each chunk;
- copy stops/fails/cleans staging before intentionally exhausting the volume.

After successful exact staging:

- budget commits actual copied bytes.

Failure:

- partial output deleted;
- no budget commit.

This closes an aggregate-bypass scenario where multiple unknown-size provider files could individually stay under per-file limit while cumulatively exceeding the batch staging cap.

---

# 46. Windows activation/drop integration

Windows includes:

- `swiftdrop` protocol activation;
- `privateNetworkClientServer` capability;
- no general `internetClient` capability for protocol-v1 local-only transfer;
- native receive-folder picker;
- native files/folders/text/pair-link drag/drop;
- atomic external-input handoff.

Dropped file/folder paths remain explicit local sources and still go through:

- regular source/link checks;
- bounded deterministic folder enumeration;
- canonical `/` manifest construction;
- hash/integrity/receiver authorization pipeline.

Windows folder sender now interoperates with non-Windows receivers using identical forward-slash wire paths.

---

# 47. Apple document/file intake

Apple containing app can stage file/document URLs into app cache with:

- temporary security-scoped access where available;
- per-file limits;
- portable filename sanitation;
- exact source length checks;
- storage capacity preflight;
- cancellation;
- cleanup on failure;
- common review-inbox handoff.

External content is never auto-sent.

---

# 48. Dedicated iOS/Mac Catalyst Share Extension

Project:

- `src/SwiftDrop.ShareExtension/SwiftDrop.ShareExtension.csproj`

Targets:

- `net10.0-ios`;
- `net10.0-maccatalyst`.

Bundle ID:

- `in.sanskar.swiftdrop.share`

Containing app bundle ID:

- `in.sanskar.swiftdrop`

Shared App Group:

- `group.in.sanskar.swiftdrop`

Extension supports bounded activation for:

- text;
- files;
- images;
- movies;
- web URLs.

It does not perform peer transfer itself. It stages a bounded local package for containing-app review.

---

# 49. Apple Share Extension provider lifetime hardening

Share Extension provider callbacks can arrive asynchronously and may stall or outlive the view lifecycle.

Current controls:

- 20-second provider **response** timeout;
- extension-lifetime cancellation token;
- cancellation registration around the awaited provider result;
- late timed-out/cancelled callbacks cannot start a new staging copy;
- provider file-copy loop checks extension cancellation between chunks;
- temporary security-scoped access released in `finally`.

A critical refinement separates provider response time from legitimate local copy duration:

- provider must respond within timeout;
- once provider has responded and copy begins, the response timer does not incorrectly cancel a large legitimate copy;
- extension-lifetime cancellation still applies to active copy.

This avoids both indefinite provider waits and accidental rejection of large-but-valid local staging that simply takes longer than the provider response threshold.

---

# 50. Apple Share Extension aggregate staging budget

Share Extension uses shared `TransferStagingBudget`.

Before a provider file is copied:

- source length is checked against remaining file/count/aggregate budget.

After exact successful copy:

- budget commits length.

Therefore the file that would exceed aggregate package size is rejected before SwiftDrop spends time/disk copying that over-limit item.

The final package manifest validator still independently enforces count/per-file/aggregate limits.

---

# 51. Apple App Group package format

Share Extension writes into App Group using:

- `.staging-<id>` temporary directory;
- `files/` payload staging;
- strict versioned manifest;
- manifest file size bound;
- atomic directory rename to `pending-<id>` only after complete staging/validation.

Package manifest contains:

- version;
- package ID;
- creation time;
- optional bounded text;
- list of file names/lengths.

It never contains:

- private key;
- pairing nonce/code;
- reusable transfer authorization;
- trusted-device secret.

---

# 52. Exact Apple package physical file-set validation

Containing app treats App Group package as untrusted.

Beyond validating every declared manifest file, it now enumerates physical top-level `files/` entries and requires exact set equality.

Rejects:

- missing declared file;
- extra undeclared file;
- nested undeclared directory;
- symlink/reparse entry;
- portable duplicate/colliding names;
- noncanonical name.

Shared Core `ExternalSharePackageFileSetValidator` performs host-independent portable exact matching.

Tests cover:

- exact match;
- missing file;
- extra file;
- duplicate physical name;
- nested/path-like filename;
- case/canonical portability behavior.

---

# 53. Apple containing-app import serialization

`AppleShareContainerImporter` uses a static `SemaphoreSlim` gate.

Warm/cold activation cannot concurrently import the same App Group package.

Current behavior intentionally surfaces one imported external bundle per pass so a later pending package cannot silently overwrite/merge the user's current review selection.

Later pending packages remain for subsequent activation/import instead of being deleted as if reviewed.

---

# 54. Apple containing-app aggregate cache capacity preflight — August 12

After strict package validation and exact physical file-set/length validation, importer sums all validated source bytes.

Before creating/recopying the app-cache review staging directory:

- `StorageCapacityGuard.EnsureCapacity(stagingRoot, aggregateBytes)` is called.

This prevents a large valid App Group package from partially recopying many files into app cache before discovering the volume cannot safely hold the whole validated package.

Transient I/O failure can leave the pending App Group package for later retry; malformed/invalid packages are discarded.

---

# 55. Native Mac Catalyst drag/drop

Mac Catalyst uses native `UIDropInteraction` attached to MAUI host view.

Supports:

- Finder files;
- folders;
- text;
- pairing links.

Controls include:

- temporary security-scoped access;
- file/folder link/reparse rejection;
- portable filename sanitation;
- collision-safe staging;
- review-inbox handoff;
- interaction detached/disposed with MainPage platform integration;
- no auto-send.

---

# 56. Mac native drop shared staging budget — August 12

Mac file/folder staging now uses `TransferStagingBudget` instead of separate ad-hoc counters.

Each regular file:

- validates source;
- checks remaining count/per-file/aggregate budget;
- checks storage capacity;
- stages exact bytes;
- validates source/destination length;
- commits budget only after success.

Recursive folders reuse the same budget across all descendant files, so aggregate/count limits apply across the whole drop action.

---

# 57. Mac provider response timeout — August 12

Native Mac drop file/text `NSItemProvider` calls now have bounded response waits.

For file URL, file representation, and text:

- callback must arrive before timeout;
- duplicate/late callback ignored;
- timeout completes task with bounded failure;
- provider invocation exceptions complete task safely.

As with Share Extension:

- response timeout is only for provider response;
- once file callback has arrived, the local staging copy is not incorrectly killed by that response timer.

Platform runtime/provider behavior still requires signed Mac validation.

---

# 58. Transfer history and privacy

Transfer history stores metadata only.

Privacy mode protects:

- peer device name;
- filename/description.

New private rows use language-neutral marker. Older rows are redacted at read time without destructive rewrite.

History storage validates writes and skips malformed local rows where corruption tolerance is intended.

Retention supports pruning and zero-day clear behavior.

History presentation localizes direction/status/size/time/private markers.

---

# 59. Diagnostic privacy and resilience

`DiagnosticPrivacyRedactor` redacts common:

- paths;
- email addresses;
- IP addresses;
- endpoints;
- GUIDs;
- SHA-256 fingerprints;
- SwiftDrop pairing URIs.

Redaction applies at record/read/export time when privacy mode requires it.

Diagnostic persistence validates bounded single-line events and skips malformed local rows rather than making the whole diagnostic history unusable.

Safe export excludes:

- transfer content;
- private keys;
- nonces;
- full reusable pairing capabilities.

---

# 60. Trusted-device store hardening

Trust persistence enforces canonical valid SHA-256 fingerprints at storage boundary.

Coverage includes:

- canonicalization;
- malformed direct write rejection;
- malformed persisted row ignored;
- same device ID with certificate change behavior;
- revoke;
- clear-all.

Trusted auto-accept:

- disabled by default;
- opt-in;
- requires exact device ID + certificate fingerprint;
- applies only to normal-risk content;
- high/caution risk still requires explicit handling.

---

# 61. Queue/concurrency/restart metadata

Queue uses cancellation-aware concurrency gate and configurable parallelism.

SQLite queue persistence remains privacy-minimal:

- generic `Transfer` label;
- state;
- created/started/finished timestamps;
- bounded machine-oriented error code.

It does not persist:

- source paths;
- filenames;
- transferred text;
- peer addresses;
- pairing invitations/nonces;
- credentials/private keys;
- free-form exception messages.

Stale persisted `Queued`/`Running` becomes `Interrupted` after restart and is not automatically retried with stale authorization.

---

# 62. Settings

Settings cover:

- device name;
- identity reset;
- receive location;
- transfer concurrency;
- history retention;
- privacy mode;
- trusted-device auto-accept;
- theme system/light/dark;
- notifications preference;
- reduce motion;
- larger interface;
- English/Hindi language;
- developer options.

Receive-folder changes restart/re-resolve receiver listener instead of silently continuing to old location.

---

# 63. Notifications/background boundaries

Android includes:

- foreground data-sync lifecycle for active user-initiated queued transfers;
- required generic foreground notification;
- optional generic completion/failure notifications where supported/configured.

Unsupported targets disable the optional preference rather than pretending notifications work.

SwiftDrop does not claim arbitrary mobile sockets survive OS suspension. Additional continuation must use supported store-compliant platform mechanisms and remains optional post-v1 work.

---

# 64. Localization

English/Hindi resource catalogs cover primary/secondary XAML and major runtime/dialog/status surfaces.

CI localization validator checks:

- XML well-formedness;
- nonempty values;
- duplicate keys rejected;
- exact English/Hindi key parity;
- formatted-placeholder index parity.

Runtime resources include:

- pairing dialogs/status;
- incoming consent;
- transfer progress/failure/resume;
- batch risk/selection;
- settings reset/save/permission;
- diagnostics export/clear;
- About link errors;
- History/Queue/Nearby/Trusted dynamic values.

Physical Hindi layout/wrapping remains a release-validation gate.

---

# 65. Accessibility

Source includes:

- semantic headings/descriptions on important UI;
- larger-interface resources;
- theme/accessibility preferences;
- reduced-motion preference;
- keyboard/focus-aware desktop surfaces;
- manual accessibility checklist.

Release still requires real:

- TalkBack;
- VoiceOver iOS;
- VoiceOver Mac Catalyst;
- Narrator;
- keyboard-only navigation;
- largest text scaling;
- high contrast;
- reduced motion;
- responsive layout;
- Hindi layout validation.

---

# 66. Portable protocol conversation tests

Portable conversation tests use exact production wire records/policies for:

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
- pair request/response without transfer nonce consumption.

This improves hostability without MAUI UI dependencies.

---

# 67. Strict JSON tests

Coverage includes:

- invalid frame sizes;
- malformed UTF-8/JSON;
- comments/trailing commas;
- duplicate top-level members;
- case-variant duplicates;
- nested duplicates;
- unknown top-level members;
- unknown nested members;
- exact known nested manifest;
- every truncated prefix of valid framed JSON.

---

# 68. Pairing codec tests — current coverage

Coverage includes:

- round trip/canonical fingerprint;
- expired payload;
- excessive lifetime;
- public/DNS host rejection;
- valid local IPv4/IPv6;
- invalid fingerprint;
- invalid nonce;
- wrong protocol version;
- wrong scheme;
- surrounding whitespace rejection;
- duplicate `p`;
- unknown query;
- unexpected path;
- explicit outer authority port;
- standard Base64 aliases;
- Base64 padding;
- percent-encoded payload alias;
- missing `=`;
- empty query segments;
- duplicate JSON property;
- case-variant duplicate JSON property;
- unknown JSON property;
- comments/trailing commas;
- random nonce uniqueness/syntax.

---

# 69. Manifest/path tests — current coverage

Coverage includes:

- valid canonical path;
- invalid hash;
- future/pre-Unix timestamp;
- unsafe length;
- max file length;
- control characters;
- oversized relative path metadata;
- traversal;
- rooted path;
- Windows drive/UNC forms;
- repeated separators;
- trailing separator;
- dot segment;
- backslash wire alias;
- invalid filename sanitation alias;
- Windows reserved name alias;
- trailing-space alias;
- decomposed Unicode alias;
- excessive path depth.

Authorization-order tests prove malformed paths do not consume nonce.

---

# 70. Source safety tests — current coverage

`TransferSourceSafetyTests` cover:

- normal regular file;
- normal regular directory;
- symlink file rejection;
- symlink directory rejection.

`TransferSourceEnumeratorTests` cover:

- deterministic relative order;
- file-count overflow;
- directory-count overflow;
- symlinked file rejection;
- symlinked directory rejection.

`TransferEngineSourceSafetyTests` cover:

- exact regular-file streaming;
- symlink rejection at actual send boundary;
- no payload emitted before rejection.

`TransferSourcePathPolicyTests` cover:

- file/folder resume candidates;
- missing entries;
- duplicate suppression;
- file/folder history metadata;
- symlink replacement removed from resume candidates.

---

# 71. Batch builder tests — current coverage

Coverage includes:

- direct file + recursive folder paths;
- caller transfer ID preserved across rebuild;
- deterministic repeated folder manifest order;
- invalid transfer ID before hashing;
- duplicate top-level file names;
- case-only portable collision;
- sanitation-equivalent collision;
- symlink top-level file rejection;
- symlink top-level directory rejection;
- cancellation before hashing;
- missing source;
- empty selection;
- protocol max file count source-of-truth.

---

# 72. Transfer integrity/resume tests

Coverage includes:

- source length mutation;
- source becomes shorter;
- invalid resume offset;
- staged partial shorter than offset;
- unexpected staged tail truncation;
- same-length staged corruption;
- interrupted receive leaves partial;
- SHA-256 mismatch cleanup;
- successful full transfer;
- real TLS file transfer;
- real TLS staged resume;
- non-overwrite final promotion race;
- completed-file revalidation;
- completed-file same-length mutation;
- repeated completed-file verification after mutation.

---

# 73. Path/fuzz/collision tests

Coverage includes:

- traversal/rooted rejection;
- portable Windows-root syntax on non-Windows hosts;
- randomized portable path fuzzing;
- Windows reserved names;
- Unicode normalization;
- portable collision equivalence;
- destination reservation pressure;
- receive-root symlink/reparse rejection;
- completed-file traversal/reparse rejection;
- UTF-8 filename bounds;
- max-length collision-marker preservation.

---

# 74. TLS tests

Portable loopback tests use real `TlsPeerServer` / `TlsPeerClient` streams and cover:

- mutual TLS;
- exact server pin success;
- pin mismatch failure;
- bootstrap observed fingerprint;
- complete file-byte transfer;
- final hash equality;
- staged resume.

These are protocol/transport evidence, not signed physical-platform evidence.

---

# 75. External share package tests

Portable Core coverage includes:

- version/package ID/time boundaries;
- item count/per-file/aggregate/text limits;
- canonical file names;
- collision handling;
- exact physical file set;
- missing/extra files;
- duplicate portable names;
- path-like filenames;
- stale/future package boundaries.

Platform-specific `NSItemProvider`, App Group sandbox, and Android `ContentResolver` behavior still requires real target validation.

---

# 76. CI/build verification configuration

Regular CI is configured for:

- localization validation;
- Apple integration metadata validation;
- Core restore/build;
- portable tests;
- benchmark-project compile.

Platform workflows are configured for:

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
- platform compile gates;
- Apple extension dependency graph for iOS;
- Apple extension dependency graph for Mac Catalyst;
- aggregate result gate.

Configured workflows are not proof the exact final commit passed.

---

# 77. Apple source-invariant validator

`scripts/validate_apple_integration.py` checks source consistency including:

- exact App Group across entitlement files;
- containing/extension bundle IDs;
- version/build parity;
- extension target frameworks;
- `IsAppExtension=true`;
- app project references extension correctly;
- correct entitlements wiring;
- Mac sandbox network entitlements;
- extension point/principal class;
- activation rules;
- Core App Group constant;
- solution inclusion.

This cannot create or validate real Apple Developer provisioning profiles.

---

# 78. Release/dependency/license engineering

`THIRD_PARTY_NOTICES.md` documents direct dependency surfaces and requires exact final release-candidate dependency/license audit.

Current direct runtime package surface includes project-specific packages such as:

- `Microsoft.Data.Sqlite`;
- logging abstractions/debug providers;
- `Microsoft.Maui.Controls`;
- `QRCoder`.

Tests include:

- Microsoft.NET.Test.Sdk;
- xUnit;
- runner;
- coverlet collector.

Share Extension references `SwiftDrop.Core` and Apple platform SDK APIs rather than introducing a separate arbitrary runtime NuGet package.

Final signed/restored graph determines actual redistribution notice obligations.

---

# 79. Documentation synchronized before final ledger write

Current-state documentation updated in this continuation includes:

- `README.md`;
- `PROJECT_STATUS.md`;
- `NEXT_STEPS.md`;
- `CHANGELOG.md`;
- `docs/protocol/wire-format.md`;
- `docs/protocol/security.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/testing/security-test-plan.md`;
- `docs/testing/manual-test-matrix.md`;
- `docs/release/release-checklist.md`;
- `docs/platform/integration-status.md`.

Previously aligned docs retained because their underlying scope did not change materially:

- `BUILDING.md`;
- `PRIVACY.md`;
- `THIRD_PARTY_NOTICES.md`;
- `docs/architecture.md`;
- `docs/platform-permissions.md`;
- `docs/storage/database-schema.md`;
- `docs/protocol/compatibility-matrix.md`.

No package dependency, privacy schema, App Group ID, bundle ID, or SQLite schema version changed during August 12 hardening.

---

# 80. Focused August 12 commit trail — source/security/tests

The continuation intentionally used many focused commits instead of one giant source commit.

Major observed focused commits include:

- `27161b7e57f24b3b61205347ef474150a33da582` — security: require canonical Base64URL pairing query encoding;
- `57c98c31de55f2e805668796aa83a68269eced22` — test: cover canonical pairing query and Base64URL transport;
- `f8120cfc2858745bd1a79450cf07f18f3416bbda` — security: reject whitespace-wrapped pairing capabilities;
- `ce3baeb7c77dda27b8b8304b4094dfdec4211eac` — test: reject whitespace-wrapped pairing links;
- `fec05363e41ed6e7938bbfc90b0b92955132953c` — security: centralize strict portable relative path parsing;
- `05e88c7c1fb1eae44bfc3e38b54d4994a48c1425` — security: reject ambiguous empty transfer path segments;
- `713f010dc47a91d20b3e8d2308c0bada10f386bb` — security: make receive path guard reject ambiguous separators;
- `a0350e751303f8a461886498c1e1ef5e8c595033` — test: cover strict portable relative path parsing;
- `a8fca28eb8b2391d9632246654ebe319b7dd6f34` — test: reject ambiguous paths at receive root boundary;
- `e2c206f34b3920e0f68d1cc65c0133d30b557d32` — test: lock strict relative path sanitation boundaries;
- `54914b48287397a581617f94007773746ad4c0d3` — security: bound portable relative path nesting depth;
- `5586d70ba574f9637fd4e600cc083789cec8cc07` — security: validate portable manifest paths before authorization;
- `6fa77d75a24431b94d28c2c40d58232d0edc7320` — security: require canonical sanitized paths in wire manifests;
- `67d9439c7a2897ba9a7f3d40bc18c1bb5d146f20` — test: cover strict portable manifest path structure;
- `bd670076e2ef1c14a4f7382a26b0032f0aecb0a7` — test: prove malformed paths do not consume transfer authorization;
- `bf8aa7b973a0f394718bbd9e2c60925d215b9193` — test: reject noncanonical sanitized wire manifest paths;
- `83ebdefd99e71a1ec2e71de032d72ce97a45fc7b` — test: avoid ambiguous join overload in path-depth coverage;
- `d10835f9617698647dd90e4cb6530c579f946081` — fix: canonicalize transfer relative paths to forward slashes;
- `23eb74e0832e027b357c68788fc59d32cd2a04d1` — fix: keep sender batch deconfliction wire-path canonical;
- `c716ba0b4bf35af068e7735c0c4d3a0e8365a059` — test: lock forward-slash canonical relative paths;
- `629b5f0fadb4d78fd2fb7e144de1bbdbacb1265a` — security: add reusable transfer staging budget policy;
- `62e305d190eae14c735000e3305483e8dbff3340` — test: cover reusable staging count and byte budgets;
- `1615480b9c6231c97be4133b08a873d61438b31d` — security: enforce Apple share aggregate budget before provider copy;
- `7c860aec2b4684a01a55ea8fa97339c01ad6f4c0` — security: enforce Android share aggregate budget during staging;
- `5a4649ce157b28851b38e2dc362a3ea734d57b30` — refactor: share staging budget policy with Mac drop intake;
- `8ad3a2cc28faa82a124fc42543a330f9ca41dc5a` — reliability: bound Mac drop provider response waits;
- `ea80b55d67f1942ee6c3d7381e1c754de8fde91b` — reliability: preflight aggregate Apple share import capacity;
- `ded8c18e4887ce20ebfea2e7a3b36cbfd960b297` — reliability: centralize existing transfer source path filtering;
- `becd7e8750b5aa7c2c6d8aa04279bf634ac54894` — test: cover file and folder resume source filtering;
- `069286577819d84409eb51fd5a0ac2e807f99c35` — fix: preserve folder sources and portable dedup in batch resume;
- `5baea43ff447c7d15986d0be92baa1dd6c7a002c` — security: drop linked sources from paused resume state;
- `09e5bd595d128854b8f2578e0033148f80ac79bd` — test: drop swapped symlink sources from resume candidates;
- `1edaa69dd5fa40c1112b04af7c25e35f5eaa5632` — security: add bounded link-safe deterministic folder enumeration;
- `8e2a566f47623e423ccb910d2d6cee8db61238d9` — test: cover deterministic bounded link-safe source enumeration;
- `db57b5b139498b08b6b3eaf4b6833fea231efd86` — security: use link-safe deterministic enumeration for folder transfers;
- `110ca5a75aa4b08ffe5560889b8b3808eba506c6` — refactor: centralize source link rejection policy;
- `faced680e74f71eb2140e968a822bab9cd5405c9d` — refactor: reuse central transfer source safety in enumeration;
- `1f575e286a3052590aca7b21e1ec281025a1e15d` — refactor: reuse regular source validation in batch builder;
- `56534e6923dfae7ebf68236671151bd1f769ab27` — test: lock deterministic folder manifests and source link rejection;
- `413d2bee68ab8cbfa0ab803df53db9a260e42508` — security: centralize regular transfer source validation;
- `6718a9151ba859864e37f050a31302562f55c147` — test: cover regular file and directory source safety;
- `ce86aaf9d61eefec4c4040c5514430c273e99119` — security: reject linked sources in single-file sends;
- `5304acf5121cff72a151acd05e479e88520c072b` — security: revalidate regular source file at send boundary;
- `9c681500143b87122103365768d2743cf487de26` — test: cover transfer engine send-boundary source safety;
- `86253c4e64ac8bc63c5574771454234c3500719e` — reliability: deconflict portable batch paths before hashing;
- `416cf8997cba93f17f2820cf81498b2638c0c0b8` — test: cover portable sender path deconfliction before hashing;
- `445992173c56cd684ad5f8ce7128639924cb9cb8` — security: bound portable filename segments by UTF-8 bytes;
- `f79676124261da976821906eba7985528dafb8b4` — test: cover UTF-8 byte bounded portable filenames;
- `5b3610824787ea08aaa6ae454b3d18e3d01afc96` — reliability: preserve collision markers under filename bounds;
- `4d49f0d6023d65b01ec0893bcfeb950428205284` — reliability: keep receive collision suffixes within filename bounds;
- `dfc7ec4db7ceca88286454e457592b76deec01e3` — reliability: bound generic collision-free filenames;
- `ceac9a8cd4ae12565e1f52b60453e4112d5a2e53` — reliability: reuse bounded collision naming in batch sender;
- `a49bc2e14b77558bbf7cc24300af61874b65d367` — reliability: reuse bounded collision naming in Apple share packages;
- `46401de21de7bedba8fa81f9b47b195a8a2c4ca3` — test: ensure collision markers survive filename bounds;
- `b91ec991e7b60d94cf16d8dc5944c5d6c1dd2b30` — test: cover bounded receive collision reservations;
- `3a51842298ef2b3f49a0716c41442df31d59708d` — reliability: preflight batch path length before hashing;
- `bbf59e2a122e768debbbcb2ba3c6e55755c1537c` — security: require canonical token syntax for batch transfer IDs;
- `502288555092614f8e195f9f10fdfbf415bbff34` — test: cover canonical transfer ID token syntax.

Additional focused commits later in the frozen continuation include:

- reliability: preserve storage reserve for unknown Android share sizes;
- refactor: remove obsolete fresh-ID batch handlers and harden single resume;
- refactor: remove obsolete fresh-ID batch compatibility overload;
- reliability: make resume source filtering nullable-safe;
- documentation synchronization commits for wire protocol, security, threat model, tests, release checklist, platform status, README, project status, roadmap, and changelog.

Every focused commit where commit-message control is available uses the requested Signed-off-by identity. One early filename-byte-bound commit used the same mailbox with case variation in the trailer; subsequent commits use the requested lowercase form.

---

# 81. Prior focused hardening retained from August 10–11

Important earlier commits/features remain part of the current source and are not superseded by this continuation:

- strict pairing decoded JSON duplicate/unknown controls;
- reusable one-time authorization store;
- incoming/response policy extraction;
- complete typed protocol models/factories/validators/authorizer;
- strict unknown-member framed JSON;
- active receive session tracking/drain;
- receive-root symlink/reparse rejection;
- non-overwrite final promotion;
- stable batch ID workflow;
- schema-v3 completion persistence;
- first completed-file fresh rehash while creating retry plan;
- second completed-file rehash before zero-byte ACK;
- Apple Share Extension/App Group target;
- exact App Group file-set validation;
- Mac native drop;
- Android share staging;
- Windows native drop;
- English/Hindi runtime localization/MVVM;
- CodeQL/security/release workflows;
- release/platform/security documentation.

The current source must be evaluated as the cumulative repository state, not only the August 12 commits.

---

# 82. Current source-completion assessment

For the current master-prompt scope, the repository contains source implementations for:

- local identity/trust;
- mDNS + UDP + QR + code + manual pairing;
- canonical short-lived pairing capabilities;
- mutual TLS and receiver certificate pinning;
- strict typed closed-schema protocol;
- one-time authorization;
- canonical cross-platform file manifest paths;
- single/multi/folder/text transfer;
- selective receive;
- pause/resume/cancel/retry;
- stable batch IDs;
- idempotent completed-file batch retry;
- repeated completed-file verification;
- receive path/collision/capacity/integrity protection;
- outgoing source link/reparse safety;
- deterministic folder enumeration;
- local SQLite metadata/history/privacy/diagnostics/queue;
- Android share intake/background transfer lifetime;
- Windows protocol/folder/drop integration;
- iOS/Mac document URL intake;
- iOS/Mac Share Extension/App Group source;
- Mac native drag/drop source;
- common external staging budgets;
- settings/about/trust/history/diagnostics/queue UI;
- English/Hindi localization infrastructure/runtime coverage;
- portable tests/CI/release docs.

The current phase is therefore:

**source-complete for the current scope and awaiting exact-candidate release validation**

not production-verified.

---

# 83. Deliberate non-claims / optional future enhancements

SwiftDrop does not falsely claim:

- arbitrary iOS/Android background sockets survive suspension;
- file-extension warnings are malware scanning;
- source App Group entitlements prove real Apple provisioning works;
- configured CI jobs passed when status evidence is absent;
- source compile equals signed/store-ready package;
- local metadata/filesystem checks protect against a fully compromised OS/kernel;
- optional native completion/failure notifications exist on every target;
- provider/content-resolver behavior is validated by portable tests alone.

Optional post-v1 enhancements may include:

- native optional completion/failure notifications on Apple/Windows;
- additional store-compliant background continuation;
- broader localization;
- representative-device performance telemetry/benchmarks;
- additional property/state-machine fuzzing;
- trustworthy platform malware-scan integration only where a supported OS API exists.

These are optional enhancements, not hidden missing correctness items in the current master-prompt source scope.

---

# 84. External validation still required

Repository source changes cannot honestly complete these release gates.

## Automated candidate evidence

- Observe all configured GitHub Actions jobs successfully complete for the exact final commit.
- Verify portable Core restore/build/tests.
- Verify localization validator.
- Verify Apple integration metadata validator.
- Verify benchmark compile.
- Verify Android/Windows/Mac/iOS Simulator compile jobs.
- Verify CodeQL/security-hygiene/release-readiness gates.
- Review dependency inventory artifacts.

## Android

- signed AAB/APK build;
- install/upgrade;
- provider URIs with declared/null/negative/wrong/changing size;
- unknown-size aggregate/runtime cap;
- low-storage reserve during unknown-size copy;
- foreground service/notification policy;
- multicast/discovery on physical Wi-Fi;
- vendor battery restrictions;
- backup-disabled behavior;
- TalkBack/large text/Hindi/background behavior.

## Windows

- signed package build/install/update;
- firewall behavior;
- packaged protocol activation;
- receive FolderPicker persistence;
- packaged native drop;
- regular source/link behavior;
- Windows→Android/iOS/Mac canonical `/` folder manifest interoperability;
- max-length Unicode collision behavior;
- Narrator/keyboard/high-contrast/high-DPI.

## Apple

- real Apple Developer App Group configuration;
- containing app + Share Extension provisioning profiles;
- signed iOS physical-device build;
- TestFlight/App Store extension embedding;
- signed Mac Catalyst sandbox/App Group/Share Extension;
- provider-response timeout behavior with real `NSItemProvider` sources;
- extension dismissal/cancellation;
- App Group cold/warm import;
- exact physical package rejection cases;
- aggregate cache capacity behavior;
- multiple pending packages;
- native Mac security-scoped drop/provider behavior;
- notarization/store package.

## Cross-device/network/filesystem

- physical Android/iOS/Mac/Windows directional matrix;
- QR/nearby/code/manual pairing;
- canonical pairing alias rejection;
- canonical `/` folder manifest identity;
- source symlink/reparse cases;
- receive-root symlink/reparse cases;
- guest Wi-Fi/client isolation;
- multicast filtered/direct IP;
- IPv4/IPv6 LANs;
- firewalls/local-network permission;
- network switching;
- sleep/lock/background;
- low storage during receive and external staging;
- multi-gigabyte files;
- many-file/folder batches;
- max-length Unicode/collision names;
- completed destination mutation between retry plan and zero-byte ACK.

## Secure storage/database

- real SecureStorage/keychain/keystore lock/upgrade/restore behavior;
- real v1/v2 database upgrade to v3;
- corrupt local metadata recovery on target devices.

## Accessibility/localization

- TalkBack;
- VoiceOver iOS/Mac;
- Narrator;
- keyboard-only navigation;
- largest text scaling;
- high contrast;
- reduced motion;
- responsive/window/rotation behavior;
- Hindi wrapping/runtime dialogs/statuses.

## Release/legal/store

- exact signed dependency graph review;
- final third-party notices;
- store privacy/data declarations;
- screenshots/metadata;
- signing/notarization;
- final privacy/security review against shipped binaries.

---

# 85. Connector/environment limitations

During this implementation session:

- the active chat runtime does not provide the full .NET/MAUI workloads required to compile/sign all target apps/extensions locally;
- GitHub connector Contents API writes were used for focused commits;
- Contents API commit operations do not expose an independent author/committer-email override;
- focused commit messages therefore use the requested Signed-off-by trailer;
- GitHub combined status responses for direct-main commits may return no status contexts.

No status contexts means:

**unknown/unreported**

not success.

This ledger does not claim local or GitHub Actions pass without actual execution evidence.

---

# 86. Definition used by this ledger

**Implemented in source** means repository source/tests/docs contain the behavior/policy.

**Portable-tested in source** means a test exists; it does not mean that test executed successfully for the final commit unless CI/local run evidence exists.

**Automated-validated** means the relevant build/test actually executed successfully for the exact commit in the correct environment.

**Platform-validated** means a signed/packaged target build actually ran successfully on the relevant OS/device/runtime/provider/filesystem.

**Production-ready** requires:

- exact-candidate automated source gates pass;
- target apps/extensions compile in release environments;
- signed packages install/upgrade;
- Apple App Group/provisioning works;
- real providers/native drop/content resolver behave correctly;
- cross-device/network/resume/path/link/low-storage tests pass;
- accessibility/localization checks pass;
- privacy/security documentation matches binaries;
- dependency/license review is complete;
- store declarations/metadata match shipped behavior.

SwiftDrop must not be described as production-verified until those external gates are completed.

---

# 87. Final engineering boundary after August 12 source freeze

The highest-value next work is no longer broad feature generation. It is release-candidate evidence and defect closure:

1. observe exact final CI/release-readiness jobs;
2. fix any compile/test defect exposed by those jobs and add regression coverage;
3. configure Apple Developer App Group/provisioning and validate app + Share Extension;
4. build/sign/install Android, Windows, iOS, and Mac Catalyst packages;
5. execute cross-device transfer matrix including Windows→non-Windows canonical folder paths;
6. exercise real Android providers and Apple/Mac `NSItemProvider` timeout/cancellation paths;
7. execute source/receive symlink-reparse tests on representative filesystems;
8. execute idempotent batch retry including mutation between plan and zero-byte ACK;
9. execute low-storage, network, lifecycle, accessibility, and Hindi validation;
10. review exact dependency/license artifacts;
11. align store metadata/privacy declarations with signed binaries;
12. tag/release only after exact-candidate evidence is complete.

See:

- `NEXT_STEPS.md`;
- `docs/testing/security-test-plan.md`;
- `docs/testing/manual-test-matrix.md`;
- `docs/release/release-checklist.md`.

---

# 88. Final repository-write rule for this continuation

This `what_changed.md` commit is intentionally the **final repository file write** of the August 12 continuation.

After this ledger write:

- no source file is modified;
- no test file is modified;
- no documentation file is modified;
- no repository file is created/deleted;
- only final HEAD/status evidence is read.

This preserves the user's requirement that the detailed ledger reflect the exact frozen repository state.

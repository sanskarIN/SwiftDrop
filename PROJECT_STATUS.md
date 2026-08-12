# SwiftDrop Project Status

Updated: 2026-08-12

## Implemented in source

### Product and transfer foundation

- Apache-2.0, .NET 10, C#, .NET MAUI.
- Android, iOS, Mac Catalyst, and Windows application targets.
- Reusable `SwiftDrop.Core`, portable xUnit tests, and synthetic benchmark project.
- Account-free direct local-network transfers with no SwiftDrop-operated cloud upload/relay path.
- mDNS/DNS-SD discovery plus bounded UDP fallback and peer expiry.
- QR/deep-link pairing, nearby pairing requests, one-time 8-digit codes, and local-IP fallback.
- Single-file, multi-file, recursive-folder, and explicit text transfer.
- Receiver file/batch consent including selective batch acceptance.
- Pause/cancel/fresh-pair resume.
- Stable batch IDs plus verified already-completed-item reuse so an interrupted batch does not resend finalized files under collision-renamed names.
- Configurable queue/concurrency and progress/throughput/ETA UI.
- Windows custom receive folder and native files/folders/text/pair-link drag/drop.
- Android bounded share-sheet intake.
- iOS/Mac Catalyst file URL/document opening.
- Dedicated iOS/Mac Catalyst Share Extension source target using App Group package handoff.
- Native Mac Catalyst `UIDropInteraction` for files, folders, text, and pairing links.
- Warm/cold external-input application handoff.

### Canonical pairing and protocol representation

Pairing capability text is now accepted in one strict representation:

- no leading/trailing whitespace;
- exact `swiftdrop://pair` URI structure;
- exactly one raw `p=` query field;
- no empty/unknown/duplicate query fields;
- unpadded canonical Base64URL payload text only;
- standard Base64 `+`, `/`, `=` and percent-encoded aliases rejected;
- decoded pairing JSON rejects duplicate and unknown members;
- protocol version, local numeric address, port, fingerprint, nonce, identity fields, and lifetime remain bounded/validated.

Application framed JSON remains closed-schema and typed:

- bounded frame length/depth;
- strict UTF-8/JSON;
- no comments/trailing commas;
- case-insensitive duplicate-member rejection;
- unknown/unmapped-member rejection;
- type-specific request-shape validation;
- cross-type field-smuggling rejection.

### Canonical cross-platform manifest paths

Protocol-v1 file manifest paths now have one operating-system-independent identity:

- `/` is the only wire separator;
- incoming backslash aliases are rejected rather than silently normalized after authorization;
- rooted/drive/UNC/device paths rejected;
- empty/repeated/trailing separators rejected;
- `.`/`..` traversal rejected;
- maximum 64 segments;
- bounded total relative-path metadata;
- every incoming manifest path must already equal SwiftDrop's canonical sanitized form before one-time authorization is consumed.

Canonical filename segment policy now includes:

- Unicode NFC;
- portable invalid/control-character removal during sender/local source construction;
- Windows reserved-device-name neutralization;
- trailing dot/space handling;
- 180 UTF-16 code-unit bound;
- 180 UTF-8 byte bound without splitting Unicode scalars;
- headroom for `.swiftdrop.part` on common byte-limited filesystems;
- collision-generated names whose uniqueness marker survives truncation at maximum length.

Windows sender folder paths are therefore advertised with `/` and match Android/iOS/Mac receiver-plan paths exactly.

### Outgoing source safety and deterministic folder manifests

Single-file sends:

- require a regular non-link/non-reparse source;
- repeat regular-source validation at the stream-open boundary;
- bind bytes to manifest-declared length;
- fail if size changes before/during streaming.

Folder/multi source construction:

- rejects selected symlink/reparse folder roots;
- rejects symlink/reparse files/directories anywhere below the selected root;
- uses explicit bounded traversal instead of unrestricted recursive enumeration;
- bounds file and directory traversal;
- sorts resulting source files deterministically by normalized relative path;
- constructs canonical `/` manifest paths;
- deconflicts case-only, Unicode-normalization, and sanitation-equivalent portable destination paths before hashing;
- preflights count/per-file/aggregate/path-length constraints before expensive hashing where knowable;
- uses regular-source validation again for pending files.

Paused source retention:

- preserves valid selected files and folders;
- uses platform-aware local path deduplication;
- drops a source that becomes missing or a symlink/reparse point before resume;
- single-file resume also validates a regular source before consuming the fresh pairing capability.

The old duplicate MainPage batch handlers and implicit fresh-ID coordinator compatibility overload have been removed. The active XAML batch controls call the stable-transfer-ID workflow only.

### Security and privacy

- SecureStorage-backed P-256 ECDSA local identity certificate/private key.
- Certificate validity/renewal/recovery policy and explicit re-pair notice after identity regeneration.
- TLS 1.2/1.3, receiver certificate pinning, sender client certificate, receiver certificate-derived sender fingerprint.
- One-time transfer authorization consumed only after strict request/manifest validation and authenticated client certificate are present.
- Malformed/noncanonical paths do not consume valid authorization.
- Separate bounded pairing attempt limiting.
- Trusted-device persistence bound to device ID plus exact canonical SHA-256 certificate fingerprint.
- Strict pairing URI/encoded JSON representation.
- Shared typed Core wire records/factories/validators/authorizer used by production sender/receiver/pairing code and tests.
- Manifest-bound sender byte counts, exact receiver byte counts, SHA-256 final verification, and invalid-partial cleanup.
- Portable rooted/traversal checks including Windows-style path syntax on non-Windows hosts.
- Existing receive-root symlink/reparse components rejected around staging/final promotion.
- Atomic concurrent destination reservations and non-overwrite final promotion.
- Collision candidates remain bounded/distinct even when original filename is at character/byte limit.
- Optional last-write timestamp application after verified promotion is best-effort and cannot turn verified content into a false transfer failure.
- Android backup disabled for app-local metadata.
- Windows package restricted to private-network capability.
- Privacy-aware history, diagnostics, queue persistence, and bounded external staging.

### Idempotent batch resume

SQLite schema-v3 `completed_batch_items` stores metadata only:

- stable transfer ID;
- canonical source relative path;
- hashed receive-root identity;
- effective local destination relative path;
- length/SHA-256;
- completion timestamp.

Before a retry receives a full-length resume offset, SwiftDrop verifies the same transfer/root/source/length/hash, rejects reparse-path destinations, confirms the destination still exists at the expected length, and re-hashes it.

After the sender returns the matching `BatchItemStart`, SwiftDrop performs the completed-file verification **again immediately before sending the zero-byte completion ACK**. A destination changed/deleted/replaced between plan creation and acknowledgement therefore fails closed and cannot be falsely reported complete.

A new explicit send uses a new transfer ID, preserving normal collision-safe duplicate-send semantics. Stable retry IDs use bounded ASCII token syntax (letters, digits, `-`, `_`).

Resume metadata is best-effort and never authorization; persistence failure does not change file-transfer success.

### Cross-platform external intake and staging budgets

Shared Core `TransferStagingBudget` now provides one source of truth for:

- maximum staged file count;
- maximum single-file bytes;
- aggregate staged bytes;
- commit-after-success semantics so a failed copy does not consume budget.

Android:

- content URI count/per-file/aggregate bounds;
- portable/UTF-8-bounded filename sanitation;
- declared-size validation where available;
- negative provider size treated as unknown;
- unknown-length runtime byte cap based on remaining aggregate budget;
- repeated storage-reserve checks during unknown-length streaming;
- exact staged-length verification;
- cleanup on failure;
- atomic inbox handoff.

Apple Share Extension:

- files/images/movies/text/web URLs under explicit activation bounds;
- bounded provider-response waits plus extension-lifetime cancellation;
- late timed-out/cancelled callbacks cannot begin a new copy;
- provider-response timeout does not cancel an already-started legitimate copy;
- aggregate staging budget checked before copying an over-limit file;
- security-scoped access where provided;
- bounded collision-safe filenames;
- strict App Group package manifest and atomic `.staging-*` → `pending-*` publication;
- never auto-sends.

Containing Apple app:

- serialized App Group import;
- strict JSON/unknown-member/package-age/version/ID validation;
- package/manifest/files/file symlink/reparse rejection;
- exact physical file-set validation: undeclared extra files or nested directories are rejected;
- exact file-length checks;
- aggregate validated package bytes preflighted against app-cache capacity before recopy;
- re-stages accepted files into app cache before review;
- surfaces one pending package for review at a time rather than silently merging later packages.

Mac Catalyst native drop:

- files/folders/text/pairing links;
- temporary security-scoped access;
- shared count/per-file/aggregate staging budget;
- bounded provider-response waits;
- symlink/reparse rejection;
- portable UTF-8-bounded collision handling;
- never auto-sends.

Windows native drop:

- explicit files/folders/text/pairing-link input;
- atomic common inbox handoff;
- direct file/folder sources still pass the shared regular-source/link-safe/canonical-manifest pipeline before transfer.

### Local metadata

SQLite schema version: **3**.

Tables:

- certificate-bound trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal restart queue metadata;
- verified completed-batch resume metadata.

Transfer bytes/text, private keys, pairing invitations/nonces, receive-root absolute paths, and reusable authorization are not stored in SQLite.

### UI, localization, and architecture

- Main dashboard presentation state uses `MainViewModel`.
- History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About use dedicated view models.
- Active single-file controls validate regular source state before send/resume.
- Active batch controls use the dedicated stable-ID partial workflow.
- Obsolete duplicate batch handlers/fresh-ID compatibility overload removed.
- Pickers/dialogs/native drop/share/lifecycle remain at the platform/UI boundary.
- Networking, TLS, certificates, storage, path policy, protocol, hashing, source safety, and authorization remain in services/Core.
- English/Hindi XAML/runtime catalogs cover the implemented UI surfaces.
- CI validates catalog XML, non-empty values, duplicate keys, key parity, and placeholder parity.
- Theme/language/accessibility preferences are applied through app services/resources.

### Testing and hostability

Portable tests cover, among other areas:

- strict/canonical pairing query/Base64URL/whitespace handling;
- pairing/identity/fingerprint/certificate policy;
- strict JSON including unknown members and duplicates;
- typed protocol request factory/shape validation;
- canonical manifest paths, rooted/traversal/empty-segment/depth/noncanonical rejection;
- malformed path rejection before one-time authorization consumption;
- canonical transfer-ID token syntax;
- one-time authorization consume/replay behavior;
- complete framed file/batch/text/pair conversation sequencing;
- real mutual-TLS loopback pinning/file/resume tests;
- transfer interruption/source/staging mutation/integrity cleanup;
- regular-source/symlink rejection at source-builder and send-engine boundaries;
- deterministic bounded link-safe folder enumeration;
- portable sender collision deconfliction;
- UTF-8 filename byte caps and collision marker preservation;
- stable batch IDs and completed-file revalidation including mutation between repeated verification passes;
- schema v0/v1/v2→v3 migrations and completion-store behavior;
- receive-root symlink/reparse rejection;
- path/collision/final-promotion races;
- reusable staging count/per-file/aggregate budgets;
- exact external share-package physical file sets;
- discovery parser fuzz/truncation/pointer loops/duplicate metadata;
- session-tracker drain/fault/cancellation/race behavior;
- privacy redaction;
- Unicode UTF-8 text truncation.

### CI/build/release engineering

- Canonical solution: `SwiftDrop.slnx`.
- Stable C# language mode (`latest`, not preview).
- Portable Core build/test/localization/Apple-metadata validation/benchmark compile configured.
- Android, Windows, Mac Catalyst, and unsigned iOS Simulator compile workflows configured.
- Apple jobs explicitly compile the Share Extension and containing app.
- Apple metadata validator checks App Group, app/extension IDs, versions, targets, entitlements, sandbox, activation rule, project reference, Core constant, and solution inclusion.
- Release-readiness includes extension dependency inventories for iOS and Mac Catalyst.
- CodeQL, Dependabot, security-hygiene and release-readiness workflows remain configured.

## Current engineering phase

**Source-complete release-validation phase for the current master-prompt scope, with additional August 12 canonical-protocol/source/staging hardening implemented but not yet physically/signed validated.**

The source now includes the previously implemented Apple Share Extension/Mac native drop/schema-v3 resume plus this continuation's canonical path representation, source-link safety, deterministic folder enumeration, external staging budgets, strict capability representation, bounded filename/collision behavior, stable-ID-only active app batch path, and repeated completed-file verification.

## Remaining source boundaries / deliberate constraints

These are deliberate platform/release boundaries or optional future enhancements rather than hidden TODO implementations:

1. **Optional completion/failure system notifications outside Android**
   - Android implementation exists.
   - Unsupported targets disable the optional preference instead of pretending notifications exist.
   - Native Apple/Windows notifications are optional post-v1 work, not transfer correctness/security.

2. **Mobile background continuation**
   - SwiftDrop does not claim arbitrary sockets survive OS suspension.
   - Any additional continuation must use store-compliant supported platform mechanisms and be physically validated.

3. **Malware scanning**
   - SwiftDrop provides extension-risk warnings and transport integrity, not a fake cross-platform malware scanner.
   - Platform malware APIs should only be added where a trustworthy supported API exists.

4. **Performance numbers**
   - Synthetic benchmark source exists.
   - Real Wi-Fi/TLS/device throughput/CPU/memory figures require representative hardware.

5. **Final binary third-party notices**
   - Workflow inventories dependencies.
   - Exact licenses/notices must be reviewed against the restored signed-release graph.

## External validation still required before production-ready claims

Repository source edits cannot honestly complete these gates:

- observe all current GitHub Actions jobs successfully complete on the exact release candidate;
- signed Android AAB/APK build/install/upgrade;
- signed Windows package build/install/update;
- Apple Developer App Group provisioning for app + Share Extension;
- signed iOS device/TestFlight Share Extension/provider behavior;
- signed Mac Catalyst sandbox/App Group/Share Extension/native-drop/provider behavior;
- physical Android/iOS/macOS/Windows transfer matrix in both directions;
- Windows→non-Windows folder manifests using canonical `/` paths;
- real content providers with null/negative/changing Android size metadata;
- real guest Wi-Fi/client isolation/multicast/firewall/local-network-permission tests;
- IPv4/IPv6 combinations, network changes, sleep/lock, low storage, multi-gigabyte files, large batches;
- source/receive symlink-reparse cases on representative filesystems;
- completed-item mutation between retry plan and zero-byte ACK;
- real SecureStorage/keychain/keystore upgrade/restore/locked-device scenarios;
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, high-contrast, and Hindi layout validation;
- final store privacy declarations, screenshots, signing/notarization, dependency-license review, and submission checks.

## Connector/environment limits

- The active chat runtime does not provide the .NET/MAUI workloads required to compile/sign all targets locally.
- GitHub status evidence must be checked for the exact final commit after the final repository write; absence of status contexts is **unknown/unreported**, not a pass.
- Contents API writes do not expose an independent author/committer-email override; focused commits use `Signed-off-by: Sanskar <sanskarin@outlook.in>`.

See `NEXT_STEPS.md` for release validation priorities and `what_changed.md` for the complete engineering ledger.

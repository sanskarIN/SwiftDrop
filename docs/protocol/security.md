# SwiftDrop protocol security

Updated: 2026-08-12

Protocol version: `1`

## Pairing invitation

`swiftdrop://pair?p=...` carries receiver device metadata, numeric local address, port, receiver SHA-256 certificate fingerprint, expiration, and a cryptographically random one-time nonce. The payload is encoded for transport, **not encrypted**; treat it as a short-lived capability.

`PairingCodec` treats both the outer URI and decoded JSON as untrusted input and enforces:

- bounded overall/encoded sizes and JSON depth;
- strict JSON with case-insensitive duplicate-property rejection;
- unknown decoded JSON members rejected;
- comments/trailing commas rejected;
- exact `swiftdrop` scheme and `pair` host;
- no surrounding whitespace;
- no unexpected authority port/path/fragment/user-info;
- exactly one raw `p=` query field;
- no unknown/empty/duplicate query fields;
- unpadded canonical Base64URL payload text using only ASCII letters, digits, `-`, and `_`;
- standard Base64 `+`, `/`, padding `=`, percent-encoded aliases, and non-canonical re-encodings rejected;
- exact protocol version;
- bounded non-control device ID/name;
- numeric loopback/private/link-local/unique-local address only;
- port 1–65535;
- canonical SHA-256 certificate fingerprint;
- bounded base64url-style nonce;
- future expiration within the maximum invitation lifetime.

Public Internet addresses, DNS hostnames, ambiguous pairing JSON, and alternate textual encodings of the same capability are rejected by protocol v1.

## TLS and peer identity

SwiftDrop uses .NET/platform TLS 1.3/1.2, not custom encryption/key exchange.

- sender pins the receiver certificate SHA-256 fingerprint learned during pairing;
- sender presents its own local device certificate;
- receiver requires a TLS client certificate;
- receiver derives sender fingerprint from the authenticated TLS channel, never an application JSON claim;
- trusted-device records require device ID plus exact canonical certificate fingerprint;
- display name alone never establishes trust.

Local identity certificates are P-256 ECDSA self-signed certificates with non-CA constraints, digital-signature key usage, TLS server/client EKUs, subject key identifier, and bounded validity. Private-key material is held through MAUI `SecureStorage`, not SQLite, pairing links, diagnostics, history, or source files.

Unusable/corrupt/expired/near-renewal stored identity creates a new device ID/certificate and invalidates old pairing capabilities; the user is told that peers must pair again.

## Strict typed application JSON

Application frames use a 4-byte big-endian length plus UTF-8 JSON. Before typed deserialization SwiftDrop enforces size/depth/UTF-8/JSON/duplicate-property rules. Typed deserialization also uses `JsonUnmappedMemberHandling.Disallow`, so **unknown members are rejected** rather than silently ignored.

Production sender, pairing client, receiver, and tests share Core wire records. `ProtocolRequestValidator` enforces type-specific fields:

- file requests cannot carry text/batch/pair fields;
- batch requests require canonical transfer ID, manifests, and declared total;
- text requests require bounded text and valid expiration;
- pair requests cannot carry transfer authorization.

Cross-type field smuggling is rejected.

## Canonical manifest paths before authorization

Incoming file paths are validated as protocol identity, not treated as strings to rewrite after authorization.

Protocol-v1 manifest paths must:

- use `/` as the only wire separator;
- contain no rooted/drive/UNC/device syntax;
- contain no empty/repeated/trailing separators;
- contain no `.` or `..` traversal segments;
- contain at most 64 path segments;
- stay within the 1,024-character manifest path limit;
- contain no control characters;
- already equal SwiftDrop's canonical sanitized representation.

Canonical segment policy applies Unicode NFC, portable invalid-character removal, Windows reserved-device-name neutralization, trailing dot/space handling, and limits each segment to both 180 UTF-16 code units and 180 UTF-8 bytes. The byte cap leaves room for SwiftDrop staging/collision suffixes on common byte-limited filesystems.

A peer-supplied path containing backslashes, invalid filename characters, decomposed aliases that normalize differently, reserved-device names, or other values that would change during sanitation is rejected before one-time authorization is consumed.

## Authorization ordering and replay resistance

For file/batch/text:

1. strict framed JSON is parsed;
2. protocol/request shape is validated;
3. file/batch manifest metadata, including canonical path structure, is validated;
4. authenticated TLS client certificate must exist;
5. the one-time pairing nonce is atomically consumed;
6. receiver consent/trust policy is evaluated;
7. transfer negotiation begins.

Malformed requests, malformed paths, or missing client certificates do not consume authorization. A consumed nonce cannot authorize a replay. Pause/retry/resume requires a fresh pairing capability even when receiver partial/completion metadata is reused.

Pair requests use separate rate limiting, optional short code, receiver approval, and certificate-bound response. They do not consume transfer nonces.

## Outgoing source safety

SwiftDrop does not intentionally follow symbolic links/reparse points as transfer sources.

Single-file sender:

- validates the selected source as a regular non-link/non-reparse file;
- repeats that validation at the actual stream-open boundary;
- binds streaming to manifest-declared length;
- fails if source size changes before/during the transfer.

Folder/batch sender:

- rejects a selected root that is a symbolic link/reparse point;
- performs an explicit bounded recursive traversal instead of unrestricted `AllDirectories` enumeration;
- rejects linked/reparse files or directories anywhere below the root;
- bounds traversed file/directory counts;
- sorts relative paths deterministically before manifest construction;
- canonicalizes all wire paths to `/`;
- deconflicts case/Unicode/sanitation-equivalent destination paths before hashing;
- preflights path length, count, per-file size, and aggregate size before expensive hashing.

Paused single/batch resume state retains only sources that still exist as regular non-link/non-reparse sources. A source swapped to a symlink after pause is dropped from resume candidates.

## Single-file integrity and destination safety

Receiver:

- validates canonical manifest/path/size/hash/timestamp metadata;
- confines destination beneath the approved receive root;
- rejects existing symlink/reparse components under the receive root;
- reserves a collision-safe destination;
- preflights storage;
- stages to `.swiftdrop.part`;
- validates resume offset/staged length;
- receives exactly the remaining bytes;
- hashes the complete staged file with SHA-256;
- compares digest in constant time;
- promotes only after verification;
- uses non-overwrite final promotion so a file created concurrently is preserved rather than replaced.

Receive path checks are repeated around staging/promotion to reduce path-redirection races. Optional last-write timestamp application happens after verified promotion and is best-effort; inability to apply metadata does not falsely convert verified content into a failed transfer.

Portable collision-generated filenames are length/UTF-8 bounded. When a normal suffix such as ` (1)` would be truncated away, a bounded prefix marker is used so collision candidates remain distinct.

Platform/filesystem policy remains an external boundary against a fully compromised local OS.

## Batch safety and idempotent resume

Both peers enforce per-file/count/aggregate limits. Receiver also preflights the accepted remainder against free storage.

Batch `transferId` uses canonical bounded ASCII token syntax: letters, digits, `-`, and `_` only. An interrupted batch keeps a stable random transfer ID; a new explicit send gets a new one. The obsolete app compatibility path that implicitly generated a new ID per call has been removed, leaving the stable-ID API as the active batch send path.

After a batch item is verified and finalized, SwiftDrop may store metadata-only completion state containing:

- stable transfer ID;
- canonical source relative path;
- SHA-256 hash of the receive-root identity (not its absolute path);
- effective local destination relative path;
- length/SHA-256;
- completion time.

On retry SwiftDrop requires the same transfer ID, root key, source path, destination metadata, length, and SHA-256 before offering a full-length zero-byte resume. The destination is path-confined, reparse/symlink checked, length checked, and freshly SHA-256 hashed while the batch response plan is created.

After the sender returns the matching `BatchItemStart`, **SwiftDrop verifies the completed destination again immediately before the zero-byte completion acknowledgement**. If the destination is removed, changed, redirected, or no longer maps to the recorded completion metadata between planning and acknowledgement, the receiver invalidates/fails that shortcut instead of falsely acknowledging stale bytes.

Changed/missing destinations, changed source manifests, different roots, or new transfer IDs therefore cannot use completed-item reuse. A later retry can safely transfer data again. A brand-new explicit send gets a new transfer ID and retains normal collision-safe duplicate-send behavior.

Completion metadata is an optimization, never authorization. Persistence failure does not make an already verified transfer fail.

## External intake security

External staging paths share reusable `TransferStagingBudget` policy for file count, per-file bytes, and aggregate bytes. A staging budget is committed only after a file has been copied and exact-length checks succeed.

### Android share sheet

Content URIs are staged into app cache with:

- item-count/per-file/aggregate limits;
- portable filename sanitation with UTF-8 byte bounds;
- provider declared-size checks where available;
- negative provider sizes treated as unknown rather than trusted metadata;
- runtime byte bound when size is unknown;
- initial free-space preflight;
- repeated free-space-reserve checks while streaming unknown-length providers;
- exact declared/staged length validation;
- cleanup on failure;
- one atomic inbox handoff.

### Apple Share Extension

The iOS/Mac Catalyst Share Extension uses a dedicated App Group package handoff. It:

- bounds input/provider/file/text counts and sizes;
- applies a bounded provider-response timeout;
- ties provider waits and provider-file copying to extension-lifetime cancellation;
- prevents late cancelled/timed-out callbacks from beginning a new staging copy;
- does **not** let the provider-response timeout cancel a copy that already began;
- checks cancellation between copy chunks;
- applies aggregate staging budget **before** copying the file that would exceed the batch cap;
- copies temporary provider representations while their access is valid;
- uses security-scoped access where supplied;
- validates/sanitizes/deconflicts filenames with bounded collision markers;
- preflights capacity and exact lengths;
- validates a strict Core manifest;
- publishes packages atomically from `.staging-*` to `pending-*`;
- never sends content automatically.

The containing app serializes App Group imports. For each pending package it:

- validates package-directory confinement and package ID;
- rejects symlink/reparse package roots, manifest files, file roots, and file entries;
- applies strict JSON/depth/unknown-member validation;
- enforces package version, age, count, text, per-file, aggregate, and canonical filename policy;
- requires the physical `files/` directory to contain **exactly** the manifest-declared top-level files—no undeclared extra files and no nested directories;
- verifies exact declared file lengths;
- calculates the aggregate validated file bytes and preflights app-cache capacity **before** recopying the package;
- re-stages accepted files into app cache before exposing them to the normal review UI;
- deletes invalid packages rather than transferring them;
- never auto-sends imported content.

Only one pending Apple share bundle is surfaced for review at a time so a later package cannot silently overwrite/merge the current user selection. Remaining pending packages stay in the App Group inbox for a later activation/import rather than being deleted as if reviewed.

### Mac Catalyst native drop

Finder/text drops use `UIDropInteraction`, temporary security-scoped access, common staging budget, linked-source rejection, portable bounded collision handling, and the same external inbox. Provider file/text callbacks have bounded response waits; once a file callback arrives, the local staging copy is not incorrectly terminated by that response timer. No dropped item is automatically transferred.

### Windows drop

User-dropped paths/text are bounded and handed to the same review inbox. Windows direct filesystem paths remain subject to the normal source builder/manifests before send, including regular-source/link checks, deterministic folder enumeration, canonical wire paths, and manifest hashing.

## Privacy boundaries

- no account required for the local-transfer path;
- no SwiftDrop cloud relay/upload path;
- transfer file/text contents are not stored in SQLite;
- queue metadata stores generic labels/machine codes only;
- completed-batch metadata stores a hashed receive-root identity rather than the absolute root and never stores authorization;
- history privacy mode redacts peer/file identifiers;
- diagnostics redact common identifiers in privacy mode;
- clipboard is read only on explicit user action;
- received files are never automatically opened/executed;
- shared/dropped/opened files/text are never automatically transferred;
- extension warnings are not malware scanning;
- Android application backup is disabled for local app metadata;
- Windows package requests private-network client/server capability rather than general Internet client capability.

## Validation coverage

Portable tests now cover, among other areas:

- canonical pairing/Base64URL/query/whitespace behavior;
- strict decoded pairing JSON;
- typed wire requests and unknown/duplicate fields;
- canonical manifest paths and path depth;
- malformed path rejection before authorization consumption;
- one-time authorization/replay;
- complete file/batch/text/pair conversation sequencing;
- TLS pinning/mutual TLS;
- source/staged mutation and SHA-256 cleanup;
- send-boundary symlink rejection;
- deterministic link-safe folder enumeration;
- portable sender path deconfliction;
- UTF-8 filename and collision-marker bounds;
- strict receive path/traversal/symlink rejection;
- destination reservation/final-promotion races;
- stable batch IDs and repeated completed-file revalidation after mutation;
- completed-batch persistence/database migrations;
- exact external-share package file sets;
- reusable staging count/byte budgets;
- discovery fuzzing, rate limiting, UTF-8 intake limits, and session-drain races.

Apple/Android provider callback/content-resolver behavior itself remains target-platform code and therefore requires target compile/runtime validation in addition to portable Core tests.

These tests do not replace signed target builds or physical-device/network/accessibility validation.

See `docs/security/THREAT_MODEL.md` for broader threat boundaries.

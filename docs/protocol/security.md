# SwiftDrop protocol security

Updated: 2026-08-11

Protocol version: `1`

## Pairing invitation

`swiftdrop://pair?p=...` carries receiver device metadata, numeric local address, port, receiver SHA-256 certificate fingerprint, expiration, and a cryptographically random one-time nonce. The payload is encoded for transport, **not encrypted**; treat it as a short-lived capability.

`PairingCodec` treats both the URI and encoded JSON as untrusted input and enforces:

- bounded overall/encoded sizes and JSON depth;
- strict JSON with case-insensitive duplicate-property rejection;
- comments/trailing commas rejected;
- exact `swiftdrop` scheme and `pair` host;
- no unexpected authority port/path/fragment/user-info;
- exactly one `p` query field and no unknown query fields;
- exact protocol version;
- bounded non-control device ID/name;
- numeric loopback/private/link-local/unique-local address only;
- port 1–65535;
- canonical SHA-256 certificate fingerprint;
- bounded base64url-style nonce;
- future expiration within the maximum invitation lifetime.

Public Internet addresses, DNS hostnames, and ambiguous pairing JSON are rejected by protocol v1.

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
- batch requests require transfer ID, manifests, and declared total;
- text requests require bounded text and valid expiration;
- pair requests cannot carry transfer authorization.

Cross-type field smuggling is rejected.

## Authorization ordering and replay resistance

For file/batch/text:

1. protocol/request shape is validated;
2. authenticated TLS client certificate must exist;
3. the one-time pairing nonce is atomically consumed;
4. receiver consent/trust policy is evaluated;
5. transfer negotiation begins.

Malformed requests or missing client certificates do not consume authorization. A consumed nonce cannot authorize a replay. Pause/retry/resume requires a fresh pairing capability even when receiver partial/completion metadata is reused.

Pair requests use separate rate limiting, optional short code, receiver approval, and certificate-bound response. They do not consume transfer nonces.

## Single-file integrity and destination safety

Sender binds streaming to the manifest-declared length. Receiver:

- validates manifest/path/size;
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

Receive path checks are repeated around staging/promotion to reduce path-redirection races. Platform/filesystem policy remains an external boundary against a fully compromised local OS.

## Batch safety and idempotent resume

Both peers enforce per-file/count/aggregate limits. Receiver also preflights the accepted remainder against free storage.

An interrupted batch keeps a stable random `transferId`; a new explicit send gets a new one. After a batch item is verified and finalized, SwiftDrop may store metadata-only completion state containing:

- stable transfer ID;
- source relative path;
- SHA-256 hash of the receive-root identity (not its absolute path);
- effective destination relative path;
- length/SHA-256;
- completion time.

On retry SwiftDrop requires the same transfer ID, root key, source path, destination metadata, length, and SHA-256 before offering a full-length zero-byte resume. The destination is path-confined, reparse/symlink checked, length checked, and freshly SHA-256 hashed while the batch response plan is created.

After the sender returns the matching `BatchItemStart`, **SwiftDrop verifies the completed destination again immediately before the zero-byte completion acknowledgement**. If the destination is removed, changed, redirected, or no longer maps to the recorded completion metadata between planning and acknowledgement, the receiver invalidates/fails that shortcut instead of falsely acknowledging stale bytes.

Changed/missing destinations, changed source manifests, different roots, or new transfer IDs therefore cannot use completed-item reuse. A later retry can safely transfer data again. A brand-new explicit send gets a new transfer ID and retains normal collision-safe duplicate-send behavior.

Completion metadata is an optimization, never authorization. Persistence failure does not make an already verified transfer fail.

## External intake security

### Android share sheet

Content URIs are staged into app cache with:

- item-count/per-file limits;
- portable filename sanitation;
- declared-size checks where available;
- runtime byte bound when size is unknown;
- free-space preflight;
- exact-length validation;
- cleanup on failure;
- one atomic inbox handoff.

### Apple Share Extension

The iOS/Mac Catalyst Share Extension uses a dedicated App Group package handoff. It:

- bounds input/provider/file/text counts and sizes;
- applies a bounded provider-response timeout;
- ties provider waits and provider-file copying to extension-lifetime cancellation;
- prevents late cancelled/timed-out callbacks from beginning a new staging copy;
- checks cancellation between copy chunks;
- copies temporary provider representations while their access is valid;
- uses security-scoped access where supplied;
- validates/sanitizes/deconflicts filenames;
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
- re-stages accepted files into app cache before exposing them to the normal review UI;
- deletes invalid packages rather than transferring them;
- never auto-sends imported content.

Only one pending Apple share bundle is surfaced for review at a time so a later package cannot silently overwrite/merge the current user selection. Remaining pending packages stay in the App Group inbox for a later activation/import rather than being deleted as if reviewed.

### Mac Catalyst native drop

Finder/text drops use `UIDropInteraction`, temporary security-scoped access, bounded cache copying, symlink rejection, portable collision handling, aggregate limits, and the same external inbox. No dropped item is automatically transferred.

### Windows drop

User-dropped paths/text are bounded and handed to the same review inbox. Windows direct filesystem paths remain subject to the normal source builder/manifests before send.

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

Portable tests cover strict pairing, typed wire requests, unknown/duplicate fields, one-time authorization/replay, full file/batch/text/pair conversation sequencing, TLS pinning/mutual TLS, resume offsets, source/staged mutation, SHA-256 failure cleanup, path/traversal/symlink rejection, destination races, stable batch IDs, repeated completed-file revalidation after mutation, completed-batch persistence/database migrations, exact external-share package file sets, discovery fuzzing, rate limiting, UTF-8 intake limits, and session-drain races.

Apple provider callback behavior itself remains target-platform code and therefore requires Apple compile/runtime validation in addition to portable Core tests.

These tests do not replace signed target builds or physical-device/network/accessibility validation.

See `docs/security/THREAT_MODEL.md` for broader threat boundaries.

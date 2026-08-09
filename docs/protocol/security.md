# SwiftDrop protocol security

Protocol version: `1`

## Pairing invitation

The generated `swiftdrop://pair?p=...` payload contains the receiver device ID/name, numeric LAN address, port, receiver certificate SHA-256 fingerprint, expiration time, and a cryptographically random one-time nonce. The payload is encoded for transport, **not encrypted**.

Treat a pairing invitation like a temporary capability. Do not publish it, paste it into public issues, or keep it as a permanent credential.

The decoder treats an incoming pairing invitation as untrusted input and enforces:

- bounded overall link length;
- bounded encoded payload length;
- bounded JSON depth;
- exact `swiftdrop` scheme and `pair` host;
- no unexpected outer URI port, path, fragment, or user information;
- exactly one `p` query parameter and no extra parameters;
- exact current protocol version;
- bounded device ID/name metadata without control characters;
- numeric local/private/link-local/loopback IP address only;
- port range 1–65535;
- exactly one valid SHA-256 certificate fingerprint;
- bounded base64url-like nonce syntax;
- a future expiration within the allowed pairing lifetime.

DNS hostnames and public Internet addresses are intentionally rejected by protocol version 1.

The framed application protocol uses the shared strict JSON guard described below. Reusing that same duplicate-property guard inside the pairing payload is a tracked hardening item; the repository connector blocked that defensive source replacement during the current implementation session, so this document does not claim it is already active in `PairingCodec`.

## TLS and peer identity

SwiftDrop uses the .NET/platform TLS implementation and requests TLS 1.3 or TLS 1.2. It does not implement custom encryption or a custom key exchange.

The sender pins the receiver certificate by comparing the certificate presented during the TLS handshake with the canonical SHA-256 fingerprint contained in the pairing invitation. A mismatch terminates the connection.

The sender also presents its own local device certificate. The receiver requires a client certificate and obtains the sender certificate fingerprint from the authenticated TLS channel. Because an initial invitation does not already know the sender fingerprint, the receiver shows the sender identity, sender certificate fingerprint, file metadata, and risk warning for explicit approval before file bytes are accepted. For sensitive transfers, users should compare fingerprints on both devices before approving.

Trusted-device records bind a local device ID to the exact canonical certificate fingerprint. A device name alone never establishes trust.

## Local identity certificate profile

SwiftDrop local device certificates are generated with .NET/platform cryptography as P-256 ECDSA self-signed certificates. The profile includes:

- non-CA basic constraints;
- digital-signature key usage;
- TLS server-auth EKU;
- TLS client-auth EKU;
- subject key identifier;
- bounded validity period.

Private-key material is stored through MAUI SecureStorage and never placed into QR links, protocol JSON, SQLite history, diagnostics, or source configuration.

`IdentityCertificatePolicy` checks that a stored identity certificate:

- still has its private key;
- is not unreasonably not-yet-valid;
- is not expired;
- is not inside the configured renewal window;
- uses the expected ECDSA private-key type.

If a stored identity certificate is corrupt or no longer safely usable, SwiftDrop creates a **new device ID and new certificate** rather than silently attaching a new certificate to the old trusted identity. Active pairing nonces are invalidated and the user is told that other devices must pair again.

## One-time authorization

The first file/text/batch application protocol request contains the active pairing nonce. The receiver atomically consumes it. Expired, unknown, and reused nonces are rejected.

A pairing nonce is an authorization factor, not a long-term password. A stolen still-valid invitation can potentially be presented until it expires or is consumed, which is why receiver consent and sender-certificate identity remain important.

Nearby/manual pairing requests use separate bounded attempt limiting. Inbound TLS connections also have a per-source-address rate limit before expensive application work.

## Strict framed JSON

Application metadata is length-prefixed and validated before typed deserialization.

- Frame length must be positive and at or below the configured header limit before payload allocation.
- JSON nesting depth is bounded.
- Comments and trailing commas are rejected.
- Invalid UTF-8/JSON is rejected.
- Duplicate object property names are rejected **case-insensitively**, including nested objects/arrays, so `type` plus `Type` cannot create ambiguous interpretation.
- Truncated headers/payloads fail rather than being treated as partial metadata.
- Read/write operations use network idle timeouts and caller cancellation.

Boundary tests cover invalid lengths, malformed UTF-8, duplicate fields, nested duplicates, and every truncated prefix of a valid frame.

## Incoming-transfer consent

Before bytes are written, the receiver displays or otherwise evaluates:

- sender device name;
- sender certificate fingerprint;
- filename/path;
- declared size;
- extension-based caution/high-risk classification when applicable;
- a reminder that SwiftDrop does not automatically open received files.

The extension classifier is only a warning aid. It is not malware scanning and cannot establish that a file is safe to execute/open.

Trusted-device auto-accept is optional, disabled by default, certificate-bound, and limited to normal-risk content.

## Integrity and resumable staging

Each file manifest contains a SHA-256 digest and expected length. The receiver writes only the expected byte count to a `.swiftdrop.part` file. A compatible partial file may provide a bounded resume offset.

Resume handling verifies that the negotiated offset does not exceed the staged partial length. If a staged file has an unexpected tail beyond the negotiated offset, that tail is truncated before additional bytes are accepted.

The sender binds its stream to the manifest-declared source length. If the source size changes after manifest construction or becomes shorter during transfer, the transfer fails instead of silently changing protocol framing.

After all expected bytes arrive, SwiftDrop hashes the complete staged file and performs a constant-time digest comparison. Only a verified file is moved into its final location. A failed integrity check deletes the invalid staged file rather than presenting it as a completed transfer.

## Destination collision and path safety

All receive paths are resolved beneath the configured receive root.

- Rooted paths are rejected.
- `.` and `..` traversal segments are rejected.
- Filename segments are Unicode Form-C normalized/sanitized.
- Portable-invalid/control characters are removed.
- Windows reserved device names are rewritten to safe names.
- Batch paths are compared after sanitation, separator normalization, Unicode normalization, and case folding so two metadata paths cannot collapse onto one portable destination.
- Path/filename lengths are bounded by SwiftDrop policy before filesystem operations.
- Receive-root confinement and active destination reservations use one centralized platform path-comparison policy (case-insensitive for Windows and common Apple targets, ordinal elsewhere).
- Completed filesystem collisions receive a new bounded suffix such as `name (1).ext`.
- Active concurrent incoming sessions additionally use an in-memory atomic destination reservation set so two transfers cannot select the same not-yet-created final destination.

## Batch transfer limits

Batch safety is enforced on both sender and receiver.

Before hashing a selected batch, the sender preflights:

- source existence;
- maximum file count;
- per-file size;
- aggregate batch bytes;
- safe/deconflicted relative paths;
- cancellation.

The receiver independently revalidates the manifest and declared aggregate total. After receiver selection, SwiftDrop sums the remaining accepted bytes using checked arithmetic and checks destination capacity **before** sending an accepted transfer plan.

A conforming sender cannot bypass receiver-side limits by claiming that it already validated the batch.

## Input validation and resource limits

- Framed JSON metadata is length-bounded before allocation.
- JSON depth is bounded and duplicate properties are rejected.
- Ports and protocol versions are validated.
- Individual file size is bounded.
- Batch file count and aggregate bytes are bounded.
- Text snippet UTF-8 bytes and lifetime are bounded.
- Rooted and traversal paths are rejected.
- Destination paths must remain beneath the configured receive root.
- Transfer loops use bounded chunks, cancellation, exact remaining-length accounting, and network idle timeouts.
- File bytes are never interpreted as commands by the transfer protocol.
- Received files are never automatically launched.
- Shared/dropped content is never automatically transferred.

## Restart-safe queue metadata

Queue persistence is metadata-only and is not transfer authorization. Persisted rows use a generic `Transfer` label plus state/timestamps and a bounded machine-oriented error code. Filenames, text, peer addresses, pairing invitations/nonces, credentials, and free-form exception messages are not stored in queue metadata.

If the app restarts while an item is `Queued` or `Running`, that row becomes `Interrupted`. SwiftDrop does not silently replay the transfer; fresh pairing/authorization is still required.

## Privacy-safe notifications

Android can optionally show generic transfer completion/failure notifications when the user enables the preference and grants notification permission. Notification text does not include filenames or transferred text. Required Android foreground data-sync status is a separate platform lifecycle requirement and must not be confused with optional completion notifications.

On targets where optional completion/failure system notifications are not implemented, the preference is disabled rather than pretending notifications exist.

## Validation coverage

Portable tests include:

- strict pairing-link validation;
- local/public address policy;
- certificate profile and lifecycle policy;
- canonical fingerprint handling;
- rate limiting;
- frame limits, malformed UTF-8/JSON, duplicate properties, and truncation;
- path/filename sanitation and normalized portable collision detection;
- destination collision reservations;
- batch manifest limits;
- source-length mutation;
- staged resume validation;
- checksum mismatch cleanup;
- SQLite schema migrations including queue metadata;
- real mutual-TLS loopback connections;
- exact certificate pinning success/failure;
- full file transfer and staged resume over loopback TLS.

These tests reduce protocol regressions but do not replace target-platform/device/network validation.

## Threat boundaries

SwiftDrop is designed to protect local-network transport from passive interception, detect receiver substitution when the pairing invitation is authentic, reject pairing replay after nonce consumption, constrain untrusted paths, limit selected resource usage, and detect transfer corruption.

SwiftDrop cannot protect data after an authorized endpoint receives it, remediate a compromised operating system, guarantee that an extension accurately describes file content, bypass enterprise/guest-network isolation, keep arbitrary sockets alive against mobile OS suspension policy, or make an untrusted file safe to open. See `docs/security/THREAT_MODEL.md` for the broader threat model.

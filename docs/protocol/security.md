# SwiftDrop protocol security

Protocol version: `1`

## Pairing invitation

The `swiftdrop://pair?p=...` payload contains the receiver device ID/name, LAN address, port, receiver certificate SHA-256 fingerprint, expiration time, and a cryptographically random one-time nonce. The payload is encoded for transport, **not encrypted**.

Treat a pairing invitation like a temporary capability. Do not publish it, paste it into public issues, or keep it as a permanent credential.

## TLS and peer identity

SwiftDrop uses the .NET/platform TLS implementation and requests TLS 1.3 or TLS 1.2. It does not implement custom encryption or a custom key exchange.

The sender pins the receiver certificate by comparing the certificate presented during the TLS handshake with the SHA-256 fingerprint contained in the pairing invitation. A mismatch terminates the connection.

The sender also presents its own local device certificate. The receiver requires a client certificate and obtains the sender certificate fingerprint from the authenticated TLS channel. Because an initial invitation does not already know the sender fingerprint, the receiver shows the sender identity, sender certificate fingerprint, file metadata, and risk warning for explicit approval before file bytes are accepted. For sensitive transfers, users should compare the sender fingerprint on the sending device before approving.

This provides authenticated receiver pinning plus an authenticated sender certificate that is explicitly confirmed by the receiver. Future trusted-device flows may persist fingerprints after a confirmed pairing, but must not silently trust a new certificate solely because a device name matches.

## One-time authorization

The first application protocol request contains the active pairing nonce. The receiver atomically consumes it. Expired, unknown, and reused nonces are rejected.

A pairing nonce is an authorization factor, not a long-term secret. A stolen still-valid invitation can be abused until it expires or is consumed, which is why the receiver still requires explicit incoming-transfer consent and exposes sender certificate identity.

## Incoming-transfer consent

Before bytes are written, the receiver displays:

- sender device name;
- sender certificate fingerprint;
- filename/path;
- declared size;
- a warning for executable, installer, script, macro-enabled, image/archive-container, or other potentially active file types when applicable;
- a reminder that SwiftDrop does not automatically open received files.

Rejecting an incoming transfer ends that authorization attempt.

## Integrity and resumable staging

Each file manifest contains a SHA-256 digest and expected length. The receiver writes only the expected byte count to a `.swiftdrop.part` file. A compatible partial file may provide a bounded resume offset. After all expected bytes arrive, SwiftDrop hashes the complete staged file and performs a fixed-time digest comparison. Only a verified file is moved into its final location.

A failed integrity check deletes the incomplete staged file rather than presenting it as a completed transfer.

## Input validation and resource limits

- Framed JSON metadata is length-bounded before allocation.
- Ports and protocol versions are validated.
- Individual file size is bounded.
- Rooted and traversal paths are rejected.
- Destination paths must remain beneath the configured receive root.
- Transfer loops use bounded chunks, cancellation, and exact remaining-length accounting.
- File bytes are never interpreted as commands by the transfer protocol.
- Received files are never automatically launched.

## Certificate handling

The local device certificate/private key is generated with platform/.NET cryptographic APIs and stored through MAUI secure storage. Private key material is never placed into QR pairing invitations or protocol metadata frames.

Self-signed device certificates are intentionally identified through out-of-band fingerprint pinning and explicit peer confirmation rather than public PKI hostname validation.

## Threat boundaries

SwiftDrop is designed to protect local-network transport from passive interception, detect receiver substitution when the pairing invitation is authentic, reject pairing replay after nonce consumption, constrain untrusted paths, and detect transfer corruption.

SwiftDrop cannot protect data after an authorized endpoint receives it, remediate a compromised operating system, guarantee that an extension accurately describes file content, bypass enterprise/guest-network isolation, or make an untrusted file safe to open. See `docs/security/THREAT_MODEL.md` for the broader threat model.

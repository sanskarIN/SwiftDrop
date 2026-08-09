# SwiftDrop protocol security

Protocol version: `1`

## Pairing link

The `swiftdrop://pair?p=...` payload contains device metadata needed to establish a local TLS connection. It is encoded for transport, **not encrypted**. Its security properties come from short expiration, a cryptographically random one-time nonce, and TLS certificate fingerprint pinning.

Treat a pairing link like a temporary invitation. Do not publish it.

## TLS

The sender validates the receiver using the SHA-256 fingerprint received out of band in the pairing payload. The code requests TLS 1.3 or TLS 1.2 and delegates cipher choice/key exchange to the platform cryptographic stack.

The first transfer request must contain the active pairing nonce. The receiver consumes it exactly once; expired, unknown, and reused nonces are rejected.

## Integrity

Each file manifest contains a SHA-256 digest. The receiver writes to a `.swiftdrop.part` file, hashes the complete result, compares the digest, and only then moves it into place.

## Input validation

- Framed JSON headers are limited to 64 KiB.
- Ports and protocol versions are validated.
- Individual file size is bounded.
- Rooted and traversal paths are rejected.
- File bytes are never interpreted as commands.

## Threat boundaries

SwiftDrop protects transport from casual network interception and pairing-link replay within the limits above. It does not protect data after a trusted endpoint has received it, does not remediate a compromised operating system, and does not bypass LAN/firewall policy.

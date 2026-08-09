# Protocol Compatibility Matrix

SwiftDrop protocol versioning is intentionally strict. A peer must not silently guess how to interpret an unknown version.

| Sender | Receiver | Expected result |
|---|---|---|
| v1 | v1 | Supported |
| v1 | unknown newer | Reject before payload transfer |
| unknown newer | v1 | Reject before payload transfer |
| malformed/missing | v1 | Reject |

## Version 1 guarantees

Version 1 defines:

- four-byte big-endian JSON metadata frame lengths;
- bounded JSON metadata frames;
- TLS 1.2/1.3 transport requirements;
- receiver fingerprint pinning at the sender after pairing;
- sender client certificate presentation;
- one-time pairing authorization;
- single-file manifests and raw payload ordering;
- multi-file batch negotiation and per-item payload ordering;
- text snippet metadata;
- SHA-256 file integrity verification;
- `.swiftdrop.part` receive staging/resume offsets;
- local/private numeric peer-address policy.

## Change rules

Increment the protocol version when a change breaks any of the following:

- frame encoding or ordering;
- authentication/authorization semantics;
- required fields or their meaning;
- hash/integrity semantics;
- batch item ordering/acknowledgement;
- resume-offset interpretation;
- resource-limit semantics that an older peer could misinterpret unsafely.

A new optional UI feature does not by itself require a new wire version when it maps entirely to existing protocol messages.

## Compatibility implementation rule

Unknown versions are rejected. SwiftDrop does not downgrade to an older security mode automatically. A future compatibility layer must be explicit, tested in both directions, and must never bypass current trust/pinning/consent requirements simply to connect to an older peer.

## Required tests for version 2+

Before introducing another protocol version:

1. Keep frozen version-1 fixtures.
2. Add v1↔v1 regression tests.
3. Add newer↔older rejection tests.
4. Add malformed-version tests.
5. Add security review for downgrade/confusion attacks.
6. Update `docs/protocol/wire-format.md`, `docs/protocol/security.md`, the threat model, and release notes.

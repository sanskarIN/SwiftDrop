# SwiftDrop Wire Protocol

Protocol version: `1`

SwiftDrop uses a TLS stream. Every metadata message is a length-prefixed UTF-8 JSON frame. The frame prefix is a four-byte big-endian signed integer. Metadata frames are rejected when their length is non-positive or exceeds the configured 64 KiB header limit. File payload bytes are streamed directly after negotiation and are never embedded in JSON.

## Common sender identity

Authenticated requests include:

- `protocolVersion`
- `senderDeviceId`
- `senderDeviceName`

The sender display name is presentation metadata, not cryptographic identity. The receiving side derives the sender certificate fingerprint from the authenticated TLS channel.

## Pair request

Type: `pair-request`

Fields:

- `pairingCode` — optional eight-digit one-time code for nearby/manual fallback.
- sender identity fields.

The receiver can require the one-time code before presenting a pairing approval prompt. On approval, the receiver returns an expiring `swiftdrop://pair?...` invitation. Manual IP bootstrap binds the returned invitation fingerprint to the exact server certificate observed during the bootstrap TLS session and then requires visual confirmation before transfer.

## Single file

Type: `file`

Fields:

- `pairingNonce`
- `entry.relativePath`
- `entry.length`
- `entry.sha256`
- `entry.lastWriteUtc`

The receiver validates and sanitizes metadata, atomically consumes the one-time pairing nonce, asks for consent when required, resolves a collision-safe destination, checks free space, and replies with:

- `accepted`
- `resumeOffset`
- `message`

The sender streams exactly `length - resumeOffset` bytes. The receiver stages them in a `.swiftdrop.part` file, verifies the complete SHA-256 digest, moves the staged file into place, and returns a final success response.

## Batch / folder

Type: `batch`

Fields:

- `pairingNonce`
- `transferId`
- `files[]`
- `totalBytes`

Each `files[]` item uses the single-file manifest schema. Folder transfer is represented as a set of files with normalized relative paths; empty directories are not serialized in protocol version 1.

The receiver validates the entire manifest, obtains an accept-all / selective / reject decision, and returns a `BatchTransferResponse` containing a plan per source relative path:

- `relativePath`
- `resumeOffset`
- `accepted`
- optional `message`

For each accepted item the sender writes a `BatchItemStart` frame and then streams that file's remaining bytes. The receiver acknowledges each item and sends a final batch completion response after all accepted items finish.

A paused or interrupted batch requires a fresh one-time pairing invitation before resume. Resume offsets can still be recovered from staged partial files.

## Text snippet

Type: `text`

Fields:

- `pairingNonce`
- `text`
- `expiresUnixSeconds`

Text size and expiry are bounded before user presentation. The receiver can reject, accept, or accept-and-copy. SwiftDrop does not continuously monitor the clipboard; clipboard access is only triggered by explicit user action.

## Authentication and authorization sequence

1. TLS is established.
2. The sender verifies the receiver certificate fingerprint when a pinned invitation/discovery record is available.
3. The receiver requires a sender client certificate.
4. Connection and pairing attempt rate limits are applied.
5. Request metadata is bounded and validated.
6. For transfer requests, the receiver atomically consumes the short-lived pairing nonce.
7. User consent/trust policy is applied.
8. Payload bytes are transferred.
9. File integrity is verified before finalization.

## Compatibility

Unknown protocol versions and unknown request types are rejected. Protocol version changes that alter field semantics, framing, authentication, or payload ordering must be documented here and accompanied by compatibility tests before release.

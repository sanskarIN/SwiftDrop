# SwiftDrop Wire Protocol

Protocol version: `1`

SwiftDrop uses a TLS stream. Every application metadata message is a length-prefixed UTF-8 JSON frame. The frame prefix is a four-byte big-endian signed integer. Metadata frames are rejected when their length is non-positive or exceeds the configured 64 KiB header limit. File payload bytes are streamed directly after negotiation and are never embedded in JSON.

Protocol v1 is intentionally a **closed schema**. Framed JSON rejects malformed UTF-8/JSON, excessive depth, comments, trailing commas, case-insensitive duplicate object members, and unknown/unmapped members before typed application handling.

## Canonical relative paths

File manifest `relativePath` values have one protocol representation on every operating system:

- `/` is the only wire separator;
- `\\` is rejected in an incoming wire manifest rather than accepted as an alternate representation;
- rooted/drive/UNC/device paths are rejected;
- empty segments, repeated separators, trailing separators, `.` and `..` are rejected;
- nesting is limited to 64 segments;
- total manifest path text is limited to 1,024 characters;
- every segment is already in SwiftDrop's canonical sanitized form before the request is authorized;
- segment sanitation uses Unicode NFC, removes control/portable-invalid characters, neutralizes Windows reserved device names, strips unsafe trailing dot/space behavior, and bounds each segment to 180 UTF-16 code units **and** 180 UTF-8 bytes.

The sender constructs this canonical form before hashing/negotiation. The receiver requires exact canonical form; it does not silently rewrite an untrusted wire path after consuming transfer authorization. Local filesystem conversion happens only after canonical protocol validation.

Collision-generated names use the same segment limits. If a conventional `name (n).ext` suffix would be truncated away, SwiftDrop uses a bounded prefix form such as `(n) name...` so separate collision candidates cannot collapse back to one filename.

## Pairing invitation representation

A transfer invitation uses:

`swiftdrop://pair?p=<payload>`

The outer capability representation is strict:

- no surrounding whitespace;
- exact `swiftdrop` scheme and `pair` host;
- no user-info, fragment, unexpected path, or explicit authority port;
- exactly one raw `p=` query parameter;
- no empty/unknown/duplicate query segments;
- the `p` value is unpadded canonical Base64URL using only ASCII letters, digits, `-`, and `_`;
- standard Base64 `+`, `/`, `=`, percent-encoded aliases, and non-canonical re-encodings are rejected.

Decoded pairing JSON is also closed-schema: strict JSON, bounded depth, duplicate/unknown member rejection, exact protocol version, bounded identity fields, local numeric address, valid port, canonical SHA-256 fingerprint, base64url-style nonce, and bounded future expiration.

## Common sender identity

Authenticated application requests include:

- `protocolVersion`
- `senderDeviceId`
- `senderDeviceName`

The sender display name is presentation metadata, not cryptographic identity. The receiving side derives the sender certificate fingerprint from the authenticated TLS channel.

## Pair request

Type: `pair-request`

Fields:

- `pairingCode` — optional eight-digit one-time code for nearby/manual fallback;
- sender identity fields.

The receiver can require the one-time code before presenting a pairing approval prompt. On approval, the receiver returns an expiring canonical `swiftdrop://pair?...` invitation. Manual IP bootstrap binds the returned invitation fingerprint to the exact server certificate observed during the bootstrap TLS session and then requires visual confirmation before transfer.

## Single file

Type: `file`

Fields:

- `pairingNonce`
- `entry.relativePath`
- `entry.length`
- `entry.sha256`
- `entry.lastWriteUtc`

Before one-time authorization is consumed, the receiver validates the request shape and the complete canonical manifest path/size/hash/timestamp metadata. After authenticated sender-certificate availability and nonce authorization, the receiver applies consent/trust policy, resolves a collision-safe destination, checks free space, and replies with:

- `accepted`
- `resumeOffset`
- `message`

The sender revalidates that the source is still a regular non-link/non-reparse file at the stream-open boundary and streams exactly `length - resumeOffset` bytes. The receiver stages them in a `.swiftdrop.part` file, verifies the complete SHA-256 digest, performs non-overwrite final promotion, and returns a final success response. Optional last-write timestamp application occurs only after verified promotion and is best-effort metadata.

## Batch / folder

Type: `batch`

Fields:

- `pairingNonce`
- `transferId`
- `files[]`
- `totalBytes`

`transferId` is a bounded canonical ASCII token containing only letters, digits, `-`, and `_`. A new explicit user send gets a new ID. Pause/failure retry preserves the same ID while a cancel/success clears the resume lineage.

Each `files[]` item uses the single-file manifest schema. Folder transfer is represented as a deterministic set of files with canonical `/` relative paths; empty directories are not serialized in protocol version 1. Outgoing recursive enumeration rejects symbolic links/reparse points, bounds file/directory traversal, sorts the resulting relative paths deterministically, and deconflicts case/Unicode/sanitization-equivalent portable names before hashing.

The receiver validates the entire manifest, obtains an accept-all / selective / reject decision, and returns a `BatchTransferResponse` containing a plan per **exact source relative path**:

- `relativePath`
- `resumeOffset`
- `accepted`
- optional `message`

For each accepted item the sender writes a `BatchItemStart` frame whose path must exactly match the negotiated canonical path, then streams that file's remaining bytes. The receiver acknowledges each item and sends a final batch completion response after all accepted items finish.

A paused/interrupted batch requires a fresh one-time pairing invitation before retry. The stable batch ID may reuse receiver-side staged/completed metadata, but that metadata is never authorization.

For an already-finalized item, protocol v1 uses `resumeOffset == entry.length` and zero additional file bytes only after the receiver verifies the same transfer/root/source/length/hash and re-hashes the final destination. SwiftDrop performs another completed-file verification after receiving that item's `BatchItemStart` and immediately before issuing the zero-byte completion acknowledgement; a destination modified/deleted in that interval fails closed and invalidates completion reuse.

## Text snippet

Type: `text`

Fields:

- `pairingNonce`
- `text`
- `expiresUnixSeconds`

Text size and expiry are bounded before user presentation. The receiver can reject, accept, or accept-and-copy. SwiftDrop does not continuously monitor the clipboard; clipboard access is only triggered by explicit user action.

## Authentication and authorization sequence

For file/batch/text requests:

1. TLS is established.
2. The sender verifies the receiver certificate fingerprint from the validated invitation.
3. The receiver requires a sender client certificate and derives its fingerprint from TLS.
4. Framed JSON and typed request shape are strictly validated.
5. Manifest/path/size/hash/timestamp metadata is validated in canonical form.
6. The receiver atomically consumes the one-time pairing nonce.
7. User consent/trust policy is applied.
8. Negotiated payload bytes are transferred.
9. File integrity is verified before finalization/acknowledgement.

Malformed request/path metadata therefore does **not** consume a valid one-time transfer capability.

## Compatibility

Unknown protocol versions, request types, JSON members, and non-canonical path/capability representations are rejected. Because protocol v1 is closed-schema, adding an application JSON field is a compatibility decision rather than an assumption that older peers will ignore it. Changes to field semantics, framing, authentication, canonical path representation, or payload ordering must be versioned/documented and accompanied by compatibility tests before release.

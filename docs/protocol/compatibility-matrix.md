# Protocol compatibility matrix

Current protocol version: **1**

This matrix describes compatibility at the SwiftDrop application wire layer. It is separate from app/store version compatibility.

## Version behavior

| Sender | Receiver | Expected result |
|---|---|---|
| v1 | v1 | Supported when request/message shape is exactly valid. |
| v1 | unknown future version | Reject before transfer. |
| unknown future version | v1 | Reject before transfer. |
| malformed/missing version | v1 | Reject. |

SwiftDrop does not silently downgrade or guess protocol versions.

## Closed-schema JSON rule

Protocol v1 typed JSON is a **closed schema**:

- unknown object members are rejected;
- duplicate members are rejected case-insensitively;
- comments/trailing commas are rejected;
- malformed UTF-8/JSON is rejected;
- nesting/frame sizes are bounded;
- type-specific request fields are enforced.

Therefore an additive JSON field is **not assumed backward compatible** in protocol v1. If a future implementation needs new wire members, treat that as a compatibility decision and normally introduce/negotiate a new protocol version rather than relying on old peers to ignore the field.

## Shared wire records in v1

Production sender, pairing client, receiver, and tests use the same Core records:

- `ProtocolRequest`;
- `TransferAcknowledgement`;
- `BatchItemStart`;
- `PairingResponse`;
- `BatchTransferResponse` / `BatchItemPlan`.

Migrating from app-private/anonymous records to these Core records did **not** add new protocol-v1 wire fields; it made the existing schema explicit/testable.

## Request-type compatibility

| Request type | v1 required behavior |
|---|---|
| `file` | Transfer nonce + sender identity + one manifest entry; transfer-only fields only. |
| `batch` | Transfer nonce + sender identity + stable transfer ID + manifest list + declared total. |
| `text` | Transfer nonce + sender identity + bounded text + expiration. |
| `pair-request` | Sender identity + optional exact 8-digit code; no transfer nonce. |
| other | Reject. |

Cross-type field smuggling is rejected. Example: file request carrying text, pair request carrying transfer nonce, or text request carrying batch metadata is invalid.

## File resume compatibility

Protocol v1 resume semantics remain:

- receiver returns a resume offset between 0 and file length;
- sender writes exactly `Length - ResumeOffset` raw bytes;
- receiver returns full-length completion acknowledgement after verified finalization.

No new wire field is required for current resume hardening.

## Batch idempotent-resume compatibility

The completed-file retry fix remains compatible with protocol v1 wire shape.

A paused/failed batch preserves its existing v1 `transferId`. A new explicit batch receives a new `transferId`.

For an already-finalized item from the same interrupted batch, receiver may use the **existing** `BatchItemPlan.ResumeOffset` field with:

`ResumeOffset == Length`

only after local metadata + destination revalidation and fresh SHA-256 verification. Sender then emits the existing `BatchItemStart` and sends zero payload bytes for that item; receiver sends the existing full-length item acknowledgement.

No new JSON property/message type was introduced for this behavior.

Consequences:

- current v1 peers that already accept `ResumeOffset == Length` remain wire-compatible;
- new source gains idempotent completed-file reuse without changing v1 schema;
- changed/missing destination or different/new transfer ID falls back to normal v1 transfer semantics.

## Pairing compatibility

Current v1 pairing decoder requires:

- exact `swiftdrop://pair` outer form;
- exactly one `p` parameter;
- strict encoded JSON;
- exact current protocol version;
- numeric local/private/link-local/unique-local address;
- canonical SHA-256 certificate fingerprint;
- bounded nonce/expiry/lifetime.

Public Internet/DNS peer addresses are intentionally unsupported by v1.

## Security-sensitive changes that require compatibility review

Do not treat these as implementation details:

- adding/removing/renaming JSON members;
- changing required/optional request fields;
- changing frame length encoding;
- changing raw payload ordering;
- changing SHA-256 manifest interpretation;
- changing pairing address policy;
- changing certificate/fingerprint semantics;
- changing batch item ordering/plan semantics;
- changing authorization replay rules;
- allowing peers to ignore unknown members.

Before such a change:

1. define the new protocol version/negotiation behavior;
2. update `ProtocolConstants.CurrentVersion` where needed;
3. add cross-version tests;
4. update this matrix and wire/security docs;
5. test old sender/new receiver and new sender/old receiver behavior explicitly;
6. avoid silently broadening security policy during fallback.

## Current compatibility test coverage

Portable tests cover:

- exact protocol version acceptance/rejection;
- typed request factory/shape validation;
- unknown/duplicate JSON members;
- one-time authorization/replay;
- full framed file/batch/text/pair conversations;
- batch plan/item ordering;
- file/batch resume offsets including full-length completion semantics;
- strict pairing decoding;
- TLS certificate pinning and mutual TLS loopback flows.

Target-platform build/runtime validation remains required independently of wire compatibility.

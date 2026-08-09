# SwiftDrop Security Test Plan

This plan complements automated unit tests and the manual cross-platform transfer matrix. It must be executed against release candidates before store/public distribution.

## Pairing and authentication

- Verify expired QR/deep-link invitations are rejected.
- Verify a consumed nonce cannot authorize a second transfer.
- Verify an incorrect 8-digit code is rejected without consuming a correct future code.
- Verify a used code cannot be replayed.
- Verify manual-IP pairing rejects public IP addresses and DNS hostnames.
- Verify manual-IP bootstrap binds the returned invitation fingerprint to the certificate observed on that TLS connection.
- Verify replacing the receiver certificate after pairing causes sender pinning failure.
- Verify receiver consent displays the sender certificate fingerprint derived from TLS, not sender JSON.
- Verify trusted-device auto-accept works only when both device ID and certificate fingerprint match.
- Verify identity reset invalidates local trust expectations.

## Protocol abuse and parsing

- Send zero, negative, oversized, and truncated JSON frame lengths.
- Send malformed and deeply nested JSON.
- Send unknown protocol versions and request types.
- Send missing sender identity, overlong names, control characters, invalid ports, malformed hashes, invalid timestamps, duplicate batch paths, and batch total mismatches.
- Exceed single-file, batch-file-count, batch-total-byte, and text-snippet limits.
- Hold metadata or payload reads/writes idle beyond the configured timeout.
- Open repeated connections from one source address and repeated requests from one certificate fingerprint to verify rate limits.
- Attempt to expand rate-limiter cardinality with many identities and verify bounded behavior.

## Filesystem safety

- Attempt `../`, rooted paths, alternate separator traversal, reserved/control characters, and platform-invalid names.
- Verify filename sanitization remains beneath the configured receive root.
- Verify existing completed files are not silently overwritten.
- Verify low-space behavior fails before receiving bytes that cannot fit with safety reserve.
- Interrupt a transfer and verify only `.swiftdrop.part` staging remains.
- Resume with a fresh invitation and verify the receiver returns a valid partial offset.
- Corrupt a payload and verify SHA-256 mismatch deletes invalid staged content and never finalizes it.
- Verify completed files preserve only supported safe metadata; no executable content is automatically launched.

## Batch and selective receive

- Reject the whole batch and verify no payload bytes are accepted.
- Accept only a subset and verify unselected items are never sent.
- Try duplicate names from separate source folders and verify sender deconfliction and receiver collision handling.
- Interrupt between items and during an item; verify completed items stay complete and the current item remains safely staged.
- Validate cumulative storage requirements for very large batches.

## Text and clipboard

- Verify text is bounded by UTF-8 byte size, not only character count.
- Verify expired text requests are rejected.
- Verify clipboard is read only after explicit user action.
- Verify rejected text is not copied or persisted as content in history/diagnostics.

## Share and activation input

- Send malformed `swiftdrop://` protocol activations.
- Send share-sheet URIs that become unavailable mid-copy.
- Share more than the configured file-count limit.
- Verify staged share-cache filenames are sanitized and stale cache data is pruned.
- Verify private keys, pairing nonces, full pairing invitations, and payload contents never enter diagnostics.

## Network and TLS

- Capture LAN traffic and verify transfer payloads are not visible in plaintext.
- Verify TLS 1.2/1.3 negotiation only; reject unsupported legacy TLS.
- Verify public-address pairing rejection remains enforced after refactors.
- Test guest Wi-Fi/client isolation and confirm SwiftDrop reports limitations instead of attempting policy bypass.

## Dependency and static analysis

- Run core tests under CI.
- Run CodeQL.
- Review Dependabot updates and package advisories.
- Generate the exact restored dependency graph for release and review third-party licenses.
- Treat any cryptographic or authentication change as security-sensitive and require focused review.

## Release evidence

Record device/OS versions, SwiftDrop commit SHA, test date, tester, outcome, and issue links. A release candidate is not considered security-validated merely because unit tests compile or pass.

# SwiftDrop Security Test Plan

Updated: 2026-08-11

This plan complements automated unit tests and the manual cross-platform transfer matrix. Execute it against the exact release candidate before public/store distribution. Use synthetic test files and test identities only.

## Pairing and authentication

- Verify expired QR/deep-link invitations are rejected.
- Verify a consumed nonce cannot authorize a second transfer.
- Verify invalid request shape is rejected before a one-time transfer nonce is consumed.
- Verify a pair request never consumes transfer authorization.
- Verify an incorrect 8-digit code is rejected without consuming a correct future code.
- Verify a used code cannot be replayed.
- Verify manual/nearby pairing rejects public IP addresses and DNS hostnames.
- Verify manual-IP bootstrap binds the returned invitation fingerprint to the certificate observed on that TLS connection.
- Verify replacing the receiver certificate after pairing causes sender pinning failure.
- Verify receiver consent uses the sender certificate fingerprint derived from TLS, not sender JSON.
- Verify trusted-device auto-accept works only when both device ID and canonical certificate fingerprint match and risk is normal.
- Verify high-risk content still requires consent even from a trusted peer.
- Verify identity reset invalidates local trust/active authorization expectations.
- Verify automatic identity regeneration creates a new device ID/certificate and requires re-pairing.

## Protocol abuse and parsing

- Send zero, negative, oversized, and truncated JSON frame lengths.
- Send malformed UTF-8, malformed JSON, comments, trailing commas, and excessive nesting.
- Send duplicate fields including case variants at top level and nested levels.
- Add unknown/unmapped fields to otherwise valid top-level and nested typed protocol messages and verify rejection.
- Send unknown/case-changed request types and unsupported protocol versions.
- Cross-smuggle type-specific fields (for example text fields in a file request or file metadata in a pair request) and verify type-shape rejection.
- Send missing/overlong/control-character sender identity.
- Send malformed pairing nonces/codes, transfer IDs, hashes, timestamps, file metadata, batch totals, and duplicate batch paths.
- Send receiver batch plans containing unknown/duplicate/missing paths, invalid offsets, contradictory acceptance, or unexpected ordering.
- Send unexpected/reordered `BatchItemStart` frames.
- Exceed single-file, batch-file-count, batch-total-byte, text-snippet, and metadata limits.
- Close the connection at each request/response/payload transition and verify bounded failure.
- Hold metadata/payload reads or writes idle beyond the configured timeout.
- Open repeated connections from one source address and repeated pairing attempts from one certificate fingerprint to verify rate limits.
- Attempt to expand limiter/authorization cardinality and verify bounded behavior.

## Filesystem safety

- Attempt `../`, rooted paths, Windows drive/UNC-style roots on non-Windows targets, alternate separators, reserved/control characters, and platform-invalid names.
- Verify filename sanitation and final resolution remain beneath the configured receive root.
- Place symlink/reparse components inside a disposable receive root and verify staging/finalization/completed-resume verification reject them.
- Verify existing completed files are never silently overwritten.
- Run concurrent same-destination incoming attempts and confirm reservation/collision behavior produces distinct safe results or bounded rejection.
- Verify final promotion fails rather than overwriting a destination that appears after planning.
- Verify low-space behavior fails before receiving bytes that cannot fit with safety reserve.
- Interrupt a transfer and verify only bounded `.swiftdrop.part` staging remains.
- Resume with a fresh invitation and verify the receiver returns only a valid staged offset.
- Corrupt or lengthen a staged partial before resume and verify safe truncation/rejection/integrity behavior.
- Mutate sender source size after manifest creation and verify framing fails rather than sending a changed length.
- Corrupt payload/staging and verify SHA-256 mismatch never finalizes it.
- Verify completed files preserve only supported safe metadata and are never automatically launched.

## Batch, selective receive, and idempotent retry

- Reject the whole batch and verify no payload bytes are accepted.
- Accept only a subset and verify unselected items are never sent.
- Try duplicate names from separate source folders and verify sender deconfliction plus receiver collision handling.
- Verify portable case/Unicode/sanitized-path collisions cannot collapse multiple source items onto one destination.
- Interrupt during an item and between items.
- Resume the same interrupted batch with a fresh pairing invitation and verify the stable batch transfer ID is retained.
- Confirm already-finalized items are revalidated and receive a full-length resume offset instead of a collision-renamed duplicate copy.
- Modify an already-finalized destination after the first interrupted attempt and verify the completed-item shortcut is rejected/invalidated.
- Delete an already-finalized destination and verify retry does not falsely acknowledge completion.
- Change the receive root and verify completed-item metadata from the old root cannot authorize/skip data in the new root.
- Start a completely new explicit send of identical files and verify it receives a new transfer ID and normal collision-safe duplicate-send behavior.
- Corrupt completed-batch SQLite rows and verify they do not become authorization or false completion.
- Verify schema-v3 completed-item metadata never contains the absolute receive root, pairing nonce/code, certificate private key, or transferred content.
- Validate cumulative storage requirements for large batches and checked-arithmetic overflow boundaries.

## Text and clipboard

- Verify text is bounded by UTF-8 byte size, not only character count.
- Verify truncation/intake limits never split surrogate pairs or multi-byte Unicode scalars.
- Verify expired text requests are rejected.
- Verify clipboard is read only after explicit user action.
- Verify rejected text is not copied and text content is not persisted in history/diagnostics/queue metadata.

## Android share intake

- Send one/multiple content URIs through `ACTION_SEND` / `ACTION_SEND_MULTIPLE`.
- Provide more providers than the configured count limit and verify bounded handling.
- Provide declared lengths above the per-file/aggregate limit and verify rejection before expensive copy where possible.
- Provide incorrect/unknown provider lengths and verify runtime byte caps still stop oversized content.
- Make a content URI unavailable during copy and verify partial cache output is removed.
- Verify portable filename sanitation and aggregate storage preflight.
- Verify text + multiple files arrive in one coherent inbox handoff and are never auto-sent.
- Verify stale nested share-cache staging is pruned without touching unrelated app files.

## Apple Share Extension and App Group handoff

- Verify containing app and Share Extension are signed/provisioned for the intended shared App Group.
- Exercise supported file/image/movie/text/web-URL providers with synthetic data.
- Exceed item count, text byte, per-file, aggregate byte, and package-age limits.
- Cancel/dismiss the extension during provider loading/staging and verify bounded cleanup.
- Provide duplicate/sanitization-colliding names and verify deterministic deconfliction.
- Verify extension publication is atomic: incomplete `.staging-*` packages are never imported as pending packages.
- Corrupt the package manifest, add duplicate/unknown JSON fields, alter version/package ID, or add unexpected files and verify import rejection.
- Add a symlink/reparse-like package/file entry where the platform/filesystem permits and verify rejection.
- Change a staged file length after manifest creation and verify exact-length import rejection.
- Verify stale pending/abandoned staging cleanup.
- Verify imported content is re-staged into app cache and shown for review rather than automatically transmitted.
- Verify App Group packages never contain private keys, pairing nonces/codes, trusted-device secrets, or reusable transfer authorization.

## Desktop and document/open input

- Send malformed `swiftdrop://` activations.
- Verify Windows file/folder/text/pairing-link drop enters the normal bounded review path and never direct-sends.
- Verify Mac Catalyst native drop acquires/releases security-scoped access only for staging, rejects symlink sources, bounds file/aggregate size, and deconflicts names.
- Verify Apple document/open-file URLs stage under temporary security-scoped access and clean failure/cancellation output.
- Verify all external-input text uses the shared UTF-8 byte-safe limiter.

## Network and TLS

- Capture LAN traffic and verify transfer payload contents are not visible in plaintext.
- Verify TLS 1.2/1.3 behavior and reject unsupported legacy configurations according to current implementation/platform policy.
- Verify receiver certificate pin success/failure with exact fingerprints.
- Verify sender client-certificate requirement.
- Verify public/DNS pairing-target rejection remains enforced after refactors.
- Test guest Wi-Fi/client isolation, multicast-blocked networks, host firewalls, and denied local-network permission; SwiftDrop must explain/fail safely rather than bypass policy.
- Test network switching, sleep/lock, and slow/idle peers.

## Local metadata and privacy

- Inspect SQLite after representative operations and confirm it contains metadata only.
- Verify privacy mode hides/redacts peer/file history and sensitive diagnostic identifiers at write/read/export boundaries.
- Verify queue metadata contains generic labels/state/timestamps/bounded error codes only and stale active rows become `Interrupted` after restart.
- Verify completed-batch metadata is bounded/pruned and stores a hashed receive-root key rather than an absolute path.
- Verify malformed/corrupted trust/history/diagnostic/queue/resume rows fail closed and do not break valid rows where corruption tolerance is intended.
- Verify Android backup-disabled configuration and Apple App Group boundaries against signed package behavior.

## Dependency, configuration, and static analysis

- Run the full portable Core test suite and localization/Apple configuration validators.
- Run CodeQL and repository security-hygiene checks.
- Review Dependabot/package advisories.
- Generate exact restored dependency graphs for Core, app, and Share Extension target frameworks.
- Review direct/transitive licenses and third-party notice obligations from the exact release graph.
- Verify Apple app/extension IDs, App Group, versions, entitlements, activation rules, project reference, and solution inclusion with repository validation tooling and the signed artifacts.
- Treat cryptographic, authentication, trust, path, App Group, protocol-framing, or resume-metadata changes as security-sensitive and require focused review.

## Release evidence

Record:

- exact SwiftDrop commit SHA;
- app/extension version/build numbers;
- signed artifact hashes where applicable;
- device/OS/network versions;
- test date/tester;
- outcome and evidence location;
- issue/defect link for every failure;
- retest result after each fix.

A candidate is not security-validated merely because source compiles or unit tests pass.

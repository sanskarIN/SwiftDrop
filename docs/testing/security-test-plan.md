# SwiftDrop Security Test Plan

Updated: 2026-08-15

This plan complements automated unit tests and the manual cross-platform transfer matrix. Execute it against the exact release candidate before public/store distribution. Use synthetic test files, synthetic identities, disposable receive roots, and disposable App Group/cache content only.

## Pairing and authentication

- Verify expired QR/deep-link invitations are rejected.
- Verify a consumed nonce cannot authorize a second transfer.
- Verify invalid request shape is rejected before a one-time transfer nonce is consumed.
- Verify invalid file/batch path metadata is rejected before a one-time transfer nonce is consumed.
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

### Pairing capability canonicality

Starting from a valid generated invitation, verify all of these are rejected rather than treated as aliases:

- leading/trailing space, tab, CR, or LF;
- an added outer authority port, path, fragment, or user-info;
- unknown, duplicate, empty, or reordered extra query fields;
- missing `=` after `p`;
- standard Base64 `+` or `/` in the payload;
- Base64 padding `=`;
- percent-encoded payload characters such as `%2D`;
- a Base64URL text whose length modulo four is invalid;
- any non-canonical Base64URL representation that decodes but does not re-encode identically;
- duplicate/case-variant/unknown decoded JSON properties.

A generated invitation should round-trip exactly through `PairingCodec.Encode`/`Decode` except documented payload-value canonicalization such as fingerprint/address formatting.

## Protocol abuse and parsing

- Send zero, negative, oversized, and truncated JSON frame lengths.
- Send malformed UTF-8, malformed JSON, comments, trailing commas, and excessive nesting.
- Send duplicate fields including case variants at top level and nested levels.
- Add unknown/unmapped fields to otherwise valid top-level and nested typed protocol messages and verify rejection.
- Send unknown/case-changed request types and unsupported protocol versions.
- Cross-smuggle type-specific fields (for example text fields in a file request or file metadata in a pair request) and verify type-shape rejection.
- Send missing/overlong/control-character sender identity.
- Send malformed pairing nonces/codes, transfer IDs, hashes, timestamps, file metadata, batch totals, and duplicate batch paths.
- Verify batch transfer IDs reject whitespace, punctuation outside `-`/`_`, non-ASCII text, and lengths above 128 characters.
- Send receiver batch plans containing unknown/duplicate/missing paths, invalid offsets, contradictory acceptance, or unexpected ordering.
- Send unexpected/reordered `BatchItemStart` frames.
- Exceed single-file, batch-file-count, batch-total-byte, text-snippet, path-depth/path-length, and metadata limits.
- Close the connection at each request/response/payload transition and verify bounded failure.
- Hold metadata/payload reads or writes idle beyond the configured timeout.
- Open repeated connections from one source address and repeated pairing attempts from one certificate fingerprint to verify rate limits.
- Attempt to expand limiter/authorization cardinality and verify bounded behavior.

## Canonical path and filesystem safety

- Attempt `../`, rooted paths, Windows drive/UNC/device-style roots on every target, repeated separators, empty segments, trailing separators, and `.` segments.
- Attempt more than 64 path segments.
- Send a backslash-containing manifest path and verify it is rejected; protocol-v1 wire paths use `/` only.
- Send a path whose filename would change during sanitation, including portable-invalid characters, reserved Windows device names, trailing dot/space behavior, or decomposed Unicode, and verify rejection before authorization.
- Verify a valid sender-created folder manifest uses `/` separators regardless of sender operating system.
- Verify each canonical segment stays within both 180 UTF-16 code units and 180 UTF-8 bytes.
- Test Unicode-heavy and emoji-heavy filenames near the UTF-8 byte limit and verify no broken surrogate/rune is emitted.
- Verify the `.swiftdrop.part` suffix remains below common 255-byte component limits for a maximum SwiftDrop segment.
- Verify collision names remain distinct when the original name is already at the character/byte limit; the uniqueness marker must not be truncated away.
- Verify filename sanitation and final resolution remain beneath the configured receive root.
- Place symlink/reparse components inside a disposable receive root and verify staging/finalization/completed-resume verification reject them.
- Verify existing completed files are never silently overwritten.
- Run concurrent same-destination incoming attempts and confirm reservation/collision behavior produces distinct bounded names or bounded rejection.
- Verify final promotion fails rather than overwriting a destination that appears after planning.
- Verify low-space behavior fails before receiving bytes that cannot fit with safety reserve.
- Interrupt a transfer and verify only bounded `.swiftdrop.part` staging remains.
- Resume with a fresh invitation and verify the receiver returns only a valid staged offset.
- Corrupt or lengthen a staged partial before resume and verify safe truncation/rejection/integrity behavior.
- Corrupt payload/staging and verify SHA-256 mismatch never finalizes it.
- Verify inability to apply optional final timestamp metadata does not convert already-verified/promoted content into a false transfer failure.
- Verify completed files are never automatically launched.

## Outgoing source safety

- Select a normal single file and verify it transfers.
- Replace the selected single-file path with a symlink/reparse point before send and verify the send-boundary check rejects it before payload bytes are written.
- Pause a single-file transfer, replace its source with a symlink where the OS permits, and verify it is removed from resume candidates.
- Select a folder whose root is a symlink/reparse point and verify rejection.
- Put a symlinked file inside a selected folder and verify folder enumeration rejects the source tree.
- Put a symlinked directory/junction inside a selected folder and verify enumeration rejects rather than following it.
- Verify recursive enumeration is bounded by file/directory limits.
- Create the same folder tree in different filesystem enumeration orders and verify the generated source-manifest order is deterministic.
- Modify source length after manifest creation and verify streaming fails rather than changing framing.
- Modify same-length source contents after hashing and verify receiver SHA-256 validation fails.
- Verify relative path-length/count/aggregate limits are rejected during source preflight before expensive hashing where the implementation can know them.

## Batch, selective receive, and idempotent retry

- Reject the whole batch and verify no payload bytes are accepted.
- Accept only a subset and verify unselected items are never sent.
- Try duplicate names from separate source folders and verify sender deconfliction plus receiver collision handling.
- Try case-only, Unicode-normalization, and sanitation-equivalent source names and verify sender creates deterministic portable-distinct manifest paths **before hashing**.
- Verify generated manifest paths are forward-slash canonical on Windows, Android, iOS, and Mac Catalyst.
- Interrupt during an item and between items.
- Pause/resume a batch containing both directly selected files and a selected folder; verify the folder source remains resumable.
- Resume the same interrupted batch with a fresh pairing invitation and verify the stable batch transfer ID is retained.
- Confirm the active UI path calls the stable-ID batch API; no compatibility path should generate a fresh transfer ID per retry.
- Confirm already-finalized items are revalidated and receive a full-length resume offset instead of a collision-renamed duplicate copy.
- Modify an already-finalized destination after the first interrupted attempt and verify the completed-item shortcut is rejected/invalidated.
- Modify/delete the finalized destination **after the receiver sends the retry plan but before the item completion ACK** and verify the second completed-file verification fails closed.
- Delete an already-finalized destination and verify retry does not falsely acknowledge completion.
- Change the receive root and verify completed-item metadata from the old root cannot authorize/skip data in the new root.
- Start a completely new explicit send of identical files and verify it receives a new transfer ID and normal collision-safe duplicate-send behavior.
- Corrupt completed-batch SQLite rows and verify they do not become authorization or false completion.
- Verify `completed_batch_items`, introduced in schema v3 and retained in current schema v6, never contains the absolute receive root, pairing nonce/code, certificate private key, or transferred content.
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
- Provide a negative `OpenableColumns.Size` value and verify it is treated as unknown length rather than trusted negative metadata.
- Provide an incorrect/unknown provider length and verify runtime bytes are bounded by the **remaining aggregate staging budget**, not only the per-file limit.
- For an unknown-length provider, reduce available cache storage during copy and verify periodic storage-reserve checks stop/clean the staging file before exhausting the volume.
- Make a content URI unavailable during copy and verify partial cache output is removed.
- Verify a failed URI copy does not consume staging-budget count/bytes for subsequent valid items.
- Verify portable/UTF-8-bounded filename sanitation and collision naming.
- Verify text + multiple files arrive in one coherent inbox handoff and are never auto-sent.
- Verify stale nested share-cache staging is pruned without touching unrelated app files.
- Verify foreground transfer-service behavior and optional notification-permission denial cannot cause a transfer to fail.
- Verify mDNS multicast-lock acquisition/release when the Android application or Wi-Fi service is temporarily unavailable.

## iOS Share Extension and App Group handoff

The maintained Share Extension target is **iOS-only**. Mac Catalyst external intake is validated separately through the containing desktop app/native-drop path.

- Verify the iOS containing app and iOS Share Extension are signed/provisioned for the intended shared App Group.
- Exercise supported file/image/movie/text/web-URL providers with synthetic data.
- Exceed item count, text byte, per-file, aggregate byte, and package-age limits.
- Verify Share Extension staging budget rejects the file that would exceed aggregate bytes **before copying that over-limit file**.
- Delay a provider callback beyond the configured response timeout and verify bounded failure/cleanup.
- Return a provider before the timeout but make the legitimate local file copy take longer than the response timeout; verify the already-started copy is not incorrectly cancelled solely by the provider-response timer.
- Cancel/dismiss the extension during provider loading/staging and verify bounded cleanup.
- Provide duplicate/sanitization-colliding/max-length names and verify deterministic bounded deconfliction whose collision marker survives truncation.
- Verify extension publication is atomic: incomplete `.staging-*` packages are never imported as pending packages.
- Corrupt the package manifest, add duplicate/unknown JSON fields, alter version/package ID, or add unexpected files and verify import rejection.
- Add undeclared nested directories or extra top-level files to `files/` and verify exact file-set rejection.
- Add a symlink/reparse-like package/file entry where the platform/filesystem permits and verify rejection.
- Change a staged file length after manifest creation and verify exact-length import rejection.
- Fill/reduce app-cache storage before containing-app import and verify the **aggregate validated package bytes** are preflighted before recopy begins.
- Verify stale pending/abandoned staging cleanup.
- Verify imported content is re-staged into app cache and shown for review rather than automatically transmitted.
- Verify only one pending bundle is surfaced for review at a time and later pending bundles are not silently merged/deleted.
- Verify App Group packages never contain private keys, pairing nonces/codes, trusted-device secrets, or reusable transfer authorization.
- Confirm hosted certificate-independent iOS Simulator compilation is treated only as source/API evidence; repeat these checks on signed physical-device/TestFlight builds with real provisioning.

## Mac Catalyst native drop

The maintained Mac Catalyst architecture is the containing desktop app plus native drop and does not include a Mac Catalyst Share Extension target.

- Drop files, folders, text, and pairing links.
- Verify security-scoped access is held only for staging.
- Verify linked/reparse source files/directories are rejected.
- Verify common file-count/per-file/aggregate staging budget behavior.
- Delay file/text provider callbacks beyond the bounded response wait and verify the drop fails/cleans up rather than hanging indefinitely.
- Return the provider before timeout but let a legitimate copy continue longer; verify the response timer does not terminate the active copy.
- Verify maximum-length/Unicode/collision filenames remain bounded and distinct.
- Verify all staged content enters review state and is never auto-sent.
- Verify signed Mac Catalyst sandbox/network/App Group configuration used by the containing app and notarized/package behavior.

## Windows and document/open input

- Send malformed `swiftdrop://` activations.
- Verify Windows file/folder/text/pairing-link drop enters the normal bounded review path and never direct-sends.
- Verify direct Windows file/folder sources pass regular-source/link rejection and canonical manifest construction before transfer.
- Verify Apple document/open-file URLs stage under temporary security-scoped access and clean failure/cancellation output.
- Verify all external-input text uses the shared UTF-8 byte-safe limiter.
- Treat hosted `WindowsPackageType=None` compilation as source/XAML/WinUI evidence only; separately generate/sign/install/update the MSIX/package release candidate and verify protocol registration plus package capabilities.

## Network and TLS

- Capture LAN traffic and verify transfer payload contents are not visible in plaintext.
- Verify TLS 1.2/1.3 behavior and reject unsupported legacy configurations according to current implementation/platform policy.
- Verify receiver certificate pin success/failure with exact fingerprints.
- Verify sender client-certificate requirement.
- Verify public/DNS pairing-target rejection remains enforced after refactors.
- Test guest Wi-Fi/client isolation, multicast-blocked networks, host firewalls, and denied local-network permission; SwiftDrop must explain/fail safely rather than bypass policy.
- Test network switching, sleep/lock, and slow/idle peers.

## Local metadata and privacy

- Inspect a fresh/upgrade SQLite database and confirm current schema version is 6.
- Verify v0/v1/v2/v3/v4/v5 databases migrate to v6 according to the supported migration contract; preserve safe legacy v3 queue rows with defaults, leave v4 performance fields null, and preserve v5 duration without inventing measured bytes.
- Inspect SQLite after representative operations and confirm it contains metadata only.
- Verify valid completed performance rows contain only bounded duration plus actual attributable measured bytes and never peer endpoints, transfer contents, pairing capabilities/nonces, credentials, certificates/private keys, or reusable authorization.
- Resume a file and verify `measured_bytes` equals bytes actually transferred after the negotiated resume offset and never exceeds logical `size_bytes`.
- Seed malformed/impossible performance metadata and verify it is rejected/skipped rather than influencing throughput or weakening transfer authorization/integrity behavior.
- Verify optional performance-normalization failure is best-effort and cannot convert a successful transfer into a failed transfer result.
- Verify privacy mode hides/redacts peer/file history and sensitive diagnostic identifiers at write/read/export boundaries.
- Verify queue metadata stores only generic labels, state/timestamps, bounded machine error codes, bounded operation categories, progress basis points, and optional item counts.
- Verify queue progress is monotonic and bounded to `0..10000`; completed item count does not exceed total count when both are known.
- Verify queue schema has no nonce/token/certificate/private-key/host/port/source-path/destination-path/content/reusable-authorization field.
- Seed queued/running rows with safe progress, restart SwiftDrop, and confirm they become `Interrupted` while retaining safe progress/context and without automatic replay.
- Verify another transfer attempt after restart still requires fresh pairing/authorization.
- Cancel queue initialization or a caller-cancelled best-effort queue metadata write and confirm later queue persistence remains available; ordinary caller cancellation must not be interpreted as storage corruption.
- Verify completed-batch metadata is bounded/pruned and stores a hashed receive-root key rather than an absolute path.
- Verify completed-batch source paths remain canonical protocol paths while destination paths remain local receiver metadata re-confined before reuse.
- Verify malformed/corrupted trust/history/diagnostic/queue/resume rows fail closed and do not break valid rows where corruption tolerance is intended.
- Verify Android backup-disabled configuration and Apple App Group boundaries against signed package behavior.

## Dependency, configuration, and static analysis

- Run the full portable Core test suite and localization/Apple configuration validators.
- Run CodeQL and repository security-hygiene checks.
- Review Dependabot/package advisories.
- Generate exact restored dependency graphs for Core, app, and the iOS Share Extension target framework.
- Review direct/transitive licenses and third-party notice obligations from the exact release graph.
- Verify Apple app/iOS extension IDs, App Group, versions, entitlements, activation rules, project reference, and solution inclusion with repository validation tooling and the signed artifacts.
- Verify no obsolete compatibility/dead transfer handler is wired from XAML after the stable-ID cleanup.
- Treat cryptographic, authentication, trust, canonical-path, source-link, App Group, protocol-framing, staging-budget, queue-persistence, or resume-metadata changes as security-sensitive and require focused review.
- Verify the restored application dependency graph uses the intended .NET 10 MAUI servicing baseline and does not reintroduce the previously blocked vulnerable SQLite native dependency.

## Release evidence

Record:

- exact SwiftDrop commit SHA;
- app and applicable iOS extension version/build numbers;
- signed artifact hashes where applicable;
- device/OS/network versions;
- test date/tester;
- outcome and evidence location;
- issue/defect link for every failure;
- retest result after each fix.

A candidate is not security-validated merely because source compiles or unit tests pass.

## Native notification privacy and lifecycle

- Verify optional terminal notifications are disabled by default and require explicit user enable.
- Verify completion/failure bodies are generic localized resource values with no dynamic format placeholders.
- Confirm no filename, peer/device name, path, transfer content, pairing capability/nonce/code/fingerprint, transfer ID, or reusable authorization reaches OS notification history.
- Confirm Apple notification authorization is local alert/sound only and does not register a remote-push token or require a relay service.
- Confirm Apple foreground notification presentation works only through the retained notification-center delegate and cleanup does not crash shutdown.
- Confirm Windows retains `privateNetworkClientServer`, does not add `internetClient`, and uses matching toast/COM CLSIDs plus the expected activation arguments.
- Confirm Windows attaches `NotificationInvoked` before `Register()` and registers at startup when the persisted preference is already enabled.
- Confirm notification permission/registration/show failure never changes the underlying transfer result.
- Run `scripts/validate_windows_integration.py` and all Windows validator regression tests before candidate packaging.

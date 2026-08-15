# Local metadata database

SwiftDrop uses SQLite for local metadata. Transferred file bytes and text snippet contents are not stored in SQLite.

Current database path in the MAUI application:

`FileSystem.AppDataDirectory/swiftdrop.db`

Current schema version: **4**.

## `trusted_peers`

Purpose: explicit local trust records.

Columns:

- `device_id` — primary key.
- `device_name` — last stored display name.
- `fingerprint` — exact trusted certificate fingerprint.
- `trusted_utc` — first trust timestamp.
- `last_seen_utc` — latest known use/observation timestamp.

Trust matching uses device ID plus certificate fingerprint. A matching display name alone is never sufficient.

## `transfer_history`

Purpose: local transfer status metadata.

Columns:

- `id` — primary key.
- `direction` — sent/received direction metadata.
- `peer_device_name` — peer display name at record time.
- `file_name` — filename/description, or privacy-mode placeholder.
- `size_bytes` — declared size.
- `timestamp_utc` — event timestamp.
- `status` — completed, failed, cancelled, paused, rejected, not-selected, etc.
- `integrity_verified` — whether completed file integrity was verified.

Retention pruning is controlled by local settings. A zero-day retention setting clears retained history rather than creating hidden permanent history.

## `diagnostic_events`

Purpose: bounded privacy-aware troubleshooting metadata.

Columns:

- `id` — primary key.
- `timestamp_utc`.
- `level`.
- `code`.
- `message`.

Diagnostic messages are bounded and single-line. The diagnostic subsystem is designed not to record transferred file contents, text snippet contents, private keys, pairing nonces, or full pairing invitations. Privacy mode further redacts path/email-like tokens from safe diagnostic messages.

## `transfer_queue_metadata`

Purpose: restart-safe, privacy-minimal queue status/progress metadata.

Columns:

- `id` — queue item identifier.
- `label` — persisted as the generic `Transfer` label; source filenames/text are not persisted here.
- `state` — `Queued`, `Running`, `Completed`, `Failed`, `Cancelled`, or `Interrupted`.
- `created_utc` — queue creation timestamp.
- `started_utc` — optional start timestamp.
- `finished_utc` — optional terminal timestamp.
- `error_code` — optional bounded machine-oriented code such as an exception type or `app-restarted`; no free-form exception message is persisted.
- `operation_kind` — bounded non-secret category: `Transfer`, `File`, `Batch`, `Text`, or `Receive`.
- `updated_utc` — most recent persisted queue/progress update timestamp.
- `progress_basis_points` — monotonic progress in the inclusive range `0..10000` (`10000 == 100%`).
- `item_count` — optional non-negative total item count when known.
- `completed_item_count` — optional non-negative completed item count, never greater than `item_count` when both are known.

The application throttles ordinary progress persistence to coarse progress buckets while still persisting state transitions and item-count changes. This avoids turning every transfer progress callback into a SQLite write while retaining useful restart diagnostics.

When SwiftDrop starts, stale persisted `Queued` or `Running` rows are marked `Interrupted`. Their most recent safe progress/context remains visible, but they are **not automatically retried**, because pairing authorization/nonces are intentionally not persisted or replayed. A retry must acquire fresh authorization through the normal pairing flow.

The queue table never stores text contents, file contents, source file paths, destination paths, peer IP addresses/ports, pairing invitations, pairing nonces, bearer/session tokens, peer certificates, private keys, or reusable credentials. Automated schema tests explicitly guard several authorization/endpoint field-name classes from entering this table.

## `completed_batch_items`

Purpose: idempotent completed-item verification metadata for stable batch retry.

Columns:

- `transfer_id` — stable batch transfer identifier.
- `source_relative_path` — canonical portable source path.
- `receive_root_key` — non-reversible local receive-root identity used to bind completion metadata to the current root.
- `destination_relative_path` — effective destination relative path.
- `length` — expected byte length.
- `sha256` — expected SHA-256 digest.
- `completed_utc` — completion timestamp.

The table stores metadata only. A retry does not trust this row by itself: the current destination is confined/revalidated and re-hashed before it can be negotiated as already complete, and it is verified again immediately before a zero-byte completion acknowledgement.

## Schema migrations

- **0 → 1:** create trusted peers, transfer history, and diagnostic events.
- **1 → 2:** create privacy-minimal transfer queue metadata and indexes.
- **2 → 3:** create completed-batch item metadata and index.
- **3 → 4:** extend queue metadata with operation kind, update timestamp, bounded progress basis points, and optional item counts; legacy rows receive safe defaults (`operation_kind='Transfer'`, progress `0`, nullable counts).

The migration manager applies versions sequentially inside transactions, rejects a database whose `PRAGMA user_version` is newer than the application supports, and has automated tests for version-zero migration, v1/v2 upgrade paths, legacy v3 queue-row migration, idempotence, and future-version rejection.

## Schema evolution rules

Before a release changes a table incompatibly:

1. Add an explicit schema version/migration step.
2. Test upgrade from the previous supported schema and preserve existing metadata that remains safe/useful.
3. Preserve privacy-mode behavior.
4. Never migrate transferred file/text contents, pairing capabilities, endpoints, session/bearer tokens, credentials, certificates/private keys, or reusable authorization into queue persistence.
5. Add migration, validation, and rollback/recovery notes/tests.
6. Treat persisted queue rows as status/progress metadata, never as reusable transfer authorization.
7. Keep persisted progress bounded and item-count relationships internally consistent.

The database is local application data, not a synchronization or cloud database.

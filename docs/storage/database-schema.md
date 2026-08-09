# Local metadata database

SwiftDrop uses SQLite for local metadata. Transferred file bytes and text snippet contents are not stored in SQLite.

Current database path in the MAUI application:

`FileSystem.AppDataDirectory/swiftdrop.db`

Current schema version: **2**.

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
- `timestamp_utc`
- `level`
- `code`
- `message`

Diagnostic messages are bounded and single-line. The diagnostic subsystem is designed not to record transferred file contents, text snippet contents, private keys, pairing nonces, or full pairing invitations. Privacy mode further redacts path/email-like tokens from safe diagnostic messages.

## `transfer_queue_metadata`

Purpose: restart-safe, privacy-minimal queue status metadata.

Columns:

- `id` — queue item identifier.
- `label` — persisted as a generic `Transfer` label; source filenames/text are not persisted here.
- `state` — `Queued`, `Running`, `Completed`, `Failed`, `Cancelled`, or `Interrupted`.
- `created_utc` — queue creation timestamp.
- `started_utc` — optional start timestamp.
- `finished_utc` — optional terminal timestamp.
- `error_code` — optional bounded machine-oriented code such as an exception type or `app-restarted`; no free-form exception message is persisted.

When SwiftDrop starts, stale persisted `Queued` or `Running` rows are marked `Interrupted`. They are status history only and are **not automatically retried**, because pairing authorization/nonces are intentionally not persisted or replayed. The store keeps a bounded recent set and supports deleting finished metadata.

The queue table never stores text contents, file contents, source file paths, destination paths, peer IP addresses, pairing invitations, nonces, certificates/private keys, or reusable credentials.

## Schema migrations

- **0 → 1:** create trusted peers, transfer history, and diagnostic events.
- **1 → 2:** create privacy-minimal transfer queue metadata and indexes.

The migration manager applies versions sequentially inside transactions, rejects a database whose `PRAGMA user_version` is newer than the application supports, and has automated tests for version-zero migration, v1→v2 upgrade, idempotence, and future-version rejection.

## Schema evolution rules

Before a release changes a table incompatibly:

1. Add an explicit schema version/migration step.
2. Test upgrade from the previous supported schema.
3. Preserve privacy-mode behavior.
4. Never migrate transferred file/text contents, pairing capabilities, credentials, or private-key material into the database.
5. Add migration tests and rollback/recovery notes.
6. Treat persisted queue rows as status metadata, never as reusable transfer authorization.

The database is local application data, not a synchronization or cloud database.

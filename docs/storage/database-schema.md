# Local metadata database

SwiftDrop uses SQLite for local metadata. Transferred file bytes and text snippet contents are not stored in SQLite.

Current database path in the MAUI application:

`FileSystem.AppDataDirectory/swiftdrop.db`

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

## Schema evolution rules

Before a release changes a table incompatibly:

1. Add an explicit schema version/migration step.
2. Test upgrade from the previous supported schema.
3. Preserve privacy-mode behavior.
4. Never migrate transferred file contents into the database.
5. Add migration tests and rollback/recovery notes.

The database is local application data, not a synchronization or cloud database.

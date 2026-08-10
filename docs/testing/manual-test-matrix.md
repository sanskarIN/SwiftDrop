# SwiftDrop Manual Test Matrix

Use synthetic disposable test files only. Never use secrets or irreplaceable personal files while validating development/release candidates.

Record the exact commit, app/extension versions, OS/device versions, network type, source/destination SHA-256, pass/fail result, defect link, and retest result.

## Cross-device directions

| Sender | Receiver | Pairing | Small file | Large file | Multi/folder | Text | Resume | Completed-item retry | Collision |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Android | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Android | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Android | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Android | Mac Catalyst | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Windows | Mac Catalyst | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| iOS | Mac Catalyst | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Mac Catalyst | Android | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Mac Catalyst | Windows | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Mac Catalyst | iOS | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Mac Catalyst | Mac Catalyst | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |

## Pairing and identity

1. Generate a fresh invitation; confirm it contains no private key.
2. Confirm strict decoder rejects malformed/duplicate/unknown pairing fields.
3. Confirm public IP/DNS target is rejected by protocol v1.
4. Confirm sender rejects a receiver certificate whose SHA-256 differs from invitation.
5. Confirm receiver requires sender client certificate.
6. Confirm malformed request does not consume a valid transfer nonce.
7. Confirm missing client certificate does not consume a valid transfer nonce.
8. Confirm accepted transfer consumes nonce exactly once.
9. Reuse same invitation and confirm rejection.
10. Wait beyond pairing lifetime and confirm rejection.
11. Verify nearby pairing binds returned invitation to discovered TLS certificate/address/port.
12. Verify manual-IP pairing requires fresh 8-digit code and visual fingerprint confirmation.
13. Reset identity; confirm local trust is cleared and peers must deliberately pair again.

## Single-file transfer safety

1. Send zero-byte file.
2. Send normal text/image file.
3. Send synthetic large file near configured safety limit.
4. Attempt above-limit file; confirm rejection before payload transfer.
5. Interrupt transfer; retain `.swiftdrop.part`; retry with fresh pairing and verify bounded resume.
6. Corrupt staged partial; confirm integrity failure prevents finalization.
7. Grow/shrink source after manifest creation; confirm sender fails rather than changing wire frame length.
8. Attempt `../`, rooted paths, Windows drive/UNC/device syntax, alternate separators, and nested traversal; confirm rejection.
9. Create receive-root symlink/reparse component; confirm receive fails safely without writing outside approved root.
10. Existing final filename: confirm collision-safe destination rather than overwrite.
11. Create final destination from another process after reservation but before promotion; confirm SwiftDrop preserves that file and fails closed.
12. Fill disposable volume close to capacity; confirm capacity guard rejects before consuming remaining payload.
13. Confirm manifest timestamp is applied to completed file where platform permits it.

## Idempotent interrupted-batch resume

1. Start batch with at least 3 files.
2. Let first file finalize.
3. Interrupt during second/third file.
4. Obtain fresh pairing invitation/authorization.
5. Resume from SwiftDrop Resume control.
6. Confirm sender retained same stable batch transfer ID.
7. Confirm receiver re-hashes the already-finalized first file.
8. Confirm first file plan returns full-length resume offset.
9. Confirm sender still emits normal batch-item-start but zero raw payload bytes for completed item.
10. Confirm no collision-renamed duplicate of first file appears.
11. Confirm interrupted current partial resumes from staged offset.
12. Delete completed first destination and retry; confirm it transfers normally.
13. Modify completed destination at same length and retry; confirm SHA mismatch prevents completed-item reuse.
14. Modify sender source/manifest and retry; confirm old completion state is not reused.
15. Switch receive root and retry; confirm old completion state is not reused.
16. Start a brand-new explicit batch of same files; confirm new transfer ID preserves normal collision semantics.
17. Corrupt/delete completion metadata database row; confirm transfer still works (resume optimization may be lost but verified transfer must not fail).

## Batch receive/selection

- Accept all files.
- Accept only selected files.
- Reject entire batch.
- Verify unknown receiver plan path is rejected by sender.
- Verify duplicate/missing/contradictory receiver plan is rejected.
- Verify item-start order mismatch is rejected.
- Verify aggregate capacity is checked before accepted payload bytes.
- Verify many-file/folder batch near 2,048-file limit.
- Verify aggregate batch-size bound.
- Verify source folders with duplicate/sanitized-colliding names are deconflicted safely.

## Android share-sheet intake

Use providers from several apps where possible.

- Share text only.
- Share one file with provider-declared size.
- Share one file whose provider does not expose size.
- Share multiple files.
- Share content whose display name requires sanitation.
- Attempt oversized provider item; confirm staging rejects and partial cache file is removed.
- Attempt more than protocol max items; confirm bounded intake.
- Fill cache volume near capacity; confirm staging fails safely.
- Confirm one share action produces one review-inbox handoff.
- Confirm shared files/text are never auto-sent.
- Confirm stale cache pruning.
- Confirm Android app-local backup remains disabled in release manifest/binary.

## iOS Share Extension / App Group

Use signed builds with real App Group provisioning.

- Confirm extension appears for text, files, images, movies, and web URLs declared by activation rule.
- Share text only.
- Share one file.
- Share multiple files.
- Share image/movie provider temporary representation.
- Share URL text.
- Verify extension copies temporary provider representations while access is valid.
- Verify extension publishes `.staging-*` → `pending-*` atomically.
- Cold-start SwiftDrop after share; confirm pending package imports.
- Warm/foreground SwiftDrop after share; confirm import occurs.
- Confirm content appears for review and is never auto-sent.
- Manually corrupt/augment manifest with unknown field; confirm package rejected.
- Create stale package; confirm pruning.
- Create symlink/reparse package/file where filesystem permits; confirm rejection.
- Remove/corrupt App Group provisioning in a test signing profile; confirm failure is detectable and release is blocked.
- Verify extension cancellation/dismissal does not continue indefinite staging.

## Mac Catalyst native drop / Share Extension

- Drop one Finder file.
- Drop multiple files.
- Drop folder with nested files.
- Drop two directories/files whose names sanitize/case-fold to collisions; confirm staging deconflicts.
- Drop text.
- Drop `swiftdrop://pair` link; confirm strict pairing review.
- Drop from external/security-scoped location/provider.
- Drop symlink/reparse file/folder; confirm rejection.
- Verify per-file/count/aggregate/capacity limits.
- Confirm native drop integration detaches when main page is disposed.
- Repeat Share Extension tests under Mac sandbox/App Group signing.
- Verify Share Extension and native drop remain review-before-send surfaces.

## Windows native drop / picker

- Drop one file.
- Drop multiple files.
- Drop folder.
- Drop text.
- Drop pairing link.
- Verify package protocol activation after cold/warm start.
- Verify receive FolderPicker location persists/works after packaging.
- Verify dropped content enters one review handoff and is never auto-sent.
- Verify firewall deny/allow behavior.

## Text/clipboard

- Send normal text snippet.
- Send text near UTF-8 byte limit with multi-byte Unicode/emoji.
- Confirm external text truncation never splits Unicode scalar/surrogate pair.
- Confirm expired text request is rejected.
- Confirm accepted text acknowledgement requires offset 0.
- Confirm clipboard is read only after explicit paste action.
- Confirm transferred text contents do not appear in SQLite history/queue/diagnostics.

## Potentially dangerous files

Use synthetic empty files such as `.exe`, `.ps1`, `.apk`, `.zip`, `.docm`, `.jpg`.

- High-risk extensions show warning.
- Caution extensions show caution.
- Ordinary files still show sender/name/size/fingerprint.
- No file auto-opens/executes.
- UI/documentation never calls the extension classifier malware scanning.

## Network/lifecycle failure scenarios

- Receiver closes during TLS handshake.
- Receiver closes after metadata before bytes.
- Sender cancels mid-transfer.
- Wi-Fi disconnects mid-transfer.
- Device changes networks mid-transfer.
- Windows/macOS firewall blocks inbound traffic.
- Apple local-network permission denied.
- Guest Wi-Fi/client isolation blocks direct connectivity.
- Multicast blocked while QR/manual direct IP remains possible.
- IPv4-only LAN.
- IPv6-capable LAN.
- Sender/receiver sleeps or locks.
- App foreground/background transition.
- Real low-storage change during active transfer.

Every failure must be bounded, must not freeze UI thread, must not silently replay stale authorization, and must leave either a verified final file or an explicit staged/failed state.

## SQLite/schema/privacy scenarios

- Fresh database → schema v3.
- Real copied v1 database → v3.
- Real copied v2 database → v3.
- Future schema version rejection.
- Corrupt trust fingerprint row ignored.
- Corrupt history/diagnostic/completion row does not break valid rows or grant trust/resume.
- Privacy mode stores/redacts peer and file names.
- Diagnostics redact paths/email/IP/endpoints/GUID/fingerprint/pair links.
- Queue persistence contains generic labels/machine codes only.
- Completed-batch metadata contains hashed receive-root identity, not absolute receive root.
- Clearing app data/history behaves as documented.

## UI/accessibility/localization

- Light/dark/system themes.
- Largest platform text scaling.
- Reduce-motion preference.
- High contrast.
- Portrait/landscape/tablet/narrow/resized desktop.
- Keyboard-only navigation on desktop.
- Focus order and semantic labels/headings.
- TalkBack.
- VoiceOver iOS.
- VoiceOver Mac Catalyst.
- Narrator.
- Long filename/device name/fingerprint wrapping.
- English UI/dialog/runtime statuses.
- Hindi UI/dialog/runtime statuses and wrapping.
- Confirm progress/status meaning does not rely on color alone.

## Release evidence

For each test set retain:

- exact commit/tag;
- app and extension version/build;
- signed package identifiers;
- OS/device versions;
- App Group/provisioning profile identifiers for Apple tests;
- network type;
- synthetic file sizes/SHA-256;
- pass/fail result;
- screenshots/logs containing no real pairing capabilities or personal content;
- defect link;
- retest result.

Manual validation is release evidence, not a substitute for automated regression tests; discovered repeatable defects should become automated tests where practical.

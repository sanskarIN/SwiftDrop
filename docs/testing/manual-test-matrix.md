# SwiftDrop Manual Test Matrix

Updated: 2026-08-14

Use synthetic disposable test files only. Never use secrets or irreplaceable personal files while validating development/release candidates.

Record the exact commit, app/extension versions, OS/device versions, network type, source/destination SHA-256, pass/fail result, defect link, and retest result.

## Cross-device directions

| Sender | Receiver | Pairing | Small file | Large file | Multi/folder | Text | Resume | Completed-item retry | Collision |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
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

For every direction that supports folder/multi transfer, explicitly inspect negotiated relative paths/log-safe diagnostics and confirm the protocol representation uses `/` regardless of sender OS.

## Pairing and identity

1. Generate a fresh invitation; confirm it contains no private key.
2. Confirm strict decoder rejects malformed/duplicate/unknown pairing fields.
3. Confirm public IP/DNS target is rejected by protocol v1.
4. Confirm sender rejects a receiver certificate whose SHA-256 differs from invitation.
5. Confirm receiver requires sender client certificate.
6. Confirm malformed request does not consume a valid transfer nonce.
7. Confirm malformed file/batch path does not consume a valid transfer nonce.
8. Confirm missing client certificate does not consume a valid transfer nonce.
9. Confirm accepted transfer consumes nonce exactly once.
10. Reuse same invitation and confirm rejection.
11. Wait beyond pairing lifetime and confirm rejection.
12. Verify nearby pairing binds returned invitation to discovered TLS certificate/address/port.
13. Verify manual-IP pairing requires fresh 8-digit code and visual fingerprint confirmation.
14. Reset identity; confirm local trust is cleared and peers must deliberately pair again.

### Canonical pairing capability representation

Starting with a freshly generated valid pairing link:

- add leading/trailing whitespace; reject;
- add unknown/duplicate/empty query fields; reject;
- remove the `=` after `p`; reject;
- replace Base64URL characters with standard Base64 `+` or `/`; reject;
- append Base64 padding `=`; reject;
- percent-encode a payload character; reject;
- add unexpected authority port/path/fragment/user-info; reject;
- inject unknown/duplicate/case-variant decoded JSON property; reject;
- confirm a normal generated link still decodes and its payload values canonicalize as documented.

## Single-file transfer safety

1. Send zero-byte file.
2. Send normal text/image file.
3. Send synthetic large file near configured safety limit.
4. Attempt above-limit file; confirm rejection before payload transfer.
5. Interrupt transfer; retain `.swiftdrop.part`; retry with fresh pairing and verify bounded resume.
6. Corrupt staged partial; confirm integrity failure prevents finalization.
7. Grow/shrink source after manifest creation; confirm sender fails rather than changing wire frame length.
8. Change source contents without changing length after hashing; confirm receiver SHA-256 prevents false success.
9. Attempt `../`, rooted paths, Windows drive/UNC/device syntax, repeated/trailing separators, empty segments, backslash wire paths, and nested traversal; confirm rejection.
10. Attempt more than 64 canonical relative-path segments; confirm rejection.
11. Send a filename that would change during canonical sanitation (portable invalid character, Windows device name, trailing space/dot alias, decomposed Unicode); confirm incoming manifest is rejected rather than rewritten after authorization.
12. Create receive-root symlink/reparse component; confirm receive fails safely without writing outside approved root.
13. Existing final filename: confirm collision-safe destination rather than overwrite.
14. Use a maximum-length/Unicode-heavy destination name and create repeated collisions; confirm every collision marker remains distinct and bounded.
15. Create final destination from another process after reservation but before promotion; confirm SwiftDrop preserves that file and fails closed.
16. Fill disposable volume close to capacity; confirm capacity guard rejects before consuming remaining payload.
17. Confirm optional manifest timestamp is applied where permitted; deny timestamp metadata update where possible and confirm verified content remains a completed transfer.
18. Replace a selected source path with a symbolic link/reparse point before streaming; confirm sender rejects before payload bytes are written.
19. Pause, replace the paused source with a link, and confirm single-file Resume becomes unavailable rather than following the link.

## Idempotent interrupted-batch resume

1. Start batch with at least 3 files.
2. Include at least one selected folder source in a separate run.
3. Let first file finalize.
4. Interrupt during second/third file.
5. Obtain fresh pairing invitation/authorization.
6. Resume from SwiftDrop Resume control.
7. Confirm sender retained same stable batch transfer ID.
8. Confirm batch Resume preserves still-valid folder sources.
9. Confirm receiver re-hashes the already-finalized first file while creating the retry plan.
10. Confirm first file plan returns full-length resume offset.
11. Confirm sender still emits normal canonical batch-item-start but zero raw payload bytes for completed item.
12. Confirm receiver re-verifies the completed item again immediately before the zero-byte completion ACK.
13. Confirm no collision-renamed duplicate of first file appears.
14. Confirm interrupted current partial resumes from staged offset.
15. Delete completed first destination and retry; confirm it transfers normally.
16. Modify completed destination at same length and retry; confirm SHA mismatch prevents completed-item reuse.
17. Modify/delete completed destination after the retry plan is sent but before item ACK and confirm second verification fails closed.
18. Modify sender source/manifest and retry; confirm old completion state is not reused.
19. Replace a paused source file/folder with a symlink and confirm it is removed from resume candidates.
20. Switch receive root and retry; confirm old completion state is not reused.
21. Start a brand-new explicit batch of same files; confirm new transfer ID preserves normal collision semantics.
22. Corrupt/delete completion metadata database row; confirm transfer still works (resume optimization may be lost but verified transfer must not fail).
23. Verify no app compatibility path can silently create a fresh transfer ID during the stable resume workflow.

## Batch source enumeration and receive selection

- Accept all files.
- Accept only selected files.
- Reject entire batch.
- Verify unknown receiver plan path is rejected by sender.
- Verify duplicate/missing/contradictory receiver plan is rejected.
- Verify item-start order mismatch is rejected.
- Verify aggregate capacity is checked before accepted payload bytes.
- Verify many-file/folder batch near 2,048-file limit.
- Verify aggregate batch-size bound.
- Verify relative-path limit is preflighted before hashing.
- Select a symlinked folder root and confirm rejection.
- Put a symlinked file inside an otherwise normal selected folder and confirm rejection.
- Put a symlinked directory/junction inside a selected folder and confirm SwiftDrop does not traverse it.
- Build/send the same folder twice with the same stable retry ID and unchanged sources; confirm deterministic manifest ordering/paths/hashes.
- Verify source folders with duplicate/sanitized/case/Unicode-colliding names are deconflicted before hashing.
- Verify generated wire relative paths use `/` on Windows and match receiver plans exactly on Android/iOS/Mac Catalyst.
- Test very deep source trees near and above the 64-segment protocol limit.

## Android share-sheet intake

Use providers from several apps where possible.

- Share text only.
- Share one file with provider-declared size.
- Share one file whose provider does not expose size.
- Share one file whose provider reports a negative size; confirm it is treated as unknown.
- Share multiple files.
- Share content whose display name requires sanitation/UTF-8 bounding.
- Attempt oversized provider item; confirm staging rejects and partial cache file is removed.
- Attempt multiple individually-valid files whose sum exceeds aggregate limit; confirm common staging budget stops the over-limit item.
- Attempt more than protocol max items; confirm bounded intake.
- For unknown-size input, verify runtime bytes cannot exceed remaining aggregate budget.
- Fill/reduce cache volume during an unknown-size copy; confirm repeated reserve checks stop/clean the copy rather than exhausting storage.
- Cause one item copy to fail, then provide a valid item; confirm the failed item did not consume staging budget.
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
- Delay provider response beyond 20 seconds; confirm bounded failure/cleanup rather than indefinite wait.
- Return provider before the timeout but let a valid local copy continue longer; confirm provider-response timeout does not cancel an already-started copy.
- Verify extension-level common staging budget rejects the file that would exceed aggregate bytes before copying that over-limit file.
- Verify extension publishes `.staging-*` → `pending-*` atomically.
- Cold-start SwiftDrop after share; confirm pending package imports.
- Warm/foreground SwiftDrop after share; confirm import occurs.
- Fill/reduce app cache before import; confirm containing app preflights aggregate validated package bytes before recopy begins.
- Confirm content appears for review and is never auto-sent.
- Manually corrupt/augment manifest with duplicate/unknown field; confirm package rejected.
- Add undeclared top-level file to package `files/`; reject.
- Add undeclared nested directory under `files/`; reject.
- Create stale package; confirm pruning.
- Create symlink/reparse package/file where filesystem permits; confirm rejection.
- Remove/corrupt App Group provisioning in a test signing profile; confirm failure is detectable and release is blocked.
- Verify extension cancellation/dismissal does not continue indefinite staging.
- Queue two pending valid packages before app activation; confirm one is presented for review and the later package is not silently merged/deleted.

## Mac Catalyst native drop

The maintained Mac Catalyst architecture uses the containing desktop app and native drop; it does not include a Mac Catalyst Share Extension target.

- Drop one Finder file.
- Drop multiple files.
- Drop folder with nested files.
- Drop two directories/files whose names sanitize/case-fold to collisions; confirm bounded staging deconflicts.
- Drop max-length/Unicode-heavy collision names; confirm distinct markers survive filename caps.
- Drop text.
- Drop `swiftdrop://pair` link; confirm strict pairing review.
- Drop from external/security-scoped location/provider.
- Drop symlink/reparse file/folder; confirm rejection.
- Verify common per-file/count/aggregate staging budget.
- Delay native-drop provider file/text response beyond configured timeout; confirm bounded failure/cleanup.
- Return provider before timeout but let copy continue longer; confirm response timer does not terminate valid active copy.
- Confirm native drop integration detaches when main page is disposed.
- Verify signed Mac Catalyst sandbox/App Group behavior used by the containing app.
- Verify native drop remains a review-before-send surface.

## Windows native drop / picker

- Drop one file.
- Drop multiple files.
- Drop folder.
- Drop text.
- Drop pairing link.
- Verify package protocol activation after cold/warm start.
- Verify receive FolderPicker location persists/works after packaging.
- Verify dropped content enters one review handoff and is never auto-sent.
- Verify direct file/folder source symlink/reparse rejection before transfer.
- Verify Windows sender folder manifests use `/` wire paths and interoperate with Android/iOS/Mac receivers.
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
- Completed-batch source path is the canonical protocol path while destination path remains local receiver metadata and is re-confined/re-hashed before reuse.
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
- app and applicable iOS extension version/build;
- signed package identifiers;
- OS/device versions;
- App Group/provisioning profile identifiers for iOS Share Extension tests;
- network type;
- synthetic file sizes/SHA-256;
- pass/fail result;
- screenshots/logs containing no real pairing capabilities or personal content;
- defect link;
- retest result.

Manual validation is release evidence, not a substitute for automated regression tests; discovered repeatable defects should become automated tests where practical.

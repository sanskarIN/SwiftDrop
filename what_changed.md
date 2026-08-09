# What changed

Date: 2026-08-09
Repository: https://github.com/sanskarIN/SwiftDrop

## Source prompt alignment

- Re-opened and analyzed the uploaded `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md` from the File Library during this continuation.
- Continued implementation against its local-first, account-free, cross-platform .NET MAUI/C# architecture, security, storage, receive-safety, settings, diagnostics, history, testing, accessibility, documentation, and release-engineering requirements.
- Preserved Apache-2.0 licensing, `Made by the Sanskar` branding, business email `sanskarin@outlook.in`, support email `supportramsandesh@gmail.com`, and repository/profile references.

## Implementation added in this continuation

- Added repository-wide `.editorconfig` rules for C#, XAML/XML/MSBuild, and Markdown formatting.
- Added a strongly typed transfer-history model.
- Added SQLite transfer-history persistence with initialization, insertion, recent-history loading, and clearing.
- Added transfer-history tests for round-trip persistence and clearing.
- Expanded trusted-device persistence with validation, indexing, listing, clearing, and improved date parsing.
- Added trusted-device lifecycle tests.
- Added strongly typed app settings for transfer concurrency, history retention, privacy mode, trusted-device auto-accept preference, and theme.
- Added settings validation and tests.
- Added MAUI settings persistence backed by device preferences.
- Added a complete Settings page with transfer, privacy/trust, and appearance controls, save behavior, reset behavior, and visible project branding.
- Added a complete Transfer History page with local-only history display, refresh, and clear-history behavior.
- Added app-layer transfer-history recording with privacy-mode filename suppression for new history rows.
- Registered settings, history, diagnostics, and navigation services through dependency injection.
- Expanded the main transfer dashboard with History, Settings, Cancel, local-network diagnostics, and clearer security guidance.
- Added sender-side cancellation handling and sent-transfer success/cancel/failure history recording.
- Added local-network diagnostics that detect missing active interfaces, IPv4 limitations, and common Wi-Fi isolation issues without exposing user content.
- Added file-risk classification for executable, installer, script, archive, disk-image, package, and macro-enabled filename extensions.
- Added file-risk classifier tests.
- Added explicit incoming-transfer approval before file bytes are accepted.
- Incoming approval now displays sender device name, filename, declared size, sender certificate fingerprint, and a warning for high-risk/caution extensions.
- Incoming approval explicitly states that SwiftDrop will not automatically open received files.
- Added received-transfer history recording for completed, rejected, cancelled, and failed outcomes.
- Changed TLS client behavior so the sending device presents its local device certificate during the TLS handshake.
- Changed TLS server behavior so an incoming SwiftDrop connection requires a sender certificate.
- Added sender device ID/name to the first authenticated transfer request.
- Receiver now derives the sender certificate fingerprint from the authenticated TLS channel instead of trusting a fingerprint supplied in application JSON.
- Retained receiver-certificate SHA-256 pinning on the sender using the fingerprint carried by the short-lived pairing invitation.
- Added collision-free receive destinations so an existing completed filename is not silently overwritten.
- Added destination free-space checking with a safety reserve before receiving the remaining payload.
- Added a reusable bounded pairing-attempt rate limiter.
- Added tests proving rate-limit window behavior and per-key isolation.
- Integrated pairing-attempt rate limiting into the receive path, keyed by the sender certificate fingerprint, before a pairing nonce is consumed.
- Pairing invitations remain short-lived and one-time; expired/reused nonces continue to be rejected.

## Security and privacy hardening

- SwiftDrop continues to use established .NET/platform TLS and SHA-256 primitives rather than custom encryption.
- Device certificate/private-key material remains stored through MAUI `SecureStorage` and is never placed into a QR pairing invitation.
- Sender validates the receiver certificate using the out-of-band SHA-256 fingerprint in the pairing invitation.
- Receiver requires a sender certificate and exposes its fingerprint during explicit transfer consent.
- A device display name is not treated as cryptographic identity.
- One-time pairing nonces remain atomic authorization factors and are rejected after expiry or consumption.
- Pairing attempts are now bounded per sender-certificate fingerprint over a rolling time window.
- Metadata protocol frames remain bounded before allocation.
- File length is validated against the configured per-file safety limit.
- Received paths remain constrained under the receive root with traversal/rooted-path rejection.
- Existing destination filenames are resolved to a non-destructive collision-free path.
- Remaining storage capacity is checked before transfer bytes are accepted.
- Transfers continue to stage into `.swiftdrop.part` files and finalize only after SHA-256 verification succeeds.
- Incoming files are never automatically executed or opened.
- Transfer history contains metadata only; file contents are never stored in SQLite.
- Privacy mode hides filenames in newly recorded transfer-history rows.
- No account, SwiftDrop cloud upload service, analytics pipeline, advertising identifier collection, or continuous clipboard monitoring was added.

## Documentation added or expanded

- Added `PRIVACY.md` documenting the current local-first data behavior, local metadata, pairing invitations, network visibility, deletion behavior, and future-feature boundary.
- Added `docs/security/THREAT_MODEL.md` covering assets, goals, passive/active LAN attackers, invitation replay, malicious paths, corruption, dangerous received files, endpoint compromise, denial-of-service limits, metadata privacy, and out-of-scope protections.
- Expanded `docs/protocol/security.md` to document receiver pinning, sender client certificates, explicit consent, one-time authorization, integrity staging, resource limits, and certificate handling.
- Added `docs/testing/manual-test-matrix.md` with Android/iOS/macOS/Windows sender-receiver combinations, pairing, integrity, resume, collision, low-storage, dangerous-file, accessibility, and network-failure cases.
- Added `docs/release/release-checklist.md` covering CI, dependencies, security, privacy, platform validation, transfer matrix, accessibility, packaging, signing, and store declarations.
- Updated `README.md` so its feature list matches the implementation instead of claiming unfinished features as completed.
- Updated `CHANGELOG.md` with the new implementation and hardening work.

## Commits in this continuation

This continuation intentionally used many focused commits rather than one large commit. Commit messages use conventional-style scopes where practical and include:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

The GitHub connector used in this chat does not expose a separate author/committer email field for these write operations, so the requested email is preserved in the Signed-off-by trailer rather than falsely claiming the Git commit metadata itself was forcibly changed.

## Automated and build verification status

- Existing GitHub Actions CI is configured to restore/build `SwiftDrop.Core` and run the portable unit-test project on .NET 10.
- Additional tests were added for transfer-history persistence, trusted-device persistence, file-risk classification, app-settings validation, and attempt rate limiting.
- A GitHub combined-status query during this continuation returned no reported statuses for the queried commit. This is not evidence that tests failed; it means the connector did not return a CI status for that commit at that time.
- This chat environment does not provide the Android SDK/Xcode/Windows MAUI signing environments needed to honestly claim physical-device or store-package validation.
- Android, iOS, Mac Catalyst, and Windows release candidates still require platform-specific builds plus the manual test matrix before a production/store release can be declared verified.

## Prompt requirements not yet fully implemented

The repository is materially more complete after this continuation, but the following master-prompt requirements still need implementation or platform validation before the project should be described as fully production-complete:

- Native mDNS/Bonjour discovery is not yet wired into the MAUI app; the reusable UDP discovery core and QR/manual pairing fallback exist.
- Nearby-discovery UI is not yet a complete first-class device list with lifecycle, deduplication, expiry, and platform permission state.
- A short human-entered one-time pairing-code flow is not yet implemented as a separate UI path; QR/deep-link pairing is implemented.
- Trusted-device persistence primitives and an auto-accept setting exist, but the full trust/revoke/confirm UI and reviewed auto-accept policy are not yet wired end-to-end. Auto-accept remains disabled by default.
- Multi-file send, full folder transfer, explicit text-snippet transfer, share-sheet receive/send integration, desktop drag-and-drop, and user-triggered one-time clipboard paste/send are not yet all implemented.
- Full transfer queue management, configurable concurrency enforcement, pause/resume controls, retry controls, and per-transfer remaining-time/speed UI are not yet complete. The transport supports resumable partial files and sender cancellation.
- The receive destination is currently the application-local `Received` directory rather than a complete cross-platform user-selected destination workflow.
- Background-transfer behavior, notification integration, and foreground-service/background-session behavior still require platform-specific implementation and testing.
- Full mDNS service declarations must be completed when mDNS is wired.
- Persistent trusted-device mutual certificate pinning for future sessions requires the full user-confirmed trust workflow; current fresh-invitation transfers authenticate the receiver by the invitation fingerprint and expose the sender certificate for receiver confirmation.
- Connection-level/IP abuse controls remain an additional hardening area. The current rate limiter is keyed by sender certificate fingerprint, so a hostile actor capable of generating many certificates could distribute attempts across identities.
- Structured privacy-aware diagnostics/log persistence and developer diagnostics controls described in the prompt are not yet complete.
- History-retention settings are stored, but automatic age-based pruning is not yet wired.
- File-risk classification is extension-based warning logic only and must not be marketed as malware detection.
- Platform-supported malware scanning integration is not implemented, and no unsupported safety claim is made.
- Accessibility still requires physical assistive-technology testing even though layouts and documentation now include accessibility expectations.
- UI-flow/integration tests for the MAUI application, database migration-version tests, protocol compatibility matrix tests, network-change tests, low-storage integration tests, and fuzz testing still need expansion.
- Store packaging, release signing, reproducibility validation, third-party notice generation, and release-artifact verification have not been executed in this chat environment.

## Repository notes

- The repository originally contained only the Apache-2.0 `LICENSE`; the initial implementation commit created the main solution, app, core library, tests, assets, platform files, docs, policies, CI, and initial `what_changed.md`.
- Unlike the earlier session note, the SwiftDrop master prompt was successfully available through the File Library in this continuation and was used to check the repository against the requested requirements.
- No signing certificates, API keys, passwords, access tokens, real pairing invitations, or other production secrets were committed.
- Platform SDK/workload availability and runtime behavior still need verification on actual target devices before publication.

## Next implementation boundary

The next highest-value engineering work is to finish nearby discovery and trusted-device UX, then implement multi-item/text/folder transfer and a real queue/concurrency layer, followed by user-selected receive destinations and platform share/drag-drop/background integrations. After that, expand protocol/integration/UI testing and run the documented physical-device release matrix before producing signed store artifacts.

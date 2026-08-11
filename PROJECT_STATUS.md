# SwiftDrop Project Status

Updated: 2026-08-11

## Implemented in source

### Product and transfer foundation

- Apache-2.0, .NET 10, C#, .NET MAUI.
- Android, iOS, Mac Catalyst, and Windows application targets.
- Reusable `SwiftDrop.Core`, portable xUnit tests, and synthetic benchmark project.
- Account-free direct local-network transfers with no SwiftDrop-operated cloud upload/relay path.
- mDNS/DNS-SD discovery plus bounded UDP fallback and peer expiry.
- QR/deep-link pairing, nearby pairing requests, one-time 8-digit codes, and local-IP fallback.
- Single-file, multi-file, recursive-folder, and explicit text transfer.
- Receiver file/batch consent including selective batch acceptance.
- Pause/cancel/fresh-pair resume.
- Stable batch IDs plus verified already-completed-item reuse so an interrupted batch does not resend finalized files under collision-renamed names.
- Configurable queue/concurrency and progress/throughput/ETA UI.
- Windows custom receive folder and native files/folders/text/pair-link drag/drop.
- Android bounded share-sheet intake.
- iOS/Mac Catalyst file URL/document opening.
- Dedicated iOS/Mac Catalyst Share Extension source target using App Group package handoff.
- Native Mac Catalyst `UIDropInteraction` for files, folders, text, and pairing links.
- Warm/cold external-input application handoff.

### Security and privacy

- SecureStorage-backed P-256 ECDSA local identity certificate/private key.
- Certificate validity/renewal/recovery policy and explicit re-pair notice after identity regeneration.
- TLS 1.2/1.3, receiver certificate pinning, sender client certificate, receiver certificate-derived sender fingerprint.
- One-time transfer authorization consumed only after valid request shape and authenticated client certificate are present.
- Separate bounded pairing attempt limiting.
- Trusted-device persistence bound to device ID plus exact canonical SHA-256 certificate fingerprint.
- Strict pairing URI and encoded JSON validation, including duplicate-property, malformed/comment/trailing-comma, **and unknown-member rejection**.
- Strict framed JSON: bounded frame/depth, invalid UTF-8 rejection, no comments/trailing commas, duplicate-property rejection at every depth, unknown-member rejection, truncation/idle-timeout handling.
- Shared typed Core wire records/factories/validators/authorizer used by production sender/receiver/pairing code and tests.
- Manifest-bound sender byte counts, exact receiver byte counts, SHA-256 final verification, and invalid-partial cleanup.
- Portable rooted/traversal checks including Windows-style path syntax on non-Windows hosts.
- Existing receive-root symlink/reparse components rejected around staging/final promotion.
- Atomic concurrent destination reservations and non-overwrite final promotion.
- Portable filename segments explicitly remove both `/` and `\\`, normalize Unicode, neutralize Windows reserved names, and enforce a surrogate-safe 180-character cap even with pathological extensions.
- Android backup disabled for app-local metadata.
- Windows package restricted to private-network capability.
- Privacy-aware history, diagnostics, queue persistence, and bounded external staging.

### Idempotent batch resume

SQLite schema-v3 `completed_batch_items` stores metadata only:

- stable transfer ID;
- source relative path;
- hashed receive-root identity;
- effective destination relative path;
- length/SHA-256;
- completion timestamp.

Before a retry receives a full-length resume offset, SwiftDrop verifies the same transfer/root/source/length/hash, rejects reparse-path destinations, confirms the destination still exists at the expected length, and re-hashes it.

After the sender responds with the matching `BatchItemStart`, SwiftDrop **re-verifies the completed destination again immediately before the zero-byte completion acknowledgement**. Removal, mutation, path redirection, metadata mismatch, or destination mismatch in the planning→ACK window fails closed rather than falsely acknowledging stale bytes. The completion record is invalidated when verification fails so a later retry can transfer safely.

A new explicit send uses a new transfer ID, preserving normal collision-safe duplicate-send semantics.

Resume metadata is best-effort and never authorization; persistence failure does not change the success of an already verified file transfer.

### Cross-platform external intake

- Android content URIs are count/size/capacity bounded, safely named, streamed into cache, exact-length checked when provider metadata exists, cleaned on failure, and handed off atomically.
- Apple Share Extension supports files/images/movies/text/web URLs under explicit activation bounds and publishes validated packages atomically into the App Group.
- Apple provider callbacks must answer within a bounded response window; extension lifetime cancellation breaks pending waits, prevents late callbacks from starting new copies, and is checked during staged file copying. Once a provider responds, a legitimate long copy is governed by extension/user lifetime rather than the provider-response timer.
- Containing Apple app serializes imports, rejects malformed/stale/unmapped/symlinked packages, requires the physical package file set to exactly match the manifest (no undeclared files/directories), and re-stages files into app cache before review.
- Only one pending Apple share bundle is surfaced for review at a time; later packages remain pending rather than being silently merged/deleted over the current user selection.
- Mac Catalyst native drop stages file/folder representations while security-scoped access is valid, rejects symlinks, bounds count/aggregate bytes, deconflicts sanitized directory/file names, and never auto-sends.
- Windows drop uses explicit user-provided paths/text and the same bounded inbox/review path.
- Shared text truncation is rune-safe and bounded by UTF-8 bytes.

### Local metadata

SQLite schema version: **3**.

Tables:

- certificate-bound trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal restart queue metadata;
- verified completed-batch resume metadata.

Transfer bytes/text, private keys, pairing invitations/nonces, receive-root absolute paths, and reusable authorization are not stored in SQLite.

### UI, localization, and architecture

- Main dashboard presentation state uses `MainViewModel`.
- History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About use dedicated view models.
- Pickers/dialogs/native drop/share/lifecycle remain at the platform/UI boundary.
- Networking, TLS, certificates, storage, path policy, protocol, hashing, and authorization remain in services/Core.
- English/Hindi XAML/runtime catalogs cover the implemented UI surfaces.
- CI validates catalog XML, non-empty values, duplicate keys, key parity, and placeholder parity.
- Theme/language/accessibility preferences are applied through app services/resources.

### Testing and hostability

Portable tests cover, among other areas:

- pairing/identity/fingerprint/certificate policy;
- strict pairing JSON including duplicate and unknown members;
- strict framed JSON including unknown members and duplicates;
- typed protocol request factory/shape validation;
- one-time authorization consume/replay behavior;
- complete framed file/batch/text/pair conversation sequencing;
- real mutual-TLS loopback pinning/file/resume tests;
- transfer interruption/source/staging mutation/integrity cleanup;
- stable batch IDs and repeated completed-file revalidation after mutation;
- schema v0/v1/v2→v3 migrations and completion-store behavior;
- receive-root symlink/reparse rejection;
- path/collision/final-promotion races;
- exact external-share package physical file-set validation;
- portable filename separators/extreme extensions/surrogate-safe bounds;
- discovery parser fuzz/truncation/pointer loops/duplicate metadata;
- session-tracker drain/fault/cancellation/race behavior;
- privacy redaction;
- Unicode UTF-8 truncation;
- external share-package manifest boundaries.

Apple `NSItemProvider` callback timing/lifetime behavior itself remains target-platform code and must be validated in Apple builds/runtime tests.

### CI/build/release engineering

- Canonical solution: `SwiftDrop.slnx`.
- Stable C# language mode (`latest`, not preview).
- Portable Core build/test/localization/Apple-metadata validation/benchmark compile.
- Android, Windows, Mac Catalyst, and unsigned iOS Simulator compile workflows configured.
- Apple jobs explicitly compile the Share Extension and containing app.
- Apple metadata validator checks App Group, app/extension IDs, versions, targets, entitlements, sandbox, activation rule, project reference, Core constant, and solution inclusion.
- Release-readiness includes extension dependency inventories for iOS and Mac Catalyst.
- CodeQL, Dependabot, security-hygiene and release-readiness workflows remain configured.
- Release checklist, threat model, security test plan, protocol security docs, privacy docs, and third-party notice process are aligned with the current Apple/schema-v3 source scope.

## Current engineering phase

**Source-complete release-validation phase for the current master-prompt scope, with platform-specific features implemented but not yet physically/signed validated.**

The previously documented Apple Share Extension, Mac Catalyst native drag/drop, typed protocol-hostability, completed-file batch-resume duplication, pairing closed-schema, App Group exact-file-set, and completed-item planning→ACK revalidation gaps are closed in source.

## Remaining source boundaries / deliberate constraints

These are not hidden TODO implementations; they are deliberate platform/release boundaries or optional future enhancements:

1. **Optional completion/failure system notifications outside Android**
   - Android implementation exists.
   - Unsupported targets disable the optional preference instead of pretending notifications exist.
   - Adding native Apple/Windows notifications is optional follow-on work, not part of transfer correctness/security.

2. **Mobile background continuation**
   - SwiftDrop does not claim arbitrary sockets survive OS suspension.
   - Any additional continuation must use store-compliant supported platform mechanisms and be physically validated.

3. **Malware scanning**
   - SwiftDrop provides extension-risk warnings and transport integrity, not a fake cross-platform malware scanner.
   - Platform malware APIs should only be added where a trustworthy supported API exists.

4. **Performance numbers**
   - Synthetic benchmark source exists.
   - Real Wi-Fi/TLS/device throughput/CPU/memory figures require representative hardware.

5. **Final binary third-party notices**
   - Workflow inventories dependencies.
   - Exact licenses/notices must be reviewed against the restored signed-release graph for app, Core, and Share Extension targets.

## External validation still required before production-ready claims

Repository source edits cannot honestly complete these gates:

- observe all current GitHub Actions jobs successfully complete on the exact release candidate;
- signed Android AAB/APK build/install/upgrade;
- signed Windows package build/install/update;
- Apple Developer App Group provisioning for app + Share Extension;
- signed iOS device/TestFlight Share Extension behavior;
- signed Mac Catalyst sandbox/App Group/Share Extension/native-drop behavior;
- physical Android/iOS/macOS/Windows transfer matrix in both directions;
- real guest Wi-Fi/client isolation/multicast/firewall/local-network-permission tests;
- IPv4/IPv6 combinations, network changes, sleep/lock, low storage, multi-gigabyte files, large batches;
- Apple provider timeout/cancellation/large-copy behavior with real Files/iCloud/third-party providers;
- real SecureStorage/keychain/keystore upgrade/restore/locked-device scenarios;
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, high-contrast, and Hindi layout validation;
- final store privacy declarations, screenshots, signing/notarization, dependency-license review, and submission checks.

## Connector/environment limits

- The active chat runtime does not provide the .NET/MAUI workloads required to compile/sign all targets locally.
- GitHub combined-status queries for recent direct-`main` Contents API commits may return no status contexts. Missing contexts are **unknown/unreported**, not a pass.
- Contents API writes do not expose an independent author/committer-email override; commits use `Signed-off-by: Sanskar <sanskarin@outlook.in>`.

See `NEXT_STEPS.md` for release validation priorities and `what_changed.md` for the full engineering ledger.

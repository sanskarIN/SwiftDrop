# SwiftDrop Project Status

Updated: 2026-08-10

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
- **Dedicated iOS/Mac Catalyst Share Extension source target** using App Group package handoff.
- **Native Mac Catalyst `UIDropInteraction`** for files, folders, text, and pairing links.
- Warm/cold external-input application handoff.

### Security and privacy

- SecureStorage-backed P-256 ECDSA local identity certificate/private key.
- Certificate validity/renewal/recovery policy and explicit re-pair notice after identity regeneration.
- TLS 1.2/1.3, receiver certificate pinning, sender client certificate, receiver certificate-derived sender fingerprint.
- One-time transfer authorization consumed only after valid request shape and authenticated client certificate are present.
- Separate bounded pairing attempt limiting.
- Trusted-device persistence bound to device ID plus exact canonical SHA-256 certificate fingerprint.
- Strict pairing URI and encoded JSON validation.
- Strict framed JSON: bounded frame/depth, invalid UTF-8 rejection, no comments/trailing commas, duplicate-property rejection at every depth, **unknown-member rejection**, truncation/idle-timeout handling.
- Shared typed Core wire records/factories/validators/authorizer used by production sender/receiver/pairing code and tests.
- Manifest-bound sender byte counts, exact receiver byte counts, SHA-256 final verification, and invalid-partial cleanup.
- Portable rooted/traversal checks including Windows-style path syntax on non-Windows hosts.
- Existing receive-root symlink/reparse components rejected around staging/final promotion.
- Atomic concurrent destination reservations and non-overwrite final promotion.
- Android backup disabled for app-local metadata.
- Windows package restricted to private-network capability.
- Privacy-aware history, diagnostics, queue persistence, and bounded external staging.

### Idempotent batch resume

Schema-v3 `completed_batch_items` stores metadata only:

- stable transfer ID;
- source relative path;
- hashed receive-root identity;
- effective destination relative path;
- length/SHA-256;
- completion timestamp.

Before a retry receives a full-length resume offset, SwiftDrop verifies the same transfer/root/source/length/hash, rejects reparse-path destinations, confirms the destination still exists at the expected length, and **re-hashes it**. A new explicit send uses a new transfer ID, preserving normal collision-safe duplicate-send semantics.

Resume metadata is best-effort and never authorization; persistence failure does not change file-transfer success.

### Cross-platform external intake

- Android content URIs are count/size/capacity bounded, safely named, streamed into cache, exact-length checked when provider metadata exists, cleaned on failure, and handed off atomically.
- Apple Share Extension supports files/images/movies/text/web URLs under explicit activation bounds and publishes validated packages atomically into the App Group.
- Containing Apple app rejects malformed/stale/unmapped/symlinked packages and re-stages files into app cache before review.
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
- strict JSON including unknown members and duplicates;
- typed protocol request factory/shape validation;
- one-time authorization consume/replay behavior;
- complete framed file/batch/text/pair conversation sequencing;
- real mutual-TLS loopback pinning/file/resume tests;
- transfer interruption/source/staging mutation/integrity cleanup;
- stable batch IDs and completed-file revalidation;
- schema v0/v1/v2→v3 migrations and completion-store behavior;
- receive-root symlink/reparse rejection;
- path/collision/final-promotion races;
- discovery parser fuzz/truncation/pointer loops/duplicate metadata;
- session-tracker drain/fault/cancellation/race behavior;
- privacy redaction;
- Unicode UTF-8 truncation;
- external share-package manifest boundaries.

### CI/build/release engineering

- Canonical solution: `SwiftDrop.slnx`.
- Stable C# language mode (`latest`, not preview).
- Portable Core build/test/localization/Apple-metadata validation/benchmark compile.
- Android, Windows, Mac Catalyst, and unsigned iOS Simulator compile workflows configured.
- Apple jobs explicitly compile the **Share Extension and containing app**.
- Apple metadata validator checks App Group, app/extension IDs, versions, targets, entitlements, sandbox, activation rule, project reference, Core constant, and solution inclusion.
- Release-readiness includes extension dependency inventories for iOS and Mac Catalyst.
- CodeQL, Dependabot, security-hygiene and release-readiness workflows remain configured.

## Current engineering phase

**Source-complete release-validation phase for the current master-prompt scope, with platform-specific features implemented but not yet physically/signed validated.**

The previously documented Apple Share Extension and Mac Catalyst native drag/drop source gaps are now implemented. The previously documented completed-file batch-resume duplication gap is also closed in source.

## Remaining source boundaries / deliberate constraints

These are not hidden TODO implementations; they are deliberate platform/release boundaries or optional future enhancements:

1. **Optional completion/failure system notifications outside Android**
   - Android implementation exists.
   - Unsupported targets currently disable the optional preference instead of pretending notifications exist.
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
   - Exact licenses/notices must be reviewed against the restored signed-release graph.

## External validation still required before production-ready claims

Repository source edits cannot honestly complete these gates:

- observe all current GitHub Actions jobs successfully complete on the release candidate;
- signed Android AAB/APK build/install/upgrade;
- signed Windows package build/install/update;
- Apple Developer App Group provisioning for app + Share Extension;
- signed iOS device/TestFlight Share Extension behavior;
- signed Mac Catalyst sandbox/App Group/Share Extension/native-drop behavior;
- physical Android/iOS/macOS/Windows transfer matrix in both directions;
- real guest Wi-Fi/client isolation/multicast/firewall/local-network-permission tests;
- IPv4/IPv6 combinations, network changes, sleep/lock, low storage, multi-gigabyte files, large batches;
- real SecureStorage/keychain/keystore upgrade/restore/locked-device scenarios;
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, high-contrast, and Hindi layout validation;
- final store privacy declarations, screenshots, signing/notarization, dependency-license review, and submission checks.

## Connector/environment limits

- The active chat runtime does not provide the .NET/MAUI workloads required to compile/sign all targets locally.
- GitHub combined-status queries in this session have returned no status contexts for recent direct-`main` Contents API commits. This is **unknown/unreported**, not a pass.
- Contents API writes do not expose an independent author/committer-email override; commits use `Signed-off-by: Sanskar <sanskarin@outlook.in>`.

See `NEXT_STEPS.md` for release validation priorities and `what_changed.md` for the full engineering ledger.

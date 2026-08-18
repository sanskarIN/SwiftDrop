# SwiftDrop

SwiftDrop is an open-source, account-free local-network file and text transfer app built with .NET MAUI and C#. It is designed for direct peer-to-peer transfers across Android, iOS, macOS (Mac Catalyst), and Windows without uploading transfer content to a SwiftDrop-operated cloud service.

> **Privacy model:** transfer payloads stay on the local peer-to-peer path. SwiftDrop stores only local metadata needed for settings, trust, history, diagnostics, restart-safe queue status/progress, and verified batch-resume state. See `PRIVACY.md`.

## Current source capabilities

### Discovery and pairing

- Internal mDNS/DNS-SD discovery plus bounded UDP fallback.
- QR/deep-link pairing.
- Nearby pairing requests.
- Short-lived one-time 8-digit pairing codes.
- Manual numeric local-IP fallback.
- Strict local/private/link-local/unique-local address policy; public Internet targets and DNS peer names are rejected in protocol v1.
- Strict pairing URI and decoded JSON validation, including duplicate/unknown-property rejection and bounded expiry/lifetime.
- Canonical pairing capability text: no surrounding whitespace, exactly one raw `p=`, no empty/unknown/duplicate query fields, and unpadded Base64URL only.
- Standard Base64 `+`, `/`, `=`, percent-encoded aliases, and non-canonical Base64URL re-encodings are rejected.
- Visual SHA-256 certificate fingerprint confirmation.

### Transport and identity

- Local P-256 ECDSA device certificate/private key stored through platform secure storage.
- TLS server/client certificate EKUs and explicit certificate renewal/recovery policy.
- TLS 1.2/1.3 using .NET/platform cryptography.
- Receiver certificate SHA-256 pinning.
- Sender client certificate required by receiver.
- One-time transfer authorization consumed only after strict request/manifest validation and authenticated client-certificate presence.
- Malformed/noncanonical paths do not burn a valid transfer nonce.
- Certificate-bound trusted-device persistence/revocation.

### Canonical cross-platform file paths

Protocol-v1 file manifest paths have one representation on every platform:

- `/` is the only wire separator;
- rooted/drive/UNC/device paths are rejected;
- repeated/trailing separators, empty segments, `.` and `..` are rejected;
- maximum 64 path segments and bounded overall manifest path text;
- incoming paths must already equal SwiftDrop's canonical sanitized form before authorization;
- filename segments use Unicode NFC, portable invalid-character filtering, Windows reserved-name protection, and are bounded to **180 UTF-16 code units and 180 UTF-8 bytes**;
- collision-generated filenames remain bounded and retain a unique collision marker even when the original filename is already at the limit.

This prevents Windows `\\` vs Unix `/` path identity drift during cross-platform batch negotiation/resume.

### Transfers

- Single files.
- Multiple files.
- Recursive folders where platform source selection permits them.
- Explicit text snippets.
- Explicit clipboard paste only; no continuous clipboard monitoring.
- Receiver accept/reject and batch accept-all/selective/reject decisions.
- Queue/concurrency controls.
- Progress, batch throughput, and ETA presentation.
- Local History performance dashboard with measured completed-transfer duration, resume-safe actual-byte throughput, and weighted average throughput; legacy/unmeasured rows are never given invented rates.
- Local 30-day UTC performance trend derived from completed measured History samples, with an explicit aggregate-only CSV export through the OS share sheet; the export contains UTC date, measured-count, measured-byte, measured-duration, and weighted-rate columns only.
- Restart-safe queue status/progress/item metadata that never serves as reusable transfer authorization.
- Pause/cancel/fresh-pair resume.
- `.swiftdrop.part` staging.
- SHA-256 final integrity verification.
- Storage capacity preflight.
- Collision-safe destinations.
- Non-overwrite final promotion.
- Existing receive-root symlink/reparse components rejected.

Outgoing source safety also rejects symbolic-link/reparse source files/folders. Single-file source status is rechecked at stream open. Recursive folder enumeration is bounded, link-safe, deterministic, and deconflicts portable case/Unicode/sanitation collisions before hashing.

### Optional native terminal notifications

Completion/failure system notifications are implemented as an explicit **off-by-default** preference on Android, iOS, Mac Catalyst, and Windows.

- Android keeps the existing local terminal-notification path and requests Android 13+ notification permission only after opt-in.
- iOS and Mac Catalyst use the Apple User Notifications framework. Alert/sound permission is requested only after opt-in, and a retained notification-center delegate allows enabled generic terminal notifications to be presented while SwiftDrop is foregrounded.
- Windows uses Windows App SDK app notifications. Registration is restored during startup when the preference is already enabled, the activation handler is attached before registration, and shutdown unregisters cleanly.
- Windows packaged notification activation uses matching toast/COM CLSIDs and the Windows App SDK activation argument contract while preserving the local-only `privateNetworkClientServer` capability and not adding `internetClient`.
- English/Hindi completion/failure notification messages are deliberately generic and placeholder-free. They contain no filename, peer name, path, transferred content, pairing data, transfer ID, or reusable authorization.
- Notification permission, registration, delivery, or presentation failure is best-effort and never changes the underlying transfer result.

The signed Apple/Windows/Android runtime still requires the notification permission/system-settings/install/update validation described in the release documents.

### Idempotent batch resume

Interrupted batches retain a stable random transfer ID using bounded ASCII token syntax. The active app batch controls call the stable-ID API directly; the obsolete implicit fresh-ID compatibility overload has been removed.

After each batch item is verified/finalized, SwiftDrop can retain metadata-only completion state in `completed_batch_items`, introduced in SQLite schema v3 and retained in current schema v6.

On retry, an already-finalized item is treated as complete **only after** SwiftDrop confirms:

- same stable transfer ID;
- same canonical sender manifest path;
- same hashed receive-root identity;
- same expected length/SHA-256;
- destination still remains beneath the receive root without symlink/reparse traversal;
- destination still exists at expected length;
- a fresh SHA-256 of that destination still matches.

Only then does the receiver offer a full-length resume offset. After the sender returns that item's matching `BatchItemStart`, SwiftDrop verifies the completed destination **again immediately before the zero-byte item completion acknowledgement**. If the destination changes/disappears in that interval, completed-item reuse fails closed.

A brand-new explicit batch uses a fresh transfer ID, so deliberate duplicate sends continue to use normal collision handling.

Paused single/batch resume state retains only still-existing regular non-link/non-reparse sources, including folder selections where supported.

### Cross-platform external intake

External file staging on Android share, the iOS Share Extension, and Mac native drop uses one reusable count/per-file/aggregate staging-budget policy. Budget is committed only after exact successful staging.

**Android**

- `ACTION_SEND` / `ACTION_SEND_MULTIPLE` for text/files.
- Provider content URIs copied into bounded app cache.
- Shared count/per-file/aggregate staging budget.
- Portable filename sanitation with UTF-8 byte bounds.
- Provider declared-size validation where available.
- Negative provider size treated as unknown.
- Unknown-length input bounded by the remaining aggregate staging budget.
- Repeated storage-reserve checks while streaming unknown-length providers.
- Exact staged length verification and cleanup on failure.
- One atomic review-inbox handoff.
- Foreground data-sync lifetime for active user-initiated transfers.
- Optional generic completion/failure notifications after explicit opt-in.

**iOS**

- Strict `swiftdrop://pair` activation.
- File/document URL opening into bounded cache staging.
- Dedicated **iOS-only SwiftDrop Share Extension** for files/images/movies/text/web URLs.
- App Group `group.in.sanskar.swiftdrop` handoff with strict versioned manifests and atomic package publication.
- Share Extension provider-response timeout plus extension-lifetime cancellation; a response timeout does not incorrectly terminate an already-started valid file copy.
- Aggregate staging budget checked before the file that would exceed the package limit is copied.
- Containing app serializes imports and rejects stale/malformed/unmapped/symlinked packages.
- Physical package `files/` must match the manifest **exactly**: no undeclared files or nested directories.
- Containing app sums validated file bytes and preflights app-cache capacity before recopy begins.
- Imported files are re-staged into app cache before review.
- One pending Apple package is surfaced for review at a time; later pending packages are not silently merged/deleted.
- Shared content is never auto-sent.
- Optional local completion/failure notifications after explicit alert/sound authorization.

**Mac Catalyst desktop**

- Strict `swiftdrop://pair` activation and normal file/document URL intake.
- Native `UIDropInteraction` for files, folders, text, and pairing links.
- Temporary security-scoped access.
- Shared count/per-file/aggregate staging budget.
- Bounded provider-response waits.
- Symlink/reparse rejection, portable collision-safe bounded staging, and review-before-send.
- Optional local completion/failure notifications through the Apple User Notifications framework.
- The maintained Mac Catalyst architecture uses the containing desktop app/native-drop path; there is no Mac Catalyst Share Extension target.

**Windows**

- `swiftdrop` protocol registration/activation.
- Native receive-folder picker.
- Native files/folders/text/pair-link drag-and-drop.
- Private-network client/server package capability.
- Windows local paths are converted into canonical `/` wire manifests before transfer.
- Optional Windows App SDK completion/failure app notifications with packaged activation registration.

### Protocol hardening

Application protocol JSON is strict and typed:

- 4-byte big-endian bounded frame length;
- bounded JSON depth;
- invalid UTF-8/JSON rejected;
- comments/trailing commas rejected;
- duplicate object members rejected case-insensitively at every depth;
- **unknown/unmapped members rejected**;
- type-specific request shapes enforced;
- cross-type field smuggling rejected;
- canonical manifest path validation before authorization;
- canonical transfer-ID token validation;
- truncated frames fail;
- idle timeouts and cancellation enforced.

Production sender, pairing client, receiver, and portable tests use the same Core wire records/factories/validators/authorizer.

## Local metadata and privacy

Current SQLite schema version: **6**.

Metadata tables cover:

- trusted peers;
- transfer history with optional bounded completed-transfer duration and attributable measured-byte metadata;
- bounded diagnostics;
- privacy-minimal restart-safe queue status/progress/item metadata;
- verified completed-batch resume metadata.

Queue metadata can retain a bounded non-secret operation category, update timestamps, progress in basis points, and optional item counts. Stale `Queued`/`Running` rows are marked `Interrupted` after restart and are never automatically replayed.

SQLite does **not** store transferred file bytes, transferred text, private keys, pairing invitations/nonces, reusable session/transfer authorization, queue peer endpoints, queue source/destination paths, source absolute paths, or receive-root absolute paths for resume state.

Privacy mode hides peer/file identifiers in history and redacts common identifiers in diagnostics. Numeric performance metadata follows the same local history-retention policy and contains no peer endpoint, transfer content, credential, or reusable authorization. The optional performance-trend CSV is derived on demand into app cache, contains aggregate UTC buckets only, and is shared only after explicit user action. Persisted queue labels remain generic rather than recording transfer filenames/text.

Optional terminal notification text is also deliberately generic and does not place transfer-specific identifiers/content into OS notification history.

Android application backup is disabled for app-local metadata. Windows requests private-network rather than general Internet client capability.

## UI, MVVM, localization, accessibility

- `MainViewModel` owns primary dashboard presentation state.
- History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About use dedicated view models.
- Queue rows expose operation kind, state, recovered progress/item counts, timing/error context, and a progress bar while persistence remains non-authorizing.
- Active single/batch send controls use regular-source checks and stable resume state.
- Obsolete duplicate batch handlers/fresh-ID compatibility overload have been removed.
- Platform pickers/dialogs/share/drop/lifecycle remain at the UI/platform boundary.
- Networking/TLS/storage/cryptography/protocol/path/integrity policy remains in services/Core.
- English/Hindi XAML and runtime resource catalogs.
- Generic terminal notification status/support/permission messages are localized in English/Hindi.
- CI validates localization XML, duplicate keys, key parity, and placeholder parity.
- Theme, larger-interface, reduce-motion, language, history/privacy/trust, concurrency, diagnostics, notifications, receive location, and identity settings.

## Testing and CI

Portable tests include:

- canonical pairing query/Base64URL/whitespace behavior;
- pairing/identity/certificate/fingerprint policy;
- one-time authorization and replay rejection;
- malformed path rejection before authorization consumption;
- strict/unknown/duplicate JSON member behavior;
- canonical `/` manifest paths, rooted/traversal/empty-segment/depth rejection;
- UTF-8 filename and bounded collision-marker behavior;
- complete framed file/batch/text/pair conversation sequencing;
- mutual-TLS loopback pinning/file/resume behavior;
- transfer interruption/source mutation/staged corruption/integrity cleanup;
- send-boundary source symlink rejection;
- deterministic link-safe folder enumeration;
- portable sender path deconfliction;
- stable batch IDs and verified completed-file reuse;
- second completed-item verification after mutation between retry-plan and ACK checkpoints;
- SQLite v0/v1/v2/v3/v4/v5→v6 migration, queue/history performance metadata, and corruption handling;
- restart interruption with safe queue operation/progress/item-context retention and no persisted reusable authorization;
- traversal/path/collision/symlink/final-promotion race handling;
- shared transfer staging-budget policy;
- exact Apple share-package physical file sets;
- discovery fuzz/truncation/pointer-loop/duplicate metadata;
- mDNS record-RDATA boundary isolation, including rejection of names that would read into a following record;
- exact-expiry behavior for one-time pairing/transfer authorizations and discovered-peer presence;
- concurrent bounded-state admission for rate-limiter peer keys and one-time authorization nonces;
- deterministic seeded reference-model state machines for rate-limiter window/reset/capacity behavior, one-time authorization register/consume/prune/clear behavior, and discovery upsert/expiry/snapshot/clear behavior;
- deterministic seeded reference-model state machines for rate-limiter window/reset/capacity behavior, one-time authorization register/consume/prune/clear behavior, and discovery upsert/expiry/snapshot/clear behavior;
- resume failure paths that reject invalid/missing staged state without creating destination directories or partial files;
- external staging symlink/reparse rejection through the same regular-source safety policy used by direct sends;
- session-drain races;
- privacy redaction;
- UTF-8 rune-safe text truncation.

Configured GitHub Actions include:

- documentation integrity validation;
- **26 Python validation-helper regression tests**, including NuGet evidence helpers, packaged-integration validators, performance-history measurement contracts, and aggregate performance-trend export contracts;
- two-OS portable verification on Ubuntu and Windows PowerShell, currently covering **572 xUnit tests**;
- portable Core build/tests;
- localization validation;
- Apple App Group/iOS Share Extension metadata validation;
- Windows protocol/private-network/app-notification manifest/source consistency validation;
- benchmark-project compile validation;
- Android compile;
- Windows compile;
- Mac Catalyst containing-app compile;
- certificate-independent iOS Simulator Share Extension + containing-app compile;
- CodeQL/security hygiene;
- explicit machine-readable direct/transitive vulnerability-report validation;
- target-specific Android, Windows, Mac Catalyst, iOS app, and iOS Share Extension dependency-audit artifacts;
- deterministic SHA-256 manifests for retained dependency-evidence JSON bundles;
- deterministic SQLite command/resource disposal validated by Windows temp-database cleanup;
- release-readiness aggregate compile/test/audit gates.

The Windows integration validator checks matching notification toast/COM CLSIDs, activation arguments, handler-before-registration/startup-registration source contracts, placeholder-free notification messages, preservation of `privateNetworkClientServer`, and absence of `internetClient`. It validates repository/package metadata consistency; signed MSIX install/update/activation remains a release test.

Successful source compilation is not equivalent to physical-device/store validation.

## Build and test

Canonical solution: `SwiftDrop.slnx`.

```bash
dotnet restore SwiftDrop.slnx
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release
```

Portable verification:

```bash
bash scripts/verify-core.sh
```

Windows PowerShell:

```powershell
./scripts/verify-core.ps1
```

The verification scripts run Python helper tests; validate documentation integrity, localization, Apple integration metadata, and Windows packaged notification integration metadata; compile/test Core and benchmarks; and reject machine-readable Core vulnerability reports containing findings.

See `BUILDING.md` for target-specific build commands and Apple/Windows signed-package requirements.

## Apple provisioning requirement

The source contains matching App Group entitlements for the iOS containing app and iOS Share Extension:

`group.in.sanskar.swiftdrop`

Signed iOS packages still require real Apple Developer configuration/provisioning profiles to include this App Group for:

- app ID `in.sanskar.swiftdrop`;
- extension ID `in.sanskar.swiftdrop.share`.

The Mac Catalyst containing app has its own sandbox/signing/notarization validation path and does not embed a Mac Catalyst Share Extension in the maintained architecture.

Do not claim iOS Share Extension, Apple local notifications, or Mac Catalyst production readiness until signed device/TestFlight/Mac sandbox validation succeeds.

## Networking notes

SwiftDrop works best when both devices are on the same normal LAN/Wi-Fi. Guest networks, AP/client isolation, multicast filtering, enterprise policies, local-network permission denial, mobile background restrictions, notification policy/settings, and host firewalls can block discovery, inbound connections, or optional notification presentation. QR/manual pairing helps discovery failures but does not bypass network or OS policy.

## Repository and support

Repository: https://github.com/sanskarIN/SwiftDrop

GitHub profile: https://www.github.com/sanskarIN

Business/security: **sanskarin@outlook.in**

General support: **supportramsandesh@gmail.com**

Optional development support: https://buymeacoffee.com/sanskarIN

Financial support is optional and does not unlock features, priority security handling, privileged support, or access to private user data.

## Engineering/release documents

- Complete documentation index: `docs/README.md`
- Installation/source run: `docs/installation.md`
- User guide: `docs/user-guide.md`
- Settings reference: `docs/configuration.md`
- FAQ: `docs/faq.md`
- Technical glossary: `docs/glossary.md`
- Troubleshooting: `docs/troubleshooting.md`
- Networking/firewall guide: `docs/networking.md`
- Development guide: `docs/development-guide.md`
- Project structure: `docs/architecture/project-structure.md`
- CI reference: `docs/testing/ci-reference.md`
- Release process: `docs/release/release-process.md`
- Dependency evidence: `docs/release/dependency-evidence.md`
- Versioning/compatibility: `docs/versioning-and-compatibility.md`
- Diagnostics/bug reports: `docs/diagnostics-and-bug-reports.md`
- Build: `BUILDING.md`
- Architecture: `docs/architecture.md`
- Protocol wire format: `docs/protocol/wire-format.md`
- Protocol security: `docs/protocol/security.md`
- Threat model: `docs/security/THREAT_MODEL.md`
- Privacy: `PRIVACY.md`
- Platform status: `docs/platform/integration-status.md`
- Permissions: `docs/platform-permissions.md`
- Local database: `docs/storage/database-schema.md`
- Security tests: `docs/testing/security-test-plan.md`
- Manual tests: `docs/testing/manual-test-matrix.md`
- Release checklist: `docs/release/release-checklist.md`
- Project status: `PROJECT_STATUS.md`
- Next validation steps: `NEXT_STEPS.md`
- Detailed ledger: `what_changed.md`

## Production-status boundary

The current master-prompt scope is implemented in repository source, including the iOS Share Extension, Mac native drop, strict/canonical pairing, cross-platform canonical manifest paths, link-safe deterministic outgoing sources, shared external staging budgets, typed protocol hostability, idempotent completed-file batch resume, restart-safe non-authorizing queue progress metadata, resume-safe local History performance measurements, and optional local terminal notifications on Android/iOS/Mac Catalyst/Windows. Production verification still requires successful current CI runs for the exact candidate, signed packages/the applicable iOS extension, real App Group provisioning, signed notification permission/activation behavior, physical cross-device/provider/network/low-storage/accessibility tests, exact dependency-license review, and store submission checks.

## License

Apache-2.0. See `LICENSE`.

---

**Made by the Sanskar**

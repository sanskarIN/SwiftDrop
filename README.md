# SwiftDrop

SwiftDrop is an open-source, account-free local-network file and text transfer app built with .NET MAUI and C#. It is designed for direct peer-to-peer transfers across Android, iOS, macOS (Mac Catalyst), and Windows without uploading transfer content to a SwiftDrop-operated cloud service.

> **Privacy model:** transfer payloads stay on the local peer-to-peer path. SwiftDrop stores only local metadata needed for settings, trust, history, diagnostics, queue state, and verified batch-resume state. See `PRIVACY.md`.

## Current source capabilities

### Discovery and pairing

- Internal mDNS/DNS-SD discovery plus bounded UDP fallback.
- QR/deep-link pairing.
- Nearby pairing requests.
- Short-lived one-time 8-digit pairing codes.
- Manual numeric local-IP fallback.
- Strict local/private/link-local/unique-local address policy; public Internet targets and DNS peer names are rejected in protocol v1.
- Strict pairing URI and encoded JSON validation, including duplicate-property rejection, **unknown-property rejection**, comments/trailing-comma rejection, and bounded expiry/lifetime.
- Visual SHA-256 certificate fingerprint confirmation.

### Transport and identity

- Local P-256 ECDSA device certificate/private key stored through platform secure storage.
- TLS server/client certificate EKUs and explicit certificate renewal/recovery policy.
- TLS 1.2/1.3 using .NET/platform cryptography.
- Receiver certificate SHA-256 pinning.
- Sender client certificate required by receiver.
- One-time transfer authorization consumed only after request validation and authenticated client certificate presence.
- Certificate-bound trusted-device persistence/revocation.

### Transfers

- Single files.
- Multiple files.
- Recursive folders where platform source selection permits them.
- Explicit text snippets.
- Explicit clipboard paste only; no continuous clipboard monitoring.
- Receiver accept/reject and batch accept-all/selective/reject decisions.
- Queue/concurrency controls.
- Progress, batch throughput, and ETA presentation.
- Pause/cancel/fresh-pair resume.
- `.swiftdrop.part` staging.
- SHA-256 final integrity verification.
- Storage capacity preflight.
- Collision-safe destinations.
- Non-overwrite final promotion.
- Existing receive-root symlink/reparse components rejected.
- Portable filename policy treats both `/` and `\\` as separators, neutralizes reserved names, normalizes Unicode, and enforces a surrogate-safe 180-character segment limit.

### Idempotent batch resume

Interrupted batches retain a stable random transfer ID. After each batch item is verified/finalized, SwiftDrop can retain metadata-only completion state in SQLite schema v3.

On retry, an already-finalized item is treated as complete **only after** SwiftDrop confirms:

- same stable transfer ID;
- same sender manifest path;
- same hashed receive-root identity;
- same expected length/SHA-256;
- destination still remains beneath the receive root without symlink/reparse traversal;
- destination still exists at expected length;
- a fresh SHA-256 of that destination still matches.

That verification occurs once while the retry plan is created **and again after the sender returns the matching `BatchItemStart`, immediately before the zero-byte completion acknowledgement**. If the destination changes, disappears, is redirected, or no longer matches recorded completion metadata in that interval, the shortcut fails closed instead of acknowledging stale bytes.

Only then does the receiver complete the full-length resume path without receiving payload bytes for that item. A brand-new explicit batch uses a fresh transfer ID, so deliberate duplicate sends continue to use normal collision handling.

### Cross-platform external intake

**Android**

- `ACTION_SEND` / `ACTION_SEND_MULTIPLE` for text/files.
- Provider content URIs copied into bounded app cache.
- Portable filename sanitation.
- Provider declared-size validation where available.
- Runtime byte cap when size is unknown.
- Storage-capacity preflight and cleanup on failure.
- One atomic review-inbox handoff.
- Foreground data-sync lifetime for active user-initiated transfers.
- Optional generic completion/failure notifications on Android.

**iOS / Mac Catalyst**

- `swiftdrop://pair` activation.
- File/document URL opening into bounded cache staging.
- Dedicated **SwiftDrop Share Extension** for files/images/movies/text/web URLs.
- App Group `group.in.sanskar.swiftdrop` handoff with strict versioned manifests and atomic package publication.
- Share Extension provider callbacks have a bounded response wait; extension lifetime cancellation cancels pending waits and active staged-copy loops.
- The provider response timeout does not incorrectly terminate a legitimate already-started large local copy; that copy remains governed by extension/user lifetime.
- Containing app rejects stale/malformed/unmapped/symlinked App Group packages.
- The physical App Group `files/` set must exactly match manifest-declared top-level files; undeclared extra files, nested directories, missing files, duplicate portable names, and non-canonical names are rejected.
- Accepted package files are re-staged into app cache.
- One pending Apple share bundle is surfaced for review at a time so later packages cannot silently overwrite/merge the active user selection.
- Shared content is presented for review; it is never auto-sent.

**Mac Catalyst desktop**

- Native `UIDropInteraction` for files, folders, text, and pairing links.
- Temporary security-scoped access.
- Symlink rejection, count/aggregate bounds, collision-safe staging, and review-before-send.

**Windows**

- `swiftdrop` protocol registration/activation.
- Native receive-folder picker.
- Native files/folders/text/pair-link drag-and-drop.
- Private-network client/server package capability.

### Protocol hardening

Protocol JSON is strict and typed:

- 4-byte big-endian bounded frame length;
- bounded JSON depth;
- invalid UTF-8/JSON rejected;
- comments/trailing commas rejected;
- duplicate object members rejected case-insensitively at every depth;
- **unknown/unmapped members rejected**;
- type-specific request shapes enforced;
- cross-type field smuggling rejected;
- truncated frames fail;
- idle timeouts and cancellation enforced.

Pairing payload JSON is also closed-schema: an extra encoded property is rejected rather than silently ignored.

Production sender, pairing client, receiver, and portable tests use the same Core wire records/factories/validators/authorizer.

## Local metadata and privacy

Current SQLite schema version: **3**.

Metadata tables cover:

- trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal queue status;
- verified completed-batch resume metadata.

SQLite does **not** store transferred file bytes, transferred text, private keys, pairing invitations/nonces, source absolute paths, receive-root absolute paths for resume state, or reusable transfer authorization.

Privacy mode hides peer/file identifiers in history and redacts common identifiers in diagnostics.

Android application backup is disabled for app-local metadata. Windows requests private-network rather than general Internet client capability.

## UI, MVVM, localization, accessibility

- `MainViewModel` owns primary dashboard presentation state.
- History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About use dedicated view models.
- Platform pickers/dialogs/share/drop/lifecycle remain at the UI/platform boundary.
- Networking/TLS/storage/cryptography/protocol/path/integrity policy remains in services/Core.
- English/Hindi XAML and runtime resource catalogs.
- CI validates localization XML, duplicate keys, key parity, and placeholder parity.
- Theme, larger-interface, reduce-motion, language, history/privacy/trust, concurrency, diagnostics, notifications, receive location, and identity settings.

## Testing and CI

Portable tests include:

- pairing/identity/certificate/fingerprint policy;
- one-time authorization and replay rejection;
- strict/unknown/duplicate JSON member behavior for framed protocol and pairing payloads;
- complete framed file/batch/text/pair conversation sequencing;
- mutual-TLS loopback pinning/file/resume behavior;
- transfer interruption/source mutation/staged corruption/integrity cleanup;
- stable batch IDs and repeated completed-file verification around retry transitions;
- SQLite v0/v1/v2→v3 migration and corruption handling;
- traversal/path/collision/symlink/final-promotion race handling;
- portable filename separator/long-extension/surrogate-boundary behavior;
- exact Apple share-package physical file-set validation;
- discovery fuzz/truncation/pointer-loop/duplicate metadata;
- session-drain races;
- privacy redaction;
- UTF-8 rune-safe truncation;
- Apple share-package manifest boundaries.

Configured GitHub Actions include:

- portable Core build/tests;
- localization validation;
- Apple App Group/Share Extension metadata validation;
- benchmark-project compile validation;
- Android compile;
- Windows compile;
- Mac Catalyst **Share Extension + containing app** compile;
- unsigned iOS Simulator **Share Extension + containing app** compile;
- CodeQL/security hygiene;
- release-readiness aggregate gates and dependency inventories.

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

The verification scripts also validate localization and Apple integration metadata.

See `BUILDING.md` for target-specific build commands and Apple Share Extension requirements.

## Apple provisioning requirement

The source contains matching App Group entitlements for the containing app and Share Extension:

`group.in.sanskar.swiftdrop`

Signed iOS/Mac Catalyst packages still require the real Apple Developer configuration/provisioning profiles to include this App Group for:

- app ID `in.sanskar.swiftdrop`;
- extension ID `in.sanskar.swiftdrop.share`.

Do not claim Share Extension production readiness until signed device/TestFlight/Mac sandbox validation succeeds, including provider timeout/cancellation and App Group tamper cases.

## Networking notes

SwiftDrop works best when both devices are on the same normal LAN/Wi-Fi. Guest networks, AP/client isolation, multicast filtering, enterprise policies, local-network permission denial, mobile background restrictions, and host firewalls can block discovery or inbound connections. QR/manual pairing helps discovery failures but does not bypass network policy.

## Repository and support

Repository: https://github.com/sanskarIN/SwiftDrop

GitHub profile: https://www.github.com/sanskarIN

Business/security: **sanskarin@outlook.in**

General support: **supportramsandesh@gmail.com**

Optional development support: https://buymeacoffee.com/sanskarIN

Financial support is optional and does not unlock features, priority security handling, privileged support, or access to private user data.

## Engineering/release documents

- Build: `BUILDING.md`
- Architecture: `docs/architecture.md`
- Protocol wire format: `docs/protocol/wire-format.md`
- Protocol security: `docs/protocol/security.md`
- Privacy: `PRIVACY.md`
- Platform status: `docs/platform/integration-status.md`
- Permissions: `docs/platform-permissions.md`
- Local database: `docs/storage/database-schema.md`
- Manual tests: `docs/testing/manual-test-matrix.md`
- Security tests: `docs/testing/security-test-plan.md`
- Release checklist: `docs/release/release-checklist.md`
- Project status: `PROJECT_STATUS.md`
- Next validation steps: `NEXT_STEPS.md`
- Detailed ledger: `what_changed.md`

## Production-status boundary

The current master-prompt scope is implemented in repository source, including Apple Share Extension, Mac native drop, typed protocol hostability, closed-schema pairing/protocol JSON, exact App Group package file sets, and idempotent completed-file batch resume with planning→ACK revalidation. Production verification still requires successful current CI runs, signed packages/extensions, real App Group provisioning, physical cross-device/network/accessibility tests, exact dependency-license review, and store submission checks.

## License

Apache-2.0. See `LICENSE`.

---

**Made by the Sanskar**

# SwiftDrop Project Status

Updated: 2026-08-15

## August 15 local performance-history continuation

- Transfer History has been extended to SQLite schema **v6** with optional bounded `duration_ms` and optional attributable `measured_bytes`. Legacy v4/v5 rows retain null measurement fields rather than receiving synthetic values.
- Completed single-file sender measurements use the actual negotiated bytes sent after resume; receiver single/batch-item measurements use bytes received after their resume offsets. Completed text sends use UTF-8 byte length.
- Throughput is calculated only from completed rows with valid positive measured bytes and duration; impossible samples, failed/unmeasured rows, and zero-byte rows do not contribute.
- The History UI shows localized English/Hindi per-row duration/rate plus a weighted aggregate rate computed from total measured bytes / total measured duration.
- Performance metadata remains local and retention-bound, and adds no peer endpoint, transfer content, pairing capability/nonce, credential, certificate/private key, or reusable authorization.
- Physical representative-device/network performance characterization remains external validation and is not inferred from hosted CI.

- Final portable evidence is **539/539 xUnit tests** plus **21/21 Python helper tests**. Clean-main CI run `31874085156` passed Ubuntu and Windows portable verification; CodeQL `31874085174` and security-hygiene `31874085159` passed.
- Exact runtime platform run `31873777639` passed Android, focused Windows, Mac Catalyst, iOS Share Extension, iOS containing app, dependency audits, and artifact uploads on runtime head `0b288cf897b11431aadfb3aadcc05cb6508f2908`.
- Release-readiness run `31874019607` passed Core/tests, Android, Windows, Apple targets, dependency evidence, and the final aggregate `release-gate`.
## August 15 final native-notification verification snapshot

- Exact runtime notification source head: `c3bd4d9fd5389a56fd203a5e4edb31033631181a`.
- Exact-source platform run `31870987664` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator containing app, target vulnerability audits, and dependency-evidence uploads.
- Portable run `31871039534` passed both Ubuntu and Windows with **16 Python validation-helper tests**, the Windows package-notification validator, **522/522 xUnit tests**, benchmark compilation, documentation/localization/Apple metadata checks, and zero reported Core vulnerable-package findings.
- Clean documentation/source-state CI `31871539203`, CodeQL `31871539201`, and security-hygiene `31871539219` all passed after the notification documentation/helper cleanup.
- Release-readiness self-test `31871195203` exposed a real workflow-only Apple restore defect: the Mac Catalyst app restore did not explicitly restore `SwiftDrop.Core` for the selected Mac Catalyst RID. Runtime notification source was not implicated.
- Commit `594e586dcda99d75b4d79da0ce9362813e28d4f5` aligned release readiness with the maintained platform workflow by restoring shared Core for Mac Catalyst and iOS simulator RIDs. Corrected release-readiness run `31871378565` passed Core/tests, Android, Windows, Mac Catalyst, iOS Share Extension, iOS containing app, dependency audits/uploads, and the final aggregate `release-gate`.
- Corrected release evidence artifacts include `dependency-audit`, `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit` with GitHub-recorded SHA-256 artifact digests.
- Final issue/PR queue check found no open GitHub issues and no open pull requests.
- Signed Android/iOS/Mac Catalyst/Windows notification permission, registration, presentation/activation, install/update, system-settings, accessibility/localization, and store-policy behavior remains a release-validation requirement rather than a hosted-source claim.

## August 15 native terminal notification continuation

- Optional completion/failure system notifications are implemented in source across Android, iOS, Mac Catalyst, and Windows and remain off by default.
- Apple uses local User Notifications alert/sound authorization after explicit opt-in, retains a notification-center delegate for foreground presentation, and does not introduce remote-push registration or a cloud notification service.
- Windows uses Windows App SDK app notifications with matching packaged toast/COM activation metadata, startup registration for an already-enabled preference, handler-before-register ordering, and deterministic unregister on shutdown.
- Notification bodies are generic English/Hindi catalog strings without format placeholders or transfer-specific identifiers/content.
- Settings guidance and permission-denial messages are localized and platform-neutral.
- `scripts/validate_windows_integration.py` protects Windows protocol/private-network/notification package and source invariants; six dedicated validator tests raise the Python helper suite to 16 tests.
- Bash, PowerShell, normal CI, and release readiness execute the Windows integration validator.
- Signed-device/package notification authorization, delivery/presentation, Windows COM activation, system notification settings, and install/update behavior remain release-validation gates.

## August 15 final queue-v4 verification snapshot

- Exact runtime/source-changing head is `67fc3feaa506b16d11307afa9da8ca9d151f6d22`. The final source fix prevents caller cancellation from permanently disabling restart-safe queue persistence and sanitizes persisted exception-type error codes to the bounded machine-code contract.
- SQLite schema remains **v4** with privacy-minimal operation/update/progress/item queue metadata. Stale active rows become `Interrupted` after restart, retain only safe context, and are never automatically replayed.
- The portable xUnit contract is now **522/522 passing tests**. The current documentation/source-state CI run `31867674137` passed both Ubuntu and the Windows PowerShell verifier.
- CodeQL run `31867674094` and security-hygiene run `31867674078` completed successfully on the current documentation/source state.
- Exact-source platform run `31867418650` completed successfully across Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing app, including the maintained target dependency/vulnerability audits and evidence uploads.
- README, architecture, storage, privacy, compatibility, manual/security testing, release checklist/process, roadmap/status, changelog, and engineering-ledger references are aligned to schema v4 and the 522-test current contract.
- The final open-issue check returned no open GitHub issues.
- Production readiness still requires the documented signed package, Apple provisioning/App Group, physical device/provider/network/filesystem/storage, accessibility/localization, exact signed-artifact dependency/license/provenance, notarization, and store/privacy gates.

## August 15 restart-safe queue persistence continuation

- SQLite `CurrentVersion` is now **4**. The v3→v4 migration extends `transfer_queue_metadata` with bounded operation category, update timestamp, basis-point progress, and optional total/completed item counts while preserving legacy rows with safe defaults.
- Queue persistence remains privacy-minimal and deliberately non-authorizing: database labels are always generic `Transfer`; no transfer content, source/destination path, peer endpoint, pairing invitation/nonce, token/session authorization, certificate/private key, or reusable credential is persisted in the queue table.
- File, batch, and text sending now report operation/progress/item context into `TransferQueueService`. Ordinary progress writes are coarsened to 5% buckets, while state and item-count changes are persisted immediately through the serialized best-effort metadata path.
- Stale `Queued`/`Running` rows are marked `Interrupted` after restart while retaining last-known safe progress/context. They are never auto-replayed and a new transfer attempt still requires fresh pairing/authorization.
- Queue UI rows now show operation category, progress percentage/item counts, and a progress bar in addition to state/timing/error information.
- Core storage tests now cover rich metadata round trips, interrupted-progress preservation, invalid progress/item relationships, schema-v4 columns/defaults, legacy v3 migration, and the deliberate absence of nonce/token/certificate/host/port field classes from the queue schema.
- `docs/storage/database-schema.md`, `PRIVACY.md`, versioning/compatibility policy, changelog, roadmap, project status, and the engineering ledger are being synchronized to schema v4 in this continuation.
- The pre-change hosted baseline remained 517/517 portable tests on Ubuntu and Windows plus a green Android/Windows/Mac Catalyst/iOS platform matrix. August 15 source-changing runs must be evaluated separately on their exact head; older green evidence is not treated as proof for the new code.
- The repository still does not claim production readiness from hosted evidence alone. Signed Android/Windows/Apple packaging, physical device/network/provider/accessibility validation, exact final package dependency/license/provenance reconciliation, App Group/notarization, and store/privacy checks remain required.

## August 14 Windows/SQLite resource-lifetime and final hosted matrix continuation

- Normal CI now has a dedicated `windows-portable-verifier` job executing `scripts/verify-core.ps1`; the Windows path is an enforced contract rather than an unexecuted helper script.
- The first Windows verifier exposed a PowerShell interpolation parser defect (`$LASTEXITCODE:`), fixed in signed commit `080126a0` without weakening the gate.
- Subsequent Windows execution exposed SQLite database-file locks that Linux had not revealed. Test teardown now clears Microsoft.Data.Sqlite pools before deleting isolated temp DB/`-wal`/`-shm` files, and schema tests dispose connections before cleanup.
- The investigation found a real production resource-lifetime defect: SQLite command objects were not deterministically disposed. `DatabaseSchemaManager`, `BatchCompletionStore`, `DiagnosticEventStore`, `TransferHistoryStore`, `TransferQueueMetadataStore`, and `TrustStore` now dispose commands explicitly; schema transactions/readers remain scoped as well.
- Added a direct pooled SQLite cleanup regression. The portable xUnit suite is now **517 tests**.
- Exact source-head CI run `31785808946` passed the complete 517-test contract on both Ubuntu and Windows, including 10 Python helper tests, documentation/localization/Apple metadata validation, Core/benchmark builds, and zero-finding machine-readable Core vulnerability validation.
- Source-head CodeQL run `31785808918` and security-hygiene run `31785808999` passed after the storage resource-lifetime fixes.
- Platform run `31786513898` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator containing app, all target dependency audits, and audit-artifact uploads using the current source plus the maintained platform workflow.
- Same-ref concurrency controls were added to platform CI, core CI, CodeQL, and security hygiene so rapid focused commits keep the newest branch evidence instead of allowing superseded runs to block hosted capacity.
- Current-main CI run `31786693757` passed both Ubuntu and Windows portable jobs with **517/517** xUnit tests; CodeQL run `31786693816` also passed on the same documentation/workflow state before this final status synchronization.
- The repository still does not claim production readiness from hosted evidence alone. Signed Android/Windows/Apple packaging, physical device/network/provider/accessibility validation, exact final package dependency/license/provenance reconciliation, App Group/notarization, and store/privacy checks remain required.

## August 14 release-evidence, verifier, and adversarial-test continuation

- Added `scripts/validate_nuget_vulnerability_report.py` plus regression tests so a machine-readable NuGet report is not treated as clean merely because it is valid JSON; non-empty vulnerability collections now fail explicitly.
- Added `scripts/create_dependency_evidence_manifest.py` plus regression tests; audit bundles now contain a deterministic schema-v1 manifest of report paths, exact byte lengths, and SHA-256 digests.
- Normal CI pins Python 3.13, runs 10 Python helper tests, validates documentation/localization/Apple metadata, builds Core, runs the portable xUnit suite, builds benchmarks, and validates the Core vulnerable-package report.
- Local Bash and PowerShell verification run the same helper/documentation/Core/audit gates. A dedicated Windows CI job now executes the PowerShell verifier so Windows-only parser/native-exit behavior is continuously exercised.
- The first Windows-verifier execution exposed a PowerShell parser bug in `$LASTEXITCODE:` interpolation. Commit `080126a0` fixes it by explicitly delimiting the variable; the gate was kept rather than weakened.
- Added deterministic randomized pairing canonicalization and strict-JSON fuzz/property regression tests. CI run `31784196373` passed **516/516** xUnit tests, 10 Python helper tests, documentation/localization/Apple validators, Core/benchmark builds, and a zero-finding machine-readable Core vulnerability audit.
- Platform run `31783405975` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app builds; each target graph produced/validated vulnerable-package JSON and uploaded hashed dependency evidence.
- The retained platform artifacts are `android-dependency-audit`, `windows-dependency-audit`, and `apple-dependency-audit`. Their internal manifests were independently recomputed after download; all listed report byte lengths and SHA-256 digests matched. The Apple manifest covers six reports across Mac Catalyst, iOS app, and iOS Share Extension graphs.
- Release-readiness self-test run `31783537853` passed portable verification, Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator app, target dependency-audit uploads, and the final aggregate `release-gate`.
- Release-readiness now also self-tests on `main`/pull-request changes to its verification/audit/evidence helpers while all `v*` tag pushes remain release-candidate triggers.
- Added canonical `docs/release/dependency-evidence.md`; release process/checklist, CI/build documentation, docs index, and third-party notices now define stable JSON output version 1, exact artifact names, vulnerability validation, evidence manifests, and final signed-artifact comparison requirements.
- These improvements strengthen reproducible source/restored-graph evidence. They still do not replace real signing, final package dependency/provenance/license review, physical device/network/provider/accessibility testing, Apple App Group/notarization, signed Windows MSIX behavior, or store/privacy checks.


## August 14 continuation hardening snapshot

- Maintained GitHub Actions use `actions/checkout@v7`, `actions/setup-dotnet@v6`, and `github/codeql-action@v4`.
- Repository-wide NuGet auditing is explicitly enabled for direct/transitive dependencies at low-or-higher severity, with warnings-as-errors retaining audit findings as blockers.
- Release readiness emits machine-readable full/vulnerable dependency JSON evidence; portable CI continuously validates the JSON vulnerability-report command.
- The .NET 10 test toolchain uses `Microsoft.NET.Test.Sdk` 18.8.1, `xunit.runner.visualstudio` 3.1.5, and `coverlet.collector` 10.0.1; 511/511 tests passed after the update.
- Platform run `31773145276` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app compilation after the action/audit hardening.
- Signed/package/device/network/accessibility/store validation remains required exactly as described below.

## August 14 documentation enforcement and dependency completion

- `scripts/validate_documentation.py` now makes the canonical documentation surface testable: required documents must exist and be nonempty, principal guides must be indexed, checked local Markdown links must resolve, and completed one-time documentation helpers must not remain.
- Documentation validation runs in regular CI and both local `verify-core` entry points; release-readiness uses the same canonical portable verification path.
- CI run `31778543950` proved the new documentation gate together with localization, Apple metadata validation, Core build, 511/511 tests, benchmark compilation, and machine-readable vulnerability auditing; run `31778749428` revalidated the integrated build documentation state.
- Added a technical glossary and aligned pull-request, bug-report, feature-request, and issue-contact routing with the maintained security/privacy/compatibility/release evidence rules.
- `QRCoder` is updated from 1.6.0 to 1.8.0 and `THIRD_PARTY_NOTICES.md` matches the direct dependency version.
- QRCoder update evidence: CI `31778661754`, CodeQL `31778661766`, security hygiene `31778661731`, and platform matrix `31778661776` all succeeded; Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing-app compilation are green.
- Dependabot PR #9 was closed after the equivalent signed update was applied directly to `main`; no open pull requests or issues remained at the completion check.
- These source/hosted checks do not replace signed Android/Windows/Apple packaging, physical-device/network/provider/accessibility validation, Apple provisioning/notarization, exact release dependency/license review, or store/privacy submission checks.

## August 14 documentation completion snapshot

- Added a canonical `docs/README.md` index covering user, developer, architecture, protocol/security, platform, storage, testing, release, support, and legal documentation.
- Added complete user-facing guides for installation/source-run boundaries, pairing/sending/receiving/resume, settings/defaults, networking/firewalls, FAQ, troubleshooting, and privacy-safe diagnostics/bug reports.
- Added developer/repository documentation for project structure, development workflow, CI evidence interpretation, versioning/compatibility, and the end-to-end signed release process.
- Expanded `CONTRIBUTING.md`, `SUPPORT.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, and `TERMS.md` so community, support, security-disclosure, and source-vs-release boundaries are explicit.
- Public README navigation now links the complete documentation surface; documentation maintenance rules identify the canonical owner for user/settings/protocol/storage/platform/testing/release changes.
- The documentation pass does not change the existing production-ready rule: signed/device/network/provider/accessibility/store validation remains required for an exact release candidate.

## Implemented in source

### Product and transfer foundation

- Apache-2.0, .NET 10, C#, .NET MAUI.
- Android, iOS, Mac Catalyst, and Windows application targets.
- `Microsoft.Maui.Controls` 10.0.90 servicing package on the application project.
- Reusable `SwiftDrop.Core`, portable xUnit tests, and synthetic benchmark project.
- Account-free direct local-network transfers with no SwiftDrop-operated cloud upload/relay path.
- mDNS/DNS-SD discovery plus bounded UDP fallback and peer expiry.
- QR/deep-link pairing, nearby pairing requests, one-time 8-digit codes, and local-IP fallback.
- Single-file, multi-file, recursive-folder, and explicit text transfer.
- Receiver file/batch consent including selective batch acceptance.
- Pause/cancel/fresh-pair resume.
- Stable batch IDs plus verified already-completed-item reuse so an interrupted batch does not resend finalized files under collision-renamed names.
- Configurable queue/concurrency plus restart-safe status/progress/item metadata; persisted queue context is never reusable authorization.
- Optional generic completion/failure system notifications on Android, iOS, Mac Catalyst, and Windows.
- Windows custom receive folder and native files/folders/text/pair-link drag/drop.
- Android bounded share-sheet intake.
- iOS/Mac Catalyst file URL/document opening.
- Dedicated **iOS-only** Share Extension source target using App Group package handoff.
- Native Mac Catalyst `UIDropInteraction` for files, folders, text, and pairing links; Mac Catalyst does not use a Share Extension target in the maintained architecture.
- Warm/cold external-input application handoff.

### Canonical pairing and protocol representation

Pairing capability text is accepted in one strict representation:

- no leading/trailing whitespace;
- exact `swiftdrop://pair` URI structure;
- exactly one raw `p=` query field;
- no empty/unknown/duplicate query fields;
- unpadded canonical Base64URL payload text only;
- standard Base64 `+`, `/`, `=` and percent-encoded aliases rejected;
- decoded pairing JSON rejects duplicate and unknown members;
- protocol version, local numeric address, port, fingerprint, nonce, identity fields, and lifetime remain bounded/validated.

Application framed JSON remains closed-schema and typed:

- bounded frame length/depth;
- strict UTF-8 decode before JSON parsing;
- no comments/trailing commas;
- case-insensitive duplicate-member rejection;
- unknown/unmapped-member rejection;
- type-specific request-shape validation;
- cross-type field-smuggling rejection.

### Canonical cross-platform manifest paths

Protocol-v1 file manifest paths have one operating-system-independent identity:

- `/` is the only wire separator;
- incoming backslash aliases are rejected rather than silently normalized after authorization;
- rooted/drive/UNC/device paths rejected;
- empty/repeated/trailing separators rejected;
- `.`/`..` traversal rejected;
- maximum 64 segments;
- bounded total relative-path metadata;
- every incoming manifest path must already equal SwiftDrop's canonical sanitized form before one-time authorization is consumed.

Canonical filename segment policy includes:

- Unicode NFC;
- portable invalid/control-character removal during sender/local source construction;
- Windows reserved-device-name neutralization;
- trailing dot/space handling;
- canonical post-filter whitespace handling;
- 180 UTF-16 code-unit bound;
- 180 UTF-8 byte bound without splitting Unicode scalars;
- headroom for `.swiftdrop.part` on common byte-limited filesystems;
- collision-generated names whose uniqueness marker survives truncation at maximum length.

Windows sender folder paths are therefore advertised with `/` and match Android/iOS/Mac receiver-plan paths exactly.

### Outgoing source safety and deterministic folder manifests

Single-file sends:

- require a regular non-link/non-reparse source;
- repeat regular-source validation at the stream-open boundary;
- bind bytes to manifest-declared length;
- fail if size changes before/during streaming.

Folder/multi source construction:

- rejects selected symlink/reparse folder roots;
- rejects symlink/reparse files/directories anywhere below the selected root;
- uses explicit bounded traversal instead of unrestricted recursive enumeration;
- bounds file and directory traversal;
- sorts resulting source files deterministically by normalized relative path;
- constructs canonical `/` manifest paths;
- deconflicts case-only, Unicode-normalization, and sanitation-equivalent portable destination paths before hashing;
- preflights count/per-file/aggregate/path-length constraints before expensive hashing where knowable;
- uses regular-source validation again for pending files.

Paused source retention:

- preserves valid selected files and folders;
- uses platform-aware local path deduplication;
- drops a source that becomes missing or a symlink/reparse point before resume;
- single-file resume also validates a regular source before consuming the fresh pairing capability.

The old duplicate MainPage batch handlers and implicit fresh-ID coordinator compatibility overload have been removed. The active XAML batch controls call the stable-transfer-ID workflow only.

### Security and privacy

- SecureStorage-backed P-256 ECDSA local identity certificate/private key.
- Certificate validity/renewal/recovery policy and explicit re-pair notice after identity regeneration.
- TLS 1.2/1.3, receiver certificate pinning, sender client certificate, receiver certificate-derived sender fingerprint.
- One-time transfer authorization consumed only after strict request/manifest validation and authenticated client certificate are present.
- Malformed/noncanonical paths do not consume valid authorization.
- Separate bounded pairing attempt limiting.
- Trusted-device persistence bound to device ID plus exact canonical SHA-256 certificate fingerprint.
- Strict pairing URI/encoded JSON representation.
- Shared typed Core wire records/factories/validators/authorizer used by production sender/receiver/pairing code and tests.
- Manifest-bound sender byte counts, exact receiver byte counts, SHA-256 final verification, and invalid-partial cleanup.
- Portable rooted/traversal checks including Windows-style path syntax on non-Windows hosts.
- Existing receive-root symlink/reparse components rejected around staging/final promotion.
- Atomic concurrent destination reservations and non-overwrite final promotion.
- Collision candidates remain bounded/distinct even when original filename is at character/byte limit.
- Optional last-write timestamp application after verified promotion is best-effort and cannot turn verified content into a false transfer failure.
- Android backup disabled for app-local metadata.
- Windows packaged release design remains private-network-only; hosted compile validation is deliberately unpackaged and does not replace signed MSIX capability/package validation.
- Privacy-aware history, diagnostics, queue persistence, and bounded external staging.

### Idempotent batch resume

Current SQLite schema is v4; `completed_batch_items` was introduced in schema v3 and continues to store metadata only:

- stable transfer ID;
- canonical source relative path;
- hashed receive-root identity;
- effective local destination relative path;
- length/SHA-256;
- completion timestamp.

Before a retry receives a full-length resume offset, SwiftDrop verifies the same transfer/root/source/length/hash, rejects reparse-path destinations, confirms the destination still exists at the expected length, and re-hashes it.

After the sender returns the matching `BatchItemStart`, SwiftDrop performs the completed-file verification **again immediately before sending the zero-byte completion ACK**. A destination changed/deleted/replaced between plan creation and acknowledgement therefore fails closed and cannot be falsely reported complete.

A new explicit send uses a new transfer ID, preserving normal collision-safe duplicate-send semantics. Stable retry IDs use bounded ASCII token syntax (letters, digits, `-`, `_`).

Resume metadata is best-effort and never authorization; persistence failure does not change file-transfer success.

### Cross-platform external intake and staging budgets

Shared Core `TransferStagingBudget` provides one source of truth for:

- maximum staged file count;
- maximum single-file bytes;
- aggregate staged bytes;
- commit-after-success semantics so a failed copy does not consume budget.

Android:

- content URI count/per-file/aggregate bounds;
- portable/UTF-8-bounded filename sanitation;
- declared-size validation where available;
- negative provider size treated as unknown;
- unknown-length runtime byte cap based on remaining aggregate budget;
- repeated storage-reserve checks during unknown-length streaming;
- exact staged-length verification;
- cleanup of failed files/directories;
- current Android intent/openable-column binding handling;
- foreground data-sync service and guarded multicast-lock behavior;
- atomic inbox handoff.

iOS Share Extension:

- **iOS-only `net10.0-ios` target**;
- files/images/movies/text/web URLs under explicit activation bounds;
- bounded provider-response waits plus extension-lifetime cancellation;
- late timed-out/cancelled callbacks cannot begin a new copy;
- provider-response timeout does not cancel an already-started legitimate copy;
- aggregate staging budget checked before copying an over-limit file;
- security-scoped access where provided;
- bounded collision-safe filenames;
- strict App Group package manifest and atomic `.staging-*` → `pending-*` publication;
- never auto-sends;
- real App Group entitlements remain in source for signed/device builds;
- hosted iOS Simulator compile commands clear signing/provisioning requirements only at CI command scope.

Containing Apple app:

- serialized App Group import;
- strict JSON/unknown-member/package-age/version/ID validation;
- package/manifest/files/file symlink/reparse rejection;
- exact physical file-set validation: undeclared extra files or nested directories are rejected;
- exact file-length checks;
- aggregate validated package bytes preflighted against app-cache capacity before recopy;
- re-stages accepted files into app cache before review;
- surfaces one pending package for review at a time rather than silently merging later packages.

Mac Catalyst native drop:

- files/folders/text/pairing links;
- temporary security-scoped access;
- shared count/per-file/aggregate staging budget;
- bounded provider-response waits;
- symlink/reparse rejection;
- portable UTF-8-bounded collision handling;
- never auto-sends;
- implemented through the containing desktop app rather than a Mac Catalyst Share Extension.

Windows native drop:

- explicit files/folders/text/pairing-link input;
- atomic common inbox handoff;
- direct file/folder sources still pass the shared regular-source/link-safe/canonical-manifest pipeline before transfer;
- WinUI activation/drag event types and WinRT data-package operations are explicitly qualified to avoid namespace ambiguity.

### Local metadata

SQLite schema version: **4**.

Tables:

- certificate-bound trusted peers;
- transfer history;
- bounded diagnostics;
- privacy-minimal restart queue state/progress metadata;
- verified completed-batch resume metadata.

Queue metadata can retain a non-secret operation category, bounded progress, item counts, and timestamps. Transfer bytes/text, private keys, pairing invitations/nonces, peer endpoints, source/destination paths, receive-root absolute paths, reusable session/transfer tokens, and reusable authorization are not stored in the queue table.

### UI, localization, and architecture

- Main dashboard presentation state uses `MainViewModel`.
- History, Queue, Nearby Devices, Trusted Devices, Diagnostics, Settings, and About use dedicated view models.
- Queue presentation exposes operation kind, recovered progress/item counts, state/timing/error context, and a progress bar while keeping persisted labels generic.
- Active single-file controls validate regular source state before send/resume.
- Active batch controls use the dedicated stable-ID partial workflow.
- Multi-file picker treats a cancelled/null platform result as an empty selection under the MAUI 10.0.90 API contract.
- Obsolete duplicate batch handlers/fresh-ID compatibility overload removed.
- Unsupported `Entry.LineBreakMode` XAML usage removed.
- `LocalizeExtension` is explicitly marked service-provider independent for XAML compilation.
- Pickers/dialogs/native drop/share/lifecycle remain at the platform/UI boundary.
- Networking, TLS, certificates, storage, path policy, protocol, hashing, source safety, and authorization remain in services/Core.
- English/Hindi XAML/runtime catalogs cover the implemented UI surfaces.
- CI validates catalog XML, non-empty values, duplicate keys, key parity, and placeholder parity.
- Theme/language/accessibility preferences are applied through app services/resources.

### Testing and hostability

Portable tests cover, among other areas:

- strict/canonical pairing query/Base64URL/whitespace handling;
- pairing/identity/fingerprint/certificate policy;
- strict JSON including unknown members and duplicates;
- typed protocol request factory/shape validation;
- canonical manifest paths, rooted/traversal/empty-segment/depth/noncanonical rejection;
- malformed path rejection before one-time authorization consumption;
- canonical transfer-ID token syntax;
- one-time authorization consume/replay behavior;
- complete framed file/batch/text/pair conversation sequencing;
- real mutual-TLS loopback pinning/file/resume tests;
- transfer interruption/source/staging mutation/integrity cleanup;
- regular-source/symlink rejection at source-builder and send-engine boundaries;
- deterministic bounded link-safe folder enumeration;
- portable sender collision deconfliction;
- UTF-8 filename byte caps and collision marker preservation;
- stable batch IDs and completed-file revalidation including mutation between repeated verification passes;
- schema v0/v1/v2/v3→v4 migration and queue/completion-store behavior;
- restart interruption preserving safe queue progress/context;
- queue metadata validation and no reusable authorization/endpoint fields;
- receive-root symlink/reparse rejection;
- path/collision/final-promotion races;
- reusable staging count/per-file/aggregate budgets;
- exact external share-package physical file sets;
- discovery parser fuzz/truncation/pointer loops/duplicate metadata;
- session-tracker drain/fault/cancellation/race behavior;
- privacy redaction;
- Unicode UTF-8 text truncation.

Portable verification evidence for the August 14 source baseline:

- Core restore/build succeeded in Release configuration;
- **517/517 portable tests passed**;
- synthetic benchmark project compiled;
- localization validation passed;
- Apple integration metadata validation passed;
- CodeQL passed on the MAUI 10.0.90/picker-contract source head;
- repository security-hygiene checks passed on that source head.

The August 15 queue-v4 source is subject to new exact-head CI/platform/security evidence; the earlier August 14 passes are retained as historical baseline evidence rather than being relabeled as validation of new code.

### CI/build/release engineering

- Canonical solution: `SwiftDrop.slnx`.
- Stable C# language mode (`latest`, not preview).
- `Microsoft.Maui.Controls` serviced to 10.0.90.
- Portable Core build/test/localization/Apple-metadata validation/benchmark compile configured.
- Maintained platform workflow covers Android, focused Windows, Mac Catalyst, and certificate-independent iOS Simulator app/extension compilation.
- Android Release compile is green on the August 14 MAUI 10.0.90/picker-contract source baseline.
- Focused Windows Release compile is green on that baseline with `WindowsPackageType=None`/`GenerateAppxPackageOnBuild=false`, producing the Windows application assembly with 0 errors.
- Windows CI uses `SwiftDropTargetFrameworksOverride` plus `SkipIosShareExtensionProjectReference` only for focused Windows validation so it does not traverse unrelated mobile workloads.
- Signed MSIX creation/install/update remains a separate release gate and is not represented by the unpackaged compile job.
- Apple jobs compile the Mac Catalyst containing app plus the iOS Simulator Share Extension and iOS containing app; the August 14 maintained MAUI 10.0.90 platform baseline is fully green for Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing app compilation.
- Apple metadata validator checks App Group, app/extension IDs, versions, iOS extension target, entitlements, Mac sandbox, activation rule, project reference, Core constant, and solution inclusion.
- Release-readiness captures the iOS Share Extension dependency inventory and mirrors the maintained platform compile boundaries.
- Obsolete one-time self-edit workflows and the duplicate stale platform smoke workflow were removed.
- CodeQL, Dependabot, security-hygiene and release-readiness workflows remain configured.

## Current engineering phase

**Source-complete release-validation phase for the current master-prompt scope, with optional post-v1 queue persistence now implemented in source.**

The repository source contains the local-transfer product scope, iOS Share Extension, Mac native drop, schema-v3 idempotent completed-batch resume within current schema v4, restart-safe non-authorizing queue progress metadata, canonical pairing/path representation, source-link safety, deterministic folder enumeration, external staging budgets, bounded filename/collision behavior, stable-ID-only active batch path, repeated completed-file verification, Android lifecycle/share hardening, and repaired platform compile infrastructure.

## Remaining source boundaries / deliberate constraints

These are deliberate platform/release boundaries or optional future enhancements rather than hidden TODO implementations:

1. **Mobile background continuation**
   - SwiftDrop does not claim arbitrary sockets survive OS suspension.
   - Any additional continuation must use store-compliant supported platform mechanisms and be physically validated.

2. **Malware scanning**
   - SwiftDrop provides extension-risk warnings and transport integrity, not a fake cross-platform malware scanner.
   - Platform malware APIs should only be added where a trustworthy supported API exists.

3. **Performance numbers**
   - Synthetic benchmark source exists.
   - Real Wi-Fi/TLS/device throughput/CPU/memory figures require representative hardware.

4. **Final binary third-party notices**
   - Workflow inventories dependencies.
   - Exact licenses/notices must be reviewed against the restored signed-release graph.

## External validation still required before production-ready claims

Repository source edits and unsigned/unpackaged hosted compile jobs cannot honestly complete these gates:

- observe all configured release-candidate workflows successfully complete on the exact release candidate;
- signed Android AAB/APK build/install/upgrade;
- signed Windows MSIX/package build/install/update and package-identity/protocol/capability checks;
- Apple Developer App Group provisioning for iOS app + iOS Share Extension;
- signed iOS device/TestFlight Share Extension/provider behavior;
- signed/notarized Mac Catalyst containing-app sandbox/App Group/native-drop/provider behavior;
- physical Android/iOS/macOS/Windows transfer matrix in both directions;
- Windows→non-Windows folder manifests using canonical `/` paths;
- real content providers with null/negative/changing Android size metadata;
- real guest Wi-Fi/client isolation/multicast/firewall/local-network-permission tests;
- IPv4/IPv6 combinations, network changes, sleep/lock, low storage, multi-gigabyte files, large batches;
- source/receive symlink-reparse cases on representative filesystems;
- completed-item mutation between retry plan and zero-byte ACK;
- real SecureStorage/keychain/keystore upgrade/restore/locked-device scenarios;
- real supported-database upgrade behavior through schema v4 on representative targets;
- TalkBack, VoiceOver, Narrator, keyboard-only, large-text, reduced-motion, high-contrast, and Hindi layout validation;
- final store privacy declarations, screenshots, signing/notarization, dependency-license review, and submission checks.

## Repository completion sweep

August 14 repository searches found no remaining maintained-source occurrences of:

- `TODO`;
- `FIXME`;
- `NotImplementedException`;
- placeholder implementation markers;
- stub implementation markers;
- stale current-state Mac Catalyst Share Extension references.

Open GitHub issue search returned no open issues at the time of the sweep.

## Connector/environment limits

- The active chat runtime does not provide private platform signing material or physical-device/store access; those release gates are intentionally documented rather than falsely claimed complete.
- GitHub status evidence is checked against exact source/workflow commits; absence of a status context is treated as **unknown/unreported**, not a pass.
- Contents API writes do not expose an independent author/committer-email override; focused commits use `Signed-off-by: Sanskar <sanskarin@outlook.in>`.

See `NEXT_STEPS.md` for release validation priorities and `what_changed.md` for the complete engineering ledger.

## August 15 aggregate performance-trend export

SwiftDrop now derives a rolling 30-day UTC performance trend from valid completed History measurements and can export it as a deterministic aggregate-only CSV through the operating-system share sheet. The query path is cutoff-based rather than UI-limit based, so all retained valid measurements in the selected window can contribute.

The export contains only UTC date, measured transfer count, measured bytes, measured duration, and weighted bytes/second. It does not expose file/device/path/network/authentication/content fields and introduces no new database schema or remote telemetry.

Portable coverage for the corrected source is **559/559 xUnit tests** plus **26/26 Python helper tests**, including a permanent cross-layer trend/export contract. Exact final platform/release run IDs are recorded in `what_changed.md` after hosted jobs complete. Representative-device and cross-network benchmark correlation remains external evidence.

# Changelog

## Unreleased - 2026-08-15

### Restart-safe queue progress and schema-v4 persistence

- Hardened queue persistence cancellation handling so caller cancellation during initialization or best-effort metadata writes does not mark SQLite persistence permanently unavailable for the app session.
- Sanitized persisted exception-type error codes to the bounded machine-oriented error-code contract.
- Expanded the current portable contract to **522/522 xUnit tests** and revalidated exact runtime source `67fc3feaa506b16d11307afa9da8ca9d151f6d22` through successful Android, focused Windows, Mac Catalyst, iOS Share Extension, and iOS containing-app platform compilation/audit run `31867418650`.

- Extended the local SQLite schema from v3 to v4 with bounded queue `operation_kind`, `updated_utc`, `progress_basis_points`, `item_count`, and `completed_item_count` metadata.
- Added a sequential v3→v4 migration that preserves legacy queue rows with safe defaults and regression coverage for v0/v1/v2/v3 upgrade paths.
- Kept queue persistence deliberately non-authorizing: persisted labels remain generic and the schema contains no pairing nonce, reusable token/session authorization, certificate/private key, peer endpoint, source/destination path, or transferred content field.
- Added bounded queue metadata validation for operation categories, timestamps, progress, item counts, and machine-oriented error codes.
- Preserved last-known safe progress/context when stale `Queued`/`Running` work is marked `Interrupted` after restart; interrupted work is not automatically replayed and still requires fresh authorization for a new transfer attempt.
- Wired file, batch, and text sender progress into the shared queue service while coarsening ordinary SQLite progress writes to 5% buckets plus state/item-count transitions.
- Added operation category, percentage/item progress, and a progress bar to the queue UI; privacy-mode in-memory labels continue to follow the existing redaction behavior while persisted labels stay generic.
- Expanded Core storage tests for rich queue round trips, interrupted-progress preservation, invalid progress/item relationships, schema-v4 migration, and explicit absence of authorization/endpoint field classes from queue persistence.
- Updated storage, privacy, compatibility, roadmap, project-status, and engineering-ledger documentation to the v4 contract while preserving the external signed/device/store release-validation boundary.

## Unreleased - 2026-08-14

### Windows portable verification and SQLite resource-lifetime hardening

- Added a dedicated Windows PowerShell portable-verifier CI job so Core tests, helper/documentation validators, benchmark compilation, and vulnerable-package validation execute on both Ubuntu and Windows.
- Fixed the PowerShell `${LASTEXITCODE}` interpolation parser defect exposed by the first Windows run.
- Added cross-platform SQLite temporary-database cleanup that clears Microsoft.Data.Sqlite pools and removes DB/WAL/SHM files, plus a direct cleanup regression.
- Fixed deterministic SQLite command disposal throughout schema migration, batch-completion, diagnostics, transfer-history, queue-metadata, and trust stores after Windows file-lock testing exposed retained native resources.
- Explicitly scoped schema-test connections before temp-file cleanup rather than masking handle-lifetime failures with retries.
- Portable xUnit coverage is now **517 tests**; exact source-head CI run `31785808946` passed all tests on Ubuntu and through the Windows PowerShell verifier.
- Source-head CodeQL `31785808918` and security hygiene `31785808999` passed after the storage fixes.
- Platform run `31786513898` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator app, target vulnerability audits, evidence manifests, and artifact uploads.
- Added same-ref concurrency controls to platform/core/CodeQL/security workflows so obsolete intermediate runs are cancelled in favor of the newest branch evidence.

### Release evidence, audit enforcement, and adversarial regression expansion

- Added a reusable NuGet vulnerability-report validator that rejects actual direct/transitive vulnerability findings and malformed report structure instead of treating any valid JSON file as clean evidence.
- Added 10 Python regression tests covering vulnerability-report interpretation and deterministic dependency-evidence manifest generation.
- Added deterministic dependency-evidence manifests containing path, exact byte length, and SHA-256 for retained audit JSON files.
- Platform/release workflows now emit and validate separate dependency evidence for Android, focused Windows, Mac Catalyst, iOS containing app, and iOS Share Extension, using explicit JSON output schema version 1.
- Platform run `31783405975` passed the complete target compile/audit matrix and uploaded hashed Android/Windows/Apple evidence bundles; downloaded bundle manifests were independently verified against the retained report bytes.
- Release-readiness self-test run `31783537853` passed portable, Android, Windows, Mac Catalyst, iOS Share Extension, iOS containing-app, dependency-audit, artifact-upload, and aggregate-gate jobs.
- Normal CI pins Python 3.13 and validates helper scripts; Bash/PowerShell portable verification now includes explicit vulnerability-report validation.
- Added a Windows CI job for the PowerShell portable verifier. Its first run exposed a PowerShell interpolation parser error, fixed in `080126a0`, proving the value of executing the Windows path instead of only reviewing it statically.
- Added deterministic randomized pairing round-trip/canonical-alias and strict-JSON fuzz/duplicate-property invariants; portable xUnit coverage increased from 511 to **516 passing tests** in CI run `31784196373`.
- Added the canonical dependency-evidence reference and synchronized release process/checklist, CI/build docs, docs index, and third-party notices while preserving the signed-artifact/device/store production boundary.


### Documentation enforcement, community workflow, and dependency completion

- Added a permanent documentation integrity validator and integrated it into regular CI, Linux/macOS and Windows portable verification, and the canonical release-readiness verification path.
- Added a technical glossary and made it part of the required/indexed documentation contract.
- Strengthened pull-request, bug-report, feature-request, and issue-contact templates around reproducibility, security/privacy, compatibility, dependencies/licenses, accessibility/localization, documentation, and signed-device/manual validation.
- Updated QRCoder 1.6.0 -> 1.8.0, synchronized third-party notices, and revalidated Core CI, CodeQL, security hygiene, Android, Windows, Mac Catalyst, iOS Share Extension, and iOS containing-app hosted compilation.
- Closed the superseded QRCoder Dependabot PR after the signed direct-to-main update passed; the final queue check found no open pull requests or issues.

### Complete documentation and contributor/support reference

- Added `docs/README.md` as the canonical documentation index.
- Added installation/source-run, end-user workflow, settings, FAQ, networking/firewall, development, project-structure, CI-reference, release-process, versioning/compatibility, and diagnostics/bug-report guides.
- Expanded troubleshooting across discovery/pairing/firewall/integrity/resume/storage and Android/iOS/Mac/Windows external-intake/build failure cases.
- Expanded contribution, support, community conduct, security disclosure, and usage-term documents while preserving the source-compile versus signed/device/store readiness boundary.
- Public documentation now maps each contract area to a canonical document and records how documentation must stay synchronized with source/tests/release evidence.

### Workflow/runtime and dependency-audit hardening

- Upgraded maintained GitHub Actions to checkout v7, setup-dotnet v6, and CodeQL v4.
- Made repository-wide direct/transitive NuGet auditing explicit at low-or-higher severity under warnings-as-errors.
- Added machine-readable dependency/vulnerability JSON evidence to release readiness and continuously validate the vulnerable-package JSON command in portable CI.
- Updated the .NET 10 test runner/tooling stack to Microsoft.NET.Test.Sdk 18.8.1, xunit.runner.visualstudio 3.1.5, and coverlet.collector 10.0.1.
- Revalidated 511/511 Core tests, benchmark compilation, CodeQL, security hygiene, and the Android/Windows/Mac Catalyst/iOS Simulator compile matrix after the continuation hardening.
- Synchronized third-party notices, release-audit instructions, and contributor guidance with the maintained `.slnx`, audit, and release-validation gates.

### Dependency and portable-gate recovery

- Upgraded `Microsoft.Data.Sqlite` to 10.0.10 and pinned `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 so restore no longer selects the vulnerable `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 dependency that was blocked by warnings-as-errors/security auditing.
- Repaired compile defects that became visible after restore was healthy: missing protocol namespace imports, stale text-size constants, and unsupported SHA-256 hex parsing overloads.
- Restored project-level xUnit namespace wiring and corrected test/analyzer failures without weakening analyzer enforcement.
- Added/retained history-pruning behavior through the compatibility-safe history maintenance API.
- Tightened strict UTF-8 decode-before-JSON behavior, staging-budget exhaustion handling, filename normalization/idempotence, and whitespace canonicalization.
- Portable verification reached 511/511 passing tests and the synthetic benchmark project compiled successfully.

### Android compile and share-intake hardening

- Hardened foreground-service notification construction/API-level checks and nullable Android binding behavior.
- Guarded Wi-Fi multicast-lock acquisition when Android context/services/bindings are unavailable.
- Updated Android intent handling to current bindings and bounded external-share staging directories.
- Preserved sanitized source filenames while staging provider content and cleaned failed staging directories.
- Fixed nullable multi-file picker handling exposed by the current MAUI servicing API contract.
- Android Release compilation is covered by the maintained platform workflow.

### Apple target architecture correction

- Corrected `SwiftDrop.ShareExtension` to the supported iOS-only `net10.0-ios` target.
- The MAUI containing app embeds the Share Extension only for the iOS target.
- Mac Catalyst remains the desktop containing-app/native-`UIDropInteraction` integration path and no longer carries an unsupported Mac Catalyst Share Extension target.
- Removed stale Mac Catalyst Share Extension entitlements.
- Updated Apple metadata validation for the iOS-only extension while preserving Mac Catalyst sandbox/App Group checks on the containing desktop app.
- Hosted iOS Simulator compilation is certificate-independent at CI command scope; the project retains real entitlements for signed/device builds.
- Verified hosted Apple compilation for Mac Catalyst containing app, iOS Simulator Share Extension, and iOS Simulator containing app before the later MAUI servicing refresh.

### Windows compile isolation and WinUI fixes

- Added a focused target-framework override so Windows validation does not enumerate unrelated Android/iOS/Mac Catalyst workloads.
- Added a Windows-only opt-out from the iOS Share Extension project-reference edge for focused restore/build validation; normal iOS product builds keep the extension reference.
- Corrected WinUI activation/drag event namespace ambiguities and explicitly qualified WinRT `DataPackageOperation` values.
- Windows source/XAML/WinUI compilation now reaches the generated application assembly.
- Hosted Windows CI now performs an unpackaged compile (`WindowsPackageType=None`, `GenerateAppxPackageOnBuild=false`) so source compilation is separated from signing/MSIX infrastructure.
- Signed MSIX generation, signing, install, update, and package-identity behavior remain explicit external release gates rather than being falsely represented by hosted compile CI.

### .NET MAUI servicing refresh

- Updated `Microsoft.Maui.Controls` from 10.0.0 to 10.0.90.
- Adapted multi-file picker handling to the nullable/cancelled result contract exposed by the serviced package.
- Kept the existing .NET 10 Android/iOS/Mac Catalyst/Windows product target matrix while allowing focused CI validation of one target at a time.

### CI and workflow cleanup

- Removed completed one-time self-edit/hardening workflows and the duplicate stale platform smoke workflow.
- Consolidated platform compilation in the maintained `platform-builds.yml` workflow.
- Aligned release-readiness platform commands with the maintained Android, focused Windows, Mac Catalyst, and iOS Simulator gates.
- Kept iOS simulator signing/provisioning overrides confined to hosted commands instead of removing real project entitlements.
- Kept Windows signed/package verification separate from unpackaged source compilation.
- Marked the localization markup extension as service-provider-independent to remove repeated XAML compiler service-provider warnings.
- Removed unsupported `Entry.LineBreakMode` usage from Settings XAML.

### Documentation synchronization

- Updated README, BUILDING, platform integration status, release checklist, manual test matrix, and release-validation roadmap to the maintained iOS-only Share Extension architecture.
- Documented Mac Catalyst as the containing desktop app/native-drop path rather than an extension host.
- Documented focused Windows target-matrix commands and the compile-versus-signed-MSIX boundary.
- Preserved signed Apple App Group/provisioning, signed Windows packaging, physical-device/network/provider/filesystem/accessibility, dependency-license, and store validation as mandatory release gates.
- Repository sweeps found no remaining `TODO`, `FIXME`, `NotImplementedException`, placeholder, or stub implementation markers in the maintained source tree.

## Unreleased - 2026-08-12

### Canonical pairing capability representation

- Pairing links now reject surrounding whitespace instead of trimming it into an alias.
- Raw pairing query parsing now requires exactly one `p=` field and rejects empty, unknown, duplicate, or malformed query segments.
- Pairing payload transport now accepts only unpadded canonical Base64URL using ASCII letters, digits, `-`, and `_`.
- Standard Base64 `+`, `/`, padding `=`, percent-encoded payload aliases, invalid Base64URL lengths, and decoded bytes that do not re-encode to the exact original payload text are rejected.
- Existing strict decoded pairing JSON validation remains active, including duplicate/case-variant duplicate/unknown-member rejection, bounded depth, comments/trailing-comma rejection, local numeric address policy, fingerprint/nonce/expiry validation, and exact protocol version.
- Added direct regression coverage for canonical query/Base64URL and whitespace behavior.

### Canonical cross-platform manifest paths

- Added reusable `PortableRelativePath` policy.
- Rooted, Windows drive/UNC/device, repeated/trailing separator, empty-segment, `.` and `..` paths are rejected portably.
- Relative paths now have a maximum 64-segment nesting depth.
- `FileNameSanitizer.SanitizeRelativePath` now emits `/` as the only wire separator on every host OS.
- Incoming file/batch manifest paths must already equal SwiftDrop's canonical sanitized form; receiver code no longer accepts a peer path that would be rewritten after authorization.
- Backslash wire paths, portable-invalid filename aliases, Windows reserved-device aliases, trailing dot/space aliases, and decomposed Unicode aliases are rejected at the manifest boundary.
- `ManifestValidator` performs path structure/canonicality checks before transfer nonce consumption, with tests proving unsafe paths do not consume authorization.
- Batch sender deconfliction uses canonical `/` paths throughout, fixing Windows→Android/iOS/Mac exact-plan identity drift.
- Batch relative-path length is preflighted before hashing.

### Portable filename and collision bounds

- Filename segments now retain the existing 180 UTF-16 code-unit cap and add a 180 UTF-8 byte cap.
- UTF-8 truncation is rune-safe and does not split surrogate pairs/Unicode scalars.
- The byte cap intentionally leaves headroom for `.swiftdrop.part` on common 255-byte filesystem component limits.
- Added `FileNameSanitizer.CreateCollisionSegment` so collision-generated names remain bounded and unique.
- When `name (n).ext` would lose its suffix because the base is already at the limit, SwiftDrop uses a bounded prefix marker such as `(n) name...`.
- Receive destination reservations, generic collision-free platform staging, batch sender deconfliction, and Apple Share package naming now share the bounded collision helper.
- Added tests for Unicode/emoji-heavy names, extreme extensions, staging-suffix headroom, bounded collision markers, and concurrent/max-length receive reservations.

### Outgoing source safety and deterministic folder manifests

- Added reusable `TransferSourceSafety` for regular file/directory validation and symlink/reparse rejection.
- Single-file coordinator validates the selected source as a regular non-link file.
- `TransferEngine.SendFileAsync` repeats regular-source validation at the actual stream-open boundary.
- Added `TransferSourceEnumerator` for explicit bounded recursive folder enumeration.
- Selected folder roots and descendant files/directories that are symbolic links/reparse points are rejected.
- Folder traversal is bounded by file/directory limits and no longer uses unrestricted `SearchOption.AllDirectories`.
- Enumerated folder files are sorted deterministically by normalized relative path before manifest construction.
- Batch builder reuses central regular-source validation and canonical `/` path construction.
- Case-only, Unicode-normalization, and sanitation-equivalent portable sender paths are deconflicted before expensive hashing and revalidated by `BatchManifestValidator` afterward.
- Filesystem-root folder selection has a safe fallback transfer root label.
- Added tests for source file/directory links, deterministic enumeration/order, stable repeated folder manifests, sender portable collisions, and send-boundary link rejection.

### Stable resume source and UI/API cleanup

- Added `TransferSourcePathPolicy` for existing regular file/folder resume candidates using platform-aware local path comparison.
- Active batch resume preserves folder source selections where they remain available.
- Paused single/batch source state drops files/folders replaced by symlinks/reparse points before resume.
- Single-file Send/Resume validates a regular source before consuming the fresh pairing invitation.
- Paused folder history no longer assumes every source is a `FileInfo`; folder metadata is handled safely.
- Removed obsolete duplicate non-XAML batch handlers from `MainPage`.
- Removed `TransferCoordinatorCompatibilityExtensions`, eliminating the implicit fresh-batch-ID overload.
- The active XAML batch workflow now has one coordinator API path: caller-supplied stable transfer ID.
- Batch transfer IDs now use bounded canonical ASCII token syntax (letters, digits, `-`, `_`).

### Completed-item retry race hardening

- Existing schema-v3 completed-item retry still verifies transfer/root/source/destination/length/hash and freshly re-hashes the finalized destination while building the receiver retry plan.
- Receiver now verifies the completed destination **again after the matching `BatchItemStart` and immediately before the zero-byte item completion acknowledgement**.
- A destination removed, mutated, redirected, or no longer matching the completion record between plan generation and item ACK now fails closed instead of being falsely acknowledged.
- Added regression coverage that verifies a completed file, mutates it, and confirms the repeated verification rejects it.

### Shared external staging budgets

- Added reusable Core `TransferStagingBudget` for maximum file count, maximum single-file bytes, aggregate bytes, and commit-after-success accounting.
- Failed file copies do not consume staging count/byte budget.
- Apple Share Extension, Android share intake, and Mac native drop now use this shared policy.

### Android share reliability

- Android `ACTION_SEND` / `ACTION_SEND_MULTIPLE` staging now enforces aggregate budget during actual copy, including providers whose size is unknown.
- Negative provider `OpenableColumns.Size` is treated as unknown rather than as valid negative metadata.
- Unknown-length provider bytes are capped to the remaining aggregate staging budget.
- Unknown-length copies recheck destination free-space reserve while streaming so a provider cannot consume the volume down to zero merely because its final size was unknown at initial preflight.
- Exact declared/staged length checks, portable filename sanitation, cleanup on failure, and atomic inbox handoff remain active.

### Apple Share Extension provider lifecycle

- Share Extension provider file/text callbacks are bounded by a provider-response timeout and extension-lifetime cancellation.
- Late callbacks after timeout/cancellation cannot begin a new staging copy.
- Cancellation is checked during provider-file copying.
- Provider-response timeout is explicitly separated from local copy duration: a provider that responded before the timeout can complete an already-started legitimate copy even if that copy takes longer.
- Aggregate staging budget is checked before copying the file that would exceed package limits.
- Existing strict App Group manifest, atomic staging→pending publication, security-scoped access, capacity checks, and review-before-send behavior remain active.

### Apple containing-app import hardening

- App Group package import remains serialized.
- Physical package `files/` contents must exactly match the manifest-declared top-level files.
- Undeclared extra files, nested directories, portable duplicate names, missing declared files, and link/reparse entries are rejected.
- Importer now sums the validated package file bytes and preflights app-cache capacity before recopying the package into normal review staging.
- Only one pending package is surfaced per import pass; later pending packages are retained rather than silently merged/deleted.

### Mac Catalyst native drop reliability

- Mac native drop now shares the Core staging-budget policy.
- File/folder copy retains security-scoped access and source link/reparse rejection.
- Native-drop file/text provider callbacks now have bounded response waits.
- A provider that returns before timeout is allowed to complete a legitimate copy beyond the response timer.
- Portable UTF-8-bounded collision naming is used by generic drop collision resolution.

### Transfer engine metadata reliability

- Send engine revalidates regular-source/link status immediately before opening the source stream.
- Source length remains bound to the manifest before/during streaming.
- Optional last-write timestamp application now occurs as best-effort metadata after verified final promotion; inability to set that metadata no longer falsely converts verified transferred content into a failed transfer.

### Documentation and release validation

- Updated wire-format documentation for canonical pairing capability text, `/` manifest paths, transfer-ID tokens, source-link handling, and second completed-item verification.
- Updated protocol security documentation with pre-authorization path validation, source-tree safety, external staging budgets, provider-response semantics, and Android unknown-size storage reserve behavior.
- Updated threat model for capability aliases, canonical path identity, outgoing link/tree escape, byte-bounded collisions, staging exhaustion, and repeated completed-file verification.
- Expanded security test plan, manual platform/cross-device matrix, and release checklist for all new invariants.
- Updated platform integration status, public README, project status, and release-validation roadmap.
- Production verification remains gated on exact-candidate CI, target workloads, signing/provisioning, real providers/devices/networks/filesystems/low-storage cases, accessibility/localization, dependency/license review, and store submission checks.

## Unreleased - 2026-08-10 to 2026-08-11

### Apple platform integration

- Added a dedicated `SwiftDrop.ShareExtension` project targeting iOS and Mac Catalyst.
- Added shared App Group `group.in.sanskar.swiftdrop` to containing app and Share Extension entitlements.
- Added strict versioned App Group package manifests validated by `SwiftDrop.Core`.
- Added atomic Share Extension package publication from `.staging-*` to `pending-*`.
- Added containing-app App Group importer with strict JSON, unknown-member rejection, package-age bounds, canonical path/name checks, symlink/reparse rejection, exact-length validation, stale staging cleanup, and app-cache re-staging.
- Added bounded Share Extension provider intake for files/images/movies/text/web URLs with security-scoped access, storage preflight, exact-length staging, cancellation, and review-before-send behavior.
- Added native Mac Catalyst `UIDropInteraction` for files, folders, text, and pairing links.
- Added Mac drop count/per-file/aggregate bounds, security-scoped staging, symlink rejection, portable filename sanitation, and collision-safe directory/file deconfliction.
- Added Apple project/entitlement/version/App Group consistency validator to portable CI and release checks.
- Added explicit Share Extension compile gates for Mac Catalyst and unsigned iOS Simulator jobs.
- Added Apple Share Extension dependency inventories for both Apple target frameworks.

### Cross-platform external intake

- Hardened Android `ACTION_SEND` / `ACTION_SEND_MULTIPLE` staging with provider-count limits, provider-declared size checks, runtime byte caps, storage preflight, portable filename sanitation, exact staged length verification, partial cleanup, and atomic inbox handoff.
- Aligned Windows native drop with protocol constants and atomic text/path handoff.
- Added shared rune-safe UTF-8 truncation for external text so multi-byte characters/surrogate pairs are never split at the byte limit.
- Extended stale external-input cache cleanup to nested staging directories.

### Application protocol architecture and security

- Added shared Core wire records for protocol requests, transfer acknowledgements, batch item starts, and pairing responses.
- Added `ProtocolRequestFactory` for validated outgoing request construction.
- Added `ProtocolRequestValidator` for type-specific incoming shape validation and cross-type field-smuggling rejection.
- Added `ProtocolSessionAuthorizer` for testable one-time authorization consumption/replay behavior.
- Centralized sender identity, pairing nonce, pairing code, transfer ID, and batch-item ordering rules.
- Migrated transfer sender, nearby/manual pairing, and receive host to the same typed Core wire records.
- Changed framed protocol JSON deserialization to reject unknown/unmapped members in addition to duplicate members, malformed UTF-8/JSON, comments, trailing commas, excessive depth, and invalid frame lengths.
- Added strict duplicate/unknown member controls to encoded pairing JSON.
- Preserved authorization ordering so malformed requests and missing TLS client certificates do not consume a valid one-time transfer nonce.
- Added portable complete file/batch/text/pair conversation tests using the production wire records/policies.

### Receiver lifecycle and filesystem safety

- Added portable `AsyncSessionTracker` and migrated receive listener active-handler tracking/draining to it.
- Added session drain tests covering normal completion, faults, cancellation, and sessions added during drain.
- Added portable rooted-path rejection for Windows drive/UNC/device syntax even on non-Windows hosts.
- Added receive-root symlink/reparse component rejection before/after staging directory creation, before hashing, and before final promotion.
- Added reparse-safe completed-batch destination verification.
- Changed final receive promotion to non-overwrite semantics so a file created by another writer after reservation is preserved instead of replaced.
- Added deterministic final-promotion race and reparse/symlink tests.

### Idempotent interrupted-batch resume

- Added caller-supplied stable batch transfer IDs to `BatchTransferSourceBuilder` and `TransferCoordinator`.
- Routed actual MainPage batch Send/Pause/Resume/Cancel controls through stable-ID lifecycle handling.
- Preserved file **and folder** source selections across pause/failure retry where sources still exist.
- Added SQLite schema version 3 with `completed_batch_items` metadata.
- Added privacy-safe `ReceiveRootKey` using SHA-256 of normalized receive-root identity instead of storing absolute receive-root path.
- Added `BatchCompletionStore`, `BatchResumeStateService`, and `BatchCompletionVerifier`.
- Receiver records verified finalized batch items before sending normal item completion ACK.
- On retry with the same batch ID, receiver revalidates metadata, path confinement/reparse status, destination length, and fresh SHA-256 before offering full-length resume offset.
- Already-completed verified items use protocol-v1 `ResumeOffset == Length` semantics and require zero additional payload bytes.
- Changed/missing destinations, changed source manifest, different root, or new transfer ID fall back to normal collision-safe transfer behavior.
- Completion metadata is bounded/pruned and best-effort; persistence failure cannot turn a successfully verified transfer into a failure.
- Added v2→v3 migration tests, completion-store corruption/pruning tests, stable-ID tests, and completed-file verification tests.
- Added a second completed-file verification before zero-byte ACK to reduce the retry-plan/ACK TOCTOU window.

### Privacy and local metadata

- Updated schema documentation to version 3.
- Kept transfer contents, private keys, pairing invitations/nonces, source absolute paths, receive-root absolute paths, and reusable authorization out of SQLite.
- Existing history privacy mode redacts both peer/file identifiers; diagnostic privacy mode redacts common paths/emails/IPs/endpoints/GUIDs/fingerprints/pairing URIs at record/read/export time.
- Android application backup remains disabled.
- Windows protocol package remains private-network-only.

### CI, build, and release engineering

- Added `scripts/validate_apple_integration.py`.
- Integrated Apple metadata validation into Unix/PowerShell verification, regular CI, and release readiness.
- Platform build triggers include Share Extension source changes.
- Apple jobs explicitly restore/build both Share Extension and containing app.
- Release readiness requires Apple extension/app compile gates and captures extension dependency graphs for both iOS and Mac Catalyst.
- Added Share Extension-specific warning policy that keeps nullable regressions strict while leaving Apple SDK availability/obsolete diagnostics visible.
- Kept stable C# language mode (`latest`, not preview).

### Documentation

- Updated README, BUILDING, privacy, platform integration/permissions, architecture, wire/security protocol docs, compatibility matrix, SQLite schema docs, project status, roadmap, release checklist, manual/security test plans, third-party notices, and the engineering ledger for the source state.
- Reclassified the master-prompt scope as source-complete while keeping signed package, App Group provisioning, real-device/network/accessibility, dependency-license, and store validation explicitly pending.

### Validation boundary

- The development chat runtime does not provide the full .NET MAUI workloads needed to compile/sign all targets locally.
- Missing GitHub combined-status contexts are treated as unknown/unreported, never as a pass.
- Signed Apple App Group provisioning, Share Extension embedding/runtime behavior, Mac native drop under release sandbox, signed Android/Windows packages, physical cross-device transfers, accessibility/localization validation, real low-storage/network lifecycle cases, and final dependency-license review remain release gates.

## 1.0.0 - 2026-08-09

- Added the initial .NET MAUI app shell for Android, iOS, macOS (Mac Catalyst), and Windows.
- Added QR/deep-link pairing payloads with expiration and one-time nonce authorization.
- Added self-signed per-device certificate generation and certificate fingerprint pinning.
- Added TLS local-network transport and framed JSON protocol messages.
- Added chunked file streaming, resumable partial files, SHA-256 verification, size limits, and path traversal protection.
- Added local device identity storage with platform secure storage for the certificate.
- Added UDP discovery core service, SQLite trusted-peer store, project documentation, tests, and CI.
- Added Apache-2.0 open-source licensing and project contribution/security policies.

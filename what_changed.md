# What changed

Date: 2026-08-10
Repository: https://github.com/sanskarIN/SwiftDrop

This file is the detailed engineering ledger requested for SwiftDrop. Chat replies are intentionally kept short; implementation details, source boundaries, security/privacy decisions, tests, platform integration, validation limits, and remaining release work are recorded here instead.

## Source prompt alignment

Work continues against `07_SwiftDrop_Local_File_Transfer_Master_Prompt.md` and its local-first, account-free, cross-platform .NET MAUI/C# requirements.

Repository/product metadata retained throughout the implementation:

- Product: `SwiftDrop`.
- License: Apache-2.0.
- Branding/watermark: `Made by the Sanskar`.
- Repository: `https://github.com/sanskarIN/SwiftDrop`.
- GitHub profile: `https://www.github.com/sanskarIN`.
- Business/security email: `sanskarin@outlook.in`.
- Support email: `supportramsandesh@gmail.com`.
- Optional project support link: `https://buymeacoffee.com/sanskarIN`.

The optional support link does not unlock transfer features, privileged support, faster security handling, private data access, hidden application capabilities, or a separate product tier.

---

# 2026-08-10 continuation work

This section records the focused work performed in the latest continuation on `main`.

## Pairing JSON hardening

- `PairingCodec.Decode` now applies `StrictJsonGuard.Validate(decoded, 16)` before deserializing the decoded invitation JSON.
- Pairing JSON therefore shares the strict ambiguity controls already used by framed protocol JSON.
- Duplicate property names are rejected.
- Case-variant duplicate properties are rejected.
- JSON comments are rejected.
- Trailing commas are rejected.
- JSON depth is bounded.
- Malformed JSON/strict-guard failures are normalized to safe pairing decode failure rather than reaching downstream logic ambiguously.
- Existing outer pairing URI rules remain active after the strict decoded-JSON guard.
- Existing local/private/link-local numeric host policy remains active.
- Existing protocol-version, device metadata, fingerprint, nonce, port, expiration and lifetime bounds remain active.

New/expanded pairing tests cover:

- duplicate decoded JSON fields;
- case-variant duplicates;
- comments;
- trailing commas;
- malformed invitation payloads;
- expiration exactly at the current instant;
- expiration one second in the future;
- excessive invitation lifetime;
- host/fingerprint normalization without changing authorization nonce;
- cryptographically random nonce uniqueness expectations.

## One-time pairing authorization store

- Replaced DeviceIdentityService's private ad-hoc pairing-nonce dictionary with reusable Core `OneTimeAuthorizationStore`.
- Active one-time authorization is bounded in memory.
- Registration rejects malformed nonce strings.
- Registration rejects already-active duplicate nonces.
- Registration rejects already-expired authorization.
- Active-store capacity is bounded.
- Expired entries are pruned.
- Consumption is atomic through concurrent dictionary removal.
- Only one concurrent consumer can win for the same nonce.
- Replayed nonce consumption fails.
- Device identity reset clears all active one-time authorization.
- Automatic device identity regeneration clears active one-time authorization.
- DeviceIdentityService disposal clears active authorization.
- Authorization expiry is stored at exact UTC tick precision rather than Unix-second precision.
- A nonce is rejected immediately after exact sub-second expiry rather than potentially remaining valid until the end of the second.

New tests cover:

- one successful consume followed by replay rejection;
- expired authorization rejection;
- sub-second exact expiry rejection;
- 64 concurrent consumers with exactly one successful winner;
- duplicate active registration;
- bounded capacity;
- expired-entry pruning reclaiming capacity;
- malformed nonce rejection.

## Incoming request policy extraction

Added reusable Core `IncomingRequestPolicy` and moved receive-host checks into it for:

- protocol version;
- allowed request types (`file`, `batch`, `text`, `pair-request`);
- bounded non-control sender device ID;
- bounded non-control sender device name;
- bounded/control-free batch transfer ID;
- exact negotiated batch item order/path validation.

`ReceiveServerService` now calls this policy rather than maintaining independent private checks for those fields.

Tests cover:

- all supported request types;
- unknown/case-changed request type rejection;
- unsupported protocol version;
- empty/control-character sender identity rejection;
- oversized sender identity rejection;
- valid bounded sender identity;
- empty/control/oversized transfer ID rejection;
- exact transfer ID return;
- reordered/unknown batch item path rejection;
- exact negotiated batch item acceptance.

## Transfer response policy extraction

Added reusable Core `TransferResponsePolicy` and moved outgoing-response checks into it for:

- receiver rejection propagation;
- single-file resume offset 0..declared length;
- exact file completion length;
- exact batch item completion length;
- exact batch final aggregate completion length;
- text acknowledgement acceptance;
- text acknowledgement offset must be exactly zero.

`TransferCoordinator` now uses the shared policy for single-file, batch and text flows.

Tests cover:

- valid zero/middle/end resume offsets;
- negative/oversized resume offsets;
- receiver rejection;
- exact completion length;
- short/long mismatched completion length;
- text zero-offset acknowledgement;
- negative/nonzero/huge text acknowledgement offsets.

## Receiver batch-plan validation improvements

The sender now validates receiver batch plans through reusable Core `BatchTransferPlanValidator`.

The validator rejects:

- receiver paths not present in the sender source manifest;
- duplicate receiver plan paths;
- missing plan items from an accepted overall response;
- resume offsets below zero;
- resume offsets above source length;
- rejected items that advertise a nonzero resume offset;
- an overall rejected response that contains accepted item plans;
- an overall accepted response with no accepted files;
- duplicate source-manifest paths.

The sender now also:

- revalidates the pairing payload at the actual send boundary;
- initializes local identity defensively before opening the TLS connection;
- reports resumed progress immediately from the receiver-provided offset;
- verifies exact receiver per-item completion length;
- verifies exact final aggregate batch completion length.

## MainPage runtime localization

Added/expanded English/Hindi runtime resource catalogs for:

- pairing dialogs;
- nearby pairing request details;
- one-time-code prompts;
- manual-IP pairing bootstrap warnings;
- incoming file consent;
- incoming text consent;
- trust prompts;
- receive-folder status/errors;
- local certificate display;
- pairing link share title;
- fingerprint confirmation;
- selected file/batch labels;
- file/batch selection errors;
- send/resume requirements;
- sending/resuming/completed/paused/cancelled/failed status;
- batch checksum preparation/progress/ETA/completion/failure;
- clipboard explicit-read status;
- text required/sending/delivered/failure status;
- external share/drop/open intake status;
- folder-picker/folder-transfer status;
- identity-recovery notice;
- incoming batch sender/risk/selection messages;
- history dialogs/counts/status/direction/size/time/private markers;
- Queue/Nearby/Trusted dynamic counts/timestamps;
- Settings runtime save/reset/permission dialogs;
- Diagnostics export/clear/self-test dialogs;
- About external-link errors.

Hindi counterparts now exist for all of these runtime catalog keys.

## Localization validation hardening

`scripts/validate_localization.py` now validates all paired English/Hindi catalogs:

- `AppStrings`;
- `MainStrings`;
- `DialogStrings`;
- `MainRuntimeStrings`;
- `PlatformRuntimeStrings`;
- `BatchRuntimeStrings`;
- `HistoryRuntimeStrings`.

The validation gate checks:

- source files exist;
- XML parses successfully;
- localization keys are non-empty;
- localization values are non-empty;
- duplicate keys are rejected;
- English/Hindi key sets match exactly;
- format placeholder indices such as `{0}`, `{1:N0}`, `{2:T}` match between English and Hindi.

This prevents a translated format string from silently dropping or inventing an argument index.

## Main presentation MVVM migration

Added dedicated singleton `MainViewModel` for MainPage presentation state.

MainViewModel owns:

- device name display;
- device ID display;
- certificate fingerprint display;
- active receive-folder display;
- remote peer display;
- selected single-file display;
- selected batch display;
- single transfer status;
- batch transfer status;
- text transfer status;
- single transfer progress;
- batch transfer progress;
- send file enabled state;
- pause single enabled state;
- resume single enabled state;
- cancel single enabled state;
- send batch enabled state;
- pause batch enabled state;
- resume batch enabled state;
- cancel batch enabled state.

MainPage XAML now binds those fields rather than mutating named labels/progress bars/buttons directly.

MainPage code now writes presentation state through `_viewModel`.

The following deliberately remain page/platform/service concerns rather than being forced into the view model:

- file picker;
- folder picker;
- QR image generation/rendering;
- clipboard read/write;
- share sheet;
- navigation;
- modal user consent/confirmation dialogs;
- receive-server lifetime;
- TLS/network operations;
- filesystem transfer;
- cryptographic identity/certificate operations.

Post-migration compile consistency work:

- `MainPage.ExternalInput.cs` was updated after the XAML named controls were removed.
- `MainPage.FolderPicker.cs` was updated after the XAML named controls were removed.
- external shared text now writes `MainViewModel.TextTransferStatus`;
- external file/folder intake now writes `MainViewModel.SelectedBatch` and batch status;
- folder selection now writes MainViewModel batch state;
- identity recovery notice remains a MainPage partial because it is a modal platform/UI interaction.

## Secondary-screen MVVM state

Dedicated view models now back presentation state for:

- Main;
- History;
- Transfer Queue;
- Nearby Devices;
- Trusted Devices;
- Diagnostics;
- Settings;
- About.

Network, storage, TLS, protocol and cryptography remain in services/Core rather than being placed in the view models.

## History privacy and localized presentation

`TransferHistoryService` privacy mode now hides both:

- peer device name;
- file/description name.

For new records in privacy mode:

- the persisted peer label is the language-neutral marker `[private]`;
- the persisted file/description label is the language-neutral marker `[private]`.

For older records while privacy mode is enabled:

- peer label is replaced at read time;
- file/description label is replaced at read time;
- old rows are not destructively deleted simply because privacy mode was enabled.

History presentation now uses `HistoryViewModel.HistoryRow` rather than exposing the raw storage model directly.

The presentation row localizes:

- privacy marker;
- direction (`sent`, `received`);
- status (`completed`, `failed`, `cancelled`, `paused`, `rejected`, `not-selected`, `accepted`, `copied`);
- byte count;
- local display timestamp.

Backward compatibility:

- older persisted English `Hidden by privacy mode` markers are still recognized and shown through the current localized private label.

## History storage validation/corruption tolerance

`TransferHistoryStore` now validates new writes:

- bounded non-control ID;
- bounded non-control direction token;
- bounded peer text without line breaks;
- bounded file/description text without line breaks;
- nonnegative size;
- plausible timestamp between Unix epoch and small future clock tolerance;
- bounded non-control status token.

Read behavior:

- malformed date/field/cast/validation rows are skipped;
- one corrupted row no longer breaks the complete history list;
- valid rows remain readable.

Tests cover:

- round-trip;
- clear;
- negative size rejection;
- newline/control metadata rejection;
- direct database insertion of a corrupted timestamp row;
- corrupted row skipped while valid row remains returned.

## Diagnostic privacy redaction

Added Core `DiagnosticPrivacyRedactor`.

In privacy mode it redacts tokens that look like:

- IPv4 addresses;
- IPv4 address plus port;
- IPv6 addresses;
- bracketed IPv6 address plus port;
- GUIDs;
- compact SHA-256 fingerprints;
- colon-separated SHA-256 fingerprints;
- file paths containing slash/backslash;
- email-like tokens;
- `swiftdrop:` pairing URIs.

`DiagnosticLogService` applies structured redaction:

- when writing new events in privacy mode;
- when reading older events in privacy mode;
- when exporting safe diagnostics because export uses the already filtered read path.

Generic diagnostic wording that does not resemble an identifier remains visible.

Tests cover each identifier class plus generic non-sensitive text.

## Diagnostic storage validation/corruption tolerance

`DiagnosticEventStore` now validates:

- bounded non-control event ID;
- plausible timestamp;
- bounded non-control level;
- bounded non-control code;
- bounded single-line message.

Read behavior:

- malformed/corrupted rows are skipped;
- valid rows remain visible.

Tests cover:

- normal round-trip and clear;
- multiline message rejection;
- direct corrupted timestamp row;
- valid row survives alongside corrupted row;
- future diagnostic timestamp rejection.

## Trusted-device storage hardening

`TrustStore` now enforces trust integrity at the Core storage boundary, not only through the app wrapper.

Before writes it validates/normalizes:

- device ID bounds/control characters;
- display name bounds/control characters;
- canonical uppercase SHA-256 certificate fingerprint.

Read behavior:

- malformed/corrupted persisted trusted-peer rows are ignored;
- malformed fingerprint rows never become implicit trust;
- exact device ID + exact certificate fingerprint remains required by trust matching.

Tests cover:

- lowercase fingerprint canonicalized to uppercase;
- normal get/list;
- remove/clear;
- malformed fingerprint write rejected;
- direct database malformed fingerprint row ignored;
- same device ID with changed certificate fingerprint updates the stored binding.

## mDNS parser hardening

`MdnsCodec.Reader.ReadTxt` now rejects duplicate TXT metadata keys rather than allowing last-value-wins semantics.

New mDNS tests cover:

- valid query recognition;
- valid announcement round-trip;
- truncated/random basic packets;
- DNS compression pointer loop in query;
- compression pointer loop in announcement;
- duplicate TXT metadata key mutation;
- impossible question count on a short packet;
- every truncated prefix of a valid announcement;
- 2,000 deterministic random packets through query and announcement parsers with no escaping parser exception.

## Portable external-file staging

Added Core `ExternalFileStager` for platform share/open adapters.

Behavior:

- source path required;
- source must exist;
- maximum byte limit enforced before copy;
- destination root created explicitly;
- destination filename sanitized through `FileNameSanitizer`;
- unique destination name generated;
- source expected length captured;
- source length rechecked at stream open;
- copy reads/writes in bounded asynchronous chunks;
- cancellation honored;
- unexpected early EOF fails;
- source length rechecked after staging;
- partial staged destination removed on failure/cancellation;
- successful staged path returned only after exact-length copy succeeds.

Tests cover:

- exact-byte copy;
- destination remains beneath staging root;
- safe destination name;
- oversized source rejected without output;
- pre-cancelled operation cleans staged output;
- missing source rejected;
- fixtures remain portable on Windows/Linux/macOS.

## Apple document/open-file staging

Added `AppleExternalFileStager` under iOS/Mac Catalyst conditional compilation.

Apple adapter behavior:

- requires a file URL;
- verifies source file exists;
- obtains temporary security-scoped resource access where provided by the OS;
- delegates all actual bounded/sanitized copying to Core `ExternalFileStager`;
- places successful staged path into `ExternalInputInbox`;
- releases security-scoped access immediately after staging attempt;
- does not direct-send the opened file;
- does not retain broad provider access.

Updated iOS AppDelegate:

- existing `swiftdrop://pair` activation retained;
- incoming file URLs are handed to Apple staging.

Updated Mac Catalyst AppDelegate similarly.

Updated iOS Info.plist:

- existing local-network description retained;
- Bonjour declaration retained;
- `swiftdrop` URL scheme retained;
- added `CFBundleDocumentTypes` for `public.data`;
- added `LSSupportsOpeningDocumentsInPlace`.

Updated Mac Catalyst Info.plist similarly.

Important product boundary:

- Apple document/open-file URL support is implemented;
- a dedicated first-class Apple Share Extension target is **not** implemented;
- documentation explicitly distinguishes those two surfaces.

## Mac Catalyst sandbox/network entitlements

Added `Platforms/MacCatalyst/Entitlements.plist` with:

- `com.apple.security.app-sandbox`;
- `com.apple.security.network.client`;
- `com.apple.security.network.server`.

`SwiftDrop.App.csproj` now wires that entitlements file only for the Mac Catalyst target through `CodesignEntitlements`.

This is source configuration only; signed/notarized sandbox behavior must still be validated on a real macOS/Xcode release environment.

## Android privacy manifest change

Android application manifest now sets:

- `android:allowBackup="false"`.

Reason:

- SwiftDrop stores local trust/history/diagnostic/queue metadata;
- the app should not opt that local application metadata into Android app backup/restore by default.

Existing Android behavior retained:

- local networking;
- network-state access;
- multicast permission;
- foreground data-sync service permissions;
- optional notification permission declaration;
- cleartext traffic disabled;
- no broad storage permission for normal picker/share workflows.

## Windows package capability minimization

Removed general `internetClient` capability from the Windows package manifest.

Retained:

- `privateNetworkClientServer`.

Reason:

- protocol version 1 intentionally rejects DNS/public Internet peer destinations;
- package capability should match the local-only peer networking model.

Existing Windows protocol activation and drag/drop behavior remain intact.

## Stable compiler mode

Root `Directory.Build.props` changed from preview language mode to:

- `LangVersion=latest`.

Retained:

- nullable enabled;
- implicit usings enabled;
- warnings treated as errors;
- latest analyzer level;
- deterministic builds.

SwiftDrop therefore does not require preview C# language mode by repository policy.

## Localization placeholder validation

In addition to catalog key parity, CI now compares formatted argument indices between English/Hindi values.

Examples of mismatches that now fail validation:

- English uses `{0}` but Hindi omits it;
- Hindi introduces `{2}` when English only provides `{0}` and `{1}`;
- a formatted token index changes in translation.

Format specifier wording may differ only insofar as the placeholder index remains the same and `string.Format` remains valid.

## GitHub Actions evidence checked during this continuation

A current direct-main commit was queried through the available GitHub workflow-run connector.

Result returned:

- `workflow_runs: []`.

Earlier combined commit status queries likewise returned no status contexts.

Interpretation recorded in project docs:

- no run/status evidence exposed through the connector is **not** treated as success;
- automated status remains unknown/unreported from this environment;
- actual Actions UI/log evidence for the exact release candidate is still required before release.

---

# Implementation completed across the project

## Repository/build structure

SwiftDrop currently contains:

- canonical `SwiftDrop.slnx`;
- `src/SwiftDrop.Core`;
- `src/SwiftDrop.App`;
- `tests/SwiftDrop.Core.Tests`;
- `benchmarks/SwiftDrop.Benchmarks`;
- GitHub Actions workflows;
- repository security/contribution templates;
- build/verification scripts;
- architecture/protocol/security/privacy/platform/release/testing documentation.

The earlier misleading XML `.sln` bootstrap was removed. The canonical XML solution file is `.slnx`.

Repository build policy:

- .NET 10;
- .NET MAUI;
- latest stable C# language mode;
- nullable enabled;
- analyzers latest;
- warnings as errors;
- deterministic builds.

## Device identity

Each installation maintains:

- random local device ID;
- editable display name;
- local P-256 ECDSA self-signed certificate with private key;
- certificate SHA-256 fingerprint shown to users for verification.

Certificate profile includes:

- non-CA basic constraints;
- digital signature key usage;
- TLS server authentication EKU;
- TLS client authentication EKU;
- subject key identifier;
- bounded certificate validity.

Private certificate/key material is stored through platform SecureStorage and is not persisted in SQLite/history/diagnostics/pairing links.

Identity policy checks:

- private key presence;
- supported ECDSA key;
- NotBefore tolerance;
- expiration;
- near-expiry renewal threshold;
- corrupt/unloadable stored certificate.

When stored identity cannot be safely reused:

- active pairing authorization is cleared;
- old in-memory certificate is disposed;
- a new device ID is created;
- a new certificate is generated/persisted;
- the app surfaces a re-pair notice;
- received files/history are not automatically deleted.

Explicit identity reset:

- creates a new device ID/certificate;
- invalidates active pairing invitations;
- clears locally trusted devices through Settings workflow;
- does not delete received files/history.

## Certificate fingerprint handling

Central Core fingerprint handling:

- requires exactly 32 SHA-256 bytes;
- accepts compact/colon forms where appropriate;
- canonical storage form uppercase 64 hex;
- pretty display colon-separated;
- trusted matching uses exact canonical fingerprint and constant-time byte comparison;
- malformed fingerprint input is rejected.

TrustStore now repeats canonical validation at the persistence boundary.

## Discovery

Discovery stack:

- internal mDNS/DNS-SD service `_swiftdrop._tcp.local`;
- bounded UDP IPv4 fallback;
- peer registry deduplication;
- last-seen/expiry;
- self-filtering;
- stable snapshot sorting;
- platform discovery status in diagnostics.

mDNS implementation:

- query encoding/parsing;
- PTR/SRV/TXT/A announcement records;
- DNS compression-pointer parsing with jump bounds;
- label length bounds;
- packet boundary checks;
- TXT length bounds;
- duplicate TXT key rejection;
- malformed input returns no peer rather than escaping parser exceptions.

Android uses a reference-counted Wi-Fi multicast lock for mDNS.

Apple Info.plist files declare `_swiftdrop._tcp`.

## Pairing modes

Implemented pairing entry points:

- QR/deep-link invitation;
- nearby discovery/request pairing;
- one-time 8-digit code;
- manual numeric local-IP + one-time code fallback;
- platform `swiftdrop://pair` activation.

Pairing invitation includes:

- protocol version;
- receiver device ID/name;
- numeric local LAN address;
- port;
- receiver certificate SHA-256 fingerprint;
- cryptographically random one-time nonce;
- expiration.

Pairing invitation does **not** contain private key material.

Pairing decode validates:

- total URI length;
- scheme/host/path/authority/userinfo/fragment;
- exactly one supported payload query parameter;
- no duplicate/unexpected outer query fields;
- base64url payload bounds;
- strict decoded JSON;
- protocol version;
- bounded device metadata;
- numeric local/private/link-local/loopback address policy;
- port range;
- canonical fingerprint;
- nonce format/length;
- expiration and maximum invitation lifetime.

## TLS/authentication

SwiftDrop uses platform/.NET TLS rather than custom cryptography.

Outgoing connection:

- sender validates pairing payload;
- sender pins receiver certificate fingerprint;
- sender presents its own local client certificate.

Receiver:

- requires sender TLS certificate;
- derives sender fingerprint from authenticated TLS channel;
- does not trust a sender fingerprint string from application JSON;
- applies bounded pairing-attempt rate limit;
- applies one-time authorization for file/batch/text.

Protocol requests TLS 1.2/1.3 according to implemented TLS options.

Self-signed local peer certificate revocation lookup is not used as a public-PKI trust mechanism; authorization instead relies on explicit fingerprint pinning, mTLS peer certificate, one-time capability, receiver consent and local trust metadata.

## File transfer

Single-file flow includes:

- platform file picker;
- source existence check;
- maximum file size;
- sanitized file name;
- SHA-256 source hash;
- timestamp metadata;
- manifest validation;
- pinned mutual TLS;
- one-time authorization;
- explicit receiver consent/trusted-device policy;
- destination path sanitation/confinement;
- atomic destination reservation;
- collision-safe naming;
- free-space reserve check;
- resume offset negotiation;
- bounded chunk streaming;
- network idle timeout;
- exact manifest-bound source byte count;
- receiver partial staging;
- SHA-256 completion verification;
- constant-time hash compare;
- invalid checksum partial deletion;
- atomic final promotion;
- exact completion acknowledgement;
- local metadata history.

No received file is automatically opened.

## Resume behavior

Interrupted receives use `.swiftdrop.part` staging.

Resume rules:

- offset cannot be negative;
- offset cannot exceed manifest length;
- staged file must contain at least requested offset;
- unexpected staged tail beyond negotiated offset is truncated;
- sender source size must still equal manifest expected length;
- resume uses fresh pairing authorization rather than replaying old nonce;
- completion still requires full-file SHA-256 verification.

Pause is implemented safely as cancellation that leaves valid partial receiver staging; resume requires fresh pairing.

## Multi-file/folder transfer

Batch source builder:

- accepts selected files/folders;
- recursively enumerates folder files;
- performs complete source preflight before expensive hashing;
- bounds source count;
- bounds individual file size;
- bounds aggregate bytes;
- sanitizes/deconflicts relative paths;
- handles cancellation;
- hashes each source after preflight.

Receiver:

- validates/sanitizes complete manifest;
- rejects duplicate/colliding paths after portable normalization;
- presents batch preview;
- supports accept all/selective/reject;
- reserves accepted destinations;
- calculates aggregate remaining bytes;
- checks free capacity before accepting payload bytes;
- returns one plan per source;
- validates batch item order/path;
- stages/verifies each accepted item independently;
- records metadata results;
- returns final aggregate completion length.

Sender validates complete receiver plan before streaming selected files.

Empty directories are not represented in protocol v1 because the transfer model is file content + relative paths.

## Text/clipboard

Text transfer:

- explicit user-entered/shared text only;
- UTF-8 byte bound;
- short expiry bound;
- same pinned TLS + one-time authorization path;
- receiver explicit reject/accept/accept-and-copy choice;
- sender validates accepted zero-offset acknowledgement.

Clipboard:

- no continuous monitoring;
- read occurs only after explicit user paste action;
- copy occurs only after explicit receiver choice.

Text content is never written to transfer-history SQLite.

## Receive consent/trust

Incoming file preview includes:

- sender display name;
- authenticated sender certificate fingerprint;
- filename/path;
- declared size;
- extension risk warning.

Extension classifier provides warning assistance only and is not represented as malware scanning.

Trusted-device behavior:

- disabled auto-accept by default;
- exact device ID + exact canonical fingerprint binding;
- high/caution-risk content still requires conservative handling;
- trust can be revoked individually or cleared;
- identity reset clears local trust;
- malformed persisted trust rows are ignored.

## Queue

Outgoing file, batch and text operations route through `TransferQueueService`.

Features:

- configurable concurrency gate;
- queued/running/completed/failed/cancelled/interrupted states;
- local queue UI;
- cancellation propagation;
- privacy-aware in-memory labels;
- restart-safe metadata.

Persisted queue metadata intentionally stores only:

- generated queue row ID;
- generic label;
- state;
- created/started/finished timestamps;
- bounded machine error code.

It does not persist:

- source file paths;
- filenames;
- text contents;
- peer name/address/fingerprint;
- pairing invitations/nonces/codes;
- credentials/private keys;
- free-form exception messages.

On restart, stale `Queued`/`Running` rows become `Interrupted`; they are not automatically retried.

## Transfer history

History metadata:

- direction;
- peer label;
- file/description label;
- byte size;
- timestamp;
- status;
- integrity verification flag.

Features:

- retention pruning;
- retention zero disables/clears history according to Settings flow;
- individual deletion;
- clear all;
- privacy-mode peer/name redaction;
- language-neutral private marker at rest;
- localized presentation rows;
- malformed persisted row tolerance.

No file bytes/text contents are stored in history SQLite.

## Diagnostics

Diagnostics include:

- mDNS status;
- UDP fallback status;
- local-network troubleshooting;
- bounded local event log;
- safe export;
- developer self-tests when explicitly enabled.

Diagnostic privacy:

- messages single-line/bounded;
- machine code bounded;
- identifier redaction in privacy mode;
- older stored event messages are redacted at read/export time;
- corrupted rows skipped;
- no private keys/pairing nonces/transfer payload contents in intended diagnostic schema.

## Receive location

Windows supports native folder selection for receive/folder workflows.

Changing configured receive root:

- signals Settings change;
- stops/drains current listener lifetime;
- resolves new destination;
- restarts listener against new root;
- displays active root.

Other targets keep conservative application receive location unless an explicit supported platform mechanism exists.

## External platform input

Android:

- inbound text share;
- inbound single/multiple file share;
- bounded app-cache staging;
- explicit review before send.

Windows:

- native file drop;
- folder drop;
- text drop;
- pairing link drop;
- routed through `ExternalInputInbox` and normal transfer authorization.

iOS/Mac Catalyst:

- `swiftdrop://pair` activation;
- document/open-file file URL activation;
- temporary security-scoped access;
- Core bounded external-file staging;
- normal `ExternalInputInbox` review/send workflow.

Current Apple source does **not** include a dedicated Share Extension target.

Current Mac Catalyst source does **not** include a first-class native drop surface equivalent to the Windows implementation.

## Platform permissions/capabilities

Android:

- INTERNET/local sockets;
- ACCESS_NETWORK_STATE;
- ACCESS_WIFI_STATE;
- CHANGE_WIFI_MULTICAST_STATE;
- FOREGROUND_SERVICE;
- FOREGROUND_SERVICE_DATA_SYNC;
- POST_NOTIFICATIONS declaration;
- no broad storage permission;
- cleartext traffic disabled;
- application backup disabled.

Windows:

- `privateNetworkClientServer` only for peer network capability in current manifest;
- `swiftdrop` protocol registration;
- no general `internetClient` capability.

Apple:

- local-network usage description;
- Bonjour service;
- `swiftdrop` scheme;
- `public.data` document opening;
- documents-in-place declaration;
- Mac Catalyst app-sandbox/network client/network server entitlements.

## Notifications/background

Android:

- foreground data-sync service during active user-initiated transfer lifetime;
- required foreground status notification;
- optional generic completion/failure notification preference;
- notification permission requested only through explicit settings enable where required.

Other platforms:

- optional completion/failure system notification implementation is not claimed where it does not exist;
- SwiftDrop does not claim to bypass iOS/macOS lifecycle restrictions;
- no hidden background clipboard monitoring.

## About/open source/support

About/support surfaces include:

- SwiftDrop logo/product name;
- version/build information where available;
- `Made by the Sanskar`;
- Apache-2.0/open source details;
- repository/profile links;
- business/support contacts;
- technology/security/privacy description;
- optional Buy Me a Coffee support link.

Support payment never unlocks hidden product functionality.

## Localization

Current source contains English/Hindi localization architecture for:

- Main XAML;
- secondary pages;
- pairing dialogs;
- transfer status/errors;
- nearby pairing/manual code;
- Settings dialogs;
- Trusted Devices dialogs;
- Diagnostics dialogs;
- About errors;
- Queue/Nearby/Trusted dynamic counts;
- external platform input;
- identity recovery;
- incoming batch consent;
- history presentation.

CI validates key/placeholder parity.

Physical device layout/wrapping/accessibility validation is still required.

## Accessibility

Source includes:

- semantic headings/descriptions on key controls;
- dynamic interface sizing preference/resources;
- reduced-motion preference;
- light/dark/system theme handling;
- localization-friendly layouts;
- progress/status text.

Real TalkBack/VoiceOver/Narrator/keyboard/large-text/high-contrast validation remains external release work.

## Testing implemented in repository

Portable test coverage now includes, among other areas:

- attempt rate limiting;
- pairing code manager;
- one-time authorization store;
- pairing codec URI/JSON/clock rules;
- certificate generation/profile/policy;
- fingerprint canonicalization;
- local-address policy;
- discovery registry;
- mDNS query/announcement/parser/fuzz;
- filename sanitation/property behavior;
- path guard;
- destination reservation concurrency;
- manifest validation;
- batch source builder;
- batch aggregate limits;
- batch plan validator;
- incoming request policy;
- transfer response policy;
- strict framed JSON;
- frame truncation/fuzz boundaries;
- transfer engine integrity;
- transfer engine resume;
- source mutation;
- TLS pinning;
- mutual-TLS loopback file transfer/resume;
- settings validation;
- trust store;
- history store/maintenance/corruption tolerance;
- diagnostic store/redaction/corruption tolerance;
- database migrations;
- queue metadata persistence/restart behavior;
- external-file staging;
- transfer self-tests.

Configured tests are not described as passing in this session because no .NET runtime/workloads are available locally and no usable GitHub workflow status evidence was returned through the connector.

## Benchmarks

Synthetic benchmark project measures generated temporary data only:

- SHA-256 hashing throughput;
- batch manifest validation;
- portable filename/path sanitation.

It does not claim to represent complete Wi-Fi/TLS/disk end-to-end transfer performance.

Real peer-to-peer performance must be measured on representative target devices.

## CI/release engineering

Configured workflows include:

- portable Core build/test;
- localization validation;
- benchmark compile;
- CodeQL;
- dependency/repository hygiene;
- Android compile;
- Windows compile;
- Mac Catalyst compile;
- unsigned iOS Simulator compile;
- release-readiness aggregate gate.

Portable verify scripts align local validation with Core/localization/benchmark CI checks.

Repository security hygiene rejects categories such as:

- private signing material;
- local database artifacts;
- production environment files;
- embedded private-key blocks.

Production signing secrets remain outside the repository.

---

# Remaining source work

The following are genuine remaining source/integration areas. They are not falsely marked complete.

## Apple Share Extension

Current source supports document/open-file file URL intake on iOS/Mac Catalyst.

A dedicated Apple Share Extension target for arbitrary inbound files/text is not implemented.

If required, it must include:

- real extension target/bundle ID;
- signing/provisioning;
- App Group only if required;
- bounded intake;
- temporary security-scoped/resource access;
- cleanup;
- safe handoff to `ExternalInputInbox`;
- no direct automatic send;
- App Store privacy/entitlement validation.

## Native Mac Catalyst drag/drop

Windows native drag/drop is implemented.

A first-class Mac Catalyst drop surface for files/folders/text/pairing URLs remains source work if required.

It must reuse the existing bounded input/staging path and respect sandbox/security-scoped access.

## Complete application-protocol host integration tests

Core policy and TLS/transfer tests are strong, but full UI-independent request host coverage remains desirable for:

- authorization consume/replay across full request;
- file accept/reject;
- resume negotiation;
- batch full sequence;
- selective receive;
- text request sequence;
- connection close at protocol transitions;
- listener lifecycle/root restart.

## Further deterministic edge tests

Remaining useful cases include:

- staged partial mutation between negotiated offset and receiver stream open;
- receive-root changes with multiple active staged transfers;
- certificate replacement during active trusted-device interaction;
- platform-specific device/UNC/path separator boundaries;
- connection termination at every application protocol boundary.

---

# External validation still required

The following cannot honestly be completed by source edits alone.

## Automated evidence

- actual Actions run success for the exact release candidate;
- analyzer/build/test output;
- platform MAUI compile output;
- dependency audit result;
- CodeQL result.

Available GitHub connector status/run queries in this session returned no usable direct-main run contexts. Unknown is not pass.

## Physical-device transfer matrix

Required across supported directions:

- Windows ↔ Android;
- Android ↔ Android;
- Windows ↔ Windows;
- macOS ↔ Android;
- iOS ↔ Windows;
- iOS ↔ macOS;
- other claimed supported combinations.

## Network/environment behavior

- guest Wi-Fi client isolation;
- multicast blocked;
- Windows Firewall profiles;
- iOS local-network permission denied/allowed;
- Android background/vendor restrictions;
- IPv4/IPv6 combinations;
- network changes mid-transfer;
- sleep/lock;
- slow network;
- low storage;
- repeated incorrect pairing attempts.

## Platform storage/security lifecycle

- SecureStorage/keychain/keystore locked/unavailable behavior;
- uninstall/reinstall;
- device restore/migration;
- certificate policy upgrade;
- Apple security-scoped document providers;
- Mac sandbox document/network behavior under signing.

## Accessibility

- TalkBack;
- VoiceOver iOS/macOS;
- Narrator;
- keyboard-only navigation;
- large text;
- high contrast;
- reduced motion;
- focus order/semantic announcements.

## Signing/distribution

- Android release keystore/AAB/APK;
- Windows production signing/MSIX;
- Apple Developer signing/provisioning/TestFlight/notarization/store;
- clean install/update/uninstall;
- final store privacy/data declarations;
- screenshots/metadata from final signed binaries;
- exact release dependency/license inventory.

---

# Environment and connector limitations

## Local build environment

The active chat execution environment does not provide the required .NET 10/.NET MAUI SDK/workloads for a trustworthy local project build/test run.

Therefore this ledger does **not** claim local compilation/tests passed.

## GitHub workflow evidence

GitHub combined status/workflow run lookups available during the implementation returned no usable direct-main contexts/runs for queried commits.

Therefore this ledger records CI state as unknown/unreported from this connector, not green and not failed.

## Commit email

The GitHub contents connector creates commits as the connected GitHub identity and does not expose explicit Git author/committer email control.

Every focused commit created through this work uses:

`Signed-off-by: Sanskar <sanskarin@outlook.in>`

This preserves the requested email in commit trailers without falsely claiming the connector changed immutable author metadata.

---

# Release quality rule

SwiftDrop must not be described as bug-free, production-verified, store-ready, or fully validated only because the source implementation is extensive.

A production release requires all of the following:

1. green automated build/test/security evidence for the exact candidate;
2. platform MAUI compilation;
3. physical cross-device transfer matrix;
4. hostile/restricted network tests;
5. storage/background/sleep/lock tests;
6. accessibility validation;
7. signed package install/update validation;
8. store privacy/policy review;
9. measured performance rather than guessed claims;
10. documentation synchronized with the final binary.

Current engineering state: **substantially implemented in source, with remaining Apple extension/native-drop work and external release validation still open.**

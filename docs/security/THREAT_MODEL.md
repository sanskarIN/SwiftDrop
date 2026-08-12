# SwiftDrop Threat Model

Updated: 2026-08-12

## Scope

SwiftDrop transfers user-selected files and explicit text snippets directly between nearby devices on a local network. The current source has no SwiftDrop account system, SwiftDrop-operated relay/cloud-upload path, advertising identifier, analytics path, or continuous clipboard collection.

Platform share/open/drop surfaces may stage explicitly supplied content locally before the user reviews it. On Apple platforms, the Share Extension and containing app exchange a bounded package through the configured App Group. This is local platform storage, not a network relay.

## Assets

- User-selected file and text contents.
- Device identity certificate and private key.
- Short-lived pairing invitations, one-time codes, and nonces.
- Trusted-device certificate fingerprints.
- Local transfer/history/diagnostic/queue metadata.
- Schema-v3 completed-batch resume metadata.
- Receive destination paths and `.swiftdrop.part` staged transfer files.
- Temporary Android/Apple external-input cache files.
- Temporary Apple App Group Share Extension packages.

## Security goals

- Keep transfer content confidential from passive local-network observers.
- Bind a transfer to the receiver certificate advertised/confirmed during pairing.
- Present an authenticated sender-certificate identity and explicit receiver decision for untrusted incoming content.
- Detect tampering/corruption before a received file is finalized.
- Require explicit user involvement for pairing and incoming acceptance except the narrowly configured trusted-device normal-risk auto-accept policy.
- Prevent traversal or link-based redirection outside the approved receive root.
- Prevent selected outgoing source trees from escaping through symbolic links/reparse points.
- Prevent replay of one-time transfer authorization after consumption/expiry.
- Prevent malformed path metadata from consuming a valid one-time authorization.
- Prevent completed-batch retry metadata from becoming transfer authorization or a false-completion shortcut.
- Reject ambiguous/extra/non-canonical protocol and pairing representations instead of silently interpreting aliases.
- Keep local external-input staging bounded by file count, per-file bytes, aggregate bytes, and storage reserve.
- Never auto-open received files and never auto-send shared/dropped/opened content.
- Minimize persistent sensitive metadata and never persist reusable pairing authorization.

## Threats and mitigations

### Passive network observer

Mitigation: TLS 1.2/1.3 is provided by the .NET/platform cryptographic stack. The sender pins the receiver certificate fingerprint from the validated pairing invitation where a pin is expected. SwiftDrop does not implement custom encryption or a custom key exchange.

Residual boundary: LAN observers/administrators can still observe network metadata such as addresses, timing, and traffic volume.

### Active local-network attacker

Mitigation: receiver-certificate pinning prevents silent receiver substitution when the authentic pairing capability/fingerprint reaches the sender. The sender presents its local P-256 ECDSA client certificate, and the receiver derives the sender fingerprint from the TLS channel before consent/trust checks. Pairing/transfer destinations are constrained to numeric loopback/private/unique-local/link-local addresses rather than public Internet or DNS targets.

Residual risk: possession of a still-valid unconsumed invitation is a temporary authorization factor. Users should compare fingerprints for sensitive transfers.

### Pairing-link replay, aliasing, or brute-force pressure

Mitigation: pairing payloads expire and contain cryptographically random one-time nonces. Transfer authorization is consumed atomically after valid request shape/path metadata and authenticated client-certificate availability. Short numeric pairing codes are time-bounded and are not long-term passwords. Inbound connection attempts and pairing attempts are separately bounded/rate-limited.

Pairing capability parsing is canonical and fail-closed:

- surrounding whitespace is rejected;
- outer URI structure is exact;
- exactly one raw `p=` query field is allowed;
- empty/unknown/duplicate query fields are rejected;
- payload text must be unpadded canonical Base64URL (`A-Z`, `a-z`, `0-9`, `-`, `_`);
- `+`, `/`, `=`, percent-encoded aliases, and non-canonical re-encodings are rejected;
- decoded pairing JSON rejects duplicate and unknown fields.

This reduces representation ambiguity around a short-lived capability and makes logging/testing/comparison reason about one accepted textual form.

Residual risk: limits reduce pressure but do not make a hostile LAN harmless. Host firewall/network isolation/OS policy remain important boundaries.

### Ambiguous, malformed, or smuggled protocol metadata

Mitigation: framed JSON has bounded length/depth, strict UTF-8/JSON parsing, no comments/trailing commas, case-insensitive duplicate-property rejection, **unknown/unmapped-member rejection**, and type-specific request-shape validation. Cross-type stray fields are rejected rather than silently ignored. File/batch/text/pair requests use shared Core wire records/factories/validators.

Pairing URI and decoded pairing JSON are also treated as untrusted input. Pairing decoding enforces bounded depth, strict JSON, duplicate-property rejection, **unknown-member rejection**, canonical raw Base64URL transport, URI structure, exact protocol version, local numeric address policy, bounded identity metadata, canonical SHA-256 fingerprint, nonce syntax, port, and expiry/lifetime.

Batch transfer IDs are canonical bounded ASCII tokens (letters, digits, `-`, `_`) rather than arbitrary free-form strings used as persistence/resume keys.

### One-time authorization confusion or replay

Mitigation: `ProtocolSessionAuthorizer` validates request shape and complete manifest metadata before consuming transfer authorization. Pair requests do not consume transfer nonces. Transfer nonces are memory-bounded, exact-expiry, atomic single-use values. Replays fail. Identity reset/regeneration clears active authorization.

File/batch paths are structurally/canonically validated before nonce consumption. Rooted paths, traversal, empty segments, noncanonical separators/sanitization aliases, and excessive depth therefore do not burn an otherwise valid one-time capability.

Authorization is never persisted in queue metadata, completed-batch resume metadata, history, diagnostics, or Apple App Group handoff data.

### Malicious filename, path traversal, collision, or link redirection

Mitigation:

- `/` is the canonical wire separator; peer-supplied `\\` aliases are rejected at the manifest boundary;
- rooted paths are rejected, including portable Windows-root syntax on non-Windows hosts;
- empty/repeated/trailing separators and `.`/`..` traversal are rejected;
- relative paths are limited to 64 segments and bounded total metadata length;
- every incoming manifest path must already equal SwiftDrop's canonical sanitized form;
- filename segments are Unicode NFC-normalized and portable-invalid/control characters are removed during sender/local intake construction;
- Windows reserved device names are neutralized;
- filename segments are capped at 180 UTF-16 code units **and 180 UTF-8 bytes**, without splitting a surrogate pair/rune;
- the byte cap leaves headroom for `.swiftdrop.part` on common 255-byte component filesystems;
- batch destinations that collide after sanitation/case/Unicode normalization are deconflicted before hashing and revalidated before transmission/receive;
- collision-generated names remain bounded, and a prefix marker is used when an end suffix would be truncated away;
- final paths must resolve under the configured receive root;
- existing symlink/reparse components beneath the receive root are rejected around staging and final promotion;
- concurrent incoming sessions reserve destinations atomically;
- final promotion does not silently overwrite an existing completed destination.

Residual boundary: filesystem semantics differ by platform/filesystem. Signed physical-target tests remain required.

### Outgoing source-tree escape or source substitution

Mitigation:

- single-file sends require a regular non-link/non-reparse source;
- that regular-source check is repeated at the actual stream-open boundary;
- selected folder roots that are symbolic links/reparse points are rejected;
- recursive folder enumeration is explicit/bounded rather than unrestricted `AllDirectories` traversal;
- descendant linked/reparse files/directories are rejected;
- enumerated folder files are sorted by normalized relative path for deterministic resume manifests;
- source path count, per-file bytes, aggregate bytes, and relative-path length are preflighted before hashing;
- paused resume candidates retain only sources that are still regular non-link/non-reparse paths.

A file swapped to a link between user selection and streaming therefore fails at the last-mile source check rather than causing SwiftDrop to intentionally follow it.

Residual boundary: a fully compromised local OS can race filesystem identity checks below application granularity.

### Corrupted, truncated, or changed transfer source

Mitigation: outgoing streams are bound to manifest-declared source length. The sender rechecks source size at stream open and after bounded streaming. The receiver accepts exactly the expected remaining bytes into `.swiftdrop.part`, truncates unexpected staged tails to the negotiated resume offset, verifies SHA-256 over the completed staged file, then performs non-overwrite final promotion. Integrity failure removes invalid staging rather than presenting a completed file.

Source growth/shrinkage after manifest construction cannot silently change wire framing. Source content changed without a length change will fail receiver SHA-256 verification because the manifest hash and streamed bytes differ. Network interruption leaves either verified final data or explicit staged/incomplete state.

Applying optional last-write timestamp metadata happens after verified promotion and is best-effort; a metadata-setting failure does not falsely mark verified file contents as transfer failure.

### Completed-batch retry confusion

Mitigation: an interrupted batch keeps a stable transfer ID only for that resume attempt lineage. The active MainPage batch controls call the stable-ID coordinator API directly; the obsolete compatibility overload that generated a fresh ID implicitly has been removed. Schema-v3 completed-item metadata stores transfer ID, canonical source relative path, hashed receive-root identity, effective local destination relative path, length/SHA-256, and completion time. It does not store authorization or the absolute receive root.

Before SwiftDrop offers a full-length resume offset for an already-finalized item, it requires the same transfer/root/source/length/hash, resolves the destination beneath the receive root, rejects reparse-path destinations, confirms the file exists at the expected length, and re-hashes it. Missing/mutated/mismatched data invalidates the optimization and must not be treated as a completed item. A new explicit send uses a new transfer ID so ordinary duplicate-send collision behavior remains intact.

After the sender returns the matching item-start frame, SwiftDrop performs the completed-file verification **again immediately before sending the zero-byte completion ACK**. A removal, replacement, mutation, reparse/path change, or completion-record mismatch between plan creation and acknowledgement therefore fails closed instead of being falsely acknowledged.

Residual TOCTOU boundary: a local process already authorized to modify the receive filesystem can still race checks at finer granularity. Repeated checks narrow that window but cannot defend against an endpoint/OS fully controlled by an attacker. Release/security testing should mutate/remove completed destinations around retry transitions and confirm fail-closed behavior.

### Storage exhaustion

Mitigation: per-file/count/aggregate batch limits are enforced. Senders preflight source count/size/path limits before expensive hashing. Receivers independently validate declared totals and preflight remaining destination capacity.

External staging uses a reusable budget that tracks committed file count, per-file maximum, and aggregate bytes. A budget is committed only after exact copy success, so a failed copy does not falsely consume/permit later bytes.

Android unknown-length providers are bounded by the remaining staging budget and recheck destination free-space reserve while streaming. Negative provider length metadata is treated as unknown rather than as a valid negative size. Apple Share Extension checks the aggregate budget before starting the file copy that would exceed it. Apple containing-app import sums the validated package and preflights cache capacity before recopying files. Mac drop uses the same staging-budget policy.

Residual risk: free capacity can still change after/between checks. Real low-storage testing remains required.

### Dangerous received file

Mitigation: executable/script/installer/macro-enabled/archive-like extensions are classified for user warning. SwiftDrop never automatically opens or executes received files.

Residual risk: extension classification is not malware scanning and cannot prove content is safe. Endpoint security/provenance/user judgment remain relevant.

### Trusted-device substitution

Mitigation: trust is stored against device ID plus canonical SHA-256 certificate fingerprint. Display name alone is insufficient. Trust can be revoked. Identity reset clears local trust. Unusable local certificate recovery creates a new identity rather than silently inheriting old trust. Automatic normal-file acceptance is opt-in and defaults off.

### Local identity key failure or expiry

Mitigation: stored identity certificates must retain the private key, expected ECDSA key type, and acceptable validity window. Corrupt/expired/unusable identity state creates a new device ID/certificate and surfaces a re-pair notice. Private-key material stays in platform secure storage and never enters protocol JSON, QR links, SQLite metadata, diagnostics, source, or Share Extension packages.

### Apple Share Extension / App Group package tampering

Mitigation:

- app and extension use a dedicated shared App Group entitlement validated by repository tooling;
- packages use a versioned strict Core manifest;
- file count/per-file/aggregate/text/time bounds are enforced;
- unknown manifest members are rejected;
- filenames are sanitized/deconflicted with UTF-8/character bounds and collision-marker preservation;
- provider callbacks have a bounded **response** wait and extension-lifetime cancellation;
- timed-out/cancelled late callbacks cannot begin a fresh staging copy;
- active file-copy loops observe extension-lifetime cancellation without incorrectly applying the response timer to an already-started legitimate copy;
- extension applies aggregate staging budget before copying an over-limit item;
- extension publication is atomic from staging to pending state;
- containing app serializes package imports;
- containing app validates package-directory identity, package age, manifest/file exact lengths, root confinement, and symlink/reparse status;
- physical `files/` entries must match the manifest exactly: undeclared files, nested directories, duplicate portable names, and missing declared files are rejected;
- containing app preflights aggregate app-cache capacity before re-staging validated files;
- containing app re-stages accepted package files into app cache before normal review;
- malformed packages are rejected/cleaned rather than transferred;
- shared content is never automatically sent.

Residual boundary: App Group access/provisioning/sandbox enforcement and `NSItemProvider` callback/lifetime behavior are platform-controlled and must be validated in signed builds with real providers. An endpoint process with equivalent platform privileges remains outside SwiftDrop's protection boundary.

### Android share and desktop drop abuse

Mitigation: Android content-URI intake is count/per-file/aggregate bounded, portable-name sanitized, streamed with runtime byte caps, exact-length checked when declared, storage reserve rechecked for unknown-length content, cleaned on failure, and handed to the normal review path atomically. Negative/unknown provider size is not trusted as a length. Mac drop uses shared staging budgets, security-scoped access, linked-source rejection, bounded provider-response waits, and review-before-send. Windows drop remains explicit user input and passes normal regular-source/link/manifests checks before send.

### Local metadata disclosure

Mitigation: SQLite stores bounded metadata only. Privacy mode replaces peer/file history labels with generic markers and applies structured diagnostic redaction. Queue persistence stores generic state/timestamps/error codes without source paths/text/addresses/pairing capabilities. Completed-batch metadata stores a hashed receive-root identity rather than the absolute receive root and contains no transfer authorization.

Residual boundary: a process/user with access to the application data directory can inspect local metadata and cache files according to OS permissions.

### Denial of service

Mitigation: source/certificate attempt limits, bounded metadata, path-depth/size limits, bounded discovery records, bounded authorization stores, file/batch/text/share-package limits, provider-response timeouts, transfer staging budgets, fixed transfer chunks, exact-length accounting, idle timeouts, cancellation, active-session draining, deterministic bounded folder traversal, and stale-cache/package cleanup constrain several exhaustion paths.

Residual risk: a hostile LAN or compromised endpoint can still consume resources. SwiftDrop does not bypass firewall, MDM, client isolation, or OS lifecycle controls.

### Compromised endpoint

Out of scope: SwiftDrop cannot protect content from malware, a rooted/jailbroken/fully compromised device, an attacker controlling the OS, or another process already authorized to read/modify selected or received files.

## Trust decisions

A certificate fingerprint is a device-identity signal, not proof of a person's identity. Users should confirm the fingerprint shown by the intended nearby device before establishing trust. A reinstalled/rekeyed device with a new certificate must be paired/trusted again.

## Out of scope for the current release

- Internet relay or cloud synchronization.
- Account recovery/account identity.
- Remote transfers outside the LAN.
- Endpoint malware remediation.
- Enterprise identity federation.
- Automatic antivirus/content-safety guarantees.
- Bypassing firewall, guest-network isolation, MDM, sandbox, background, or store policy.
- Guaranteeing arbitrary mobile sockets survive suspension.

## Validation boundary

Source controls and portable tests are not equivalent to production validation. Release readiness still requires successful automated gates for the exact candidate, signed package tests, physical cross-device transfer matrices, restricted-network cases, real low-storage/lifecycle cases, Apple App Group/Share Extension provider and sandbox validation, Android content-provider behavior, accessibility/localization checks, and platform secure-storage/restore behavior.

## Security reporting

Report vulnerabilities privately to `sanskarin@outlook.in`. Do not publish pairing invitations, private certificates, real transferred files, or exploit details in public issues before coordinated disclosure.

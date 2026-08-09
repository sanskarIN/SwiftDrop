# SwiftDrop Threat Model

## Scope

SwiftDrop transfers user-selected files and explicit text snippets directly between nearby devices on a local network. The current release has no SwiftDrop account system, relay server, cloud upload path, advertising identifier, analytics path, or continuous clipboard collection.

## Assets

- User-selected file and text contents.
- Device identity certificate and private key.
- Short-lived pairing invitations, one-time codes, and nonces.
- Trusted-device certificate fingerprints.
- Local transfer/history/diagnostic/queue metadata.
- Receive destination paths and staged partial transfer files.

## Security goals

- Keep transfer content confidential from passive local-network observers.
- Bind a transfer to the receiver certificate advertised/confirmed during pairing.
- Present a sender certificate and explicit receiver decision for untrusted incoming content.
- Detect tampering or corruption before a received file is finalized.
- Require explicit user involvement for pairing and incoming transfer acceptance except the narrowly configured trusted-device auto-accept policy.
- Prevent traversal outside the approved receive root.
- Prevent replay of one-time pairing authorization after it is consumed or expired.
- Avoid auto-opening received content.
- Minimize persistent sensitive metadata and never persist reusable pairing authorization.

## Threats and mitigations

### Passive network observer

Mitigation: TLS 1.2/1.3 is provided by the .NET/platform cryptographic stack. The sender pins the receiver certificate fingerprint from the validated pairing invitation. SwiftDrop does not implement custom encryption or a custom key exchange.

### Active local-network attacker

Mitigation: receiver certificate pinning prevents silent substitution when the pairing invitation is authentic. The sender presents its local P-256 ECDSA certificate, and the receiver obtains that fingerprint from the TLS channel before consent/trust checks. Pairing and transfer destinations are constrained to numeric loopback/private/unique-local/link-local addresses rather than public Internet or DNS targets.

Residual risk: if an attacker steals or views a still-valid pairing invitation before it is used, possession of that invitation is a temporary authorization factor. Users should compare certificate fingerprints when transferring sensitive content.

### Pairing-link replay or brute-force pressure

Mitigation: pairing payloads expire and contain cryptographically random one-time nonces. The receiver consumes a nonce atomically. Short pairing codes are time-bounded and are not long-term credentials. Inbound connection attempts are rate-limited by network source, and pairing requests have additional certificate-oriented attempt limits.

Residual risk: rate limits reduce abuse but do not make a hostile LAN harmless. Host firewall, network isolation, and OS policy remain important boundaries.

### Ambiguous or malformed protocol metadata

Mitigation: framed JSON has bounded length, bounded nesting depth, strict UTF-8/JSON parsing, no comments/trailing-comma tolerance, and case-insensitive duplicate-property rejection. Batch/file/text metadata then passes type-specific validation, count/size limits, timestamps, fingerprint/hash validation, and path checks before payload bytes are accepted.

Pairing URI parsing independently rejects unexpected outer authority/path/query fields, unsupported versions, public/DNS addresses, malformed fingerprints/nonces, and invalid lifetimes. A defensive consistency change to reuse the shared duplicate-property guard inside the encoded pairing JSON remains a tracked hardening item because the repository connector blocked that source replacement during this implementation session; the existing pairing field validation remains active.

### Malicious filename or path

Mitigation: rooted paths and traversal segments are rejected. Names are Unicode-normalized, portable-invalid characters are removed, Windows reserved device names are neutralized, and batch manifests reject destinations that collide after sanitation/case-folding. Final paths must resolve under the configured receive root. Concurrent incoming transfers reserve destination paths atomically to prevent same-name race collisions.

### Corrupted, truncated, or changed transfer source

Mitigation: outgoing streams are bounded to the manifest-declared source length. The receiver enforces exact expected length and stages data as `.swiftdrop.part`. SHA-256 is verified over the complete staged file before final rename. Integrity failure removes invalid partial data; network interruption can leave a bounded verified resume staging point. Source growth/shrinkage after manifest creation cannot silently change protocol framing.

### Storage exhaustion

Mitigation: per-file and aggregate batch limits are enforced. The sender preflights batch count/size before hashing; the receiver validates declared aggregate totals and preflights remaining destination capacity before accepting batch bytes. Real low-storage behavior still requires target-device validation because platform filesystems can change available capacity during a transfer.

### Dangerous received file

Mitigation: executable, script, installer, macro-enabled, and archive/container-like extensions are classified for user warning. SwiftDrop never automatically opens or executes a received file.

Residual risk: extension-based classification is not malware scanning and cannot prove that a file is safe. Operating-system protections, endpoint security, provenance, and user judgment remain relevant.

### Trusted-device substitution

Mitigation: trust is stored against a device ID and canonical SHA-256 certificate fingerprint. A display name is never sufficient. Trust can be revoked locally, reset identity clears local trust, and unusable/expired local certificate recovery generates a new identity rather than silently inheriting the old trusted identity. Automatic normal-file acceptance for trusted devices is opt-in and defaults off.

### Local identity key failure or expiry

Mitigation: the local identity certificate must have a private key, supported ECDSA key, and acceptable validity window. SwiftDrop renews/recreates unusable identities according to the documented policy and shows the user when identity regeneration means peers must pair again. Private-key material is stored through platform secure storage and is never placed in pairing metadata.

### Compromised endpoint

Out of scope: SwiftDrop cannot protect content from malware, a rooted/jailbroken device, an attacker controlling the operating system, or another process already authorized to read selected/received files.

### Denial of service

Mitigation: source/certificate attempt rate limits, framed metadata limits, file/batch size limits, bounded chunks, connection/idle timeouts, cancellation, bounded discovery records, and one-time authorization constrain several resource-exhaustion paths.

Residual risk: an attacker controlling the LAN or endpoint can still create pressure. SwiftDrop does not attempt to bypass host firewalls, enterprise controls, Wi-Fi client isolation, or OS background limits.

### Local metadata disclosure

Mitigation: SQLite stores metadata only. Privacy mode replaces filename-oriented history with generic labels and redacts identifier-like diagnostic tokens. Restart-safe queue persistence stores a generic `Transfer` label, state/timestamps, and a bounded machine-oriented error code; it does not persist source paths, text, peer addresses, pairing invitations, nonces, credentials, or free-form exception messages.

## Trust decisions

A certificate fingerprint is a device identity signal, not proof of a person's identity. Users must confirm that the fingerprint shown by the peer corresponds to the intended nearby device before treating it as trusted. A replaced/reinstalled device with a new certificate must be paired/trusted again.

## Out of scope for the current release

- Internet relay or cloud synchronization.
- Account recovery or account identity.
- Remote transfers outside the LAN.
- Endpoint malware remediation.
- Enterprise identity federation.
- Automatic antivirus or content-safety claims.
- Bypassing firewall, guest-network isolation, MDM, background, or store policy.

## Validation boundary

Source controls and automated tests are not equivalent to production validation. Release readiness still requires target-platform compilation, signed-package tests, physical-device transfer matrices, restricted-network cases, low-storage cases, accessibility tests, and platform secure-storage/restore behavior.

## Security reporting

Report vulnerabilities privately to `sanskarin@outlook.in`. Do not publish pairing invitations, private certificates, real transferred files, or exploit details in public issues before coordinated disclosure.

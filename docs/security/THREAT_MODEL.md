# SwiftDrop Threat Model

## Scope

SwiftDrop transfers user-selected files and text directly between nearby devices on a local network. The current release has no SwiftDrop account system, relay server, cloud upload path, or continuous clipboard collection.

## Assets

- User-selected file and text contents.
- Device identity certificate and private key.
- Short-lived pairing invitations and nonces.
- Trusted-device certificate fingerprints.
- Local transfer history metadata.
- Receive destination paths and partial transfer files.

## Security goals

- Keep transfer content confidential from passive local-network observers.
- Detect tampering or corruption before a received file is finalized.
- Require explicit user involvement for pairing and incoming transfer acceptance.
- Prevent traversal outside the approved receive root.
- Prevent replay of a one-time pairing invitation after it is consumed or expired.
- Avoid auto-opening received content.
- Minimize persistent sensitive metadata.

## Threats and mitigations

### Passive network observer

Mitigation: TLS 1.2/1.3 is provided by the .NET/platform cryptographic stack. The sender pins the receiver certificate fingerprint from the pairing invitation. SwiftDrop does not implement custom encryption.

### Active local-network attacker

Mitigation: receiver certificate pinning prevents an attacker from silently substituting an arbitrary receiver certificate when the pairing invitation is authentic. The sender also presents a device certificate, and the receiver exposes its fingerprint during explicit incoming-transfer approval.

Residual risk: if an attacker steals or views a still-valid pairing invitation before it is used, possession of the invitation is a temporary authorization factor. Users should compare certificate fingerprints when transferring sensitive content.

### Pairing-link replay

Mitigation: pairing payloads expire and contain cryptographically random one-time nonces. The receiver consumes a nonce atomically before accepting the transfer request.

### Malicious filename or path

Mitigation: rooted paths, invalid paths, and paths resolving outside the receive root are rejected. Transfers are staged as `.swiftdrop.part` files and finalized only after integrity verification.

### Corrupted or truncated transfer

Mitigation: expected length is enforced while streaming and SHA-256 is verified over the completed partial file before final rename.

### Dangerous received file

Mitigation: executable, script, installer, macro-enabled, and archive-like extensions are classified for user warning. SwiftDrop never automatically opens a received file.

Residual risk: extension-based classification cannot prove that a file is safe. The operating system, endpoint protection, file provenance, and user judgment remain relevant.

### Compromised endpoint

Out of scope: SwiftDrop cannot protect content from malware, a rooted/jailbroken device, an attacker controlling the operating system, or another process already authorized to read the selected/received file.

### Denial of service

Mitigation: protocol frame length, file length, cancellation, bounded chunks, and one-time authorization limit several resource-exhaustion paths.

Residual risk: a hostile device on the LAN can still create connection pressure. Production hardening should include connection-rate limits and platform firewall guidance.

### Local metadata disclosure

Mitigation: transfer history stores metadata only and supports privacy mode to replace filenames with a generic label. File contents are never stored in SQLite.

## Trust decisions

A certificate fingerprint is a device identity signal, not proof of a person's identity. Users must confirm that the fingerprint shown by the peer corresponds to the intended nearby device before treating it as trusted.

## Out of scope for the current release

- Internet relay or cloud synchronization.
- Account recovery.
- Remote transfers outside the LAN.
- Endpoint malware remediation.
- Enterprise identity federation.
- Automatic antivirus claims.

## Security reporting

Report vulnerabilities privately to `sanskarin@outlook.in`. Do not publish pairing invitations, private certificates, real transferred files, or exploit details in public issues before coordinated disclosure.

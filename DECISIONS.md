# SwiftDrop Architectural Decisions

Updated: 2026-08-09

## ADR-001 — Local-first, account-free current release

**Decision:** Current SwiftDrop transfers remain device-to-device over the local network. No SwiftDrop cloud upload, account, email, phone number, or subscription backend is required.

**Reason:** This is the product privacy boundary and keeps local transfers independent of an internet service.

## ADR-002 — Established cryptography only

**Decision:** Use .NET/platform TLS and SHA-256. Do not create a custom encryption algorithm.

**Reason:** Custom cryptography would add avoidable security risk. Receiver certificate fingerprints are verified out of band through pairing, and senders present client certificates.

## ADR-003 — One-time pairing authorization

**Decision:** Pairing invitations contain a short-lived cryptographically random nonce and receiver certificate fingerprint. The receiver atomically consumes the nonce once.

**Reason:** Prevent invitation replay and bind the transfer to a user-initiated pairing window.

## ADR-004 — Explicit consent before receive

**Decision:** Incoming files and text require receiver consent unless an explicitly trusted device matches its stored certificate fingerprint and the user has enabled the conservative auto-accept option. High-risk and caution file types never use silent auto-accept.

**Reason:** Device display names are not authentication and transferred files may be harmful after receipt.

## ADR-005 — Stream files, store metadata only

**Decision:** File bytes stream between network and filesystem. SQLite stores metadata such as trust and history; it does not store transferred file contents.

**Reason:** Avoid duplicate storage, reduce database size, and keep the privacy model simple.

## ADR-006 — Staged receive with final integrity check

**Decision:** Receive into `.swiftdrop.part`, resume only within the declared length, hash the completed partial file, and move it into place only after SHA-256 succeeds.

**Reason:** A cancelled, corrupted, or incomplete transfer must not look like a completed file.

## ADR-007 — Layered discovery

**Decision:** Use nearby local discovery with mDNS/Bonjour where platform support is available, UDP broadcast fallback where permitted, plus QR/manual fallback. Discovery entries expire automatically.

**Reason:** Local-network APIs and permissions differ by platform and network policy.

## ADR-008 — No broad filesystem permission for baseline flow

**Decision:** Prefer system file/document pickers and application/user-approved receive locations instead of broad storage access.

**Reason:** Least privilege and store-policy compatibility.

## ADR-009 — Privacy-aware history and diagnostics

**Decision:** History stores metadata only and can hide filenames. Diagnostics must not contain file contents, text snippet contents, private keys, full pairing invitations, or one-time nonces.

**Reason:** Troubleshooting should not create a second sensitive-data sink.

## ADR-010 — Conservative dangerous-file handling

**Decision:** Extension-based risk classification is only a warning signal. SwiftDrop never claims that this is malware detection and never auto-opens received content.

**Reason:** File extensions are not a security scanner and supported OS malware APIs vary.

## ADR-011 — Focused commits and explicit incomplete-platform status

**Decision:** Keep changes in focused commits with a `Signed-off-by: Sanskar <sanskarin@outlook.in>` trailer when connector write APIs cannot set Git author email metadata directly. Track source-complete versus physically verified work separately.

**Reason:** Reviewability and truthful release status are more important than a single large commit or unsupported verification claims.

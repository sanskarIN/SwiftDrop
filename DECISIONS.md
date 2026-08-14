# SwiftDrop Architectural Decisions

Updated: 2026-08-14

## ADR-001 — Local-first, account-free current release

**Decision:** Current SwiftDrop transfers remain device-to-device over the local network. No SwiftDrop cloud upload, account, email, phone number, or subscription backend is required.

**Reason:** This is the product privacy boundary and keeps local transfers independent of an internet service.

## ADR-002 — Established cryptography only

**Decision:** Use .NET/platform TLS and SHA-256. Do not create a custom encryption algorithm.

**Reason:** Custom cryptography would add avoidable security risk. Receiver certificate fingerprints are verified out of band through pairing, and senders present client certificates.

## ADR-003 — One-time pairing authorization

**Decision:** Pairing invitations contain a short-lived cryptographically random nonce and receiver certificate fingerprint. The receiver atomically consumes the nonce once after strict request/manifest validation and authenticated client-certificate presence.

**Reason:** Prevent invitation replay, bind the transfer to a user-initiated pairing window, and avoid burning valid authorization on malformed/unauthed requests.

## ADR-004 — Explicit consent before receive

**Decision:** Incoming files and text require receiver consent unless an explicitly trusted device matches its stored certificate fingerprint and the user has enabled the conservative auto-accept option. High-risk and caution file types never use silent auto-accept.

**Reason:** Device display names are not authentication and transferred files may be harmful after receipt.

## ADR-005 — Stream files, store metadata only

**Decision:** File bytes stream between network and filesystem. SQLite stores metadata such as trust, history, queue state, and bounded verified batch-resume metadata; it does not store transferred file/text contents.

**Reason:** Avoid duplicate storage, reduce database size, and keep the privacy model simple.

## ADR-006 — Staged receive with final integrity check

**Decision:** Receive into `.swiftdrop.part`, resume only within the declared length, hash the completed partial file, and move it into place only after SHA-256 succeeds. Final promotion is non-overwriting and receive-root symlink/reparse confinement is rechecked.

**Reason:** A cancelled, corrupted, redirected, overwritten, or incomplete transfer must not look like a completed file.

## ADR-007 — Layered discovery

**Decision:** Use nearby local discovery with mDNS/Bonjour where platform support is available, UDP broadcast fallback where permitted, plus QR/manual fallback. Discovery entries expire automatically.

**Reason:** Local-network APIs and permissions differ by platform and network policy.

## ADR-008 — No broad filesystem permission for baseline flow

**Decision:** Prefer system file/document pickers and application/user-approved receive locations instead of broad storage access.

**Reason:** Least privilege and store-policy compatibility.

## ADR-009 — Privacy-aware history and diagnostics

**Decision:** History stores metadata only and can redact peer/file identifiers. Diagnostics must not contain file/text contents, private keys, full pairing invitations, one-time nonces, or reusable authorization.

**Reason:** Troubleshooting should not create a second sensitive-data sink.

## ADR-010 — Conservative dangerous-file handling

**Decision:** Extension-based risk classification is only a warning signal. SwiftDrop never claims that this is malware detection and never auto-opens received content.

**Reason:** File extensions are not a security scanner and supported OS malware APIs vary.

## ADR-011 — Focused commits and explicit incomplete-platform status

**Decision:** Keep changes in focused commits with a `Signed-off-by: Sanskar <sanskarin@outlook.in>` trailer when connector write APIs cannot independently set Git author email metadata. Track source-complete versus physically verified work separately.

**Reason:** Reviewability and truthful release status are more important than one large commit or unsupported verification claims.

## ADR-012 — Canonical protocol capabilities and file paths

**Decision:** Pairing capability text and file manifest paths have one canonical wire representation. Pairing uses exact `swiftdrop://pair?p=` structure plus unpadded Base64URL. File manifest paths use `/` only, reject rooted/traversal/empty/deep/noncanonical aliases, and are validated before one-time authorization is consumed.

**Reason:** Alias representations can create authorization/replay ambiguity, cross-platform path drift, and inconsistent resume identities. Canonical wire identity makes sender/receiver negotiation deterministic across Windows and Unix-like filesystems.

## ADR-013 — Stable batch IDs with verified completed-item reuse

**Decision:** A paused/failed batch retains its random stable transfer ID. Completed-item metadata is only a resume optimization: the receiver must re-confine and re-hash the finalized file when building the retry plan and verify it again immediately before a zero-byte completion acknowledgement.

**Reason:** Stable IDs avoid duplicate collision-renamed files on retry, while repeated physical-file verification prevents stale metadata or a retry-plan/ACK race from falsely acknowledging changed content.

## ADR-014 — Shared external staging budget and review-before-send

**Decision:** Android shares, the iOS Share Extension, and Mac native drop use shared count/per-file/aggregate staging-budget policy. Failed staging does not consume budget. External input is staged for explicit review and never becomes an automatic-send command.

**Reason:** Platform provider APIs differ, but resource-exhaustion and consent boundaries should remain consistent.

## ADR-015 — iOS-only Share Extension; Mac Catalyst native-drop desktop path

**Decision:** `SwiftDrop.ShareExtension` targets iOS only. Mac Catalyst external intake is implemented by the containing desktop app through native `UIDropInteraction` and normal document/file flows.

**Reason:** The current .NET 10 Apple SDK supports the required iOS app-extension path but does not provide the equivalent Mac Catalyst app-extension target used by the original draft. Keeping the unsupported target would create a permanently unbuildable configuration without adding product behavior that Mac native drop does not already provide.

## ADR-016 — Compile validation is distinct from signing/package validation

**Decision:** Hosted Windows CI compiles the Windows app with `WindowsPackageType=None`; signed MSIX generation/install/update is a separate release gate. Hosted iOS Simulator CI clears signing/provisioning inputs only for simulator compilation; signed iOS App Group/extension provisioning is a separate release gate.

**Reason:** Source/API/XAML compilation and platform signing/package infrastructure validate different failure classes. CI must not report source failure because private signing assets are absent, and it must not imply package/signing readiness merely because source compiles.

## ADR-017 — Keep serviced framework dependencies current within the .NET 10 line

**Decision:** The application uses `Microsoft.Maui.Controls` 10.0.90 and Core pins the repaired SQLite dependency path (`Microsoft.Data.Sqlite` 10.0.10 plus `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12) after CI exposed the earlier vulnerable/native and Windows packaging baselines.

**Reason:** Servicing updates close known dependency/tooling problems and keep platform build behavior aligned with the currently installed .NET 10 workloads, while exact release dependency/license review remains mandatory.

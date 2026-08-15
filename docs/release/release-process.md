# SwiftDrop Release Process

Updated: 2026-08-15

This process turns a source-complete commit into a release candidate and, only after all required evidence passes, into a publishable release.

The authoritative detailed checklist remains `docs/release/release-checklist.md`. The machine-readable dependency artifact contract is defined in `docs/release/dependency-evidence.md`. This document explains sequence and ownership of the evidence.

## 1. Define the candidate

Choose one exact commit on `main` and record its full SHA.

Do not call an unfrozen moving branch “the release candidate.” Every automated/manual/signed-package result must be traceable to an exact commit.

## 2. Confirm source/document synchronization

Before candidate freeze, confirm the following reflect the same source state:

- `README.md`;
- `BUILDING.md`;
- `PROJECT_STATUS.md`;
- `NEXT_STEPS.md`;
- `CHANGELOG.md`;
- `PRIVACY.md`;
- `SECURITY.md`;
- `THIRD_PARTY_NOTICES.md`;
- `docs/README.md`;
- protocol/security/architecture/platform/storage/testing docs;
- release process/checklist, dependency-evidence contract, and store privacy declarations;
- `what_changed.md` engineering ledger.

Confirm the current local database schema number and migration path agree across source, storage documentation, privacy documentation, compatibility policy, manual/security tests, and release checklist. For the current source that means schema **v6**, including safe v3 queue-row migration defaults, null-preserving v4/v5 History performance migrations, the non-authorizing restart-safe queue contract, and bounded optional duration/measured-byte metadata.

If a source fix is required after freeze, create a new candidate SHA and repeat invalidated evidence.

## 3. Require automated candidate gates

For the exact candidate, require the relevant maintained workflows:

- Core CI;
- platform builds;
- CodeQL;
- security hygiene;
- release readiness.

Review failures; do not bypass a gate simply to publish.

The release-readiness workflow also self-tests when its verification/audit/evidence helper inputs change on `main` or in a pull request. That reduces the chance of discovering a broken release gate only after a version tag is created.

## 4. Retrieve and verify dependency evidence

Use the exact-candidate release-readiness run, not a historical successful run from a different SHA.

Retain all four machine-readable artifact bundles:

- `dependency-audit`;
- `android-dependency-audit`;
- `windows-dependency-audit`;
- `apple-dependency-audit`.

The reports use explicit JSON output schema version 1. Vulnerable-package reports include transitive dependencies and are checked by `scripts/validate_nuget_vulnerability_report.py` during the workflow.

Before long-term retention/review:

1. download every audit artifact from the exact candidate;
2. verify the expected package/vulnerability JSON files are present;
3. independently recompute each report file's byte length and SHA-256 digest;
4. compare those values with the bundle's deterministic `manifest.json`;
5. reject/recreate an evidence bundle if a retained file no longer matches its manifest;
6. keep the verified artifact bundle with the release record.

The hash manifest detects report-byte changes. It is not a digital signature, provenance attestation, SBOM signature, or proof that a separately built signed package used the same dependency graph.

## 5. Review dependency provenance, vulnerabilities, and licenses

Review the direct and transitive restored graphs captured for:

- `SwiftDrop.Core`;
- portable tests and benchmarks where release engineering depends on them;
- Android `SwiftDrop.App`;
- focused Windows `SwiftDrop.App`;
- Mac Catalyst `SwiftDrop.App`;
- iOS `SwiftDrop.App`;
- iOS `SwiftDrop.ShareExtension`.

Confirm:

- the machine-readable vulnerable-package files contain no findings under the configured advisory data;
- no unreviewed low/moderate/high/critical advisory is being ignored;
- versions match the exact candidate restore graph;
- dependency provenance is understood;
- license/notice/redistribution obligations are represented in `THIRD_PARTY_NOTICES.md`, `NOTICE`, package/store materials, or other required attribution locations;
- the previously blocked vulnerable SQLite native dependency path has not returned;
- hosted simulator/unpackaged evidence is not mistaken for the final signed package graph.

After signed/distribution artifacts are built, compare their actual dependency/runtime contents with this restored/source evidence and investigate unexplained differences before publication.

## 6. Prepare signing outside the repository

Never commit production signing secrets.

Follow `docs/release/signing-configuration.md`.

Maintain signing/provisioning material in appropriate secure external systems.

### Android

Prepare release keystore/signing configuration and store credentials outside Git.

### iOS

Configure Apple Developer identifiers/profiles for:

- containing app `in.sanskar.swiftdrop`;
- Share Extension `in.sanskar.swiftdrop.share`;
- App Group `group.in.sanskar.swiftdrop` for both where required by the source entitlements.

### Mac Catalyst

Configure signing/sandbox/distribution/notarization as required for the selected distribution path.

### Windows

Configure package signing certificate and selected store/distribution identity outside the repository.

## 7. Build signed/distribution artifacts from the candidate

Produce the final-format artifacts from the same frozen source commit.

Examples of target output categories:

- Android release AAB/APK as required by distribution/test flow;
- signed iOS/TestFlight build containing the Share Extension;
- signed Mac Catalyst distribution artifact;
- signed Windows MSIX/package.

Hosted unsigned/simulator compile artifacts are not substitutes for these final signed artifacts.

## 8. Validate package metadata, entitlements, and dependency correspondence

Inspect the produced artifacts, not only source files.

Compare packaged/runtime dependency contents with the exact-candidate restored evidence from steps 4–5. Record and review any target/runtime components that are introduced by packaging and are not represented as ordinary NuGet package rows.

### Apple

Verify bundle IDs, versions/build numbers, App Group entitlements, Share Extension extension point/activation rules, containing-app embedding, sandbox/network entitlements, provisioning signatures, and the extension/containing-app packaged dependency boundary.

### Android

Verify manifest/permissions/foreground-service declarations, min/target SDK expectations, signing identity, exported component posture, backup behavior, packaged resources, and packaged runtime/native dependency contents.

### Windows

Verify package identity, protocol registration, private-network capability, signing, supported OS metadata, icons/assets, install/update behavior, and packaged runtime dependency contents.

## 9. Execute physical transfer matrix

Use `docs/testing/manual-test-matrix.md` and `docs/testing/release-candidate-additional-cases.md`.

Test supported sender/receiver combinations on physical environments where required.

Cover at minimum:

- discovery and fallback pairing;
- QR/deep-link/one-time code/manual IP;
- certificate fingerprint/pin mismatch rejection;
- single/multi/folder/text transfers;
- zero-byte/small/large files;
- pause/cancel/network interruption/resume;
- stable batch retry/completed-item reuse;
- collision and canonical path behavior;
- source/destination link/reparse rejection;
- low storage;
- dangerous-extension warning;
- trust/revoke/identity reset;
- receive-location changes;
- schema-v6 database upgrade plus queue restart/progress/item recovery;
- History performance sampling for full/resumed transfers, including actual post-resume measured-byte attribution and exclusion of zero-byte/failed/legacy/unmeasured rows;
- stale active queue rows becoming `Interrupted` without automatic replay;
- fresh authorization still being required after restart;
- caller cancellation during queue initialization/best-effort persistence not permanently disabling later queue metadata persistence.

## 10. Execute restricted-network/lifecycle cases

Test real environments such as:

- guest Wi-Fi/client isolation;
- multicast-blocked networks;
- host firewall blocked/allowed states;
- IPv4-only and IPv6-capable LANs;
- network changes mid-transfer;
- slow/unstable LAN;
- app sleep/lock/background transitions;
- low storage during staging/transfer;
- repeated invalid pairing/connection pressure.

## 11. Validate platform-specific external intake

### Android

Exercise `ACTION_SEND` / `ACTION_SEND_MULTIPLE` providers including known, unknown, negative, and delayed/failed size/provider cases.

### iOS

Exercise the signed Share Extension with real provider types, App Group handoff, provider delays/timeouts, malformed/stale package rejection, exact physical file-set validation, and containing-app review-before-send.

### Mac Catalyst

Exercise native file/folder/text/pair-link drop with security-scoped source lifetime and sandbox behavior.

### Windows

Exercise file/folder/text/pair-link drag/drop, protocol activation, receive-folder picker, and package/firewall behavior.

## 12. Accessibility and localization validation

Use `docs/testing/accessibility-checklist.md`.

Test:

- English and Hindi;
- long/wrapped strings;
- large text;
- light/dark/system theme;
- keyboard-only desktop navigation;
- TalkBack/VoiceOver/Narrator where applicable;
- status/error communication that does not rely only on color;
- queue operation/progress/item/timing/interrupted-state presentation;
- reduced-motion/high-contrast expectations.

## 13. Privacy/store declaration review

Compare final binaries/behavior with:

- `PRIVACY.md`;
- `docs/release/store-privacy-declarations.md`;
- platform store forms/declarations.

For current schema v6, confirm the queue contract introduced through v4 still stores only the documented generic label/state/error/operation/timestamp/progress/item metadata and does not introduce pairing nonces, reusable authorization/session tokens, certificates/private keys, peer endpoints, source/destination paths, or transferred contents into the queue table. Confirm History performance metadata is limited to bounded duration/actual measured-byte fields under existing History retention/privacy rules and is never reusable transfer authorization.

Do not claim that data is absent if the candidate actually stores/transmits it, and do not declare permissions/features that are not present merely because they were once planned.

## 14. Final release checklist sign-off

Complete every required applicable item in `docs/release/release-checklist.md`.

Any unchecked required item means the candidate is not yet production-ready.

## 15. Version/tag/release notes

When the exact candidate has passed:

1. confirm app/extension version/build consistency;
2. update changelog/release notes as needed;
3. create the intended Git tag from the exact verified commit;
4. publish release notes that identify what changed and known limitations;
5. retain the verified dependency-evidence bundles and relevant automated/manual/signed-package evidence.

## 16. Store/distribution submission

Submit only the artifacts built and validated from the approved candidate.

Do not rebuild from a different commit after final verification without repeating the invalidated gates.

## 17. Post-release verification

After publication/distribution:

- verify install/update from the actual distribution channel;
- verify basic pairing/transfer on the published binary;
- confirm support/privacy links resolve correctly;
- monitor incoming crash/security/support reports;
- open a new patch candidate rather than modifying a published artifact in place.

## 18. Emergency security fix path

For a security-relevant defect:

1. assess/report according to `SECURITY.md`;
2. create the smallest safe source fix;
3. add regression tests that fail before/pass after the fix;
4. review compatibility/data migration implications;
5. run the full relevant candidate gates;
6. regenerate/re-verify dependency evidence if the restore graph or release tooling changed;
7. produce/revalidate signed artifacts;
8. publish a security patch/release note without exposing unnecessary exploit detail before users can update.

## 19. Release completion rule

SwiftDrop is production-ready only when the exact release candidate has passed the required automated gates **and** its signed target artifacts have completed the applicable physical-device/network/provider/storage/accessibility/localization/dependency/license/privacy/store checks.

Source completeness, hosted compilation, clean vulnerable-package reports, and matching evidence hashes are necessary evidence, not the final release claim.

---

## Native notification candidate validation

For any candidate that ships optional terminal notifications:

1. run both Apple and Windows integration validators through the portable gate;
2. confirm the 16 Python helper tests and 522 xUnit tests pass;
3. inspect the final Windows packaged manifest/COM activation metadata rather than relying on unpackaged source compile alone;
4. verify signed Android/iOS/Mac Catalyst/Windows notification permission/registration/presentation behavior on real targets;
5. verify English/Hindi terminal messages remain generic and contain no transfer-specific identifiers/content;
6. verify notification denial/failure never changes transfer correctness;
7. rebuild and repeat invalidated signed/package notification evidence after any notification-source/manifest/resource change.

**Made by the Sanskar**

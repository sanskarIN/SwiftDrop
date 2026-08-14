# SwiftDrop Release Process

This process turns a source-complete commit into a release candidate and, only after all required evidence passes, into a publishable release.

The authoritative detailed checklist remains `docs/release/release-checklist.md`. This document explains sequence and ownership of the evidence.

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
- release checklist and store privacy declarations;
- `what_changed.md` engineering ledger.

If a source fix is required after freeze, create a new candidate SHA and repeat invalidated evidence.

## 3. Require automated candidate gates

For the exact candidate, require the relevant maintained workflows:

- Core CI;
- platform builds;
- CodeQL;
- security hygiene;
- release readiness.

Review failures; do not bypass a gate simply to publish.

## 4. Review dependency and license evidence

Generate/review direct and transitive package inventories for shipped/runtime projects and required target frameworks.

Use the maintained .NET 10 package-list syntax, including machine-readable vulnerable-package views where configured:

```bash
dotnet package list --project <project> --include-transitive --vulnerable --format json
```

Confirm:

- no unreviewed low/moderate/high/critical advisory is being ignored;
- versions match the candidate restore graph;
- dependency provenance is understood;
- license/notice obligations are represented in `THIRD_PARTY_NOTICES.md` and `NOTICE` where applicable.

## 5. Prepare signing outside the repository

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

## 6. Build signed/distribution artifacts from the candidate

Produce the final-format artifacts from the same frozen source commit.

Examples of target output categories:

- Android release AAB/APK as required by distribution/test flow;
- signed iOS/TestFlight build containing the Share Extension;
- signed Mac Catalyst distribution artifact;
- signed Windows MSIX/package.

Hosted unsigned/simulator compile artifacts are not substitutes for these final signed artifacts.

## 7. Validate package metadata and entitlements

Inspect the produced artifacts, not only source files.

### Apple

Verify bundle IDs, versions/build numbers, App Group entitlements, Share Extension extension point/activation rules, containing-app embedding, sandbox/network entitlements, and provisioning signatures.

### Android

Verify manifest/permissions/foreground-service declarations, min/target SDK expectations, signing identity, exported component posture, backup behavior, and packaged resources.

### Windows

Verify package identity, protocol registration, private-network capability, signing, supported OS metadata, icons/assets, and install/update behavior.

## 8. Execute physical transfer matrix

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
- queue/restart behavior.

## 9. Execute restricted-network/lifecycle cases

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

## 10. Validate platform-specific external intake

### Android

Exercise `ACTION_SEND` / `ACTION_SEND_MULTIPLE` providers including known, unknown, negative, and delayed/failed size/provider cases.

### iOS

Exercise the signed Share Extension with real provider types, App Group handoff, provider delays/timeouts, malformed/stale package rejection, exact physical file-set validation, and containing-app review-before-send.

### Mac Catalyst

Exercise native file/folder/text/pair-link drop with security-scoped source lifetime and sandbox behavior.

### Windows

Exercise file/folder/text/pair-link drag/drop, protocol activation, receive-folder picker, and package/firewall behavior.

## 11. Accessibility and localization validation

Use `docs/testing/accessibility-checklist.md`.

Test:

- English and Hindi;
- long/wrapped strings;
- large text;
- light/dark/system theme;
- keyboard-only desktop navigation;
- TalkBack/VoiceOver/Narrator where applicable;
- status/error communication that does not rely only on color;
- reduced-motion/high-contrast expectations.

## 12. Privacy/store declaration review

Compare final binaries/behavior with:

- `PRIVACY.md`;
- `docs/release/store-privacy-declarations.md`;
- platform store forms/declarations.

Do not claim that data is absent if the candidate actually stores/transmits it, and do not declare permissions/features that are not present merely because they were once planned.

## 13. Final release checklist sign-off

Complete every required applicable item in `docs/release/release-checklist.md`.

Any unchecked required item means the candidate is not yet production-ready.

## 14. Version/tag/release notes

When the exact candidate has passed:

1. confirm app/extension version/build consistency;
2. update changelog/release notes as needed;
3. create the intended Git tag from the exact verified commit;
4. publish release notes that identify what changed and known limitations;
5. retain relevant verification/dependency evidence.

## 15. Store/distribution submission

Submit only the artifacts built and validated from the approved candidate.

Do not rebuild from a different commit after final verification without repeating the invalidated gates.

## 16. Post-release verification

After publication/distribution:

- verify install/update from the actual distribution channel;
- verify basic pairing/transfer on the published binary;
- confirm support/privacy links resolve correctly;
- monitor incoming crash/security/support reports;
- open a new patch candidate rather than modifying a published artifact in place.

## 17. Emergency security fix path

For a security-relevant defect:

1. assess/report according to `SECURITY.md`;
2. create the smallest safe source fix;
3. add regression tests that fail before/pass after the fix;
4. review compatibility/data migration implications;
5. run the full relevant candidate gates;
6. produce/revalidate signed artifacts;
7. publish a security patch/release note without exposing unnecessary exploit detail before users can update.

## 18. Release completion rule

SwiftDrop is production-ready only when the exact release candidate has passed the required automated gates **and** its signed target artifacts have completed the applicable physical-device/network/provider/storage/accessibility/localization/dependency/license/privacy/store checks.

Source completeness and hosted compilation are necessary evidence, not the final release claim.

---

**Made by the Sanskar**

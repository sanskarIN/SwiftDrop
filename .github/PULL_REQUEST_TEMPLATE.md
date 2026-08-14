## Summary

Describe the user-visible or engineering change and the layer(s) affected.

## Why

Explain the problem this change solves and why the chosen approach is appropriate.

## Compatibility

- [ ] Protocol/wire compatibility was reviewed if protocol behavior changed.
- [ ] SQLite/settings migration compatibility was reviewed if persisted data changed.
- [ ] Device trust/identity/resume compatibility was reviewed if security state changed.
- [ ] Minimum OS/package identity/version implications were reviewed when applicable.
- [ ] No compatibility impact, or the impact is documented below.

Compatibility notes:

## Testing and verification

- [ ] `scripts/verify-core.sh` or `scripts/verify-core.ps1` passes when applicable.
- [ ] Documentation integrity validation passes.
- [ ] Core unit/integration tests pass.
- [ ] New/changed untrusted-input behavior has positive and negative/boundary coverage.
- [ ] Changed target platform compiles successfully.
- [ ] Relevant manual transfer/provider path was exercised when practical.
- [ ] Cancellation/error/interruption/low-storage path was considered when applicable.

Exact commands/workflow runs:

## Security and privacy

- [ ] No custom cryptography was added without an explicit reviewed architecture decision.
- [ ] No private keys, pairing capabilities/nonces, transferred content, credentials, tokens, signing material, or unnecessary personal data are logged or committed.
- [ ] Incoming paths, sizes, counts, protocol fields, provider metadata, and external content remain bounded and validated.
- [ ] Authorization/replay/certificate/path/integrity/resume boundaries were not weakened.
- [ ] The change does not silently broaden filesystem, network, clipboard, camera, notification, background, sandbox, or package permissions.
- [ ] Privacy/security documentation was updated if behavior/data collection/storage changed.

Security/privacy notes:

## Dependencies and licenses

- [ ] No dependency change.
- [ ] Or: direct/transitive vulnerability audit was reviewed for changed dependencies.
- [ ] License/provenance/notice impact was reviewed.
- [ ] `THIRD_PARTY_NOTICES.md` was updated if the direct dependency/version inventory changed.

Dependency notes:

## Accessibility and localization

- [ ] New controls have meaningful labels/semantics where applicable.
- [ ] Keyboard/screen-reader/focus behavior was considered where applicable.
- [ ] The change does not rely on color alone.
- [ ] Large text/wrapping/theme/reduced-motion implications were considered.
- [ ] English/Hindi resource keys and formatted placeholders stay in parity for user-facing text changes.

## Platforms affected

- [ ] Android
- [ ] iOS
- [ ] Mac Catalyst
- [ ] Windows
- [ ] iOS Share Extension
- [ ] Portable Core only
- [ ] Documentation/tooling only

## Documentation

- [ ] The canonical document that owns the changed contract was updated.
- [ ] `README.md` / `PROJECT_STATUS.md` / `NEXT_STEPS.md` / `CHANGELOG.md` / `what_changed.md` were updated when the change warrants it.
- [ ] No documentation change is required, with reason below.

Documentation notes:

## Signed-device/manual release validation still required

List any validation that hosted CI cannot prove, such as signing/provisioning, App Group/provider behavior, Windows MSIX/protocol/firewall behavior, Mac sandbox/notarization, real LAN conditions, low storage, lifecycle/background behavior, accessibility, localization, or store declarations.

## Release notes

State whether this change should appear in the next release notes and whether it changes known limitations.

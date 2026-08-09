## Summary

Describe the user-visible or engineering change.

## Why

Explain the problem this change solves.

## Testing

- [ ] Core unit tests pass.
- [ ] Changed target platform builds successfully.
- [ ] Relevant manual transfer path was exercised.
- [ ] Cancellation/error path was exercised when applicable.

## Security and privacy

- [ ] No custom cryptography was added.
- [ ] No private keys, pairing nonces, transferred content, credentials, tokens, or personal data are logged or committed.
- [ ] Incoming paths, sizes, counts, and protocol fields remain bounded and validated.
- [ ] The change does not silently broaden filesystem, network, clipboard, camera, notification, or background permissions.
- [ ] Privacy/security documentation was updated if behavior changed.

## Accessibility

- [ ] New controls have meaningful labels/semantics.
- [ ] Keyboard/screen-reader/focus behavior was considered where applicable.
- [ ] The change does not rely on color alone.

## Platforms affected

- [ ] Android
- [ ] iOS
- [ ] macOS / Mac Catalyst
- [ ] Windows
- [ ] Portable core only

## Release notes

State whether `CHANGELOG.md`, `PROJECT_STATUS.md`, or release documentation needs an update.

# SwiftDrop Release Checklist

## Source and dependency review

- [ ] `main` is green in CI for restore, build, and tests.
- [ ] No secrets, signing keys, PFX/P12 files, tokens, pairing invitations, or real transferred files are committed.
- [ ] Dependency changes are reviewed for provenance, license, security advisories, and supported target frameworks.
- [ ] `dotnet list package --vulnerable` is reviewed in a connected development environment.
- [ ] Release notes describe protocol/security behavior changes.

## Security

- [ ] Pairing invitations expire and are one-time use.
- [ ] Receiver certificate fingerprint pinning is verified on every transfer connection.
- [ ] Sender certificate is required and its fingerprint is shown to the receiver before transfer acceptance.
- [ ] Incoming transfers require explicit approval unless a separately reviewed trusted-device policy intentionally permits otherwise.
- [ ] Dangerous-file warnings are shown before acceptance.
- [ ] Received files are never auto-opened.
- [ ] Path traversal tests pass.
- [ ] SHA-256 integrity verification passes for normal and resumed transfers.
- [ ] Corrupted transfers never become final files.
- [ ] Free-space checks reject unsafe transfers before receiving the remaining payload.
- [ ] Security documentation and `SECURITY.md` match implementation behavior.

## Privacy

- [ ] No account is required for local transfer.
- [ ] No transferred content is uploaded to a SwiftDrop service.
- [ ] No analytics or identifiers are collected without a documented opt-in.
- [ ] Clipboard is accessed only after explicit user action.
- [ ] Transfer history contains metadata only.
- [ ] Privacy mode hides filenames in newly recorded history entries.
- [ ] `PRIVACY.md` is reviewed for accuracy.

## Android

- [ ] Build and install a signed release candidate on at least one supported physical Android device.
- [ ] Verify system file picker behavior without broad storage permissions.
- [ ] Verify local network/Wi-Fi connectivity and background/foreground behavior.
- [ ] Verify share intent integration if enabled for the release.
- [ ] Verify app icon, splash, theme, rotation, and large-text behavior.

## iOS and macOS

- [ ] Build with the supported Xcode/.NET MAUI toolchain on macOS.
- [ ] Verify local-network privacy prompt wording and behavior.
- [ ] Verify Bonjour declarations when mDNS discovery is enabled.
- [ ] Verify document picker sandbox access.
- [ ] Verify app transport does not depend on unsupported background execution.
- [ ] Verify custom `swiftdrop` pairing URL handling.

## Windows

- [ ] Build and install the release package on supported Windows versions.
- [ ] Verify `privateNetworkClientServer` capability.
- [ ] Test Windows Defender Firewall prompt/blocked cases and diagnostics guidance.
- [ ] Verify keyboard navigation, high-DPI rendering, and window resizing.
- [ ] Verify file picker and drag/drop behavior for release-supported flows.

## Transfer matrix

Complete `docs/testing/manual-test-matrix.md` for supported sender/receiver combinations. At minimum validate:

- [ ] small file;
- [ ] large file;
- [ ] zero-byte file;
- [ ] interrupted transfer and resume;
- [ ] sender cancellation;
- [ ] receiver rejection;
- [ ] filename collision;
- [ ] low-storage rejection;
- [ ] dangerous extension warning;
- [ ] network interruption;
- [ ] expired/replayed pairing invitation.

## Accessibility and UX

- [ ] Light, dark, and system themes are readable.
- [ ] Large accessibility text does not hide critical actions.
- [ ] Desktop keyboard focus can reach all essential controls.
- [ ] Status and errors are not communicated by color alone.
- [ ] Pairing fingerprints are readable and selectable/copyable where practical.
- [ ] Errors avoid exposing private paths or cryptographic secrets unnecessarily.

## Packaging and publication

- [ ] Version/build numbers are updated consistently.
- [ ] Changelog is updated.
- [ ] Store privacy declarations match actual behavior.
- [ ] Third-party notices/licenses are included when required.
- [ ] Release artifacts are signed with protected keys outside the repository.
- [ ] Final artifacts are tested after signing/packaging, not only before packaging.
- [ ] Git tag and release notes identify the exact tested commit.

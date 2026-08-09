# Release Signing Configuration

SwiftDrop does not commit signing certificates, private keys, provisioning profiles, passwords, access tokens, or store credentials. Release signing must be supplied by the release environment through secure repository/environment secrets or the platform's protected credential store.

## General rules

1. Build from a reviewed tagged commit.
2. Run portable tests, CodeQL, and platform compile/release-readiness workflows before signing.
3. Generate the exact dependency graph and complete the third-party license review.
4. Store signing material outside the repository.
5. Restrict signing secrets to protected release environments and least-privilege maintainers.
6. Never print signing passwords/private material into Actions logs.
7. Verify the final artifact signature and package identity after signing.
8. Record artifact hashes in release evidence.

## Android

Expected release inputs include an Android keystore, alias, and protected passwords. Use .NET MAUI Android signing properties supplied from CI secrets/environment rather than checked-in project values.

A release must also verify:

- application ID `in.sanskar.swiftdrop`;
- target/minimum SDK policy for the release date;
- data-sync foreground service declarations;
- notification declarations and store privacy/data-safety answers;
- APK/AAB signature after packaging.

## iOS

Expected inputs include Apple signing identity, provisioning profile, bundle identifier configuration, and App Store Connect credentials where automated upload is used.

A release must also verify:

- local-network usage description;
- Bonjour service declarations;
- URL scheme behavior;
- entitlements and provisioning profile match;
- archive/export signature;
- App Store privacy declarations.

SwiftDrop does not claim arbitrary background socket entitlement or behavior that Apple has not approved.

## macOS / Mac Catalyst

Expected inputs include Developer ID/App Store signing identity and provisioning/entitlement configuration appropriate to the chosen distribution path. Notarization credentials must be stored securely if outside the Mac App Store.

Verify firewall/network behavior and final code signature/notarization result on the produced application bundle.

## Windows

Expected inputs include the package-signing certificate/private key and password where MSIX/package signing is used. Package Publisher/Identity values must match the certificate/store reservation used for the actual release.

Verify:

- package signature;
- private-network capability;
- `swiftdrop` protocol activation;
- native FolderPicker behavior in the packaged build;
- Windows Defender/SmartScreen behavior without claiming that transport integrity equals malware safety.

## GitHub Actions

The repository's current automated workflows intentionally perform unsigned compile/readiness checks. A future signing/publishing workflow should reference protected environment secrets only after store identities and credentials are configured. Do not add placeholder private keys or sample real credentials to make CI appear complete.

## Release evidence

For every published build retain outside the repository where appropriate:

- tag and commit SHA;
- toolchain versions;
- dependency inventory;
- platform build/test results;
- physical-device matrix results;
- signing identity/certificate public metadata (never private key);
- artifact SHA-256 hashes;
- store submission/version identifiers;
- known limitations and release notes.

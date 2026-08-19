# SwiftDrop Documentation

This directory is the canonical navigation point for SwiftDrop technical, user, security, testing, platform, and release documentation.

SwiftDrop is an account-free local-network transfer application built with .NET MAUI and C#. The maintained application targets are Android, iOS, Mac Catalyst, and Windows. Transfer payloads are intended to move directly between nearby peers rather than through a SwiftDrop-operated cloud relay.

## Start here

- [Project overview](../README.md)
- [Installation and source run](installation.md)
- [User guide](user-guide.md)
- [Settings reference](configuration.md)
- [FAQ](faq.md)
- [Technical glossary](glossary.md)
- [Troubleshooting](troubleshooting.md)
- [Diagnostics and bug reports](diagnostics-and-bug-reports.md)
- [Building SwiftDrop](../BUILDING.md)
- [Development guide](development-guide.md)
- [Contributing](../CONTRIBUTING.md)

## Architecture

- [Architecture overview](architecture.md)
- [Clean architecture and MVVM boundaries](architecture/clean-architecture.md)
- [Project and repository structure](architecture/project-structure.md)
- [Networking and firewall model](networking.md)
- [Architecture decisions](../DECISIONS.md)

## Protocol and security

- [Wire format](protocol/wire-format.md)
- [Protocol security](protocol/security.md)
- [Compatibility rules](protocol/compatibility.md)
- [Compatibility matrix](protocol/compatibility-matrix.md)
- [Versioning and compatibility policy](versioning-and-compatibility.md)
- [Technical glossary](glossary.md)
- [Threat model](security/THREAT_MODEL.md)
- [Security policy](../SECURITY.md)
- [Privacy policy](../PRIVACY.md)

## Platform integration

- [Platform integration status](platform/integration-status.md)
- [Permissions and entitlements](platform-permissions.md)
- [Networking and firewall guide](networking.md)
- [Signing configuration](release/signing-configuration.md)
- [Store privacy declarations](release/store-privacy-declarations.md)

## Storage and local data

- [SQLite database schema](storage/database-schema.md)
- [Settings reference](configuration.md)
- [Privacy policy](../PRIVACY.md)

## Testing and quality

- [CI reference](testing/ci-reference.md)
- [Deterministic state-model testing](testing/deterministic-state-models.md)
- [Security test plan](testing/security-test-plan.md)
- [Manual test matrix](testing/manual-test-matrix.md)
- [Release-candidate additional cases](testing/release-candidate-additional-cases.md)
- [Accessibility checklist](testing/accessibility-checklist.md)
- [Performance benchmarks](testing/performance-benchmarks.md)
- [Diagnostics and bug reports](diagnostics-and-bug-reports.md)

## Release and operations

- [Post-v1 hardening ledger — 2026-08-19](../what_changed_2026-08-19.md)
- [Final in-repository audit — 2026-08-18](release/final-audit-2026-08-18.md)
- [Release process](release/release-process.md)
- [Release checklist](release/release-checklist.md)
- [Manual release evidence](release/manual-release-evidence.md)
- [Dependency evidence](release/dependency-evidence.md)
- [Signing configuration](release/signing-configuration.md)
- [Store privacy declarations](release/store-privacy-declarations.md)
- [Versioning and compatibility](versioning-and-compatibility.md)
- [Project status](../PROJECT_STATUS.md)
- [Next validation steps](../NEXT_STEPS.md)
- [Changelog](../CHANGELOG.md)
- [Detailed engineering ledger](../what_changed.md)

## Community and legal

- [Support](../SUPPORT.md)
- [Diagnostics and bug reports](diagnostics-and-bug-reports.md)
- [Code of Conduct](../CODE_OF_CONDUCT.md)
- [Terms](../TERMS.md)
- [License](../LICENSE)
- [Notice](../NOTICE)
- [Third-party notices](../THIRD_PARTY_NOTICES.md)

## Documentation status rules

Documentation distinguishes four different evidence levels:

1. **Implemented in source** — code/configuration exists in the repository.
2. **Portable-tested** — relevant portable automated tests have executed successfully.
3. **Hosted-platform compiled** — source compiled on the maintained GitHub-hosted platform gate.
4. **Signed/device validated** — a signed package has been exercised on the real target environment with its real permissions, entitlements, filesystem, providers, networking, lifecycle, accessibility, and packaging behavior.

A successful source compile is not proof of signed-device or store readiness. The release checklist is the authoritative production-readiness boundary.

## Current maintained identifiers

- App ID: `in.sanskar.swiftdrop`
- iOS Share Extension ID: `in.sanskar.swiftdrop.share`
- Apple App Group: `group.in.sanskar.swiftdrop`
- Canonical solution: `SwiftDrop.slnx`
- Main repository branch: `main`

## Documentation maintenance rules

When source behavior changes, update the document that owns that contract in the same change set:

- user-visible workflow -> user guide/FAQ/README;
- settings/defaults -> settings reference;
- build/tooling -> `BUILDING.md` and development guide;
- architecture/project boundaries -> architecture docs;
- network/ports/address policy -> networking guide and protocol/security docs;
- protocol/canonicality -> protocol docs and compatibility policy;
- terminology -> technical glossary where the term is project-specific or security-relevant;
- local metadata -> database schema and privacy policy;
- platform permissions/entitlements -> platform permissions/integration status;
- tests/CI -> testing docs and CI reference;
- dependency/audit artifact format -> dependency evidence reference;
- release/signing/store behavior -> release docs;
- significant continuation work -> changelog/status/engineering ledger.

Do not change documentation merely to make an unsafe implementation look intended. Resolve the source contract, tests, and documentation together.

## Support

- Repository: https://github.com/sanskarIN/SwiftDrop
- GitHub profile: https://github.com/sanskarIN
- Business/security email: `sanskarin@outlook.in`
- General support email: `supportramsandesh@gmail.com`
- Optional development support: https://buymeacoffee.com/sanskarIN

---

**Made by the Sanskar**

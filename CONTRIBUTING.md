# Contributing to SwiftDrop

Thank you for improving SwiftDrop.

SwiftDrop handles untrusted local-network input, file paths, external providers, certificates, transfer authorization, and local persistence. Contributions should treat security, privacy, interoperability, and failure behavior as part of the feature—not as cleanup after implementation.

## Read before changing source

Start with:

- `docs/README.md` — documentation index;
- `docs/development-guide.md` — development workflow and layer rules;
- `docs/architecture/project-structure.md` — repository/project boundaries;
- `docs/architecture.md` — system architecture;
- `docs/security/THREAT_MODEL.md` — security assumptions/threats;
- `docs/protocol/security.md` and `docs/protocol/wire-format.md` for protocol changes;
- `PROJECT_STATUS.md` and `NEXT_STEPS.md` for current verification boundaries.

## Development workflow

1. Install the .NET 10 SDK and the .NET MAUI workload(s) required by the platform you are changing.
2. Fork and clone the repository.
3. Create a focused branch.
4. Run `dotnet restore SwiftDrop.slnx`.
5. Run the maintained portable verification gate:
   - Linux/macOS: `bash scripts/verify-core.sh`
   - Windows PowerShell: `./scripts/verify-core.ps1`
6. Build the platform target you changed using `BUILDING.md` and the maintained CI workflow as reference.
7. If dependencies changed, review the direct/transitive vulnerability output and exact restored package graph; do not suppress an audit finding merely to make CI green.
8. Update the document that owns the changed contract.
9. Open a pull request describing behavior, tests, security/privacy/dependency impact, affected platforms, exact verification, and any remaining signed-device/manual validation.

## Quality rules

- Do not introduce custom cryptographic primitives when platform/.NET cryptography can provide the required primitive/protocol.
- Never log pairing nonces/capabilities, certificate private keys, transferred file/text contents, clipboard contents, signing secrets, or reusable authorization.
- Treat peer-controlled paths, lengths, JSON, discovery data, provider metadata, and filenames as untrusted.
- Preserve strict/canonical protocol behavior unless a versioned compatibility decision deliberately changes it.
- Preserve one-time authorization/replay boundaries.
- Validate source and destination path/link/reparse safety at the correct time-of-use boundaries.
- Use cancellation tokens for network/file operations where the operation is cancellable.
- Keep platform-specific OS APIs under `Platforms/` or the iOS Share Extension boundary where practical.
- Add regression tests for protocol/security/storage/path/integrity/resume behavior changes.
- Add negative/boundary tests, not only happy-path tests, for untrusted input changes.
- Keep English/Hindi localization keys/placeholders synchronized.
- Preserve Apple App Group/iOS Share Extension metadata invariants.
- Do not weaken warnings-as-errors, NuGet vulnerability auditing, CodeQL, security-hygiene, or platform gates simply to bypass a failure.
- Keep signed-package/device/store validation distinct from hosted source compilation; never label an unpackaged/simulator compile as production-ready evidence.

## Dependency changes

Repository-wide NuGet auditing is explicit for direct/transitive packages at low-or-higher severity.

Use machine-readable review when relevant:

```bash
dotnet package list --project <project> --include-transitive --vulnerable --format json
```

When updating dependencies:

- prefer the smallest compatible update that resolves the issue;
- run portable tests;
- compile affected platform targets;
- check analyzer/test-tool behavior;
- update `THIRD_PARTY_NOTICES.md` if the dependency/version/license inventory changes;
- document security/license implications when relevant.

## Protocol changes

Any wire/protocol change must update the relevant documents under `docs/protocol/` and include compatibility/security analysis.

Do not create permissive aliases for canonical pairing/path/JSON representations without explicitly considering replay, ambiguity, cross-platform identity, and downgrade behavior.

## Persistence/privacy changes

If a change stores new local data:

- justify why it is needed;
- define retention/deletion/migration behavior;
- update SQLite schema/migration tests;
- update `docs/storage/database-schema.md`;
- update `PRIVACY.md` when data categories or behavior change.

Do not persist transferred contents, private keys, reusable transfer authorization, or unnecessary sensitive absolute paths for convenience.

## Platform changes

Changes that use Android/iOS/Mac Catalyst/Windows APIs require the relevant hosted compile gate and a manual-validation plan for behaviors CI cannot prove.

Examples include permissions, App Group, provider lifetimes, foreground/background behavior, sandbox, firewall, package protocol registration, signing, and accessibility.

## Documentation changes

Documentation-only contributions should still be checked against current source. Prefer linking to a canonical document rather than copying a long contract into multiple locations unless the duplication is necessary for user readability.

Use `docs/README.md` to find the document that owns each topic.

## Tests

Relevant test categories include:

- parser/validator unit tests;
- negative/malformed input tests;
- loopback mutual-TLS/protocol integration tests;
- SQLite migration/corruption tests;
- transfer interruption/mutation/resume tests;
- path/collision/link/reparse tests;
- platform hosted compilation;
- signed-device/manual matrix testing.

A change is not complete simply because one test method passes.

## Commit style

Use concise conventional-style messages such as:

```text
feat: add transfer retry
fix(protocol): reject traversal path
test(storage): cover migration failure
docs(release): document package validation
```

Keep commits focused and include the tests/docs needed to make each behavior change reviewable.

Project maintenance commits may include the requested sign-off trailer:

```text
Signed-off-by: Sanskar <sanskarin@outlook.in>
```

A sign-off trailer is not the same as a cryptographically signed Git commit.

## Pull request checklist

A pull request should answer:

- What changed?
- Why is the change needed?
- Which platforms/layers are affected?
- What security/privacy implications were reviewed?
- Which tests were added/changed?
- Which exact commands/workflows passed?
- Which docs were updated?
- What signed-device/manual validation remains?
- Does the change affect protocol/storage/version compatibility?
- Does it affect dependencies/licenses?

## Security reports

Do not open a public issue or PR containing exploit details, private keys, pairing capabilities, real transferred private files, or other sensitive material. Follow `SECURITY.md` for private reporting.

## Conduct

Follow `CODE_OF_CONDUCT.md`. Be respectful and constructive. Harassment, threats, discriminatory behavior, doxxing, and publication of private user data are not acceptable.

---

**Made by the Sanskar**

# Contributing to SwiftDrop

Thank you for improving SwiftDrop.

## Development

1. Install the .NET 10 SDK and the .NET MAUI workload(s) required by the platform you are changing.
2. Fork and clone the repository.
3. Create a focused branch.
4. Run `dotnet restore SwiftDrop.slnx`.
5. Run the maintained portable verification gate: `bash scripts/verify-core.sh` on Linux/macOS or `./scripts/verify-core.ps1` in Windows PowerShell.
6. Build the platform target you changed using the commands in `BUILDING.md`.
7. If dependencies changed, review the direct/transitive vulnerability output and the exact restored package graph; do not suppress an audit finding merely to make CI green.
8. Open a pull request describing behavior, tests, security/dependency impact, and platform impact.

## Quality rules

- Do not introduce custom cryptographic primitives.
- Never log pairing nonces, certificate private keys, transferred file contents, or clipboard contents.
- Validate all paths and sizes received from peers.
- Use cancellation tokens for network/file operations.
- Keep platform-specific code under `Platforms/` where possible.
- Add tests for protocol/security behavior changes.
- Keep localization keys/placeholders synchronized and preserve Apple integration metadata invariants.
- Do not weaken warnings-as-errors, NuGet vulnerability auditing, CodeQL, or repository-hygiene gates to bypass a failure.
- Keep signed-package/device/store validation distinct from hosted source compilation; never label an unpackaged/simulator compile as release-ready evidence.

## Commit style

Use concise conventional-style messages such as `feat: add transfer retry` or `fix: reject traversal path`.

Keep commits focused and include the tests/docs needed to make each behavior change reviewable.

## Conduct

Be respectful and constructive. Harassment, threats, or publication of private user data are not acceptable.

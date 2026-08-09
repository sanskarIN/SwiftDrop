# Contributing to SwiftDrop

Thank you for improving SwiftDrop.

## Development

1. Install the .NET SDK and relevant .NET MAUI workloads.
2. Fork and clone the repository.
3. Create a focused branch.
4. Run `dotnet restore SwiftDrop.sln`.
5. Run `dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release`.
6. Build the platform you changed.
7. Open a pull request describing behavior, tests, and platform impact.

## Quality rules

- Do not introduce custom cryptographic primitives.
- Never log pairing nonces, certificate private keys, transferred file contents, or clipboard contents.
- Validate all paths and sizes received from peers.
- Use cancellation tokens for network/file operations.
- Keep platform-specific code under `Platforms/` where possible.
- Add tests for protocol/security behavior changes.

## Commit style

Use concise conventional-style messages such as `feat: add transfer retry` or `fix: reject traversal path`.

## Conduct

Be respectful and constructive. Harassment, threats, or publication of private user data are not acceptable.

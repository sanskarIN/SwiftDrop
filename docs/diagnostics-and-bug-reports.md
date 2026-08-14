# SwiftDrop Diagnostics and Bug Reports

This guide explains how to collect useful troubleshooting information without exposing transfer contents, cryptographic material, or unnecessary personal data.

## Use diagnostics only when needed

SwiftDrop includes developer diagnostics that are disabled by default. Enable them only when investigating a reproducible problem or when a maintainer asks for the relevant sanitized information.

## Safe diagnostic design

The maintained diagnostic/export policy is intended to exclude:

- transferred file bytes;
- transferred text contents;
- clipboard contents;
- local certificate private keys;
- signing certificates/keystores/provisioning secrets;
- pairing invitations/capabilities/nonces;
- reusable transfer authorization;
- other secret credentials.

Privacy Mode further redacts common peer/file identifiers in privacy-sensitive diagnostic/history surfaces.

## Information that helps a bug report

Include, when relevant:

- exact SwiftDrop display version and Git commit SHA;
- sender platform and OS version;
- receiver platform and OS version;
- device model/architecture when platform-specific;
- pairing method used;
- whether automatic discovery worked;
- whether manual IP/QR pairing worked;
- transfer type (file, batch, folder, text, external share/drop);
- approximate file count and sizes without attaching private files;
- network type (home LAN, guest Wi-Fi, hotspot, enterprise, VPN);
- firewall/local-network permission state;
- exact error text;
- sanitized SwiftDrop diagnostic entries;
- minimal reproducible steps;
- whether the issue reproduces after app restart/new pairing.

## Information to redact before posting publicly

Remove or replace:

- full private/local IP addresses if not necessary;
- SSIDs and enterprise network names;
- device names containing real names;
- filenames revealing private information;
- absolute user profile paths;
- email addresses unrelated to the report;
- QR images/pairing URIs/codes;
- certificate/private-key files;
- Apple provisioning profiles;
- Android keystores;
- Windows signing certificates;
- real documents/photos/databases transferred during the test.

## Security-sensitive reports

Do not open a public issue when the report includes a vulnerability, authentication/authorization bypass, private key exposure, sandbox escape, path traversal, unsafe overwrite, sensitive-data leak, or a practical integrity/replay weakness.

Follow `SECURITY.md` and contact:

`sanskarin@outlook.in`

Provide enough detail for reproduction while avoiding public disclosure before a fix is available.

## Non-sensitive public bug report template

A useful report can follow this structure:

```text
SwiftDrop version/commit:
Sender OS/device:
Receiver OS/device:
Network type:
Pairing method:
Transfer type:
Expected result:
Actual result:
Exact error:
Reproduction steps:
Does it reproduce after fresh pairing/restart?:
Sanitized diagnostics attached?:
```

## Reproducing with non-sensitive test data

Prefer generated or disposable test data rather than private documents.

Examples:

- empty files;
- small random/test text files;
- generated folders with numbered filenames;
- copies of public/non-sensitive sample files.

For corruption/integrity tests, use controlled test fixtures in development/test environments rather than intentionally damaging important user data.

## Network reports

For connectivity bugs, state whether each of these works:

1. both devices appear in nearby discovery;
2. manual local IP pairing reaches the receiver;
3. fresh QR/deep-link pairing reaches the receiver;
4. small file transfer starts;
5. large file transfer starts/completes;
6. firewall/guest-network policy changes the outcome.

This separates discovery failures from transport/pairing/transfer failures.

## External share/provider reports

### Android

Include provider/app name only when relevant and non-sensitive, content type, whether size was known/unknown, and whether the same file works through SwiftDrop's normal picker.

### iOS Share Extension

Include share source app/type, whether the extension appeared, whether the containing app received the package, and whether the failure happens only on device versus simulator.

Never attach App Group package contents containing private files to a public report.

### Mac Catalyst

State whether the issue occurs with file drop, folder drop, text, pairing link, or normal file picker and whether the source is local/cloud/security-scoped.

### Windows

State whether the issue is protocol activation, drag/drop, receive-folder picker, firewall, or package install/update specific.

## Build/CI reports

Include:

- exact commit SHA;
- .NET SDK version;
- MAUI workload versions when known;
- OS/architecture;
- exact command;
- first relevant error and surrounding context;
- whether portable `verify-core` passes;
- whether failure is local-only or also appears in GitHub Actions.

Do not paste access tokens, signing variables, or secret environment values.

## Database/history problems

Do not publish a real user's SQLite database by default. It may contain metadata such as peer/history identifiers even though transfer contents/private keys are not intended to be stored there.

Prefer:

- sanitized schema/version information;
- generated test database reproduction;
- exact migration error;
- steps to reproduce from a fresh app state.

## What maintainers should do with diagnostics

Maintainers should:

- request the minimum information needed;
- avoid asking users to publish secrets/private transfer data;
- move security-sensitive details to private reporting;
- delete/avoid retaining unnecessary personal data;
- reproduce with generated fixtures where possible;
- add regression tests when a deterministic source defect is fixed.

## Related documentation

- [Troubleshooting](troubleshooting.md)
- [FAQ](faq.md)
- [Privacy](../PRIVACY.md)
- [Security policy](../SECURITY.md)
- [Support](../SUPPORT.md)
- [Security test plan](testing/security-test-plan.md)

---

**Made by the Sanskar**

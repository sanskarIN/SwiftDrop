# SwiftDrop Security Policy

SwiftDrop accepts good-faith reports about vulnerabilities in the maintained source, protocol, platform integrations, packaging configuration, and project-controlled distribution artifacts.

## Supported versions

Security fixes are developed against the latest maintained source/release line.

The repository source currently carries application display version `1.0.0`, but source version metadata alone does not mean a production release has completed the signed-device/store release process. When reporting a source issue, include the exact Git commit SHA. When reporting an installed release issue, include the exact published version/build and distribution channel.

Older unmaintained commits/builds may be useful for regression comparison but should not be assumed to receive separate fixes.

## Reporting a vulnerability

Do **not** publish exploit details, secrets, private keys, signing material, pairing capabilities/nonces, or private transferred content in a public GitHub issue or discussion.

Report privately to:

**sanskarin@outlook.in**

A useful report includes:

- affected version/build and exact commit when known;
- affected platform/OS/device architecture;
- concise reproduction steps;
- expected security boundary;
- observed behavior;
- realistic impact;
- whether user interaction/pairing/trust is required;
- sanitized logs or minimal proof-of-concept details;
- suggested fix/mitigation if you have one.

Use synthetic/non-sensitive files for reproduction whenever possible.

## Examples of security-sensitive findings

Private reporting is appropriate for issues such as:

- authentication/authorization bypass;
- one-time pairing/transfer replay;
- certificate/pinning/trust bypass;
- path traversal or receive-root escape;
- unsafe overwrite/final-promotion behavior;
- symlink/reparse bypass at source/destination boundaries;
- integrity/resume logic that can acknowledge incorrect content;
- protocol parser ambiguity that creates a security-relevant alias;
- unintended disclosure of transferred file/text contents;
- private key or reusable authorization leakage;
- App Group/sandbox/provider boundary exposure;
- unsafe package/protocol activation behavior;
- dependency vulnerability with a plausible SwiftDrop impact;
- diagnostics/history persistence of secrets that policy says must not be stored.

## Usually not a security vulnerability by itself

Examples that are generally support/bug topics unless they create a security boundary failure:

- discovery failure on guest Wi-Fi;
- expected firewall blocking;
- a transfer failing when the app/OS terminates networking;
- a user intentionally accepting a file from a device they trust;
- lack of malware scanning (SwiftDrop does not claim to be antivirus software);
- denial of service that requires full control of the local device and has no cross-user/security consequence.

If uncertain, private reporting is acceptable.

## Security design summary

SwiftDrop's maintained design includes:

- TLS 1.2/1.3 through .NET/platform cryptography;
- local P-256 ECDSA device identity certificates;
- receiver certificate SHA-256 pinning;
- sender client certificate requirement;
- strict short-lived/one-time pairing and transfer authorization;
- strict/canonical pairing capability representation;
- bounded framed protocol messages;
- strict UTF-8/JSON parsing with duplicate/unknown member rejection;
- canonical portable path/filename validation;
- path traversal/rooted/device/empty-segment rejection;
- link/reparse source/destination protections;
- bounded transfer/file/batch/staging limits;
- SHA-256 file integrity verification before final promotion;
- non-overwrite final promotion and collision-safe naming;
- fail-closed partial/completed-item resume verification;
- metadata-minimal SQLite persistence;
- repository NuGet vulnerability auditing, CodeQL, and security-hygiene gates.

See:

- `docs/security/THREAT_MODEL.md`;
- `docs/protocol/security.md`;
- `docs/protocol/wire-format.md`;
- `PRIVACY.md`;
- `docs/testing/security-test-plan.md`.

## Cryptography policy

SwiftDrop does not implement custom cryptographic primitives/algorithms for transport protection. Use maintained .NET/operating-system cryptographic/TLS capabilities and review any cryptographic-protocol change as security-critical.

## Endpoint trust boundary

SwiftDrop cannot protect data from a compromised endpoint that the user has allowed to read/send/receive it. Local-network encryption/integrity does not make an infected sender/receiver trustworthy.

## Responsible testing

Security research/testing should:

- use devices/networks/accounts/data you own or are authorized to test;
- avoid disrupting unrelated users/services;
- use synthetic data instead of private user files;
- avoid exfiltrating unnecessary data;
- stop after obtaining enough evidence to demonstrate the issue;
- preserve relevant logs/test fixtures safely for private disclosure.

This policy does not authorize testing systems/networks you do not have permission to test.

## Disclosure and fixes

The project aims to evaluate credible reports, reproduce the issue when practical, develop regression coverage, and fix the security boundary rather than only suppress the symptom.

Response/fix timelines cannot be guaranteed. Avoid public exploit disclosure before affected users can reasonably update when early disclosure would materially increase risk.

## Dependency vulnerabilities

The repository explicitly audits direct/transitive NuGet packages at low-or-higher severity with warnings-as-errors. A dependency advisory should still be evaluated for actual SwiftDrop reachability/impact, fixed versions, compatibility, license/provenance, and target-platform behavior.

Do not suppress an advisory merely to make CI green.

## Secrets and signing material

Never commit or publish:

- Android keystores/private signing keys;
- Apple signing certificates/private keys/provisioning secrets;
- Windows PFX/P12/private signing keys;
- access tokens/store credentials;
- device identity private keys;
- pairing capabilities/nonces;
- real private transfer contents.

Repository hygiene checks are defense-in-depth, not permission to commit secrets temporarily.

## Security regression requirements

A source fix should add/adjust automated regression coverage when the defect is deterministic and testable. Platform-specific fixes should also define the signed/device/manual validation required where hosted CI cannot prove the runtime boundary.

## Public credit

Reporter credit may be given when appropriate and when the reporter wants attribution. Do not disclose a reporter's private identity/contact details without permission.

---

**Made by the Sanskar**

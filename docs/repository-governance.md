# Repository Governance and Protected Change Policy

SwiftDrop treats repository governance as part of its security and release-readiness model. The application exchanges files and text across local networks, uses device identity and TLS, persists local metadata, and contains native platform integrations. Changes to those boundaries therefore need an explicit ownership and review policy in addition to automated tests.

## Ownership model

`.github/CODEOWNERS` assigns `@sanskarIN` as the fallback owner for the repository and explicitly repeats ownership for the most security- and release-sensitive surfaces:

- GitHub workflows, dependency automation, build properties, SDK selection, and verification scripts;
- Core security, protocol, networking, transfer, and storage code;
- native platform integration code and the iOS Share Extension;
- security, privacy, third-party notice, protocol, security, and release documentation.

The explicit entries are intentional even though the fallback already covers them. They make sensitive boundaries reviewable and allow the repository-completion validator to detect accidental ownership-policy erosion.

## Required protection for `main`

CODEOWNERS identifies reviewers, but GitHub only *enforces* CODEOWNER approval when branch protection or a repository ruleset requires it. For production-grade maintenance, configure `main` with the following protections in GitHub repository settings:

1. Require changes to reach `main` through a pull request.
2. Require at least one approving review and require review from Code Owners.
3. Dismiss stale approvals when new commits materially change the pull request.
4. Require all conversations to be resolved before merge.
5. Require the maintained CI/security/release-readiness checks that apply to the change.
6. Block force pushes and branch deletion.
7. Restrict direct bypasses to genuine emergency maintenance and record any bypass in the engineering ledger.
8. Keep repository administrators subject to the same protected-branch policy where the account/repository plan supports that configuration.

The exact visible status-check names can evolve with GitHub Actions job naming. Select the current required checks from a known-good pull request rather than documenting stale opaque check identifiers here.

## External-setting evidence boundary

Branch protection/rulesets are GitHub-hosted repository settings, not files in this source tree. Portable repository validation can verify that CODEOWNERS and this policy exist and remain structurally correct, but it cannot prove that the remote GitHub setting is enabled.

As of the 2026-08-20 governance audit, the GitHub API reported `main` as not protected. Treat enabling the protections above as an external repository-administration action. Do not describe CODEOWNER approval as enforced until the remote setting has actually been enabled and rechecked.

## Review expectations by change type

### Security, protocol, networking, transfer, and storage

Reviewers should verify:

- authorization remains fail-closed;
- protocol/canonicalization compatibility is intentional and documented;
- local-network restrictions and certificate/fingerprint policy are not weakened;
- persisted metadata does not gain secrets, reusable authorization, transfer content, or unnecessary identifiers;
- path, staging, resume, integrity, and symlink/reparse protections remain portable;
- relevant adversarial and regression tests are updated.

### Platform-native integrations

Reviewers should verify:

- platform permissions/capabilities/entitlements are the minimum required;
- external share/drop/protocol activation remains review-before-send;
- platform metadata changes are reflected in platform validators and release documentation;
- signed-device behavior is not inferred from source compilation alone.

### Dependencies and automation

Reviewers should verify:

- dependency changes pass NuGet vulnerability auditing and notice/license review;
- GitHub Actions changes preserve least-privilege permissions and existing release-critical triggers;
- verification helpers remain executable from both the portable Linux and Windows paths where applicable;
- changes do not bypass repository-completion or release-evidence checks.

### Documentation-only changes

Documentation must remain truthful about evidence level. A document must not upgrade a claim from “implemented” or “compiled” to “signed/device validated” without actual candidate evidence.

## Emergency changes

If an urgent security or build-recovery change must bypass the normal protected-branch flow, record:

- why the bypass was necessary;
- exact commit SHA;
- validation executed before/after the change;
- any missing validation that must still be completed;
- whether release/device evidence needs to be repeated.

Emergency access is a recovery mechanism, not a normal development path.

## Machine-enforced repository contract

`scripts/validate_repository_completion.py` validates that:

- `.github/CODEOWNERS` exists and is non-empty;
- the global fallback owner remains present;
- the sensitive ownership entries above remain assigned to `@sanskarIN`;
- this governance document remains part of the canonical repository/documentation surface.

The validator runs through normal CI and the portable verification entry points. This makes accidental deletion or silent weakening of the source-side ownership policy a test failure.

## Related references

- [Contributing](../CONTRIBUTING.md)
- [Security policy](../SECURITY.md)
- [Repository completion validation](testing/repository-completion-validation.md)
- [CI reference](testing/ci-reference.md)
- [Release process](release/release-process.md)
- [Release checklist](release/release-checklist.md)

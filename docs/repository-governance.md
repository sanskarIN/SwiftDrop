# Repository Governance and Protected Change Policy

SwiftDrop treats repository governance as part of its security and release-readiness model. The application exchanges files and text across local networks, uses device identity and TLS, persists local metadata, and contains native/platform-specific integrations across Android, iOS, macOS, Windows, and Linux. Changes to those boundaries therefore need explicit ownership and review policy in addition to automated tests.

## Ownership model

`.github/CODEOWNERS` assigns `@sanskarIN` as the fallback owner for the repository and explicitly repeats ownership for the most security- and release-sensitive surfaces:

- GitHub workflows, dependency automation, build properties, SDK selection, and verification scripts;
- Core security, discovery, protocol, networking, transfer, and storage code;
- native MAUI platform integration code;
- the Avalonia `SwiftDrop.Desktop` host;
- the iOS Share Extension;
- Linux packaging;
- security, privacy, third-party notice, protocol, release, and platform documentation.

The explicit entries are intentional even though the fallback already covers them. They make sensitive boundaries reviewable and allow the repository-completion validator to detect accidental ownership-policy erosion.

## Required protection for `main`

CODEOWNERS identifies ownership and review routing, but GitHub only *enforces* approval when branch protection or a repository ruleset requires it. For production-grade maintenance, configure `main` with the following protections in GitHub repository settings:

1. Require changes to reach `main` through a pull request.
2. Require the maintained CI/security/platform/release-readiness checks that apply to the change.
3. Require all conversations to be resolved before merge.
4. Block force pushes and branch deletion.
5. Restrict direct bypasses to genuine emergency maintenance and record any bypass in the engineering ledger.
6. Keep repository administrators subject to the same protected-branch policy where the account/repository plan supports that configuration and doing so does not create an unrecoverable single-maintainer lockout.

### Approval policy for a single-maintainer repository

At the time of this policy, CODEOWNERS assigns the current maintainer account `@sanskarIN`. GitHub does not treat a pull-request author's self-approval as an independent review. Requiring one approving review plus Code Owner approval while the author is the only eligible reviewer can therefore make ordinary pull requests impossible to merge without a bypass.

Until at least one trusted independent maintainer/reviewer is available:

- keep pull-request, required-check, conversation-resolution, force-push, and deletion protections enabled where feasible;
- use CODEOWNERS for ownership visibility and automatic review routing;
- do **not** enable an approval count or required-Code-Owner rule that cannot be satisfied without routinely bypassing it;
- require the full automated validation surface before merge and document exceptional bypasses.

When a trusted independent reviewer is added, strengthen the rule to require at least one approval, require Code Owner review for owned paths, and dismiss stale approvals when new commits materially change the pull request. Add that reviewer to the appropriate CODEOWNERS entries in the same reviewed change.

The exact visible status-check names can evolve with GitHub Actions job naming. Select required checks from a known-good current pull request rather than freezing stale opaque check identifiers in this policy.

## External-setting evidence boundary

Branch protection/rulesets are GitHub-hosted repository settings, not files in this source tree. Portable repository validation can verify that CODEOWNERS and this policy exist and remain structurally correct, but it cannot prove that the remote GitHub setting is enabled.

The most recently inspected GitHub API state still reported `main` as **not protected**. Treat enabling the feasible protections above as an external repository-administration action. Do not describe approval, required-check, or CODEOWNER enforcement as enabled until the remote setting has actually been configured and rechecked.

This remote setting is independent of the prepared SwiftDrop `2.5.18` source version and must not be inferred from the presence of governance files.

## Review expectations by change type

### Security, discovery, protocol, networking, transfer, and storage

Reviewers should verify:

- authorization remains fail-closed;
- protocol/canonicalization compatibility is intentional and documented;
- discovery remains constrained to the intended local-network model;
- local-address restrictions and certificate/fingerprint policy are not weakened;
- persisted metadata does not gain secrets, reusable authorization, transfer content, or unnecessary identifiers;
- path, staging, resume, integrity, and symlink/reparse protections remain portable;
- relevant adversarial and regression tests are updated.

### Platform-native, desktop, and packaging integrations

Reviewers should verify:

- platform permissions/capabilities/entitlements are the minimum required;
- external share/drop/protocol activation remains review-before-send;
- Linux desktop-entry/protocol-handler/package behavior remains aligned with maintained application identifiers;
- MAUI, Share Extension, Windows package, and Avalonia versions stay aligned through `scripts/validate_version_alignment.py`;
- platform metadata changes are reflected in platform validators, CODEOWNERS, and release documentation;
- signed-device or real-distribution behavior is not inferred from source compilation alone.

### Dependencies and automation

Reviewers should verify:

- dependency changes pass NuGet vulnerability auditing and notice/license review;
- GitHub Actions changes preserve least-privilege permissions and existing release-critical triggers;
- Linux `linux-x64`/`linux-arm64` packaging remains part of aggregate release readiness;
- manual release-evidence validation/generation/status tooling remains fail-closed;
- verification helpers remain executable from both portable Linux/Bash and Windows/PowerShell paths where applicable;
- changes do not bypass Linux integration, version alignment, repository completion, or release-evidence checks.

### Documentation-only changes

Documentation must remain truthful about evidence level. A document must not upgrade a claim from “implemented” or “compiled” to “signed/device validated” without actual exact-candidate evidence.

Historical ledgers may describe an earlier version or repository state, but canonical current-status and release-preparation documents must identify the current prepared candidate accurately.

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
- explicit ownership remains assigned to `@sanskarIN` for automation/toolchain, Core Security/Discovery/Protocol/Networking/Transfer/Storage, MAUI native platform code, Avalonia desktop, Share Extension, Linux packaging, and security/protocol/release/platform documentation;
- this governance document remains part of the canonical repository/documentation surface;
- governance requirements coexist with the maintained Linux, 2.5.18 version-alignment, and manual release-evidence completion contracts.

The validator runs through normal CI and both portable verification entry points. This makes accidental deletion or silent weakening of the source-side ownership policy a test failure.

## 2.5.18 integration note

The governance history was preserved in the active 2.5.18 integration candidate through two-parent merge commit `cfeb728682c11c0f20eaaf9c63de775ae3d26716`. Where the older governance branch overlapped newer Linux/2.5.18 files, the current candidate retains the newer release identifiers and adds governance assertions explicitly rather than restoring the historical `1.0.0` source state.

The release remains a prepared candidate until the exact final head passes its required automated and signed/manual evidence gates.

## Related references

- [Contributing](../CONTRIBUTING.md)
- [Security policy](../SECURITY.md)
- [Repository completion validation](testing/repository-completion-validation.md)
- [CI reference](testing/ci-reference.md)
- [2.5.18 release preparation](release/2.5.18-preparation.md)
- [Release process](release/release-process.md)
- [Release checklist](release/release-checklist.md)
- [Linux platform guide](platforms/linux.md)

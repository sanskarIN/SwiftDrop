from pathlib import Path
import subprocess

RUNTIME_HEAD = "406c2cfb48c45e04cc34662776e67a68f167745d"
CANDIDATE_HEAD = "6b1544b3a91ecfef2937a909f58a7e9faee31cff"
PLATFORM_RUN = "31877893372"
RELEASE_RUN = "31878003640"

PLATFORM_ANDROID = "sha256:64d42198e8ba039b3e27a0f9dbe29456f67b6194102ce7a4e90e94ce5e50d072"
PLATFORM_WINDOWS = "sha256:b91275a0a3af00ca913f0fbbe9811b156b7d366fbcaa8ea0e032814c75bccf79"
PLATFORM_APPLE = "sha256:9ff19f16d821d92dbb131320587887882505565e7f8b1e30264ee36c22f15f9c"
RELEASE_PORTABLE = "sha256:f409ded988ab83a844879cdc508c9bb1eebba03bdf3b84f936790a88b0582f34"
RELEASE_ANDROID = "sha256:d4041649f7d12205c8efe8ba89f68849729287e990094251bdb915d8b495511b"
RELEASE_WINDOWS = "sha256:77d8bbe5c9b6003c9167217dc454b034e27df89f41889dbfaad283e295c26a86"
RELEASE_APPLE = "sha256:4eb6679649fb4e3224ee024e02294a09c65c1cbe6fe062c0d9bd9fa87d277713"

ledger = Path("what_changed.md")
text = ledger.read_text(encoding="utf-8")
if "## 187." not in text:
    addition = f"""

## 187. Final receive/resume failure-side-effect hardening

- Final repository audit found that `TransferEngine.ReceiveFileAsync` validated the negotiated resume offset only after destination-directory/staging I/O.
- A positive resume used `OpenOrCreate`, so a missing staged partial could create an empty `.swiftdrop.part` before failing.
- Runtime fix `d82604faf23d8983a96606bb89fe1940d9a63702` validates the manifest offset before filesystem mutation, creates destination parents only for a fresh offset-zero receive, and uses `FileMode.Open` for positive resume so staged state must already exist.
- Regressions in `61e4c1bce6228ccf9defe917360a6bf0e3f43a3e` protect invalid offsets and missing positive-resume state without destination/partial creation.
- Portable exception semantics were corrected in test-only commit `0e98960323743a6cb859993958050e36ec2ce970`: a missing parent may surface as `DirectoryNotFoundException` and a missing file as `FileNotFoundException`, so the regression asserts the shared `IOException` contract while retaining exact no-side-effect assertions.

## 188. Final external-source staging safety alignment

- Audit found `ExternalFileStager` used plain `FileInfo` instead of SwiftDrop's regular-file source safety primitive.
- Runtime fix `c16c08396bd7b4a8b39d9ad4f72be953f98a0699` routes external staging through `TransferSourceSafety.GetRegularFile`, aligning security-scoped/native staging with direct send/drop behavior.
- Symlink/reparse source inputs are therefore rejected rather than followed into staging.
- Regression `842c92a40f7a9a5998b491160e91e445a1fa5079` creates a symlink where supported and verifies rejection occurs before any staging output is created.

## 189. Exact-expiry semantics for one-time credentials and discovery

- Numeric one-time pairing codes previously accepted `now == expires`; runtime fix `4c07b886be42dfad0f4f0067636ed0497736e018` changes consumption to require strictly pre-expiry time, matching QR/deep-link pairing semantics.
- Regression `2a5ba4211b07accdee91fe4471ac7ef6924175f0` protects exact-expiry rejection.
- One-time protocol authorizations had the same boundary issue in both consumption and pruning. Runtime fix `01d8e8a4358b3521e890d6b674f7f7c237413caa` makes `expires == now` expired for both paths; regression `34599d9047e95811399640b9f2ca494608375958` covers consumption and pruning at the exact instant.
- Discovered peers likewise remained present at exactly their configured lifetime boundary. Runtime fix `6b5c972fdf57bace85d52a0ad91edbe45ecef058` makes expiry inclusive at the boundary, with regression `bf9013bd96e009e234a55ad909082be2167a35d2`.

## 190. Atomic bounded security-state admission

- Audit found `AttemptRateLimiter` checked `maxKeys` before unsynchronized distinct-key insertion, so concurrent first-seen peers could race past the configured memory/abuse bound.
- Runtime fix `b92c67acb8b97b5608deeb7e3281c4bcc8ba96db` serializes new-key admission with expiry pruning and a post-prune cap recheck while retaining per-key queue locking.
- Regression `7e6d833fe84a63e422aa2b638a226c4d3817d3c3` stresses 512 distinct concurrent peers against a 16-key cap and requires exactly 16 admissions.
- `OneTimeAuthorizationStore` had an analogous distinct-nonce capacity race. Final runtime source commit `{RUNTIME_HEAD}` adds atomic prune/duplicate/cap/add admission under an explicit gate.
- Regression `7fa8a1e3269d0783afbb8458b1f603226f7f8b5c` stresses 512 distinct nonces against a 16-entry cap and requires both successful admissions and retained count to remain exactly bounded.

## 191. mDNS record-boundary parser hardening

- Audit found known PTR/SRV parsing could read a compressed/uncompressed name beyond the record's declared RDATA length, consume bytes belonging to a following record, and then reset the cursor to the declared end.
- Runtime fix `dee79b3aa642f819f1b1f388727bccdd24ea6091` requires recognized PTR/SRV/TXT/A branches to consume exactly their declared RDATA payload while retaining normal DNS compression-pointer support.
- Malformed cross-record name reads now fail the announcement parser instead of borrowing following-record bytes.
- Regression `faeedc7d20c01d8ea8192b7e249607c6d7c57ea8` constructs a valid announcement, corrupts the PTR RDATA length to zero while leaving following bytes, and verifies rejection.

## 192. Release-readiness source/test trigger correction

- Final workflow audit found `release-readiness.yml` did not automatically run for ordinary `src/**`, `tests/**`, benchmark, solution, or central build-input changes; its path filter covered only the workflow and a narrow helper-script set.
- Candidate commit `{CANDIDATE_HEAD}` expands both `push` and `pull_request` path filters to production source, portable tests, benchmarks, `SwiftDrop.slnx`, `Directory.Build.props`, `global.json`, `NuGet.config`, and the existing verification/audit/evidence helper paths.
- This prevents normal candidate-affecting source/test/build changes from silently bypassing the aggregate Android/Windows/Apple release gate.
- Version tags and manual `workflow_dispatch` remain supported.

## 193. Final portable regression and documentation audit

- The final portable Core suite is **569/569 xUnit tests**, up from 559 at the start of this pass.
- The Python validation-helper suite remains **26/26 tests**.
- Final Ubuntu candidate verification confirmed documentation integrity, English/Hindi localization parity, Apple integration metadata, Windows package/notification metadata, Core Release compilation, benchmark Release compilation, and a machine-readable Core vulnerable-package report with zero findings.
- Documentation audit removed duplicated aggregate-performance sections/privacy text and stale 559/21/16 current-state counts from README/build/CI guidance while preserving historical evidence entries.
- `README.md`, `BUILDING.md`, `docs/testing/ci-reference.md`, `docs/release/release-process.md`, `NEXT_STEPS.md`, `PROJECT_STATUS.md`, and `CHANGELOG.md` now describe the final 569/26 contract and the expanded source-triggered release-readiness gate.
- No additional mandatory source feature is intentionally left on the in-repository roadmap; future source changes should be driven by reproducible defects, dependency/platform changes, or deliberately scoped post-v1 work.

## 194. Exact final runtime platform evidence

- Exact runtime source-changing head: `{RUNTIME_HEAD}`.
- Hosted platform run **{PLATFORM_RUN}** completed successfully for Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, and iOS Simulator containing app, including target dependency/vulnerability audit generation and uploads.
- Platform evidence digests recorded by GitHub Actions:
  - `android-dependency-audit`: `{PLATFORM_ANDROID}`
  - `windows-dependency-audit`: `{PLATFORM_WINDOWS}`
  - `apple-dependency-audit`: `{PLATFORM_APPLE}`
- Later regression/workflow/documentation commits do not change application runtime source from this exact platform-tested head.

## 195. Final aggregate release-readiness evidence

- Final source/test/release-trigger candidate: `{CANDIDATE_HEAD}`.
- Release-readiness run **{RELEASE_RUN}** completed successfully for `core-and-tests`, Android compile/audit, Windows compile/audit, Apple Mac Catalyst + iOS Share Extension + iOS containing-app compile/audits, and the final aggregate `release-gate`.
- Release evidence digests recorded by GitHub Actions:
  - `dependency-audit`: `{RELEASE_PORTABLE}`
  - `android-dependency-audit`: `{RELEASE_ANDROID}`
  - `windows-dependency-audit`: `{RELEASE_WINDOWS}`
  - `apple-dependency-audit`: `{RELEASE_APPLE}`
- This run directly exercises the corrected release-readiness workflow after its path-trigger expansion.

## 196. Final in-repository completion boundary

- The complete tracked source/test/workflow/documentation tree received a final defect-oriented review covering transfer/resume, source staging, one-time credentials, bounded security state, discovery/mDNS parsing, SQLite/history/queue/trust persistence, protocol/path/storage boundaries, platform integration metadata, CI/release automation, and current-state documentation.
- Every reproducible in-repository defect found in this pass was fixed and protected by regression coverage rather than waived.
- The final runtime source boundary is `{RUNTIME_HEAD}`; test/release-trigger contract boundary is `{CANDIDATE_HEAD}`. Subsequent documentation/ledger/helper-cleanup commits do not alter runtime source.
- Production readiness must still be evidenced externally from signed artifacts and real environments: Android AAB/APK signing/install/upgrade/policy/notification behavior; Windows signed MSIX install/update/protocol/firewall/app-notification registration/activation; Apple Developer App Group/profiles, signed iOS Share Extension/provider behavior, iOS/Mac notification behavior, Mac signing/sandbox/notarization; physical cross-device/network/storage/low-space/lifecycle/SecureStorage tests; accessibility/localization on real assistive technologies; exact signed-artifact dependency/license/provenance reconciliation; and final store/privacy declarations.
- Hosted source validation must not be described as proof of those signed/device/store requirements.
"""
    ledger.write_text(text.rstrip() + addition + "\n", encoding="utf-8")

subprocess.run(["git", "add", "what_changed.md"], check=True)
if subprocess.run(["git", "diff", "--cached", "--quiet"]).returncode != 0:
    subprocess.run([
        "git", "commit", "-m", "docs(ledger): record final defect audit",
        "-m", "Signed-off-by: Sanskar <sanskarin@outlook.in>"
    ], check=True)
    subprocess.run(["git", "push", "origin", "HEAD:main"], check=True)

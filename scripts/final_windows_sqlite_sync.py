from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text, encoding="utf-8", newline="\n")


def insert_after_once(text: str, marker: str, insertion: str, guard: str) -> str:
    if guard in text:
        return text
    if marker not in text:
        raise SystemExit(f"marker not found: {marker!r}")
    return text.replace(marker, marker + insertion, 1)


# README: surface the two-OS portable contract and latest test count.
path = "README.md"
text = read(path)
marker = "- Python validation-helper regression tests;\n"
insertion = "- two-OS portable verification on Ubuntu and Windows PowerShell, currently covering 517 xUnit tests;\n"
text = insert_after_once(text, marker, insertion, "- two-OS portable verification on Ubuntu and Windows PowerShell")
marker = "- deterministic SHA-256 manifests for retained dependency-evidence JSON bundles;\n"
insertion = "- deterministic SQLite command/resource disposal validated by Windows temp-database cleanup;\n"
text = insert_after_once(text, marker, insertion, "- deterministic SQLite command/resource disposal validated by Windows temp-database cleanup;")
write(path, text)


# PROJECT_STATUS: newest evidence belongs immediately after the Updated line.
path = "PROJECT_STATUS.md"
text = read(path)
section = """## August 14 Windows/SQLite resource-lifetime and final hosted matrix continuation

- Normal CI now has a dedicated `windows-portable-verifier` job executing `scripts/verify-core.ps1`; the Windows path is an enforced contract rather than an unexecuted helper script.
- The first Windows verifier exposed a PowerShell interpolation parser defect (`$LASTEXITCODE:`), fixed in signed commit `080126a0` without weakening the gate.
- Subsequent Windows execution exposed SQLite database-file locks that Linux had not revealed. Test teardown now clears Microsoft.Data.Sqlite pools before deleting isolated temp DB/`-wal`/`-shm` files, and schema tests dispose connections before cleanup.
- The investigation found a real production resource-lifetime defect: SQLite command objects were not deterministically disposed. `DatabaseSchemaManager`, `BatchCompletionStore`, `DiagnosticEventStore`, `TransferHistoryStore`, `TransferQueueMetadataStore`, and `TrustStore` now dispose commands explicitly; schema transactions/readers remain scoped as well.
- Added a direct pooled SQLite cleanup regression. The portable xUnit suite is now **517 tests**.
- Exact source-head CI run `31785808946` passed the complete 517-test contract on both Ubuntu and Windows, including 10 Python helper tests, documentation/localization/Apple metadata validation, Core/benchmark builds, and zero-finding machine-readable Core vulnerability validation.
- Source-head CodeQL run `31785808918` and security-hygiene run `31785808999` passed after the storage resource-lifetime fixes.
- Platform run `31786513898` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator containing app, all target dependency audits, and audit-artifact uploads using the current source plus the maintained platform workflow.
- Same-ref concurrency controls were added to platform CI, core CI, CodeQL, and security hygiene so rapid focused commits keep the newest branch evidence instead of allowing superseded runs to block hosted capacity.
- Current-main CI run `31786693757` passed both Ubuntu and Windows portable jobs with **517/517** xUnit tests; CodeQL run `31786693816` also passed on the same documentation/workflow state before this final status synchronization.
- The repository still does not claim production readiness from hosted evidence alone. Signed Android/Windows/Apple packaging, physical device/network/provider/accessibility validation, exact final package dependency/license/provenance reconciliation, App Group/notarization, and store/privacy checks remain required.

"""
text = insert_after_once(text, "Updated: 2026-08-14\n\n", section, "## August 14 Windows/SQLite resource-lifetime and final hosted matrix continuation")
# Keep current snapshot counts current while preserving historical sections later in the file.
text = text.replace("- **516/516 portable tests passed**;", "- **517/517 portable tests passed**;", 1)
text = text.replace("- **511/511 portable tests passed**;", "- **517/517 portable tests passed**;", 1)
write(path, text)


# NEXT_STEPS: mark the source-level Windows/SQLite work complete, leaving external gates untouched.
path = "NEXT_STEPS.md"
text = read(path)
section = """### Two-OS portable and SQLite resource-lifetime hardening completed on August 14

- `ci.yml` now requires both Ubuntu portable verification and the Windows PowerShell verifier.
- Windows CI exposed and drove fixes for a PowerShell parser defect and SQLite native-handle/file-lock behavior that Ubuntu alone did not reveal.
- SQLite-backed test teardown clears idle pools before deleting isolated temp DB/`-wal`/`-shm` files; a direct regression protects the cleanup helper.
- Every Core SQLite storage component now disposes command objects deterministically, with readers/connections/transactions scoped around actual use.
- Current portable coverage is 517 xUnit tests plus 10 Python validation-helper tests; the full contract has passed on both Ubuntu and Windows.
- Platform compile/audit run `31786513898` validates the resulting source across Android, focused Windows, Mac Catalyst, iOS Share Extension, and iOS containing app.
- Same-ref CI/platform/CodeQL/security concurrency now prevents superseded intermediate commits from blocking the newest branch evidence.
- No additional source workaround is planned for SQLite file locking; future recurrence should be treated as a resource-lifetime regression and fixed rather than hidden with sleeps/retries.

"""
text = insert_after_once(text, "## Source work completed through the August 14 continuation\n\n", section, "### Two-OS portable and SQLite resource-lifetime hardening completed on August 14")
text = text.replace("portable xUnit count is now 516.", "portable xUnit count is now 517.", 1)
write(path, text)


# CHANGELOG: latest engineering change first in Unreleased.
path = "CHANGELOG.md"
text = read(path)
section = """### Windows portable verification and SQLite resource-lifetime hardening

- Added a dedicated Windows PowerShell portable-verifier CI job so Core tests, helper/documentation validators, benchmark compilation, and vulnerable-package validation execute on both Ubuntu and Windows.
- Fixed the PowerShell `${LASTEXITCODE}` interpolation parser defect exposed by the first Windows run.
- Added cross-platform SQLite temporary-database cleanup that clears Microsoft.Data.Sqlite pools and removes DB/WAL/SHM files, plus a direct cleanup regression.
- Fixed deterministic SQLite command disposal throughout schema migration, batch-completion, diagnostics, transfer-history, queue-metadata, and trust stores after Windows file-lock testing exposed retained native resources.
- Explicitly scoped schema-test connections before temp-file cleanup rather than masking handle-lifetime failures with retries.
- Portable xUnit coverage is now **517 tests**; exact source-head CI run `31785808946` passed all tests on Ubuntu and through the Windows PowerShell verifier.
- Source-head CodeQL `31785808918` and security hygiene `31785808999` passed after the storage fixes.
- Platform run `31786513898` passed Android, focused Windows, Mac Catalyst, iOS Simulator Share Extension, iOS Simulator app, target vulnerability audits, evidence manifests, and artifact uploads.
- Added same-ref concurrency controls to platform/core/CodeQL/security workflows so obsolete intermediate runs are cancelled in favor of the newest branch evidence.

"""
text = insert_after_once(text, "## Unreleased - 2026-08-14\n\n", section, "### Windows portable verification and SQLite resource-lifetime hardening")
write(path, text)


# what_changed.md: append without rewriting any historical section.
path = "what_changed.md"
text = read(path)
if "# 144. Windows portable verification became a required CI contract" not in text:
    footer = "**Made by the Sanskar**"
    idx = text.rfind(footer)
    if idx < 0:
        raise SystemExit("what_changed footer not found; refusing to rewrite history")
    base = text[:idx].rstrip()
    appendix = r"""

---

# 144. Windows portable verification became a required CI contract

Commit `e858fc4a` added the `windows-portable-verifier` job to normal CI and made `scripts/verify-core.ps1` execute on a real Windows hosted runner.

The first Windows execution, run `31784473076`, immediately found a PowerShell parser error in the native-command error message: `$LASTEXITCODE:` was parsed as an invalid variable reference. Commit `080126a0` changed the interpolation to `${LASTEXITCODE}:`.

The Windows gate was deliberately retained. It exists because platform-specific process, filesystem, and native-library behavior cannot be proven by Ubuntu-only execution.

# 145. SQLite test teardown was made portable instead of retry-based

Windows then exposed SQLite temp-database file locks after otherwise-successful tests.

Focused test commits introduced `SqliteTestDatabaseCleanup` and applied it across transfer history, history maintenance, schema migration, diagnostic events, completed-batch metadata, trusted peers, and transfer-queue metadata tests. The helper calls `SqliteConnection.ClearAllPools()` before deleting the isolated database plus `-wal` and `-shm` companions.

A direct `SqliteTestDatabaseCleanupTests` regression creates a pooled temporary database, disposes the connection, invokes cleanup, and verifies that the main/WAL/SHM files are gone. This raised the portable xUnit suite to 517 tests.

Schema migration tests were also changed from broad method-lifetime `await using var` connections to explicit `await using (...)` scopes so connection disposal necessarily occurs before the outer cleanup `finally`.

Arbitrary sleeps/retries were not introduced. A locked test database remains a signal of incorrect SQLite resource ownership.

# 146. Windows testing exposed a production SQLite resource-lifetime defect

Even after pool-aware test cleanup and explicit schema-test connection scopes, the version-zero migration path still retained the database on Windows. Inspection found the real cause in production storage code: SQLite commands were created without deterministic disposal.

Focused signed production fixes:

- `ef8d9deb` — `DatabaseSchemaManager` disposes version/migration commands;
- `a87b486e` — `BatchCompletionStore` disposes all commands;
- `c6be1c1a` — `DiagnosticEventStore` disposes all commands;
- `07616b2a` — `TransferHistoryStore` disposes all commands;
- `ab8a6605` — `TransferQueueMetadataStore` disposes all commands;
- `13af8507` — `TrustStore` disposes all commands.

Readers/connections and migration transactions remain scoped to their actual operation. The Core storage directory was audited after these changes; every Microsoft.Data.Sqlite command-owning storage component is covered by deterministic command disposal.

# 147. Two-OS portable evidence reached 517 tests

Exact source-head CI run `31785808946` completed successfully after the production disposal fixes.

Ubuntu `core` passed:

- 10 Python helper tests;
- documentation validation;
- localization validation;
- Apple integration metadata validation;
- Core Release build;
- **517/517 xUnit tests**;
- benchmark Release build;
- Core machine-readable vulnerable-package validation with zero findings.

Windows `windows-portable-verifier` passed the same PowerShell verification contract, including **517/517 xUnit tests**, benchmark compilation, and zero-finding vulnerable-package validation on Windows.

The Windows success proves that the earlier schema/database-lock failures are closed under the maintained hosted verifier instead of merely passing on Linux.

Source-head CodeQL run `31785808918` and security-hygiene run `31785808999` also succeeded.

# 148. Superseded branch runs no longer block the newest evidence

This continuation produced many intentionally focused commits, which exposed another engineering issue: older platform runs could occupy hosted runner capacity while a newer source head waited.

Focused workflow commits added same-ref concurrency cancellation:

- `7ef7b354` — platform build/audit matrices;
- `a870ff73` — core/two-OS CI;
- `9d9934f3` — CodeQL analysis;
- `51f94cc0` — repository security hygiene.

The platform concurrency change immediately allowed the newest Android, Windows, and Apple jobs to run together rather than waiting behind superseded intermediate matrices. This changes only CI scheduling; it does not skip or downgrade checks on the newest branch run.

# 149. Latest maintained platform matrix is green after the SQLite fixes

Platform run `31786513898` uses commit `7ef7b354`, which contains the complete SQLite production/test fixes plus the maintained platform workflow with concurrency control.

The run succeeded for:

- Android Release compile and dependency audit/upload;
- focused Windows Release compile and dependency audit/upload;
- Mac Catalyst containing-app Release compile and dependency audit;
- iOS Simulator Share Extension Release compile;
- iOS Simulator containing-app Release compile;
- separate iOS app/extension vulnerable-package validation;
- deterministic Apple dependency-evidence manifest generation and artifact upload.

This is hosted source/restored-graph evidence. It is not a signed AAB/APK, MSIX, iOS archive/TestFlight build, or notarized Mac distribution result.

# 150. Current-main portable/security evidence after workflow and documentation alignment

After the source fixes, normal CI/workflow documentation was aligned with the two-OS contract and concurrency behavior.

CI run `31786693757` passed both Ubuntu and Windows jobs on the aligned main state, including 517/517 xUnit tests on both paths. CodeQL run `31786693816` also completed successfully.

The final documentation synchronization and helper cleanup that follow this entry are documentation/repository-maintenance-only changes; they do not alter SwiftDrop transfer/storage runtime source.

# 151. Source/release boundary after Windows and SQLite hardening

The repository now additionally proves:

- PowerShell verification is actually executable on Windows;
- native-command failures are propagated reliably by the Windows verifier;
- SQLite test teardown handles connection pooling without hiding failures;
- production SQLite command objects have deterministic lifetimes across every Core SQLite store;
- the 517-test portable contract passes on both Ubuntu and Windows;
- CodeQL/security hygiene remain green after the resource-lifetime fixes;
- Android/Windows/Mac Catalyst/iOS hosted compilation and target dependency audits remain green after those fixes;
- obsolete intermediate CI runs no longer block newest same-ref platform/core/security evidence.

Still external/candidate-specific: production signing and packaging; physical Android/iOS/device-to-device transfer testing; signed App Group/Share Extension behavior; Windows MSIX install/update/protocol/firewall validation; Mac sandbox/notarization; real restricted-network/provider/lifecycle/low-storage testing; accessibility/localization on actual assistive technologies; exact final signed-artifact dependency/license/provenance reconciliation; and store/privacy publication checks.
"""
    write(path, base + appendix.rstrip() + "\n\n" + footer + "\n")

print("Final Windows/SQLite documentation synchronization complete.")

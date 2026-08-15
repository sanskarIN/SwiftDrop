from pathlib import Path
import subprocess

ledger = Path("what_changed.md")
text = ledger.read_text(encoding="utf-8")
if "## 197." not in text:
    addition = """

## 197. Final cleaned-main verification after defect-audit ledger

- After Sections 187–196 were written and the defect-audit trigger/workflow/writer were removed, `main` reached clean evidence head `429b26e626d819c6e741d9817e68f327e2595d9a`.
- CI run **31878772423** completed successfully on that exact head across both the Ubuntu Core job and Windows PowerShell portable verifier. The maintained contract remained **569/569 xUnit tests** plus **26/26 Python helper tests**, documentation/localization/Apple/Windows integration validation, Core/benchmark builds, and machine-readable portable vulnerability validation.
- CodeQL run **31878772418** completed successfully on the same head.
- Security-hygiene run **31878772419** completed successfully on the same head.
- `.github/workflows` contained exactly the five maintained workflows: `ci.yml`, `codeql.yml`, `platform-builds.yml`, `release-readiness.yml`, and `security-hygiene.yml` before this documentation-only appendix helper was staged.
- This appendix and its helper cleanup do not change runtime source `406c2cfb48c45e04cc34662776e67a68f167745d` or the final source/test/release-trigger candidate `6b1544b3a91ecfef2937a909f58a7e9faee31cff`.
- Signed package/device/network/provider/accessibility/store validation remains external release evidence and is not replaced by hosted source checks.
"""
    ledger.write_text(text.rstrip() + addition + "\n", encoding="utf-8")

subprocess.run(["git", "add", "what_changed.md"], check=True)
if subprocess.run(["git", "diff", "--cached", "--quiet"]).returncode != 0:
    subprocess.run([
        "git", "commit", "-m", "docs(ledger): record final clean evidence",
        "-m", "Signed-off-by: Sanskar <sanskarin@outlook.in>"
    ], check=True)
    subprocess.run(["git", "push", "origin", "HEAD:main"], check=True)

from pathlib import Path
import subprocess

ledger = Path('what_changed.md')
text = ledger.read_text(encoding='utf-8')
if '## 186.' not in text:
    addition = '''

## 186. Post-ledger cleaned-main verification

- After Sections 178–185 were written and the first temporary ledger trigger/workflow/writer were removed, `main` reached cleanup head `70022841a02efd9714ad514baf674d5a10207c4e`.
- CI run **31876713728** completed successfully on that head across both the Ubuntu Core job and Windows PowerShell portable verifier, including the **26 Python helper tests**, **559 xUnit tests**, documentation/localization/platform metadata validation, Core/benchmark builds, and portable vulnerability audit.
- CodeQL run **31876713729** completed successfully on the same cleanup head.
- Security-hygiene run **31876713709** completed successfully on the same cleanup head.
- The repository workflow directory had returned to exactly the five maintained workflows: `ci.yml`, `codeql.yml`, `platform-builds.yml`, `release-readiness.yml`, and `security-hygiene.yml` before this documentation-only evidence appendix was staged.
- This appendix and its temporary helper cleanup do not alter application/runtime source or the source/test contract established at `9e637b909550ea433bf0c453774d6ab20ba7f605` and `3df4a50836a64655fbf1fb990d0946198f32b52b` respectively.
'''
    ledger.write_text(text.rstrip() + addition + '\n', encoding='utf-8')

subprocess.run(['git', 'add', 'what_changed.md'], check=True)
if subprocess.run(['git', 'diff', '--cached', '--quiet']).returncode != 0:
    subprocess.run([
        'git', 'commit', '-m', 'docs(ledger): record cleaned trend evidence',
        '-m', 'Signed-off-by: Sanskar <sanskarin@outlook.in>'
    ], check=True)
    subprocess.run(['git', 'push', 'origin', 'HEAD:main'], check=True)

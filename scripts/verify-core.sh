#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

audit_report="$(mktemp)"
trap 'rm -f "$audit_report"' EXIT

dotnet --info
python3 -m unittest discover -s scripts/tests -p 'test_*.py'
python3 scripts/validate_documentation.py
python3 scripts/validate_localization.py
python3 scripts/validate_apple_integration.py
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj
dotnet restore tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj
dotnet restore benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release --no-restore
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release --no-restore --logger "console;verbosity=normal"
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release --no-restore
dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --vulnerable --format json --output-version 1 > "$audit_report"
python3 scripts/validate_nuget_vulnerability_report.py "$audit_report"

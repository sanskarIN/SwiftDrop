#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet --info
python3 scripts/validate_localization.py
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj
dotnet restore tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj
dotnet restore benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release --no-restore
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release --no-restore --logger "console;verbosity=normal"
dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release --no-restore

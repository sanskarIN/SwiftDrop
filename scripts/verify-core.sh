#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

dotnet --info
dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj
dotnet restore tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj
dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release --no-restore
dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release --no-restore --logger "console;verbosity=normal"

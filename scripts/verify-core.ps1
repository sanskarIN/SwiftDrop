$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot
try {
    dotnet --info
    if (Get-Command python -ErrorAction SilentlyContinue) {
        python scripts/validate_localization.py
    }
    elseif (Get-Command py -ErrorAction SilentlyContinue) {
        py -3 scripts/validate_localization.py
    }
    else {
        throw 'Python 3 is required to validate localization catalogs.'
    }
    dotnet restore src/SwiftDrop.Core/SwiftDrop.Core.csproj
    dotnet restore tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj
    dotnet restore benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj
    dotnet build src/SwiftDrop.Core/SwiftDrop.Core.csproj -c Release --no-restore
    dotnet test tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj -c Release --no-restore --logger 'console;verbosity=normal'
    dotnet build benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj -c Release --no-restore
}
finally {
    Pop-Location
}

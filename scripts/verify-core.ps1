$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')

function Invoke-CheckedNative {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [string[]]$CommandArguments = @()
    )

    & $FilePath @CommandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Native command failed with exit code ${LASTEXITCODE}: $FilePath $($CommandArguments -join ' ')"
    }
}

$pythonCommand = $null
$pythonPrefix = @()
if (Get-Command python -ErrorAction SilentlyContinue) {
    $pythonCommand = 'python'
}
elseif (Get-Command py -ErrorAction SilentlyContinue) {
    $pythonCommand = 'py'
    $pythonPrefix = @('-3')
}
else {
    throw 'Python 3 is required to validate documentation, localization, Apple/Windows integration metadata, and NuGet audit reports.'
}

function Invoke-Python {
    param([string[]]$CommandArguments = @())
    Invoke-CheckedNative -FilePath $pythonCommand -CommandArguments (@($pythonPrefix) + $CommandArguments)
}

Push-Location $repoRoot
try {
    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('--info')
    Invoke-Python -CommandArguments @('-m', 'unittest', 'discover', '-s', 'scripts/tests', '-p', 'test_*.py')
    Invoke-Python -CommandArguments @('scripts/validate_documentation.py')
    Invoke-Python -CommandArguments @('scripts/validate_localization.py')
    Invoke-Python -CommandArguments @('scripts/validate_apple_integration.py')
    Invoke-Python -CommandArguments @('scripts/validate_windows_integration.py')

    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('restore', 'src/SwiftDrop.Core/SwiftDrop.Core.csproj')
    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('restore', 'tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj')
    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('restore', 'benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj')
    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('build', 'src/SwiftDrop.Core/SwiftDrop.Core.csproj', '-c', 'Release', '--no-restore')
    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('test', 'tests/SwiftDrop.Core.Tests/SwiftDrop.Core.Tests.csproj', '-c', 'Release', '--no-restore', '--logger', 'console;verbosity=normal')
    Invoke-CheckedNative -FilePath 'dotnet' -CommandArguments @('build', 'benchmarks/SwiftDrop.Benchmarks/SwiftDrop.Benchmarks.csproj', '-c', 'Release', '--no-restore')

    $auditReport = Join-Path ([System.IO.Path]::GetTempPath()) ("swiftdrop-core-vulnerabilities-{0}.json" -f [guid]::NewGuid().ToString('N'))
    try {
        & dotnet package list --project src/SwiftDrop.Core/SwiftDrop.Core.csproj --include-transitive --vulnerable --format json --output-version 1 | Out-File -FilePath $auditReport -Encoding utf8
        if ($LASTEXITCODE -ne 0) {
            throw "NuGet vulnerability report generation failed with exit code $LASTEXITCODE."
        }
        Invoke-Python -CommandArguments @('scripts/validate_nuget_vulnerability_report.py', $auditReport)
    }
    finally {
        Remove-Item -LiteralPath $auditReport -Force -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}

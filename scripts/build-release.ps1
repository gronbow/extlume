[CmdletBinding()]
param(
    [switch]$SkipLiveTests
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))

$testArguments = if ($SkipLiveTests) { '--skip-live' } else { '' }
& cmd.exe /d /c "`"$projectRoot\test.cmd`" $testArguments"
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    (Join-Path $PSScriptRoot 'build-installer.ps1') -SkipBuild
if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed with exit code $LASTEXITCODE."
}

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
    (Join-Path $PSScriptRoot 'package-release.ps1') -SkipTests -SkipBuild
if ($LASTEXITCODE -ne 0) {
    throw "Portable package build failed with exit code $LASTEXITCODE."
}

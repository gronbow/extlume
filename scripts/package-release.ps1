[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = Join-Path $projectRoot 'artifacts'
$releaseRoot = Join-Path $artifactsRoot 'release'
$stageRoot = Join-Path $artifactsRoot 'portable-stage'
$version = '0.3.0-beta.1'
$archiveName = "ExtLume-$version-portable.zip"
$archivePath = Join-Path $releaseRoot $archiveName

function Assert-ChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Child
    )

    $parentPath = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    $childPath = [System.IO.Path]::GetFullPath($Child)
    if (-not $childPath.StartsWith($parentPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the artifact directory: $childPath"
    }
}

if (-not $SkipTests) {
    & cmd.exe /d /c "`"$projectRoot\test.cmd`""
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE."
    }
}
elseif (-not $SkipBuild) {
    & cmd.exe /d /c "`"$projectRoot\build.cmd`""
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
Assert-ChildPath -Parent $artifactsRoot -Child $stageRoot
Assert-ChildPath -Parent $artifactsRoot -Child $archivePath

Get-ChildItem -LiteralPath $releaseRoot -File | ForEach-Object {
    Assert-ChildPath -Parent $releaseRoot -Child $_.FullName
    Remove-Item -LiteralPath $_.FullName -Force
}

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

New-Item -ItemType Directory -Path $stageRoot | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stageRoot 'docs') | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stageRoot 'assets') | Out-Null

$rootFiles = @(
    'LICENSE',
    'README.md',
    'README.zh-CN.md',
    'PRIVACY.md'
)
foreach ($file in $rootFiles) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $stageRoot
}

$docFiles = @(
    'COMPATIBILITY.md',
    'COMPATIBILITY.zh-CN.md',
    'VALIDATION_REPORT_v0.3.0-beta.1.md'
)
foreach ($file in $docFiles) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "docs\$file") -Destination (Join-Path $stageRoot 'docs')
}

$assetFiles = @(
    'app-logo.png',
    'ui-preview.png'
)
foreach ($file in $assetFiles) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "assets\$file") -Destination (Join-Path $stageRoot 'assets')
}

Copy-Item -LiteralPath (Join-Path $artifactsRoot 'ExtLume.exe') -Destination $stageRoot
Compress-Archive -Path (Join-Path $stageRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal
Copy-Item -LiteralPath (Join-Path $artifactsRoot 'ExtLume.exe') -Destination $releaseRoot -Force

$checksumTargets = @(
    (Join-Path $releaseRoot 'ExtLume.exe'),
    $archivePath
)
$installerPath = Join-Path $artifactsRoot "installer\ExtLume-$version-Setup.exe"
if (Test-Path -LiteralPath $installerPath) {
    $releaseInstallerPath = Join-Path $releaseRoot ([System.IO.Path]::GetFileName($installerPath))
    Copy-Item -LiteralPath $installerPath -Destination $releaseInstallerPath -Force
    $checksumTargets += $releaseInstallerPath
}
$checksumLines = foreach ($target in $checksumTargets) {
    $hash = Get-FileHash -LiteralPath $target -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), [System.IO.Path]::GetFileName($target)
}
$checksumPath = Join-Path $releaseRoot 'SHA256SUMS.txt'
$checksumLines | Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Host "Release artifacts:"
Get-ChildItem -LiteralPath $releaseRoot | Select-Object Name, Length

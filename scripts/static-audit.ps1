[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure {
    param([string]$Message)
    $script:failures.Add($Message)
}

function Find-Text {
    param(
        [string[]]$Paths,
        [string]$Pattern
    )

    $files = foreach ($path in $Paths) {
        if (Test-Path -LiteralPath $path) {
            Get-ChildItem -LiteralPath $path -Recurse -File |
                Where-Object { $_.Extension -in @('.cs', '.ps1', '.cmd', '.iss', '.yml', '.yaml') }
        }
    }
    return $files | Select-String -Pattern $Pattern -CaseSensitive:$false
}

$sourceRoot = Join-Path $projectRoot 'src'
$productionModelLeaks = Find-Text -Paths @($sourceRoot) -Pattern 'H25T7'
if ($productionModelLeaks) {
    Add-Failure 'A monitor-specific H25T7 identifier is present in production source.'
}

$privatePaths = Find-Text -Paths @(
    $sourceRoot,
    (Join-Path $projectRoot 'scripts'),
    (Join-Path $projectRoot '.github')
) -Pattern '[A-Z]:\\(Users|Codex)\\'
if ($privatePaths) {
    Add-Failure 'A private absolute Windows path is present in tracked source or automation.'
}

$networkApis = Find-Text -Paths @($sourceRoot) -Pattern 'HttpClient|WebRequest|WebClient|TcpClient|UdpClient|Socket\s*\('
if ($networkApis) {
    Add-Failure 'A network API is present in production source.'
}

$secretPatterns = Find-Text -Paths @(
    $sourceRoot,
    (Join-Path $projectRoot 'scripts')
) -Pattern '(api[_-]?key|client[_-]?secret|password)\s*='
if ($secretPatterns) {
    Add-Failure 'A possible hard-coded secret is present.'
}

$nativeMethodsPath = Join-Path $sourceRoot 'Interop\NativeMethods.cs'
$ddcPath = Join-Path $sourceRoot 'Services\DdcBrightnessService.cs'
$nativeText = Get-Content -LiteralPath $nativeMethodsPath -Raw
$ddcText = Get-Content -LiteralPath $ddcPath -Raw

if ($nativeText -notmatch 'BrightnessVcpCode\s*=\s*0x10') {
    Add-Failure 'BrightnessVcpCode is not fixed to VCP 0x10.'
}
$allSetVcpCalls = [regex]::Matches(
    $ddcText,
    'NativeMethods\.SetVCPFeature\s*\(',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)
$safeSetVcpCalls = [regex]::Matches(
    $ddcText,
    'NativeMethods\.SetVCPFeature\s*\(\s*[^,]+,\s*NativeMethods\.BrightnessVcpCode\s*,',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)
if ($allSetVcpCalls.Count -ne 1 -or $safeSetVcpCalls.Count -ne 1) {
    Add-Failure 'SetVCPFeature is called without the fixed brightness VCP constant.'
}
if ($ddcText -notmatch 'DestroyPhysicalMonitors') {
    Add-Failure 'Physical-monitor handle cleanup is missing.'
}
$highLevelFlow = [regex]::Match(
    $ddcText,
    'internal static BrightnessResult SetHighLevel\s*\([\s\S]+?(?=internal static BrightnessResult SetVcp)',
    [System.Text.RegularExpressions.RegexOptions]::Singleline).Value
$vcpFlow = [regex]::Match(
    $ddcText,
    'internal static BrightnessResult SetVcp\s*\([\s\S]+?(?=private sealed class PhysicalMonitorLease)',
    [System.Text.RegularExpressions.RegexOptions]::Singleline).Value
if ($highLevelFlow -notmatch 'adapter\.TryReadHighLevel[\s\S]+adapter\.SetHighLevel[\s\S]+adapter\.TryReadHighLevel') {
    Add-Failure 'High-level brightness write does not contain a readback.'
}
if ($vcpFlow -notmatch 'adapter\.TryReadVcp[\s\S]+adapter\.SetVcp[\s\S]+adapter\.TryReadVcp') {
    Add-Failure 'VCP brightness write does not contain a readback.'
}
if ($ddcText -notmatch 'NativeMethods\.GetMonitorBrightness' -or
    $ddcText -notmatch 'NativeMethods\.SetMonitorBrightness') {
    Add-Failure 'The native high-level brightness adapter is incomplete.'
}
if ($ddcText -notmatch 'NativeMethods\.GetVCPFeatureAndVCPFeatureReply') {
    Add-Failure 'The native VCP brightness read adapter is missing.'
}

$softwareDimmingPath = Join-Path $sourceRoot 'Services\SoftwareDimmingService.cs'
$softwareDimmingText = Get-Content -LiteralPath $softwareDimmingPath -Raw
if ($softwareDimmingText -notmatch '!target\.SharesSourceWithInternal') {
    Add-Failure 'Software dimming does not block a source mirrored to the built-in display.'
}
if ($ddcText -notmatch 'SoftwareDimmingService\.IsSafeGroup\s*\(') {
    Add-Failure 'The DDC fallback bypasses the software-dimming safety policy.'
}
if ($ddcText -notmatch 'SharesSourceWithInternal[\s\S]+physicalCount != targets\.Count') {
    Add-Failure 'An internal clone can reach positional physical-monitor mapping.'
}

$expectedVersion = '0.2.0-beta.1'
$appInfo = Get-Content -LiteralPath (Join-Path $sourceRoot 'AppInfo.cs') -Raw
$installer = Get-Content -LiteralPath (Join-Path $projectRoot 'installer\ExtLume.iss') -Raw
if ($appInfo -notmatch [regex]::Escape('Version = "' + $expectedVersion + '"')) {
    Add-Failure 'AppInfo version does not match the release version.'
}
if ($installer -notmatch [regex]::Escape('#define MyAppVersion "' + $expectedVersion + '"')) {
    Add-Failure 'Installer version does not match the release version.'
}

if ($failures.Count -gt 0) {
    Write-Error ("Static audit failed:`r`n- " + ($failures -join "`r`n- "))
    exit 1
}

Write-Host 'Static audit passed.'

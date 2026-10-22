param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location -LiteralPath $root
$output=[IO.Path]::GetFullPath($OutputDirectory)
$reports=Join-Path $output 'reports'
New-Item -ItemType Directory -Path $reports -Force | Out-Null
# Never inherit a maintainer's opt-in foreground/live-input flags.
Get-ChildItem Env: | Where-Object {$_.Name -like 'LOCALREMOTE_*CHECK*'} | ForEach-Object {Remove-Item ('Env:'+ $_.Name)}
$env:LOCALREMOTE_CI_CHECKS='1'
$passed=[Collections.Generic.List[string]]::new()
function Run-Check([string]$name,[string[]]$arguments) {
    & dotnet @arguments 2>&1 | Tee-Object -FilePath (Join-Path $reports "$name.txt")
    if($LASTEXITCODE -ne 0) {throw "$name failed: $LASTEXITCODE"}
    $passed.Add($name)
}
dotnet --info | Set-Content -LiteralPath (Join-Path $reports 'dotnet.txt')
& ./tests/WindowsPackageChecks/check.ps1 -WorkDirectory (Join-Path $env:RUNNER_TEMP ('lrd-fixture-' + [Guid]::NewGuid().ToString('N').Substring(0,8))) 2>&1 | Tee-Object -FilePath (Join-Path $reports 'WindowsPackageChecks.txt')
$passed.Add('WindowsPackageChecks')
foreach($name in @('ProtocolChecks','FileChecks','ClipboardChecks','LocalizationChecks','DeviceNameChecks','DiscoveryChecks','WindowsHostChecks')) {
    Run-Check $name @('run','--project',"tests/$name/$name.csproj",'-c','Release')
}
& ./scripts/package-windows.ps1 -OutputDirectory $output
[xml]$project=Get-Content -LiteralPath apps/windows/LocalRemoteDesktop.csproj
$version=[string]$project.Project.PropertyGroup.Version
$package=Join-Path $env:RUNNER_TEMP ('lrd zip check ' + [Guid]::NewGuid().ToString('N').Substring(0,8))
& ./scripts/ci/check-windows-archive.ps1 -Archive (Join-Path $output "LocalRemoteDesktop-v$version-windows-x64.zip") -Version $version -Commit ((& git rev-parse HEAD).Trim()) -ExtractDirectory $package -ReportsDirectory $reports 2>&1 | Tee-Object -FilePath (Join-Path $reports 'PackageStartupCheck.txt')
$passed.Add('PackageStartupCheck')
$env:LOCALREMOTE_SHELL_CHECK='1'
Run-Check 'ClientChecks' @('run','--project','tests/ClientChecks/ClientChecks.csproj','-c','Release',"-p:ClientAssemblyPath=$package/LocalRemoteDesktop.dll")
@{
    version=$version; commit=(& git rev-parse HEAD).Trim(); platform='windows-x64'; kind='development-build'
    sdk=(& dotnet --version).Trim(); passed=@($passed.ToArray())
    notRun=@('Physical desktop capture/input','MF hardware encoding performance','Real clipboard/LAN acceptance')
    ffmpeg='7.1.1 (CI-only dependency; not bundled)'
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'ci.json') -Encoding utf8
if($env:GITHUB_STEP_SUMMARY) {
    "Windows x64: native build and $($passed.Count) background suites passed. Package source: $((& git rev-parse HEAD).Trim()). Hardware/physical acceptance not run; FFmpeg is not bundled." | Out-File -LiteralPath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}

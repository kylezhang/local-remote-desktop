param([Parameter(Mandatory)][string]$WorkDirectory)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$directory = [IO.Path]::GetFullPath($WorkDirectory)
if (Test-Path -LiteralPath $directory) { throw 'Fixture directory must be new.' }
New-Item -ItemType Directory -Path $directory | Out-Null
$version = '0.3.0-alpha.6'
$commit = 'a' * 40
$verifier = Join-Path $root 'scripts/check-windows-package.ps1'
foreach ($name in @('LocalRemoteDesktop.exe','LocalRemoteDesktop.dll','LocalRemoteDesktop.deps.json','LocalRemoteDesktop.runtimeconfig.json','hostfxr.dll','hostpolicy.dll','coreclr.dll','System.Private.CoreLib.dll','PresentationCore.dll','PresentationFramework.dll','WindowsBase.dll','LICENSE','NOTICE.md','SOURCE.txt','source.zip','START-HERE.txt','Verify-Package.ps1','CHECK-INSTALLATION.cmd')) {
    [IO.File]::WriteAllText((Join-Path $directory $name), "fixture $name")
}
New-Item -ItemType Directory -Path (Join-Path $directory 'zh-Hans') | Out-Null
[IO.File]::WriteAllText((Join-Path $directory 'zh-Hans/PresentationCore.resources.dll'), 'fixture satellite')
$files = @(Get-ChildItem -LiteralPath $directory -File -Recurse | ForEach-Object {
    @{path = $_.FullName.Substring($directory.Length + 1).Replace('\','/'); bytes = $_.Length; sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
})
function Write-Inventory($inventory) {
    @{schema = 1; version = $version; commit = $commit; files = $inventory} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $directory 'package-files.json') -Encoding utf8
}
function Check { & $verifier -Directory $directory -ExpectedVersion $version -ExpectedCommit $commit }
function Reject([string]$message, [scriptblock]$action) {
    $rejected = $false
    try { & $action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw "Did not reject: $message" }
    Write-Output "PASS rejection: $message"
}
Write-Inventory $files
Check
# This reproduces the partial extraction that left the apphost but no runtime.
$runtime = Join-Path $directory 'System.Private.CoreLib.dll'
$original = [IO.File]::ReadAllBytes($runtime)
Remove-Item -LiteralPath $runtime
Reject 'missing runtime after partial extraction' { Check }
[IO.File]::WriteAllBytes($runtime, $original)
$changed = $original.Clone(); $changed[0] = $changed[0] -bxor 1
[IO.File]::WriteAllBytes($runtime, $changed)
Reject 'same-length corrupted runtime' { Check }
[IO.File]::WriteAllBytes($runtime, $original)
Reject 'wrong expected source' { & $verifier -Directory $directory -ExpectedCommit ('b' * 40) }
Write-Inventory @($files + @{path = 'SYSTEM.PRIVATE.CORELIB.DLL'; bytes = 1; sha256 = 'a' * 64})
Reject 'case-insensitive duplicate' { Check }
Write-Inventory @($files + @{path = '../outside.dll'; bytes = 1; sha256 = 'a' * 64})
Reject 'path traversal' { Check }
Write-Inventory @($files + @{path = 'C:/outside.dll'; bytes = 1; sha256 = 'a' * 64})
Reject 'absolute inventory path' { Check }
Write-Inventory @($files | Where-Object {$_.path -ne 'coreclr.dll'})
Reject 'runtime omitted from inventory' { Check }
Write-Inventory $files
# Optional side-by-side FFmpeg files must not invalidate an intact app.
[IO.File]::WriteAllText((Join-Path $directory 'ffmpeg.exe'), 'separately installed fixture')
Check
$longRoot = $directory
while ($longRoot.Length -lt 248) { $longRoot = Join-Path $longRoot 'long-folder' }
Reject 'deep extraction folder before touching files' { & $verifier -Directory $longRoot }
$compatScript = Join-Path $directory 'verifier-compat.ps1'
[IO.File]::WriteAllText($compatScript, [IO.File]::ReadAllText($verifier), [Text.UTF8Encoding]::new($true))
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $compatScript -Directory $directory -ExpectedVersion $version -ExpectedCommit $commit
if ($LASTEXITCODE -ne 0) { throw 'Bundled verifier failed in Windows PowerShell 5.1.' }
Write-Output 'PASS bundled verifier in Windows PowerShell 5.1 with bilingual diagnostics.'
Write-Output 'PASS Windows package fixtures: complete files, partial extraction, corruption, provenance, paths and optional dependencies. No app was started.'

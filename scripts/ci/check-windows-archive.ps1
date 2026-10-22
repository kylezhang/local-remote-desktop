param(
    [Parameter(Mandatory)][string]$Archive,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Commit,
    [Parameter(Mandatory)][string]$ExtractDirectory,
    [Parameter(Mandatory)][string]$ReportsDirectory
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination = [IO.Path]::GetFullPath($ExtractDirectory)
if (Test-Path -LiteralPath $destination) { throw 'Use a new, isolated extraction directory.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($Archive))
try {
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $zip.Entries) {
        $name = $entry.FullName
        if (-not $entry.Name) { continue }
        if ($name -match '[\\:]|^/|[<>"|?*]' -or $name.Split('/') -contains '..' -or $name.Split('/') -contains '.' -or $name.Split('/') -contains '' -or -not $names.Add($name)) { throw 'Unsafe or duplicate ZIP entry.' }
        if ((Join-Path $destination $name).Length -ge 260) { throw 'Extraction directory is too long.' }
    }
    if (-not $names.Contains('LocalRemoteDesktop.exe') -or -not $names.Contains('package-files.json')) {
        throw 'ZIP must contain the app and inventory at its root, without another version-named folder.'
    }
    [IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($Archive), $destination)
} finally { $zip.Dispose() }
& (Join-Path $root 'scripts/check-windows-package.ps1') -Directory $destination -ExpectedVersion $Version -ExpectedCommit $Commit
$manifest = Get-Content -LiteralPath (Join-Path $destination 'package-files.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($names.Count -ne @($manifest.files).Count + 1) { throw 'ZIP and package inventory disagree.' }
foreach ($item in $manifest.files) {
    if (-not $names.Contains([string]$item.path)) { throw 'ZIP and package inventory disagree.' }
}
New-Item -ItemType Directory -Path $ReportsDirectory -Force | Out-Null
$report = Join-Path ([IO.Path]::GetFullPath($ReportsDirectory)) 'package-startup.json'
if (Test-Path -LiteralPath $report) { throw 'Refusing a stale startup report.' }
$start = [Diagnostics.ProcessStartInfo]::new((Join-Path $destination 'LocalRemoteDesktop.exe'))
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WorkingDirectory = [IO.Path]::GetFullPath($ReportsDirectory)
$start.ArgumentList.Add('--check-startup')
$start.ArgumentList.Add($report)
# The packaged self-contained app must start without an SDK/shared-runtime root.
$start.Environment.Remove('DOTNET_ROOT') | Out-Null
$start.Environment.Remove('DOTNET_ROOT_X64') | Out-Null
$start.Environment['DOTNET_MULTILEVEL_LOOKUP'] = '0'
$process = [Diagnostics.Process]::Start($start)
try {
    if (-not $process.WaitForExit(30000)) { $process.Kill($true); throw 'Packaged startup check timed out.' }
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report -PathType Leaf)) { throw "Packaged apphost failed: $($process.ExitCode)." }
} finally { $process.Dispose() }
$result = Get-Content -LiteralPath $report -Raw -Encoding UTF8 | ConvertFrom-Json
if ($result.passed -ne $true -or $result.check -ne 'packaged-apphost-unshown-startup') { throw 'Packaged startup report failed.' }
Write-Output 'PASS final flat ZIP: complete inventory, isolated extraction and actual self-contained apphost startup; no window displayed.'

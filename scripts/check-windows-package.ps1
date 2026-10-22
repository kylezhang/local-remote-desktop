param(
    [string]$Directory = $PSScriptRoot,
    [string]$ExpectedVersion = '',
    [string]$ExpectedCommit = ''
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($Directory)
if ($root.Length -ge 248) {
    throw 'The folder path is too long. Move the complete folder to a shorter path, such as C:\Apps\LocalRemoteDesktop. / 请将完整程序目录移到较短路径。'
}
$manifestPath = Join-Path $root 'package-files.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'package-files.json is missing. Extract the complete ZIP first. / 请先完整解压 ZIP。'
}
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($manifest.schema -ne 1 -or $manifest.version -notmatch '^\d+\.\d+\.\d+(?:-(?:alpha|beta|rc)\.[1-9][0-9]*)?$' -or $manifest.commit -cnotmatch '^[a-f0-9]{40}$') {
    throw 'Invalid package inventory identity.'
}
if (($ExpectedVersion -and $ExpectedVersion -cne $manifest.version) -or ($ExpectedCommit -and $ExpectedCommit -cne $manifest.commit)) {
    throw 'Package version/source does not match the requested build.'
}
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$problems = [Collections.Generic.List[string]]::new()
foreach ($entry in $manifest.files) {
    $relative = [string]$entry.path
    if (-not $relative -or $relative -match '[\\:]|^/|[<>"|?*]' -or $relative.Split('/') -contains '..' -or $relative.Split('/') -contains '.' -or $relative.Split('/') -contains '' -or $relative -ieq 'package-files.json' -or -not $seen.Add($relative)) {
        throw 'Unsafe or duplicate package inventory path.'
    }
    if ($entry.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $entry.bytes -isnot [ValueType] -or $entry.bytes -lt 0 -or [decimal]$entry.bytes -ne [decimal]::Truncate([decimal]$entry.bytes)) {
        throw "Invalid package inventory record: $relative"
    }
    $path = Join-Path $root $relative
    if ($path.Length -ge 260) {
        $problems.Add("Path too long / 路径过长: $relative"); continue
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $problems.Add("Missing / 缺少文件: $relative"); continue
    }
    $item = Get-Item -LiteralPath $path
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Unexpected linked file: $relative" }
    if ($item.Length -ne $entry.bytes -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $entry.sha256) {
        $problems.Add("Changed or incomplete / 文件损坏或不完整: $relative")
    }
}
foreach ($name in @('LocalRemoteDesktop.exe', 'LocalRemoteDesktop.dll', 'LocalRemoteDesktop.deps.json', 'LocalRemoteDesktop.runtimeconfig.json', 'hostfxr.dll', 'hostpolicy.dll', 'coreclr.dll', 'System.Private.CoreLib.dll', 'PresentationCore.dll', 'PresentationFramework.dll', 'WindowsBase.dll', 'LICENSE', 'NOTICE.md', 'SOURCE.txt', 'source.zip', 'START-HERE.txt', 'Verify-Package.ps1', 'CHECK-INSTALLATION.cmd')) {
    if (-not $seen.Contains($name)) { throw "Required file absent from package inventory: $name" }
}
if ($problems.Count) {
    $details = ($problems | Select-Object -First 8) -join [Environment]::NewLine
    throw "Package is incomplete ($($problems.Count) files). Extract the original ZIP to a short, new folder; keep all files together. / 解压不完整，请将原始 ZIP 完整解压到新的较短目录。`n$details"
}
Write-Output "PASS complete package: $($manifest.version), source $($manifest.commit), $($seen.Count) files. / 程序文件完整。"
Write-Output 'Run LocalRemoteDesktop.exe from this folder. / 请运行本目录的 LocalRemoteDesktop.exe。'
# This inventory detects incomplete extraction; release SHA256SUMS.txt provides
# the independent archive digest. Neither is a publisher code signature.

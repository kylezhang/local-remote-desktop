param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory = 'dist',
    [string]$NuGetConfig = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'apps/windows/LocalRemote.csproj')
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid project version' }
$releaseRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) { [IO.Path]::GetFullPath($OutputDirectory) } else { [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory)) }
$packageName = "LocalRemote-v$version-windows-x64"
$package = Join-Path $releaseRoot $packageName
$archive = Join-Path $releaseRoot "$packageName.zip"
if ((Test-Path -LiteralPath $archive) -or (Test-Path -LiteralPath $package)) { throw 'Release output already exists; choose an empty output directory. Never overwrite published artifacts.' }
$null = New-Item -ItemType Directory -Path $releaseRoot -Force
$arguments = @('publish', (Join-Path $projectRoot 'apps/windows/LocalRemote.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', $package, "-p:OutputPath=$releaseRoot/.build-output/", '-p:DebugType=None', '-p:DebugSymbols=false')
if ($NuGetConfig) { $arguments += @('--configfile', [IO.Path]::GetFullPath($NuGetConfig)) }
& $Dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Publish failed: $LASTEXITCODE" }

$dependencies = Get-Content -LiteralPath (Join-Path $package 'LocalRemote.deps.json') -Raw | ConvertFrom-Json
$coreEntry = $dependencies.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'runtimepack.Microsoft.NETCore.App.Runtime.win-x64/*' }
$desktopEntry = $dependencies.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64/*' }
if (-not $coreEntry -or -not $desktopEntry) { throw 'Self-contained runtime metadata missing' }
$coreVersion = $coreEntry.Split('/')[-1]
$desktopVersion = $desktopEntry.Split('/')[-1]
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages' }
$corePack = Join-Path $nugetRoot "microsoft.netcore.app.runtime.win-x64/$coreVersion"
$desktopPack = Join-Path $nugetRoot "microsoft.windowsdesktop.app.runtime.win-x64/$desktopVersion"
$dotnetExecutable = (Get-Command $Dotnet -ErrorAction Stop).Source
$sdkVersion = (& $Dotnet --version).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify build SDK' }
$desktopSdk = Join-Path (Split-Path -Parent $dotnetExecutable) "sdk/$sdkVersion/Sdks/Microsoft.NET.Sdk.WindowsDesktop"
$notices = Join-Path $package 'licenses'
$null = New-Item -ItemType Directory -Path $notices
$licenseFiles = @{
    'dotnet-LICENSE.txt' = (Join-Path $corePack 'LICENSE.TXT')
    'dotnet-THIRD-PARTY-NOTICES.txt' = (Join-Path $corePack 'THIRD-PARTY-NOTICES.TXT')
    'windowsdesktop-LICENSE.txt' = (Join-Path $desktopPack 'LICENSE')
    'windowsdesktop-SDK-THIRD-PARTY-NOTICES.txt' = (Join-Path $desktopSdk 'THIRD-PARTY-NOTICES.TXT')
}
foreach ($entry in $licenseFiles.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $entry.Value)) { throw "Required runtime notice missing: $($entry.Value)" }
    Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $notices $entry.Key)
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'NOTICE.md') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'apps/windows/BUILD.md') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'apps/windows/BUILD.zh-CN.md') -Destination $package
@"
LocalRemote $version — Windows x64

Run LocalRemote.exe. Keep this folder together; .NET Desktop Runtime is included.
FFmpeg is NOT included: install a complete H.264-capable build and expose ffmpeg.exe on PATH,
or place it and all its required DLLs beside the app. https://ffmpeg.org/download.html
Mac must be logged in, running the Host and authorized for screen capture/Accessibility.
Enter Mac LAN IPv4 and its 64-character pairing key. Click the picture to type.
F11: full screen. Ctrl+Alt+Esc: release input. Quality changes briefly reconnect.
Windows Shortcuts defaults on: Ctrl is Mac Command, Win is Control. Disable for native mapping.
Copy/paste uses the Mac clipboard; cross-device clipboard synchronization is not included.

运行 LocalRemote.exe，保持完整目录；无需另装 .NET。
FFmpeg 需单独完整安装并配置 PATH；现有正常安装可继续用。
Mac 正常登录并开启共享、完成权限；填写 IP 和配对密钥，点击画面后输入。

Repository: https://atomgit.com/zkxw2008/LocalRemote
Runtime: .NET $coreVersion / Desktop $desktopVersion; SDK $sdkVersion.
Final Mac branding update was not natively built in this Windows release environment.
"@ | Set-Content -LiteralPath (Join-Path $package 'START-HERE.txt') -Encoding utf8
Compress-Archive -LiteralPath $package -DestinationPath $archive -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path -Leaf $archive)" | Set-Content -LiteralPath (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii
Write-Output "Packaged $archive"
Write-Output "SHA256 $hash"

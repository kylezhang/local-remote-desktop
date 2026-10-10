param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory = 'dist',
    [string]$NuGetConfig = ''
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'apps/windows/LocalRemoteDesktop.csproj')
$version = [string]$project.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+(?:-(?:alpha|beta|rc)\.[1-9][0-9]*)?$') { throw 'Invalid project version' }
$sourceStatus = & git -C $projectRoot status --porcelain --untracked-files=normal
if ($LASTEXITCODE -ne 0 -or $sourceStatus) { throw 'Commit the final source and documentation before packaging.' }
$commit = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw 'Cannot identify the source commit.' }
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'LICENSE'))) { throw 'Project license is missing.' }
$releaseRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) { [IO.Path]::GetFullPath($OutputDirectory) } else { [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory)) }
$packageName = "LocalRemoteDesktop-v$version-windows-x64"
$package = Join-Path $releaseRoot $packageName
$archive = Join-Path $releaseRoot "$packageName.zip"
if ((Test-Path -LiteralPath $archive) -or (Test-Path -LiteralPath $package)) { throw 'Release output already exists; choose an empty output directory. Never overwrite published artifacts.' }
$null = New-Item -ItemType Directory -Path $releaseRoot -Force
$arguments = @('publish', (Join-Path $projectRoot 'apps/windows/LocalRemoteDesktop.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', $package, "-p:OutputPath=$releaseRoot/.build-output/", '-p:DebugType=None', '-p:DebugSymbols=false')
if ($NuGetConfig) { $arguments += @('--configfile', [IO.Path]::GetFullPath($NuGetConfig)) }
& $Dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Publish failed: $LASTEXITCODE" }

$dependencies = Get-Content -LiteralPath (Join-Path $package 'LocalRemoteDesktop.deps.json') -Raw | ConvertFrom-Json
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
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'apps/windows/BUILD.md') -Destination $package
Copy-Item -LiteralPath (Join-Path $projectRoot 'apps/windows/BUILD.zh-CN.md') -Destination $package
$sourceArchive = Join-Path $package 'source.zip'
& git -C $projectRoot archive --format=zip "--prefix=LocalRemoteDesktop-source-$commit/" "--output=$sourceArchive" $commit
if ($LASTEXITCODE -ne 0) { throw 'Corresponding source archive failed.' }
@"
Local Remote Desktop — corresponding source
License: GNU GPL version 3 only (GPL-3.0-only); see LICENSE.
Source commit: $commit
Source archive: source.zip (inside this package, not dependent on repository access)
Extract the source archive and follow README.md and apps/windows/BUILD.md.
SDK used: .NET $sdkVersion; runtime: .NET $coreVersion / Desktop $desktopVersion.
Third-party components retain their own licenses; see NOTICE.md and licenses/.

相应源码已附在 source.zip，提交为 $commit；解压后按 README.zh-CN.md
和 apps/windows/BUILD.zh-CN.md 构建。项目许可见 LICENSE，第三方许可见 NOTICE.md 和 licenses/。
"@ | Set-Content -LiteralPath (Join-Path $package 'SOURCE.txt') -Encoding utf8
@"
Local Remote Desktop $version — Windows x64

Run LocalRemoteDesktop.exe. Keep this folder together; .NET Desktop Runtime is included.
FFmpeg is NOT included: install a complete H.264-capable build and expose ffmpeg.exe on PATH,
or place it and all its required DLLs beside the app. https://ffmpeg.org/download.html
Mac must be logged in, running the Host and authorized for screen capture/Accessibility.
Enter Mac LAN IPv4 and its pairing password (legacy 64-character keys also work).
Mac: Change Password opens an editor; generated passwords take effect only after Save.
Update both apps for short-password authentication. Click the picture to type.
F11: full screen. Ctrl+Alt+Esc: release input. Quality changes briefly reconnect.
Windows Shortcuts defaults on: Ctrl is Mac Command, Win is Control. Disable for native mapping.
Text/regular-file clipboard sync defaults on for paired sessions; explicit off is remembered.
Wait for enabled status, then copy again. Files use TCP 47981, with progress/cancel.
Folders and image clipboard formats are not supported. See the repository user guide.

运行 LocalRemoteDesktop.exe，保持完整目录；无需另装 .NET。
FFmpeg 需单独完整安装并配置 PATH；现有正常安装可继续用。
Mac 正常登录并开启共享、完成权限；填写 IP 和配对密码，点击画面后输入。
修改密码在独立窗口操作，保存才生效；短密码需更新两端。
文字/普通文件默认同步；在设置中可关闭，等启用后重新复制。文件需 TCP 47981。

Repository: https://atomgit.com/zkxw2008/local-remote-desktop
License: GPL-3.0-only; see LICENSE. Corresponding source: source.zip and SOURCE.txt.
Source commit: $commit
Runtime: .NET $coreVersion / Desktop $desktopVersion; SDK $sdkVersion.
Beta scope: Windows build/automated tests are verified; Mac source is provided.
Current Apple SDK build, native UI/Keychain/TCC and real two-device acceptance are pending.
No Mac binary is included. No FileVault unlock or guaranteed lock-screen control.
"@ | Set-Content -LiteralPath (Join-Path $package 'START-HERE.txt') -Encoding utf8
$finalCommit = & git -C $projectRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $finalCommit -ne $commit) { throw 'Source commit changed during packaging.' }
$finalStatus = & git -C $projectRoot status --porcelain --untracked-files=normal
if ($LASTEXITCODE -ne 0 -or $finalStatus) { throw 'Source changed during packaging; refusing to create the release archive.' }
Compress-Archive -LiteralPath $package -DestinationPath $archive -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path -Leaf $archive)" | Set-Content -LiteralPath (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding ascii
Write-Output "Packaged $archive"
Write-Output "SHA256 $hash"

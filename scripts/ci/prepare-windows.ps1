param([Parameter(Mandatory)][string]$Directory)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Directory) {throw 'Dependency output already exists'}
New-Item -ItemType Directory -Path $Directory | Out-Null
$archive=Join-Path $Directory 'ffmpeg.zip'
# CI only. This dependency is not copied into application artifacts.
$url='https://github.com/GyanD/codexffmpeg/releases/download/7.1.1/ffmpeg-7.1.1-essentials_build.zip'
$expected='04861d3339c5ebe38b56c19a15cf2c0cc97f5de4fa8910e4d47e5e6404e4a2d4'
Invoke-WebRequest -Uri $url -OutFile $archive
if((Get-FileHash -LiteralPath $archive).Hash.ToLowerInvariant() -ne $expected) {throw 'FFmpeg test dependency checksum mismatch'}
Expand-Archive -LiteralPath $archive -DestinationPath $Directory
$executables=@(Get-ChildItem -LiteralPath $Directory -Filter ffmpeg.exe -Recurse -File)
if($executables.Count -ne 1) {throw 'Expected one FFmpeg test executable'}
$bin=$executables[0].DirectoryName
$env:PATH=$bin+[IO.Path]::PathSeparator+$env:PATH
if($env:GITHUB_PATH) {$bin | Out-File -LiteralPath $env:GITHUB_PATH -Append -Encoding utf8}
& $executables[0].FullName -version
if($LASTEXITCODE -ne 0) {throw 'FFmpeg test dependency failed'}

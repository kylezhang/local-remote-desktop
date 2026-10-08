# Windows build and usage

[简体中文](BUILD.zh-CN.md)

Windows 10/11 x64, .NET 8 SDK for development, and FFmpeg with an H.264 decoder. No third-party NuGet packages. Run from the repository root:

```powershell
dotnet build windows-client/RemoteMacLAN.csproj -c Release
dotnet run --project windows-client/RemoteMacLAN.csproj -c Release
```

For a release package that includes the .NET Desktop Runtime:

```powershell
dotnet publish windows-client/RemoteMacLAN.csproj -c Release -r win-x64 --self-contained true -o dist/windows
```

Runtime packs must be available during restore. See [Microsoft deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/). Keep the complete output directory together. `RemoteMacLAN.exe` is the compatibility executable name; the product/window/icon are LocalRemote 0.1.0. A normal build, unlike the release ZIP, needs .NET Desktop Runtime installed.

FFmpeg is not bundled. Use the [official download entry point](https://ffmpeg.org/download.html), install its complete required files and expose ffmpeg.exe on PATH or beside this executable. Respect the chosen FFmpeg build's license. Media Foundation/hardware decode and D3D11 rendering are not implemented.

First start defaults the address to the Windows machine's LAN IPv4 to make editing the subnet easier; this does not discover the Mac. Saved address/key take precedence. Connect saves valid-format credentials using current-user DPAPI under `%LOCALAPPDATA%/RemoteMacLAN`; wrong keys still fail authentication. Forget Connection clears the selected entry.

Click the picture for keyboard focus. F11 toggles full screen; Ctrl+Alt+Esc releases input. UI/diagnostic-button focus means KeyboardFocus=false is expected. InputPosted reports CGEvent submission, not target-app acceptance. Windows IME is disabled on the picture so physical key events reach the Mac; Mac input methods can handle them.

Quality remains selectable during a connection. Choosing a setting causes a brief disconnect and automatic reauthentication after about three seconds; the latest rapid selection wins even when Auto Reconnect is unchecked. No Mac protocol update is needed solely for this behavior.

The existing local deployment path `windows-client/bin/Release/net8.0-windows/win-x64` remains supported. Close that running instance before publishing over its files. Never delete remembered credentials as part of deployment.

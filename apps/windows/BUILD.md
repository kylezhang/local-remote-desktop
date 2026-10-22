# Windows build and usage

[简体中文](BUILD.zh-CN.md)

Windows 10/11 x64, .NET 8 SDK for development, and FFmpeg with an H.264 decoder. No third-party NuGet packages. Run from the repository root:

```powershell
dotnet build apps/windows/LocalRemoteDesktop.csproj -c Release
dotnet run --project apps/windows/LocalRemoteDesktop.csproj -c Release
```

For a release package that includes the .NET Desktop Runtime:

```powershell
dotnet publish apps/windows/LocalRemoteDesktop.csproj -c Release -r win-x64 --self-contained true -o dist/windows
```

Runtime packs must be available during restore. See [Microsoft deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/). Keep the complete output directory together and run `LocalRemoteDesktop.exe`. Published v0.1.0 archives still contain `RemoteMacLAN.exe`; existing release assets are unchanged. A normal build, unlike the release ZIP, needs .NET Desktop Runtime installed.

New source packages place the app directly at the ZIP root. Use **Extract All**
to a short, new folder, then run `LocalRemoteDesktop.exe`; do not run it inside
the ZIP or copy only the EXE. A very deep path can interrupt Windows extraction
and leave the EXE without its runtime. If startup fails, run the included
`CHECK-INSTALLATION.cmd`: it checks the packaged file inventory and gives missing,
damaged or overlong-path details, without starting the app or changing system
settings. Its child PowerShell process allows the bundled local checker for that
invocation only; it does not change the machine execution policy. The inventory
detects incomplete extraction; compare the original ZIP with the release's
`SHA256SUMS.txt` to check the independent archive digest. Neither is a code signature.

For the complete ZIP, license notices, corresponding source and file inventory,
run `./scripts/package-windows.ps1` from a clean committed checkout. CI extracts
that final ZIP into an isolated folder, checks every inventoried file and runs
the extracted EXE with `--check-startup <report.json>`. This opt-in diagnostic
loads the actual apphost, runtime and unshown home resources, then exits; it does
not connect, share, read stored credentials/clipboards or show a window. Normal
launch continues to open the app. The background client suites then reference the
assembly from the extracted ZIP, rather than the pre-archive publish directory.

FFmpeg is not bundled. Use the [official download entry point](https://ffmpeg.org/download.html), install its complete required files and expose ffmpeg.exe on PATH or beside this executable. Respect the chosen FFmpeg build's license. Media Foundation/hardware decode and D3D11 rendering are not implemented.

First start defaults the address to the Windows machine's LAN IPv4 to make editing the subnet easier; this does not discover the Mac. Saved address/key take precedence. Connect saves valid-format credentials using current-user DPAPI under `%LOCALAPPDATA%/LocalRemote`; wrong keys still fail authentication. Forget Connection clears the selected entry.

The first renamed launch copies recognized encrypted records from the old
`%LOCALAPPDATA%/RemoteMacLAN` directory once, retaining the originals and existing
new settings. A completion marker prevents forgotten records from returning.

Click the picture for keyboard focus. F11 toggles full screen; Ctrl+Alt+Esc releases input. UI/diagnostic-button focus means KeyboardFocus=false is expected. InputPosted reports CGEvent submission, not target-app acceptance. Windows IME is disabled on the picture so physical key events reach the Mac; Mac input methods can handle them.

Development builds default to Windows Shortcuts: Ctrl→Mac Command and Win→Control, with both key codes and modifier flags converted. Disable the checkbox for the original physical mapping. Its preference is stored locally with DPAPI. The physical Ctrl+Alt+Esc release shortcut works in both modes. This change needs only a Windows update and is compatible with the existing Mac input protocol; it does not add cross-device clipboard synchronization by itself. The beta adds separate remembered text/file sharing settings; see the user guide.

Quality remains selectable during a connection. Choosing a setting causes a brief disconnect and automatic reauthentication after about three seconds; the latest rapid selection wins even when Auto Reconnect is unchecked. No Mac protocol update is needed solely for this behavior.

The current source build path is `apps/windows/bin/Release/net8.0-windows/win-x64`.
Close the running instance before deploying over its files. An existing local
`windows-client/bin/.../RemoteMacLAN.exe` shortcut can be retained as a compatibility
deployment target; it is not the source project or the release executable.

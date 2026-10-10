# Windows host backend checks

Run on Windows with a complete FFmpeg build supporting `h264_mf` on PATH:

```powershell
dotnet run --project tests/WindowsHostChecks/WindowsHostChecks.csproj -c Release
```

The default suite uses generated video, loopback sockets and a recording input
adapter. DPAPI credentials use a unique temporary path and are removed. It does
not inject system input, read the user clipboard or record a desktop file.

To additionally exercise brief actual primary-desktop capture into memory:

```powershell
$env:LOCALREMOTE_REAL_CAPTURE_CHECK = '1'
dotnet run --project tests/WindowsHostChecks/WindowsHostChecks.csproj -c Release
```

The actual sharing/viewer UI is checked by `tests/ClientChecks`. Its optional
`LOCALREMOTE_NATIVE_INPUT_CHECK=1` check creates its own temporary WPF field,
requires foreground ownership, and restores focus/cursor only while still owned.
It may skip when the OS denies focus or local modifiers/buttons are pressed.
Do not treat a skip or recording-adapter pass as real SendInput event acceptance.
See [scope and status](../../docs/windows-sharing.md).

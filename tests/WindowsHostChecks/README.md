# Windows host backend checks

Run on Windows with a complete FFmpeg build supporting `h264_mf` on PATH:

```powershell
dotnet run --project tests/WindowsHostChecks/WindowsHostChecks.csproj -c Release
```

The default suite uses generated video, loopback sockets and a recording input
adapter. DPAPI credentials use a unique temporary path and are removed. It does
not inject system input, read the user clipboard or record a desktop file.

GitHub Actions uses `LOCALREMOTE_CI_CHECKS=1`: the generated-video transport tests
use software encoding, so they do not require a GPU. Hardware/physical acceptance
is explicitly Not run. An absent hosted display skips only native display
enumeration; synthetic pointer layouts still run. The normal local default keeps
the MF hardware check. See [online builds](../../docs/continuous-integration.md).

Pointer regression covers single/left/above/mixed-scale portrait layouts and
move/click/wheel landing points. On Windows, display modes are read under three
DPI contexts; the production native boundary uses a recording submission to
verify per-monitor awareness and restoration, including failure. No `SendInput`
call occurs in these tests. Physical click acceptance is a separate scheduled gate.

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

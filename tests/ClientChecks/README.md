# Background Windows client checks

[简体中文](README.zh-CN.md)

Requires Windows, the .NET 8 SDK/Desktop runtime and a complete H.264-capable
FFmpeg build available as `ffmpeg.exe` on PATH. From the repository root:

```powershell
ffmpeg -version
dotnet run --project tests/ClientChecks -c Release
```

Default checks never show or activate windows. They use unshown WPF layouts,
isolated preferences/storage, recording input/clipboards, encrypted loopback
peers and generated video; they do not capture the user's desktop or inject input.
The picture check uses FFmpeg to generate/decode a small test pattern. File and
TCP fixtures write disposable temporary files and bind loopback ports.

`LOCALREMOTE_INTERACTIVE_CHECKS`, `LOCALREMOTE_NATIVE_INPUT_CHECK` and
`LOCALREMOTE_LAN_INPUT_CHECK` are separate opt-in paths. Do not enable them during
ordinary background runs: native interaction requires an agreed test period and
a dedicated target. Unshown focus/window checks do not establish actual OS focus,
fullscreen monitor geometry or real two-device acceptance.

To test a final package, pass its assembly using the `ClientAssemblyPath` MSBuild
property. Keep its matching source and license files with the package. See
[validation](../../docs/validation.md) for evidence and remaining native gates.

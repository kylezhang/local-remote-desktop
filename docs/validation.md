# v0.1.0 validation

[简体中文](zh-CN/validation.md)

Release environment: Windows x64, .NET SDK 8.0.425 and Desktop/Core runtime 8.0.31. Date: 2026-10-08. Results are captured in `tests/results/` after the final release build.

## Automated coverage

- Unreleased v0.1.1 work (2026-10-09): InstallerChecks also exercises late move/signature/metadata failure, interrupt/termination rollback, recovery-backup retention, first-install cleanup and launch failure. The Keychain background/silent-read changes are syntax-checked only, not natively compiled or exercised; see the [release checklist](next-release.md).
- ProtocolChecks: mutual authentication, fragmented TCP, wrong-key/tamper/replay/oversize rejection, actual H.264→BGRA decode, synthetic-key DPAPI round-trip, 10,000-move queue coalescing, ordered input/release and letterbox/clamp/NaN coordinates.
- ClientChecks: real WPF window and FFmpeg against an encrypted simulated peer; displayed bitmap, routed letter/top-row/keypad/modifier input, pointer/drag/wheel, focus-release, permission status, disconnect cleanup and connected quality changes including rapid selection with Auto Reconnect off. The window is off-screen and non-activated, and credentials are not loaded/saved.
- InstallerChecks: mocked Mac tools in a disposable directory, complete replacement/duplicate cleanup, staged icon validation, loaded agent behavior, force-stop, and old-application preservation on build/signature/icon/reset failure. Current installer checks cover the default two-service scoped reset, permission-preserving updates and the legacy screen-only option. Signing checks cover missing/pinned/invalid/certificate/ad-hoc identities and temporary probe cleanup. No actual TCC, launchd or certificate operations.
- Swift syntax parsing only; plist XML/icon formats/link consistency and release contents are also inspected. These do not substitute for Apple SDK type checking or native execution.
- ReleaseChecks validates aligned versions, compatibility identity, icon containers, local documentation links and basic tracked-file/secret hygiene. It is a release sanity check, not a comprehensive security scan.

Run on Windows with .NET SDK and FFmpeg:

```powershell
dotnet run --project tests/ProtocolChecks/ProtocolChecks.csproj -c Release
dotnet run --project tests/ClientChecks/ClientChecks.csproj -c Release
python tests/InstallerChecks/check.py --bash "C:/Program Files/Git/bin/bash.exe" --work-dir work/installer-checks
python tests/InstallerChecks/signing.py --bash "C:/Program Files/Git/bin/bash.exe" --work-dir work/signing-checks
python tests/ReleaseChecks/check.py
```

## What these results do not prove

The user reports that the real Windows→Mac workflow now meets most needs. This is user feedback, not a reproducible two-machine lab measurement. The release environment cannot build/run macOS; final branding/icon bundle changes are unverified natively, and there is no Mac binary release asset. TCC, Keychain prompts, real display capture, hardware encoding, secure/lock-screen input, sleep/wake, long sessions and end-to-end 1080p60 remain hardware acceptance items.

An earlier controlled FFmpeg buffering comparison is retained as `decoder-latency-comparison.json`. It is a synthetic decoder-path observation, not LAN input-to-display latency or a performance promise.

For future Mac acceptance: build/install normally, authorize once, test ordinary text/keypad/modifiers/click/drag/scroll, change quality, disconnect and resume locally, test login auto-start after normal FileVault unlock, then test updates with a stable signing identity. Record failures and platform versions without publishing pairing keys or typed content.

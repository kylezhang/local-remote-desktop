# v0.1.0 validation

[简体中文](zh-CN/validation.md)

Release environment: Windows x64, .NET SDK 8.0.425 and Desktop/Core runtime 8.0.31. Date: 2026-10-08. Results are captured in `tests/results/` after the final release build.

## Automated coverage

- Clipboard development (2026-10-09): C# and real Swift 5.9.2 compile/run the
  production state logic for Unicode, exact byte limits/JSON record fit, malformed
  data, consent baselines, no echo, bounded busy retries, local-copy priority,
  expiry and disconnect. Linux also compiles/runs the production Mac adapter with
  mock AppKit/Wire to check timer/consent/order/revocation/stale IDs. The exact
  rebuilt self-contained Windows DLL passes real WPF/encrypted-peer clipboard
  negotiation, both directions, before-paste ordering, invalid data, consent
  changes and old-peer fallback alongside existing control/quality/close tests.
  System clipboards are replaced by test adapters; no user clipboard is accessed.
  Native AppKit/full Mac SDK and real two-device paste acceptance are pending.

- Windows ResourceChecks (2026-10-09): production decoder fed 720p/30 and 1080p/60
  for 40 seconds each; CPU/private-memory samples, bounded diagnostic draining,
  cancellation, dimension rejection and decoder exit checked. Production source passed
  ProtocolChecks; the exact rebuilt DLL passed WPF ClientChecks, including closing a connected window
  and stopping the disconnected display timer. See [resource scope and results](performance.md).

- Naming migration (2026-10-09): LocalRemote Windows project/assembly built and
  packaged self-contained; the exact packaged DLL passed WPF mock-peer regression.
  Protocol tests exercised opaque DPAPI migration, decryptability, newer-setting
  priority, retained originals and no resurrection after forgetting. Mac installer
  mocks exercised all legacy locations, rollback and login-job path updates; signing
  mocks exercised old ignored pin migration. Swift startup checks still execute with
  a real Linux compiler; full Apple SDK/native install acceptance remains pending.
  Historical result files retain the source/artifact names from their original runs.

- MacPackageChecks (2026-10-09): portable bundle/archive checks reject missing icons,
  wrong version/CPU, development mode, extra files, malformed icons, unsafe ZIP entries,
  modified content and wrong provenance. Mocked release cases cover invalid identity,
  dirty/version-mismatched source, missing runtime/timestamp, debug entitlement,
  notarization rejection/error, stapling/extracted-ticket failure, changed source and
  no-overwrite output. The successful fixture recreates the ZIP after stapling and
  verifies checksums. No Apple signing/notary/Gatekeeper operations were executed.
  Signing mocks also check fresh bundles and old build-output recovery on failed move
  or termination; these do not touch the installed app.

- Latest development: MacStartupChecks compiles/executes production SharingPreparation
  on Ubuntu 22.04 with Swift 5.9.2. Seven scenarios cover permission failure without
  credential access, ordering/cache/recheck, standalone key reuse, failed read without
  auto-retry, and three cancellation boundaries. On Mac the script also compiles the
  whole Host with the Apple SDK; that platform gate has not run here.
- Windows shortcut update: WPF Ctrl+C/V/X/A/Z/Y/S/F chords reach the encrypted mock
  Host with Command key codes/flags and ordered releases. Mode changes, native
  Control+Shift and physical local release are checked. Protocol tests cover every
  modifier combination and isolated DPAPI preference persistence/recovery. These
  verify transport, not real Mac applications or cross-device clipboard behavior.

- Unreleased v0.1.1 work (2026-10-09): InstallerChecks also exercises late move/signature/metadata failure, interrupt/termination rollback, recovery-backup retention, first-install cleanup and launch failure. Signature-preserving update checks cover changed ad-hoc identity, duplicate-copy mismatch, unreadable requirements and verification failure without stopping the working Host or resetting permissions. Keychain startup gating and silent-read changes are syntax-checked only, not natively compiled or exercised; see the [release checklist](next-release.md).
- ProtocolChecks: mutual authentication, fragmented TCP, wrong-key/tamper/replay/oversize rejection, actual H.264→BGRA decode, synthetic-key DPAPI round-trip, 10,000-move queue coalescing, ordered input/release and letterbox/clamp/NaN coordinates.
- ClientChecks: real WPF window and FFmpeg against an encrypted simulated peer; displayed bitmap, routed letter/top-row/keypad/modifier input, pointer/drag/wheel, focus-release, permission status, disconnect cleanup and connected quality changes including rapid selection with Auto Reconnect off. The window is off-screen and non-activated, and credentials are not loaded/saved.
- InstallerChecks: mocked Mac tools in a disposable directory, complete replacement/duplicate cleanup, staged icon validation, loaded agent behavior, force-stop, and old-application preservation on build/signature/icon/reset failure. Current installer checks cover the default two-service scoped reset, permission-preserving updates and the legacy screen-only option. Signing checks cover missing/pinned/invalid/certificate/ad-hoc identities and temporary probe cleanup. No actual TCC, launchd or certificate operations.
- Swift syntax parsing only; plist XML/icon formats/link consistency and release contents are also inspected. These do not substitute for Apple SDK type checking or native execution.
- ReleaseChecks validates aligned versions, compatibility identity, icon containers, local documentation links and basic tracked-file/secret hygiene. It is a release sanity check, not a comprehensive security scan.

Run on Windows with .NET SDK and FFmpeg:

```powershell
dotnet run --project tests/ProtocolChecks/ProtocolChecks.csproj -c Release
dotnet run --project tests/ClientChecks/ClientChecks.csproj -c Release
dotnet run --project tests/ClipboardChecks/ClipboardChecks.csproj -c Release
dotnet run --project tests/ResourceChecks/ResourceChecks.csproj -c Release -- 40 resources.json
python tests/InstallerChecks/check.py --bash "C:/Program Files/Git/bin/bash.exe" --work-dir work/installer-checks
python tests/InstallerChecks/signing.py --bash "C:/Program Files/Git/bin/bash.exe" --work-dir work/signing-checks
python tests/ReleaseChecks/check.py
python tests/MacPackageChecks/check.py --bash "C:/Program Files/Git/bin/bash.exe" --work-dir work/mac-package-checks
# macOS or Linux with Swift; Mac also compiles the whole Host:
bash tests/MacStartupChecks/check.sh
bash tests/ClipboardChecks/check.sh
```

## What these results do not prove

The user reports that the real Windows→Mac workflow now meets most needs. This is user feedback, not a reproducible two-machine lab measurement. The release environment cannot build/run macOS; final branding/icon bundle changes are unverified natively, and there is no Mac binary release asset. TCC, Keychain prompts, real display capture, hardware encoding, secure/lock-screen input, sleep/wake, long sessions and end-to-end 1080p60 remain hardware acceptance items.

An earlier controlled FFmpeg buffering comparison is retained as `decoder-latency-comparison.json`. It is a synthetic decoder-path observation, not LAN input-to-display latency or a performance promise.

For future Mac acceptance: build/install normally, authorize once, test ordinary text/keypad/modifiers/click/drag/scroll, change quality, disconnect and resume locally, test login auto-start after normal FileVault unlock, then test updates with a stable signing identity. Record failures and platform versions without publishing pairing keys or typed content.

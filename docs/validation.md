# Validation

## Published alpha and automated synchronization — 2026-10-14

v0.3.0-alpha.3 now provides Windows and Mac downloads on both repositories.
Native builds/package checks and the hardened automatic synchronization passed;
the final job verified six existing attachments and selected AtomGit homepage
latest without replacing files. See the [release evidence](../tests/results/automated-release-2026-10-14.md)
for exact commits, runs and hashes. Interactive acceptance was deferred by the
maintainer; screen-picker/TCC/Keychain updates, real clipboard and sustained
hardware performance remain pending. The alpha designation is retained.

## GitHub Actions native builds — 2026-10-13

The [first online build](../tests/results/github-actions-2026-10-13.md) passed
source/package fixtures, native Windows build/background checks, and native Mac
Swift fixtures/full Apple SDK compilation/unshown layout/preview packaging.
Downloaded package and source hashes were independently verified. CI is now the
primary build environment; local compilation is optional. Mac preview is ad-hoc,
not notarized. Real desktop/input, TCC/Keychain, clipboard and sustained hardware
performance remain separate acceptance items.

## Session picture and focus — 2026-10-11

Windows Release/unshown fixtures pass with an independent encrypted peer and
generated H.264: decoded pixels, fresh picture/decoder after quality changes,
latest rapid selection, disconnected retry cancellation and prompt cancellation
of stalled authentication. Unshown fullscreen state restoration and routed
focus-loss release pass. Portable Mac model checks retain the latest quality;
updated Swift UI syntax parses. See the [record](../tests/results/session-interaction-2026-10-11.txt)
and [background check setup](../tests/ClientChecks/README.md).
Real foreground focus, held physical input, fullscreen animation/monitor geometry
and native Mac AppKit/Apple SDK remain pending; these are not live latency measurements.

## Disconnect and file cleanup — 2026-10-11

Windows Release/unshown encrypted checks pass, including 12 alternating normal
disconnect/transport-loss sessions, reusable file ports, renewed file identities,
stopped controller timers and stale transfer-notice rejection. Portable Swift
production file adapter/worker checks cover clipboard replacement, stale offers,
partial cleanup and cancellation. See the [coverage record](../tests/results/session-cleanup-2026-10-11.txt).
These are correctness fixtures, not long-duration CPU/memory or live latency
measurements. Updated Apple SDK/native pasteboard and two-device acceptance remain pending.

## Sharing edit lifecycle — 2026-10-11

Unshown Windows sharing/editor lifecycle and encrypted regressions pass.
Portable production Mac HostModel checks cover failed saves, explicit restart
and stop during persistence. See the [coverage record](../tests/results/sharing-edit-lifecycle-2026-10-11.txt).
Native Mac UI/Keychain/TCC and actual two-device acceptance remain pending.

## Compact desktop entry — 2026-10-11

Windows Release build and unshown WPF measured-layout/connection regressions passed;
portable Swift model, localization and syntax checks passed. See the
[coverage record](../tests/results/compact-single-column-2026-10-11.txt).
Updated native Mac UI and real two-device acceptance remain pending.

## Reverse files — 2026-10-11

Both desktop roles now implement paired regular-file sharing. Production Windows
host push/pull and an independent Python ↔ production Swift file worker passed,
including cancel, corruption, consent and paste-readiness checks with recording
clipboards. No real desktop input or clipboard was used. See the
[coverage record](../tests/results/reverse-files-2026-10-11.txt).
Updated Apple SDK, installed Mac clipboard and two-device acceptance remain pending.

## Reverse text clipboard — 2026-10-11

Mac controller and Windows sharing source now reuse the existing text protocol.
Production Windows encrypted/WPF-dispatcher, portable Swift state/model and
independent encrypted-session checks passed with recording clipboards, without
showing windows or using real input. See the [coverage record](../tests/results/reverse-text-clipboard-2026-10-11.txt).
Updated Apple SDK/pasteboard and real two-device acceptance remain pending;
reverse files were subsequently implemented; see the newer record above.

The v0.2.0-beta.1 preview uses the current final-package Windows checks, portable Swift/model checks and 2026-10-10 decoder samples. Mac source is provided without a native SDK/runtime claim. Earlier v0.1.0 records remain as historical baseline.

[简体中文](zh-CN/validation.md)

## Mac checkout relocation — 2026-10-11

`tests/InstallerChecks/cache.py` passes four tests covering seven scenarios with
mock Apple tools in Linux temporary directories: unmarked/old-path cache rebuild,
an actual directory rename (including a space) followed by incremental reuse,
build failure retaining the prior bundle/signing setting, and refusal of cache
directory/marker symlinks or a regular-file cache path. Shell syntax, the 13
signing cases and 21 installer tests also pass. This establishes script control
flow, not an Apple SDK build or native installation. The owner's Mac build is
still the native gate.

## Discovery and returning pairing — 2026-10-11

Windows OS DNS-SD construction, registration and withdrawal passed with temporary
metadata; independent loopback discovery and unshown WPF fixtures passed. Portable
Swift discovery and controller pairing use explicit service doubles and an injected
scan-deadline scheduler. They do not validate native Security/Bonjour/SwiftUI APIs.
See [the current record](../tests/results/discovery-pairing-2026-10-11.txt).
Updated Mac SDK, installed permissions/Keychain and two-device input remain pending;
Mac SSH is stopped because the device is outside this LAN. No release gate is cleared.

## Native development checkpoint — 2026-10-10

An isolated Mac checkout at `b651e760a1baca8b28571614b170e2f788be7b5a`
passed the Apple SDK production build. Production Swift models, native
CryptoKit/CommonCrypto and Darwin-socket fixtures passed; VideoToolbox decoded
nine 320×180 H.264 color frames with pixel checks and dimension-change rejection.
The compiler/OS were Swift 6.4 and macOS 27.0.1 on Apple Silicon. This updates
earlier build-pending statements below; those dated entries retain their original scope.

The live LAN experiment decoded 46 frames but did **not** establish Windows
application input acceptance. Installed Mac GUI, Keychain/TCC update behavior,
bidirectional clipboard/files and complete two-device input acceptance remain
open. No installed Mac app or permissions were changed by the isolated checks.
No new release is implied. See the [current scope and remaining gates](v0.3.0-plan.md).

Foreground input tests are manual opt-in and scheduled, following the user
report that test windows interrupted their work. Background checks do not clear
the native input gate.


Release environment: Windows x64, .NET SDK 8.0.425 and Desktop/Core runtime 8.0.31. Date: 2026-10-08. Results are captured in `tests/results/` after the final release build.

## Automated coverage

- Shared language/discovery development (2026-10-10): exact Windows client checks
  cover live English/Chinese labels, tooltips, minimum-width layout and preserved
  credentials/settings in both pairing modes. Production C#/Swift translators
  pass all 181 catalog entries/27 templates with opaque values; Windows language
  persistence and Mac package resource checks pass. Native Mac UI/installed
  preferences remain pending. See [language record](../tests/results/localization-2026-10-10.txt)
  and [discovery record](../tests/results/discovery-2026-10-10.txt).

- Mac password-flow development (2026-10-09): real Swift compiler executes the
  production HostModel/PairingEditor with explicit service doubles. Covered
  deliberate/cached reveal without sharing-status changes, bounded legacy display
  and exact copy, draft cancel, validation/same-password no-op, failed-save
  draft/session preservation, successful cached replacement/restart, stale callback
  rejection and stop/quit during save. No native SwiftUI/Keychain claim; see
  `tests/MacHostModelChecks/check.sh` and `tests/results/pairing-flow-2026-10-09.txt`.

- Pairing passwords (2026-10-09): independent PBKDF2 vector, wrong password/server
  proof/salt rejection and exact packaged Windows WPF regression in both RML1 and
  RMP1 modes passed, including control, clipboard, file transfer and quality reconnects.
  Real Swift 5.9.2 executed production credential encoding/KDF/legacy/cache logic
  using a test CommonCrypto bridge to OpenSSL on Linux. Native Apple SDK,
  Keychain save/authorization and restart persistence remain pending. See
  `tests/results/password-2026-10-09.txt` and `tests/PairingChecks/check.sh`.

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

- Naming migration (2026-10-09): Local Remote Desktop Windows project/assembly built and
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
- InstallerChecks: mocked Mac tools in a disposable directory, complete replacement/duplicate cleanup, staged icon validation, loaded agent behavior, force-stop, and old-application preservation on build/signature/icon/reset failure. Installer checks cover default permission-preserving updates, explicit two-service scoped repair and the legacy screen-only option. Signing checks cover missing/pinned/invalid/certificate/ad-hoc identities and temporary probe cleanup. No actual TCC, launchd or certificate operations.
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

## Windows sharing development checks — 2026-10-10

Independent authentication, DPAPI, lifecycle and actual WPF checks passed.
Generated video in all three profiles and brief actual Windows primary-desktop
capture passed hardware encoding → encrypted loopback → decoding checks.
This is not sustained FPS or physical-LAN acceptance. Native input acceptance was
skipped because the OS refused foreground focus; Mac control remains pending.
See [the record](../tests/results/windows-sharing-2026-10-10.txt).

## What these results do not prove

Unreleased LAN discovery checks are recorded in
[discovery-2026-10-10.txt](../tests/results/discovery-2026-10-10.txt).
Windows DNS/UDP and WPF selection regressions passed; Mac publication lifecycle
uses explicit NetService doubles. Actual Mac Bonjour and LAN multicast remain pending.

The v0.2.0-beta.1 preview was published on 2026-10-10. Its final Windows
package was rebuilt from the tagged commit, tested in both pairing modes,
uploaded and downloaded again with identical SHA256. See the
[delivery record](../tests/results/release-v0.2.0-beta.1-2026-10-10.txt).
The preview includes Mac source only; native Mac acceptance remains pending.

The user reports that the real Windows→Mac workflow now meets most needs. This is user feedback, not a reproducible two-machine lab measurement. The release environment cannot build/run macOS; final branding/icon bundle changes are unverified natively, and there is no Mac binary release asset. TCC, Keychain prompts, real display capture, hardware encoding, secure/lock-screen input, sleep/wake, long sessions and end-to-end 1080p60 remain hardware acceptance items.

An earlier controlled FFmpeg buffering comparison is retained as `decoder-latency-comparison.json`. It is a synthetic decoder-path observation, not LAN input-to-display latency or a performance promise.

For future Mac acceptance: build/install normally, authorize once, test ordinary text/keypad/modifiers/click/drag/scroll, change quality, disconnect and resume locally, test login auto-start after normal FileVault unlock, then test updates with a stable signing identity. Record failures and platform versions without publishing pairing keys or typed content.

## File clipboard development checks — 2026-10-09

FileChecks exercises actual C# source/sink and encrypted transfer against an
independent loopback peer: Unicode/empty/multi-chunk files, byte/hash checks,
metadata/path/count/size rejection, wrong session/consent, cancellation cleanup and
stalled-authentication deadlines. ClientChecks uses the exact rebuilt WPF assembly
with injected file clipboard and temporary files, testing automatic upload/download,
input while transferring, paste readiness, no echo, newer-copy priority, revocation,
recovery and existing video/quality/disconnect regressions. DPAPI checks cover
remembered file consent. No real user clipboard or credentials are used.

Swift 5.9.2 compiles/runs production file models/transport on Linux with a Wire
fixture and a test CryptoKit module backed by OpenSSL. Production MacFiles adapter
checks use mocked AppKit/Darwin/Wire for consent, metadata offers, baseline, duplicates,
revocation/recovery, stale IDs and stopped timers. These do **not** validate Apple's
CryptoKit/AppKit, native sockets or the macOS SDK. Mac runs of the script also
compile the full native Host; those runs are pending.

```sh
dotnet run --project tests/FileChecks/FileChecks.csproj -c Release
bash tests/FileChecks/check.sh
```

Native two-machine file clipboard acceptance, concurrent LAN latency and large-file
throughput remain unverified. Published release assets are unchanged. See [file
behavior and limits](files.md).

## Compact interface and defaults (development)

The exact rebuilt Windows candidate passed one-row minimum-width layout,
connected credentials, transient file notices, optional metrics and existing
encrypted-peer input/video/text/file regressions. DPAPI checks cover fresh-on,
explicit-off and corrupt preferences. Real Swift checks cover isolated Mac
sharing preferences; full native SwiftUI/Apple SDK acceptance remains pending.
See the [record](../tests/results/compact-ui-2026-10-09.txt).

## File pacing and clipboard completion (development)

C# transfer checks and the exact WPF candidate cover cumulative pacing, byte
completion vs clipboard readiness, delayed/busy publication, false confirmation
fallback, cancellation and existing control/video regressions. Real Swift
compiler checks include a paced 8 MiB transport and legacy/missing confirmation.
Native Apple SDK/clipboard and physical LAN contention remain pending.
See the [benchmark and check record](../tests/results/file-speed-2026-10-09.txt).

## Product rename (development)

Current code and packaging use **Local Remote Desktop / LocalRemoteDesktop**.
Old data and protocol identities remain compatible. Actual Windows/WPF and
DPAPI migration checks passed; native Mac installation and TCC/Keychain
acceptance remain pending. See the [rename check record](../tests/results/product-rename-2026-10-10.txt).

## Mac controller source preview — 2026-10-10

The v0.3.0-alpha.2 Windows candidate passed actual build, WPF encrypted-peer
regressions and production Windows desktop capture/encode/decode checks.
Production Swift controller core, model, transport and session checks passed
on Linux with platform/decoder adapters. These do not establish an Apple SDK
build or actual Mac decoding and desktop input acceptance. The optional native
Windows input check was skipped because the OS refused test-window focus.
See the [full check record](../tests/results/mac-controller-2026-10-10.txt).

Native Mac and two-machine acceptance are required before releasing this work.
SSH to a logged-in development Mac can run native builds and collect test logs;
macOS privacy approval remains an interactive system operation. A Mac CI runner
can automate builds, but a headless build does not validate desktop permissions,
rendering or input acceptance.

## Unified desktop UI — 2026-10-10

See [native UI progress](native-ui.md) and the [check record](../tests/results/native-ui-2026-10-10.txt). Windows UI fixtures stay unshown; they are not native input acceptance. The updated Mac UI has syntax checks only until the development Mac is reachable.

## Recovery and installation — 2026-10-11

Unshown production Windows encrypted peers verify transient reconnect, cancellation, changed-password proof rejection, invalid server proof and incompatible input model; drafts remain intact. Installer checks (21 cases) and signing control-flow checks (13 cases) use disposable Linux trees and mocked macOS tools. Default installation preserves decisions after compatibility checks; repair is explicit. These are not native TCC/Keychain results. See the [desktop acceptance checklist](desktop-acceptance.md).

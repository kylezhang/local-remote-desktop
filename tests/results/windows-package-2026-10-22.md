# Windows extraction/startup correction — 2026-10-22

## Report and correction

The owner's extracted candidate retained the apphost but only 18 of 474 files.
The first failed path was 261 characters; Windows long-path support was disabled.
Re-extraction to a short isolated directory restored all files and the owner
confirmed the app opened. No machine policy change was needed.

New packages retain the versioned archive filename, with app files directly at
its root. `package-files.json` inventories every supplied file other than itself;
`CHECK-INSTALLATION.cmd` runs a read-only verifier, without starting the app or
changing the system execution policy. The script is BOM-encoded for Windows
PowerShell 5.1 bilingual diagnostics. Optional separately installed FFmpeg files
do not invalidate the inventory. This is extraction integrity, not code signing.

CI now extracts the final ZIP to a new short directory (including spaces), checks
all bytes/provenance, then runs the extracted EXE in explicit `--check-startup`
mode. It loads the actual self-contained apphost/runtime and unshown home resources,
reports the build identity, and exits. Normal startup still uses `MainWindow.xaml`.
The client suite references the extracted DLL, not the pre-archive directory.

The first diagnostic attempt rejected assigning null to WPF `StartupUri`; the
corrected path leaves it unset for diagnosis and sets the normal URI only during
ordinary launch. Final-package execution caught and verified that correction.

## Local checks

Local application/package source: **e04d499928c7523f0348e9db8134643b8b3090b2**.
This remains a development package displaying **0.3.0-alpha.5**, not the immutable
published alpha.5 (source fb1e8d1), and not a published alpha.6.

| Check | Result |
|---|---|
| Windows production Release compilation | Pass |
| Complete flat ZIP, 476 inventoried files plus manifest | Pass |
| Missing runtime, same-length corruption, wrong source, duplicate/traversal/absolute paths, omitted runtime, overlong directory | Rejected as expected |
| Intact package with optional side-by-side dependency | Pass |
| Bundled verifier on Windows PowerShell 5.1 | Pass |
| Isolated extraction + actual packaged EXE startup + build JSON report | Pass, no visible window |
| ProtocolChecks, FileChecks, ClipboardChecks, LocalizationChecks, DeviceNameChecks, DiscoveryChecks, WindowsHostChecks | Pass |
| ClientChecks against final extracted assembly | Pass, full default unshown suite |
| CloudReleaseChecks / AtomGitSyncChecks | Pass, 5 / 26 cases; independent loopback APIs |
| Release/localization checks, PowerShell syntax and actionlint | Pass |

The complete WPF suite ran at native **150% DPI**, in English/Chinese and its
default/minimum/multiple-width fixtures. An old 179-DIP lower bound rounded down
to 178 2/3 DIPs; it now allows one physical pixel while preserving the anti-stretch
upper bound. The accepting page also had a real 3 1/3-DIP routine overflow at this
scale. A 10-DIP reduction in its outer vertical spacing removed it; the existing
no-scroll assertion remains at 0.5 DIPs and passes. No overflow was hidden.

Local development ZIP SHA-256 (not a public release asset):

```text
d8ef490f226655138ce8b69b8ff042d6f9d978df4f5665508c584f1930d6eab8  LocalRemoteDesktop-v0.3.0-alpha.5-windows-x64.zip
```

## Scope and next gate

No working client was replaced or opened. No real input, screen capture, personal
clipboard, pairing data or permissions were touched. Network fixtures used
independent loopback peers; sandbox-denied loopback attempts were rerun with
the needed local networking permission. Actual multicast registration remained
opt-in and was not run. FFmpeg tests used generated video and software encoding.

The Mac app code,
bundle identity and credentials are unchanged. Mac native build, actual installed
startup/permissions/updates, physical two-device input/clipboard and sustained
hardware performance remain separate gates. Interactive acceptance stays paused
while the owner has no Mac available. No tag or release asset is replaced here.

## Exact-source cloud candidate

[Desktop CI run 39761373979](https://github.com/kylezhang/local-remote-desktop/actions/runs/39761373979)
completed successfully for **e31fe13ec95460d5b5639a637cd754b9ff2e126a**:

- Source/package checks, native Windows and native Apple Silicon jobs passed.
- Windows passed the eight existing suites plus WindowsPackageChecks and
  PackageStartupCheck. The downloaded startup JSON identifies the exact source.
- The existing-alpha publication/sync job passed; no new version/tag was requested.
- Windows artifact **12418411554** and Mac artifact **12417873420** were downloaded.
  CI identity, checksums, Windows flat ZIP/all 476 inventoried files, both source
  archives, Mac stable bundle/preview inventory and GPL were independently checked.
- The candidates retain their exact CI bytes under shorter local filenames.
  Their app version remains alpha.5; source e31fe13 distinguishes them from the
  published alpha.5. They were not installed or opened for interactive acceptance.

| Cloud package | SHA-256 |
|---|---|
| Windows ZIP | `06c6d6941f5a0fc9d61a94e2ea496d5c4c3f9982b4f0ac42d907f0440aa91f14` |
| Mac ZIP | `cd18bfb273801c027cf78893431a8e3af7ae629025e8d203cd4eea1534810b4c` |
| Mac DMG | `b08e4f603e75b4b9370a849f90e4667e61ecfc5588e3a1da580923e97e41c9a7` |

This clears the new native CI/package gate, not the physical desktop, Mac
TCC/Keychain/installed-update or sustained performance gates above. Alpha.6 remains
planned; published alpha.5 assets remain immutable.

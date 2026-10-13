# Cross-device native checks

These helpers have different scopes. They are not included in routine checks.
Do not publish local metadata, IPs, credentials, captured frames or clipboard data.

## Background interoperability

On Windows, run ClientChecks with `LOCALREMOTE_LAN_BACKGROUND_CHECK=1`,
`LOCALREMOTE_LAN_ADDRESS` set to that machine's private IPv4 and
`LOCALREMOTE_LAN_METADATA` set to a disposable local ready-file path. Use the
usual .NET test runtime and an H.264-capable FFmpeg on PATH. The helper binds
47980/47981; it fails if the ports are in use and never changes firewall rules.

Once ready, run on the Mac:

```sh
bash tests/MacControllerChecks/background-lan.sh <Windows-fixture-private-IP> 47980
```

Only connect to the companion fixture, not the user's sharing application. It
uses a public test password, generated 320×180 software H.264, recording input
and recording clipboard adapters. Three quality sessions, native VideoToolbox
decode, ordered keys/release, Unicode text and three-file batches travel across
the real network. Regular files are disposable, including an empty file and a
512 KiB deterministic binary. No window, real capture, pasteboard, Keychain,
OS input or user pairing changes occur.

## Scheduled physical acceptance

`NativeLANCheck.swift` with `LOCALREMOTE_LAN_INPUT_CHECK=1` targets the companion
Windows test-owned field. Focus must be explicitly established. The guarded
adapter refuses down/wheel events when that window loses foreground; key/button
release remains allowed. Logs distinguish foreground refusals from driver errors.
The helper uses synthetic Media Foundation hardware H.264, not a captured desktop.
Decoder completion or submitted-event counts do not prove field acceptance.

`NativeAcceptanceTarget.swift` is an opt-in Mac field. It reports only fixture
coordinates, timestamp/process identity and expected-text/event results. It exits
after success or timeout and invalidates focus before exit. `LOCALREMOTE_LIVE_MAC_CHECK=1`
plus `LOCALREMOTE_INTERACTIVE_CHECKS=1` runs the Windows live probe against the
installed Mac Host. Supply its private address, an existing paired saved address
and fresh local fixture metadata. The probe reuses that paired secret only in
memory, does not save it, and leaves clipboard sync off. It waits for live decoded
frames **and** capabilities before verifying Accessibility and submitting input.

Run only in an owner-agreed period with that dedicated Mac field focused. Capture
and input use the installed Host's actual permission context. Read the Mac target
result separately: posted CGEvents alone are not application acceptance. Native
Keychain interaction, real clipboard/files and sustained performance require
separate scheduled checks. An ad-hoc build does not validate distribution signing.

中文：后台互通用真实网络、生成画面和记录型输入/剪贴板，不能当成实际桌面验收。
真实输入检查必须约定时段并使用专用框；焦点丢失或元数据过期必须停止。
系统事件提交计数、解码通过、测试框实际收到输入要分别记录；测试密码不是用户凭据。

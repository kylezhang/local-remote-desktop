<p align="center">
  <img src="assets/brand/icon.png" alt="Local Remote Desktop" width="112" />
</p>

# Local Remote Desktop

**English** | [简体中文](README.zh-CN.md)

## Background

This project started with an everyday office need: control a Mac from a Windows PC. After trying several tools, I still hadn't found a client that felt right. Some were awkward to use, others were dated, and some required payment. For a simple personal setup connecting my own two computers over the LAN, I wanted a lightweight, convenient tool without an additional cost.

With AI-assisted development, I decided to build a LAN remote desktop tool myself, starting with that everyday need and keeping connection and control as simple as possible.

## Introduction

Local Remote Desktop lets you view and control another computer's current desktop over your LAN. Devices connect directly, without an account or a cloud relay. When you return to the remote computer, your work is still in the same session.

The current alpha provides **Windows ↔ Mac** desktop roles in one application per platform: connect to another device or accept connections to this one. Mobile controllers, Linux and HarmonyOS are planned; they are not available yet.

## Features

- **Current-session control:** connect to a Windows or Mac desktop using the other computer's keyboard and mouse.
- **Local, paired connections:** authentication and encryption, with pairing credentials stored securely on each device.
- **Remembered connections:** keep your target address and password for the next connection.
- **Desktop controls:** full screen, automatic reconnect and three picture-quality presets, adjustable while connected.
- **Convenient pairing:** generate a password or choose your own; existing keys remain valid until explicitly changed.
- **Text and file clipboard:** copy and paste plain text and regular files between paired devices, without pairing again for each transfer.

The alpha also includes nearby-device selection and English/Chinese interfaces. Both desktop roles are implemented; complete two-device permission, clipboard and update acceptance is still pending. See the [user guide](docs/usage.md) for behavior and limits.

## Installation

The latest published preview is **v0.3.0-alpha.5**. Download the complete Windows and Apple Silicon Mac packages, corresponding source and SHA-256 checksums from [GitHub](https://github.com/kylezhang/local-remote-desktop/releases/tag/v0.3.0-alpha.5). The [AtomGit release](https://atomgit.com/zkxw2008/local-remote-desktop/releases/tag/v0.3.0-alpha.5) currently has an incomplete attachment mirror; use GitHub for the complete set. Earlier releases remain available; v0.1.0 is the historical stable baseline.

The screen-desk interface refresh on the main branch is still in development and is not included in alpha.5. See the [roadmap](docs/roadmap.md) for the next candidate and its validation gates.

### Windows

Requires **Windows 10/11, x64**.

1. Download and extract the Windows ZIP.
2. Install [FFmpeg](https://ffmpeg.org/download.html) with H.264 decoding and make `ffmpeg.exe` available on PATH or beside the application. An existing working installation can be reused.
3. Run `LocalRemoteDesktop.exe`.

Keep the extracted folder together. Release packages include the .NET Desktop Runtime.

### macOS

Requires **macOS 13+, Apple Silicon**. No local compiler or developer membership is needed for the downloaded app.

1. Download and open the Mac DMG. Quit an older running copy before replacing it.
2. Drag **Local Remote Desktop.app** to **Applications**, then open the installed copy.
3. When accepting connections on macOS 14+, select the main display in the system picker. On macOS 13, grant Screen Recording access. Keyboard/mouse control also requires Accessibility approval.

The Mac app is **ad-hoc signed and not notarized**. macOS may require normal first-open approval or renewed Accessibility/Keychain approval after an update; do not disable Gatekeeper or repeatedly reinstall to reopen the app. The new screen-selection/update behavior has not yet completed physical acceptance. See the [Mac guide](docs/macos.md) for installation and troubleshooting.

## Usage

1. On the computer you want to control, choose **Accept connections**, enable sharing and obtain its pairing password.
2. On the other computer, choose **Start a connection**, select a nearby device or enter its **Target IP**, then connect using that password.
3. Click the remote picture to use your keyboard and mouse. Disconnect when finished; the target's desktop session stays open.

Both devices must be directly reachable on the private network, with TCP **47980** allowed to the target (**47981** for files). Share the password only with devices you trust.

On Windows, **F11** toggles full screen and **Ctrl+Alt+Esc** releases remote input. With Windows Shortcuts enabled, Ctrl+C/V maps to the Mac's Command+C/V. Clipboard sharing is enabled by default in supported paired sessions and can be changed in Settings → Clipboard. See the [file guide](docs/files.md) for supported copy-and-paste formats.

Currently, one controller and the primary display are supported. The target must be awake and logged in. Login-screen control, secure-desktop bypass and FileVault preboot unlock are unsupported; lock-screen control is not guaranteed.

## Development

The native applications use **Swift** on macOS and **C# / .NET 8 / WPF** on Windows. Mac capture and encoding use ScreenCaptureKit and VideoToolbox; the Windows viewer currently decodes through FFmpeg. Sessions use authenticated, encrypted TCP connections over the LAN. See the [architecture](docs/architecture.md) for details.

GitHub Actions is the primary build and automated-test environment. After source
is mirrored from AtomGit, it builds Windows x64 and an Apple Silicon Mac preview.
See [online builds and downloads](docs/continuous-integration.md). Local compilation
is optional; from the repository root, build Windows with the .NET 8 SDK:

```powershell
dotnet build apps/windows/LocalRemoteDesktop.csproj -c Release
dotnet run --project apps/windows/LocalRemoteDesktop.csproj -c Release
```

Build the Mac application on macOS:

```sh
bash apps/macos/build-app.sh
```

Native Windows/Mac builds, background checks, unshown bilingual layouts and automated release synchronization have passed in GitHub Actions. The [release record](tests/results/automated-release-2026-10-14.md) separates CI evidence, earlier user-reported desktop control and outstanding physical acceptance. Changes on main do not overwrite published packages. Check [validation status](docs/validation.md) and [Contributing](CONTRIBUTING.md) before preparing a release.

## Documentation

- [Product direction](docs/product.md) · [v0.3.0 delivery plan](docs/v0.3.0-plan.md) · [UI design proposal](docs/design.md)

- [User guide](docs/usage.md) · [File clipboard](docs/files.md)
- [Mac controller preview](docs/mac-controller.md) · [Windows sharing preview](docs/windows-sharing.md)
- [Mac installation and troubleshooting](docs/macos.md) · [Windows source build](apps/windows/BUILD.md)
- [Roadmap and version plan](docs/roadmap.md) · [Changelog](CHANGELOG.md)
- [Architecture](docs/architecture.md) · [Protocol](docs/protocol.md) · [Performance](docs/performance.md)
- [Cross-platform interface conventions](docs/interface.md) · [Project layout](docs/naming.md)
- [Development handbook](docs/development-handbook.md)
- [Online builds and downloads](docs/continuous-integration.md)
- [Validation status](docs/validation.md) · [Release process](docs/releasing.md)

## License

Local Remote Desktop is licensed under the [GNU General Public License v3.0 or later](LICENSE) (`GPL-3.0-or-later`). You may use, modify and redistribute it, including commercially. When distributing covered modified versions or binaries, follow GPL's corresponding-source and same-license requirements.

Third-party components retain their own licenses. See [NOTICE](NOTICE.md) for copyright, dependency and distribution information.

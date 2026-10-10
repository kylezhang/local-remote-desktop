<p align="center">
  <img src="assets/brand/icon.png" alt="LocalRemote" width="112" />
</p>

# LocalRemote

**Remote control across your local network.**

LocalRemote lets you view and control another computer's existing desktop over your LAN. Connect directly, without an account or a cloud relay, and continue the same work when you return to that computer.

**Currently available: Windows → Mac.** Two-way desktop control and mobile controllers are planned.

**Preview: v0.2.0-beta.1** — shorter pairing passwords, text/file clipboard sharing and a simpler Mac interface. Windows is packaged; Mac is source-only. Native Mac acceptance is pending. The latest stable release remains v0.1.0.

**English** | [简体中文](README.zh-CN.md)

[Download](https://atomgit.com/zkxw2008/LocalRemote/releases) · [Documentation](#documentation) · [Roadmap](docs/roadmap.md) · [Changelog](CHANGELOG.md)

Unreleased main adds [nearby-device selection](docs/discovery.md). Native Mac
Bonjour acceptance is pending; this does not replace the published preview.

## Features

- **Work on the current desktop** — view the screen and use your keyboard and mouse.
- **Connect locally** — a direct connection between devices on the same network.
- **Pair trusted devices** — authenticated, encrypted connections with securely stored pairing keys.
- **Keep setup convenient** — remember your Mac address and key, use full screen, and reconnect after interruptions.
- **Adjust picture quality** — choose Smooth, Standard or High, including while connected.

## Supported platforms

| Platform | Role | Requirements |
|---|---|---|
| Windows | Control a Mac | Windows 10/11, x64 |
| macOS | Share the current desktop | macOS 13+, Apple Silicon |

The current release supports one controller and one display. The Mac must be awake and logged in; FileVault unlock and control before login are not supported. Lock-screen control is not guaranteed. The application interface is currently Chinese; English UI is planned.

## Installation

### Windows

1. Download the Windows x64 ZIP from [Releases](https://atomgit.com/zkxw2008/LocalRemote/releases) and extract it.
2. Install a complete [FFmpeg build](https://ffmpeg.org/download.html) with H.264 decoding and add it to PATH.
3. Run `LocalRemote.exe` from the extracted folder (`RemoteMacLAN.exe` in the existing v0.1.0 release).

The ZIP includes the .NET Desktop Runtime. If FFmpeg already works on your machine, you can keep that installation. The executable retains its original name for compatibility.

### macOS

Install Xcode Command Line Tools if needed (`xcode-select --install`), then build and install:

```sh
git clone https://atomgit.com/zkxw2008/LocalRemote.git
cd LocalRemote
bash apps/macos/install-app.sh
```

Grant **Screen Recording** and **Accessibility** in System Settings → Privacy & Security. Quit and reopen the application after granting access, then start sharing. Run the installer as your normal user, without `sudo`.

The default installer resets this app's two permission decisions for a clean installation. Updates can use `--keep-permissions` when new and installed signatures are compatible; incompatible updates stop before changing the installed app or permissions. See the Mac guide before updating an ad-hoc development build.

macOS is currently distributed as source. See the [Mac guide](docs/macos.md) for signing, updates, permissions and troubleshooting.

## Usage

1. Open LocalRemote on the Mac, start sharing, and copy its pairing key.
2. On Windows, enter the Mac's LAN IP address and pairing key, then connect.
3. Click the remote picture to control the desktop. Disconnect when finished; your Mac session stays open.

Both devices must be reachable on the same network. Allow TCP **47980** to the Mac. Keep the pairing key private and use LocalRemote on a trusted LAN.

| Shortcut | Action |
|---|---|
| F11 | Toggle full screen |
| Ctrl+Alt+Esc | Release remote input |
| Ctrl key, Windows shortcut mode on | Send the Mac Command modifier |

The development branch enables **Windows shortcuts** by default: Ctrl+C/V operates the Mac clipboard. Disable it for the original Ctrl→Control / Win→Command mapping. Published v0.1.0 packages still use that original mapping.

Development builds implement **bidirectional text and file clipboard sharing**, enabled by default for paired sessions. Both apps remember an explicit off choice in Settings → Clipboard. Wait for the enabled status, then copy again. Text is limited to 32 KiB; regular files transfer automatically with progress and cancellation, without repeated pairing or per-file dialogs. Windows encrypted-peer/UI tests pass; native Mac acceptance is pending. Published v0.1.0 packages do not include clipboard sharing. See the [user guide](docs/usage.md#text-clipboard-sync-development) and [file guide](docs/files.md) for supported formats and limits.

Development builds also support a manually chosen **8–64 character pairing password** or a generated **12-character password**. Click Change Password beside Current Password on the Mac, enter or generate a password in the separate editor, then Save Password. Existing 64-character keys remain valid until explicitly changed; update both apps before using passwords. See the [user guide](docs/usage.md#pairing-password-development).

Quality changes briefly reconnect the session. See the [user guide](docs/usage.md) for controls, picture settings and connection help.

## Roadmap

The goal is one application on each supported device, with a familiar way to connect or allow a connection. Next steps focus on reliable installation, English UI, easier pairing and device discovery, followed by Windows ↔ Mac control and mobile controllers.

The installation/input improvements originally planned for v0.1.1/v0.1.2 are consolidated into the **v0.2.0-beta.1** preview. **v0.2.0** targets validated discovery/clipboard/localization, **v0.3.0** desktop control in both directions, then **v1.0.0** stable desktop, **v1.1–v1.2** Android/iOS controllers, **v1.3.0** Linux desktop and **v1.4.0** HarmonyOS phone/tablet controllers. See the [roadmap](docs/roadmap.md) for scope and release criteria; these features are not yet released.

## Background

LocalRemote began with a practical frustration: finding a Windows-to-Mac client that fit an everyday office workflow. The tools I tried felt awkward, were dated, or required payment. AI-assisted development made it practical to build a focused alternative, starting with the devices I use every day.

## Documentation

- [User guide](docs/usage.md) and [Mac troubleshooting](docs/macos.md)
- [File copy and paste](docs/files.md)
- [Windows source build](apps/windows/BUILD.md)
- [Architecture](docs/architecture.md) and [security protocol](docs/protocol.md)
- [Build and test status](docs/validation.md)
- [Resource use and performance](docs/performance.md)
- [Contributing](CONTRIBUTING.md) and [release process](docs/releasing.md)
- [Project layout and naming migration](docs/naming.md)

## License

An open-source license has not yet been selected. See [NOTICE](NOTICE.md) for the current distribution and dependency information.

<img src="assets/brand/icon.png" alt="LocalRemote icon" width="96" />

# LocalRemote

**Local-first remote desktop. Your devices, your network, your current workspace.**

[简体中文](README.zh-CN.md) · [Roadmap](docs/roadmap.md) · [Release notes](CHANGELOG.md) · [Releases](https://atomgit.com/zkxw2008/LocalRemote/releases)

## Why this exists

I wanted to use a Windows desktop to work on a Mac on the same office network. The tools I tried did not quite fit: some felt awkward, some clients were dated, and some required payment. I wanted a small tool that handled this one everyday problem without adding accounts or cloud infrastructure.

With AI-assisted development making it practical to build and iterate on a focused application, I started LocalRemote. The first goal is simple: see and control the Mac's actual desktop from Windows, then pick up the Mac and continue the same work.

## Available today: v0.1.0

**Windows 10/11 x64 → Apple Silicon macOS 13+.** This is the first named product release; the earlier 0.2.x numbers were development iterations. Both applications now report 0.1.0. This numbering reset does not remove the existing features.

- Current-session desktop sharing, mouse movement/buttons/dragging, scrolling and physical keyboard input.
- Direct LAN connection using a manually entered Mac IPv4 address; no account, public relay or central server.
- Shared-key pairing, mutual authentication and encrypted video/control; remembered Windows address and key protected with DPAPI.
- Full screen, focus-loss input release, reconnect after an authenticated connection, and quality changes while connected (a brief reconnect applies the new setting).
- Mac login auto-start via an optional user LaunchAgent, permission diagnostics and a local keyboard self-test.

| Quality | Requested maximum | Requested bitrate |
|---|---|---|
| Smooth / 流畅 | 1280×720, 30 fps | 4 Mbps |
| Standard / 标准 | 1920×1080, 60 fps | 10 Mbps |
| High / 高清 | 1920×1080, 60 fps | 18 Mbps |

These are encoder requests, not measured end-to-end guarantees. Start with Smooth. Windows currently uses FFmpeg software decoding and WPF rendering; hardware decoding and Direct3D rendering are future work.

## Get started

### 1. Prepare the Mac

Install Xcode Command Line Tools if necessary (`xcode-select --install`). Clone the repository and run, as your normal logged-in user:

```sh
git clone https://atomgit.com/zkxw2008/LocalRemote.git
cd LocalRemote
bash macos-host/install-app.sh
```

The installer builds first, replaces the complete old application bundle and opens the installed app. It preserves the pairing key and does **not** reset screen permissions by default.

Allow **Screen Recording / Screen & System Audio Recording** and **Accessibility** in System Settings → Privacy & Security. The wording varies by macOS version. After granting access, quit and reopen the app; do not reinstall merely to reopen it. Click **开始共享** (Start Sharing), then reveal/copy the pairing key. Audio is not captured and no recording file is written.

### 2. Open the Windows app

Download and extract the Windows x64 ZIP from [Releases](https://atomgit.com/zkxw2008/LocalRemote/releases), then run `RemoteMacLAN.exe`. The ZIP includes the .NET Desktop Runtime. **FFmpeg remains a separate dependency**: install a build with an H.264 decoder via the [official download entry point](https://ffmpeg.org/download.html), and put the complete installation on PATH, or beside the executable with any DLLs it needs. Existing users with working FFmpeg need not reinstall it.

Source builds require .NET 8 SDK; see [Windows build instructions](windows-client/BUILD.md). Only the release ZIP is self-contained; a normal development build still needs the .NET Desktop Runtime.

### 3. Connect

Find the Mac's LAN IPv4 address in its network settings. Both devices must be on a mutually reachable LAN, with TCP **47980** allowed to the Mac. Guest Wi-Fi device isolation can prevent connectivity.

Enter the Mac address and its 64-character hexadecimal pairing key on Windows, select Smooth and connect. Click the remote picture to send keyboard input. Test with a normal text editor first. The key grants control of the Mac: share it only with trusted devices.

- **F11**: full screen. **Ctrl+Alt+Esc**: release remote input and return local focus.
- Ctrl → Mac Control, Alt → Option, Win → Command. For example, Win+C maps to Mac Command+C. Windows-reserved shortcuts may stay on Windows.
- IP and key are remembered using current-user DPAPI. **忘记连接** (Forget Connection) clears the selected saved connection.
- Change quality while connected; the picture briefly disconnects and automatically reconnects in about three seconds. Rapid changes use the last selection.

## Limits and trust

One controller, one primary display, IPv4 only. No audio, file transfer, recording, clipboard sync, automatic discovery or mobile application in v0.1.0. Windows IME composition is not transmitted; physical key events can use the Mac's own input method.

This shares an existing logged-in desktop. It does **not** create another login session, bypass FileVault Preboot, or promise control before user login. Lock-screen and secure-field input depend on macOS permissions and system behavior and are not guaranteed. Sleeping or closed-lid Macs may be unreachable.

Mac keys use Keychain; Windows keys use DPAPI. The connection uses HMAC authentication and AES-GCM encryption, but the protocol has not had an independent security audit and does not provide forward secrecy. Do not expose the port to the internet. See [protocol and threat limits](docs/protocol.md).

Ad-hoc Mac signing can invalidate permission/Keychain trust after code changes. A fixed valid Apple signing identity reduces update-related identity changes; it cannot guarantee that macOS never asks again. See [Mac installation and troubleshooting](docs/macos.md).

## Where we are heading

The vision is one familiar application on each supported device: choose a nearby device, pair, connect and work. A device may initiate control or accept control, subject to permission. Windows ↔ Mac comes next; phones as controllers and more desktop platforms come later. **Those capabilities are a roadmap, not current support.** The product remains focused on local networks. See the [roadmap](docs/roadmap.md).

## Development and validation

[Architecture](docs/architecture.md) · [Protocol](docs/protocol.md) · [Validation](docs/validation.md) · [Contributing](CONTRIBUTING.md) · [Release process](docs/releasing.md)

Windows is built and tested with real WPF/FFmpeg against synthetic encrypted peers. The user has reported successful real Windows→Mac use. The release environment is Windows: it cannot independently compile or run the final Mac bundle. The Mac branding/resource changes remain unverified on an actual Mac; native Mac binaries are not attached to this release. This is an early 0.x product, not a claim of universal compatibility or measured 1080p60 performance.

Compatibility note: the internal `RemoteMacLAN` executable/project names, `/Applications/RemoteMacLAN.app`, `com.remotemaclan.host`, RML1 wire format and saved credential locations are deliberately retained to avoid unnecessary permission and pairing migration. The product and repository are LocalRemote.

The repository remains private; no open-source license has been selected. Third-party runtime notices are retained in release packages; FFmpeg is not bundled. See [NOTICE](NOTICE.md).

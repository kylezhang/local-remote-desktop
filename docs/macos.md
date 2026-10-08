# macOS installation, updates and permissions

[简体中文](zh-CN/macos.md)

Requirements: Apple Silicon, macOS 13+, normal logged-in GUI session, Xcode Command Line Tools with Swift 5.9+. Run scripts as that user, without sudo. The application displays Chinese controls in v0.1.0; full UI localization is future work.

```sh
bash macos-host/install-app.sh
```

This builds first, verifies the staged signature, stops existing Host instances and a loaded login agent, deletes only the two verified old application bundles at `/Applications/RemoteMacLAN.app` and `~/Applications/RemoteMacLAN.app`, installs the full bundle, then starts one instance. Build failure leaves the installed app intact. Pairing data is not deleted; screen permission reset is opt-in. Changing code under ad-hoc signing can still require new authorization.

Grant Screen Recording / Screen & System Audio Recording and Accessibility to the installed application. Quit and reopen it after granting; do not run install again simply to reopen. Click Start Sharing (开始共享). Accessibility false means view-only. If legacy screen preflight is false, an explicit sharing click checks the actual ScreenCaptureKit API; startup failures expose domain/code in Copy Startup Diagnostic (复制启动诊断). A successful build does not prove TCC approval.

## Stable signing

[Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) explains that ad-hoc designated requirements identify a particular code version. Keeping the path and bundle ID alone does not stabilize identity across code updates.

Configure an existing valid Apple Development or Developer ID Application signing identity with its private key on the Mac, then run:

```sh
bash macos-host/configure-signing.sh
bash macos-host/install-app.sh
```

The configuration script signs/verifies a temporary probe, saves the chosen fingerprint in ignored local files, and enables stable mode. It does not create certificates, change trust settings, read pairing secrets or reset permissions. Future builds stop if the pinned identity fails; they do not fall back to ad-hoc. The first migration from ad-hoc still requires approval. Certificates, Keychain policy and macOS behavior mean permanent prompt-free operation is not guaranteed. `SIGN_IDENTITY` can select a valid existing identity explicitly.

## Troubleshooting

Do not repeatedly reinstall/reset when the app is already authorized. Verify the entry corresponds to the installed application, quit/reopen, and use the startup diagnostic and local keyboard self-test. Keychain reads occur once per process startup; failed reads are retried only with the explicit button. Updating an ad-hoc executable can affect Keychain ACL trust.

```sh
bash macos-host/diagnose-permissions.sh
```

Only when repairing a confirmed stale screen permission entry:

```sh
bash macos-host/install-app.sh --reset-screen-permission
```

That option runs targeted `tccutil reset ScreenCapture com.remotemaclan.host`, not a global reset or Accessibility reset. You must grant screen access again afterward. The keyboard self-test posts only into its own temporary window; success does not prove another application or secure field accepts remote input.

## Login auto-start and limits

After manual connection succeeds, run `bash macos-host/install-launch-agent.sh`. It installs a user Aqua-session LaunchAgent at `~/Library/LaunchAgents/com.remotemaclan.host.plist`. It starts after normal user login; it does not continually restart an intentionally quit app. If screen preflight is false, explicit Start Sharing may still be necessary.

Remove auto-start:

```sh
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist
rm ~/Library/LaunchAgents/com.remotemaclan.host.plist
```

No FileVault Preboot bypass, new login session or guaranteed login/lock-screen control. Sleeping or closed-lid devices may be unreachable; the app does not alter power policy.

Pairing rotation (stops trusting every existing controller): stop and quit the Host, run `security delete-generic-password -s com.remotemaclan.host -a pairing-v1`, then reopen and pair again. Do not publish the new key. The app bundle/binary and credential service retain legacy names for compatibility; Finder and UI use LocalRemote branding.

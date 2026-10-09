# macOS installation, updates and permissions

[简体中文](zh-CN/macos.md)

Requirements: Apple Silicon, macOS 13+, normal logged-in GUI session, Xcode Command Line Tools with Swift 5.9+. Run scripts as that user, without sudo. The application displays Chinese controls in v0.1.0; full UI localization is future work.

```sh
bash macos-host/install-app.sh
```

This builds first, verifies the staged signature and icon resources, stops existing Host instances and a loaded login agent, resets this app's Screen Recording and Accessibility authorization, completely removes the two verified old application bundles from `/Applications/RemoteMacLAN.app` and `~/Applications/RemoteMacLAN.app`, installs the full bundle, then starts one instance. Old contents are never merged into the new app. Build or resource failure leaves the installed app intact. The pairing key is retained. Default installation is a clean permission installation: grant both permissions again afterward.

During replacement, the complete old bundles are held temporarily inside a private staging directory. If moving or verifying the new bundle fails, or installation receives an interrupt/termination signal, the installer attempts to restore those bundles. It deletes temporary backups after successful verification. If restoration fails, it reports and retains the backup directory for recovery. This protects bundle contents, not already-reset permissions, process state or hard power loss. A loaded login agent remains stopped after rollback. If only launching fails, the verified new app stays installed and the message explains how to open it.

Grant Screen Recording / Screen & System Audio Recording and Accessibility to the installed application. The Screen Permission (屏幕权限) and Accessibility Permission (辅助功能权限) buttons open the corresponding settings; the latter also requests Accessibility access. Quit and reopen after granting; do not run install again simply to reopen, because default installation resets both decisions again. Click Start Sharing (开始共享). Accessibility false means view-only. If legacy screen preflight is false, an explicit sharing click checks the actual ScreenCaptureKit API; startup failures expose domain/code in Copy Startup Diagnostic (复制启动诊断). A successful build does not prove TCC approval.

For updates with compatible signing requirements, use `bash macos-host/install-app.sh --keep-permissions`. Before stopping the existing Host or changing permissions, the installer compares the new designated requirement with each installed copy and verifies both bundles against those requirements. A mismatch, unreadable requirement or failed verification aborts and leaves the running Host, installed bundles and permissions untouched. Changed ad-hoc builds normally fail this check; the option is not a solution to unstable signing. Identical requirements also do not guarantee that macOS retains every permission.

The default reset is scoped to `com.remotemaclan.host`; it never resets other apps or deletes the pairing key. macOS controls the settings list: resetting authorization does not guarantee an old row immediately disappears, and it does not grant permission automatically. See [Apple's authorization reset documentation](https://developer.apple.com/documentation/xcode/resetting-access-to-protected-resources-in-macos).

## Dock icon

The bundle includes both PNG and ICNS resources with an explicit `LocalRemote.icns` declaration. At launch, the app loads the packaged PNG (ICNS fallback) and assigns it to [NSApplication.applicationIconImage](https://developer.apple.com/documentation/appkit/nsapplication/applicationiconimage), including when a LaunchAgent starts the executable directly. The installer verifies staged icon files and updates the bundle timestamp without deleting global icon caches or restarting the Dock. Startup diagnostics include `AppIconLoaded`; the diagnosis script reports the installed icon declaration and resource checksums.

## Stable signing

[Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) explains that ad-hoc designated requirements identify a particular code version. Keeping the path and bundle ID alone does not stabilize identity across code updates.

Configure an existing valid Apple Development or Developer ID Application signing identity with its private key on the Mac, then run:

```sh
bash macos-host/configure-signing.sh
bash macos-host/install-app.sh
```

The configuration script signs/verifies a temporary probe, saves the chosen fingerprint in ignored local files, and enables stable mode. It does not create certificates, change trust settings, read pairing secrets or reset permissions. Future builds stop if the pinned identity fails; they do not fall back to ad-hoc. The first migration from ad-hoc still requires approval. Certificates, Keychain policy and macOS behavior mean permanent prompt-free operation is not guaranteed. `SIGN_IDENTITY` can select a valid existing identity explicitly.

## Troubleshooting

Development builds signed ad-hoc, or without a signing-mode resource, do not access Keychain at startup. Start Sharing first checks screen access; if that fails, no credential read occurs. Certificate auto-start also checks screen access first. A failed background read waits for a deliberate action; there is no automatic interactive fallback. The packaged signing mode is a startup policy hint, not a certificate-trust check. Diagnostics include the mode, internal build number, backend and read/create counts, never the key value.

To inspect the pairing key independently of screen permission, enable Show Pairing Key and click Read Pairing Key (读取配对密钥). That deliberate action may authenticate; it does not start sharing. Sharing later reuses the successfully loaded key for that process.

Explicit sharing clicks can allow macOS authentication and first-time creation; failure is not automatically retried. Successfully loaded keys are reused in memory for that process. Development builds therefore open after login but do not automatically begin sharing. Certificate builds can auto-start only when both the key and screen permission are accessible without UI.

Queries explicitly select the existing file-based Keychain (`kSecUseDataProtectionKeychain=false`), retaining the service/account and ACL. [Apple's Keychain bridge source](https://github.com/apple-oss-distributions/Security/blob/main/OSX/libsecurity_keychain/lib/SecItem.cpp) shows that an unspecified backend can query both stores, while legacy-compatible additions prefer the file-based store. This removes ambiguous routing; it does not establish why the user received two dialogs. No storage migration or modern-store fallback is attempted.

Certificate-build silent reads temporarily disable legacy interaction and restore the previous setting afterward; see [Apple's interaction control](https://developer.apple.com/documentation/security/seckeychainsetuserinteractionallowed(_:)) and [background execution guidance](https://developer.apple.com/documentation/security/secitemcopymatching(_:_:)). Modern query-level UI keys are not relied on for this explicitly legacy backend. These compatibility controls are deprecated. The exact two-dialog cause and revised native behavior have not been reproduced on a Mac; do not consider the prompt issue fixed. No plaintext fallback, key rotation or permissive ACL is introduced.

The shared startup ordering/cache/cancellation logic is compiled and exercised by [MacStartupChecks](../tests/MacStartupChecks/check.sh). Linux execution does not validate Apple frameworks or system dialogs. See the [stable signing and distribution plan](macos-signing.md) for the permanent update path.

```sh
bash macos-host/diagnose-permissions.sh
```

The default clean installation resets both entries. The legacy screen-only option remains available:

```sh
bash macos-host/install-app.sh --reset-screen-permission
```

That option resets only this app's Screen Recording decision and preserves Accessibility. `--reset-permissions` explicitly requests the default two-service reset. `--keep-permissions` skips both only after the compatibility checks pass. If a reset fails, installation stops before deleting the old app; earlier successful reset operations may already have revoked a decision. The keyboard self-test posts only into its own temporary window; success does not prove another application or secure field accepts remote input.

## Login auto-start and limits

After manual connection succeeds, run `bash macos-host/install-launch-agent.sh`. It installs a user Aqua-session LaunchAgent at `~/Library/LaunchAgents/com.remotemaclan.host.plist`. It starts after normal user login; it does not continually restart an intentionally quit app. If screen preflight is false, explicit Start Sharing may still be necessary.

Remove auto-start:

```sh
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist
rm ~/Library/LaunchAgents/com.remotemaclan.host.plist
```

No FileVault Preboot bypass, new login session or guaranteed login/lock-screen control. Sleeping or closed-lid devices may be unreachable; the app does not alter power policy.

Pairing rotation (stops trusting every existing controller): stop and quit the Host, run `security delete-generic-password -s com.remotemaclan.host -a pairing-v1`, then reopen and pair again. Do not publish the new key. The app bundle/binary and credential service retain legacy names for compatibility; Finder and UI use LocalRemote branding.

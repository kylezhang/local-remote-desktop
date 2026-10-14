# macOS installation, updates and permissions

[简体中文](zh-CN/macos.md)

Requirements: Apple Silicon, macOS 13+, normal logged-in GUI session. Current builds offer English and Chinese interfaces.

## Downloaded application

Open the cloud-built DMG and drag **Local Remote Desktop.app** to **Applications**.
Quit the older running copy before replacing it; open the installed application,
rather than a copy in Downloads or a source-build directory. No local compiler or
Apple Developer membership is needed to use this package. It is ad-hoc signed and
unnotarized, so macOS may require its normal first-open approval.

On macOS 14+, choose **Accept connections → Start accepting connections**, then
select the main display in the system picker. That selected filter authorizes the
capture; no legacy Screen Recording toggle is required for this path. Accessibility
remains separate and necessary for keyboard/mouse control. Canceling selection
does not read Keychain or start a listener. Same-run reconnect/quality changes
reuse the selection; relaunch requires a fresh selection. See [screen access](mac-screen-access.md).
macOS 13 retains global Screen Recording approval. Changed ad-hoc builds may still
require Accessibility/Keychain reapproval; this is not automatically reset or granted.

## Build from source

For development, install Xcode Command Line Tools with Swift 5.9+ and run as the
logged-in user, without sudo:

```sh
bash apps/macos/install-app.sh
```

The build records the physical checkout path in its ignored `.build-native`
cache. After moving or renaming the project (or on the first build with an older,
unmarked cache), it clears only that cache and rebuilds. This prevents stale
`SwiftShims`/Clang module paths; subsequent builds at the same path stay incremental.
Cache symlinks are refused. This cleanup does not reset permissions, pairing or
signing settings, and a failed build leaves the installed app intact.

This builds first, verifies the staged signature and icon resources, stops existing Host instances and a loaded login agent, preserves this app's Screen Recording and Accessibility decisions after signature compatibility checks, and replaces verified current/legacy application bundles in `/Applications` and `~/Applications`. The result is `/Applications/Local Remote Desktop.app`; old `RemoteMacLAN.app` copies are removed after verification, and any existing login job is updated to the new executable path. Old contents are never merged into the new app. Build or resource failure leaves the installed app intact. The pairing key is retained. Normal installation does not reset permissions. First installation and explicit permission repair still require OS approval.

During replacement, the complete old bundles are held temporarily inside a private staging directory. If moving or verifying the new bundle fails, or installation receives an interrupt/termination signal, the installer attempts to restore those bundles. It deletes temporary backups after successful verification. If restoration fails, it reports and retains the backup directory for recovery. This protects bundle contents, not already-reset permissions, process state or hard power loss. A loaded login agent remains stopped after rollback. If only launching fails, the verified new app stays installed and the message explains how to open it.

Accessibility approval must apply to the installed application; false means view-only. The Accessibility Permission button opens settings and requests access. macOS 13 also needs Screen Recording approval and may need a quit/reopen; on macOS 14+ use the system display picker described above. Do not run install again simply to reopen. Copy Startup Diagnostic exposes actual errors and authorization mode; a successful build does not prove TCC approval.

Normal installation now uses the permission-preserving path; `--keep-permissions` is an explicit alias. Before stopping the existing Host or changing permissions, the installer compares the new designated requirement with each installed copy and verifies both bundles against those requirements. A mismatch, unreadable requirement or failed verification aborts and leaves the running Host, installed bundles and permissions untouched. Changed ad-hoc builds normally fail this check; the option is not a solution to unstable signing. Identical requirements also do not guarantee that macOS retains every permission.

An explicit reset is scoped to `com.remotemaclan.host`; it never resets other apps or deletes the pairing key. macOS controls the settings list: resetting authorization does not guarantee an old row immediately disappears, and it does not grant permission automatically. See [Apple's authorization reset documentation](https://developer.apple.com/documentation/xcode/resetting-access-to-protected-resources-in-macos).

## Dock icon

The bundle includes both PNG and ICNS resources with an explicit `LocalRemoteDesktop.icns` declaration. At launch, the app loads the packaged PNG (ICNS fallback) and assigns it to [NSApplication.applicationIconImage](https://developer.apple.com/documentation/appkit/nsapplication/applicationiconimage), including when a LaunchAgent starts the executable directly. The installer verifies staged icon files and updates the bundle timestamp without deleting global icon caches or restarting the Dock. Startup diagnostics include `AppIconLoaded`; the diagnosis script reports the installed icon declaration and resource checksums.

## Optional certificate signing

[Apple TN3127](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements) explains that ad-hoc designated requirements identify a particular code version. Keeping the path and bundle ID alone does not stabilize identity across code updates.

This optional path is for maintainers who already have an identity; it is not required for the maintained ad-hoc download. Configure an existing valid Apple Development or Developer ID Application signing identity with its private key on the Mac, then run:

```sh
bash apps/macos/configure-signing.sh
# First migration from ad-hoc signing requires explicit permission repair.
bash apps/macos/install-app.sh --reset-permissions
```

The configuration script signs/verifies a temporary probe, saves the chosen fingerprint in ignored local files, and enables stable mode. It does not create certificates, change trust settings, read pairing secrets or reset permissions. Future builds stop if the pinned identity fails; they do not fall back to ad-hoc. The first migration from ad-hoc still requires approval. Certificates, Keychain policy and macOS behavior mean permanent prompt-free operation is not guaranteed. `SIGN_IDENTITY` can select a valid existing identity explicitly.

## Troubleshooting

Development builds signed ad-hoc, or without a signing-mode resource, do not access Keychain at startup. Start Sharing first checks screen access; if that fails, no credential read occurs. Certificate auto-start also checks screen access first. A failed background read waits for a deliberate action; there is no automatic interactive fallback. The packaged signing mode is a startup policy hint, not a certificate-trust check. Diagnostics include the mode, internal build number, backend and read/create counts, never the key value.

To inspect the credential independently of screen permission, click **查看** (View) beside **当前密码** (Current Password). That deliberate action may authenticate; it does not start sharing or change the sharing status. Later views and sharing reuse the successfully loaded credential for that process. **复制** copies the exact password without display line breaks.

Explicit sharing clicks can allow macOS authentication and first-time creation; failure is not automatically retried. Successfully loaded keys are reused in memory for that process. Development builds therefore open after login but do not automatically begin sharing. Certificate builds can auto-start only when both the key and screen permission are accessible without UI.

Queries explicitly select the existing file-based Keychain (`kSecUseDataProtectionKeychain=false`), retaining the service/account and ACL. [Apple's Keychain bridge source](https://github.com/apple-oss-distributions/Security/blob/main/OSX/libsecurity_keychain/lib/SecItem.cpp) shows that an unspecified backend can query both stores, while legacy-compatible additions prefer the file-based store. This removes ambiguous routing; it does not establish why the user received two dialogs. No storage migration or modern-store fallback is attempted.

Certificate-build silent reads temporarily disable legacy interaction and restore the previous setting afterward; see [Apple's interaction control](https://developer.apple.com/documentation/security/seckeychainsetuserinteractionallowed(_:)) and [background execution guidance](https://developer.apple.com/documentation/security/secitemcopymatching(_:_:)). Modern query-level UI keys are not relied on for this explicitly legacy backend. These compatibility controls are deprecated. The exact two-dialog cause and revised native behavior have not been reproduced on a Mac; do not consider the prompt issue fixed. No plaintext fallback, automatic key rotation or permissive ACL is introduced.

The shared startup ordering/cache/cancellation logic is compiled and exercised by [MacStartupChecks](../tests/MacStartupChecks/check.sh). Linux execution does not validate Apple frameworks or system dialogs. See the [stable signing and distribution plan](macos-signing.md) for the permanent update path.

```sh
bash apps/macos/diagnose-permissions.sh
```

Only run repair if it is needed: `bash apps/macos/install-app.sh --reset-permissions` resets both entries. The legacy screen-only option remains available:

```sh
bash apps/macos/install-app.sh --reset-screen-permission
```

That option resets only this app's Screen Recording decision and preserves Accessibility. `--reset-permissions` explicitly requests a two-service repair reset. `--keep-permissions` skips both only after the compatibility checks pass. If a reset fails, installation stops before deleting the old app; earlier successful reset operations may already have revoked a decision. The keyboard self-test posts only into its own temporary window; success does not prove another application or secure field accepts remote input.

## Login auto-start and limits

After manual connection succeeds, run `bash apps/macos/install-launch-agent.sh`. It installs a user Aqua-session LaunchAgent at `~/Library/LaunchAgents/com.remotemaclan.host.plist`. It starts after normal user login; it does not continually restart an intentionally quit app. If screen preflight is false, explicit Start Sharing may still be necessary.

Remove auto-start:

```sh
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.remotemaclan.host.plist
rm ~/Library/LaunchAgents/com.remotemaclan.host.plist
```

No FileVault Preboot bypass, new login session or guaranteed login/lock-screen control. Sleeping or closed-lid devices may be unreachable; the app does not alter power policy.

In development builds, change pairing in **当前密码 → 修改密码… → 保存密码**: enter an eight-character password or Generate an eight-character draft; only Save applies it. Saving replaces the existing credential and disconnects all current controllers; enter the new password on updated Windows clients. Existing legacy keys remain until saved. Do not delete the Keychain item for a normal password change or publish the password. Application files use Local Remote Desktop; the established bundle/credential/launchd identities remain stable. See [naming migration](naming.md).

## SDK 27 Command Line Tools compatibility

The 2026-10-13 native build verified macOS 27 SDK / Apple Swift 6.4 with only
Command Line Tools. This SDK exposes @State as a macro, but the tested tools
lacked its SwiftUIMacros plugin. The application's ViewState alias uses the
existing SwiftUI State property wrapper instead and keeps the macOS 13 baseline.
Update source before building; missing-macro failures are not permission failures
and do not call for reinstalling/resetting TCC. Native build success does not prove
installed UI, Keychain approval or remote input acceptance.

The same native validation found system Bash 3.2 packaging/Chinese-output issues
and a newer codesign requirement prefix. Build 27 fixes these without resetting
permissions or weakening signature preservation. Native unshown layout and
isolated bundle checks are recorded in [native progress](native-ui.md); normal
installation still rejects incompatible signing before replacing the old app.

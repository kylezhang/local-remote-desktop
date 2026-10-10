# Project layout and naming

[简体中文](zh-CN/naming.md) · [Product name](product-name.md)

**Local Remote Desktop** is the product. The repository slug is
`local-remote-desktop`; `LocalRemoteDesktop` is the native source/assembly name.
Platform folders describe their OS rather than fixing a permanent client/host role.

```text
apps/
  macos/       Swift package, Sources/LocalRemoteDesktop, installer
  windows/     LocalRemoteDesktop.csproj, LocalRemoteDesktop namespace
assets/brand/  Shared icons and editable symbol
docs/          English and zh-CN documentation
scripts/       Build and package tooling
tests/         Checks and historical validation records
```

Mac builds produce `Local Remote Desktop.app` with `Contents/MacOS/LocalRemoteDesktop`
and install at `/Applications/Local Remote Desktop.app`. Windows produces
`LocalRemoteDesktop.exe` and `LocalRemoteDesktop.dll`. Current icon containers and
build/package scripts use LocalRemoteDesktop names.

## Existing installations

| Retained identity | Compatibility purpose |
|---|---|
| Mac bundle ID, Keychain service and LaunchAgent label `com.remotemaclan.host` | Preserve the user's privacy/storage/login identity; Keychain account remains `pairing-v1`. |
| Windows `%LOCALAPPDATA%/LocalRemote` | Keep passwords, consent, language, remembered connections and received files. |
| Mac Application Support `LocalRemote/ReceivedFiles` | Preserve received files; do not duplicate or relocate user data. |
| `_localremote._tcp` discovery, RML1/RMP1 and `LOCALREMOTE-PAIRING-PASSWORD-V1:` | Keep existing authenticated peers and stored credentials compatible. |
| `LOCALREMOTE_*` / `RML_*` development environment switches | Keep build/test/signing configuration compatible. |

The Mac installer checks all six current/previous system/user bundle paths,
stops LocalRemoteDesktop, LocalRemote and RemoteMacLAN processes, replaces bundles
without merging contents and restores complete backups on verification failure.
It updates the existing login job after verification. A keep-permissions request
requires compatible signatures; renaming does not guarantee ad-hoc authorization.
The old LocalRemote.app and RemoteMacLAN.app are removed only as this app's verified
bundles. No Keychain or stored pairing data is deleted.

Windows keeps the existing encrypted-store migration from RemoteMacLAN to
LocalRemote. No new data-store migration is introduced. Locally maintained old
launch directories may retain a single LocalRemote.exe compatibility launcher
for existing shortcuts; it starts the current LocalRemoteDesktop assembly.
New packages and the canonical build directory use LocalRemoteDesktop.exe.

Released v0.1.0/v0.2.0-beta.1 tags, asset names and validation records are historical;
do not rewrite or overwrite them. Current repository links use the new slug.

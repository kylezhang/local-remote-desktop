# Project layout and naming

[简体中文](zh-CN/naming.md)

LocalRemote is the product, repository, application and source namespace. Platform
directories describe the platform, rather than permanently assigning client/host
roles; Windows-to-Mac remains the only implemented direction today.

```text
apps/
  macos/                 Swift package, Sources/LocalRemote, installer
  windows/               LocalRemote.csproj, LocalRemote namespace
assets/brand/            Shared icons and editable identity
docs/                    English/zh-CN documentation, including protocol.md
scripts/                 Build/package/brand tooling
tests/                   Automated checks and historical validation records
```

Current builds produce `LocalRemote.app` with `Contents/MacOS/LocalRemote` on
Mac, and `LocalRemote.exe`/`LocalRemote.dll` on Windows. Mac installation uses
`/Applications/LocalRemote.app`. Build, package and test scripts use these names.

## Compatibility during updates

| Retained identity | Reason |
|---|---|
| Mac bundle ID `com.remotemaclan.host` | An established signing/privacy identity; changing it would require a separate authorization migration. |
| Keychain service `com.remotemaclan.host`, account `pairing-v1` | Keep the user's existing pairing key. |
| LaunchAgent label `com.remotemaclan.host` | Keep one login job; update its executable path to the renamed application. |
| Protocol magic `RML1` and existing crypto labels | Renamed applications remain compatible with previously deployed peers. |

These are stable internal contracts, not visible product names. New display,
source and artifact names use LocalRemote. Do not change persistent identities
just to satisfy a text search.

The Mac installer validates and backs up all four known current/legacy system
and user bundles before replacement. It removes old `RemoteMacLAN.app` copies,
can restore them on verification failure, and stops either executable name.
It retains pairing storage and updates an existing login job's path only after
the new app is verified. The same keep/reset-permissions policies still apply;
renaming does not guarantee that macOS preserves authorization.

Git moves tracked source, but ignored signing pins remain in old checkouts.
Development builds copy an existing legacy signing pin/strict-policy marker into
the new directory when missing. New settings win; release builds do not alter
those local settings. Old RML signing environment names remain accepted aliases.

Windows copies recognized DPAPI records from `%LOCALAPPDATA%/RemoteMacLAN` to
`%LOCALAPPDATA%/LocalRemote` once. It does not decrypt/re-encrypt during migration,
overwrite newer records or delete originals. The completion marker prevents
forgotten connections from being restored later. Old local executable shortcuts
may have a compatibility deployment copy; release packages only use LocalRemote.

Existing v0.1.0 tags, downloadable archives and historical test records keep their
original names. Naming changes belong to the upcoming release, not rewritten history.

# Development handbook

[简体中文](zh-CN/development-handbook.md) · [Product](product.md) · [Interface](interface.md) · [Roadmap](roadmap.md)

Project agreements and lessons recorded on 2026-10-11. Read this before starting a
new platform or changing an established workflow. The linked scope and validation
records determine what is implemented; this handbook is not a support claim.

## Product commitments

- One native app per platform. **Connect** and **Share this device** are roles of
  that app, not separate products. Share the current logged-in desktop and retain
  its work when the remote connection ends.
- Solve the personal office workflow first: simple connection, familiar input,
  remembered pairing and predictable recovery. Avoid accounts, cloud relays,
  public traversal and unrelated management features.
- Reuse a reachable private IPv4 route, including an already configured VPN.
  Manual Target IP works independently of discovery. Multicast discovery may not
  cross routed networks; RDP reaching a device does not open our application's ports.
- Native Swift/C# and the existing LAN transport remain the desktop foundation.
  Do not introduce a rewrite or WebRTC to address an input-permission bug.
- New platforms must distinguish screen capture, receiving input and controlling
  another device. Controller-first mobile plans do not permanently exclude sharing,
  nor promise unrestricted phone control. Document actual OS limits and evidence.

## Interaction and UI agreements

- Use **Target IP**, **Local name**, **Pairing password**, **Connect**, **Share this
  device**, **Start sharing** and **Stop sharing** consistently. Adapt grammar and
  native shortcuts; avoid inventing another flow for each platform.
- Default local name comes from the OS and is editable on Share. The shared app
  announces its name/platform through discovery and authenticated capabilities.
  An unknown manually entered IP displays no invented name or operating system.
  Discovery metadata is not an authenticated identity or access permission.
- New passwords are exactly eight printable ASCII characters without spaces;
  automatic generation produces an eight-character draft. Existing longer secrets
  remain valid. Generate/edit/cancel does not change the current password. Save is
  atomic; successful replacement ends the old session and requires the new password.
- Remember IP/password through appropriate OS secure storage. No automatic
  Keychain credential reads merely to populate a list. Read saved credentials on
  a deliberate connection action; retain a failed draft and provide Forget.
- Settings have four categories: General, Connection, Clipboard and About. Keep
  local sharing credentials on Share and target credentials on Connect. Defaults
  apply to the next connection; session controls do not overwrite them.
- Global clipboard consent is a ceiling; session choices may only restrict it.
  Text and regular files default on for paired sessions unless previously disabled.
  No extra manual authentication or per-file confirmation during an allowed session.
- Prioritize the remote screen. Use one compact session bar and temporary transfer
  progress/cancel notices. Avoid duplicate logos, oversized headers, repeated LAN
  disclaimers and implementation details in normal product flows.
- Keep the approved brand assets and palette, native Segoe UI/San Francisco and
  shared hierarchy. Desktop entry windows start around **640 × 620 logical units**
  including native chrome, centered within usable screen bounds with a margin.
  Keep the navigation rail near 144. Use one device list and one Add device entry;
  show credentials/quality only in the connection dialog. Stack sharing information
  and its password vertically in one surface. Long content scrolls;
  do not fill a wide window with empty fixed-width forms. Users may resize/fullscreen;
  navigation and connection should not repeatedly reset their window size.
- Future desktop clients follow the same content proportions, rather than blindly
  copying physical pixels. Phones/tablets adapt to available space, touch and native
  safe areas; they do not inherit a fixed desktop window.
- Design changes affecting flows are reviewed before replacing native UI. Approved
  minor refinements may be implemented directly. Mockups must label fictional devices
  and future platforms; they are not proof of a working native client.

## Engineering rules learned from desktop work

- Discovery is not pairing. File channels repeat authentication automatically with
  fresh nonces and require the active desktop token and consent generation. Control
  uses TCP 47980 and regular-file contents use separate TCP 47981. Keep old peers
  compatible through explicit capabilities; a file-port failure must not kill video.
- Map shortcuts to the target OS: Windows Ctrl+C/V → Mac Command+C/V; Mac
  Command+C/V → Windows Ctrl+C/V. Release held keys/buttons on focus loss,
  disconnect and failure. A successful injection call is not application acceptance.
- Clipboard changes take a fresh baseline on enable/reconnect, suppress received
  echoes and preserve newer local copies. Queue text before paste. Files require
  confirmed receiver readiness; suppress an early paste key pair and let the user
  retry. Do not execute paste later against a potentially different focused app.
- Current limits: text 32 KiB UTF-8; regular files 32 per batch, 4 GiB per file and
  8 GiB per batch; 32 KiB chunks, portable validated names and SHA-256 before commit.
  Folders, virtual files and raw image pixels are separate future work, not promised
  by regular-file clipboard support.
- Use local generated staging destinations; never accept peer filesystem paths.
  Remove incomplete batches on normal failure/cancel. Completed files remain until
  users clean them; preserve existing received-directory locations across renames.
- Keep capture, decode, clipboard and network lifetimes explicit. Use bounded queues,
  latest-frame presentation, one file job, cancellation and timeouts. Clipboard
  access stays on the UI thread; disk, hashing and sockets run off it. Record resource
  and latency measurements separately from short correctness fixtures.
- Do not describe a transient failure as an endless loop without evidence. Diagnose
  focus, consent, received/posted/rejected input and transport independently.

## Installation, identity and permissions

- Public product: **Local Remote Desktop**. Repository: `local-remote-desktop`.
  Windows launcher/assembly: `LocalRemoteDesktop`. Old output directories may remain
  compatibility deployment targets, not duplicate products or source directories.
- Preserve `com.remotemaclan.host`, established Keychain/service identifiers and
  stored credentials unless a deliberate migration has passed acceptance. Cosmetic
  renaming must not silently reset privacy permissions or lose pairing.
- Build first; if it fails, keep the installed application. Stop only authorized
  instances before replacement and remove the old app bundle completely. Verify
  the new bundle, executable and icons; never layer a new bundle over stale contents.
- Normal upgrades should preserve permissions. Permission repair/reset must be an
  explicit documented path, not a retry loop of reinstalling and resetting TCC.
  A reset command cannot grant permissions or guarantee every visible settings row
  disappears. OS approval still belongs to the user.
- Ad-hoc signature changes can invalidate TCC/Keychain trust. Stable signing and
  actual restart/update acceptance are needed; do not claim a source edit fixes all
  password prompts. Do not read/write the same stored secret repeatedly on launch.
- Promise only the logged-in user session. FileVault preboot, system login/lock-screen
  access and Windows secure desktops are not bypassed. Screen capture streams do
  not imply recording files or audio features.

## Development and validation workflow

- Continue clearly authorized work; avoid repeated confirmation for routine edits.
  Finish coding and applicable independent checks before asking the owner to test.
  Report the concrete outcome, remaining uncertainty and next step after each turn.
- Do not open foreground test windows, steal focus, access the user's clipboard,
  capture their active desktop or inject real OS input during their work. Use unshown
  layouts, generated video, isolated storage, recording services and loopback peers.
  Real interaction acceptance requires an agreed period and a dedicated target.
- Do not retry a known unreachable Mac SSH route. Resume only after a reachable
  validation environment is explicitly supplied. End users do not need SSH or CI
  for ordinary use. Maintain useful Windows/portable checks while native gates wait.
- Separate evidence: source/static checks; real platform build; portable adapters;
  native runtime fixtures; installed two-device acceptance; user-reported behavior.
  Explicitly record what did not run. Never turn a mock into a native success claim.
- Consolidate genuine native acceptance into a prepared checklist instead of making
  the owner repeatedly debug coding mistakes. Fix independent failures first.
- Rebuild Windows after code changes and deploy the verified package to the existing
  agreed launch locations. Keep one product executable. If a client is in use, defer
  replacement until it exits; do not close/reopen it or spawn a competing instance.
  Only the newest verified candidate may be queued for deferred deployment.

## Repository and release rules

- English is primary, with aligned Chinese documentation and shared localization.
  README structure: Background, Introduction, Features, Installation, Usage,
  Development, Documentation and License. Keep development caveats out of the
  opening product pitch; place downloads and details in their relevant sections.
- AtomGit is the project remote; do not substitute another hosting service. Commit
  meaningful increments with feat/fix/docs/test/chore prefixes and push verified work.
  Keep output, caches, local logs, credentials, certificates and personal device data
  out of Git. Check tracked files as well as ignore rules.
- Original project work uses **GPL-3.0-or-later**. Preserve third-party notices.
  Binary packages include license, corresponding exact committed source, build
  instructions, runtime notices and checksums; do not ship build debris.
- Versioned platform scope and gates live in the [roadmap](roadmap.md) and
  [delivery plan](v0.3.0-plan.md). v0.3.0 targets verified Windows ↔ Mac; v1.0.0
  stable desktop distribution; v1.1.0 Android, v1.2.0 iOS/iPadOS, v1.3.0 Linux and
  v1.4.0 HarmonyOS remain gated targets, not release-date promises. Android,
  iOS/iPadOS and HarmonyOS milestones start with controllers; Linux targets both
  controlling and sharing supported desktop sessions.
- Tags and release assets are immutable. Align versions, tag the tested commit,
  package clean source and read back release metadata/assets. AtomGit's homepage
  latest selection is separate from list order; release title is only `vX.Y.Z`
  (with its alpha/beta suffix). A latest marker does not clear native acceptance.
- No new stable desktop release until real two-device and required native/installer
  gates pass. Any source-only or Windows-only preview states its limits clearly.

When an agreement changes, update this handbook, its Chinese counterpart and the
specific product/interface/protocol/release document in the same commit. Record
observed results in [validation](validation.md); keep tentative conclusions labeled.

Normal Mac installation uses permission preservation by default. Signature changes stop before replacement; --reset-permissions is explicit repair. Use meaningful device empty states and content-sized settings/sharing sections rather than filling spare space with unrelated controls.

Deferred Windows deployment must be verified after exit, not reported as completed merely because it was queued. Compare deployed DLL hashes and SOURCE.txt against the tested candidate. Hidden Windows PowerShell helpers launched from PowerShell 7 must load required modules from their own runtime; inherited module paths can leave Get-FileHash unavailable. Record actual failure details and validate the same hidden launch path.

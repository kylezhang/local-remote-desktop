# Revision 2: design and implementation contract

Status: **approved for incremental implementation**. Native local-name editing and
authenticated peer metadata are implemented; the full interaction/visual contract
is still in progress. See [native progress](../../docs/native-ui.md). This approval
does not replace native acceptance or authorize an unverified release.

## Start with the task

The job is to sit at one device and continue work on another device's current
desktop. The recurring journey is **open → choose → connect → work**, not configure
services, identify host/client products, or manage a dashboard. One native app
provides Connect and Share this device. Keep paired connections convenient;
discovery helps locate a target but never authenticates it.

The previous implementation fell short in three ways: settings mixed preferences,
local permissions and troubleshooting; desktop navigation did not carry the
proposal's typography/spacing/control styles through to native screens; legacy
deployment paths retained a launcher name that no longer matched the product.
This review addresses those problems without changing the approved brand or
rewriting working desktop transport.

## Proposed information architecture

| Place | Owns | Does not own |
|---|---|---|
| Connect | Real device list, online/offline states, Target IP, first password and remembered target | Local sharing password or screen permission requests |
| Share this device | Local availability/in-use state, stop action, local IP/password, sharing permission recovery | Another target's password or connection defaults |
| Settings → General | Language and proposed native theme preference | Network claims, clipboard access or diagnostics |
| Settings → Connection | Next-connection quality, reconnect and shortcut defaults | Controls that silently change an active session |
| Settings → Clipboard | Global local consent for text/files, received-file folder | A second authentication step or unrelated connection settings |
| Settings → About | Product, version, GPL notice; expandable troubleshooting report | Permanent developer counters on routine screens |
| Session options | Temporary text/file/shortcut choices for the current connection | Editing credentials or overriding a disabled global consent |

Settings sits at the bottom of the desktop rail; Connect and Share remain the
primary tasks at the top. Four small settings tabs avoid a long mixed list and
another sidebar. Mobile uses the same vocabulary with bottom navigation and
scrollable settings tabs. Theme selection, secure Mac controller remembrance,
reverse discovery/clipboard/files and diagnostic redaction are **proposed
behavior**, not claims of implementation in the current alpha.

## State and interaction decisions

- A selected remembered online device needs one Connect action. An unremembered
  device shows its password and secure-remember choice inline. Wrong passwords
  retain the input for correction. Selection never starts control.
- Manual connection has **one dialog** containing Target IP, password and the
  remember choice; it does not split into an address dialog followed by pairing.
  A matching discovery or saved record also shows its name and platform there.
  Without a record, show only the address and say device information follows
  connection; never derive a name or platform from an IP. After authentication,
  refresh the list and session label from peer-supplied metadata. Cancel or failed
  authentication must not create/rename a record; matching addresses must not
  create duplicate rows. Discovery labels help recognition, not authorization.
- Saved-but-offline devices remain visible with a disabled Connect action and a
  concrete recovery hint. Never mark an offline device as ready.
- Forget password removes only that target's saved credential after an explicit
  action. Keep its identity/address; do not reset all preferences or sharing keys.
- The current one-role restriction is explicit. Connecting while sharing asks
  **Stop sharing & connect** after target validation. Cancel preserves sharing.
  The feature plan may later support concurrent roles; the current design does
  not imply that capability.
- Share distinguishes Off, Available and In use. Navigation does not stop sharing.
  Stop sharing is visible locally and ends the active remote session.
- Share → This device’s name has a Change name action. Default to the local
  system name; a saved override belongs to this app, not OS computer settings.
  Edit opens a prefilled draft; Cancel preserves the name and Use system name
  fills the draft only. Save accepts 1–32 Unicode characters without controls,
  trims surrounding whitespace and updates the local name without stopping
  sharing or changing pairing. Use the same name in discovery and authenticated
  peer metadata; platform is app-reported and cannot be edited as a name.
  Mac advertises its app name through DNS-SD/TXT; Windows parses it with the
  platform record. Both hosts send configurable names after authentication;
  existing heartbeats propagate renames. Windows publication and Mac browsing
  remain follow-up work, so reverse discovery is not a completed capability.
  Native propagation must refresh one existing record, retaining credentials;
  do not use a mutable display name as the saved-password identity.
- Permission recovery belongs to Share. Screen denial blocks sharing; missing
  input permission allows a clearly labeled view-only session. Opening Connect
  does not trigger these permissions. The preview never opens system settings.
- Password Reveal and Copy are compact labeled icon actions. Change opens a draft;
  Generate changes the draft only, Cancel preserves the saved password. Save's
  consequences are stated: current behavior stops sharing and disconnects, then
  the user starts sharing again. Do not promise preservation without implementing
  and testing it. Routine startup does not regenerate the password.
- Newly created passwords are exactly **8 characters**, without whitespace,
  whether typed or generated. Generate is a visible bordered secondary button
  with a refresh icon; Save remains primary. Existing native long passwords must
  remain usable until explicitly changed, rather than being truncated or rotated
  during an update. This preview policy is not yet a native authentication change.
- Connection defaults apply to **new logical connections**. Session controls have
  a distinct scope and survive temporary quality reconnects. The active session
  does not silently adopt edited future defaults.
- Global clipboard switches are local consent, default on. Turning one off stops
  that category immediately, cancels pending writes/transfers and discards stale
  consent. Session options can restrict global consent, never expand it. Both
  peers must permit sharing; advertise unsupported capabilities as unavailable.
- Reconnect dims the last frame and says that it is paused. Cancel disconnects
  and ends retries. Transfer cancellation stops the job, not the desktop session.
- No permanent LAN slogans, duplicate title-bar logos, credential toolbar or
  raw diagnostic counters on the remote screen. Future mobile sharing remains
  view-only here, subject to native system consent and feasibility checks.

## Visual decisions for faithful native implementation

The memorable asset is the approved blueberry icon. Keep ordinary controls quiet.
Use #5265EC for active actions, #202435 for readable neutral text, #697183 for
secondary text, #FFFFFF for content, #F6F7FA for the rail and #E5E8F0 for rules.
Use brand #282C67 only where needed; avoid tinting all text purple. No decorative
glow, wallpaper gradient or card dashboard around ordinary app controls.

Use Segoe UI Variable/Segoe UI on Windows, the system Apple font on Mac, and native
equivalents elsewhere. Body 14px, secondary 12px, heading 25px; system scaling
and translated text must remain readable. Standard desktop buttons 34–38px;
session actions fit a 42px toolbar. Main content has 32–36px padding; gaps use
4/8px steps. The rail is about 174px; content should have a sensible maximum
width instead of stretching controls across the window. Settings labels and
controls align to stable left and right columns. Use 1.5–1.75px line icons with
accessible names, not unrelated Unicode symbols. Radii follow hierarchy:
7px controls, 9px selection, 12–14px dialog/device mark, native app chrome.

At narrow desktop widths, stack list/details and allow content scrolling rather
than squeezing the credential form. Mobile controls need **44px minimum touch
targets**, native safe areas and a session dock. Desktop keyboard focus remains
visible; dialogs retain drafts, trap focus and return it on dismissal. Respect
reduced motion and use native dark colors rather than simply inverting images.

## Implementation after approval

1. Extract shared tokens, strings and interaction/state rules. Do not change
   Keychain service names, Mac bundle identity, signing, TCC or transport as part
   of visual work. Capability gaps remain independently tracked.
   Authenticated device-name metadata still needs a protocol implementation:
   the current capabilities include platform but not the peer's device name.
   Bound and validate display metadata, escape it, and fall back to IP / unknown
   system when absent. A display name is not a cryptographic device identity.
2. Implement the Windows shell/styles/settings and the relevant Mac native views
   in separate increments. Native controls keep OS conventions, while spacing,
   structure and action scope follow this contract.
3. Use only `LocalRemoteDesktop.exe` for the Windows product launcher. Existing
   package scripts already use that name; remove the local compatibility rename
   to `LocalRemote.exe`, update documented shortcuts and retain user preferences.
   Never add duplicate executable copies or stop an active session to deploy.
4. Check unshown WPF layout/lifecycle in English and Chinese at minimum/default
   sizes and 100/125/150% scaling. On Mac, compile against the actual SDK and
   verify installed native layout. Compare screenshots against the approved
   proposal; the prior mismatch is not fixed merely by preserving page headings.
5. Verify real pairing/returning connection, local permission recovery, cancellation,
   role changes, quality/reconnect, global consent and both-direction clipboard/
   files. Run focus-taking native input acceptance only in an agreed test window.
6. Rebuild exact-source packages, deploy to the familiar path without launching
   extra windows, and release only after native acceptance gates pass.

Browser checks confirm the proposal's simulated interactions, not remote desktop
performance, native accessibility, credential security or platform capability.

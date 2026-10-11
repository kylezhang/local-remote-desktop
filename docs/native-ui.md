# Native interface progress

The [second design revision](design.md) is approved and being implemented in stages.
Both desktop sources now include local-name editing and authenticated target
names. Both sources also use four settings categories and separate connection
defaults from active session options. Windows manual pairing uses one dialog and
saves credentials only after authentication. New sharing passwords are 8 characters
on both sources; existing longer credentials remain unchanged. Mac controller
discovery and secure target-password memory are now in source. Theme details and
native Mac acceptance remain pending.

[简体中文](zh-CN/native-ui.md) · [Design](design.md) · [Delivery plan](v0.3.0-plan.md)

The desktop design was reviewed on 2026-10-10. This is an incremental development
change, not a new release or a claim that v0.3.0 acceptance is complete.

Both desktop sources now provide **Connect**, **Share this device** and
**Settings** in one window. During a connection, navigation and credentials are
hidden; quality, full screen, options and disconnect stay on the session bar.
Sharing retains explicit start/stop, masked passwords and edit/cancel behavior.
Existing transport, credential identities and installation permissions are retained.

Windows shows discovered devices beside the connection form. Its current secure
address/password memory and paired clipboard/file workflow remain in place.
The sharing view reuses the existing lifecycle controller without displaying a
second window. Settings uses General, Connection, Clipboard and About. The rail
places Settings at its bottom; ordinary text and controls use neutral colors.

Mac uses native window chrome, system typography and the same navigation. The
controller now uses the main window, including its existing Command-key routing.
Mac now has a bounded Windows-device browser, recent-device records and a native
manual connection sheet. Credential accounts are separate from its own sharing
password; startup and discovery do not read Keychain. Reverse clipboard/files
and updated Apple SDK/installed acceptance remain follow-up work.

## Device names

Share this device → This device’s name → Change name edits a saved app display
name. Use system name fills the draft; Cancel preserves the old value. Save accepts
1–32 Unicode scalars, trims surrounding whitespace and rejects control characters.
It updates metadata without stopping the listener, replacing credentials or
changing the OS computer name. Mac republishes its Bonjour service while sharing;
Windows also republishes through the OS DNS-SD service while sharing.

Both sharing roles send `deviceName` after authentication and on the existing
heartbeat. Windows updates one address-matched list item and retains it in memory
across refreshes; this is not persistent device history or proof of availability.
Mac updates its session label and bounded nonsecret recent records. Missing/invalid legacy metadata uses the known name
or address. Changing a manual address clears unrelated name/system information.

## Manual pairing and password policy

Both desktop sources use one manual Target IP/password/remember dialog. Known targets show
real metadata; unknown targets get metadata only from an authenticated peer.
Cancel or failed authentication makes no credential write and creates no peer
record. Successful authentication saves at most once per logical connection;
storage failure does not abort that connection. An unchecked remember choice
uses the input for this session without deleting older stored credentials.
Role switching validates the target before stopping sharing and requires an
explicit choice. New passwords are 8 characters; record/wire validation still
accepts existing longer values. See the [pairing check record](../tests/results/manual-pairing-2026-10-11.txt).

## Verification

Connection defaults are saved separately for quality, reconnect and shortcut
mapping. They are copied once at a new logical connection; temporary session
changes and quality reconnects do not overwrite or reapply future defaults.
Windows global clipboard switches are persistent local consent. Session toggles
can only restrict them. Turning global text sharing off stops polling, revokes
the current request and rejects stale acknowledgements; files are disabled through
the existing cancellation path. New logical connections reset temporary restrictions.
Mac connection defaults persist similarly; reverse clipboard/files remain unsupported.
Sharing permissions are on the Mac sharing page, not settings for connecting.

Windows was built with .NET 8. Unshown WPF fixtures check English/Chinese layouts
at 820, 1100 and 1440 pixels, navigation, selection, password cancellation, a
single visible-window design, session presentation, role exclusion and listener
cleanup. The fixtures use their own credentials and fake input/capture adapters.
They do not establish real desktop input, latency or permission acceptance.

Additional unshown fixtures check the four categories at 820/1100 pixels, render
at 100/125/150% scale, default/session independence and clipboard consent revocation.
Scaled renders are not actual per-monitor DPI acceptance. See the
[settings check record](../tests/results/settings-ui-2026-10-11.txt).

Windows name persistence, metadata fallback, independent DNS-SD fixtures and
encrypted backend rename propagation pass. Swift Foundation checks cover isolated
name preferences, active host-model rename without changing pairing, publication
lifecycle and controller label updates using explicit service doubles.

Swift source syntax and portable pairing/model checks passed. The development
Mac is outside this LAN; SSH attempts are stopped. The changed Mac UI has **not**
had an Apple SDK build or installed interaction check. Earlier SDK results apply to the earlier interface only.
Do not publish a new tag or release on the basis of these checks.

`ClientChecks` now runs the unshown shell check by default. The older integration
fixtures that display windows require `LOCALREMOTE_INTERACTIVE_CHECKS=1` and an
agreed test period. The physical LAN input harness remains separately opt-in.

See the [shell record](../tests/results/native-ui-2026-10-10.txt) and
[name checks](../tests/results/device-names-2026-10-11.txt). Next: apply the remaining
approved pairing/visual details, restore
native Mac verification and finish session-owned clipboard/files,
then perform scheduled two-device acceptance before packaging v0.3.0.

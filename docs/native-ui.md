# Native interface progress

The [second design revision](design.md) is approved and being implemented in stages.
Both desktop sources now include local-name editing and authenticated target
names. The complete revised settings, pairing flow and visual treatment are pending.

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
second window. Settings groups match the design and the same session preferences.

Mac uses native window chrome, system typography and the same navigation. The
controller now uses the main window, including its existing Command-key routing.
Mac controller discovery, persistent pairing memory, reverse clipboard/files and
the compact first-pairing dialog remain follow-up work. The current manual form
stays available so those gaps do not masquerade as working features.

## Device names

Share this device → This device’s name → Change name edits a saved app display
name. Use system name fills the draft; Cancel preserves the old value. Save accepts
1–32 Unicode scalars, trims surrounding whitespace and rejects control characters.
It updates metadata without stopping the listener, replacing credentials or
changing the OS computer name. Mac republishes its Bonjour service while sharing;
Windows publication remains pending.

Both sharing roles send `deviceName` after authentication and on the existing
heartbeat. Windows updates one address-matched list item and retains it in memory
across refreshes; this is not persistent device history or proof of availability.
Mac updates its session label. Missing/invalid legacy metadata uses the known name
or address. Changing a manual address clears unrelated name/system information.

## Verification

Windows was built with .NET 8. Unshown WPF fixtures check English/Chinese layouts
at 820, 1100 and 1440 pixels, navigation, selection, password cancellation, a
single visible-window design, session presentation, role exclusion and listener
cleanup. The fixtures use their own credentials and fake input/capture adapters.
They do not establish real desktop input, latency or permission acceptance.

Windows name persistence, metadata fallback, independent DNS-SD fixtures and
encrypted backend rename propagation pass. Swift Foundation checks cover isolated
name preferences, active host-model rename without changing pairing, publication
lifecycle and controller label updates using explicit service doubles.

Swift source syntax checks passed. The development Mac's SSH connection timed
out, so the changed Mac UI has **not** had an Apple SDK build or installed
interaction check. Earlier SDK results apply to the earlier interface only.
Do not publish a new tag or release on the basis of these checks.

`ClientChecks` now runs the unshown shell check by default. The older integration
fixtures that display windows require `LOCALREMOTE_INTERACTIVE_CHECKS=1` and an
agreed test period. The physical LAN input harness remains separately opt-in.

See the [shell record](../tests/results/native-ui-2026-10-10.txt) and
[name checks](../tests/results/device-names-2026-10-11.txt). Next: apply the remaining
approved settings/visual changes, restore
native Mac verification, finish reverse discovery and session-owned clipboard/files,
then perform scheduled two-device acceptance before packaging v0.3.0.

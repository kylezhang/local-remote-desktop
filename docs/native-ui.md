# Native interface progress

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

## Verification

Windows was built with .NET 8. Unshown WPF fixtures check English/Chinese layouts
at 820, 1100 and 1440 pixels, navigation, selection, password cancellation, a
single visible-window design, session presentation, role exclusion and listener
cleanup. The fixtures use their own credentials and fake input/capture adapters.
They do not establish real desktop input, latency or permission acceptance.

Swift source syntax checks passed. The development Mac's SSH connection timed
out twice, so the changed Mac UI has **not** had an Apple SDK build or installed
interaction check. Earlier SDK results apply to the earlier interface only.
Do not publish a new tag or release on the basis of these checks.

`ClientChecks` now runs the unshown shell check by default. The older integration
fixtures that display windows require `LOCALREMOTE_INTERACTIVE_CHECKS=1` and an
agreed test period. The physical LAN input harness remains separately opt-in.

See the [check record](../tests/results/native-ui-2026-10-10.txt). Next: restore
native Mac verification, finish reverse discovery and session-owned clipboard/files,
then perform scheduled two-device acceptance before packaging v0.3.0.

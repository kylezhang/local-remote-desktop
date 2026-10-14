# Mac screen access

[简体中文](zh-CN/mac-screen-access.md) · [Mac guide](macos.md)

## Why an enabled permission can still fail

The owner's downloaded alpha.2 package could control Windows, but accepting a
connection kept requesting screen permission. Read-only diagnostics on 2026-10-14
confirmed a stale global ScreenCapture authorization: TCC matched an older local
build's code requirement while the downloaded package ran through App Translocation.
TCC explicitly reported a code-requirement mismatch. This is not evidence that
buying Apple Developer membership is required to implement screen sharing.
The downloaded package had the expected CI source revision and intact signature.

## Current implementation

On macOS 14+, deliberately starting **Accept connections** presents Apple's system
display picker. Select the main display. The app retains that authorized filter
in memory for the current run and passes it unchanged to ScreenCaptureKit; it does
not enumerate SCShareableContent again after selection. Reconnecting or changing
quality uses the same selected source. Stop closes the listener and stream; an
explicit later Start may reuse the selection while the app remains running.
Relaunching requires a fresh system selection. Selection cancellation/failure
does not read Keychain, start a listener or automatically request permission again.

The first phase still controls the primary display. macOS 15.2+ exposes the
selected display ID, which is checked before input. On 14–15.1 only an unambiguous
single-display setup is accepted. macOS 13 retains the normal global Screen
Recording permission path. No system privacy database or certificate trust is changed.

Accessibility is separate and still required for keyboard/mouse control. Allow
the installed application, not a similarly named older source-build copy. The
system picker grants capture of the selected display, not remote input permission.

Current development checks CGPreflightPostEventAccess for the actual CGEvent
posting operation, reports AX separately and refreshes when returning from Settings.
The deliberate control-permission action uses CGRequestPostEventAccess. If the
current installed copy is missing from the list, Locate current app reveals it
for manual addition. These changes do not prove the reported alpha.4 input failure
fixed on hardware; see [the check scope](../tests/results/input-access-startup-2026-10-14.md).

## Install the downloaded build

Prefer the DMG: open it and drag **Local Remote Desktop.app** to **Applications**.
Quit an older running copy before replacing it, then open the installed copy.
The ZIP also works; copy its complete app bundle to `/Applications` first.
Avoid starting an older source-build copy or the app directly inside Downloads.
No local compilation, automatic TCC reset or Gatekeeper disabling is required.
An unnotarized application may still need the normal macOS first-open approval.

## Verification

Automated checks cover denial, cancellation, duplicate Start, no credentials before
selection, retry and cached same-run restart using recording screen services.
CI compiles the native picker and full app. Native system selection, accepted
frames, input and actual permission/upgrade behavior still require the owner's
agreed acceptance period; compilation alone is not that evidence.

References: [Apple's system-picker permission model](https://developer.apple.com/videos/play/wwdc2023/10053/),
[ScreenCaptureKit picker integration](https://developer.apple.com/videos/play/wwdc2023/10136/),
[ad-hoc identity requirements](https://developer.apple.com/documentation/technotes/tn3127-inside-code-signing-requirements).

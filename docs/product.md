# Product direction

[简体中文](zh-CN/product.md) · [Version plan](roadmap.md) · [UI proposal](design.md)

## The problem

The project began with a personal office workflow: use a Windows desktop to work
on a Mac without moving between computers. Existing tools did not fit that daily
workflow: some felt awkward or outdated, while paid offerings were difficult to
justify for this simple personal need. AI-assisted development made it practical
to build a focused alternative.

## The product

**Local Remote Desktop is one application per platform for controlling another
device's current desktop over a local network.** Desktop installations can
connect to another device or explicitly allow connections to themselves. Host
and controller are session roles, not separate products users must understand.

Open the app, choose a device, pair once and work. Returning connections should
reuse securely remembered credentials. Discovery helps find a device; it never
authorizes access. Remote input, familiar shortcuts, text and regular-file
clipboard sharing should form one coherent workflow in an authorized session.
Returning to the controlled computer continues the same working session.

## Principles that guide scope

- LAN direct connections; no account, central service, relay or public traversal.
- Explicit sharing and pairing, a visible local stop action and clear revocation.
- Remote screen first; compact controls, no permanent credential bar during use.
- Shared vocabulary and flows across platforms, adapted to native controls,
  keyboard conventions and touch interaction.
- Routine use should not require repeated password entry, permission resets or
  manual cleanup. Installation and upgrade reliability are product work.
- Features are described by verified capability, not merely by available source.
- Background development checks are the default. Tests that capture foreground
  focus or inject real input require an agreed test period and a dedicated target.
  Do not interrupt the user's work with unsolicited test windows.

## Destination and boundaries

Windows ↔ macOS is the next desktop milestone. Linux follows as a desktop role;
Android, iOS/iPadOS and HarmonyOS initially control authorized desktops. Platform
capability determines available actions: a mobile app is not a promise that the
phone itself can accept unattended remote control. Screen sharing and receiving
input are assessed separately; controller-first milestones do not permanently
exclude mobile sharing. See the versioned roadmap and
[mobile feasibility note](mobile-sharing.md).

The product shares the current logged-in desktop. FileVault preboot, universal
login/lock-screen control, secure desktop bypass and new remote login sessions
are outside the promise. Required system permissions cannot be silently granted.
SSH and CI are development tools, never normal user prerequisites.

## What success looks like

An ordinary user installs an app on each supported device, enables sharing on
the target and pairs from the other device. Later they select the remembered
device and connect. Keyboard, mouse, clipboard, transfers, quality changes and
reconnection work predictably; disconnect releases held input and leaves the
target's work intact. This is tested on real supported systems before release.

# Nearby devices

[简体中文](zh-CN/discovery.md)

Development builds publish a device only after **Share this device** starts a
listener. Windows publishes through the built-in Windows DNS-SD service; Mac uses
Bonjour. Windows can discover compatible Mac and Windows sharing services. The
Mac controller currently offers Windows targets only; Mac-to-Mac control is not
implemented. Selecting a row never connects or authorizes control.

Use **Connect → Nearby devices** or **Enter Target IP**. Names come from the
sharing app's **This device’s name**, which defaults to its system hostname. The
OS label comes from the app's platform metadata. A bare IP does not identify a
name or OS; manual targets display those details after authenticated connection.
Names are display labels, never credential identities.

## Publication and scanning

Both platforms use `_localremote._tcp.local`, TCP 47980 and TXT `protocol=1`,
`platform=macos|windows`, `deviceName`. No password, account or clipboard data is
advertised. Names are visible to the LAN while sharing. Renaming republishes
without closing the listener or changing credentials. Stopping sharing or
replacing its password withdraws publication. Discovery failure leaves manual
IP connections available.

Windows uses the operating system's asynchronous
[DNS-SD registration API](https://learn.microsoft.com/en-us/windows/win32/api/windns/nf-windns-dnsserviceregister).
Registration is owned by the app process; exit removes it. Renames coalesce to
one pending native operation. Stop/Exit waits at most five seconds for DNS cleanup,
independently of closing the desktop listener. Pending native memory is retained
until the OS callback, never freed prematurely. No permanent discovery polling
or additional Bonjour installation is needed.

Windows scans for three seconds on opening and explicit refresh, using at most
four Ethernet/Wi-Fi IPv4 interfaces, 32 results and bounded DNS follow-up queries.
Its [RFC 6762 one-shot queries](https://www.rfc-editor.org/rfc/rfc6762.html#section-5.1)
use an ephemeral port, not another listener on UDP 5353. Own local addresses are
excluded. Mac uses a five-second main-runloop Bonjour scan, at most 32 resolved
services and three seconds per resolve. Refresh replaces the previous generation;
leaving Connect or beginning control stops browsing. No firewall/DNS policy is
changed. IPv6 discovery is not implemented.

## Recent devices and pairing

Authenticated peers have one record per address. Windows retains metadata during
the current process; Mac retains up to 32 nonsecret recent records across app
starts. Rows distinguish **Found nearby** from **Recent connection · Not found
this time**. Recent records are not proof that sharing is enabled. Refresh or
retry through Target IP when multicast discovery is unavailable.

Returning Mac connections may explicitly load a remembered target password from
Keychain; opening the app, selecting rows and refreshing never reads it. A typed
password can be saved after successful authentication. Storage failure does not
end the connection, and unchecking Remember password skips this save without
deleting older credentials. See [Mac controller](mac-controller.md). Existing
Windows DPAPI credential memory remains in place. Editable names, discovery
records and history never replace authenticated identity checks.

## Verification

Windows native DNS instance construction, registration/completion, deregistration
and freeing passed with temporary fixture metadata. Independent UDP loopback
checks cover Windows/Mac platform records, compressed/malformed DNS, filtering,
query bounds and cancellation. Publication lifecycle doubles cover coalescing,
late completion and failure without polling. Unshown WPF checks cover selection,
shared-name updates, opt-in publication and withdrawal during owner shutdown.

Production Swift discovery/model code is compiled with explicit browser, session
and credential doubles. These are not native Bonjour, Keychain or Apple SDK
acceptance. **The updated Mac app and actual two-device multicast/input acceptance
remain pending.** The Mac is outside this LAN; SSH attempts are stopped. See
[the check record](../tests/results/discovery-pairing-2026-10-11.txt).

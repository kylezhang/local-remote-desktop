# Nearby devices

[简体中文](zh-CN/discovery.md)

Development builds advertise the Mac with Bonjour/DNS-SD only after desktop
sharing starts. In Windows, open the arrow inside **Target IP** to choose a device.
Selecting fills the address and restores only that address's remembered password;
it does not connect, pair, authorize control or overwrite a manually typed address
merely because a device appeared. Manual IPv4 entry remains available.

Windows performs a three-second search once on opening and on explicit refresh.
There is no permanent discovery polling loop. At most four active Ethernet/Wi-Fi
IPv4 interfaces, 32 results and bounded DNS records/follow-up queries are used.
The current resolver offers only compatible Mac services on TCP 47980. Discovery
names and addresses are untrusted; the existing authenticated connection is still
required. A stale result cannot bypass pairing.

Mac publishes `_localremote._tcp.local` with TXT `protocol=1`, `platform=macos`
and `deviceName`. The app name defaults to its system hostname and can be changed
under Share this device → This device’s name. Windows prefers this bounded TXT
name and falls back to the legacy DNS-SD service name. No password, key, account
or clipboard data is advertised. The display name is visible to the LAN while
sharing; rename refreshes publication without disconnecting. Stop or replace its password and
the old publication stops; a restarted listener publishes a new service.

Manual IP does not reveal a name or platform by itself. Once paired, the sharing
app returns authenticated `deviceName`/`platform`; heartbeat updates the name.
Windows keeps one record per address in memory across searches, without treating
it as a newly discovered or confirmed online device. Older peers without a name
keep the known name or IP. Passwords remain associated with the address, not this
editable label. Windows announcement and Mac browsing are not implemented yet.

Windows uses [RFC 6762 one-shot/legacy unicast queries](https://www.rfc-editor.org/rfc/rfc6762.html#section-5.1):
send a PTR query from an ephemeral local port to IPv4 multicast 224.0.0.251:5353,
then resolve SRV/TXT/A records. It neither takes over UDP 5353 nor changes DNS or
firewall rules. Allow local-network access if macOS asks. Wi-Fi client isolation,
firewall rules or multicast filtering can prevent discovery; use the known IP
instead. IPv6 discovery is not implemented.

Automated Windows tests cover malformed/compressed DNS, port/platform/LAN filters,
split packets, real loopback UDP, bounded queries and cancellation. WPF tests cover
selection, manual fallback, narrow layout and no automatic trust. Swift production
publication lifecycle is checked with a NetService double. **Native Mac Bonjour,
local-network permission and physical multicast acceptance remain unverified.**

# LocalRemote protocol (RML1 / RMP1)

[简体中文](zh-CN/protocol.md)

TCP 47980, IPv4, one controller. Integers are big-endian. Legacy RML1 uses a 32-byte random PSK shown as 64 hex characters. Development password mode RMP1 derives a 32-byte PSK as specified below. Maximum encrypted body: Host→Client 8 MiB, Client→Host 64 KiB. LAN/private, link-local and loopback IPv4 are supported; not public internet traversal.

## Authentication and framing

1. Host sends ASCII `RML1` and random 32-byte S.
2. Client sends random 32-byte C and HMAC-SHA256(PSK, `client` || S || C).
3. Host verifies and sends HMAC-SHA256(PSK, `server` || S || C); Client verifies with a fixed-time comparison.
4. Derive c2h = HMAC-SHA256(PSK, `c2h` || S || C), h2c similarly with `h2c`.

### Password authentication (RMP1, development)

Manual passwords are 8–64 printable ASCII characters excluding spaces (bytes
33–126), case-sensitive. New credentials default to 12 uniformly random characters
from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (60 bits). Existing raw 32-byte credentials
stay RML1 until explicitly replaced. A 64-character hex password can be used in
RMP1; the Host's magic selects the mode, not the password's appearance.

Host stores a unique random 16-byte salt and the password in its existing Keychain
item. Its versioned value is ASCII `LOCALREMOTE-PAIRING-PASSWORD-V1:` followed by
the salt and UTF-8 password; a raw 32-byte value remains the legacy format. Corrupt
records fail without automatic regeneration. Explicit save updates the same item,
disconnects existing controllers and replaces the in-memory credential.

Derive PSK with PBKDF2-HMAC-SHA256(password, salt, 600000 iterations, 32 bytes).
Host sends `RMP1` || random S (32 bytes) || salt (16 bytes). Define transcript as
`RMP1` || salt || S || C. Client sends C (32 bytes) plus
HMAC-SHA256(PSK, `client` || transcript); Host replies with
HMAC-SHA256(PSK, `server` || transcript). Direction keys use `c2h`/`h2c` and this
same transcript. Windows derives off the UI thread; Mac derives on its credential
queue and caches the result. AES-GCM records and application version=1 are unchanged.
This mode is also used on the file socket with the same stored salt and fresh S/C.
Both apps need the development update to use RMP1; new clients retain RML1 support.

The iteration choice follows [OWASP password-storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).
It slows guesses; it does not turn a weak password into a random 256-bit secret.
A captured authentication exchange permits offline password guessing. Prefer a
generated password and a trusted LAN; there is no PAKE or forward secrecy.

Each encrypted record is u32 body length + ciphertext + 16-byte GCM tag. Plaintext is one type byte plus payload. AES-256-GCM nonce: four zero bytes + direction counter u64, starting at zero. The length header is AAD. Separate direction keys/counters and a serialized writer preserve nonce/order. Fresh handshake nonces create new keys on reconnect. Invalid authentication, lengths, tags, unexpected counter/order or EOF terminate the connection. There is no unauthenticated development-control mode.

Type 1 is UTF-8 JSON. Type 2 is one Annex B H.264 access unit, without timestamps or B frames; decode in receive order. No UDP fragmentation.

| Operation | Direction | Fields |
|---|---|---|
| handshake | C→H, first encrypted record | version=1, quality=0/1/2 |
| video_header | H→C, before frames | width, height, fps, codec=h264-annexb |
| metrics | H→C | encodeMs; optional cumulative captureSkipped |
| mouse_move | C→H | x,y normalized 0..1 |
| mouse_button | C→H | button=0/1/2 left/right/middle, down, x,y |
| scroll | C→H | delta, Windows 120 units mapped to pixels, bounded ±1200 |
| key_event | C→H | Windows virtual key vk, down, flags |
| release_all | C→H | release held keys/buttons |
| heartbeat | both | Client every 3 seconds, Host responds; 15-second timeout |
| capabilities | H→C | accessibility bool; also reported in newer heartbeats |
| clipboard_sync | C→H request / H→C acknowledgement | enabled bool, requestId positive monotonic integer |
| clipboard_text | both, only while mutually enabled | requestId, encoding=utf8-base64url, text=canonical padded base64url of UTF-8 |
| disconnect | C→H | normal disconnect; EOF also closes |

Flags: Shift=1, Control=2, Option=4, Command=8. Includes left/right modifier VK mapping and numeric-pad VK 96..111 with CGEvent numeric-pad flag. Num Lock-off navigation is not converted into digits. No Windows Unicode IME composition; Mac input methods can process physical events. Unknown operations are ignored; malformed JSON closes; non-finite coordinates are rejected.

Heartbeat input diagnostics are cumulative for the current connection: inputReceived (recognized messages), inputPosted (CGEvent post calls), inputBlocked (permission denied), inputRejected (invalid/unsupported or event creation failure), inputError (latest non-content error code). Release-all may post multiple events. Posted is not confirmation of application acceptance. Diagnostics omit key content and pairing secrets.

Quality 0 requests up to 1280×720/30fps/4Mbps; 1 up to 1920×1080/60fps/10Mbps; 2 same size/fps at 18Mbps. Preserve aspect ratio, even dimensions, no upscaling. Changing quality releases input, closes stream/decoder and reauthenticates after roughly three seconds with the latest selected quality, independently of Auto Reconnect. Settings are requests, not guarantees.

## Optional text clipboard

The Host advertises clipboardText=true in capabilities. Client consent needs a
matching clipboard_sync acknowledgement with enabled=true and its own toggle on.
The Host also needs its local Allow Clipboard toggle; changes produce an ack with
the current requestId. New request IDs strictly increase per connection. Stale IDs
and disabled-session text are ignored. Old peers ignore unknown operations; magic,
version and encrypted record limits remain unchanged.

UTF-8 text is limited to 32 KiB, encoded with padded base64url (-/_ instead of +/)
to prevent JSON escaping inflating the record past 64 KiB. Invalid UTF-8, noncanonical
encoding, NUL and oversize data are rejected; empty text is allowed. No files/images/
HTML. Existing clipboard contents are not sent on enable/reconnect. Received writes
advance the observed clipboard revision to suppress echo; polling is 500 ms, busy
writes retain at most one text for two seconds, and a newer local copy cancels it.
Clipboard and paste input share one ordered client queue; the Host processes text
before the next input event. Busy writes can still prevent immediate paste readiness.

## Threat limits

Initial key transfer needs a trusted out-of-band path. Mac Keychain and current-user Windows DPAPI store the long-term key. No independent audit, forward secrecy, certificate PKI, comprehensive denial-of-service protection. A known PSK plus a recorded handshake can decrypt historical sessions. An unauthenticated peer may occupy the single slot until the 15-second timeout. Use only on a trusted LAN; do not expose the port publicly.

## Optional file clipboard

The desktop capabilities record adds fileClipboard=true, filePort=47981 and
fileSession=64 hex characters generated fresh for this desktop session. Client
sends file_sync with enabled and a positive increasing requestId; Host acknowledges
the same ID only when both peers allow files. Revocation acknowledges false and
aborts active jobs. File consent is separate from text consent and remembered on
each device, but enable/reconnect takes a new clipboard baseline.

On a new Mac file copy, Host sends file_offer {requestId,offer,items}; offer is
32 hex characters identifying the latest source batch. Items are {name,size}
without source paths. Windows file copies start a push instead.

Windows always initiates TCP 47981, including Mac-to-Windows pulls. It repeats
RML1 or RMP1 challenge/proof and direction-key derivation automatically with fresh
nonces, then sends a type-1 request:

- file_push {session: fileSession,requestId,items}
- file_pull {session: fileSession,requestId,offer}

Host requires the active desktop token, matching enabled requestId and local
consent; only one file connection is accepted. Old tokens and revoked generations
are rejected. Host replies file_ready (with items for pulls). Pull metadata must
match the offered metadata exactly. These are background checks, not a second
manual pairing/password step.

For each ordered item, sender emits file_start {index}, zero or more **type-3 raw
file chunks**, then file_end {index,sha256:64 lowercase hex}. Type 3 is used only
on the file socket. Chunks are nonempty and at most 32 KiB, lengths match declarations.
Then file_complete terminates the batch. Receiver validates lengths and SHA-256
before promoting its generated staging directory. For pushes, Host replies
file_done with clipboardReady. For pulls, Client commits, then attempts Windows
clipboard publication on the UI thread (up to two seconds of busy retries, without
overriding a newer copy), and replies file_done with clipboardReady. Host reports
paste-ready only for explicit true; missing/false from legacy or headless peers
confirms transfer completion without claiming clipboard readiness.

Both directions cap encrypted file records at 64 KiB, metadata at 32 items,
each file at 4 GiB and total at 8 GiB. NFC portable basenames are at most 240 UTF-8
bytes and unique ignoring case; separators/device names and trailing dots/spaces
are rejected. Sources are regular files. Cancel uses connection closure. Send
pacing uses cumulative monotonic time at 8 MiB/s with a 512 KiB initial allowance; receive inactivity is 15 seconds, jobs
one hour. Committed files are retained; uncommitted files are removed during normal
cleanup. There is no automatic source deletion or arbitrary remote filesystem API.

Authenticated Host failures may send file_error {code}; stable codes are invalid,
changed, incomplete, checksum, denied, diskFull and io. No source paths or secrets
are included. Connection loss can prevent error delivery.

This is LocalRemote's extension, not LocalSend protocol compatibility. See the
[file guide](files.md) for clipboard races, storage and current limitations.

# LocalRemote protocol (RML1)

[简体中文](zh-CN/protocol.md)

TCP 47980, IPv4, one controller. Integers are big-endian. A PSK is 32 random bytes shown as 64 hex characters. Maximum encrypted body: Host→Client 8 MiB, Client→Host 64 KiB. LAN/private, link-local and loopback IPv4 are supported; not public internet traversal.

## Authentication and framing

1. Host sends ASCII `RML1` and random 32-byte S.
2. Client sends random 32-byte C and HMAC-SHA256(PSK, `client` || S || C).
3. Host verifies and sends HMAC-SHA256(PSK, `server` || S || C); Client verifies with a fixed-time comparison.
4. Derive c2h = HMAC-SHA256(PSK, `c2h` || S || C), h2c similarly with `h2c`.

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
| disconnect | C→H | normal disconnect; EOF also closes |

Flags: Shift=1, Control=2, Option=4, Command=8. Includes left/right modifier VK mapping and numeric-pad VK 96..111 with CGEvent numeric-pad flag. Num Lock-off navigation is not converted into digits. No Windows Unicode IME composition; Mac input methods can process physical events. Unknown operations are ignored; malformed JSON closes; non-finite coordinates are rejected.

Heartbeat input diagnostics are cumulative for the current connection: inputReceived (recognized messages), inputPosted (CGEvent post calls), inputBlocked (permission denied), inputRejected (invalid/unsupported or event creation failure), inputError (latest non-content error code). Release-all may post multiple events. Posted is not confirmation of application acceptance. Diagnostics omit key content and pairing secrets.

Quality 0 requests up to 1280×720/30fps/4Mbps; 1 up to 1920×1080/60fps/10Mbps; 2 same size/fps at 18Mbps. Preserve aspect ratio, even dimensions, no upscaling. Changing quality releases input, closes stream/decoder and reauthenticates after roughly three seconds with the latest selected quality, independently of Auto Reconnect. Settings are requests, not guarantees.

## Threat limits

Initial key transfer needs a trusted out-of-band path. Mac Keychain and current-user Windows DPAPI store the long-term key. No independent audit, forward secrecy, certificate PKI, key-revocation UI or comprehensive denial-of-service protection. A known PSK plus a recorded handshake can decrypt historical sessions. An unauthenticated peer may occupy the single slot until the 15-second timeout. Use only on a trusted LAN; do not expose the port publicly.

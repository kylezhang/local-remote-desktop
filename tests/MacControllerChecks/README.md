# Mac controller checks

These checks never read real pairing credentials or inject system input.
Fixture credentials, loopback sockets and isolated UserDefaults are used.

```sh
bash tests/MacControllerChecks/check.sh
bash tests/MacControllerChecks/protocol.sh
```

Requires Swift 5.9+, Python 3 with `cryptography`. Linux additionally requires a C
compiler and OpenSSL development libraries for the test-only CryptoKit/CommonCrypto
adapters. On Mac, wire tests use native CryptoKit/CommonCrypto; `check.sh` also builds
the full application using the installed Apple SDK. A full native build is required
before calling the Mac viewer verified.

- **Core:** LAN/capability/video bounds, fitted coordinates, ordinary/keypad keys,
  Command/Control/native mapping, modifier edges, bounded/coalesced outbox, release
  priority, Annex B/AVCC framing and malformed video.
- **Model:** production ControllerModel compiled with service/property/image
  doubles: exclusive roles, gated input, 10,000-frame latest-only presentation,
  address-only persistence, stale callbacks, auth/initial failures, cancel/reconnect,
  rapid quality changes and quality restart with automatic reconnect disabled.
- **Wire:** production Wire controller against an independent Python AES-GCM peer;
  RMP1/RML1, fragmented records, 100 KiB video, ordered input, invalid host proof,
  replay and oversized record rejection.
- **Session:** production ControllerSession against the same independent peer;
  duplicate start prevented, authenticated capability gate, writer order, heartbeat,
  interruptible stop and one completion callback. The decoder adapter only checks
  H.264 framing; it does not emulate VideoToolbox or test native rendering.

The production model body is extracted only into a disposable test file; source
files are unchanged. Linux wire/session copies adapt Darwin sockets and secure
randomness to Linux APIs, with real OpenSSL HMAC/AES-GCM/PBKDF2. Passing these checks
is not a native AppKit/SwiftUI/VideoToolbox compile or real-device acceptance.

中文：检查使用隔离凭据、回环网络和 UserDefaults，不注入系统输入。
测试覆盖生产 Swift 规则、状态、传输和会话逻辑；Linux 平台适配与界面/解码替身
不代替 Apple SDK 构建、真实画面或 Mac → Windows 键鼠验收。

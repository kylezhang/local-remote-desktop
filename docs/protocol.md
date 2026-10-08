# RML1 协议

固定 TCP 47980，IPv4，单一控制端。整数为大端。安全密钥是 32 字节随机 PSK，UI 64 字符 hex。以下字符串均 ASCII，连接前没有 JSON。最大 Host→Client 报文 8MiB，Client→Host 64KiB。

1. Host → Client：`RML1` (4 bytes) + S (32 random bytes)。
2. Client → Host：C (32 random bytes) + HMAC-SHA256(PSK, `client` || S || C)。
3. Host 验证后 → Client：HMAC-SHA256(PSK, `server` || S || C)。Client 固定时间比较。
4. c2h key = HMAC-SHA256(PSK, `c2h` || S || C)，h2c key 同理 `h2c`。每条连接双方随机数都重新生成。

后续每条报文：长度 u32（不含长度头，含 GCM tag）+ ciphertext + 16 字节 tag。明文 = type u8 + payload。AES-256-GCM nonce = 四个 0 字节 + 方向计数器 u64（从 0 开始）；长度头作为 AAD。两个方向有独立 key/counter；单发送锁保证密文次序。鉴权失败、计数器错序、长度错误或 EOF 立即断开。计数器在连接重建时归零，密钥随随机 transcript 改变。

Type 1 = UTF-8 JSON，Type 2 = 一个 Annex B H.264 access unit，无额外时间戳。无 B 帧；按接收次序显示。视频头是 JSON。没有 UDP 分片。

| op | 方向 | 字段 |
|---|---|---|
| handshake | C→H，第一个加密消息 | version=1，quality=0/1/2 |
| video_header | H→C，在视频前 | width,height,fps,codec="h264-annexb" |
| metrics | H→C | encodeMs（回调耗时均值） |
| mouse_move | C→H | x,y（0..1 主显示器归一化） |
| mouse_button | C→H | button=0左/1右/2中，down bool，x,y |
| scroll | C→H | delta，Windows 120 单位映射像素，限制 ±1200 |
| key_event | C→H | vk Windows virtual key，down bool，flags bitmask |
| release_all | C→H | 无，失焦时释放按键 |
| heartbeat | 双向 | Client 每 3 秒发送，Host 回复；15 秒超时断线 |
| disconnect | C→H | 正常关闭；客户端也可 EOF 关闭 |

flags：1 Shift、2 Control、4 Option、8 Command。vk 映射包含 Windows 左右修饰键；不支持 Unicode 文本/IME。未知 op 忽略，畸形 JSON 或加密报文断开。鼠标 x/y NaN/Infinity 不接受。

质量 0：最多1280×720/30fps/4Mbps；1：最多1920×1080/60fps/10Mbps；2：同尺寸频率/18Mbps。保持屏幕比例、偶数尺寸，不放大低分辨率。切档需重新连接。编码设置是请求值，码率与帧率不是保证值。

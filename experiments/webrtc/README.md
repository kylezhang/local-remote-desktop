# 局域网 WebRTC 画面对比实验

独立、只读的实验，用于评估现有 TCP/FFmpeg/WPF 画面链路是否值得迁移。没有替换正式客户端，也没有实现远程键盘鼠标。现有无法输入的问题仍须排查 Accessibility、输入转发和 CGEvent；更换视频传输不能自动解决这些问题。

## 最短运行步骤

1. Mac 打开原有 RemoteMacLAN Host，点击开始共享，并取得配对密钥。先断开原 Windows 客户端：现有 Host 同时只支持一个连接。
2. Mac 下载实验的 Apple Silicon 可执行文件。在文件所在目录运行以下两条命令，然后在终端的隐藏输入提示中粘贴**原 Host 的配对密钥**：

```sh
chmod +x rtc-experiment-macos-arm64
./rtc-experiment-macos-arm64 -mode host -quality 0
```

3. Windows 运行 `rtc-experiment-windows.exe`（默认 viewer 模式），浏览器打开 `http://127.0.0.1:47982`，填写 Mac 局域网 IPv4 和同一个配对密钥，点击连接。使用支持 H.264 WebRTC 的 Chromium 浏览器。

Mac 桥接程序只连接本机 `127.0.0.1:47980` 获取原 Host 的 H.264。它不采集屏幕，不需要额外屏幕录制授权；原 Host 的权限依然必须有效。如果 macOS 提示阻止未签名二进制，请仅在确认文件来源后按系统提示允许；本实验没有 Apple 开发者签名或公证。出现防火墙提示时允许可信局域网连接。实验使用 Mac TCP 47981（信令）及 UDP 47990–48010（视频），Windows 本地页面 TCP 47982。

退出桥接终端用 Ctrl+C，退出 Windows viewer 用 Ctrl+C 或关闭其控制台。页面“断开”仅断开视频。没有自动重连。

## 从源码构建

需要 Go 1.26 或更新版本；Mac 原 Host 仍按原项目说明构建。桥接程序是用于对比的 Go/Pion 辅助工具，不是重写 Swift Host。

```sh
cd experiments/webrtc
go build -trimpath -o rtc-experiment .
./rtc-experiment -mode host -quality 0
```

Windows 可运行 `go build -trimpath -o rtc-experiment-windows.exe .`，再运行生成文件。浏览器和 Go 程序均不需要 FFmpeg；原 TCP 客户端仍需要它。`-quality 0` 为 720p30；1、2 为原 Host 的标准/高清 1080p60 上限，实际 FPS 取决于采集和网络。

## 架构与安全

Mac Swift Host → 本机原协议加密 TCP → Pion 桥接 → 局域网 WebRTC H.264 → Windows 浏览器。没有重新编码，Windows 页面由本机 viewer 提供，SDP 信令通过 viewer 转发到 Mac。

不配置公网 STUN/TURN，只收集本机私有 IPv4、loopback 和 link-local 候选；ICE 仍会在两端之间执行连接检查。没有中继，隔离访客网、禁止设备互访或阻断 UDP 的网络可能无法连接。没有跨公网的可用性承诺。

信令使用原配对密钥、一次性 30 秒挑战、按方向派生的 AES-GCM 密钥加密并认证 SDP（包含 DTLS 指纹）；视频由 WebRTC DTLS/SRTP 保护。没有匿名控制或匿名视频连接。Windows 本地服务检查 Host、Origin 和 JSON 内容类型。密钥不写日志、命令行或配置文件，仅保存在进程/页面内存，浏览器成功协商后清空密码框。与原协议一样，32 字节随机配对密钥不是用户登录密码。

采用 Pion 默认编解码协商及 RTCP 拦截器；原 VideoToolbox 编码仍为固定画质。没有完整的带宽自适应控制，也未把 PLI 反馈接入原 Host 强制关键帧接口。因此不能把本实验等同于完整 libwebrtc 的拥塞控制，Wi-Fi 丢包后的恢复需实测。

## 已验证与未验证

2026-10-08：Windows 本机合成测试图经过真实 ICE/DTLS/SRTP/UDP，在内置 Chromium 浏览器完成 H.264 播放。约 355 秒采样，1280×720，平均 30.00fps、7.65Mbps、平均解码 0.90ms；末次统计丢包/丢帧为 0，候选 host ↔ host、UDP。这是同机测试，不是实际 Mac 桌面，不代表跨设备延迟、CPU 或硬件解码性能。

Windows 和 macOS arm64 二进制均完成构建。Go 认证、方向隔离、挑战过期/重放、局域网地址过滤、本地页面跨来源保护测试通过；Pion 接收合成视频 RTP 的集成测试通过。**macOS 二进制尚未在 Mac 执行；与真实 Swift Host 的桥接、Mac → Windows 跨机视频和各端 CPU 尚未验证。**浏览器决定是否使用硬件解码，不能保证。

`go test ./...` 执行基础检查。设置 `RML_RTC_SAMPLE_PATH` 为带 AUD 的 Annex-B H.264 测试文件可执行 UDP 视频集成测试；没有设置时该测试明确跳过。可用 FFmpeg 的 testsrc2 生成测试图文件，这不是录屏。

## 公平对比方式

两个客户端均选流畅 720p30，同一 Mac 桌面、网络和操作，逐一运行，预热 15 秒后记录至少 60 秒。WebRTC 页面可导出 FPS、码率、解码时间、抖动缓冲、RTT、丢包/丢帧 JSON。解码时间和 RTT **不是**屏幕采集到显示的完整延迟。

CPU 对比需计入原客户端的 WPF+FFmpeg，以及实验的浏览器+viewer；Mac 实验侧还要计入桥接程序。端到端延迟可用手机同时拍摄 Mac 和 Windows 上相同的运动画面/计时器比较。只有跨机结果证明改善后，才决定是否迁移正式视频链路。

仍只共享已登录用户会话，不支持 FileVault 预启动解锁；本实验没有改变锁屏或登录前的控制能力。

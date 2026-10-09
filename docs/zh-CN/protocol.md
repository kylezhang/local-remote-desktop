# LocalRemote 协议（RML1）

[English / 完整字段规范](../protocol.md)

固定 TCP 47980、IPv4、单控制端，整数大端。PSK 32 随机字节，显示为 64 位 hex。加密包体上限 Host→Client 8MiB、反向 64KiB。

Host 发 RML1 与随机 S，Client 发随机 C 及 HMAC-SHA256(PSK, client || S || C)，Host 验证后回 server 标签的 HMAC；双方用 c2h/h2c 标签及随机 transcript 派生独立方向密钥。每包是 u32 长度、密文、16 字节 GCM tag，明文含类型字节及载荷。nonce 为四个零字节加方向 u64 计数器，长度作 AAD；认证、长度、tag、顺序或 EOF 错误断开。无未认证控制模式。

类型 1 是 JSON，类型 2 是无时间戳的 Annex B H.264 access unit。操作包括 handshake、video_header、metrics、mouse_move、mouse_button、scroll、key_event、release_all、heartbeat、capabilities、disconnect；完整字段表以英文规范为准。心跳每三秒，15 秒超时。修饰位 Shift=1、Control=2、Option=4、Command=8。数字键盘设置 numeric-pad 标记，不强制把 Num Lock 关闭后的导航改为数字。Windows IME 组合文本不传输，Mac 输入法可处理物理事件。

诊断累计记录收到、post、权限阻止、无效事件及不含内容的错误代码；post 不表示目标应用确认执行，release_all 可产生多个事件。画质切换释放输入后关闭流及解码器，约三秒重新认证，以最后档位握手，不依赖自动重连选项。

新 Host 以 capabilities.clipboardText=true 声明支持可选文本同步。Client 发 clipboard_sync（enabled、递增正整数 requestId），
Host 在自身允许且 Client 请求时确认；本地允许项变化后用当前 requestId 回报。clipboard_text 双向携带同一 requestId、
encoding=utf8-base64url、text；关闭、旧 ID 或非法载荷忽略。RML1/version=1 及包体上限不变，旧端忽略未知操作。
纯文本最多 32 KiB UTF-8，使用带 padding 的 base64url（-/_），避免 JSON 转义膨胀超过反向 64 KiB 包体。
拒绝非法 UTF-8、非规范编码、NUL 和超长文本，允许空文本，不传图片/文件/HTML。开启和重连不传已有内容，收到文本后更新
本地观察版本以防回环。每 500 毫秒检查；忙时最多保留一条文本重试两秒，新本地复制取消待写。文本在粘贴按键前经同一 Client
队列发送，Host 处理后再处理下一输入；剪贴板忙时不能保证立即可粘贴。

首次密钥需可信渠道传递；存储使用 Keychain/DPAPI。没有独立审计、前向保密、证书体系、撤销界面或完整防拒绝服务。已知长期密钥及握手可解密历史流量；未认证连接可占用单连接槽直至超时。仅可信局域网，不公开端口。

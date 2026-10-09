# LocalRemote 协议（RML1 / RMP1）

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

首次密钥需可信渠道传递；存储使用 Keychain/DPAPI。没有独立审计、前向保密、证书体系或完整防拒绝服务。已知长期密钥及握手可解密历史流量；未认证连接可占用单连接槽直至超时。仅可信局域网，不公开端口。

## 短密码认证（RMP1，开发更新）

手填 8–64 位可打印 ASCII（33–126），区分大小写、不含空格；新记录默认生成 12 位随机字符，
字符集 `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`，共 60 位随机熵。原始 32 字节旧密钥保持 RML1，直到用户主动保存。
新 Windows 依据 Host 的 magic 选择模式，不根据输入是否像 hex 判断；64 位 hex 也可作为 RMP1 密码。

原有钥匙串项存储 `LOCALREMOTE-PAIRING-PASSWORD-V1:` 标记、16 字节随机 salt、UTF-8 密码。
PBKDF2-HMAC-SHA256 以 600000 次迭代派生 32 字节 PSK。Host 发 `RMP1 || S(32) || salt(16)`；
transcript 是 `RMP1 || salt || S || C`。Client 发 `C(32)` 和 client 标签的 HMAC，Host 回 server 标签 HMAC；
c2h/h2c 用相同 transcript。加密帧及业务 version=1 不变，文件连接复用相同 salt、使用新随机 S/C。
Mac 后台派生并缓存，Windows 后台派生。显式保存更新同一个钥匙串项、断开旧连接；损坏记录不会自动重置。

迭代只增加猜测成本：捕获握手仍可离线猜密码，没有 PAKE/前向保密。推荐自动生成随机密码，仅用于可信局域网。
两端更新后才能使用短密码；新 Windows 仍兼容旧 RML1。实现细节及来源见英文规范。

## 可选文件剪贴板

capabilities 声明 fileClipboard、filePort=47981 和每次桌面连接新生成的 fileSession（64 hex）。file_sync 独立于文本允许，使用递增 requestId 和匹配确认；撤销即关闭文件连接。选择会记住，但开启/重连不发送已有剪贴板。

Mac 新复制文件后发 file_offer（requestId、offer、items），offer 为最新来源批次的 32 hex 标识，items 只有 name/size，不含源路径。Windows 始终主动连接 TCP 47981，自动以原配对密钥执行 RML1 或 RMP1 双向证明及新方向密钥派生，然后发 file_push（session、requestId、items）或 file_pull（session、requestId、offer）。Host 校验当前桌面令牌、允许状态和 ID 后回复 file_ready，拉取时还返回匹配元数据。不增加人工认证。

按序传 file_start/index、类型 3 原始文件分块、file_end/index/sha256，最后 file_complete。类型 3 只用于文件连接；每块最多 32 KiB、加密包体最多 64 KiB。接收端校验长度及 SHA-256 后才提交本地生成目录。上传由 Host 回 file_done/clipboardReady，下载由 Client 提交后在 UI 线程尝试更新 Windows 剪贴板（忙时最多重试两秒），之后回 file_done/clipboardReady。Host 只有收到显式 true 才提示可粘贴；旧端缺少字段或 false 时仅表示传输完成。新本地复制优先；完成文件保留，未完成的正常清理删除。

最多 32 个普通文件，单个 4 GiB、合计 8 GiB；NFC 文件名最多 240 UTF-8 字节且忽略大小写不重名，拒绝路径分隔符、设备名、末尾点/空格。不支持文件夹/符号链接。按单调时间计算持续目标 8 MiB/s、起始额度 512 KiB，接收等待 15 秒、总任务一小时，取消关闭连接。不提供任意远程文件系统操作，不删除源文件。此为 LocalRemote 扩展，不兼容 LocalSend 协议；详见[文件指南](files.md)及英文完整规范。

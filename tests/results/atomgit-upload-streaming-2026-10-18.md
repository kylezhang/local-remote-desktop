# AtomGit release attachment streaming

2026-10-18. Focused publication-script correction while native session CI is
waiting for the existing source mirror. No application source, version, signing,
mirror configuration or credential is changed.

## Observed failure and correction

CI repeatedly timed out writing the missing alpha.5 Windows ZIP to storage.
The published GitHub asset is 75,750,677 bytes. The sync script previously read
the whole staged ZIP into one byte body; Python's HTTP sender writes such a body
with one sendall call, subject to its write timeout.

The PUT now opens the verified staged file as a binary stream. The standard HTTP
sender reads bounded blocks. Content-Length is explicit, preserving fixed-length
presigned uploads and the existing storage headers. Each retry reopens from zero;
ExitStack closes the descriptor after success, partial send or HTTP failure.
Conflicting length headers or simultaneous file/byte bodies are rejected before
network I/O.

This removes a demonstrated whole-body write risk; it does not establish the
exclusive cause of the real network timeout or prove the live upload recovered.
The existing request retry count/timeouts and workflow bounds remain.

## Evidence

All 26 AtomGitSyncChecks passed with Python 3.12 and an isolated temporary
directory. Existing tests use local mock APIs, not real release credentials.
Four new tests exercise actual stdlib HTTP serialization over a recording socket:

- A simulated limited write budget rejects the previous large-byte body while
  accepting the same file in bounded writes, with exact bytes/length/headers.
- A partial-send failure retries from position zero using a new file object;
  both descriptors close and the final body is complete.
- An empty file uses Content-Length 0 without chunked transfer encoding.
- Incorrect length or conflicting body sources fail before sending.

Existing end-to-end mock sync tests still verify GitHub tag/digests/provenance,
preflight of every existing attachment before writes, missing-only uploads,
upload/read-back hashes, idempotence, immutable conflicts, status policy and token
redaction. Release hygiene/documentation checks passed.

No live upload was invoked by these tests. AtomGit alpha.5's missing attachments
remain an open delivery item until cloud execution and read-back verification
succeed. This script does not publish the development UI under an old version.

中文：既有 CI 在上传 alpha.5 Windows ZIP 时反复写入超时，原实现将约 75.8 MB 文件作为
一个字节体写出。现改为已校验文件的流式固定长度 PUT，保留预签名头；每次重试重新打开
并从零读取，成功/失败均关闭文件。此修正消除整块写入的已复现风险，不声称已确定真实网络
超时的唯一原因或已恢复线上上传。26 项模拟检查通过，保留重复同步、冲突保护、
来源与回读哈希验证。未改镜像、应用版本/签名，未使用真实发布凭据；线上附件齐全仍待验证。

# File copy and paste (development)

[简体中文](zh-CN/files.md)

File clipboard sharing is implemented on the development branch. It is not in the
published v0.1.0 package. Windows encrypted-peer/UI tests and portable Swift tests
pass; native Mac and real two-device acceptance remain pending.

## Use it

1. Update both apps and connect normally using the existing pairing key.
2. On Mac, enable **允许双向文件复制粘贴**. On Windows, enable
   **同步复制的文件**. Wait for the enabled status.
3. Copy files in Explorer or Finder. Transfer starts automatically. When the status
   says ready, paste in Finder on the Mac or Explorer on Windows.

Both apps remember the file-sharing choice. There is no additional password,
pairing dialog or per-file confirmation. Enabling or reconnecting does not send
old clipboard contents; copy again after the session is ready. The consent applies
to authenticated desktop sessions, not an unauthenticated LAN peer.

The Windows option **Windows 快捷键** maps Ctrl+C/V to Mac Command+C/V. Click the
remote picture before using those shortcuts there. For a Windows-to-Mac copy,
Ctrl+V before completion is consumed with a waiting status: it is not queued for
later execution against a possibly different focused Mac application. Paste
again after completion. Paste into the local Windows application is managed by
Windows itself; wait for the received-file status there too.

## Supported files and limits

- Regular files, including images, documents and archives. Selecting several files
  is supported; copying image pixels into the clipboard is a different format and
  is not supported yet.
- Up to 32 files per copy, 4 GiB per file and 8 GiB per batch.
- No folders, symbolic links, shortcuts with directory semantics, or virtual files
  supplied by an application without a real local path. Zip a folder to send it.
- Names must be portable between Windows and Mac: no path separators, Windows
  device names, trailing dots/spaces or duplicate names ignoring case; 240 UTF-8
  bytes maximum. Invalid batches fail clearly; files are never silently renamed.

Only one file job runs at a time. A new local copy can cancel it. Clipboard access
stays on the UI thread; disk, hashing and network work run in the background.
File contents use 32 KiB chunks rather than whole-file memory buffers. Traffic is
paced to about 4 MiB/s before overhead to leave room for the desktop; this is a
limit, not a measured throughput guarantee. A separate socket avoids inserting
file bodies into the keyboard/video queue, but both still share LAN bandwidth.

## Progress, cancellation and received files

Both apps show progress and have a cancel action. Cancel, disconnect or revoke
consent closes the file connection; failed/incomplete batches are removed during
normal cleanup. Operations have a 15-second receive timeout and a one-hour job
deadline. The Mac socket also bounds individual send waits to three seconds.
Files from completed batches are retained even if clipboard publication fails.

The receiver checks declared lengths and SHA-256 before promoting a unique staging
folder. It never overwrites another batch or follows a peer-supplied destination
path. A newer clipboard change during receipt is preserved. Clipboard-busy Windows
writes retry for up to two seconds. Received clipboard updates are not sent back.

Use **接收目录** on Windows or **打开接收目录** on Mac to open saved files:

| Platform | Location |
|---|---|
| Windows | `%LOCALAPPDATA%\LocalRemote\ReceivedFiles` |
| Mac | `~/Library/Application Support/LocalRemote/ReceivedFiles` |

Each batch has its own folder. Remove files there when no longer needed; retained
files consume disk space and are not automatically deleted. A crash/forced OS
termination can leave `.incoming-*` folders; after quitting LocalRemote on that
device, those incomplete folders may be removed. Completed batches remain usable.
Clipboard file drops represent **copy**, never a request to delete the source.

## Connection and privacy

Allow TCP **47981** to the Mac in addition to desktop port **47980**. The independent
connection reuses the existing pairing secret for automatic mutual authentication
and AES-GCM, then requires a fresh desktop-session token and the current consent
generation. These checks run in the background; no second user authentication is
required. A stopped session invalidates file access. Older Hosts simply do not
advertise file sharing; desktop control still works.

Selected file bytes and names go only to the paired peer. File names are needed
for transfer but full source paths are not sent, and file-drop paths are excluded
from text sync. No file contents are logged. Receiving files necessarily saves
them to disk; they are not encrypted at rest by LocalRemote. Text clipboard and
file clipboard have separate permissions. This borrows the idea of metadata
preparation followed by content transfer; it does not implement LocalSend protocol
interoperability. See [protocol](protocol.md) and [validation](validation.md).

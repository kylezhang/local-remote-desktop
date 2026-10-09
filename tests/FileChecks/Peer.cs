using System.Buffers.Binary;
using System.IO;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalRemote;

// Independent server framing fixture: real PSK mutual authentication and AES-GCM,
// synthetic files only. No real clipboard/credentials or Apple APIs are accessed.
sealed class FilePeer : IDisposable {
    readonly TcpClient tcp; readonly NetworkStream stream; byte[] tx=[],rx=[]; ulong sent,received;
    static byte[] Join(params byte[][] arrays)=>arrays.SelectMany(x=>x).ToArray();
    static byte[] Proof(byte[] key,string label,byte[] transcript)=>HMACSHA256.HashData(key,Join(Encoding.ASCII.GetBytes(label),transcript));
    static byte[] Nonce(ulong counter) { var b=new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(4),counter); return b; }
    public FilePeer(TcpClient tcp) { this.tcp=tcp; stream=tcp.GetStream(); }
    public async Task Authenticate(byte[] key,CancellationToken ct,byte[]? salt=null) {
        var nonce=RandomNumberGenerator.GetBytes(32); await stream.WriteAsync(salt==null?Join("RML1"u8.ToArray(),nonce):Join("RMP1"u8.ToArray(),nonce,salt),ct);
        var reply=new byte[64]; await stream.ReadExactlyAsync(reply,ct); var transcript=salt==null?Join(nonce,reply[..32]):Join("RMP1"u8.ToArray(),salt,nonce,reply[..32]);
        if(!CryptographicOperations.FixedTimeEquals(reply[32..],Proof(key,"client",transcript))) throw new IOException("Bad proof");
        await stream.WriteAsync(Proof(key,"server",transcript),ct); tx=Proof(key,"h2c",transcript); rx=Proof(key,"c2h",transcript);
    }
    public async Task<(byte type,byte[] data)> Read(CancellationToken ct) {
        var header=new byte[4]; await stream.ReadExactlyAsync(header,ct); int size=(int)BinaryPrimitives.ReadUInt32BigEndian(header);
        if(size<17 || size>65536) throw new IOException("Invalid size");
        var body=new byte[size]; await stream.ReadExactlyAsync(body,ct); var plain=new byte[size-16];
        using var aes=new AesGcm(rx,16); aes.Decrypt(Nonce(received++),body.AsSpan(0,plain.Length),body.AsSpan(plain.Length),plain,header);
        return (plain[0],plain[1..]);
    }
    public async Task<JsonElement> JsonRead(CancellationToken ct) {
        var record=await Read(ct); if(record.type!=1) throw new IOException("Wrong type");
        using var doc=JsonDocument.Parse(record.data); return doc.RootElement.Clone();
    }
    public async Task Send(byte type,byte[] data,CancellationToken ct) {
        var plain=Join([type],data); var header=new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(header,(uint)(plain.Length+16));
        var cipher=new byte[plain.Length]; var tag=new byte[16]; using var aes=new AesGcm(tx,16); aes.Encrypt(Nonce(sent++),plain,cipher,tag,header);
        await stream.WriteAsync(Join(header,cipher,tag),ct);
    }
    public Task Json(object message,CancellationToken ct)=>Send(1,JsonSerializer.SerializeToUtf8Bytes(message),ct);
    public void Dispose()=>tcp.Dispose();
}
sealed class MockFileServer : IDisposable {
    readonly TcpListener listener=new(IPAddress.Loopback,0);
    public int Port=>((IPEndPoint)listener.LocalEndpoint).Port;
    readonly CancellationTokenSource stop=new(TimeSpan.FromSeconds(90));
    readonly byte[] key; readonly byte[]? salt; readonly string token; readonly Func<long,bool> allowed;
    FilePeer? current;
    public FileSource? Offer;
    public string OfferId=Guid.NewGuid().ToString("N");
    public readonly ConcurrentQueue<byte[][]> Uploaded=new();
    public readonly ConcurrentQueue<bool> DownloadClipboardReady=new();
    public int DelayMilliseconds=5, CompletionDelayMilliseconds=0;
    public bool Stall {get;set;}
    public readonly ConcurrentQueue<string> Errors=new();
    public MockFileServer(byte[] key,string token,Func<long,bool> allowed,byte[]? salt=null) { this.key=key; this.salt=salt; this.token=token; this.allowed=allowed; listener.Start(); }
    public async Task Run() {
        try {
            while(!stop.IsCancellationRequested) {
                using var peer=new FilePeer(await listener.AcceptTcpClientAsync(stop.Token)); current=peer;
                try {
                    await peer.Authenticate(key,stop.Token,salt); var request=await peer.JsonRead(stop.Token);
                    if(request.GetProperty("session").GetString()!=token || !allowed(request.GetProperty("requestId").GetInt64())) continue;
                    if(Stall) { await Task.Delay(Timeout.Infinite,stop.Token); continue; }
                    string? op=request.GetProperty("op").GetString();
                    if(op=="file_push") {
                        var items=FileSource.Validate(request.GetProperty("items").Deserialize<FileItem[]>()!);
                        await peer.Json(new {op="file_ready"},stop.Token); var results=new List<byte[]>();
                        for(int i=0;i<items.Length;i++) {
                            var begin=await peer.JsonRead(stop.Token); FileTransfer.Expect(begin,"file_start");
                            if(begin.GetProperty("index").GetInt32()!=i) throw new IOException("Wrong index");
                            using var content=new MemoryStream();
                            while(true) {
                                var record=await peer.Read(stop.Token);
                                if(record.type==3) { if(record.data.Length>32768 || content.Length+record.data.Length>items[i].size) throw new IOException("Invalid chunk"); content.Write(record.data); }
                                else {
                                    using var doc=JsonDocument.Parse(record.data); var end=doc.RootElement; FileTransfer.Expect(end,"file_end");
                                    if(end.GetProperty("index").GetInt32()!=i || content.Length!=items[i].size || end.GetProperty("sha256").GetString()!=Convert.ToHexString(SHA256.HashData(content.ToArray())).ToLowerInvariant()) throw new IOException("Invalid hash/length");
                                    break;
                                }
                            }
                            results.Add(content.ToArray());
                        }
                        FileTransfer.Expect(await peer.JsonRead(stop.Token),"file_complete"); Uploaded.Enqueue(results.ToArray());
                        if(CompletionDelayMilliseconds>0) await Task.Delay(CompletionDelayMilliseconds,stop.Token);
                        await peer.Json(new {op="file_done",clipboardReady=true},stop.Token);
                    } else if(op=="file_pull" && request.GetProperty("offer").GetString()==OfferId && Offer!=null) {
                        var source=Offer; await peer.Json(new {op="file_ready",items=source.Items},stop.Token);
                        for(int i=0;i<source.Items.Length;i++) {
                            await peer.Json(new {op="file_start",index=i},stop.Token); var data=await File.ReadAllBytesAsync(source.Paths[i]);
                            for(int offset=0;offset<data.Length;offset+=32768) {
                                await peer.Send(3,data[offset..Math.Min(data.Length,offset+32768)],stop.Token);
                                await Task.Delay(DelayMilliseconds,stop.Token);
                            }
                            await peer.Json(new {op="file_end",index=i,sha256=Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()},stop.Token);
                        }
                        await peer.Json(new {op="file_complete"},stop.Token); var done=await peer.JsonRead(stop.Token); FileTransfer.Expect(done,"file_done");
                        DownloadClipboardReady.Enqueue(done.TryGetProperty("clipboardReady",out var ready) && ready.ValueKind==JsonValueKind.True);
                    }
                } catch(Exception ex) when(ex is IOException or OperationCanceledException or SocketException) { Errors.Enqueue(ex.GetType().Name); }
                finally { current=null; }
            }
        } catch(Exception ex) when(ex is OperationCanceledException or SocketException or ObjectDisposedException) { }
    }
    public void Dispose() { stop.Cancel(); listener.Stop(); current?.Dispose(); }
}

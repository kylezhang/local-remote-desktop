using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
namespace LocalRemoteDesktop;

internal interface IHostFiles : IAsyncDisposable {
    string Session {get;}
    int Port {get;}
    Task Control(JsonElement message);
    void Cancel();
}

// One listener/job per paired desktop session. The UI delegate serializes all
// clipboard access; socket/disk work never runs on the dispatcher.
internal sealed class WindowsHostFiles : IHostFiles {
    readonly object gate=new(); readonly Wire control; readonly byte[] key,salt;
    readonly IFileClipboard board; readonly Func<Action,Task> ui; readonly Func<bool> allowed;
    readonly Action<string,bool> status; readonly string root;
    readonly TcpListener listener; readonly CancellationTokenSource lifetime;
    readonly Task accept,pump; Task? worker; CancellationTokenSource? job;
    bool requested,enabled,stopped; long requestId,revision,offerRevision;
    (string id,FileSource source)? offer;
    public string Session {get;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    internal bool HasTransfer {get {lock(gate)return job!=null;}}
    public int Port=>((IPEndPoint)listener.LocalEndpoint).Port;
    internal WindowsHostFiles(Wire control,byte[] key,byte[] salt,IFileClipboard board,Func<Action,Task> ui,
        Func<bool> allowed,CancellationToken token,Action<string,bool>? status=null,string? root=null,IPAddress? address=null,int port=47981) {
        this.control=control; this.key=key.ToArray(); this.salt=salt.ToArray(); this.board=board; this.ui=ui; this.allowed=allowed;
        this.status=status??((_,_)=>{}); this.root=root??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalRemote","ReceivedFiles");
        lifetime=CancellationTokenSource.CreateLinkedTokenSource(token); listener=new(address??IPAddress.Any,port);
        try {listener.Start(2);} catch {lifetime.Dispose(); CryptographicOperations.ZeroMemory(this.key); CryptographicOperations.ZeroMemory(this.salt); throw;}
        accept=Task.Run(Accept); pump=Task.Run(Pump);
    }
    async Task<T> OnUI<T>(Func<T> work) { T result=default!; await ui(()=>result=work()); return result; }
    bool Valid(long id) { lock(gate) return !stopped && enabled && requested && requestId==id; }
    async Task Refresh(bool force=false) {
        bool permission=await OnUI(allowed); long id; bool next,changed;
        lock(gate) {
            next=!stopped && requested && permission; changed=next!=enabled || force; enabled=next; id=requestId;
            if(changed) { offer=null; job?.Cancel(); }
        }
        if(!changed) return;
        await ui(()=>revision=board.Revision);
        await control.Json(new {op="file_sync",requestId=id,enabled=next},lifetime.Token);
        status(next?"文件同步：已启用，复制文件后自动传输":"文件同步：关闭",false);
    }
    public async Task Control(JsonElement message) {
        if(!message.TryGetProperty("enabled",out var value) || value.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
            !message.TryGetProperty("requestId",out var number) || !number.TryGetInt64(out var id) || id<=0) return;
        lock(gate) { if(stopped || id<=requestId) return; requested=value.GetBoolean(); requestId=id; }
        await Refresh(true);
    }
    async Task Pump() {
        try {
            using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            while(await timer.WaitForNextTickAsync(lifetime.Token)) {
                await Refresh(); long id; lock(gate) id=requestId;
                if(!Valid(id)) continue;
                var copy=await OnUI(()=> {
                    if(board.Revision==revision || !board.TryReadFiles(out var paths)) return (changed:false,paths:(string[]?)null,version:revision);
                    revision=board.Revision; return (changed:true,paths,version:revision);
                });
                if(!copy.changed) continue;
                lock(gate) { offer=null; job?.Cancel(); }
                if(copy.paths is not {Length:>0}) continue;
                try {
                    var source=FileSource.Prepare(copy.paths); string name=Guid.NewGuid().ToString("N");
                    lock(gate) { if(!Valid(id)) continue; offer=(name,source); offerRevision=copy.version; }
                    await control.Json(new {op="file_offer",requestId=id,offer=name,items=source.Items},lifetime.Token);
                    status("文件已复制，正在等待目标设备接收…",true);
                } catch(Exception e) when(e is IOException or UnauthorizedAccessException or ArgumentException) { status("文件同步失败，请检查文件后重新复制。",false); }
            }
        } catch(OperationCanceledException) when(lifetime.IsCancellationRequested) { }
        catch { control.Dispose(); }
    }
    async Task Accept() {
        try {
            while(!lifetime.IsCancellationRequested) {
                var peer=await listener.AcceptTcpClientAsync(lifetime.Token);
                lock(gate) {
                    if(stopped || !enabled || job!=null || peer.Client.RemoteEndPoint is not IPEndPoint endpoint || !WindowsHost.IsLan(endpoint.Address)) {peer.Dispose();continue;}
                    job=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); job.CancelAfter(TimeSpan.FromHours(1));
                    var current=job; worker=Task.Run(()=>Transfer(peer,current));
                }
            }
        } catch(Exception e) when(e is OperationCanceledException or SocketException or ObjectDisposedException) { }
    }
    async Task Transfer(TcpClient peer,CancellationTokenSource current) {
        using(peer) {
            try {
                using var wire=await Wire.Accept(peer,key,salt,current.Token,fileChannel:true);
                var message=await FileTransfer.Json(wire,current.Token);
                var supplied=message.GetProperty("session").GetString(); var id=message.GetProperty("requestId").GetInt64();
                if(supplied==null || supplied.Length!=64 || !CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(supplied),System.Text.Encoding.ASCII.GetBytes(Session)) || !Valid(id) || !await OnUI(allowed)) throw new IOException("Session denied");
                var clock=System.Diagnostics.Stopwatch.StartNew(); double last=-1; string direction="接收";
                void Progress(long done,long total) {if(clock.Elapsed.TotalSeconds-last<0.25 && done!=total)return;last=clock.Elapsed.TotalSeconds;status(FileTransfer.ProgressText(direction,done,total,clock.Elapsed.TotalSeconds),true);}
                if(message.GetProperty("op").GetString()=="file_push") {
                    var items=FileSource.Validate(message.GetProperty("items").Deserialize<FileItem[]>()!);
                    var baseline=await OnUI(()=>board.Revision);
                    await wire.Json(new {op="file_ready"},current.Token);
                    var paths=await FileTransfer.Receive(wire,items,root,Progress,current.Token,()=>Valid(id));
                    bool ready=false;
                    for(int attempt=0;attempt<20;attempt++) {
                        bool changed=false;
                        ready=await OnUI(()=> {
                            if(!Valid(id) || !allowed() || board.Revision!=baseline) {changed=true;return false;}
                            if(!board.TryWriteFiles(paths))return false; revision=board.Revision; return true;
                        });
                        if(ready || changed)break; await Task.Delay(100,current.Token);
                    }
                    await wire.Json(new {op="file_done",clipboardReady=ready},current.Token);
                    status(ready?"文件已接收，可在 Windows 粘贴":"文件已接收；剪贴板已变化，可打开接收目录",false);
                } else if(message.GetProperty("op").GetString()=="file_pull") {
                    direction="发送"; (string id,FileSource source)? candidate; long baseline;
                    lock(gate) {candidate=offer;baseline=offerRevision;}
                    if(candidate==null || candidate.Value.id!=message.GetProperty("offer").GetString() || await OnUI(()=>board.Revision)!=baseline) throw new IOException("Offer changed");
                    await wire.Json(new {op="file_ready",items=candidate.Value.source.Items},current.Token);
                    await FileTransfer.Send(wire,candidate.Value.source,Progress,current.Token);
                    var done=await FileTransfer.Json(wire,current.Token); FileTransfer.Expect(done,"file_done");
                    status(done.GetProperty("clipboardReady").GetBoolean()?"文件已发送，可在目标设备粘贴":"文件已发送；目标设备未确认剪贴板就绪",false);
                } else throw new IOException("Invalid file operation");
            } catch { if(!lifetime.IsCancellationRequested) status("文件传输结束或失败；重新复制可重试",false); }
            finally {lock(gate) {if(ReferenceEquals(job,current))job=null;} current.Dispose();}
        }
    }
    public void Cancel() {lock(gate) {offer=null;job?.Cancel();} status("文件传输已取消；重新复制可重试",false);}
    public async ValueTask DisposeAsync() {
        lock(gate) {if(stopped)return;stopped=true;enabled=false;offer=null;job?.Cancel();}
        lifetime.Cancel();listener.Stop();await Task.WhenAll(accept,pump); Task? running;lock(gate)running=worker;
        if(running!=null)await running;
        CryptographicOperations.ZeroMemory(key);CryptographicOperations.ZeroMemory(salt);lifetime.Dispose();
    }
}

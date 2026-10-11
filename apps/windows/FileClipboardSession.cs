using System.IO;
using System.Diagnostics;
using System.Text.Json;
namespace LocalRemoteDesktop;

public interface IFileClipboard {
    long Revision { get; }
    bool TryReadFiles(out string[]? paths);
    bool TryWriteFiles(string[] paths);
}
internal sealed class DisabledFileClipboard : IFileClipboard {
    public long Revision=>0;
    public bool TryReadFiles(out string[]? paths) { paths=null; return true; }
    public bool TryWriteFiles(string[] paths)=>false;
}

// All state/clipboard access is owned by the UI thread. File and socket work is
// background-only, with a single cancellable job and bounded progress updates.
public sealed class FileClipboardSession {
    readonly IFileClipboard board;
    readonly Action<object> send;
    readonly Action<string,bool> status;
    readonly Action<Action> dispatch;
    readonly string root;
    readonly int port;
    string ip="", key="", token="";
    long requestId, revision, generation;
    bool connected, supported, requested, enabled, remoteReady=true;
    CancellationTokenSource? job;
    public bool Busy=>job!=null;
    public bool PasteReady=>!Busy && remoteReady;
    public string ReceivedDirectory=>root;
    public FileClipboardSession(IFileClipboard board,Action<object> send,Action<string,bool> status,Action<Action> dispatch,string? root=null,int port=47981) {
        this.board=board; this.send=send; this.status=status; this.dispatch=dispatch;
        this.port=port;
        this.root=root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalRemote","ReceivedFiles");
    }
    public void Connect(string address,string secret) { Reset(); ip=address; key=secret; connected=true; }
    public void Reset() { Cancel(); connected=supported=enabled=false; ip=key=token=""; revision=board.Revision; remoteReady=true; }
    public void Cancel() { generation++; job?.Cancel(); job=null; }
    public void Configure(bool value) {
        requested=value; enabled=false; Cancel(); revision=board.Revision; remoteReady=true;
        status(!requested?"文件同步：关闭":!connected?"文件同步：等待连接":!supported?"文件同步：目标设备版本不支持":"文件同步：等待目标设备允许",false);
        if(connected && supported) send(new {op="file_sync",enabled=value,requestId=++requestId});
    }
    public void Control(JsonElement message) {
        string? op=message.GetProperty("op").GetString();
        if(op=="capabilities") {
            supported=message.TryGetProperty("fileClipboard",out var flag) && flag.ValueKind==JsonValueKind.True
                && message.TryGetProperty("filePort",out var endpoint) && endpoint.TryGetInt32(out var p) && p==port
                && message.TryGetProperty("fileSession",out var session) && session.ValueKind==JsonValueKind.String;
            if(supported) { token=message.GetProperty("fileSession").GetString()!; supported=token.Length==64 && token.All(Uri.IsHexDigit); }
            Configure(requested); return;
        }
        if(!connected || !supported || !message.TryGetProperty("requestId",out var request) || !request.TryGetInt64(out var id) || id!=requestId) return;
        if(op=="file_sync" && message.TryGetProperty("enabled",out var permission) && permission.ValueKind is JsonValueKind.True or JsonValueKind.False) {
            bool next=requested && permission.GetBoolean();
            if(next!=enabled) { Cancel(); revision=board.Revision; remoteReady=true; }
            enabled=next; status(enabled?"文件同步：已启用，复制文件后自动传输":requested?"文件同步：等待目标设备允许":"文件同步：关闭",false);
        } else if(op=="file_offer" && enabled) {
            try {
                var items=FileSource.Validate(message.GetProperty("items").Deserialize<FileItem[]>()!);
                string offer=message.GetProperty("offer").GetString()!;
                if(offer.Length!=32 || !offer.All(Uri.IsHexDigit)) return;
                Start(null,offer,items);
            } catch(Exception ex) when(ex is IOException or JsonException or InvalidOperationException or ArgumentException) { status("文件信息无效，未接收",false); }
        }
    }
    public void Poll() {
        if(!enabled || board.Revision==revision) return;
        long changed=board.Revision;
        if(!board.TryReadFiles(out var paths)) return;
        revision=changed;
        if(paths==null) { bool wasBusy=Busy; Cancel(); remoteReady=true; if(wasBusy) status("文件传输已取消：剪贴板已更新",false); return; }
        Start(paths,null,null);
    }
    void Start(string[]? paths,string? offer,FileItem[]? items) {
        Cancel(); var owner=new CancellationTokenSource(TimeSpan.FromHours(1)); job=owner;
        long run=generation, baseline=board.Revision, request=requestId;
        string address=ip, secret=key, session=token;
        remoteReady=paths==null; status(paths!=null?$"正在准备发送 {paths.Length} 个文件…":$"正在准备接收 {items!.Length} 个文件…",true);
        _=Run();
        async Task Run() {
            try {
                long last=0; var clock=Stopwatch.StartNew();
                void Progress(long done,long total) {
                    long now=Environment.TickCount64;
                    if(now-last<250 && done!=total) return; last=now;
                    dispatch(()=> { if(run==generation && enabled) status(FileTransfer.ProgressText(paths!=null?"发送":"接收",done,total,clock.Elapsed.TotalSeconds),true); });
                }
                if(paths!=null) {
                    bool ready=await Task.Run(async()=> {
                        var source=FileSource.Prepare(paths);
                        return await FileTransfer.Upload(address,secret,session,request,source,Progress,owner.Token,port);
                    },owner.Token);
                    if(run==generation && enabled) { remoteReady=ready; status(ready?"文件已发送，可在目标设备粘贴":"文件已保存到目标设备；其剪贴板已变化，请从接收目录取用",false); }
                } else {
                    bool published=false;
                    async Task<bool> Publish(string[] received,CancellationToken ct) {
                        for(int i=0;i<20;i++) {
                            var result=new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                            dispatch(()=> {
                                try {
                                    if(ct.IsCancellationRequested || run!=generation || !enabled || board.Revision!=baseline) { result.TrySetResult(-1); return; }
                                    if(!board.TryWriteFiles(received)) { result.TrySetResult(0); return; }
                                    revision=board.Revision; result.TrySetResult(1);
                                } catch(Exception ex) { result.TrySetException(ex); }
                            });
                            int attempt=await result.Task.WaitAsync(ct);
                            if(attempt==1) { published=true; return true; }
                            if(attempt<0) return false;
                            await Task.Delay(100,ct);
                        }
                        return false;
                    }
                    await Task.Run(()=>FileTransfer.Download(address,secret,session,request,offer!,items!,root,Progress,owner.Token,port,Publish),owner.Token);
                    if(run==generation && enabled) status(published?"文件已接收，可在 Windows 粘贴":"文件已接收，剪贴板忙或已变化；可打开接收目录",false);
                }
            } catch(OperationCanceledException) { if(run==generation) status(owner.IsCancellationRequested?"文件传输已取消":"文件传输超时；重新复制可重试",false); }
            catch(Exception ex) { if(run==generation) status($"文件传输失败：{ex.Message}；重新复制可重试",false); }
            finally { if(run==generation) { job=null; } owner.Dispose(); }
        }
    }
}

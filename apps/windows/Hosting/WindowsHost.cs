using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
namespace LocalRemote;

// Not a second application: the sharing role's lifetime is owned by the GUI.
// The listener remains opt-in; construction alone never opens a socket.
internal sealed class WindowsHost: IAsyncDisposable {
    readonly object gate=new();
    readonly byte[] key,salt;
    readonly TcpListener listener;
    readonly Func<IDesktopCapture> capture;
    readonly Func<WindowsInput> input;
    readonly CaptureArea area;
    readonly CancellationTokenSource stop=new();
    readonly Action<string> status;
    Task? accept,session; TcpClient? controller; int disposed;
    internal string LastError {get;private set;}="";
    internal int Port=>((IPEndPoint)listener.LocalEndpoint).Port;
    internal WindowsHost(byte[] key,byte[] salt,CaptureArea area,Func<IDesktopCapture> capture,Func<WindowsInput> input,Action<string>? status=null,IPAddress? address=null,int port=47980) {
        if(key.Length!=32 || salt.Length!=16) throw new ArgumentException("Invalid host credential");
        this.key=key.ToArray(); this.salt=salt.ToArray(); this.area=area; this.capture=capture; this.input=input;
        this.status=status??(_=>{}); listener=new(address??IPAddress.Any,port);
    }
    internal void Start() {
        lock(gate) { if(disposed!=0 || accept!=null) throw new InvalidOperationException("Host already started/stopped"); listener.Start(4); accept=AcceptLoop(); }
        status("Available");
    }
    internal static bool IsLan(IPAddress ip) {
        if(ip.AddressFamily!=AddressFamily.InterNetwork) return false;
        var b=ip.GetAddressBytes(); return b[0]==10 || b[0]==172 && b[1] is >=16 and <=31 || b[0]==192 && b[1]==168 || b[0]==169 && b[1]==254 || b[0]==127;
    }
    async Task AcceptLoop() {
        try {
            while(!stop.IsCancellationRequested) {
                var peer=await listener.AcceptTcpClientAsync(stop.Token);
                lock(gate) {
                    if(stop.IsCancellationRequested || controller!=null || peer.Client.RemoteEndPoint is not IPEndPoint endpoint || !IsLan(endpoint.Address)) { peer.Dispose(); continue; }
                    controller=peer; session=Task.Run(()=>Serve(peer));
                }
            }
        } catch(Exception e) when(e is OperationCanceledException or SocketException or ObjectDisposedException) { if(!stop.IsCancellationRequested) status("Listener failed"); }
    }
    async Task Serve(TcpClient peer) {
        WindowsInput? injector=null; Task? video=null;
        using var lifetime=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        try {
            using var wire=await Wire.Accept(peer,key,salt,lifetime.Token);
            var hello=await wire.Read(lifetime.Token);
            if(hello.type!=1) throw new IOException("Expected control handshake");
            using var json=JsonDocument.Parse(hello.data); var h=json.RootElement;
            if(h.ValueKind!=JsonValueKind.Object || !h.TryGetProperty("op",out var op) || op.GetString()!="handshake" ||
                !h.TryGetProperty("version",out var version) || !version.TryGetInt32(out var v) || v!=1 ||
                !h.TryGetProperty("quality",out var quality) || !quality.TryGetInt32(out var q) || q is <0 or >2) throw new IOException("Invalid control handshake");
            var profile=VideoProfile.For(area,q); injector=input(); LastError="";
            await wire.Json(new {op="capabilities",platform="windows",inputModel="win-vk-v1",inputEnabled=true,clipboardText=false,fileClipboard=false},lifetime.Token);
            await wire.Json(new {op="video_header",width=profile.Width,height=profile.Height,fps=profile.Fps,codec="h264-annexb"},lifetime.Token);
            status("In use");
            // Backpressure reaches the encoder pipe; no unbounded frame queue.
            video=Task.Run(async()=> {
                try { await capture().Run(profile,frame=>wire.Send(2,frame,lifetime.Token),lifetime.Token); }
                finally { lifetime.Cancel(); }
            });
            while(!lifetime.IsCancellationRequested) {
                var packet=await wire.Read(lifetime.Token);
                if(packet.type!=1) throw new IOException("Unexpected client record");
                using var message=JsonDocument.Parse(packet.data); var m=message.RootElement;
                if(m.ValueKind!=JsonValueKind.Object || !m.TryGetProperty("op",out var kind) || kind.ValueKind!=JsonValueKind.String) throw new IOException("Invalid control record");
                var name=kind.GetString();
                if(name=="disconnect") break;
                if(name=="heartbeat") { await wire.Json(new {op="heartbeat",inputEnabled=true,inputReceived=injector.Received,inputPosted=injector.Posted,inputBlocked=injector.Blocked,inputRejected=injector.Rejected,inputError=injector.Error},lifetime.Token); continue; }
                if(name is "mouse_move" or "mouse_button" or "scroll" or "key_event" or "release_all") injector.Handle(m);
                else if(name is not ("clipboard_sync" or "file_sync")) throw new IOException("Unsupported control operation");
            }
        } catch(Exception e) {
            // Never expose the password, captured pixels or peer payload in status.
            if(!stop.IsCancellationRequested && e is not OperationCanceledException) LastError=e.GetType().Name;
            if(!stop.IsCancellationRequested) status("Session ended");
        } finally {
            lifetime.Cancel(); peer.Dispose();
            // Release OS input immediately; do not keep modifiers held while an
            // encoder drains/exits on a slow or failing machine.
            injector?.Release();
            if(video!=null) { try { await video; } catch(Exception e) { if(e is not OperationCanceledException) LastError="capture_"+e.GetType().Name; } }
            lock(gate) { if(ReferenceEquals(controller,peer)) controller=null; }
            if(!stop.IsCancellationRequested) status("Available");
        }
    }
    public async ValueTask DisposeAsync() {
        if(Interlocked.Exchange(ref disposed,1)!=0) return;
        stop.Cancel(); listener.Stop(); Task? running;
        lock(gate) { controller?.Dispose(); running=session; }
        if(accept!=null) await accept;
        if(running!=null) await running;
        CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(salt); stop.Dispose(); status("Off");
    }
}

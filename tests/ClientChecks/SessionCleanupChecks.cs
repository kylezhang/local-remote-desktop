using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using LocalRemoteDesktop;

static partial class Program {
    static async Task CheckSessionCleanup() {
        var root=Path.Combine(Path.GetTempPath(),"lrd-session-cleanup-"+Guid.NewGuid().ToString("N"));
        var board=new ReverseBoard();var fileBoard=new ReverseFileBoard();
        var area=new CaptureArea(0,0,320,180);var credential=HostCredential.Create("Fixture1");
        int available=0;WindowsHostFiles? current=null;var sessions=new HashSet<string>();
        try {
            await using var host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,
                ()=>new SilentCapture(),()=>new WindowsInput(area,area,new SharingInput()),
                status:s=>{if(s=="Available")Interlocked.Increment(ref available);},address:IPAddress.Loopback,port:0,
                clipboard:(wire,ct)=>new WindowsHostClipboard(wire,board,Application.Current.Dispatcher,()=>true,ct),
                files:(wire,key,salt,ct)=>current=new WindowsHostFiles(wire,key,salt,fileBoard,async action=>{await Application.Current.Dispatcher.InvokeAsync(action);},()=>true,ct,root:root,address:IPAddress.Loopback,port:0));
            host.Start();using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));
            for(int cycle=0;cycle<12;cycle++) {
                using var control=new Wire();await control.Connect("127.0.0.1",credential.Password,deadline.Token,host.Port);
                await control.Json(new {op="handshake",version=1,quality=0},deadline.Token);
                var caps=await FileTransfer.Json(control,deadline.Token);await FileTransfer.Json(control,deadline.Token);
                Assert(sessions.Add(caps.GetProperty("fileSession").GetString()!),"Reconnect reused file-session identity");
                int filePort=caps.GetProperty("filePort").GetInt32();
                await control.Json(new {op="clipboard_sync",requestId=1,enabled=true},deadline.Token);await FileTransfer.Json(control,deadline.Token);
                await control.Json(new {op="file_sync",requestId=1,enabled=true},deadline.Token);await FileTransfer.Json(control,deadline.Token);
                // Alternate deliberate disconnect and abrupt transport loss.
                if(cycle%2==0) await control.Json(new {op="disconnect"},deadline.Token);
                control.Dispose();await Until(()=>Volatile.Read(ref available)==cycle+2,"Ended session did not release its workers");
                Assert(!current!.HasTransfer,"Disconnected host retains file job");
                var replacement=new TcpListener(IPAddress.Loopback,filePort);
                try {replacement.Start();} finally {replacement.Stop();}
            }
            Assert(!Directory.Exists(root) && board.Text==null && fileBoard.Paths==null,"Idle reconnect touched clipboard or file storage");
            await host.DisposeAsync();
            var previous=current!;
            int finalAvailable=available;await Task.Delay(650);
            Assert(!previous.HasTransfer && available==finalAvailable,"Stopped host resumed background work");
            Console.WriteLine("PASS 12 production encrypted sharing sessions: alternating disconnect/transport loss, fresh file identities, text/file workers settle, file ports reusable, idle clipboard/storage unchanged; no capture or physical input");
        } finally {if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}

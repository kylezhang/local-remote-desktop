using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using LocalRemoteDesktop;

static partial class Program {
    static async Task CheckReverseFiles() {
        var root=Path.Combine(Path.GetTempPath(),"lrd-reverse-files-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var board=new ReverseFileBoard(); bool allowed=true; WindowsHostFiles? files=null;
        var area=new CaptureArea(0,0,320,180);var credential=HostCredential.Create("Fixture1");
        try {
            await using var host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,
                ()=>new SilentCapture(),()=>new WindowsInput(area,area,new SharingInput()),address:IPAddress.Loopback,port:0,
                files:(wire,key,salt,ct)=>files=new WindowsHostFiles(wire,key,salt,board,async action=>{await Application.Current.Dispatcher.InvokeAsync(action);},()=>allowed,ct,root:Path.Combine(root,"received"),address:IPAddress.Loopback,port:0));
            host.Start();using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var control=new Wire();await control.Connect("127.0.0.1",credential.Password,deadline.Token,host.Port);
            await control.Json(new {op="handshake",version=1,quality=0},deadline.Token);
            async Task<JsonElement> Read()=>await FileTransfer.Json(control,deadline.Token);
            var caps=await Read();await Read(); Assert(caps.GetProperty("fileClipboard").GetBoolean(),"Reverse files not advertised");
            string session=caps.GetProperty("fileSession").GetString()!;int port=caps.GetProperty("filePort").GetInt32();
            Assert(session.Length==64 && files!=null,"Missing session-bound file identity");
            await control.Json(new {op="file_sync",requestId=1,enabled=true},deadline.Token);Assert((await Read()).GetProperty("enabled").GetBoolean(),"File consent not acknowledged");
            var a=Path.Combine(root,"中文.txt");var b=Path.Combine(root,"empty.txt");await File.WriteAllTextAsync(a,"Mac to Windows 🎉");await File.WriteAllBytesAsync(b,[]);
            var source=FileSource.Prepare([a,b]);
            Assert(await FileTransfer.Upload("127.0.0.1",credential.Password,session,1,source,null,deadline.Token,port),"Mac-side push not clipboard-ready");
            Assert(board.Paths is {Length:2} && await File.ReadAllTextAsync(board.Paths[0])=="Mac to Windows 🎉" && new FileInfo(board.Paths[1]).Length==0,"Reverse upload bytes/empty file mismatch");
            await control.Json(new {op="heartbeat"},deadline.Token);Assert((await Read()).GetProperty("op").GetString()=="heartbeat","Received-file echo leaked onto desktop control");
            board.Copy([a]);var offer=await Read();Assert(offer.GetProperty("op").GetString()=="file_offer","Windows copy not offered to Mac");
            var downloaded=await FileTransfer.Download("127.0.0.1",credential.Password,session,1,offer.GetProperty("offer").GetString()!,source.Items[..1],Path.Combine(root,"mac-received"),null,deadline.Token,port,(_,_)=>Task.FromResult(true));
            Assert(await File.ReadAllTextAsync(downloaded[0])=="Mac to Windows 🎉","Windows return-file path changed bytes");
            async Task Rejected(string token,long id) {
                bool rejected=false;try{await FileTransfer.Upload("127.0.0.1",credential.Password,token,id,source,null,deadline.Token,port);}catch(Exception e) when(e is IOException or System.Net.Sockets.SocketException){rejected=true;}
                Assert(rejected,"Wrong file session/generation accepted");
                await Until(()=>!files!.HasTransfer,"Rejected file worker did not release slot");
            }
            await Rejected(new string('0',64),1);await Rejected(session,99);
            // Hold a partial encrypted upload, then revoke local consent.
            using(var pending=new Wire(65536,15)) {
                await pending.Connect("127.0.0.1",credential.Password,deadline.Token,port);
                await pending.Json(new {op="file_push",session,requestId=1,items=source.Items},deadline.Token);FileTransfer.Expect(await FileTransfer.Json(pending,deadline.Token),"file_ready");
                await pending.Json(new {op="file_start",index=0},deadline.Token);await pending.Send(3,new byte[]{1},deadline.Token);
                allowed=false;var ack=await Read();Assert(!ack.GetProperty("enabled").GetBoolean(),"Host file consent revocation missing");
                await Until(()=>!files!.HasTransfer,"Revoked transfer still owns socket");
                Assert(!Directory.EnumerateDirectories(Path.Combine(root,"received"),".incoming-*").Any(),"Canceled partial files retained");
            }
            var before=board.Revision;await control.Json(new {op="file_sync",requestId=2,enabled=false},deadline.Token);Assert(!(await Read()).GetProperty("enabled").GetBoolean(),"Disable ack missing");
            await control.Json(new {op="file_sync",requestId=1,enabled=true},deadline.Token);await control.Json(new {op="heartbeat"},deadline.Token);
            Assert((await Read()).GetProperty("op").GetString()=="heartbeat" && board.Revision==before,"Old consent re-enabled file channel");
            await control.Json(new {op="disconnect"},deadline.Token);await host.DisposeAsync();
            Console.WriteLine("PASS production Windows file host encrypted push/pull: Unicode/empty files, SHA256, no echo, wrong session/stale generation, revoke mid-upload, staging cleanup and stop; recording clipboard/input, unshown");
        } finally {Directory.Delete(root,true);}
    }
}
sealed class ReverseFileBoard:IFileClipboard {
    public long Revision {get;private set;} public string[]? Paths;
    public bool TryReadFiles(out string[]? paths){paths=Paths;return true;}
    public bool TryWriteFiles(string[] paths){Copy(paths);return true;}
    internal void Copy(string[]? paths){Paths=paths;Revision++;}
}

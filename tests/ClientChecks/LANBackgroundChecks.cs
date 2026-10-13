using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using LocalRemoteDesktop;

static partial class Program {
    // Explicit cross-device fixture: production service, synthetic encoder source,
    // recording input/clipboards, no window, discovery or firewall modifications.
    static async Task CheckLANBackground() {
        var address=IPAddress.Parse(Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_ADDRESS")??throw new Exception("Private test address required"));
        Assert(WindowsHost.IsLan(address),"Private test address required");
        var metadata=Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_METADATA")??throw new Exception("Ready file required");
        var root=Path.Combine(Path.GetTempPath(),"lrd-lan-background-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var area=new CaptureArea(0,0,320,180); var recording=new SharingInput(); var input=new WindowsInput(area,area,recording);
        var text=new ReverseBoard(); var board=new ReverseFileBoard(); var statuses=new ConcurrentQueue<string>();
        var captures=new ConcurrentBag<FfmpegDesktopCapture>(); WindowsHostFiles? files=null;
        var credential=HostCredential.Create("Fixture1");
        try {
            await using var host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,
                ()=>{var next=new FfmpegDesktopCapture(area,software:true,synthetic:true); captures.Add(next);return next;},()=>input,statuses.Enqueue,
                address:address,deviceName:()=>"Fixture Windows",
                clipboard:(wire,ct)=>new WindowsHostClipboard(wire,text,Application.Current.Dispatcher,()=>true,ct),
                files:(wire,key,salt,ct)=>files=new WindowsHostFiles(wire,key,salt,board,
                    async action=>{await Application.Current.Dispatcher.InvokeAsync(action);},()=>true,ct,
                    root:Path.Combine(root,"received"),address:address));
            host.Start();
            File.WriteAllText(metadata,JsonSerializer.Serialize(new {address=address.ToString(),port=host.Port}));
            Console.WriteLine("READY paired cross-device fixture; synthetic video, recording input/clipboards, no visible window");
            await Until(()=>text.Text=="Mac to Windows 中文 🎉","Mac fixture text not received",90000);
            text.Copy("Windows to Mac 中文 🎉");
            await Until(()=>board.Paths is {Length:3},"Mac fixture file batch not received",30000);
            var uploaded=board.Paths!;
            Assert(await File.ReadAllTextAsync(uploaded[0])=="Mac file 中文 🎉","Unicode file differs");
            Assert(new FileInfo(uploaded[1]).Length==0,"Empty file differs");
            var binary=Enumerable.Range(0,512*1024).Select(i=>(byte)(i%251)).ToArray();
            Assert((await File.ReadAllBytesAsync(uploaded[2])).SequenceEqual(binary),"Binary file differs");
            await Until(()=>files!=null && !files.HasTransfer,"Upload worker did not settle",10000);
            var outgoing=new[]{Path.Combine(root,"Windows-中文.txt"),Path.Combine(root,"empty.txt"),Path.Combine(root,"binary.bin")};
            await File.WriteAllTextAsync(outgoing[0],"Windows file 中文 🎉"); await File.WriteAllBytesAsync(outgoing[1],[]); await File.WriteAllBytesAsync(outgoing[2],binary);
            board.Copy(outgoing);
            await Until(()=>text.Text=="interop complete","Mac did not verify return text/files",30000);
            await Until(()=>statuses.Count(x=>x=="Available")>=4,"Three quality sessions did not cleanly settle",30000);
            Assert(input.Held==0 && input.Blocked==0 && input.Rejected==0,"Recorded input did not release cleanly");
            var keys=recording.Values.Where(x=>x.Type==1).Select(x=>x.Data.Key.Vk).ToArray();
            Assert(keys.Take(6).SequenceEqual(new ushort[]{65,65,49,49,97,97}),"Mac letter/top-row/keypad mapping or order differs");
            Assert(recording.Values.Any(x=>(x.Data.Mouse.Flags&0x800)!=0),"Wheel not recorded");
            Assert(captures.Count==3,"Quality reconnect created unexpected capture count");
            await host.DisposeAsync();
            Assert(!Directory.EnumerateDirectories(Path.Combine(root,"received"),".incoming-*").Any(),"Partial files retained");
            Console.WriteLine("PASS real Mac/Windows LAN: mutual auth, three quality sessions, generated H264, recorded input order/release, bidirectional Unicode text and three-file batches (empty/Unicode/512 KiB binary), cleanup");
        } finally {
            if(File.Exists(metadata)) File.Delete(metadata);
            Directory.Delete(root,true);
        }
    }
}

using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LocalRemoteDesktop;

static class Program {
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }
    static async Task Until(Func<bool> value,string message,int timeout=10000) {
        var watch=Stopwatch.StartNew(); while(!value()) { if(watch.ElapsedMilliseconds>timeout) throw new Exception(message); await Task.Delay(20); }
    }
    static byte[] Join(params byte[][] values)=>values.SelectMany(v=>v).ToArray();
    static byte[] H(byte[] key,string label,byte[] data)=>HMACSHA256.HashData(key,Join(Encoding.ASCII.GetBytes(label),data));
    static byte[] Nonce(ulong n) { var result=new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(4),n); return result; }
    static async Task Main() {
        if(Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_TRANSPORT_CHECK")=="1") { await LANTransport(); return; }
        await Framing(); Input(); PointerGeometryChecks.Run(); Credentials(); await Authentication(); await Lifecycle(); await FailureCleanup();
        if(OperatingSystem.IsWindows()) { foreach(int quality in new[]{0,1,2}) await Capture(false,quality); if(Environment.GetEnvironmentVariable("LOCALREMOTE_REAL_CAPTURE_CHECK")=="1") await Capture(true,0); }
        Console.WriteLine("PASS Windows sharing backend checks; no system input or clipboard injection");
    }
    static async Task LANTransport() {
        var address=IPAddress.Parse(Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_ADDRESS")??throw new Exception("LAN address required"));
        Require(WindowsHost.IsLan(address),"LAN test must bind a private address");
        var recorder=new RecordingInput(); var area=CaptureArea.Primary();
        var input=new WindowsInput(area,CaptureArea.Virtual(),recorder);
        var credential=HostCredential.Create("Office-LAN#42");
        FfmpegDesktopCapture? capture=null;
        await using var host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,
            ()=>capture=new FfmpegDesktopCapture(area,software:false,synthetic:true),()=>input,address:address);
        host.Start(); Console.WriteLine("READY headless LAN production WindowsHost / synthetic MF H264 / recording input adapter");
        await Until(()=>recorder.Values.Count>=8,"Mac LAN inputs not received",45000);
        await Until(()=>input.Received>=9 && input.Held==0,"Mac LAN release_all not received");
        Require(input.Blocked==0 && input.Rejected==0,"Mac LAN malformed/blocked events");
        Require(recorder.Values.Where(v=>v.Type==1).Select(v=>(int)v.Data.Key.Vk).SequenceEqual(new[]{65,65,49,49}),"Mac key order or target mapping wrong");
        Require(recorder.Values.Any(v=>(v.Data.Mouse.Flags&0x800)!=0),"Mac wheel not received");
        Console.WriteLine($"PASS actual Mac native controller -> LAN -> production Windows host: {input.Received} input records, ordered keys, pointer/button/wheel and release; recording adapter, no system input");
        await Task.Delay(1500);
    }
    static async Task Framing() {
        byte[] stream=[0,0,0,1,9,0xf0,0,0,1,7,4,5,0,0,0,1,8,3,0,0,1,5,1,2,3,0,0,0,1,9,0xf0,0,0,1,1,4,5];
        for(int split=1;split<stream.Length;split++) {
            var parser=new AnnexBFrames(); var frames=new List<byte[]>(); Task Add(byte[] f) {frames.Add(f); return Task.CompletedTask;}
            for(int offset=0;offset<stream.Length;offset+=split) await parser.Feed(stream.AsMemory(offset,Math.Min(split,stream.Length-offset)),Add);
            await parser.Finish(Add);
            Require(frames.Count==2 && frames.SelectMany(x=>x).SequenceEqual(stream),"AUD framing loses bytes at split "+split);
        }
        try { await new AnnexBFrames().Feed(new byte[8*1024*1024],_=>Task.CompletedTask); throw new Exception("Oversize frame accepted"); } catch(System.IO.IOException) {}
        Console.WriteLine("PASS bounded H264 access-unit framing at every split / three and four-byte prefixes");
    }
    static void Input() {
        Require(Marshal.SizeOf<NativeInput>()==40 && Marshal.OffsetOf<NativeInput>("Data").ToInt32()==8,"Native x64 INPUT layout wrong");
        var native=new RecordingInput(); var input=new WindowsInput(new(0,0,1920,1080),new(-1920,0,3840,1080),native);
        void Send(object m) { var message=JsonSerializer.SerializeToElement(m); input.Handle(message); }
        Send(new {op="key_event",vk=162,down=true,flags=2}); Send(new {op="key_event",vk=67,down=true,flags=2});
        Send(new {op="mouse_button",button=0,down=true,x=1d,y=1d});
        Require(input.Held==3 && native.Values[0].Data.Key.Vk==162,"Windows Control was mapped to Win/Command");
        Require(native.Values[2].Data.Mouse.X==65535 && native.Values[2].Data.Mouse.Y==65535,"Primary/virtual-display position wrong");
        Send(new {op="release_all"}); Require(input.Held==0 && native.Values.Count==6,"Held-key/button cleanup wrong");
        Send(new {op="key_event",vk=163,down=true}); Require((native.Values[^1].Data.Key.Flags&1)!=0,"Right Ctrl missing extended identity");
        Send(new {op="scroll",delta=-120,x=0d,y=0d}); Require(native.Values[^1].Data.Mouse.Wheel==unchecked((uint)-120),"Wheel sign lost");
        var posted=input.Posted;
        foreach(var m in new object[]{new {op="key_event",vk=999,down=true},new {op="key_event",vk=65,down="true"},new {op="mouse_move",x=2d,y=0d},new {op="mouse_button",button=5,down=true,x=0d,y=0d},new {op="scroll",delta=int.MaxValue,x=0d,y=0d}}) Send(m);
        Require(input.Rejected==5 && input.Posted==posted,"Malformed input was injected");
        native.Block=true; Send(new {op="key_event",vk=65,down=true}); Require(input.Blocked==1 && input.Held==1,"Failed input changed held state");
        input.Release(); Require(input.Held==1,"Blocked releases forgot held keys"); native.Block=false; input.Release(); Require(input.Held==0,"Release recovery failed");
        Console.WriteLine("PASS native INPUT layout / Windows VK / modifiers / virtual coordinates / wheel / malformed denial / blocked and cleanup state (recording adapter)");
    }
    static async Task Authentication() {
        foreach(bool legacy in new[]{false,true}) foreach(string mode in new[]{"valid","wrong-key","tamper","replay","oversize"}) {
            byte[] salt=Enumerable.Range(0,16).Select(i=>(byte)i).ToArray(),key=PairingPassword.Derive("Office-LAN#42",salt);
            var listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(12));
            bool rejected=false;
            var server=Task.Run(async()=> { try {
                using var wire=await Wire.Accept(await listener.AcceptTcpClientAsync(deadline.Token),key,legacy?null:salt,deadline.Token);
                if(mode=="wrong-key") throw new Exception("Wrong client accepted");
                var received=await wire.Read(deadline.Token); Require(received.type==1 && Encoding.UTF8.GetString(received.data)=="control","Wrong client direction key");
                await wire.Send(2,new byte[100000],deadline.Token);
                if(mode=="replay") await wire.Read(deadline.Token);
            } catch(Exception ex) when(ex is CryptographicException or System.IO.IOException) { rejected=true; } });
            try {
                using var peer=new TcpClient(); await peer.ConnectAsync(IPAddress.Loopback,((IPEndPoint)listener.LocalEndpoint).Port,deadline.Token); var stream=peer.GetStream();
                byte[] hello=new byte[legacy?36:52]; await stream.ReadExactlyAsync(hello,deadline.Token);
                Require(hello.AsSpan(0,4).SequenceEqual(legacy?"RML1"u8:"RMP1"u8),"Wrong host version");
                if(!legacy) Require(hello.AsSpan(36).SequenceEqual(salt),"Wrong salt");
                var nonce=RandomNumberGenerator.GetBytes(32); var transcript=legacy?Join(hello[4..36],nonce):Join("RMP1"u8.ToArray(),salt,hello[4..36],nonce);
                var proof=H(key,"client",transcript); if(mode=="wrong-key") proof[0]^=1;
                foreach(byte b in Join(nonce,proof)) await stream.WriteAsync(new[]{b},deadline.Token);
                if(mode!="wrong-key") {
                    var hostProof=new byte[32]; await stream.ReadExactlyAsync(hostProof,deadline.Token); Require(CryptographicOperations.FixedTimeEquals(hostProof,H(key,"server",transcript)),"Independent host proof wrong");
                    byte[] plain=Join([1],"control"u8.ToArray()),cipher=new byte[plain.Length],tag=new byte[16],header=new byte[4];
                    BinaryPrimitives.WriteUInt32BigEndian(header,(uint)(plain.Length+16)); using var aes=new AesGcm(H(key,"c2h",transcript),16); aes.Encrypt(Nonce(0),plain,cipher,tag,header);
                    if(mode=="tamper") tag[0]^=1;
                    var packet=Join(header,cipher,tag); await stream.WriteAsync(mode=="oversize"?new byte[]{0,1,0,1}:packet,deadline.Token);
                    if(mode is "valid" or "replay") {
                        byte[] length=new byte[4]; await stream.ReadExactlyAsync(length,deadline.Token); var body=new byte[BinaryPrimitives.ReadUInt32BigEndian(length)]; await stream.ReadExactlyAsync(body,deadline.Token);
                        var output=new byte[body.Length-16]; using var decoder=new AesGcm(H(key,"h2c",transcript),16); decoder.Decrypt(Nonce(0),body.AsSpan(0,output.Length),body.AsSpan(output.Length),output,length);
                        Require(output.Length==100001 && output[0]==2,"Host video write bound/direction wrong");
                        if(mode=="replay") await stream.WriteAsync(packet,deadline.Token);
                    }
                }
                await server; Require(rejected==(mode!="valid"),"Auth/frame result wrong "+mode);
            } finally { listener.Stop(); CryptographicOperations.ZeroMemory(key); }
        }
        Console.WriteLine("PASS independent fragmented RMP1/RML1 controller → production host authentication, direction keys, 100KiB video, wrong proof/tamper/replay/64KiB input bound");
    }
    static void Credentials() {
        Require(HostCredential.Generate().Length==8,"New generated credential must have eight characters");
        _=HostCredential.CreateNew("Office#2");
        bool rejected=false;try {_=HostCredential.CreateNew("Office-LAN#42");} catch(ArgumentException) {rejected=true;}
        Require(rejected,"New long password accepted");
        var root=Path.Combine(Path.GetTempPath(),"localremote-host-store-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try {
            var path=Path.Combine(root,"sharing.dpapi"); var first=HostCredential.Load(path); var second=HostCredential.Load(path);
            Require(first.Password.Length==8 && first.Password==second.Password && first.Salt.SequenceEqual(second.Salt),"Generated sharing credential not persisted");
            Require(!Encoding.UTF8.GetString(File.ReadAllBytes(path)).Contains(first.Password),"Sharing password stored in plaintext");
            var changed=HostCredential.Create("Office-LAN#42"); changed.Save(path); var restored=HostCredential.Load(path);
            Require(restored.Password==changed.Password && restored.Salt.SequenceEqual(changed.Salt) && !restored.Salt.SequenceEqual(first.Salt),"Explicit password update/salt failed");
            Require(Directory.GetFiles(root).Length==1,"Atomic credential save left temporary files");
            var targetRoot=Path.Combine(root,"target-passwords");Directory.CreateDirectory(targetRoot);
            SecretStore.Save("fixture-target","Old#1234",targetRoot);
            var targetFile=Directory.GetFiles(targetRoot).Single();bool writeFailed=false;
            using(var locked=File.Open(targetFile,FileMode.Open,FileAccess.Read,FileShare.None)) {
                try {SecretStore.Save("fixture-target","New#1234",targetRoot);}catch(IOException) {writeFailed=true;}catch(UnauthorizedAccessException) {writeFailed=true;}
            }
            Require(writeFailed && SecretStore.Load("fixture-target",targetRoot)=="Old#1234" && Directory.GetFiles(targetRoot).Length==1,"Failed atomic target save damaged previous DPAPI credential or left a temporary file");
            SecretStore.Save("fixture-target","New#1234",targetRoot);
            Require(SecretStore.Load("fixture-target",targetRoot)=="New#1234","Atomic target credential update failed");
            Console.WriteLine("PASS isolated target DPAPI atomic replacement and locked-file failure preserves old password without temporary files");
            var quality=Path.Combine(root,"quality.dpapi");var reconnect=Path.Combine(root,"reconnect.dpapi");
            Require(SecretStore.LoadConnectionQuality(quality)==0 && SecretStore.LoadAutoReconnect(reconnect),"Missing defaults changed startup behavior");
            SecretStore.SaveConnectionQuality(2,quality);SecretStore.SaveAutoReconnect(false,reconnect);
            Require(SecretStore.LoadConnectionQuality(quality)==2 && !SecretStore.LoadAutoReconnect(reconnect),"Connection defaults not restored");
            File.WriteAllBytes(quality,new byte[]{255});Require(SecretStore.LoadConnectionQuality(quality)==0,"Corrupt quality outside valid range");
            try { SecretStore.SaveConnectionQuality(3,quality);throw new Exception("Invalid quality persisted"); } catch(ArgumentOutOfRangeException) {}
            const string fixtureAddress="192.168.1.24";
            var target=Path.Combine(root,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixtureAddress)))+".dpapi");
            var last=Path.Combine(root,"last-host.dpapi");var addressBytes=SecretStore.Protect(Encoding.UTF8.GetBytes(fixtureAddress));
            File.WriteAllBytes(target,SecretStore.Protect(Encoding.UTF8.GetBytes("Fixture#42")));File.WriteAllBytes(last,addressBytes);
            SecretStore.Forget(fixtureAddress,root);
            Require(!File.Exists(target) && File.ReadAllBytes(last).SequenceEqual(addressBytes) && File.Exists(path),"Forget password removed target address or local sharing credential");
            Console.WriteLine("PASS isolated connection-default persistence, missing/corrupt fallback and bounds");
            Console.WriteLine("PASS real current-user DPAPI sharing credential, restart persistence, explicit replacement and atomic cleanup (isolated path)");
        } finally { Directory.Delete(root,true); }
    }
    static async Task Lifecycle() {
        var salt=RandomNumberGenerator.GetBytes(16); var key=PairingPassword.Derive("Office-LAN#42",salt);
        string displayName="Office fixture";
        var area=new CaptureArea(0,0,320,180); var native=new RecordingInput(); var capture=new FakeCapture(); var injector=new WindowsInput(area,area,native);
        await using var host=new WindowsHost(key,salt,area,()=>capture,()=>injector,address:IPAddress.Loopback,port:0,deviceName:()=>displayName); host.Start();
        using(var wrong=new Wire()) { try { await wrong.Connect("127.0.0.1","wrong-password",CancellationToken.None,host.Port); throw new Exception("Wrong password accepted"); } catch(Exception e) when(e is CryptographicException or System.IO.IOException) {} }
        Require(capture.Starts==0,"Unauthenticated capture started"); await Task.Delay(100);
        for(int attempt=0;attempt<2;attempt++) {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(12)); using var client=new Wire();
            await client.Connect("127.0.0.1","Office-LAN#42",deadline.Token,host.Port); await client.Json(new {op="handshake",version=1,quality=0},deadline.Token);
            using(var caps=JsonDocument.Parse((await client.Read(deadline.Token)).data)) Require(caps.RootElement.GetProperty("platform").GetString()=="windows" && caps.RootElement.GetProperty("deviceName").GetString()==displayName,"Authenticated device metadata missing");
            await client.Read(deadline.Token); await client.Read(deadline.Token);
            displayName="Renamed fixture"; await client.Json(new {op="heartbeat"},deadline.Token);
            using(var changed=JsonDocument.Parse((await client.Read(deadline.Token)).data)) Require(changed.RootElement.GetProperty("deviceName").GetString()==displayName,"Live rename not returned without disconnect");
            using(var busy=new TcpClient()) { await busy.ConnectAsync(IPAddress.Loopback,host.Port); var probe=new byte[1]; Require(await busy.GetStream().ReadAsync(probe,deadline.Token)==0,"Second controller not rejected"); }
            await client.Json(new {op="key_event",vk=162,down=true,flags=2},deadline.Token); await Until(()=>injector.Held==1,"Input not received");
            await client.Json(new {op="disconnect"},deadline.Token); await Until(()=>capture.Stops==attempt+1 && injector.Held==0,"Disconnect did not clean capture/input");
        }
        Require(capture.Starts==2 && host.LastError!="capture_InvalidOperationException","Reconnect failed");
        Require(WindowsHost.IsLan(IPAddress.Parse("192.168.1.2")) && !WindowsHost.IsLan(IPAddress.Parse("8.8.8.8")) && !WindowsHost.IsLan(IPAddress.IPv6Loopback),"LAN filter wrong");
        Console.WriteLine("PASS opt-in listener, unauthorized capture denial, explicit handshake, one controller, disconnect/reconnect and held-key release");
    }
    static async Task Capture(bool real,int quality) {
        var area=real?CaptureArea.Primary():new CaptureArea(0,0,1920,1080); var profile=VideoProfile.For(area,quality);
        var capture=new FfmpegDesktopCapture(area,software:false,synthetic:!real);
        using var decoder=new FfmpegDecoder(profile.Width,profile.Height); int frames=0,units=0;
        var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        decoder.Frame+=_=> { if(Interlocked.Increment(ref frames)>=8) ready.TrySetResult(); };
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var salt=RandomNumberGenerator.GetBytes(16); var key=PairingPassword.Derive("Office-LAN#42",salt);
        await using var host=new WindowsHost(key,salt,area,()=>capture,()=>new WindowsInput(area,area,new RecordingInput()),address:IPAddress.Loopback,port:0);
        host.Start(); using var client=new Wire(); await client.Connect("127.0.0.1","Office-LAN#42",deadline.Token,host.Port);
        await client.Json(new {op="handshake",version=1,quality},deadline.Token);
        await client.Read(deadline.Token); await client.Read(deadline.Token);
        var run=Task.Run(async()=> { while(!deadline.IsCancellationRequested) {
            var packet=await client.Read(deadline.Token); Require(packet.type==2,"Capture packet not video");
            Interlocked.Increment(ref units); await decoder.Feed(packet.data,deadline.Token);
        }});
        try { await Task.WhenAny(ready.Task,run).WaitAsync(deadline.Token); if(!ready.Task.IsCompletedSuccessfully) await run; await ready.Task.WaitAsync(deadline.Token); }
        finally { deadline.Cancel(); client.Dispose(); try { await run; } catch(Exception e) when(e is OperationCanceledException or ObjectDisposedException or System.IO.IOException) {} await host.DisposeAsync(); }
        Require(frames>=8 && units>=8,"Capture/decode insufficient frames");
        try { using var p=Process.GetProcessById(capture.ProcessId); Require(p.HasExited,"Owned encoder still alive"); } catch(ArgumentException) {}
        Console.WriteLine($"PASS {(real?"real current Windows primary desktop (memory only)":"generated video")} → MF hardware H264/AUD → encrypted TCP → actual decoder: {profile.Width}x{profile.Height}@{profile.Fps}, {frames} frames / {units} units, process cleanup (short check, not sustained FPS)");
    }
    static async Task FailureCleanup() {
        var salt=RandomNumberGenerator.GetBytes(16); var key=PairingPassword.Derive("Office-LAN#42",salt);
        var area=new CaptureArea(0,0,320,180); var input=new WindowsInput(area,area,new RecordingInput());
        var failure=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host=new WindowsHost(key,salt,area,()=>new BrokenCapture(failure.Task),()=>input,address:IPAddress.Loopback,port:0); host.Start();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10)); using var client=new Wire();
        await client.Connect("127.0.0.1","Office-LAN#42",timeout.Token,host.Port); await client.Json(new {op="handshake",version=1,quality=0},timeout.Token);
        await client.Read(timeout.Token); await client.Read(timeout.Token);
        await client.Json(new {op="key_event",vk=162,down=true},timeout.Token); await Until(()=>input.Held==1,"Fault fixture did not receive key");
        failure.TrySetResult(); await Until(()=>input.Held==0 && host.LastError=="capture_InvalidOperationException","Unexpected capture failure leaked input/session");
        await host.DisposeAsync();
        using var reconnect=new TcpClient(); bool refused=false; try { await reconnect.ConnectAsync(IPAddress.Loopback,host.Port,timeout.Token); } catch(Exception e) when(e is SocketException or InvalidOperationException) { refused=true; }
        Require(refused,"Stopped sharing listener still available");
        Console.WriteLine("PASS unexpected capture failure cancels socket and releases held keys; explicit stop closes listener");
    }
}
sealed class RecordingInput:INativeInput {
    internal List<NativeInput> Values=[]; internal bool Block;
    public bool Post(NativeInput value) { if(Block) return false; Values.Add(value); return true; }
}
sealed class FakeCapture:IDesktopCapture {
    internal int Starts,Stops;
    public async Task Run(VideoProfile p,Func<byte[],Task> frame,CancellationToken ct) {
        Interlocked.Increment(ref Starts);
        try { await frame([0,0,0,1,9,0xf0,0,0,0,1,5,0]); await Task.Delay(Timeout.Infinite,ct); }
        finally { Interlocked.Increment(ref Stops); }
    }
}
sealed class BrokenCapture(Task fail):IDesktopCapture {
    public async Task Run(VideoProfile p,Func<byte[],Task> frame,CancellationToken ct) { await fail.WaitAsync(ct); throw new InvalidOperationException("Fixture encoder failure"); }
}

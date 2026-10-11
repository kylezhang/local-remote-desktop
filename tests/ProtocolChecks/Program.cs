using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using LocalRemoteDesktop;
static byte[] Join(params byte[][] a)=>a.SelectMany(x=>x).ToArray();
static byte[] H(byte[] k,string label,byte[] transcript)=>HMACSHA256.HashData(k,Join(Encoding.ASCII.GetBytes(label),transcript));
static byte[] N(ulong n) { var r=new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(r.AsSpan(4),n); return r; }
static byte[] Packet(byte[] key,byte[] payload,ulong counter,bool corrupt=false) {
    var header=new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(header,(uint)(payload.Length+16));
    var cipher=new byte[payload.Length]; var tag=new byte[16]; using var aes=new AesGcm(key,16); aes.Encrypt(N(counter),payload,cipher,tag,header);
    if(corrupt) tag[0]^=1; return Join(header,cipher,tag);
}
static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
await PasswordChecks.Run();
foreach(var mode in new[]{"valid","tamper","replay","wrong-key","oversize"}) {
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10)); var ct=timeout.Token;
    var key=RandomNumberGenerator.GetBytes(32); var listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
    try {
        var server=Task.Run(async()=> {
            using var peer=await listener.AcceptTcpClientAsync(ct); var stream=peer.GetStream();
            var nonce=RandomNumberGenerator.GetBytes(32); await stream.WriteAsync(Join("RML1"u8.ToArray(),nonce),ct);
            var response=new byte[64]; await stream.ReadExactlyAsync(response,ct); var transcript=Join(nonce,response[..32]);
            if(mode=="wrong-key") { Assert(!CryptographicOperations.FixedTimeEquals(response[32..],H(key,"client",transcript)),"wrong key accepted"); return; }
            Assert(CryptographicOperations.FixedTimeEquals(response[32..],H(key,"client",transcript)),"client proof invalid");
            await stream.WriteAsync(H(key,"server",transcript),ct);
            var header=new byte[4]; await stream.ReadExactlyAsync(header,ct); var body=new byte[(int)BinaryPrimitives.ReadUInt32BigEndian(header)]; await stream.ReadExactlyAsync(body,ct);
            var plain=new byte[body.Length-16]; using(var aes=new AesGcm(H(key,"c2h",transcript),16)) aes.Decrypt(N(0),body.AsSpan(0,plain.Length),body.AsSpan(plain.Length),plain,header);
            Assert(plain[0]==1 && Encoding.UTF8.GetString(plain[1..]).Contains("heartbeat"),"control payload mismatch");
            if(mode=="oversize") { await stream.WriteAsync(new byte[]{0,128,0,1},ct); return; }
            var p=Packet(H(key,"h2c",transcript),Join([2],"sample"u8.ToArray()),0,mode=="tamper");
            // Exercise TCP fragmentation; no assumptions about recv boundaries.
            foreach(var b in p) await stream.WriteAsync(new byte[]{b},ct);
            if(mode=="replay") await stream.WriteAsync(p,ct);
        },ct);
        using var wire=new Wire(); bool rejected=false;
        try {
            await wire.Connect("127.0.0.1",Convert.ToHexString(mode=="wrong-key"?RandomNumberGenerator.GetBytes(32):key),ct,((IPEndPoint)listener.LocalEndpoint).Port);
            await wire.Json(new {op="heartbeat"},ct); var packet=await wire.Read(ct);
            Assert(packet.type==2 && Encoding.UTF8.GetString(packet.data)=="sample","video payload mismatch");
            if(mode=="replay") await wire.Read(ct);
        } catch(Exception e) when(e is CryptographicException or IOException) { rejected=true; }
        await server;
        Assert(rejected==(mode!="valid"),$"unexpected outcome {mode}"); Console.WriteLine($"PASS authenticated transport: {mode}");
    } finally { listener.Stop(); }
}
var directory=Path.Combine(Path.GetTempPath(),"localremote-check-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
try {
    var sample=Path.Combine(directory,"sample.h264");
    var start=new ProcessStartInfo("ffmpeg.exe") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
    foreach(var arg in new[]{"-hide_banner","-loglevel","error","-f","lavfi","-i","testsrc2=size=320x180:rate=60","-t","2","-c:v","libx264","-preset","ultrafast","-tune","zerolatency","-profile:v","baseline","-f","h264",sample}) start.ArgumentList.Add(arg);
    using(var generate=Process.Start(start)!) { var err=await generate.StandardError.ReadToEndAsync(); await generate.WaitForExitAsync(); Assert(generate.ExitCode==0,err); }
    using var decoder=new FfmpegDecoder(320,180); var got=new TaskCompletionSource(); int count=0;
    decoder.Frame+=pixels=> { Assert(pixels.Length==320*180*4,"wrong BGRA size"); if(Interlocked.Increment(ref count)>=60) got.TrySetResult(); };
    using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(10)); await decoder.Feed(await File.ReadAllBytesAsync(sample),cancel.Token); await got.Task.WaitAsync(cancel.Token);
    Console.WriteLine($"PASS actual H.264 → BGRA decoding: at least {count} frames, 320x180");
} finally { Directory.Delete(directory,true); }
// Synthetic credentials in isolated storage; never touch user pairing settings.
var fake="test-"+Guid.NewGuid().ToString("N"); var secret=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var pairingRoot=Path.Combine(Path.GetTempPath(),"localremote-protocol-pairing-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(pairingRoot);
try {
    SecretStore.Save(fake,secret,pairingRoot); Assert(SecretStore.Load(fake,pairingRoot)==secret,"DPAPI roundtrip");
    SecretStore.Save(fake,"Office-LAN#42",pairingRoot); Assert(SecretStore.Load(fake,pairingRoot)=="Office-LAN#42","short password DPAPI roundtrip");
} finally {Directory.Delete(pairingRoot,true);}
Console.WriteLine("PASS Windows current-user DPAPI roundtrip");

var migrationRoot=Path.Combine(Path.GetTempPath(),"localremote-migration-"+Guid.NewGuid().ToString("N"));
try {
    var legacy=Path.Combine(migrationRoot,"RemoteMacLAN"); var current=Path.Combine(migrationRoot,"LocalRemote");
    Directory.CreateDirectory(legacy); Directory.CreateDirectory(current);
    var blobName=new string('A',64)+".dpapi";
    var dpapiFile=Path.Combine(migrationRoot,"encrypted.dpapi");
    SecretStore.SaveWindowsShortcuts(false,dpapiFile);
    var opaque=File.ReadAllBytes(dpapiFile);
    File.WriteAllBytes(Path.Combine(legacy,blobName),opaque);
    File.WriteAllBytes(Path.Combine(legacy,"last-host.dpapi"),opaque);
    File.WriteAllBytes(Path.Combine(legacy,"windows-shortcuts.dpapi"),opaque);
    File.WriteAllText(Path.Combine(legacy,"unrelated.dpapi"),"fixture, not a connection");
    File.WriteAllText(Path.Combine(current,"last-host.dpapi"),"new settings must win");
    SecretStore.MigrateLegacyDirectory(legacy,current);
    Assert(File.ReadAllBytes(Path.Combine(current,blobName)).SequenceEqual(opaque),"DPAPI ciphertext changed during migration");
    Assert(!SecretStore.LoadWindowsShortcuts(Path.Combine(current,"windows-shortcuts.dpapi")),"migrated ciphertext cannot be decrypted");
    Assert(File.ReadAllText(Path.Combine(current,"last-host.dpapi"))=="new settings must win","migration overwrote current settings");
    Assert(!File.Exists(Path.Combine(current,"unrelated.dpapi")),"unrelated file migrated");
    Assert(File.Exists(Path.Combine(legacy,blobName)),"legacy store was deleted");
    File.Delete(Path.Combine(current,blobName));
    SecretStore.MigrateLegacyDirectory(legacy,current);
    Assert(!File.Exists(Path.Combine(current,blobName)),"forgotten connection was resurrected by migration");
    Console.WriteLine("PASS LocalRemoteDesktop encrypted storage migration: decryptable blobs, no overwrite, legacy retained, no resurrection");
} finally { Directory.Delete(migrationRoot,true); }

// Input floods must not drop button/key transitions or move across them.
var queue=new InputQueue(4);
for(int i=0;i<10000;i++) Assert(queue.TryWrite(new {op="mouse_move",x=i/10000.0,y=0.5}),"move flood filled queue");
Assert(queue.TryWrite(new {op="mouse_button",button=0,down=true}),"button rejected");
Assert(queue.TryWrite(new {op="mouse_move",x=1.0,y=1.0}),"drag rejected");
Assert(queue.TryWrite(new {op="key_event",vk=49,down=true}),"key rejected");
Assert(!queue.TryWrite(new {op="key_event",vk=49,down=false}),"overflow should be reported");
queue.Complete(); var collected=new List<System.Text.Json.JsonElement>();
await foreach(var item in queue.ReadAllAsync(CancellationToken.None)) collected.Add(System.Text.Json.JsonSerializer.SerializeToElement(item));
Assert(collected.Count==4 && collected[0].GetProperty("x").GetDouble()==0.9999,"move coalescing");
Assert(collected[1].GetProperty("op").GetString()=="mouse_button" && collected[2].GetProperty("op").GetString()=="mouse_move","drag order");
queue=new InputQueue(1); queue.TryWrite(new {op="key_event",vk=17,down=true});
Assert(queue.TryWrite(new {op="release_all"}),"release must bypass overload"); queue.Complete();
await foreach(var item in queue.ReadAllAsync(CancellationToken.None)) Assert(System.Text.Json.JsonSerializer.SerializeToElement(item).GetProperty("op").GetString()=="release_all","stale key survived release");
Assert(!queue.TryWrite(new {op="mouse_move",x=0,y=0}),"closed queue accepts input");
Console.WriteLine("PASS 10,000 mouse moves, ordered click/drag/key, overload and release safety");
Assert(RemoteCoordinates.Map(100,50,200,100,1920,1080) is {x:0.5,y:0.5},"center coordinate");
Assert(RemoteCoordinates.Map(100,0,200,200,1920,1080)==null,"letterbox click should be ignored");
Assert(RemoteCoordinates.Map(-10,300,200,200,1920,1080,true) is {x:0,y:1},"drag should clamp beyond viewport");
Assert(RemoteCoordinates.Map(0,0,0,0,1920,1080)==null && RemoteCoordinates.Map(double.NaN,0,200,200,1920,1080)==null,"invalid coordinates accepted");
Console.WriteLine("PASS letterbox coordinate conversion, outside drag clamping, invalid size/NaN safety");
foreach(int modifiers in Enumerable.Range(0,16)) {
    foreach(var (vk,expected) in new[]{(17,91),(162,91),(163,92),(91,162),(92,163),(67,67),(86,86),(16,16),(164,164),(96,96)}) {
        var result=KeyboardMapping.Map(vk,modifiers,true);
        Assert(result.VirtualKey==expected,"modifier VK side lost");
        Assert((result.Flags&5)==(modifiers&5),"Shift/Option changed");
        Assert(((result.Flags&8)!=0)==((modifiers&2)!=0) && ((result.Flags&2)!=0)==((modifiers&8)!=0),"Control/Command flags inconsistent");
        Assert(KeyboardMapping.Map(vk,modifiers,false)==(vk,modifiers),"native mapping changed");
    }
}
Console.WriteLine("PASS Windows/native shortcut mappings: both modifier sides, all Shift/Control/Alt/Win combinations");
var preferencePath=Path.Combine(Path.GetTempPath(),"localremote-preference-"+Guid.NewGuid().ToString("N")+".dpapi");
try {
    Assert(SecretStore.LoadFileSync(preferencePath) && SecretStore.LoadClipboardSync(preferencePath),"fresh sharing preferences should default on");
    Assert(SecretStore.LoadWindowsShortcuts(preferencePath),"missing shortcut preference should default on");
    SecretStore.SaveWindowsShortcuts(false,preferencePath); Assert(!SecretStore.LoadWindowsShortcuts(preferencePath),"native preference not restored");
    SecretStore.SaveWindowsShortcuts(true,preferencePath); Assert(SecretStore.LoadWindowsShortcuts(preferencePath),"Windows preference not restored");
    File.WriteAllBytes(preferencePath,new byte[]{0}); Assert(SecretStore.LoadWindowsShortcuts(preferencePath),"corrupt preference did not recover to default");
    Assert(!SecretStore.LoadClipboardSync(preferencePath),"corrupt text-sharing preference must default off");
    SecretStore.SaveClipboardSync(false,preferencePath); Assert(!SecretStore.LoadClipboardSync(preferencePath),"text revocation not remembered");
    SecretStore.SaveClipboardSync(true,preferencePath); Assert(SecretStore.LoadClipboardSync(preferencePath),"text permission not remembered");
    File.WriteAllBytes(preferencePath,new byte[]{0});
    Assert(!SecretStore.LoadFileSync(preferencePath),"corrupt file-sharing preference must default off");
    SecretStore.SaveFileSync(true,preferencePath); Assert(SecretStore.LoadFileSync(preferencePath),"file permission not remembered");
    SecretStore.SaveFileSync(false,preferencePath); Assert(!SecretStore.LoadFileSync(preferencePath),"file revocation not remembered");
} finally { File.Delete(preferencePath); }
Console.WriteLine("PASS clipboard defaults/explicit-off/corruption and shortcut preference: current-user DPAPI roundtrip, missing/corrupt recovery, isolated from user settings");

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using RemoteMacLAN;
static byte[] Join(params byte[][] a)=>a.SelectMany(x=>x).ToArray();
static byte[] H(byte[] k,string label,byte[] transcript)=>HMACSHA256.HashData(k,Join(Encoding.ASCII.GetBytes(label),transcript));
static byte[] N(ulong n) { var r=new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(r.AsSpan(4),n); return r; }
static byte[] Packet(byte[] key,byte[] payload,ulong counter,bool corrupt=false) {
    var header=new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(header,(uint)(payload.Length+16));
    var cipher=new byte[payload.Length]; var tag=new byte[16]; using var aes=new AesGcm(key,16); aes.Encrypt(N(counter),payload,cipher,tag,header);
    if(corrupt) tag[0]^=1; return Join(header,cipher,tag);
}
static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
foreach(var mode in new[]{"valid","tamper","replay","wrong-key","oversize"}) {
    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10)); var ct=timeout.Token;
    var key=RandomNumberGenerator.GetBytes(32); var listener=new TcpListener(IPAddress.Loopback,47980); listener.Start();
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
            await wire.Connect("127.0.0.1",Convert.ToHexString(mode=="wrong-key"?RandomNumberGenerator.GetBytes(32):key),ct);
            await wire.Json(new {op="heartbeat"},ct); var packet=await wire.Read(ct);
            Assert(packet.type==2 && Encoding.UTF8.GetString(packet.data)=="sample","video payload mismatch");
            if(mode=="replay") await wire.Read(ct);
        } catch(Exception e) when(e is CryptographicException or IOException) { rejected=true; }
        await server;
        Assert(rejected==(mode!="valid"),$"unexpected outcome {mode}"); Console.WriteLine($"PASS authenticated transport: {mode}");
    } finally { listener.Stop(); }
}
var directory=Path.Combine(Path.GetTempPath(),"rml-check-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
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
// A unique synthetic host/key exercises DPAPI without touching a real pairing.
var fake="test-"+Guid.NewGuid().ToString("N"); var secret=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
SecretStore.Save(fake,secret); Assert(SecretStore.Load(fake)==secret,"DPAPI roundtrip");
var store=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RemoteMacLAN",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fake)))+".dpapi"); File.Delete(store);
Console.WriteLine("PASS Windows current-user DPAPI roundtrip");

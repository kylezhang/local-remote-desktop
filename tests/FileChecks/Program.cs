using System.Security.Cryptography;
using LocalRemoteDesktop;
static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
static void Reject(Action action,string message) { try { action(); } catch(IOException) { return; } throw new Exception(message); }
if(Environment.GetEnvironmentVariable("LOCALREMOTE_FILE_BENCH") is { Length: >0 } benchmark) { await TransferBenchmark.Run(benchmark); return; }
var root=Path.Combine(Path.GetTempPath(),"localremote-files-check-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
try {
    Assert(FilePacing.DelaySeconds(FilePacing.Burst,0)==0,"small transfer is deliberately delayed");
    Assert(FilePacing.DelaySeconds(8*1024*1024,2)==0,"slow disk/network incurs additional pacing wait");
    Assert(FilePacing.DelaySeconds(8*1024*1024,0.1)>0.8,"fast sender can exceed sustained budget");
    Assert(FileTransfer.ProgressText("发送",10,10,1).Contains("正在校验") && !FileTransfer.ProgressText("发送",10,10,1).Contains("可"),"byte completion promises clipboard readiness");
    Assert(FileTransfer.ProgressText("发送",5,10,1).Contains("50%"),"fractional progress missing");
    Console.WriteLine("PASS small-file burst, slow-network accounting, sustained pacing and preparation-vs-ready status");
    foreach(string name in new[]{"../oops","a/b","a\\b","NUL.txt","COM1","COM¹.txt","CON .txt","CONIN$","LPT9.txt","a:","a.","a ","","..","e\u0301.txt"}) Assert(!FileSource.SafeName(name),"Unsafe name accepted: "+name);
    using(var error=System.Text.Json.JsonDocument.Parse("{\"op\":\"file_error\",\"code\":\"diskFull\"}")) {
        try { FileTransfer.Expect(error.RootElement,"file_ready"); throw new Exception("Host error ignored"); }
        catch(IOException ex) { Assert(ex.Message.Contains("磁盘空间不足"),"Host error not readable"); }
    }
    Assert(FileSource.SafeName("报告 📝.txt"),"Unicode filename rejected");
    Reject(()=>FileSource.Validate([new("a",0),new("A",0)]),"Duplicate accepted");
    Reject(()=>FileSource.Validate([new("a",FileSource.MaxFile+1)]),"File quota accepted");
    Reject(()=>FileSource.Validate([new("a",FileSource.MaxFile),new("b",FileSource.MaxFile),new("c",1)]),"Batch quota accepted");
    Reject(()=>FileSource.Validate(Enumerable.Range(0,33).Select(i=>new FileItem(i+".txt",0)).ToArray()),"Count limit accepted");
    Reject(()=>FileSource.Prepare([root]),"Directory accepted");
    var sources=Path.Combine(root,"source"); Directory.CreateDirectory(sources);
    string a=Path.Combine(sources,"报告 📝.txt"), b=Path.Combine(sources,"empty.txt");
    byte[] content=RandomNumberGenerator.GetBytes(100003); File.WriteAllBytes(a,content); File.WriteAllBytes(b,[]);
    var source=FileSource.Prepare([a,b]); var receive=Path.Combine(root,"received");
    using(var sink=new FileSink(receive,source.Items)) {
        sink.Start(0); for(int offset=0;offset<content.Length;offset+=32768) sink.Write(content[offset..Math.Min(content.Length,offset+32768)]);
        sink.End(0,Convert.ToHexString(SHA256.HashData(content))); sink.Start(1); sink.End(1,Convert.ToHexString(SHA256.HashData([])));
        var paths=sink.Commit(); Assert(File.ReadAllBytes(paths[0]).SequenceEqual(content) && new FileInfo(paths[1]).Length==0,"Commit corrupted bytes");
    }
    int complete=Directory.GetDirectories(receive).Length;
    using(var sink=new FileSink(receive,[new("bad.txt",1)])) { sink.Start(0); sink.Write([1]); Reject(()=>sink.End(0,new string('0',64)),"Wrong checksum accepted"); }
    using(var sink=new FileSink(receive,[new("short.txt",2)])) { sink.Start(0); sink.Write([1]); Reject(()=>sink.End(0,Convert.ToHexString(SHA256.HashData([1]))),"Truncation accepted"); }
    using(var sink=new FileSink(receive,[new("extra.txt",0)])) { Reject(()=>sink.Start(1),"Index accepted"); sink.Start(0); Reject(()=>sink.Write([1]),"Oversize chunk accepted"); }
    Assert(Directory.GetDirectories(receive).Length==complete,"Partial transfer left files or deleted completed batch");
    Console.WriteLine("PASS filenames/quotas/directory rejection, Unicode/empty/multi-chunk SHA256, corruption/truncation/order rejection and partial cleanup");
    byte[] key=RandomNumberGenerator.GetBytes(32); string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    using(var server=new MockFileServer(key,token,id=>id==7) { Offer=source }) {
        var running=server.Run(); using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        long progress=0;
        Assert(await FileTransfer.Upload("127.0.0.1",Convert.ToHexString(key),token,7,source,(done,total)=>progress=done,timeout.Token,server.Port),"Upload not ready");
        Assert(progress==content.Length && server.Uploaded.TryDequeue(out var sent) && sent[0].SequenceEqual(content) && sent[1].Length==0,"Encrypted upload corrupted");
        var paths=await FileTransfer.Download("127.0.0.1",Convert.ToHexString(key),token,7,server.OfferId,source.Items,receive,null,timeout.Token,server.Port);
        Assert(File.ReadAllBytes(paths[0]).SequenceEqual(content) && new FileInfo(paths[1]).Length==0,"Encrypted download corrupted");
        await Task.Delay(50);
        Assert(server.DownloadClipboardReady.TryDequeue(out var published) && !published,"bare receiver claimed OS clipboard publication");
        var callbackPaths=await FileTransfer.Download("127.0.0.1",Convert.ToHexString(key),token,7,server.OfferId,source.Items,receive,null,timeout.Token,server.Port,async (_,ct)=> { await Task.Delay(150,ct); return true; });
        await Task.Delay(50);
        Assert(server.DownloadClipboardReady.TryDequeue(out published) && published,"successful clipboard callback not acknowledged");
        foreach(var request in new[]{(new string('0',64),7L),(token,8L)}) {
            bool denied=false; try { await FileTransfer.Upload("127.0.0.1",Convert.ToHexString(key),request.Item1,request.Item2,source,null,timeout.Token,server.Port); } catch(IOException) { denied=true; }
            Assert(denied,"Wrong session/request was accepted");
        }
        server.Dispose(); await running;
    }
    Console.WriteLine("PASS automatic mutual authentication, encrypted upload/download, progress and stale session/consent rejection");
    using(var server=new MockFileServer(key,token,_=>true) { Offer=source,DelayMilliseconds=500 }) {
        var running=server.Run(); using var cancel=new CancellationTokenSource(150);
        try { await FileTransfer.Download("127.0.0.1",Convert.ToHexString(key),token,1,server.OfferId,source.Items,receive,null,cancel.Token,server.Port); throw new Exception("Cancel failed"); }
        catch(OperationCanceledException) { }
        Assert(!Directory.GetDirectories(receive).Any(p=>Path.GetFileName(p).StartsWith(".incoming-")),"Cancelled download retained partial batch");
        server.Dispose(); await running;
    }
    var pacedPath=Path.Combine(sources,"paced.bin"); File.WriteAllBytes(pacedPath,new byte[8*1024*1024]);
    using(var server=new MockFileServer(key,token,_=>true)) {
        var running=server.Run(); using var cancel=new CancellationTokenSource(200);
        try { await FileTransfer.Upload("127.0.0.1",Convert.ToHexString(key),token,1,FileSource.Prepare([pacedPath]),null,cancel.Token,server.Port); throw new Exception("paced upload cancellation failed"); }
        catch(OperationCanceledException) { }
        Assert(server.Uploaded.IsEmpty,"cancelled paced upload completed"); server.Dispose(); await running;
    }
    Console.WriteLine("PASS cancellation interrupts paced upload");
    // A stalled handshake times out independently of an unbounded parent token.
    var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0); listener.Start();
    try {
        var accept=listener.AcceptTcpClientAsync(); using var wire=new Wire(65536,1);
        var connect=wire.Connect("127.0.0.1",Convert.ToHexString(key),CancellationToken.None,((System.Net.IPEndPoint)listener.LocalEndpoint).Port); using var peer=await accept;
        bool timedOut=false; try { await connect; } catch(OperationCanceledException) { timedOut=true; }
        Assert(timedOut,"Stalled handshake has no deadline");
    } finally { listener.Stop(); }
    Console.WriteLine("PASS cancellation clears partial files; stalled file authentication times out");
} finally { Directory.Delete(root,true); }

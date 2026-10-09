using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using LocalRemote;
static class TransferBenchmark {
    public static async Task Run(string report) {
        var root=Path.Combine(Path.GetTempPath(),"localremote-transfer-bench-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try {
            var key=RandomNumberGenerator.GetBytes(32); string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            using var peer=new MockFileServer(key,token,_=>true); var running=peer.Run();
            var results=new List<object>();
            foreach(int size in new[]{256*1024,8*1024*1024}) {
                var bytes=RandomNumberGenerator.GetBytes(size); var path=Path.Combine(root,"sample.bin"); File.WriteAllBytes(path,bytes);
                for(int round=0;round<3;round++) {
                    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var elapsed=Stopwatch.StartNew();
                    var ready=await FileTransfer.Upload("127.0.0.1",Convert.ToHexString(key),token,1,FileSource.Prepare([path]),null,timeout.Token,peer.Port);
                    elapsed.Stop();
                    if(!ready || !peer.Uploaded.TryDequeue(out var received) || !received[0].SequenceEqual(bytes)) throw new Exception("benchmark bytes/ready differ");
                    results.Add(new {size,round,milliseconds=elapsed.Elapsed.TotalMilliseconds,mibPerSecond=size/1048576.0/elapsed.Elapsed.TotalSeconds});
                    Console.WriteLine($"BENCH encrypted upload {size/1024} KiB: {elapsed.Elapsed.TotalMilliseconds:F1} ms, {size/1048576.0/elapsed.Elapsed.TotalSeconds:F2} MiB/s");
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
            File.WriteAllText(report,JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));
            peer.Dispose(); await running;
        } finally { Directory.Delete(root,true); }
    }
}

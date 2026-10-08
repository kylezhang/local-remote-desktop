using System.Diagnostics;
using System.Text.Json;
using RemoteMacLAN;

var directory=Path.Combine(Path.GetTempPath(),"rml-latency-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
try {
    var file=Path.Combine(directory,"test.h264");
    var generate=new ProcessStartInfo("ffmpeg.exe") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true };
    foreach(var arg in new[]{"-hide_banner","-loglevel","error","-f","lavfi","-i","testsrc2=size=1280x720:rate=30","-t","5","-c:v","libx264","-preset","ultrafast","-tune","zerolatency","-profile:v","baseline","-x264-params","aud=1:keyint=30:repeat-headers=1","-f","h264",file}) generate.ArgumentList.Add(arg);
    using(var p=Process.Start(generate)!) { var error=await p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync(); if(p.ExitCode!=0) throw new Exception(error); }
    var bytes=await File.ReadAllBytesAsync(file); var boundaries=new List<int>();
    for(int i=0;i<bytes.Length-5;i++) {
        int prefix=bytes[i]==0 && bytes[i+1]==0 && bytes[i+2]==0 && bytes[i+3]==1?4:bytes[i]==0 && bytes[i+1]==0 && bytes[i+2]==1?3:0;
        if(prefix>0) { if((bytes[i+prefix]&31)==9) boundaries.Add(i); i+=prefix; }
    }
    if(boundaries.Count!=150) throw new Exception($"expected 150 access units, got {boundaries.Count}"); boundaries.Add(bytes.Length);
    var units=Enumerable.Range(0,150).Select(i=>bytes[boundaries[i]..boundaries[i+1]]).ToArray();
    var results=new List<object>();
    // Identical source and paced arrival for both paths; this measures pipe/feed
    // through BGRA output, not capture, network, WPF display or Mac latency.
    foreach(bool legacy in new[]{true,false}) {
        using IVideoDecoder decoder=legacy?new LegacyDecoder():new FfmpegDecoder(1280,720);
        var clock=Stopwatch.StartNew(); var timestamps=new double[150]; var delays=new List<double>(); var gate=new object(); int frame=0;
        decoder.Frame+=pixels=> { lock(gate) { int index=frame++; if(index<150 && index>=10) delays.Add(clock.Elapsed.TotalMilliseconds-Volatile.Read(ref timestamps[index])); } };
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        for(int i=0;i<150;i++) {
            var pause=i*1000.0/30-clock.Elapsed.TotalMilliseconds; if(pause>0) await Task.Delay(TimeSpan.FromMilliseconds(pause),deadline.Token);
            Volatile.Write(ref timestamps[i],clock.Elapsed.TotalMilliseconds); await decoder.Feed(units[i],deadline.Token);
        }
        await Task.Delay(300,deadline.Token);
        double[] values; int decoded; lock(gate) { values=delays.Order().ToArray(); decoded=frame; }
        if(values.Length<60) throw new Exception($"insufficient decoded samples: {decoded}");
        results.Add(new {path=legacy?"previous-auto-frame-threads":"single-thread-low-delay",width=1280,height=720,sourceFps=30,decodedFrames=decoded,samples=values.Length,medianFeedToBgraMs=values[values.Length/2],p95FeedToBgraMs=values[(int)((values.Length-1)*0.95)]});
    }
    Console.WriteLine(JsonSerializer.Serialize(new {test="synthetic-paced-decoder-comparison",note="not desktop end-to-end latency; no actual Mac involved",results},new JsonSerializerOptions {WriteIndented=true}));
} finally { Directory.Delete(directory,true); }

sealed class LegacyDecoder:IVideoDecoder {
    readonly Process process; readonly CancellationTokenSource stop=new();
    public event Action<byte[]>? Frame;
    public Task Completion {get;}
    public LegacyDecoder() {
        var start=new ProcessStartInfo("ffmpeg.exe") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true };
        foreach(var arg in new[]{"-hide_banner","-loglevel","error","-probesize","32","-analyzeduration","0","-f","h264","-i","pipe:0","-an","-fps_mode","passthrough","-f","rawvideo","-pix_fmt","bgra","pipe:1"}) start.ArgumentList.Add(arg);
        process=Process.Start(start)!; _=process.StandardError.ReadToEndAsync();
        Completion=Task.Run(async()=> { while(true) { var pixels=new byte[1280*720*4]; await process.StandardOutput.BaseStream.ReadExactlyAsync(pixels,stop.Token); Frame?.Invoke(pixels); } });
    }
    public Task Feed(byte[] bytes,CancellationToken ct)=>process.StandardInput.BaseStream.WriteAsync(bytes,ct).AsTask();
    public void Dispose() { stop.Cancel(); try { if(!process.HasExited) process.Kill(true); } catch {} process.Dispose(); }
}

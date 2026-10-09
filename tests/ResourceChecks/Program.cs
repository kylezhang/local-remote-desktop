using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using LocalRemote;

static void Assert(bool value,string message) { if(!value) throw new Exception(message); }
// Exercise production draining with a large logical stream without allocating
// that stream: bounded reads/allocations and cancellation are the behavior under test.
var noise=new NoiseReader(256L*1024*1024);
long before=GC.GetAllocatedBytesForCurrentThread();
await FfmpegDecoder.DrainDiagnostics(noise,CancellationToken.None);
Assert(noise.Remaining==0 && noise.MaxRead<=2048,"diagnostic draining is not bounded");
Assert(GC.GetAllocatedBytesForCurrentThread()-before<1024*1024,"diagnostic draining retained/allocated excessive text");
using(var stop=new CancellationTokenSource(50))
    await FfmpegDecoder.DrainDiagnostics(new WaitingReader(),stop.Token).WaitAsync(TimeSpan.FromSeconds(2));
foreach(var (w,h) in new[]{(0,180),(320,-1),(3841,2160),(3840,2161)}) {
    bool rejected=false; try { using var invalid=new FfmpegDecoder(w,h); } catch(ArgumentOutOfRangeException) { rejected=true; }
    Assert(rejected,"invalid dimensions started a decoder");
}
Console.WriteLine("PASS bounded diagnostic drain (256 MiB logical stream), cancellation and dimensions");

int seconds=args.Length>0?int.Parse(args[0]):40;
Assert(seconds>=20 && seconds<=600,"Use 20–600 seconds per quality case");
var directory=Path.Combine(Path.GetTempPath(),"localremote-resource-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var cases=new List<object>();
try {
    foreach(var (width,height,fps) in new[]{(1280,720,30),(1920,1080,60)}) {
        var source=Path.Combine(directory,$"{width}.h264");
        var start=new ProcessStartInfo("ffmpeg.exe") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true };
        foreach(var arg in new[]{"-hide_banner","-loglevel","error","-f","lavfi","-i",$"testsrc2=size={width}x{height}:rate={fps}","-t","2","-c:v","libx264","-preset","ultrafast","-tune","zerolatency","-profile:v","baseline","-x264-params",$"aud=1:keyint={fps}:repeat-headers=1","-f","h264",source}) start.ArgumentList.Add(arg);
        using(var generator=Process.Start(start)!) {
            var error=await generator.StandardError.ReadToEndAsync(); await generator.WaitForExitAsync(); Assert(generator.ExitCode==0,error);
        }
        var bytes=await File.ReadAllBytesAsync(source);
        var boundaries=new List<int>();
        for(int i=0;i<bytes.Length-5;i++) {
            int prefix=bytes[i]==0 && bytes[i+1]==0 && bytes[i+2]==0 && bytes[i+3]==1?4:bytes[i]==0 && bytes[i+1]==0 && bytes[i+2]==1?3:0;
            if(prefix>0) { if((bytes[i+prefix]&31)==9) boundaries.Add(i); i+=prefix; }
        }
        Assert(boundaries.Count==fps*2,"unexpected sample access units"); boundaries.Add(bytes.Length);
        var units=Enumerable.Range(0,fps*2).Select(i=>bytes[boundaries[i]..boundaries[i+1]]).ToArray();
        var samples=new List<object>();
        using var decoder=new FfmpegDecoder(width,height);
        var child=(Process)typeof(FfmpegDecoder).GetField("process",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(decoder)!;
        int childId=child.Id, decoded=0;
        decoder.Frame+=pixels=> { Assert(pixels.Length==width*height*4,"invalid frame"); Interlocked.Increment(ref decoded); };
        using var parent=Process.GetCurrentProcess();
        parent.Refresh(); child.Refresh();
        double parentInitial=parent.TotalProcessorTime.TotalMilliseconds, childInitial=child.TotalProcessorTime.TotalMilliseconds;
        var clock=Stopwatch.StartNew(); int sent=0, nextSample=1;
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(seconds+15));
        while(clock.Elapsed.TotalSeconds<seconds) {
            var pause=sent*1000.0/fps-clock.Elapsed.TotalMilliseconds;
            if(pause>0) await Task.Delay(TimeSpan.FromMilliseconds(pause),deadline.Token);
            await decoder.Feed(units[sent++%units.Length],deadline.Token);
            if(clock.Elapsed.TotalSeconds>=nextSample) {
                parent.Refresh(); child.Refresh();
                samples.Add(new {elapsedSeconds=Math.Round(clock.Elapsed.TotalSeconds,2),decodedFrames=Volatile.Read(ref decoded),
                    driverPrivateMiB=Math.Round(parent.PrivateMemorySize64/1048576.0,1),ffmpegPrivateMiB=Math.Round(child.PrivateMemorySize64/1048576.0,1),
                    managedHeapMiB=Math.Round(GC.GetTotalMemory(false)/1048576.0,1),gen2Collections=GC.CollectionCount(2)});
                nextSample++;
            }
        }
        await Task.Delay(300,deadline.Token);
        parent.Refresh(); child.Refresh();
        int frames=Volatile.Read(ref decoded);
        Assert(frames>=seconds*fps*0.75,"decoder cannot sustain even 75% of the target feed rate");
        double elapsed=clock.Elapsed.TotalMilliseconds;
        var cpu=new {driverMachinePercent=Math.Round((parent.TotalProcessorTime.TotalMilliseconds-parentInitial)/elapsed/Environment.ProcessorCount*100,2),
                     ffmpegMachinePercent=Math.Round((child.TotalProcessorTime.TotalMilliseconds-childInitial)/elapsed/Environment.ProcessorCount*100,2)};
        decoder.Dispose(); decoder.Dispose();
        await decoder.Completion.ContinueWith(done=> { _=done.Exception; }).WaitAsync(TimeSpan.FromSeconds(5));
        bool exited=false;
        for(int attempt=0;attempt<40;attempt++) {
            try { using var remaining=Process.GetProcessById(childId); exited=remaining.HasExited; }
            catch(ArgumentException) { exited=true; }
            if(exited) break; await Task.Delay(50);
        }
        Assert(exited,"disposed decoder process still running");
        cases.Add(new {width,height,sourceFps=fps,durationSeconds=seconds,decodedFrames=frames,decodedFps=Math.Round(frames/(elapsed/1000),2),
                       logicalProcessors=Environment.ProcessorCount,cpu,samples,decoderExitedAfterDispose=exited});
        Console.WriteLine($"PASS paced {width}x{height}/{fps}: {frames} decoded frames; decoder exited after repeated Dispose");
    }
    var report=JsonSerializer.Serialize(new {scope="synthetic paced decoder pipeline; driver includes producer; no capture, network or WPF rendering",cases},new JsonSerializerOptions {WriteIndented=true});
    if(args.Length>1) await File.WriteAllTextAsync(args[1],report+"\n");
    else Console.WriteLine(report);
} finally { Directory.Delete(directory,true); }

sealed class NoiseReader(long remaining):TextReader {
    public long Remaining=remaining;
    public int MaxRead;
    public override ValueTask<int> ReadAsync(Memory<char> buffer,CancellationToken ct=default) {
        ct.ThrowIfCancellationRequested(); MaxRead=Math.Max(MaxRead,buffer.Length);
        int count=(int)Math.Min(Remaining,buffer.Length); buffer.Span[..count].Fill('x'); Remaining-=count;
        return ValueTask.FromResult(count);
    }
}
sealed class WaitingReader:TextReader {
    public override async ValueTask<int> ReadAsync(Memory<char> buffer,CancellationToken ct=default) { await Task.Delay(Timeout.Infinite,ct); return 0; }
}

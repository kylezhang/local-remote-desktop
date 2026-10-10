using System.Diagnostics;
using System.IO;
namespace LocalRemoteDesktop;
public interface IVideoDecoder : IDisposable {
    event Action<byte[]>? Frame;
    Task Feed(byte[] annexB, CancellationToken ct);
    Task Completion { get; }
}
// Replace this adapter with Media Foundation/D3D11 without changing transport/input/UI.
public sealed class FfmpegDecoder : IVideoDecoder
{
    readonly Process process; readonly CancellationTokenSource stop=new();
    readonly Task errorDrain;
    int disposed;
    public event Action<byte[]>? Frame;
    public Task Completion { get; }
    public FfmpegDecoder(int width,int height) {
        if(width<2 || height<2 || width>3840 || height>2160) throw new ArgumentOutOfRangeException(nameof(width),"Unsupported video dimensions");
        var frameSize=checked(width*height*4);
        var local=Path.Combine(AppContext.BaseDirectory,"ffmpeg.exe");
        var start=new ProcessStartInfo(File.Exists(local)?local:"ffmpeg.exe") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardInput=true, RedirectStandardOutput=true, RedirectStandardError=true };
        // FFmpeg's automatic frame threading can retain multiple frames before
        // output. One decoder thread and low_delay favour interactive latency.
        foreach(var arg in new[]{"-hide_banner","-loglevel","error","-probesize","32","-analyzeduration","0","-threads","1","-flags","low_delay","-f","h264","-i","pipe:0","-an","-fps_mode","passthrough","-f","rawvideo","-pix_fmt","bgra","pipe:1"}) start.ArgumentList.Add(arg);
        process=Process.Start(start) ?? throw new IOException("无法启动 FFmpeg");
        // Drain into a fixed buffer; ReadToEnd would retain unlimited diagnostics
        // for a noisy/broken stream until the long-running process exits.
        errorDrain=DrainDiagnostics(process.StandardError,stop.Token);
        Completion=Task.Run(async()=> { while(!stop.IsCancellationRequested) { var pixels=new byte[frameSize]; await process.StandardOutput.BaseStream.ReadExactlyAsync(pixels,stop.Token); Frame?.Invoke(pixels); } });
    }
    internal static async Task DrainDiagnostics(TextReader reader,CancellationToken ct) {
        var buffer=new char[2048];
        try { while(await reader.ReadAsync(buffer.AsMemory(),ct)>0) {} }
        catch(Exception e) when(e is IOException or OperationCanceledException or ObjectDisposedException) {}
    }
    public Task Feed(byte[] annexB,CancellationToken ct) => process.StandardInput.BaseStream.WriteAsync(annexB,ct).AsTask();
    public void Dispose() {
        if(Interlocked.Exchange(ref disposed,1)!=0) return;
        stop.Cancel(); try { if(!process.HasExited) process.Kill(true); } catch {} process.Dispose();
        _=Task.WhenAll(Completion,errorDrain).ContinueWith(done=> { _=done.Exception; stop.Dispose(); },TaskScheduler.Default);
    }
}

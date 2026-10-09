using System.Diagnostics;
using System.IO;
namespace LocalRemote;
public interface IVideoDecoder : IDisposable {
    event Action<byte[]>? Frame;
    Task Feed(byte[] annexB, CancellationToken ct);
    Task Completion { get; }
}
// Replace this adapter with Media Foundation/D3D11 without changing transport/input/UI.
public sealed class FfmpegDecoder : IVideoDecoder
{
    readonly Process process; readonly CancellationTokenSource stop=new();
    public event Action<byte[]>? Frame;
    public Task Completion { get; }
    public FfmpegDecoder(int width,int height) {
        var local=Path.Combine(AppContext.BaseDirectory,"ffmpeg.exe");
        var start=new ProcessStartInfo(File.Exists(local)?local:"ffmpeg.exe") { UseShellExecute=false, CreateNoWindow=true, RedirectStandardInput=true, RedirectStandardOutput=true, RedirectStandardError=true };
        // FFmpeg's automatic frame threading can retain multiple frames before
        // output. One decoder thread and low_delay favour interactive latency.
        foreach(var arg in new[]{"-hide_banner","-loglevel","error","-probesize","32","-analyzeduration","0","-threads","1","-flags","low_delay","-f","h264","-i","pipe:0","-an","-fps_mode","passthrough","-f","rawvideo","-pix_fmt","bgra","pipe:1"}) start.ArgumentList.Add(arg);
        process=Process.Start(start) ?? throw new IOException("无法启动 FFmpeg");
        _ = process.StandardError.ReadToEndAsync();
        Completion=Task.Run(async()=> { var frameSize=checked(width*height*4); while(!stop.IsCancellationRequested) { var pixels=new byte[frameSize]; await process.StandardOutput.BaseStream.ReadExactlyAsync(pixels,stop.Token); Frame?.Invoke(pixels); } });
    }
    public Task Feed(byte[] annexB,CancellationToken ct) => process.StandardInput.BaseStream.WriteAsync(annexB,ct).AsTask();
    public void Dispose() { stop.Cancel(); try { if(!process.HasExited) process.Kill(true); } catch {} process.Dispose(); }
}

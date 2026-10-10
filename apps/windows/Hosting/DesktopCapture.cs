using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
namespace LocalRemoteDesktop;

internal readonly record struct CaptureArea(int X,int Y,int Width,int Height) {
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern nint SetThreadDpiAwarenessContext(nint context);
    internal static CaptureArea Primary() {
        var previous=SetThreadDpiAwarenessContext(-4);
        try { return new(0,0,GetSystemMetrics(0),GetSystemMetrics(1)); }
        finally { if(previous!=0) SetThreadDpiAwarenessContext(previous); }
    }
    internal static CaptureArea Virtual() {
        var previous=SetThreadDpiAwarenessContext(-4);
        try { return new(GetSystemMetrics(76),GetSystemMetrics(77),GetSystemMetrics(78),GetSystemMetrics(79)); }
        finally { if(previous!=0) SetThreadDpiAwarenessContext(previous); }
    }
}
internal readonly record struct VideoProfile(int Width,int Height,int Fps,int Bitrate) {
    internal static VideoProfile For(CaptureArea area,int quality) {
        if(quality is <0 or >2 || area.Width<2 || area.Height<2) throw new ArgumentException("Invalid display/profile");
        double scale=Math.Min(1,Math.Min((quality==0?1280d:1920)/area.Width,(quality==0?720d:1080)/area.Height));
        return new(Math.Max(2,(int)(area.Width*scale)/2*2),Math.Max(2,(int)(area.Height*scale)/2*2),quality==0?30:60,quality==0?4_000_000:quality==1?10_000_000:18_000_000);
    }
}
internal interface IDesktopCapture {
    Task Run(VideoProfile profile,Func<byte[],Task> frame,CancellationToken ct);
}
// First Windows host adapter: GDI desktop capture + MF hardware H264 in FFmpeg.
// No disk recording. DXGI/native MFT can replace this interface after profiling.
internal sealed class FfmpegDesktopCapture(CaptureArea area,bool software=false,bool synthetic=false):IDesktopCapture {
    internal int ProcessId {get;private set;}
    internal string Encoder=>software?"libx264":"h264_mf";
    internal ProcessStartInfo Command(VideoProfile p) {
        var local=Path.Combine(AppContext.BaseDirectory,"ffmpeg.exe");
        var start=new ProcessStartInfo(File.Exists(local)?local:"ffmpeg.exe") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        void Add(params string[] args) { foreach(var a in args) start.ArgumentList.Add(a); }
        Add("-hide_banner","-loglevel","error","-nostdin");
        if(synthetic) Add("-re","-f","lavfi","-i",$"testsrc2=size={area.Width}x{area.Height}:rate={p.Fps}");
        else Add("-f","gdigrab","-draw_mouse","1","-framerate",p.Fps.ToString(),"-offset_x",area.X.ToString(),"-offset_y",area.Y.ToString(),"-video_size",$"{area.Width}x{area.Height}","-i","desktop");
        Add("-an","-vf",$"scale={p.Width}:{p.Height}","-c:v",Encoder,"-b:v",p.Bitrate.ToString(),"-g",p.Fps.ToString(),"-bf","0");
        if(software) Add("-threads","2","-preset","ultrafast","-tune","zerolatency","-profile:v","baseline","-pix_fmt","yuv420p","-x264-params","aud=1:repeat-headers=1");
        else Add("-hw_encoding","1","-scenario","display_remoting","-rate_control","ld_vbr","-pix_fmt","nv12","-bsf:v","h264_metadata=aud=insert");
        Add("-flush_packets","1","-f","h264","pipe:1"); return start;
    }
    public async Task Run(VideoProfile profile,Func<byte[],Task> frame,CancellationToken ct) {
        using var process=Process.Start(Command(profile))??throw new IOException("Cannot start desktop encoder");
        ProcessId=process.Id;
        var errors=new char[2048]; string lastError="";
        var drain=Task.Run(async()=> { try { int n; while((n=await process.StandardError.ReadAsync(errors.AsMemory(),ct))>0) lastError=new string(errors,0,n); } catch(OperationCanceledException) {} });
        try {
            var framer=new AnnexBFrames(); var buffer=new byte[32768]; int count;
            while(true) {
                using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(8));
                try { count=await process.StandardOutput.BaseStream.ReadAsync(buffer,deadline.Token); }
                catch(OperationCanceledException) when(!ct.IsCancellationRequested) { throw new IOException("Desktop encoder produced no data within 8 seconds"); }
                if(count==0) break;
                await framer.Feed(buffer.AsMemory(0,count),frame);
            }
            await process.WaitForExitAsync(ct); await drain;
            if(process.ExitCode!=0) throw new IOException("Desktop encoder failed: "+lastError.Trim());
            await framer.Finish(frame);
            if(!ct.IsCancellationRequested) throw new IOException("Desktop capture ended");
        } finally {
            if(!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync();
            try { await drain; } catch(IOException) {}
        }
    }
}

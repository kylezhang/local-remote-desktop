using System.IO;
using System.Text.Json;
using LocalRemoteDesktop;

static partial class Program {
    // Explicit native acceptance only. Reuse the owner's existing paired secret
    // in memory; never print/save it or modify pairing. Clipboard sync stays off.
    static async Task CheckLiveMac() {
        Assert(Environment.GetEnvironmentVariable("LOCALREMOTE_INTERACTIVE_CHECKS")=="1","Live Mac acceptance needs an agreed interactive test period");
        var address=Environment.GetEnvironmentVariable("LOCALREMOTE_LIVE_MAC_ADDRESS")??throw new Exception("Target required");
        var saved=Environment.GetEnvironmentVariable("LOCALREMOTE_LIVE_MAC_SAVED_ADDRESS")??address;
        var metadata=Environment.GetEnvironmentVariable("LOCALREMOTE_LIVE_MAC_TARGET")??throw new Exception("Dedicated target required");
        using var target=JsonDocument.Parse(await File.ReadAllBytesAsync(metadata));
        Assert(target.RootElement.GetProperty("owned").GetBoolean(),"Mac fixture did not own focus");
        double age=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000d-target.RootElement.GetProperty("timestamp").GetDouble();
        Assert(age is >=-5 and <=5,"Mac fixture observation is stale");
        var secret=SecretStore.Load(saved); Assert(secret.Length>=8,"No existing paired credential available");
        using var wire=new Wire(); using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(25));
        await wire.Connect(address,secret,deadline.Token); secret="";
        await wire.Json(new {op="handshake",version=1,quality=0},deadline.Token);
        FfmpegDecoder? decoder=null; int frames=0; bool trusted=false,capabilitiesReceived=false; long posted=0;
        var reading=Task.Run(async()=> {
            while(!deadline.IsCancellationRequested) {
                var packet=await wire.Read(deadline.Token);
                if(packet.type==2) { if(decoder!=null) await decoder.Feed(packet.data,deadline.Token); }
                else if(packet.type==1) {
                    using var json=JsonDocument.Parse(packet.data); var message=json.RootElement;
                    string? op=message.GetProperty("op").GetString();
                    if(op=="video_header") { decoder=new FfmpegDecoder(message.GetProperty("width").GetInt32(),message.GetProperty("height").GetInt32()); decoder.Frame+=_=>Interlocked.Increment(ref frames); }
                    if(op=="capabilities") { trusted=message.GetProperty("accessibility").GetBoolean(); Volatile.Write(ref capabilitiesReceived,true); }
                    if(op=="heartbeat" && message.TryGetProperty("inputPosted",out var count)) posted=count.GetInt64();
                }
            }
        });
        try {
            await Until(()=>Volatile.Read(ref frames)>=3 && Volatile.Read(ref capabilitiesReceived),"Installed Mac live screen/capabilities not received",20000);
            Assert(trusted,"Installed Mac input permission not effective");
            foreach(int vk in new[]{65,49,97}) {
                await wire.Json(new {op="key_event",vk,down=true,flags=0},deadline.Token);
                await wire.Json(new {op="key_event",vk,down=false,flags=0},deadline.Token);
            }
            double x=target.RootElement.GetProperty("x").GetDouble(),y=target.RootElement.GetProperty("y").GetDouble();
            await wire.Json(new {op="mouse_move",x,y},deadline.Token);
            await wire.Json(new {op="mouse_button",button=0,down=true,x,y},deadline.Token);
            await wire.Json(new {op="mouse_button",button=0,down=false,x,y},deadline.Token);
            await wire.Json(new {op="scroll",delta=120},deadline.Token);
            await wire.Json(new {op="release_all"},deadline.Token);
            await wire.Json(new {op="heartbeat"},deadline.Token);
            await Until(()=>posted>=10,"Installed Mac did not post the test sequence",5000);
            Console.WriteLine($"PASS installed Mac screen capture/hardware H264 -> production Windows decoder: {frames} live frames; effective Accessibility and {posted} submitted events; target application acceptance must be checked separately");
        } finally {
            try { await wire.Json(new {op="disconnect"},deadline.Token); } catch {}
            deadline.Cancel(); wire.Dispose(); try { await reading; } catch(OperationCanceledException) {} catch(IOException) {}
            decoder?.Dispose();
        }
    }
}

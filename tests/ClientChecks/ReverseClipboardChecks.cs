using System.Net;
using System.Text.Json;
using System.Windows;
using LocalRemoteDesktop;

static partial class Program {
    static async Task CheckReverseClipboard() {
        // Real production host, encrypted wire and WPF dispatcher adapter. The
        // clipboard and input are recordings; no window or physical capture.
        var board=new ReverseBoard(); bool allowed=true;
        var area=new CaptureArea(0,0,320,180); var input=new WindowsInput(area,area,new SharingInput());
        var credential=HostCredential.Create("Fixture1");
        await using var host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,
            ()=>new SilentCapture(),()=>input,address:IPAddress.Loopback,port:0,
            clipboard:(wire,ct)=>new WindowsHostClipboard(wire,board,Application.Current.Dispatcher,()=>allowed,ct));
        host.Start(); using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var client=new Wire(); await client.Connect("127.0.0.1",credential.Password,deadline.Token,host.Port);
        await client.Json(new {op="handshake",version=1,quality=0},deadline.Token);
        async Task<JsonElement> Read() { var packet=await client.Read(deadline.Token); Assert(packet.type==1,"Unexpected clipboard packet"); return JsonSerializer.Deserialize<JsonElement>(packet.data); }
        Assert((await Read()).GetProperty("clipboardText").GetBoolean(),"Production host failed to advertise text clipboard"); await Read();
        board.Copy("before pairing");
        await client.Json(new {op="clipboard_sync",requestId=1,enabled=true},deadline.Token);
        Assert((await Read()).GetProperty("enabled").GetBoolean(),"Missing paired clipboard ack");
        await client.Json(new {op="clipboard_text",requestId=1,encoding="utf8-base64url",text=TextClipboardSync.Encode("Mac → Windows 中文")},deadline.Token);
        await Until(()=>board.Text=="Mac → Windows 中文","Mac text did not reach Windows UI adapter");
        board.Copy("Windows → Mac 🎉");
        var message=await Read(); Assert(message.GetProperty("op").GetString()=="clipboard_text" && TextClipboardSync.Decode(message.GetProperty("text").GetString()!)==board.Text,"Host text failed encrypted return path");
        allowed=false; Assert(!(await Read()).GetProperty("enabled").GetBoolean(),"Local consent not revoked over wire");
        await client.Json(new {op="clipboard_text",requestId=1,encoding="utf8-base64url",text=TextClipboardSync.Encode("rejected")},deadline.Token);
        await client.Json(new {op="heartbeat"},deadline.Token); Assert((await Read()).GetProperty("op").GetString()=="heartbeat" && board.Text=="Windows → Mac 🎉","Revoked incoming text applied");
        allowed=true; Assert((await Read()).GetProperty("enabled").GetBoolean(),"Host consent restore missing");
        await client.Json(new {op="clipboard_sync",requestId=2,enabled=false},deadline.Token); Assert(!(await Read()).GetProperty("enabled").GetBoolean(),"Controller disable not acknowledged");
        await client.Json(new {op="clipboard_text",requestId=1,encoding="utf8-base64url",text=TextClipboardSync.Encode("stale")},deadline.Token);
        await client.Json(new {op="heartbeat"},deadline.Token); Assert((await Read()).GetProperty("op").GetString()=="heartbeat" && board.Text=="Windows → Mac 🎉","Stale text applied");
        await client.Json(new {op="disconnect"},deadline.Token); await host.DisposeAsync();
        Console.WriteLine("PASS production Windows sharing encrypted clipboard + WPF dispatcher (unshown): Unicode in both directions, consent/revoke/restore, stale IDs and disconnect; recording clipboard/input");
    }
}
sealed class ReverseBoard:ITextClipboard {
    public long Revision { get;private set; }
    internal string? Text;
    public bool TryRead(out string? text) { text=Text; return true; }
    public bool TryWrite(string text) { Copy(text); return true; }
    internal void Copy(string text) { Text=text; Revision++; }
}
sealed class SilentCapture:IDesktopCapture {
    public Task Run(VideoProfile profile,Func<byte[],Task> output,CancellationToken token)=>Task.Delay(Timeout.Infinite,token);
}

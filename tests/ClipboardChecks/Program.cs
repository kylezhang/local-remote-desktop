using System.Text;
using System.Text.Json;
using LocalRemoteDesktop;
static void Assert(bool value,string message) { if(!value) throw new Exception(message); }
foreach(var text in new[]{"", "hello", "中文 🎉\r\nsecond line\t", new string('?',32768), new string('\uffff',10922)}) {
    var payload=TextClipboardSync.Encode(text)!;
    Assert(payload!=null && TextClipboardSync.Decode(payload)==text,"text roundtrip");
    var message=JsonSerializer.SerializeToUtf8Bytes(new {op="clipboard_text",requestId=long.MaxValue,encoding="utf8-base64url",text=payload});
    Assert(message.Length+17<=65536,"escaped payload exceeds legacy encrypted record limit");
}
Assert(TextClipboardSync.Encode(new string('a',32769))==null,"oversize accepted");
Assert(TextClipboardSync.Encode(new string('中',10923))==null,"multibyte oversize accepted");
Assert(TextClipboardSync.Encode("a\0b")==null && TextClipboardSync.Encode("\ud800")==null,"unsafe text accepted");
foreach(var bad in new[]{"!", "Zg=", "Zg==\n", "/w==", "_w==", "wK8=", "AA==", new string('A',TextClipboardSync.MaxBase64+4)})
    Assert(TextClipboardSync.Decode(bad)==null,"malformed payload accepted");
Console.WriteLine("PASS Unicode/empty text, strict UTF8/base64url, byte limits and JSON record fit");

long clock=0; var board=new FakeBoard(); board.Copy("existing private text");
var sync=new TextClipboardSync(board,()=>clock);
Assert(sync.Poll()==null && !sync.Receive("aGVsbG8="),"default-off read or write");
sync.SetEnabled(true); Assert(sync.Poll()==null && board.Reads==0,"enable leaked pre-existing clipboard");
board.Copy("new text"); board.BusyRead=true;
Assert(sync.Poll()==null,"busy read accepted"); board.BusyRead=false;
Assert(TextClipboardSync.Decode(sync.Poll()!)=="new text" && sync.Poll()==null,"new copy lost or duplicated");
Assert(sync.Receive(TextClipboardSync.Encode("remote")!) && board.Text=="remote" && sync.Poll()==null,"received text echoed");
board.Copy(null); Assert(sync.Poll()==null,"non-text sent");
board.Copy(new string('a',32769)); Assert(sync.Poll()==null,"oversized clipboard sent");
board.BusyWrite=true; sync.Receive(TextClipboardSync.Encode("retry")!); sync.Poll();
board.BusyWrite=false; sync.Poll(); Assert(board.Text=="retry" && sync.Poll()==null,"busy write not retried or echoed");
board.BusyWrite=true; sync.Receive(TextClipboardSync.Encode("stale")!); board.Copy("local wins"); board.BusyWrite=false;
Assert(TextClipboardSync.Decode(sync.Poll()!)=="local wins" && board.Text=="local wins","retry overwrote newer local copy");
board.BusyWrite=true; sync.Receive(TextClipboardSync.Encode("expired")!); clock=2001; sync.Poll(); board.BusyWrite=false; sync.Poll();
Assert(board.Text=="local wins","expired write applied");
board.BusyWrite=true; sync.Receive(TextClipboardSync.Encode("cancelled")!); sync.SetEnabled(false); board.BusyWrite=false; sync.Poll();
Assert(board.Text=="local wins","disable retained pending write");
board.Copy("offline secret"); sync.SetEnabled(true); Assert(sync.Poll()==null,"reconnect leaked offline copy");
Console.WriteLine("PASS consent baseline, non-text/oversize skip, no echo, busy recovery, local priority, expiry and disconnect");

var left=new FakeBoard(); var right=new FakeBoard(); var a=new TextClipboardSync(left); var b=new TextClipboardSync(right);
a.SetEnabled(true); b.SetEnabled(true); left.Copy("Windows → Mac 中文");
Assert(b.Receive(a.Poll()!) && right.Text==left.Text,"outgoing transfer");
right.Copy("Mac → Windows emoji 😀"); Assert(a.Receive(b.Poll()!) && left.Text==right.Text,"incoming transfer");
for(int i=0;i<200;i++) Assert(a.Poll()==null && b.Poll()==null,"clipboard feedback loop");
Console.WriteLine("PASS both directions settle without a feedback loop (200 polls)");
var queue=new InputQueue();
for(int i=0;i<10000;i++) Assert(queue.TryWrite(new {op="clipboard_text",text=i}),"consecutive clipboard changes accumulated");
Assert(queue.TryWrite(new {op="key_event",vk=86,down=true}),"paste queue failed");
Assert(queue.TryWrite(new {op="clipboard_text",text=10000}),"new clipboard failed"); queue.Complete();
var received=new List<JsonElement>(); await foreach(var message in queue.ReadAllAsync(CancellationToken.None)) received.Add(JsonSerializer.SerializeToElement(message));
Assert(received.Count==3 && received[0].GetProperty("text").GetInt32()==9999 && received[1].GetProperty("op").GetString()=="key_event","clipboard/key ordering broken");
Console.WriteLine("PASS clipboard coalescing is bounded and cannot cross paste-key boundary");

sealed class FakeBoard : ITextClipboard {
    public long Revision {get;private set;}
    public string? Text; public bool BusyRead,BusyWrite; public int Reads;
    public void Copy(string? text) { Text=text; Revision++; }
    public bool TryRead(out string? text) { Reads++; text=Text; return !BusyRead; }
    public bool TryWrite(string text) { if(BusyWrite) return false; Copy(text); return true; }
}

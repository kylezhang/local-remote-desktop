using System.Text;
namespace LocalRemote;

// Used only on the UI thread. The adapter is injectable so tests never touch
// the user's system clipboard. Network negotiation is owned by MainWindow.
public interface ITextClipboard {
    long Revision { get; }
    bool TryRead(out string? text);
    bool TryWrite(string text);
}
public sealed class TextClipboardSync(ITextClipboard clipboard,Func<long>? clock=null) {
    public const int MaxBytes=32*1024;
    public const int MaxBase64=((MaxBytes+2)/3)*4;
    static readonly UTF8Encoding Utf8=new(false,true);
    readonly Func<long> now=clock ?? (()=>Environment.TickCount64);
    long observed, pendingRevision, expires;
    string? pending;
    public bool Enabled { get; private set; }
    public void SetEnabled(bool enabled) {
        Enabled=enabled; pending=null; observed=clipboard.Revision;
    }
    public static string? Encode(string text) {
        if(text.Length>MaxBytes || text.Contains('\0')) return null;
        try { var bytes=Utf8.GetBytes(text); return bytes.Length<=MaxBytes?Convert.ToBase64String(bytes).Replace('+','-').Replace('/','_'):null; }
        catch(EncoderFallbackException) { return null; }
    }
    public static string? Decode(string data) {
        if(data.Length>MaxBase64 || data.Length%4!=0) return null;
        try {
            var bytes=Convert.FromBase64String(data.Replace('-','+').Replace('_','/'));
            // Canonical base64 only: whitespace and alternate encodings rejected.
            if(bytes.Length>MaxBytes || Convert.ToBase64String(bytes).Replace('+','-').Replace('/','_')!=data) return null;
            var text=Utf8.GetString(bytes); return text.Contains('\0')?null:text;
        } catch(Exception e) when(e is FormatException or DecoderFallbackException) { return null; }
    }
    public string? Poll() {
        if(!Enabled) return null;
        long revision=clipboard.Revision;
        if(pending!=null) {
            // A newer local copy wins over a busy incoming write. Retry for at
            // most two seconds; never keep an unbounded queue or stale text.
            if(revision!=pendingRevision || now()>=expires) pending=null;
            else if(clipboard.TryWrite(pending)) { pending=null; observed=clipboard.Revision; return null; }
            else return null;
        }
        if(revision==observed || !clipboard.TryRead(out var text) || revision!=clipboard.Revision) return null;
        observed=revision;
        return text==null?null:Encode(text);
    }
    public bool Receive(string data) {
        if(!Enabled || Decode(data) is not {} text) return false;
        pending=null;
        if(clipboard.TryWrite(text)) observed=clipboard.Revision;
        else { pending=text; pendingRevision=clipboard.Revision; expires=now()+2000; }
        return true;
    }
}

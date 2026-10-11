using System.Text.Json;
namespace LocalRemoteDesktop;

// Serialized by the UI adapter. Uses the same request/ack protocol as the Mac
// sharing role; an authenticated session does not override local consent.
internal sealed class HostTextClipboard(ITextClipboard board, Func<bool> allowed) {
    readonly TextClipboardSync sync=new(board);
    long requestId; bool requested,advertised,stopped;
    void Refresh() { bool enabled=!stopped && requested && allowed(); if(sync.Enabled!=enabled) sync.SetEnabled(enabled); }
    object Ack() { advertised=sync.Enabled; return new {op="clipboard_sync",requestId,enabled=advertised}; }
    internal object? Control(JsonElement message) {
        if(stopped || !message.TryGetProperty("op",out var kind) || kind.ValueKind!=JsonValueKind.String ||
           !message.TryGetProperty("requestId",out var request) || request.ValueKind!=JsonValueKind.Number || !request.TryGetInt64(out var id) || id<=0) return null;
        if(kind.GetString()=="clipboard_sync" && id>requestId && message.TryGetProperty("enabled",out var enable) && enable.ValueKind is JsonValueKind.True or JsonValueKind.False) {
            requestId=id; requested=enable.GetBoolean(); sync.SetEnabled(false); Refresh(); return Ack();
        }
        Refresh();
        if(kind.GetString()=="clipboard_text" && id==requestId && message.TryGetProperty("encoding",out var encoding) &&
           encoding.ValueKind==JsonValueKind.String && encoding.GetString()=="utf8-base64url" &&
           message.TryGetProperty("text",out var text) && text.ValueKind==JsonValueKind.String) sync.Receive(text.GetString()!);
        return null;
    }
    internal object? Poll() {
        if(stopped) return null;
        Refresh(); if(advertised!=sync.Enabled) return Ack();
        return sync.Poll() is {} text?new {op="clipboard_text",requestId,encoding="utf8-base64url",text}:null;
    }
    internal void Stop() { stopped=true; sync.SetEnabled(false); }
}

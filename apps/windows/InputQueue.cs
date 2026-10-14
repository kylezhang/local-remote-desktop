using System.Text.Json;
using System.Threading.Channels;
namespace LocalRemoteDesktop;

// Replace only consecutive, unsent moves: never move a pointer event across a
// button/key boundary. Focus release purges only input and takes priority over
// retained clipboard/file controls. Overload closes the session.
public sealed class InputQueue
{
    readonly object gate = new();
    readonly LinkedList<object> pending = new();
    readonly Channel<bool> wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode=BoundedChannelFullMode.DropWrite });
    readonly int capacity;
    bool complete;
    public InputQueue(int capacity=256) => this.capacity=capacity;
    static string? Operation(object message) => JsonSerializer.SerializeToElement(message).GetProperty("op").GetString();
    static bool IsInput(string? op) => op is "mouse_move" or "mouse_button" or "scroll" or "key_event" or "release_all";
    public bool TryWrite(object message) {
        lock(gate) {
            if(complete) return false;
            string? op=Operation(message);
            if(op=="release_all") {
                for(var node=pending.First;node!=null;) {
                    var next=node.Next;
                    if(IsInput(Operation(node.Value))) pending.Remove(node);
                    node=next;
                }
                if(pending.Count>=capacity) return false;
                pending.AddFirst(message);
            }
            else if((op is "mouse_move" or "clipboard_text") && pending.Last is {} last && Operation(last.Value)==op) last.Value=message;
            else { if(pending.Count>=capacity) return false; pending.AddLast(message); }
            wake.Writer.TryWrite(true); return true;
        }
    }
    public void Complete() { lock(gate) { complete=true; wake.Writer.TryComplete(); } }
    public async IAsyncEnumerable<object> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct) {
        while(await wake.Reader.WaitToReadAsync(ct)) {
            wake.Reader.TryRead(out _);
            while(true) {
                object? message;
                lock(gate) { message=pending.First?.Value; if(message!=null) pending.RemoveFirst(); }
                if(message==null) break;
                yield return message;
            }
        }
    }
}

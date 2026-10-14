using System.Text.Json;
using LocalRemoteDesktop;

static class ReleaseQueueChecks {
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }
    static async Task<List<JsonElement>> Drain(InputQueue queue) {
        queue.Complete(); var values=new List<JsonElement>();
        await foreach(var value in queue.ReadAllAsync(CancellationToken.None)) values.Add(JsonSerializer.SerializeToElement(value));
        return values;
    }
    internal static async Task Run() {
        var queue=new InputQueue(8);
        foreach(var value in new object[]{
            new {op="key_event",vk=162,down=true},
            new {op="clipboard_sync",enabled=false,requestId=7},
            new {op="mouse_button",button=0,down=true},
            new {op="file_sync",enabled=false,requestId=8},
            new {op="mouse_move",x=0.2,y=0.3},
            new {op="file_offer",requestId=9},
            new {op="scroll",delta=120},
            new {op="clipboard_text",text="fixture"}
        }) Require(queue.TryWrite(value),"Mixed queue fixture rejected");
        for(int i=0;i<20;i++) Require(queue.TryWrite(new {op="release_all"}),"Repeated release overflowed");
        Require(queue.TryWrite(new {op="key_event",vk=65,down=true}),"New input after release rejected");
        var values=await Drain(queue);
        Require(values.Select(v=>v.GetProperty("op").GetString()).SequenceEqual(new[]{
            "release_all","clipboard_sync","file_sync","file_offer","clipboard_text","key_event"
        }),"Focus release lost clipboard/file controls, retained stale input or reordered controls");
        Require(values[1].GetProperty("requestId").GetInt32()==7 &&
                !values[1].GetProperty("enabled").GetBoolean() &&
                values[3].GetProperty("requestId").GetInt32()==9 &&
                values[4].GetProperty("text").GetString()=="fixture", "Release changed transfer payloads");

        queue=new InputQueue(1);
        Require(queue.TryWrite(new {op="file_sync",enabled=false,requestId=10}),"Control fixture rejected");
        Require(!queue.TryWrite(new {op="release_all"}),"Non-input overload must be reported, not silently discard revocation");
        values=await Drain(queue);
        Require(values.Count==1 && values[0].GetProperty("requestId").GetInt32()==10,"Failed release discarded queued consent");
        Console.WriteLine("PASS focus release prioritizes key cleanup, preserves clipboard/file consent and payload order, stays bounded and reports control-only overload");
    }
}

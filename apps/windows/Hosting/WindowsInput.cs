using System.Runtime.InteropServices;
using System.Text.Json;
namespace LocalRemote;

[StructLayout(LayoutKind.Sequential)] internal struct NativeInput {
    internal uint Type; internal InputUnion Data;
}
[StructLayout(LayoutKind.Explicit)] internal struct InputUnion {
    [FieldOffset(0)] internal MouseInput Mouse;
    [FieldOffset(0)] internal KeyInput Key;
}
[StructLayout(LayoutKind.Sequential)] internal struct MouseInput {
    internal int X,Y; internal uint Wheel,Flags,Time; internal nuint Extra;
}
[StructLayout(LayoutKind.Sequential)] internal struct KeyInput {
    internal ushort Vk,Scan; internal uint Flags,Time; internal nuint Extra;
}
internal interface INativeInput { bool Post(NativeInput value); }
internal sealed class SendInputAdapter:INativeInput {
    [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,NativeInput[] inputs,int size);
    public bool Post(NativeInput value)=>SendInput(1,[value],Marshal.SizeOf<NativeInput>())==1;
}
internal sealed class WindowsInput(CaptureArea area, CaptureArea virtualArea,INativeInput native) {
    readonly HashSet<int> keys=[],buttons=[];
    internal long Received {get;private set;}
    internal long Posted {get;private set;}
    internal long Blocked {get;private set;}
    internal long Rejected {get;private set;}
    internal string Error {get;private set;}="";
    internal int Held=>keys.Count+buttons.Count;
    static bool AllowedKey(int vk)=>vk is >=8 and <=20 or >=27 and <=46 or >=48 and <=57 or >=65 and <=93 or >=96 and <=135 or 144 or 145 or >=160 and <=165 or >=186 and <=192 or >=219 and <=222 or 226;
    static NativeInput Key(int vk,bool down)=>new() {Type=1,Data=new() {Key=new() {Vk=(ushort)vk,Flags=(down?0u:2u)|(vk is 33 or 34 or 35 or 36 or 37 or 38 or 39 or 40 or 45 or 46 or 91 or 92 or 111 or 144 or 163 or 165?1u:0u)}}};
    bool Post(NativeInput input) { if(native.Post(input)) { Posted++; Error=""; return true; }; Blocked++; Error="windows_input_blocked"; return false; }
    void Reject() { Rejected++; Error="invalid_or_unsupported_input"; }
    static uint ButtonFlag(int button,bool down)=>button==0?(down?2u:4u):button==1?(down?8u:16u):(down?32u:64u);
    NativeInput Pointer(double x,double y,uint flags,uint wheel=0)=>new() {Data=new() {Mouse=new() {
        X=(int)Math.Round((area.X+x*(area.Width-1)-virtualArea.X)*65535/Math.Max(1,virtualArea.Width-1)),
        Y=(int)Math.Round((area.Y+y*(area.Height-1)-virtualArea.Y)*65535/Math.Max(1,virtualArea.Height-1)),
        Flags=flags|1u|0x8000u|0x4000u,Wheel=wheel}}};
    static bool Int(JsonElement m,string key,out int value) { value=0; return m.TryGetProperty(key,out var p) && p.ValueKind==JsonValueKind.Number && p.TryGetInt32(out value); }
    static bool Bool(JsonElement m,string key,out bool value) { value=false; if(!m.TryGetProperty(key,out var p) || p.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value=p.GetBoolean(); return true; }
    static bool Position(JsonElement m,out double x,out double y) {
        x=y=0;
        return m.TryGetProperty("x",out var a) && a.ValueKind==JsonValueKind.Number && a.TryGetDouble(out x) && double.IsFinite(x) && x is >=0 and <=1
            && m.TryGetProperty("y",out var b) && b.ValueKind==JsonValueKind.Number && b.TryGetDouble(out y) && double.IsFinite(y) && y is >=0 and <=1;
    }
    internal void Handle(JsonElement m) {
        Received++;
        if(!m.TryGetProperty("op",out var p) || p.ValueKind!=JsonValueKind.String) { Reject(); return; }
        var op=p.GetString();
        if(op=="release_all") { Release(); return; }
        if(op=="key_event") {
            if(!Int(m,"vk",out var vk) || !AllowedKey(vk) || !Bool(m,"down",out var down) ||
               (m.TryGetProperty("flags",out _) && (!Int(m,"flags",out var flags) || flags is <0 or >15))) { Reject(); return; }
            if(Post(Key(vk,down))) { if(down) keys.Add(vk); else keys.Remove(vk); }
            return;
        }
        if(op=="scroll") {
            if(!Int(m,"delta",out var delta) || delta is <-1200 or >1200) { Reject(); return; }
            if(m.TryGetProperty("x",out _) || m.TryGetProperty("y",out _)) {
                if(!Position(m,out var sx,out var sy)) { Reject(); return; }
                Post(Pointer(sx,sy,0x800,unchecked((uint)delta)));
            } else Post(new() {Data=new() {Mouse=new() {Flags=0x800,Wheel=unchecked((uint)delta)}}});
            return;
        }
        if(!Position(m,out var x,out var y)) { Reject(); return; }
        if(op=="mouse_move") { Post(Pointer(x,y,0)); return; }
        if(op=="mouse_button") {
            if(!Int(m,"button",out var button) || button is <0 or >2 || !Bool(m,"down",out var down)) { Reject(); return; }
            if(Post(Pointer(x,y,ButtonFlag(button,down)))) { if(down) buttons.Add(button); else buttons.Remove(button); }
            return;
        }
        Reject();
    }
    internal void Release() {
        foreach(var vk in keys.ToArray()) if(Post(Key(vk,false))) keys.Remove(vk);
        foreach(var b in buttons.ToArray()) if(Post(new() {Data=new() {Mouse=new() {Flags=ButtonFlag(b,false)}}})) buttons.Remove(b);
    }
}

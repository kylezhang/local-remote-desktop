using System.Runtime.InteropServices;
using System.Text.Json;
using LocalRemoteDesktop;

static class PointerGeometryChecks {
    [DllImport("user32.dll")] static extern nint SetThreadDpiAwarenessContext(nint context);
    [DllImport("user32.dll")] static extern nint GetThreadDpiAwarenessContext();
    [DllImport("user32.dll")] static extern int GetAwarenessFromDpiAwarenessContext(nint context);
    [DllImport("user32.dll")] static extern bool AreDpiAwarenessContextsEqual(nint first,nint second);
    static void Require(bool value,string error) { if(!value) throw new Exception(error); }
    internal static void Run() {
        foreach(var layout in new[] {
            new[] {(new CaptureArea(0,0,1920,1080),true)},
            new[] {(new CaptureArea(0,0,1920,1080),true),(new CaptureArea(-1920,0,1920,1080),false)},
            new[] {(new CaptureArea(0,0,3840,2160),true),(new CaptureArea(3840,-223,1440,2560),false)},
            new[] {(new CaptureArea(0,0,2560,1440),true),(new CaptureArea(-1080,-1920,1080,1920),false)}
        }) {
            var geometry=DisplayGeometry.FromDisplays(layout);
            foreach(var point in new[] {(0d,0d),(.25,.25),(.5,.5),(.75,.75),(1d,1d)}) {
                var recorder=new RecordingInput(); var input=new WindowsInput(geometry.Primary,geometry.Virtual,recorder);
                foreach(var op in new[] {"mouse_move","mouse_button","scroll"}) {
                    input.Handle(JsonSerializer.SerializeToElement(new {op,x=point.Item1,y=point.Item2,button=0,down=true,delta=120}));
                    var mouse=recorder.Values[^1].Data.Mouse;
                    // Reconstruct the physical landing point, rather than
                    // comparing two copies of the integer encoding formula.
                    var landedX=geometry.Virtual.X+mouse.X/65535d*(geometry.Virtual.Width-1);
                    var landedY=geometry.Virtual.Y+mouse.Y/65535d*(geometry.Virtual.Height-1);
                    var expectedX=geometry.Primary.X+point.Item1*(geometry.Primary.Width-1);
                    var expectedY=geometry.Primary.Y+point.Item2*(geometry.Primary.Height-1);
                    Require(Math.Abs(landedX-expectedX)<1 && Math.Abs(landedY-expectedY)<1,"Pointer leaves capture pixels: "+op);
                    Require((mouse.Flags&0xC001)==0xC001,"Pointer lost absolute/virtual/move flags");
                }
            }
        }
        var mixed=DisplayGeometry.FromDisplays([(new(0,0,3840,2160),true),(new(3840,-223,1440,2560),false)]);
        Require(mixed.Virtual==new CaptureArea(0,-223,5280,2560),"Mixed-scale/portrait union wrong");
        Require(VideoProfile.For(mixed.Primary,0).Width==1280 && VideoProfile.For(mixed.Primary,2).Width==1920,"Capture quality sizing wrong");
        Console.WriteLine("PASS primary/left/above/mixed-scale portrait layouts: move/click/wheel map to capture pixels");
        if(!OperatingSystem.IsWindows()) return;
        DisplayGeometry physical;
        try { physical=DisplayGeometry.Read(); }
        catch(ArgumentException) when(Environment.GetEnvironmentVariable("LOCALREMOTE_CI_CHECKS")=="1") {
            Console.WriteLine("NOT RUN native physical-display enumeration: hosted CI has no active display; synthetic layout checks passed");
            return;
        }
        foreach(var context in new nint[] {-1,-2,-4}) {
            var original=SetThreadDpiAwarenessContext(context);
            Require(original!=0,"Cannot enter test DPI context");
            try {
                Require(DisplayGeometry.Read()==physical,"Geometry varies with UI/worker DPI awareness");
                var before=GetThreadDpiAwarenessContext(); int calls=0;
                var adapter=new SendInputAdapter(_=> {
                    calls++; Require(GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext())==2,"Native call uses logical DPI context");
                    Require(DisplayGeometry.Read()==physical,"Physical bounds changed inside input call"); return true;
                });
                Require(adapter.Post(default) && calls==1,"Recording native boundary did not run");
                Require(AreDpiAwarenessContextsEqual(before,GetThreadDpiAwarenessContext()),"Input changed caller DPI context");
                Require(!new SendInputAdapter(_=>throw new InvalidOperationException()).Post(default),"Failed native boundary not rejected");
                Require(AreDpiAwarenessContextsEqual(before,GetThreadDpiAwarenessContext()),"Failed input leaked DPI context");
            } finally {SetThreadDpiAwarenessContext(original);}
        }
        Console.WriteLine("PASS real Windows physical display enumeration and native-call DPI scope/restoration across 3 contexts (recording submission; no OS input)");
    }
}

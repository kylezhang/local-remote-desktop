using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using LocalRemoteDesktop;

static partial class Program {

    // Explicit opt-in acceptance against a real Mac controller. All native down
    // events remain gated to this test-owned field; lose focus -> deny injection.
    static async Task CheckLANWindowsInput() {
        var address=IPAddress.Parse(Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_ADDRESS")??throw new Exception("LAN address required"));
        Assert(WindowsHost.IsLan(address),"LAN test must bind a private address");
        var metadata=Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_METADATA")??throw new Exception("Metadata path required");
        if(new[]{1,2,16,17,18,91,92}.Any(vk=>(GetAsyncKeyState(vk)&0x8000)!=0)) throw new Exception("Local modifiers/buttons held; native test cannot run");
        nint previous=GetForegroundWindow(); GetCursorPos(out var previousPoint);
        var field=new TextBox {Margin=new Thickness(12),MinHeight=44}; InputMethod.SetIsInputMethodEnabled(field,false);
        var panel=new StackPanel(); panel.Children.Add(new TextBlock {Text="Local Remote Desktop: isolated Mac input test",Margin=new Thickness(12)}); panel.Children.Add(field);
        var window=new Window {Title="Local Remote Desktop · Mac input test",Content=panel,Width=420,Height=180,Left=50,Top=50,ShowInTaskbar=true,ShowActivated=false};
        var seen=new List<int>(); int clicks=0,wheels=0;
        field.PreviewKeyDown+=(_,e)=>seen.Add(KeyInterop.VirtualKeyFromKey(e.Key));
        field.PreviewMouseDown+=(_,_)=>clicks++; field.PreviewMouseWheel+=(_,_)=>wheels++;
        WindowsHost? host=null; WindowsInput? input=null; FfmpegDesktopCapture? capture=null; GuardedNative? guarded=null;
        nint hwnd=0; Point? target=null;
        try {
            window.Show(); window.UpdateLayout(); hwnd=new WindowInteropHelper(window).Handle;
            Console.WriteLine("MANUAL: Activate this test window and focus its field when ready. No foreground activation is automated.");
            await Until(()=>GetForegroundWindow()==hwnd && field.IsKeyboardFocusWithin,"Manual test-field focus required",120000);
            seen.Clear(); clicks=0; wheels=0; // Manual focus clicks are not remote acceptance.
            var area=CaptureArea.Primary(); target=field.PointToScreen(new Point(field.ActualWidth/2,field.ActualHeight/2));
            guarded=new GuardedNative(()=>GetForegroundWindow()==hwnd);
            input=new WindowsInput(area,CaptureArea.Virtual(),guarded);
            var credential=HostCredential.Create("Office-LAN#42");
            host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,
                ()=>capture=new FfmpegDesktopCapture(area,software:false,synthetic:true),()=>input,address:address);
            host.Start();
            File.WriteAllText(metadata,JsonSerializer.Serialize(new {address=address.ToString(),port=host.Port,
                x=target.Value.X/Math.Max(1,area.Width-1),y=target.Value.Y/Math.Max(1,area.Height-1)}));
            Console.WriteLine("READY real production Windows host, native test-owned field, synthetic MF H264 capture");
            await Until(()=>seen.Contains(65) && seen.Contains(49) && clicks>0 && wheels>0,"Mac native input did not reach test-owned field",45000);
            Assert(field.Text.ToUpperInvariant().StartsWith("A1"),"Mac letter/digit events were not accepted as text");
            Assert(input.Blocked==0 && input.Rejected==0 && input.Posted>=8,"Input was blocked or malformed");
            await Until(()=>input.Held==0,"Mac disconnect did not release held input",10000);
            Console.WriteLine("PASS actual Mac controller -> LAN RMP1 -> production WindowsHost -> SendInput -> WPF test field: text, mouse click, wheel and release");
        } finally {
            Console.WriteLine($"Native test summary: received={input?.Received}, posted={input?.Posted}, blocked={input?.Blocked}, rejected={input?.Rejected}; keys={string.Join(',',seen)}, expectedText={field.Text.ToUpperInvariant().StartsWith("A1")}, clicks={clicks}, wheels={wheels}");
            Console.WriteLine($"Native refusal details: lostForeground={guarded?.LostForeground}, driverFailures={guarded?.NativeFailures}, driverError={guarded?.NativeError}");
            if(target is Point expected && GetCursorPos(out var actual)) Console.WriteLine($"Test pointer delta: x={actual.X-expected.X:F1}, y={actual.Y-expected.Y:F1}");
            if(host!=null) await host.DisposeAsync(); input?.Release();
            bool stillOwned=hwnd!=0 && GetForegroundWindow()==hwnd;
            if(stillOwned && target is Point point && GetCursorPos(out var now) && Math.Abs(now.X-point.X)<3 && Math.Abs(now.Y-point.Y)<3) SetCursorPos(previousPoint.X,previousPoint.Y);
            window.Close(); if(stillOwned && previous!=0) SetForegroundWindow(previous);
            if(File.Exists(metadata)) File.Delete(metadata);
        }
    }
}

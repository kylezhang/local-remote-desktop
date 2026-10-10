using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using LocalRemote;
static partial class Program {
    [DllImport("user32.dll")] static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out CursorPoint point);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
    [StructLayout(LayoutKind.Sequential)] struct CursorPoint { public int X,Y; }
    static async Task CheckNativeWindowsInput() {
        // The optional native check uses only this test-owned window. No existing
        // app receives down events, no clipboard is read, and focus/cursor restore
        // happens only while the test still owns them. Never toggle Num Lock/IME.
        if(new[]{1,2,16,17,18,91,92}.Any(vk=>(GetAsyncKeyState(vk)&0x8000)!=0)) { Console.WriteLine("SKIP native input: local modifiers/buttons are held"); return; }
        var previous=GetForegroundWindow(); GetCursorPos(out var previousPoint);
        var field=new TextBox {Margin=new Thickness(12),MinHeight=44}; InputMethod.SetIsInputMethodEnabled(field,false);
        var panel=new StackPanel(); panel.Children.Add(new TextBlock {Text="LocalRemote isolated input check",Margin=new Thickness(12)}); panel.Children.Add(field);
        var window=new Window {Title="LocalRemote · Input check",Content=panel,Width=360,Height=150,Left=50,Top=50,ShowInTaskbar=false,ResizeMode=ResizeMode.NoResize};
        var seen=new List<int>(); int clicks=0,wheels=0; field.PreviewKeyDown+=(_,e)=>seen.Add(KeyInterop.VirtualKeyFromKey(e.Key)); field.PreviewMouseDown+=(_,_)=>clicks++; field.PreviewMouseWheel+=(_,_)=>wheels++;
        nint hwnd=0; Point? moved=null;
        WindowsHost? host=null; Wire? client=null; WindowsInput? input=null;
        try {
            window.Show(); hwnd=new WindowInteropHelper(window).Handle; window.Activate(); field.Focus(); Keyboard.Focus(field); SetForegroundWindow(hwnd); await Task.Delay(100);
            if(GetForegroundWindow()!=hwnd || !field.IsKeyboardFocusWithin) { Console.WriteLine("SKIP native input: OS refused test-window focus"); return; }
            var area=CaptureArea.Primary(); input=new WindowsInput(area,CaptureArea.Virtual(),new GuardedNative(()=>GetForegroundWindow()==hwnd));
            var credential=HostCredential.Create("Office-LAN#42"); host=new WindowsHost(PairingPassword.Derive(credential.Password,credential.Salt),credential.Salt,area,()=>new SharingCapture(),()=>input,address:IPAddress.Loopback,port:0); host.Start();
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(8)); client=new Wire(); await client.Connect("127.0.0.1",credential.Password,deadline.Token,host.Port);
            await client.Json(new {op="handshake",version=1,quality=0},deadline.Token); await client.Read(deadline.Token); await client.Read(deadline.Token); await client.Read(deadline.Token);
            foreach(int vk in new[]{65,49,97}) {
                if(GetForegroundWindow()!=hwnd) throw new OperationCanceledException("Test lost foreground");
                await client.Json(new {op="key_event",vk,down=true,flags=0},deadline.Token); await client.Json(new {op="key_event",vk,down=false,flags=0},deadline.Token);
            }
            await Until(()=>seen.Contains(65) && seen.Contains(49) && seen.Contains(97),"Native key identities were not received by test field",3000);
            Assert(field.Text.ToUpperInvariant().StartsWith("A1"),"Native letter/main digit were not accepted as text");
            var point=field.PointToScreen(new Point(field.ActualWidth/2,field.ActualHeight/2)); moved=point;
            double x=point.X/Math.Max(1,area.Width-1),y=point.Y/Math.Max(1,area.Height-1);
            if(GetForegroundWindow()!=hwnd || x is <0 or >1 || y is <0 or >1) throw new OperationCanceledException("Test no longer owns primary target");
            await client.Json(new {op="mouse_move",x,y},deadline.Token);
            await client.Json(new {op="mouse_button",button=0,down=true,x,y},deadline.Token); await client.Json(new {op="mouse_button",button=0,down=false,x,y},deadline.Token);
            await client.Json(new {op="scroll",delta=120},deadline.Token);
            await Until(()=>clicks>0 && wheels>0,"Native mouse/wheel not received by test field",3000);
            await client.Json(new {op="disconnect"},deadline.Token); await host.DisposeAsync(); Assert(input.Held==0,"Native disconnect left pressed input");
            Console.WriteLine("PASS actual encrypted Windows host → SendInput → test-owned WPF field: letter/top-row/keypad identities, text, pointer/click/wheel and release");
        } finally {
            client?.Dispose(); if(host!=null) await host.DisposeAsync(); input?.Release();
            bool owned=hwnd!=0 && GetForegroundWindow()==hwnd;
            if(owned && moved is Point target && GetCursorPos(out var now) && Math.Abs(now.X-target.X)<3 && Math.Abs(now.Y-target.Y)<3) SetCursorPos(previousPoint.X,previousPoint.Y);
            window.Close(); if(owned && previous!=0) SetForegroundWindow(previous);
        }
    }
    sealed class GuardedNative(Func<bool> owned):INativeInput {
        readonly SendInputAdapter native=new();
        public bool Post(NativeInput value) {
            bool release=value.Type==1?(value.Data.Key.Flags&2)!=0:(value.Data.Mouse.Flags&(4|16|64))!=0;
            return (owned() || release) && native.Post(value);
        }
    }
}

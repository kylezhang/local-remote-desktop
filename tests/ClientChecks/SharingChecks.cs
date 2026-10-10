using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using LocalRemote;
static partial class Program {
    static async Task CheckWindowsSharing() {
        var credential=HostCredential.Create("Office-LAN#42"); bool failSave=false; int creates=0; bool enabled=false;
        WindowsHost? current=null; var capture=new SharingCapture(); var recorder=new SharingInput(); var area=new CaptureArea(0,0,320,180); var input=new WindowsInput(area,area,recorder);
        var sharing=new SharingWindow(v=>enabled=v,()=>credential,c=> {if(failSave) throw new IOException("Fixture save failure"); credential=c;},(c,key,software,report)=> {
            creates++; current=new WindowsHost(key,c.Salt,area,()=>capture,()=>input,report,IPAddress.Loopback,0); return current;
        }) {Left=-20000,Top=-20000,ShowActivated=false,ShowInTaskbar=false};
        void Click(string name)=>((Button)sharing.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        string Text(string name)=>((TextBlock)sharing.FindName(name)).Text;
        void Draft(string value)=>((TextBox)sharing.FindName("Draft")).Text=value;
        try {
            sharing.Show(); Assert(!enabled && creates==0 && Text("CurrentPassword")=="••••••••••••","Sharing opened a listener/revealed password without action");
            Click("Reveal"); Assert(Text("CurrentPassword")==credential.Password && !enabled,"Password viewing starts sharing"); Click("Reveal");
            Click("SharingStart"); await Until(()=>current!=null && ((Button)sharing.FindName("SharingStart")).IsEnabled,"Sharing failed to start");
            Assert(enabled,"Connect role not disabled while sharing");
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(12)); using var client=new Wire();
            await client.Connect("127.0.0.1",credential.Password,deadline.Token,current!.Port); await client.Json(new {op="handshake",version=1,quality=0},deadline.Token);
            await client.Read(deadline.Token); await client.Read(deadline.Token); await client.Read(deadline.Token);
            await client.Json(new {op="key_event",vk=162,down=true},deadline.Token); await Until(()=>input.Held==1,"Sharing UI backend did not receive input");
            Click("ChangeSharingPassword"); Click("GenerateSharingPassword");
            Assert(((TextBox)sharing.FindName("Draft")).Text.Length==12 && credential.Password=="Office-LAN#42" && input.Held==1,"Generation changed credential/session instead of draft");
            Draft("Office-LAN#42"); Click("SaveSharingPassword");
            Assert(Text("PasswordError").Length>0 && credential.Password=="Office-LAN#42" && input.Held==1 && enabled,"Same password unnecessarily revoked session");
            Draft("hello world"); Click("SaveSharingPassword");
            Assert(Text("PasswordError").Length>0 && input.Held==1,"Invalid draft interrupted sharing");
            Click("CancelSharingPassword"); Assert(input.Held==1 && ((FrameworkElement)sharing.FindName("Editor")).Visibility==Visibility.Collapsed,"Cancel interrupted sharing");
            Click("ChangeSharingPassword"); Draft("New-Office#42"); failSave=true; Click("SaveSharingPassword");
            Assert(credential.Password=="Office-LAN#42" && input.Held==1 && enabled,"Failed password save lost previous session");
            failSave=false; Click("SaveSharingPassword"); await Until(()=>!enabled && input.Held==0 && ((FrameworkElement)sharing.FindName("Editor")).Visibility==Visibility.Collapsed,"Saved credential did not stop/release previous session");
            Assert(credential.Password=="New-Office#42" && Text("CurrentPassword")=="••••••••••••","Saved password not hidden");
            UiText.Current.Use("en"); Assert(((Button)sharing.FindName("SharingStart")).Content.ToString()=="Start sharing" && Text("SharingStatus").StartsWith("Password saved"),"Sharing English labels/status missing");
            if(Environment.GetEnvironmentVariable("LOCALREMOTE_UI_SNAPSHOTS") is string root) {
                sharing.UpdateLayout(); Directory.CreateDirectory(root); var png=new System.Windows.Media.Imaging.PngBitmapEncoder(); var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)sharing.ActualWidth,(int)sharing.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);
                image.Render(sharing); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image)); using var file=File.Create(Path.Combine(root,"sharing-english.png")); png.Save(file);
            }
            UiText.Current.Use("zh-Hans");
            Click("SharingStart"); sharing.Close(); await Until(()=>!sharing.IsVisible && !enabled,"Close during credential derivation restarted sharing");
            Assert(creates==1 && capture.Stops==1,"Close during startup created another listener");
            Console.WriteLine("PASS actual WPF sharing: opt-in, hidden credential, live language, authenticated control, draft/cancel/failed save preservation, successful revocation and close-during-start cleanup");
        } finally { await sharing.Shutdown(); UiText.Current.Use("zh-Hans"); }
        await CheckWindowsTarget();
        if(Environment.GetEnvironmentVariable("LOCALREMOTE_NATIVE_INPUT_CHECK")=="1") await CheckNativeWindowsInput();
    }
    static async Task CheckWindowsTarget() {
        var area=new CaptureArea(0,0,320,180); var recorder=new SharingInput(); var input=new WindowsInput(area,area,recorder);
        var credential=HostCredential.Create("Office-LAN#42"); var key=PairingPassword.Derive(credential.Password,credential.Salt);
        await using var host=new WindowsHost(key,credential.Salt,area,()=>new FfmpegDesktopCapture(area,software:true,synthetic:true),()=>input,address:IPAddress.Loopback,port:0); host.Start();
        System.Windows.Input.ModifierKeys modifiers=System.Windows.Input.ModifierKeys.None;
        var window=new MainWindow(false,()=>modifiers,desktopPort:host.Port) {Left=-20000,Top=-20000,ShowActivated=false,ShowInTaskbar=false};
        try {
            window.Show(); ((TextBox)window.FindName("Address")).Text="127.0.0.1"; ((PasswordBox)window.FindName("Secret")).Password=credential.Password;
            ((MenuItem)window.FindName("Reconnect")).IsChecked=false;
            ((Button)window.FindName("ConnectButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Until(()=>((Image)window.FindName("Desktop")).Source!=null,"WPF client could not display real Windows sharing pipeline");
            Assert(((TextBlock)window.FindName("ConnectionTarget")).Text.StartsWith("Windows"),"Target type not shown");
            Assert(!((MenuItem)window.FindName("AllowConnections")).IsEnabled,"Control role allows accidental simultaneous sharing");
            var surface=(System.Windows.UIElement)window.FindName("Surface");
            void Key(System.Windows.Input.Key key,bool down) {
                var args=new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,System.Windows.PresentationSource.FromVisual(window),0,key) {RoutedEvent=down?System.Windows.Input.Keyboard.PreviewKeyDownEvent:System.Windows.Input.Keyboard.PreviewKeyUpEvent}; surface.RaiseEvent(args);
            }
            modifiers=System.Windows.Input.ModifierKeys.Control; Key(System.Windows.Input.Key.LeftCtrl,true); Key(System.Windows.Input.Key.C,true); Key(System.Windows.Input.Key.C,false); modifiers=System.Windows.Input.ModifierKeys.None; Key(System.Windows.Input.Key.LeftCtrl,false);
            await Until(()=>recorder.Values.Count(x=>x.Type==1)>=4,"Windows-target routed shortcut lost events");
            var keys=recorder.Values.Where(x=>x.Type==1).Take(4).Select(x=>x.Data.Key.Vk).ToArray(); Assert(keys.SequenceEqual(new ushort[]{162,67,67,162}),"Windows shortcut sent Command/Win instead of Control");
            surface.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice,0,-120) {RoutedEvent=System.Windows.Input.Mouse.MouseWheelEvent});
            await Until(()=>recorder.Values.Any(x=>(x.Data.Mouse.Flags&0x800)!=0),"Coordinate-free wheel packet rejected by Windows host");
            window.Close(); await Until(()=>input.Held==0,"Closing Windows controller left held input");
            Console.WriteLine("PASS actual WPF viewer → production Windows host → generated H264/decoder; target-aware Ctrl+C, wheel without coordinates, exclusive role and disconnect cleanup");
        } finally { window.Close(); }
    }
}
sealed class SharingCapture:IDesktopCapture {
    internal int Stops;
    public async Task Run(VideoProfile p,Func<byte[],Task> frame,CancellationToken ct) {
        try { await frame([0,0,0,1,9,0xf0,0,0,1,5,0]); await Task.Delay(Timeout.Infinite,ct); }
        finally { Interlocked.Increment(ref Stops); }
    }
}
sealed class SharingInput:INativeInput {
    internal System.Collections.Concurrent.ConcurrentQueue<NativeInput> Values=new();
    public bool Post(NativeInput value) {Values.Enqueue(value); return true;}
}

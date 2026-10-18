using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using LocalRemoteDesktop;

static partial class Program {
    static async Task CheckSessionInteraction() {
        var root=Path.Combine(Path.GetTempPath(),"lrd-session-picture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var sample=Path.Combine(root,"sample.h264");
        var start=new ProcessStartInfo("ffmpeg.exe") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true};
        foreach(var argument in new[]{"-hide_banner","-loglevel","error","-f","lavfi","-i","testsrc2=size=320x180:rate=30","-t","2","-c:v","libx264","-preset","ultrafast","-tune","zerolatency","-x264-params","aud=1:keyint=30:repeat-headers=1","-f","h264",sample})start.ArgumentList.Add(argument);
        try {
            using(var generator=Process.Start(start)!) {var error=await generator.StandardError.ReadToEndAsync();await generator.WaitForExitAsync();Assert(generator.ExitCode==0,error);}
            using var peer=new MockHost(await File.ReadAllBytesAsync(sample));var serving=peer.Run();
            int passwordReads=0;
            var window=new MainWindow(false,null,new TestClipboard(),new TestFileClipboard(),Path.Combine(root,"received"),peer.Port,passwordLoader:_=> { passwordReads++; return ""; });
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;Task? runner=null;
            T Find<T>(string name)=>(T)window.FindName(name);
            void Invoke(string method,params object[] args)=>typeof(MainWindow).GetMethod(method,flags)!.Invoke(window,args);
            bool Picture() {
                if(Find<Image>("Desktop").Source is not WriteableBitmap bitmap)return false;
                var pixels=new byte[320*180*4];bitmap.CopyPixels(pixels,320*4,0);
                return pixels.Where((_,index)=>index%4!=3).Any(value=>value!=0);
            }
            void Snapshot(string name) {
                var directory=Environment.GetEnvironmentVariable("LOCALREMOTE_UI_SNAPSHOTS");
                var content=(FrameworkElement)window.Content;
                content.Measure(new Size(520,450));content.Arrange(new Rect(0,0,520,450));content.UpdateLayout();
                var bar=Find<FrameworkElement>("Toolbar");
                var action=Find<Button>("ConnectButton").TranslatePoint(new Point(0,0),bar);
                Assert(action.X>=0 && action.X+Find<Button>("ConnectButton").ActualWidth<=bar.ActualWidth+1 &&
                    bar.ActualHeight<55,"Session actions overflow the compact viewport");
                Assert(!window.IsVisible && !window.IsActive,"Session render displayed a window");
                if(directory==null)return;
                var image=new RenderTargetBitmap(520,450,96,96,PixelFormats.Pbgra32);image.Render(content);
                Directory.CreateDirectory(directory);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));
                using var file=File.Create(Path.Combine(directory,name+".png"));png.Save(file);
            }
            try {
                Find<TextBox>("Address").Text="127.0.0.1";Find<PasswordBox>("Secret").Password=peer.Credential;
                Find<CheckBox>("DefaultReconnect").IsChecked=false;
                typeof(MainWindow).GetField("manualConnecting",flags)!.SetValue(window,true);
                runner=(Task)typeof(MainWindow).GetMethod("StartConnectionAsync",flags)!.Invoke(window,null)!;
                await Until(Picture,"Unshown synthetic stream produced no displayed pixels",15000);
                Assert(Find<FrameworkElement>("SessionNotice").Visibility==Visibility.Collapsed &&
                    (string)typeof(MainWindow).GetField("rawStatus",flags)!.GetValue(window)! != "已配对，等待画面…",
                    "First decoded picture retains the waiting overlay/status");
                var originalPicture=Find<Image>("Desktop").Source;
                int originalReads=passwordReads;
                int beforeNavigation=peer.Inputs.Count(m=>m.GetProperty("op").GetString()=="release_all");
                for(int navigation=0;navigation<5;navigation++) {
                    int previousRelease=peer.Inputs.Count(m=>m.GetProperty("op").GetString()=="release_all");
                    Invoke("HomeBackClick",window,new RoutedEventArgs());
                    // The bounded queue deliberately merges redundant releases.
                    // Observe each completed trip before starting the next one.
                    await Until(()=>peer.Inputs.Count(m=>m.GetProperty("op").GetString()=="release_all")>previousRelease,"Leaving the canvas did not release input");
                    Assert(Find<FrameworkElement>("EntryPage").Visibility==Visibility.Visible &&
                        Find<FrameworkElement>("SessionPage").Visibility==Visibility.Collapsed &&
                        Find<FrameworkElement>("ReturnSession").Visibility==Visibility.Visible,"Home did not preserve a return route");
                    Assert(!Find<RadioButton>("NavConnect").IsEnabled && !Find<RadioButton>("NavShare").IsEnabled,"Active session allowed another role");
                    Find<RadioButton>("NavSettings").IsChecked=true;
                    Assert(Find<FrameworkElement>("SettingsPage").Visibility==Visibility.Visible,"Settings unavailable during session");
                    Invoke("Input",new {op="key_event",vk=65,down=true});
                    Invoke("Input",new {op="mouse_button",button=0,down=true,x=.5,y=.5});
                    Invoke("ReturnSessionClick",window,new RoutedEventArgs());
                    Assert(Find<FrameworkElement>("SessionPage").Visibility==Visibility.Visible &&
                        ReferenceEquals(originalPicture,Find<Image>("Desktop").Source) && !runner.IsCompleted,"Navigation replaced the session picture/runner");
                }
                await Until(()=>peer.Inputs.Count(m=>m.GetProperty("op").GetString()=="release_all")>=beforeNavigation+5,"Navigation did not release held input");
                Assert(passwordReads==originalReads && peer.Qualities.Count==1 && peer.Inputs.All(m=>m.GetProperty("op").GetString() is not ("key_event" or "mouse_button")),"Navigation reread credentials, reconnected or leaked hidden-canvas input");
                var language=UiText.Current.Language;
                foreach(var code in new[]{"en","zh-Hans"}) {
                    UiText.Current.Use(code,false);
                    Snapshot("session-live-fixture-"+code);
                }
                UiText.Current.Use(language,false);
                Console.WriteLine("PASS five unshown Home/settings/return round trips preserve authenticated transport/decoder, release input and block hidden-canvas key/mouse input");
                bool Allowed()=>(bool)typeof(MainWindow).GetField("inputAllowed",flags)!.GetValue(window)!;
                await peer.EventPostingPermission(true,false);
                await Until(()=>!Allowed(),"AX approval masked denied event posting");
                Snapshot("session-view-only");
                await peer.EventPostingPermission(false,true);
                await Until(Allowed,"Event posting approval was overridden by AX state");
                await peer.Permission(false);
                await Until(()=>!Allowed(),"Legacy Mac permission fallback was lost");
                await peer.Permission(true);
                await Until(Allowed,"Legacy Mac approval did not recover");
                Console.WriteLine("PASS encrypted Mac event-posting capability takes precedence over AX, with legacy permission fallback; no OS input");
                var first=Find<Image>("Desktop").Source;
                Find<ComboBox>("Quality").SelectedIndex=1;
                await Until(()=>peer.Qualities.Count==2 && !ReferenceEquals(first,Find<Image>("Desktop").Source) && Picture(),"Quality switch did not replace decoder/picture",15000);
                first=Find<Image>("Desktop").Source;
                Find<ComboBox>("Quality").SelectedIndex=2;Find<ComboBox>("Quality").SelectedIndex=0;
                await Until(()=>peer.Qualities.Count==3 && !ReferenceEquals(first,Find<Image>("Desktop").Source) && Picture(),"Rapid quality changes failed",15000);
                Assert(peer.Qualities.ToArray().SequenceEqual(new[]{0,1,0}),"Rapid change did not retain latest quality");
                Assert(Find<ComboBox>("DefaultQuality").SelectedIndex==0,"Session quality overwrote future default");
                // Release uses the actual encrypted writer, but no physical key
                // is synthesized. Window-state tests never show/activate a window.
                int releases=peer.Inputs.Count(m=>m.GetProperty("op").GetString()=="release_all");
                typeof(MainWindow).GetField("pasteSuppressed",flags)!.SetValue(window,true);
                var surface=Find<UIElement>("Surface");
                surface.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice,0,surface,Find<Button>("ConnectButton")) {RoutedEvent=Keyboard.LostKeyboardFocusEvent});
                await Until(()=>peer.Inputs.Count(m=>m.GetProperty("op").GetString()=="release_all")>releases,"Focus-loss release was not sent");
                Assert(!(bool)typeof(MainWindow).GetField("pasteSuppressed",flags)!.GetValue(window)!,"Focus loss retains suppressed paste");
                var focus=Keyboard.FocusedElement;var state=window.WindowState;var style=window.WindowStyle;
                Invoke("ToggleFull");Assert(window.WindowStyle==WindowStyle.None && Find<FrameworkElement>("Toolbar").Visibility==Visibility.Collapsed,"Fullscreen did not hide chrome");
                Invoke("ToggleFull");Assert(window.WindowState==state && window.WindowStyle==style && Find<FrameworkElement>("Toolbar").Visibility==Visibility.Visible,"Fullscreen exit lost window state/chrome");
                Assert(!window.IsVisible && !window.IsActive && ReferenceEquals(focus,Keyboard.FocusedElement),"Background/fullscreen fixture changed desktop focus");
                Invoke("ToggleFull");Find<ComboBox>("Quality").SelectedIndex=2;
                await Until(()=>Find<Image>("Desktop").Source==null,"Quality cancellation did not clear picture");
                Invoke("HomeBackClick",window,new RoutedEventArgs());
                Find<RadioButton>("NavSettings").IsChecked=true;
                typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
                await runner.WaitAsync(TimeSpan.FromSeconds(4));
                Assert(Find<FrameworkElement>("SettingsPage").Visibility==Visibility.Visible &&
                    Find<FrameworkElement>("ReturnSession").Visibility==Visibility.Collapsed,"Disconnect hijacked settings or left a stale return route");
                await Task.Delay(3200);
                Assert(peer.Qualities.Count==3 && window.WindowState==state && window.WindowStyle==style,"Disconnect during quality wait retried or left fullscreen");
                Console.WriteLine("PASS unshown production client + encrypted peer + generated H264: decoded pixels, fresh quality decoder, rapid latest choice with reconnect off, unchanged defaults, focus-loss release/paste reset, fullscreen state restoration and canceled quality retry; no OS input/capture");
            } finally {
                if(typeof(MainWindow).GetField("session",flags)!.GetValue(window)!=null)typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
                if(runner!=null)await runner.WaitAsync(TimeSpan.FromSeconds(5));window.Close();peer.Dispose();
                try {await serving;} catch(Exception error) when(error is OperationCanceledException or ObjectDisposedException or IOException or SocketException) { }
            }
            await CheckQualityDuringAuthentication();
        } finally {Directory.Delete(root,true);}
    }
    static async Task CheckQualityDuringAuthentication() {
        var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        var window=new MainWindow(false,null,desktopPort:((IPEndPoint)listener.LocalEndpoint).Port);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;Task? runner=null;
        try {
            ((TextBox)window.FindName("Address")).Text="127.0.0.1";((PasswordBox)window.FindName("Secret")).Password="Fixture1";
            typeof(MainWindow).GetField("manualConnecting",flags)!.SetValue(window,true);
            runner=(Task)typeof(MainWindow).GetMethod("StartConnectionAsync",flags)!.Invoke(window,null)!;
            using var peer=await listener.AcceptTcpClientAsync();
            ((ComboBox)window.FindName("Quality")).SelectedIndex=2;
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(2));
            int read=await peer.GetStream().ReadAsync(new byte[1],deadline.Token);
            Assert(read==0,"Changing quality did not promptly cancel stalled authentication");
            typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
            await runner.WaitAsync(TimeSpan.FromSeconds(3));
            Assert(!window.IsVisible && !window.IsActive,"Stalled authentication fixture opened a window");
            Console.WriteLine("PASS quality change immediately cancels a stalled authentication socket; explicit disconnect cancels scheduled retry");
        } finally {
            listener.Stop();if(typeof(MainWindow).GetField("session",flags)!.GetValue(window)!=null)typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
            if(runner!=null)await runner.WaitAsync(TimeSpan.FromSeconds(4));window.Close();
        }
    }
}

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LocalRemoteDesktop;

static partial class Program {
    [STAThread] static int Main() {
        int exit=0; var app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
        app.Startup+=async (_,_)=> { try { await Check(); } catch(Exception ex) { Console.Error.WriteLine(ex); exit=1; } finally { app.Shutdown(); } };
        app.Run(); return exit;
    }
    static void Assert(bool value,string message) { if(!value) throw new Exception(message); }
    static async Task Until(Func<bool> condition,string message,int ms=10000) {
        var watch=Stopwatch.StartNew(); while(!condition()) { if(watch.ElapsedMilliseconds>ms) {
            var status=(Application.Current.MainWindow?.FindName("Status") as TextBlock)?.Text;
            throw new TimeoutException($"{message}; client status: {status}");
        } await Task.Delay(20); }
    }
    static async Task Check() {
        if(Environment.GetEnvironmentVariable("LOCALREMOTE_SHELL_CHECK")=="1") { await CheckShell(); return; }
        if(Environment.GetEnvironmentVariable("LOCALREMOTE_LAN_INPUT_CHECK")=="1") { await CheckLANWindowsInput(); return; }
        // Legacy integration fixtures create real windows. Keep routine/CI runs
        // unshown; interactive acceptance requires an explicitly agreed period.
        if(Environment.GetEnvironmentVariable("LOCALREMOTE_INTERACTIVE_CHECKS")!="1") { await CheckShell(); return; }
        UiText.Current.Use("zh-Hans");
        await CheckLanguages();
        await CheckWindowsSharing();
        await CheckDiscovery();
        var directory=Path.Combine(Path.GetTempPath(),"localremote-client-check-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var sample=Path.Combine(directory,"sample.h264");
        var start=new ProcessStartInfo("ffmpeg.exe") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true };
        foreach(var arg in new[]{"-hide_banner","-loglevel","error","-f","lavfi","-i","testsrc2=size=320x180:rate=30","-t","2","-c:v","libx264","-preset","ultrafast","-tune","zerolatency","-x264-params","aud=1:keyint=30:repeat-headers=1","-f","h264",sample}) start.ArgumentList.Add(arg);
        try {
            using(var process=Process.Start(start)!) { var error=await process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync(); Assert(process.ExitCode==0,error); }
            using var host=new MockHost(await File.ReadAllBytesAsync(sample));
            var server=host.Run();
            // No persisted user credentials are read or changed, no physical input is
            // synthesized, and the offscreen test window does not activate itself.
            ModifierKeys fakeModifiers=ModifierKeys.None;
            var board=new TestClipboard(); board.Copy("existing private clipboard");
            var fileBoard=new TestFileClipboard();
            var fileRoot=Path.Combine(directory,"received");
            using var fileServer=new MockFileServer(host.Key,host.FileSession,id=>host.FilesAllowed && id==host.FileRequestId,host.PasswordSalt);
            host.FilePort=fileServer.Port;
            var fileRunner=fileServer.Run();
            var window=new MainWindow(false,()=>fakeModifiers,board,fileBoard,fileRoot,host.Port,fileServer.Port) { Left=-20000,Top=-20000,ShowActivated=false,ShowInTaskbar=false };
            try {
                window.Show();
                var toolbar=(FrameworkElement)window.FindName("Toolbar");
                var fields=(FrameworkElement)window.FindName("ConnectionFields");
                var notice=(FrameworkElement)window.FindName("TransferNotice");
                var metrics=(MenuItem)window.FindName("ShowPerformance");
                Assert(!metrics.IsChecked && notice.Visibility==Visibility.Collapsed,"idle UI shows optional details");
                Assert(ReferenceEquals(notice.Parent,toolbar.Parent),"file notice would be hidden with fullscreen toolbar");
                void Snapshot(string name) {
                    var root=Environment.GetEnvironmentVariable("LOCALREMOTE_UI_SNAPSHOTS"); if(string.IsNullOrEmpty(root)) return;
                    window.UpdateLayout(); Directory.CreateDirectory(root);
                    var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(toolbar.ActualWidth),(int)Math.Ceiling(toolbar.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
                    var drawing=new System.Windows.Media.DrawingVisual();
                    using(var context=drawing.RenderOpen()) {
                        var bounds=new Rect(0,0,toolbar.ActualWidth,toolbar.ActualHeight);
                        context.DrawRectangle(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(247,248,255)),null,bounds);
                        context.DrawRectangle(new System.Windows.Media.VisualBrush(toolbar),null,bounds);
                    }
                    image.Render(drawing); var png=new System.Windows.Media.Imaging.PngBitmapEncoder(); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                    using var stream=File.Create(Path.Combine(root,name+".png")); png.Save(stream);
                }
                window.Width=720; window.UpdateLayout(); Assert(toolbar.ActualHeight<40,"toolbar wraps excessively at minimum window width");
                Snapshot("disconnected-720"); window.Width=1280;

                ((TextBox)window.FindName("Address")).Text="127.0.0.1";
                ((PasswordBox)window.FindName("Secret")).Password=host.Credential;
                ((MenuItem)window.FindName("Reconnect")).IsChecked=false;
                var connect=(Button)window.FindName("ConnectButton"); var surface=(UIElement)window.FindName("Surface");
                connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>((Image)window.FindName("Desktop")).Source!=null,"real client did not decode/display mock H264");
                Assert(fields.Visibility==Visibility.Collapsed && ((FrameworkElement)window.FindName("ConnectionTarget")).Visibility==Visibility.Visible,"connected credentials remain visible");
                Assert(notice.Visibility==Visibility.Collapsed,"idle sharing consumes toolbar space");
                Snapshot("connected-1280");
                Console.WriteLine("PASS compact minimum-width toolbar, hidden connected credentials and optional details");
                Console.WriteLine("PASS real WPF client authenticates, receives H264 and creates displayed bitmap");
                Assert(!InputMethod.GetIsInputMethodEnabled(surface),"local IME can consume remote input");
                foreach(var key in new[]{Key.A,Key.D0,Key.D1,Key.NumPad0,Key.NumPad9,Key.Enter,Key.LeftCtrl,Key.LeftAlt,Key.LWin}) {
                    foreach(bool down in new[]{true,false}) {
                        var e=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window)!,Environment.TickCount,key) { RoutedEvent=down?Keyboard.PreviewKeyDownEvent:Keyboard.PreviewKeyUpEvent };
                        surface.RaiseEvent(e); Assert(e.Handled,"key routed to local navigation");
                    }
                }
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event")==18,"keyboard route/queue/transport lost events");
                var keys=host.Inputs.Where(x=>x.GetProperty("op").GetString()=="key_event").ToArray();
                Assert(keys.Select(x=>x.GetProperty("vk").GetInt32()).SequenceEqual(new[]{65,65,48,48,49,49,96,96,105,105,13,13,91,91,164,164,162,162}),"unexpected VK mapping");
                Assert(keys.Select(x=>x.GetProperty("down").GetBoolean()).SequenceEqual(Enumerable.Range(0,18).Select(i=>i%2==0)),"lost press/release order");
                Console.WriteLine("PASS routed letter, top-row/keypad digits, Enter, Ctrl/Alt/Win → authenticated mock host, in order");
                void Raise(Key key,bool down) {
                    var e=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window)!,Environment.TickCount,key) { RoutedEvent=down?Keyboard.PreviewKeyDownEvent:Keyboard.PreviewKeyUpEvent };
                    surface.RaiseEvent(e); Assert(e.Handled,"shortcut escaped remote input routing");
                }
                foreach(var letter in new[]{Key.C,Key.V,Key.X,Key.A,Key.Z,Key.Y,Key.S,Key.F}) {
                    fakeModifiers=ModifierKeys.Control; Raise(Key.LeftCtrl,true); Raise(letter,true); Raise(letter,false);
                    fakeModifiers=ModifierKeys.None; Raise(Key.LeftCtrl,false);
                }
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event")==50,"Ctrl shortcut chords were not delivered");
                var chords=host.Inputs.Where(x=>x.GetProperty("op").GetString()=="key_event").Skip(18).ToArray();
                for(int i=0;i<8;i++) {
                    var chord=chords.Skip(i*4).Take(4).ToArray();
                    Assert(chord[0].GetProperty("vk").GetInt32()==91 && chord[3].GetProperty("vk").GetInt32()==91,"Ctrl modifier VK not mapped on both edges");
                    Assert(chord.Select(x=>x.GetProperty("flags").GetInt32()).SequenceEqual(new[]{8,8,8,0}),"Command flags incomplete on shortcut chord");
                    Assert(chord.Select(x=>x.GetProperty("down").GetBoolean()).SequenceEqual(new[]{true,true,false,false}),"shortcut release order changed");
                }
                Console.WriteLine("PASS Ctrl+C/V/X/A/Z/Y/S/F chords: Command VK/flags, letter edges and releases → encrypted mock host");
                fakeModifiers=ModifierKeys.Control; Raise(Key.LeftCtrl,true);
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event")==51,"held Ctrl not delivered");
                int releases=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all");
                ((MenuItem)window.FindName("WindowsShortcuts")).IsChecked=false;
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all")>releases,"keyboard mode switch left modifiers held");
                fakeModifiers=ModifierKeys.None; Raise(Key.LeftCtrl,false);
                fakeModifiers=ModifierKeys.Control|ModifierKeys.Shift; Raise(Key.RightCtrl,true); Raise(Key.V,true); Raise(Key.V,false);
                fakeModifiers=ModifierKeys.None; Raise(Key.RightCtrl,false);
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event")==56,"native shortcut mode lost events");
                var native=host.Inputs.Where(x=>x.GetProperty("op").GetString()=="key_event").Skip(52).ToArray();
                Assert(native[0].GetProperty("vk").GetInt32()==163 && native[3].GetProperty("vk").GetInt32()==163,"native right Control mapping changed");
                Assert(native.Select(x=>x.GetProperty("flags").GetInt32()).SequenceEqual(new[]{3,3,3,0}),"native Control+Shift flags changed");
                releases=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all");
                ((MenuItem)window.FindName("WindowsShortcuts")).IsChecked=true;
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all")>releases,"return to Windows shortcut mode did not release keys");
                releases=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all");
                fakeModifiers=ModifierKeys.Control|ModifierKeys.Alt; Raise(Key.Escape,true); fakeModifiers=ModifierKeys.None;
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all")>releases,"physical release shortcut did not work with remapping");
                Assert(!host.Inputs.Any(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==27),"local escape shortcut sent to Mac");
                Console.WriteLine("PASS keyboard mode switch releases held keys; native Control+Shift preserved; physical Ctrl+Alt+Esc stays local");
                var center=new Point(((FrameworkElement)surface).ActualWidth/2,((FrameworkElement)surface).ActualHeight/2);
                window.SendPointerButton(center,MouseButton.Left,true);
                window.SendPointerMove(new Point(-50,5000),true);
                window.SendPointerButton(new Point(-50,5000),MouseButton.Left,false);
                window.SendPointerButton(center,MouseButton.Right,true); window.SendPointerButton(center,MouseButton.Right,false);
                surface.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,120) { RoutedEvent=UIElement.MouseWheelEvent });
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="mouse_button")==4 && host.Inputs.Any(x=>x.GetProperty("op").GetString()=="scroll"),"pointer/scroll not delivered");
                var drag=host.Inputs.First(x=>x.GetProperty("op").GetString()=="mouse_move");
                Assert(drag.GetProperty("x").GetDouble()==0 && drag.GetProperty("y").GetDouble()==1,"outside drag not clamped");
                Console.WriteLine("PASS client pointer conversion, left drag beyond viewport, right click and routed wheel → mock host");
                surface.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice,Environment.TickCount,surface,connect) { RoutedEvent=Keyboard.LostKeyboardFocusEvent });
                await Until(()=>host.Inputs.Any(x=>x.GetProperty("op").GetString()=="release_all"),"focus loss did not release remote input");
                Console.WriteLine("PASS focus loss sends release_all");
                await host.Permission(false);
                await Until(()=>((TextBlock)window.FindName("Status")).Text.Contains("只能查看"),"permission denial not shown");
                await host.Permission(true);
                await Until(()=>!((TextBlock)window.FindName("Status")).Text.Contains("只能查看"),"permission recovery not shown");
                Console.WriteLine("PASS Mac permission denial/recovery is reflected by real client");
                metrics.IsChecked=true;
                await Until(()=>((TextBlock)window.FindName("Status")).Text.Contains("Mbps"),"performance setting did not reveal metrics");
                metrics.IsChecked=false;
                await Until(()=>!((TextBlock)window.FindName("Status")).Text.Contains("Mbps"),"performance setting did not restore concise status");
                Console.WriteLine("PASS optional performance menu shows and hides counters");
                var clipboardOption=(MenuItem)window.FindName("ClipboardSync");
                var clipboardStatus=(TextBlock)window.FindName("ClipboardStatus");
                await Until(()=>clipboardStatus.Text.Contains("已启用"),"default text sharing did not negotiate");
                Assert(clipboardOption.IsChecked && board.Reads==0 && !host.Inputs.Any(x=>x.GetProperty("op").GetString()=="clipboard_text"),"default sharing sent pre-existing clipboard");
                clipboardOption.IsChecked=true;
                await Until(()=>clipboardStatus.Text.Contains("已启用"),"clipboard negotiation failed");
                Assert(board.Reads==0 && !host.Inputs.Any(x=>x.GetProperty("op").GetString()=="clipboard_text"),"enable sent existing clipboard");
                board.Copy("Windows → Mac 中文\r\nemoji 😀");
                fakeModifiers=ModifierKeys.Control; Raise(Key.LeftCtrl,true); Raise(Key.V,true); Raise(Key.V,false); fakeModifiers=ModifierKeys.None; Raise(Key.LeftCtrl,false);
                await Until(()=>host.Inputs.Any(x=>x.GetProperty("op").GetString()=="clipboard_text"),"local copy not sent");
                var transfers=host.Inputs.ToArray(); int copyIndex=Array.FindIndex(transfers,x=>x.GetProperty("op").GetString()=="clipboard_text");
                Assert(TextClipboardSync.Decode(transfers[copyIndex].GetProperty("text").GetString()!)==board.Text,"text corrupted on encrypted transport");
                await Until(()=>host.Inputs.Skip(copyIndex+1).Any(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==86 && x.GetProperty("down").GetBoolean()),"paste key ordering failed");
                int textCount=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="clipboard_text");
                await host.ClipboardText(TextClipboardSync.Encode("Mac → Windows\n第二行 🎉")!);
                await Until(()=>board.Text=="Mac → Windows\n第二行 🎉","remote clipboard not applied");
                await Task.Delay(650);
                Assert(host.Inputs.Count(x=>x.GetProperty("op").GetString()=="clipboard_text")==textCount,"incoming clipboard echoed");
                await host.ClipboardText("_w=="); await host.ClipboardText(new string('A',TextClipboardSync.MaxBase64+4));
                await Task.Delay(100); Assert(board.Text=="Mac → Windows\n第二行 🎉","malformed clipboard applied");
                await host.ClipboardConsent(false);
                await Until(()=>clipboardStatus.Text.Contains("等待 Mac 开启"),"Mac revocation not reflected");
                int writesBefore=board.Writes; await host.ClipboardText(TextClipboardSync.Encode("disabled remote")!);
                board.Copy("copied while disabled"); await Task.Delay(600);
                Assert(board.Writes==writesBefore && host.Inputs.Count(x=>x.GetProperty("op").GetString()=="clipboard_text")==textCount,"clipboard moved without mutual consent");
                await host.ClipboardConsent(true); await Until(()=>clipboardStatus.Text.Contains("已启用"),"Mac consent recovery failed");
                await Task.Delay(600); Assert(host.Inputs.Count(x=>x.GetProperty("op").GetString()=="clipboard_text")==textCount,"permission recovery sent offline clipboard");
                long oldRequest=host.ClipboardRequestId; clipboardOption.IsChecked=false;
                await host.ClipboardAck(true,oldRequest); await host.ClipboardText(TextClipboardSync.Encode("stale remote")!,oldRequest);
                await Task.Delay(100); Assert(board.Text=="copied while disabled" && clipboardStatus.Text.Contains("关闭"),"stale acknowledgement enabled clipboard");
                clipboardOption.IsChecked=true; await Until(()=>clipboardStatus.Text.Contains("已启用"),"re-enable failed");
                await host.Capabilities(false); await Until(()=>clipboardStatus.Text.Contains("版本不支持"),"old peer did not degrade safely");
                await host.Capabilities(true); await Until(()=>clipboardStatus.Text.Contains("已启用"),"new peer recovery failed");
                Console.WriteLine("PASS WPF encrypted clipboard: mutual consent, both directions, before-paste ordering, Unicode, no echo, invalid payloads, revocation, stale IDs and old-peer fallback");
                var fileOption=(MenuItem)window.FindName("FileSync");
                var fileStatus=(TextBlock)window.FindName("FileStatus");
                await Until(()=>fileStatus.Text.Contains("已启用"),"default file sharing did not negotiate");
                Assert(fileOption.IsChecked && fileBoard.Reads==0,"default file sharing sent pre-existing files");
                fileOption.IsChecked=true;
                await Until(()=>fileStatus.Text.Contains("已启用"),"file consent failed");
                Assert(fileBoard.Reads==0 && fileServer.Uploaded.IsEmpty,"enable sent existing files");
                string file=Path.Combine(directory,"文件 📎.txt"); byte[] data=RandomNumberGenerator.GetBytes(8000003); File.WriteAllBytes(file,data);
                fileServer.CompletionDelayMilliseconds=400;
                fileBoard.Copy([file]);
                int pastes=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==86 && x.GetProperty("down").GetBoolean());
                fakeModifiers=ModifierKeys.Control; Raise(Key.V,true); Raise(Key.V,false); fakeModifiers=ModifierKeys.None;
                await Task.Delay(40);
                Assert(host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==86 && x.GetProperty("down").GetBoolean())==pastes,"early paste sent old clipboard");
                Raise(Key.B,true); Raise(Key.B,false);
                await Until(()=>host.Inputs.Any(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==66),"file upload blocked input");
                Assert(((Button)window.FindName("CancelFiles")).IsEnabled,"file job finished before input concurrency was checked");
                Assert(notice.Visibility==Visibility.Visible && !((Button)window.FindName("DismissFileNotice")).IsEnabled,"active transfer notice missing or dismissible");
                await host.Video();
                await Until(()=> (long)typeof(MainWindow).GetField("frames",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!>0,"video did not decode during file transfer");
                await Until(()=>fileStatus.Text.Contains("正在校验"),"bytes-complete did not show clipboard preparation phase");
                Assert(((Button)window.FindName("CancelFiles")).IsEnabled,"100% data incorrectly treated as ready");
                int beforeReady=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==86 && x.GetProperty("down").GetBoolean());
                fakeModifiers=ModifierKeys.Control; Raise(Key.V,true); Raise(Key.V,false); fakeModifiers=ModifierKeys.None;
                await Task.Delay(40);
                Assert(host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==86 && x.GetProperty("down").GetBoolean())==beforeReady && notice.Visibility==Visibility.Visible,"paste accepted or waiting notice hidden before host ready");
                await Until(()=>fileStatus.Text.Contains("文件已发送"),"automatic upload failed");
                fileServer.CompletionDelayMilliseconds=0;
                ((Button)window.FindName("DismissFileNotice")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(notice.Visibility==Visibility.Collapsed,"completed notice cannot be dismissed");
                Assert(fileServer.Uploaded.TryDequeue(out var upload) && upload[0].SequenceEqual(data),"WPF upload bytes differ");
                fakeModifiers=ModifierKeys.Control; Raise(Key.V,true); Raise(Key.V,false); fakeModifiers=ModifierKeys.None;
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event" && x.GetProperty("vk").GetInt32()==86 && x.GetProperty("down").GetBoolean())==pastes+1,"completed-file paste blocked");
                fileBoard.BusyWrites=3;
                fileServer.Offer=FileSource.Prepare([file]); await host.FileOffer(fileServer.OfferId,fileServer.Offer.Items);
                await Until(()=>fileBoard.Writes==1,"automatic download did not populate file clipboard");
                Assert(File.ReadAllBytes(fileBoard.Paths![0]).SequenceEqual(data),"WPF received file corrupted");
                await Until(()=>fileServer.DownloadClipboardReady.Count>0,"host not told clipboard readiness");
                Assert(fileServer.DownloadClipboardReady.TryDequeue(out var published) && published && fileBoard.BusyWrites==0,"host-ready acknowledged before successful clipboard retry");
                await Task.Delay(650); Assert(fileServer.Uploaded.IsEmpty,"received file echoed back");
                fileBoard.BusyWrites=30; fileServer.DelayMilliseconds=0;
                await host.FileOffer(fileServer.OfferId,fileServer.Offer.Items);
                await Until(()=>fileStatus.Text.Contains("剪贴板忙"),"permanently busy clipboard did not show saved-file fallback");
                await Until(()=>fileServer.DownloadClipboardReady.Count>0,"busy clipboard completion not acknowledged");
                Assert(fileServer.DownloadClipboardReady.TryDequeue(out published) && !published && fileBoard.Writes==1,"busy clipboard reported ready or replaced existing content");
                fileBoard.BusyWrites=0; fileServer.DelayMilliseconds=5;
                Console.WriteLine("PASS file readiness waits for clipboard retry; persistent busy state acknowledges false and preserves received files");
                // A new local clipboard copy wins over an in-progress download.
                fileServer.DelayMilliseconds=30; await host.FileOffer(fileServer.OfferId,fileServer.Offer.Items);
                await Until(()=>((Button)window.FindName("CancelFiles")).IsEnabled,"download did not start");
                fileBoard.Copy(null); await Task.Delay(750); Assert(fileBoard.Writes==1,"download replaced a newer local copy");
                await Until(()=>!Directory.GetDirectories(fileRoot).Any(p=>Path.GetFileName(p).StartsWith(".incoming-")),"cancelled WPF job left partial files");
                await host.FileOffer(fileServer.OfferId,fileServer.Offer.Items);
                await Until(()=>((Button)window.FindName("CancelFiles")).IsEnabled,"cancel-button job did not start");
                ((Button)window.FindName("CancelFiles")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>!Directory.GetDirectories(fileRoot).Any(p=>Path.GetFileName(p).StartsWith(".incoming-")),"cancel button left partial files");
                Assert(fileBoard.Writes==1,"cancelled job changed clipboard");
                await host.FilesConsent(false); await Until(()=>fileStatus.Text.Contains("等待 Mac"),"file consent revocation failed");
                fileBoard.Copy([file]); await Task.Delay(600); Assert(fileServer.Uploaded.IsEmpty,"files sent while revoked");
                await host.FilesConsent(true); await Until(()=>fileStatus.Text.Contains("已启用"),"file consent recovery failed");
                await Task.Delay(600); Assert(fileServer.Uploaded.IsEmpty,"re-enable sent existing files");
                fileOption.IsChecked=false;
                await host.FileOffer(fileServer.OfferId,fileServer.Offer.Items); await Task.Delay(150); Assert(fileBoard.Writes==1,"disabled file offer applied");
                Console.WriteLine("PASS exact WPF file client: copy-triggered encrypted upload/download, input during transfer, paste readiness, no echo, newer copy wins, revocation/recovery and partial cleanup");
                var quality=(ComboBox)window.FindName("Quality");
                Assert(quality.IsEnabled,"quality selector disabled while connected");
                var firstImage=((Image)window.FindName("Desktop")).Source;
                quality.SelectedIndex=1;
                await Until(()=>host.Qualities.Count>=2 && ((Image)window.FindName("Desktop")).Source!=null && !ReferenceEquals(firstImage,((Image)window.FindName("Desktop")).Source),"quality did not reconnect with a fresh image",15000);
                Assert(host.Qualities.ToArray().SequenceEqual(new[]{0,1}),"new quality not sent in handshake");
                firstImage=((Image)window.FindName("Desktop")).Source;
                quality.SelectedIndex=2; quality.SelectedIndex=0;
                await Until(()=>host.Qualities.Count>=3 && ((Image)window.FindName("Desktop")).Source!=null && !ReferenceEquals(firstImage,((Image)window.FindName("Desktop")).Source),"rapid quality changes did not use latest selection",15000);
                Assert(host.Qualities.ToArray().SequenceEqual(new[]{0,1,0}),"stale quality selected on rapid change");
                Console.WriteLine("PASS connected quality changes reauthenticate and refresh decoder; latest rapid selection wins with auto-reconnect unchecked");
                connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>Equals(connect.Content,"连接"),"disconnect did not complete");
                Assert(((Image)window.FindName("Desktop")).Source==null,"stale desktop after disconnect");
                Assert(fields.Visibility==Visibility.Visible && notice.Visibility==Visibility.Collapsed,"disconnect did not restore compact connection UI");
                Console.WriteLine("PASS disconnect cleans video/transport and restores controls");
                var timer=(DispatcherTimer)typeof(MainWindow).GetField("render",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
                Assert(!timer.IsEnabled,"disconnected client still polls display at 60Hz");
                var clipboardTimer=(DispatcherTimer)typeof(MainWindow).GetField("clipboardTimer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
                Assert(!clipboardTimer.IsEnabled,"disconnected clipboard timer is running");
                connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>((Image)window.FindName("Desktop")).Source!=null,"reconnection before close failed");
                var ownedDecoder=typeof(MainWindow).GetField("activeDecoder",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
                var child=(Process)typeof(FfmpegDecoder).GetField("process",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(ownedDecoder)!;
                int childId=child.Id;
                window.Close();
                await Until(()=> { try { using var remaining=Process.GetProcessById(childId); return remaining.HasExited; } catch(ArgumentException) { return true; } },"closing the connected window left FFmpeg running");
                Assert(!timer.IsEnabled,"closed window kept display timer running");
                Assert(!clipboardTimer.IsEnabled,"closed window kept clipboard timer running");
                Console.WriteLine("PASS disconnected display timer stops; closing connected window terminates the owned FFmpeg process");
            } finally { window.Close(); host.Dispose(); fileServer.Dispose(); }
            await fileRunner;
            try { await server; } catch(Exception ex) when(ex is OperationCanceledException or IOException or ObjectDisposedException or SocketException) {}
        } finally { Directory.Delete(directory,true); }
    }
    static async Task CheckDiscovery() {
        var first=new TaskCompletionSource<IReadOnlyList<LanDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts=0; CancellationToken last=default;
        async Task<IReadOnlyList<LanDevice>> Browse(CancellationToken ct) {
            attempts++; last=ct;
            if(attempts==1) return await first.Task.WaitAsync(ct);
            if(attempts==2) throw new IOException("test discovery unavailable");
            await Task.Delay(Timeout.Infinite,ct); return [];
        }
        var window=new MainWindow(false,null,discovery:Browse) {Left=-20000,Top=-20000,ShowActivated=false,ShowInTaskbar=false,Width=720};
        try {
            var address=(TextBox)window.FindName("Address"); var password=(PasswordBox)window.FindName("Secret");
            address.Text="192.168.7.20"; password.Password="Local-test#42"; window.Show();
            await Until(()=>attempts==1,"initial scan missing");
            Assert(address.Text=="192.168.7.20" && password.Password=="Local-test#42","scan overwrote manual credentials");
            first.SetResult(new[]{new LanDevice("Office Mac","192.168.7.10","macos",47980)});
            var button=(Button)window.FindName("DiscoverButton");
            await Until(()=>button.ContextMenu.Items[0] is MenuItem m && m.Header?.ToString()?.Contains("Office Mac")==true,"device list not populated");
            Assert(address.Text=="192.168.7.20","device discovery auto-selected target");
            ((MenuItem)button.ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert(address.Text=="192.168.7.10" && password.Password.Length==0,"selection retained another device's password");
            Assert(((TextBlock)window.FindName("ConnectionTarget")).Visibility==Visibility.Collapsed,"selection started control");
            window.UpdateLayout(); Assert(((FrameworkElement)window.FindName("Toolbar")).ActualHeight<40,"discovery widened toolbar");
            ((MenuItem)button.ContextMenu.Items[2]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            await Until(()=>attempts==2 && button.ContextMenu.Items[0] is MenuItem m && m.Header?.ToString()?.Contains("不可用")==true,"discovery error did not preserve fallback");
            Assert(address.IsEnabled && address.Text=="192.168.7.10","failed scan blocked manual connection");
            ((MenuItem)button.ContextMenu.Items[2]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            await Until(()=>attempts==3,"refresh missing"); window.Close();
            Assert(last.IsCancellationRequested,"window close left discovery running");
            Console.WriteLine("PASS actual WPF discovery menu: no auto-pair/credential overwrite, selection clears mismatched password, compact layout, manual fallback and close cancels scan");
        } finally { window.Close(); }
    }
    static async Task CheckLanguages() {
        var window=new MainWindow(false,null) {Left=-20000,Top=-20000,ShowActivated=false,ShowInTaskbar=false,Width=720};
        try {
            window.Show(); var label=(TextBlock)window.FindName("TargetIpLabel");
            var quality=(ComboBox)window.FindName("Quality"); var board=(MenuItem)window.FindName("ClipboardSync");
            ((TextBox)window.FindName("Address")).Text="192.168.100.250";
            ((PasswordBox)window.FindName("Secret")).Password="Local-test#42";
            ((MenuItem)window.FindName("LanguageEnglish")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            await Until(()=>label.Text=="Target IP","English label/binding missing");
            Assert(((Button)window.FindName("ConnectButton")).Content.ToString()=="Connect" && board.Header.ToString()=="Sync text","English controls not translated");
            Assert(((ComboBoxItem)quality.Items[1]).Content.ToString()=="Standard","quality translation missing");
            Assert(((Button)window.FindName("FullscreenButton")).ToolTip.ToString()!.StartsWith("F11:"),"tooltip translation missing");
            Assert(((TextBox)window.FindName("Address")).ToolTip.ToString()!.StartsWith("Target device"),"long indexer key failed");
            window.UpdateLayout(); var bar=(FrameworkElement)window.FindName("Toolbar");
            Assert(bar.ActualHeight<40,"English toolbar wraps at minimum width");
            var root=Environment.GetEnvironmentVariable("LOCALREMOTE_UI_SNAPSHOTS");
            if(root!=null) {
                Directory.CreateDirectory(root); var png=new System.Windows.Media.Imaging.PngBitmapEncoder();
                var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(bar.ActualWidth),(int)Math.Ceiling(bar.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
                var drawing=new System.Windows.Media.DrawingVisual();
                using(var context=drawing.RenderOpen()) {
                    var bounds=new Rect(0,0,bar.ActualWidth,bar.ActualHeight);
                    context.DrawRectangle(window.Background,null,bounds);
                    context.DrawRectangle(new System.Windows.Media.VisualBrush(bar),null,bounds);
                }
                image.Render(drawing); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                using var stream=File.Create(Path.Combine(root,"english-720.png")); png.Save(stream);
            }
            ((MenuItem)window.FindName("LanguageChinese")).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            await Until(()=>label.Text=="目标IP","Chinese switch did not refresh binding");
            Assert(quality.SelectedIndex==0 && board.IsChecked && ((PasswordBox)window.FindName("Secret")).Password=="Local-test#42","language switch changed session settings or password");
            Assert(((TextBox)window.FindName("Address")).Text=="192.168.100.250","language switch changed target");
            Console.WriteLine("PASS actual WPF live English/Chinese labels, long tooltips, menu/quality, 720-wide layout and preserved credentials/settings");
        } finally { window.Close(); UiText.Current.Use("zh-Hans"); }
    }
}

sealed class MockHost:IDisposable {
    readonly TcpListener listener=new(IPAddress.Loopback,0);
    public int Port=>((IPEndPoint)listener.LocalEndpoint).Port;
    public int FilePort;
    readonly CancellationTokenSource stop=new(TimeSpan.FromSeconds(90));
    readonly SemaphoreSlim writes=new(1);
    readonly byte[] video;
    NetworkStream stream=null!; byte[] tx=[],rx=[]; ulong sent,received; TcpClient? peer;
    public byte[]? PasswordSalt {get;}=Environment.GetEnvironmentVariable("LOCALREMOTE_PASSWORD_CHECK")=="1"?Enumerable.Range(0,16).Select(x=>(byte)x).ToArray():null;
    public byte[] Key {get;}
    public string Credential=>PasswordSalt==null?Convert.ToHexString(Key):"Office-LAN#42";
    public ConcurrentQueue<JsonElement> Inputs {get;}=new();
    public ConcurrentQueue<int> Qualities {get;}=new();
    public long ClipboardRequestId {get;private set;}
    public long FileRequestId {get;private set;}
    public string FileSession {get;}=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public bool FilesAllowed=true;
    bool clipboardAllowed=true;
    public MockHost(byte[] video) { this.video=video; Key=PasswordSalt==null?RandomNumberGenerator.GetBytes(32):Convert.FromHexString("0dca4c657c16ea4e595de60ab5d6bad618ea62de9184dc41c08e40051905d87c"); listener.Start(); }
    static byte[] Join(params byte[][] arrays)=>arrays.SelectMany(x=>x).ToArray();
    static byte[] H(byte[] key,string label,byte[] transcript)=>HMACSHA256.HashData(key,Join(Encoding.ASCII.GetBytes(label),transcript));
    static byte[] Nonce(ulong value) { var n=new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(n.AsSpan(4),value); return n; }
    public Task Permission(bool trusted)=>Json(new {op="heartbeat",accessibility=trusted,inputReceived=Inputs.Count,inputPosted=Inputs.Count,inputBlocked=0,inputRejected=0,inputError=""});
    public Task Video()=>Send(2,video);
    public Task Capabilities(bool clipboard)=>Json(new {op="capabilities",accessibility=true,clipboardText=clipboard,fileClipboard=true,filePort=FilePort,fileSession=FileSession});
    public Task FileOffer(string offer,FileItem[] items)=>Json(new {op="file_offer",offer,items,requestId=FileRequestId});
    public Task FilesConsent(bool enabled) { FilesAllowed=enabled; return Json(new {op="file_sync",enabled,requestId=FileRequestId}); }
    public Task ClipboardAck(bool enabled,long request)=>Json(new {op="clipboard_sync",enabled,requestId=request});
    public Task ClipboardConsent(bool enabled) { clipboardAllowed=enabled; return ClipboardAck(enabled,ClipboardRequestId); }
    public Task ClipboardText(string text,long? request=null)=>Json(new {op="clipboard_text",encoding="utf8-base64url",text,requestId=request??ClipboardRequestId});
    Task Json(object value)=>Send(1,JsonSerializer.SerializeToUtf8Bytes(value));
    async Task Send(byte type,byte[] data) {
        await writes.WaitAsync(stop.Token);
        try { var plain=Join([type],data); var header=new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(header,(uint)(plain.Length+16)); var cipher=new byte[plain.Length]; var tag=new byte[16]; using var aes=new AesGcm(tx,16); aes.Encrypt(Nonce(sent++),plain,cipher,tag,header); await stream.WriteAsync(Join(header,cipher,tag),stop.Token); } finally { writes.Release(); }
    }
    public async Task Run() {
        while(!stop.IsCancellationRequested) {
            try { await Session(); }
            catch(Exception ex) when(ex is IOException or SocketException or ObjectDisposedException) { if(stop.IsCancellationRequested) throw; }
            finally { peer?.Dispose(); }
        }
    }
    async Task Session() {
        sent=received=0; ClipboardRequestId=0;
        peer=await listener.AcceptTcpClientAsync(stop.Token); peer.NoDelay=true; stream=peer.GetStream(); var nonce=RandomNumberGenerator.GetBytes(32);
        await stream.WriteAsync(PasswordSalt==null?Join("RML1"u8.ToArray(),nonce):Join("RMP1"u8.ToArray(),nonce,PasswordSalt),stop.Token); var response=new byte[64]; await stream.ReadExactlyAsync(response,stop.Token);
        var transcript=PasswordSalt==null?Join(nonce,response[..32]):Join("RMP1"u8.ToArray(),PasswordSalt,nonce,response[..32]);
        if(!CryptographicOperations.FixedTimeEquals(response[32..],H(Key,"client",transcript))) throw new CryptographicException("invalid test client proof");
        await stream.WriteAsync(H(Key,"server",transcript),stop.Token); tx=H(Key,"h2c",transcript); rx=H(Key,"c2h",transcript);
        using var hello=JsonDocument.Parse(await Read()); if(hello.RootElement.GetProperty("op").GetString()!="handshake") throw new IOException("no handshake");
        Qualities.Enqueue(hello.RootElement.GetProperty("quality").GetInt32());
        await Json(new {op="video_header",width=320,height=180,fps=30,codec="h264-annexb"}); await Capabilities(true); await Permission(true); await Send(2,video);
        while(true) {
            using var message=JsonDocument.Parse(await Read()); var root=message.RootElement;
            if(root.GetProperty("op").GetString()=="heartbeat") await Permission(true);
            else {
                Inputs.Enqueue(root.Clone());
                if(root.GetProperty("op").GetString()=="clipboard_sync") {
                    ClipboardRequestId=root.GetProperty("requestId").GetInt64();
                    await ClipboardAck(root.GetProperty("enabled").GetBoolean() && clipboardAllowed,ClipboardRequestId);
                }
                if(root.GetProperty("op").GetString()=="file_sync") {
                    FileRequestId=root.GetProperty("requestId").GetInt64();
                    await Json(new {op="file_sync",requestId=FileRequestId,enabled=root.GetProperty("enabled").GetBoolean() && FilesAllowed});
                }
            }
        }
    }
    async Task<byte[]> Read() {
        var header=new byte[4]; await stream.ReadExactlyAsync(header,stop.Token); int size=(int)BinaryPrimitives.ReadUInt32BigEndian(header);
        if(size<17 || size>65536) throw new IOException("invalid input size"); var body=new byte[size]; await stream.ReadExactlyAsync(body,stop.Token); var plain=new byte[size-16]; using var aes=new AesGcm(rx,16); aes.Decrypt(Nonce(received++),body.AsSpan(0,plain.Length),body.AsSpan(plain.Length),plain,header); if(plain[0]!=1) throw new IOException("invalid input type"); return plain[1..];
    }
    public void Dispose() { stop.Cancel(); listener.Stop(); peer?.Dispose(); }
}
sealed class TestClipboard : ITextClipboard {
    public long Revision {get;private set;}
    public string? Text; public int Reads,Writes;
    public void Copy(string text) { Text=text; Revision++; }
    public bool TryRead(out string? text) { Reads++; text=Text; return true; }
    public bool TryWrite(string text) { Writes++; Copy(text); return true; }
}
sealed class TestFileClipboard : IFileClipboard {
    public long Revision {get;private set;}
    public string[]? Paths; public int Reads,Writes,BusyWrites;
    public void Copy(string[]? paths) { Paths=paths; Revision++; }
    public bool TryReadFiles(out string[]? paths) { Reads++; paths=Paths; return true; }
    public bool TryWriteFiles(string[] paths) { if(BusyWrites>0) { BusyWrites--; return false; } Writes++; Copy(paths); return true; }
}

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
using LocalRemote;

static class Program {
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
            var window=new MainWindow(false,()=>fakeModifiers) { Left=-20000,Top=-20000,ShowActivated=false,ShowInTaskbar=false };
            try {
                window.Show();
                ((TextBox)window.FindName("Address")).Text="127.0.0.1";
                ((PasswordBox)window.FindName("Secret")).Password=Convert.ToHexString(host.Key);
                ((CheckBox)window.FindName("Reconnect")).IsChecked=false;
                var connect=(Button)window.FindName("ConnectButton"); var surface=(UIElement)window.FindName("Surface");
                connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>((Image)window.FindName("Desktop")).Source!=null,"real client did not decode/display mock H264");
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
                ((CheckBox)window.FindName("WindowsShortcuts")).IsChecked=false;
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all")>releases,"keyboard mode switch left modifiers held");
                fakeModifiers=ModifierKeys.None; Raise(Key.LeftCtrl,false);
                fakeModifiers=ModifierKeys.Control|ModifierKeys.Shift; Raise(Key.RightCtrl,true); Raise(Key.V,true); Raise(Key.V,false);
                fakeModifiers=ModifierKeys.None; Raise(Key.RightCtrl,false);
                await Until(()=>host.Inputs.Count(x=>x.GetProperty("op").GetString()=="key_event")==56,"native shortcut mode lost events");
                var native=host.Inputs.Where(x=>x.GetProperty("op").GetString()=="key_event").Skip(52).ToArray();
                Assert(native[0].GetProperty("vk").GetInt32()==163 && native[3].GetProperty("vk").GetInt32()==163,"native right Control mapping changed");
                Assert(native.Select(x=>x.GetProperty("flags").GetInt32()).SequenceEqual(new[]{3,3,3,0}),"native Control+Shift flags changed");
                releases=host.Inputs.Count(x=>x.GetProperty("op").GetString()=="release_all");
                ((CheckBox)window.FindName("WindowsShortcuts")).IsChecked=true;
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
                Console.WriteLine("PASS disconnect cleans video/transport and restores controls");
                var timer=(DispatcherTimer)typeof(MainWindow).GetField("render",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
                Assert(!timer.IsEnabled,"disconnected client still polls display at 60Hz");
                connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Until(()=>((Image)window.FindName("Desktop")).Source!=null,"reconnection before close failed");
                var ownedDecoder=typeof(MainWindow).GetField("activeDecoder",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(window)!;
                var child=(Process)typeof(FfmpegDecoder).GetField("process",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(ownedDecoder)!;
                int childId=child.Id;
                window.Close();
                await Until(()=> { try { using var remaining=Process.GetProcessById(childId); return remaining.HasExited; } catch(ArgumentException) { return true; } },"closing the connected window left FFmpeg running");
                Assert(!timer.IsEnabled,"closed window kept display timer running");
                Console.WriteLine("PASS disconnected display timer stops; closing connected window terminates the owned FFmpeg process");
            } finally { window.Close(); host.Dispose(); }
            try { await server; } catch(Exception ex) when(ex is OperationCanceledException or IOException or ObjectDisposedException or SocketException) {}
        } finally { Directory.Delete(directory,true); }
    }
}

sealed class MockHost:IDisposable {
    readonly TcpListener listener=new(IPAddress.Loopback,47980);
    readonly CancellationTokenSource stop=new(TimeSpan.FromSeconds(30));
    readonly SemaphoreSlim writes=new(1);
    readonly byte[] video;
    NetworkStream stream=null!; byte[] tx=[],rx=[]; ulong sent,received; TcpClient? peer;
    public byte[] Key {get;}=RandomNumberGenerator.GetBytes(32);
    public ConcurrentQueue<JsonElement> Inputs {get;}=new();
    public ConcurrentQueue<int> Qualities {get;}=new();
    public MockHost(byte[] video) { this.video=video; listener.Start(); }
    static byte[] Join(params byte[][] arrays)=>arrays.SelectMany(x=>x).ToArray();
    static byte[] H(byte[] key,string label,byte[] transcript)=>HMACSHA256.HashData(key,Join(Encoding.ASCII.GetBytes(label),transcript));
    static byte[] Nonce(ulong value) { var n=new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(n.AsSpan(4),value); return n; }
    public Task Permission(bool trusted)=>Json(new {op="heartbeat",accessibility=trusted,inputReceived=Inputs.Count,inputPosted=Inputs.Count,inputBlocked=0,inputRejected=0,inputError=""});
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
        sent=received=0;
        peer=await listener.AcceptTcpClientAsync(stop.Token); peer.NoDelay=true; stream=peer.GetStream(); var nonce=RandomNumberGenerator.GetBytes(32);
        await stream.WriteAsync(Join("RML1"u8.ToArray(),nonce),stop.Token); var response=new byte[64]; await stream.ReadExactlyAsync(response,stop.Token);
        var transcript=Join(nonce,response[..32]);
        if(!CryptographicOperations.FixedTimeEquals(response[32..],H(Key,"client",transcript))) throw new CryptographicException("invalid test client proof");
        await stream.WriteAsync(H(Key,"server",transcript),stop.Token); tx=H(Key,"h2c",transcript); rx=H(Key,"c2h",transcript);
        using var hello=JsonDocument.Parse(await Read()); if(hello.RootElement.GetProperty("op").GetString()!="handshake") throw new IOException("no handshake");
        Qualities.Enqueue(hello.RootElement.GetProperty("quality").GetInt32());
        await Json(new {op="video_header",width=320,height=180,fps=30,codec="h264-annexb"}); await Permission(true); await Send(2,video);
        while(true) { using var message=JsonDocument.Parse(await Read()); if(message.RootElement.GetProperty("op").GetString()=="heartbeat") await Permission(true); else Inputs.Enqueue(message.RootElement.Clone()); }
    }
    async Task<byte[]> Read() {
        var header=new byte[4]; await stream.ReadExactlyAsync(header,stop.Token); int size=(int)BinaryPrimitives.ReadUInt32BigEndian(header);
        if(size<17 || size>65536) throw new IOException("invalid input size"); var body=new byte[size]; await stream.ReadExactlyAsync(body,stop.Token); var plain=new byte[size-16]; using var aes=new AesGcm(rx,16); aes.Decrypt(Nonce(received++),body.AsSpan(0,plain.Length),body.AsSpan(plain.Length),plain,header); if(plain[0]!=1) throw new IOException("invalid input type"); return plain[1..];
    }
    public void Dispose() { stop.Cancel(); listener.Stop(); peer?.Dispose(); }
}

using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace RemoteMacLAN;
public partial class MainWindow : Window
{
    CancellationTokenSource? session;
    Task? runner;
    Channel<object>? inputs;
    WriteableBitmap? bitmap;
    byte[]? latest;
    int width,height; long frames,bytes; double encodeMs;
    readonly Stopwatch stats=Stopwatch.StartNew(), mouseClock=Stopwatch.StartNew();
    bool full, connected;
    WindowState previousState;
    readonly DispatcherTimer render = new() { Interval=TimeSpan.FromMilliseconds(16) };
    public MainWindow() {
        InitializeComponent(); Secret.Password=SecretStore.Load(Address.Text);
        Address.LostFocus+=(_,_)=> { var s=SecretStore.Load(Address.Text.Trim()); if(s.Length>0) Secret.Password=s; };
        render.Tick+=(_,_)=>Render(); render.Start();
        Surface.MouseMove+=(_,e)=> { if(mouseClock.ElapsedMilliseconds<8) return; mouseClock.Restart(); if(Position(e.GetPosition(Surface)) is {} p) Input(new {op="mouse_move",p.x,p.y}); };
        Surface.MouseDown+=(_,e)=> { Surface.Focus(); Surface.CaptureMouse(); if(Position(e.GetPosition(Surface)) is {} p) Input(new {op="mouse_button",button=Button(e.ChangedButton),down=true,p.x,p.y}); e.Handled=true; };
        Surface.MouseUp+=(_,e)=> { if(Position(e.GetPosition(Surface),true) is {} p) Input(new {op="mouse_button",button=Button(e.ChangedButton),down=false,p.x,p.y}); if(e.LeftButton==MouseButtonState.Released && e.RightButton==MouseButtonState.Released && e.MiddleButton==MouseButtonState.Released) Surface.ReleaseMouseCapture(); e.Handled=true; };
        Surface.MouseWheel+=(_,e)=> { Input(new {op="scroll",delta=e.Delta}); e.Handled=true; };
        Surface.PreviewKeyDown+=(_,e)=>Key(e,true); Surface.PreviewKeyUp+=(_,e)=>Key(e,false);
        Surface.LostKeyboardFocus+=(_,_)=>Input(new {op="release_all"}); Deactivated+=(_,_)=>Input(new {op="release_all"});
        PreviewKeyDown+=(_,e)=> { if(e.Key==System.Windows.Input.Key.F11) { ToggleFull(); e.Handled=true; } };
        Closing+=(_,_)=>session?.Cancel();
    }
    static int Button(MouseButton b)=>b==MouseButton.Right?1:b==MouseButton.Middle?2:0;
    (double x,double y)? Position(Point p,bool clamp=false) {
        if(width==0 || height==0) return null;
        double scale=Math.Min(Surface.ActualWidth/width,Surface.ActualHeight/height);
        double x=(p.X-(Surface.ActualWidth-width*scale)/2)/(width*scale), y=(p.Y-(Surface.ActualHeight-height*scale)/2)/(height*scale);
        if(!clamp && (x<0 || x>1 || y<0 || y>1)) return null;
        return (Math.Clamp(x,0,1),Math.Clamp(y,0,1));
    }
    void Key(KeyEventArgs e,bool down) {
        var key=e.Key==System.Windows.Input.Key.System?e.SystemKey:e.Key;
        if(key==System.Windows.Input.Key.F11) { e.Handled=true; return; }
        if(down && key==System.Windows.Input.Key.Escape && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) { Input(new {op="release_all"}); ConnectButton.Focus(); e.Handled=true; return; }
        int flags=(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?1:0)|(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)?2:0)|(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)?4:0)|(Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)?8:0);
        Input(new {op="key_event",vk=KeyInterop.VirtualKeyFromKey(key),down,flags}); e.Handled=true;
    }
    void Input(object message) { if(connected && inputs?.Writer.TryWrite(message)==false) session?.Cancel(); }
    async void ConnectClick(object sender,RoutedEventArgs e) {
        if(session!=null) { session.Cancel(); return; }
        var ip=Address.Text.Trim(); var secret=Secret.Password.Trim(); int quality=Quality.SelectedIndex;
        session=new(); ConnectButton.Content="断开"; Address.IsEnabled=Secret.IsEnabled=Quality.IsEnabled=false;
        runner=Run(ip,secret,quality,session.Token);
        try { await runner; } finally { connected=false; session.Dispose(); session=null; ConnectButton.Content="连接"; Address.IsEnabled=Secret.IsEnabled=Quality.IsEnabled=true; Desktop.Source=null; bitmap=null; width=height=0; }
    }
    async Task Run(string ip,string secret,int quality,CancellationToken ct) {
        bool paired=false;
        do {
            using var wire=new Wire(); IVideoDecoder? decoder=null;
            using var attempt=CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task? heartbeat=null, controls=null;
            try {
                Status.Text="连接和验证 Mac…";
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct)) { timeout.CancelAfter(TimeSpan.FromSeconds(8)); await wire.Connect(ip,secret,timeout.Token); }
                paired=true; SecretStore.Save(ip,secret);
                inputs=Channel.CreateBounded<object>(256); connected=true;
                await wire.Json(new {op="handshake",version=1,quality},attempt.Token);
                heartbeat=Task.Run(async()=> { try { while(true) { await Task.Delay(3000,attempt.Token); await wire.Json(new {op="heartbeat"},attempt.Token); } } catch { attempt.Cancel(); } });
                controls=Task.Run(async()=> { try { await foreach(var m in inputs.Reader.ReadAllAsync(attempt.Token)) await wire.Json(m,attempt.Token); } catch { attempt.Cancel(); } });
                Surface.Focus(); Status.Text="已配对，等待画面…";
                while(true) {
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(attempt.Token); timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    var packet=await wire.Read(timeout.Token);
                    if(packet.type==1) {
                        using var json=JsonDocument.Parse(packet.data); var root=json.RootElement;
                        if(root.GetProperty("op").GetString()=="video_header") {
                            width=root.GetProperty("width").GetInt32(); height=root.GetProperty("height").GetInt32();
                            if(width<2 || height<2 || width>3840 || height>2160) throw new IOException("Invalid video dimensions");
                            decoder?.Dispose(); decoder=new FfmpegDecoder(width,height); decoder.Frame+=p=> { Interlocked.Exchange(ref latest,p); Interlocked.Increment(ref frames); };
                            _=decoder.Completion.ContinueWith(_=>attempt.Cancel(),TaskScheduler.Default);
                            bitmap=new WriteableBitmap(width,height,96,96,PixelFormats.Bgra32,null); Desktop.Source=bitmap; stats.Restart(); frames=bytes=0;
                        } else if(root.GetProperty("op").GetString()=="metrics") encodeMs=root.GetProperty("encodeMs").GetDouble();
                    } else if(packet.type==2 && decoder!=null) { Interlocked.Add(ref bytes,packet.data.Length); await decoder.Feed(packet.data,attempt.Token); }
                }
            } catch(Exception ex) { Status.Text=ct.IsCancellationRequested?"已断开":$"连接中断：{ex.Message}"; }
            finally {
                connected=false; inputs?.Writer.TryComplete(); attempt.Cancel(); wire.Dispose(); decoder?.Dispose(); Interlocked.Exchange(ref latest,null);
                if(heartbeat!=null) await heartbeat; if(controls!=null) await controls;
            }
            if(ct.IsCancellationRequested || !paired || Reconnect.IsChecked!=true) break;
            Status.Text+="；3 秒后重连";
            try { await Task.Delay(3000,ct); } catch(OperationCanceledException) { break; }
        } while(!ct.IsCancellationRequested);
    }
    void Render() {
        var pixels=Interlocked.Exchange(ref latest,null);
        if(pixels!=null && bitmap!=null && pixels.Length==width*height*4) bitmap.WritePixels(new Int32Rect(0,0,width,height),pixels,width*4,0);
        if(connected && bitmap!=null && stats.ElapsedMilliseconds>=1000) {
            double sec=stats.Elapsed.TotalSeconds; Status.Text=$"{width}×{height} | 解码 {Interlocked.Exchange(ref frames,0)/sec:F1} fps | 接收 {Interlocked.Exchange(ref bytes,0)*8/sec/1e6:F2} Mbps | Mac 编码 {encodeMs:F1} ms | Ctrl+Alt+Esc 释放输入"; stats.Restart();
        }
    }
    void FullscreenClick(object sender,RoutedEventArgs e)=>ToggleFull();
    void ToggleFull() { full=!full; if(full) { previousState=WindowState; WindowState=WindowState.Normal; WindowStyle=WindowStyle.None; WindowState=WindowState.Maximized; Toolbar.Visibility=Visibility.Collapsed; Surface.Focus(); } else { WindowState=WindowState.Normal; WindowStyle=WindowStyle.SingleBorderWindow; WindowState=previousState; Toolbar.Visibility=Visibility.Visible; } }
}

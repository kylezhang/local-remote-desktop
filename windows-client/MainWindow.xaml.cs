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
    CancellationTokenSource? activeAttempt;
    int requestedQuality;
    bool qualityChangePending;
    InputQueue? inputs;
    WriteableBitmap? bitmap;
    byte[]? latest;
    int width,height; long frames,bytes; double encodeMs;
    readonly Stopwatch stats=Stopwatch.StartNew(), mouseClock=Stopwatch.StartNew();
    bool full, connected;
    volatile bool inputAllowed = true;
    volatile bool hostReportsInput;
    long inputsQueued, inputsSent, hostReceived, hostPosted, hostBlocked, hostRejected;
    string hostInputError = "";
    readonly bool rememberConnection;
    readonly Func<ModifierKeys> readModifiers;
    WindowState previousState;
    readonly DispatcherTimer render = new() { Interval=TimeSpan.FromMilliseconds(16) };
    public MainWindow():this(true) {}
    internal MainWindow(bool remember):this(remember,null) {}
    internal MainWindow(bool remember, Func<ModifierKeys>? modifiers) {
        rememberConnection=remember;
        readModifiers=modifiers ?? (()=>Keyboard.Modifiers);
        InitializeComponent(); var savedHost=remember?SecretStore.LastHost():""; Address.Text=savedHost.Length>0?savedHost:LocalAddress.Find(); Secret.Password=remember?SecretStore.Load(Address.Text):"";
        WindowsShortcuts.IsChecked=!remember || SecretStore.LoadWindowsShortcuts();
        WindowsShortcuts.Checked+=(_,_)=>KeyboardModeChanged(); WindowsShortcuts.Unchecked+=(_,_)=>KeyboardModeChanged();
        Address.LostFocus+=(_,_)=> { if(!rememberConnection) return; var s=SecretStore.Load(Address.Text.Trim()); if(s.Length>0) Secret.Password=s; };
        Quality.SelectionChanged+=(_,_)=> {
            if(session==null) return;
            requestedQuality=Quality.SelectedIndex; qualityChangePending=true;
            Input(new {op="release_all"}); activeAttempt?.Cancel();
            Status.Text="正在切换画质，将自动重新连接…";
        };
        render.Tick+=(_,_)=>Render(); render.Start();
        Surface.MouseMove+=(_,e)=> { if(mouseClock.ElapsedMilliseconds<8) return; mouseClock.Restart(); SendPointerMove(e.GetPosition(Surface),Surface.IsMouseCaptured); };
        Surface.MouseDown+=(_,e)=> { Keyboard.Focus(Surface); Surface.CaptureMouse(); SendPointerButton(e.GetPosition(Surface),e.ChangedButton,true); e.Handled=true; };
        Surface.MouseUp+=(_,e)=> { SendPointerButton(e.GetPosition(Surface),e.ChangedButton,false); if(e.LeftButton==MouseButtonState.Released && e.RightButton==MouseButtonState.Released && e.MiddleButton==MouseButtonState.Released) Surface.ReleaseMouseCapture(); e.Handled=true; };
        Surface.MouseWheel+=(_,e)=> { Input(new {op="scroll",delta=e.Delta}); e.Handled=true; };
        Surface.PreviewKeyDown+=(_,e)=>Key(e,true); Surface.PreviewKeyUp+=(_,e)=>Key(e,false);
        Surface.LostKeyboardFocus+=(_,_)=>Input(new {op="release_all"}); Deactivated+=(_,_)=>Input(new {op="release_all"});
        Surface.LostMouseCapture+=(_,_)=> { if(Mouse.LeftButton==MouseButtonState.Pressed || Mouse.RightButton==MouseButtonState.Pressed || Mouse.MiddleButton==MouseButtonState.Pressed) Input(new {op="release_all"}); };
        PreviewKeyDown+=(_,e)=> { if(e.Key==System.Windows.Input.Key.F11) { ToggleFull(); e.Handled=true; } };
        Closing+=(_,_)=>session?.Cancel();
    }
    static int Button(MouseButton b)=>b==MouseButton.Right?1:b==MouseButton.Middle?2:0;
    internal void SendPointerMove(Point point,bool captured) { if(Position(point,captured) is {} p) Input(new {op="mouse_move",p.x,p.y}); }
    internal void SendPointerButton(Point point,MouseButton button,bool down) { if(Position(point,!down) is {} p) Input(new {op="mouse_button",button=Button(button),down,p.x,p.y}); }
    (double x,double y)? Position(Point p,bool clamp=false) => RemoteCoordinates.Map(p.X,p.Y,Surface.ActualWidth,Surface.ActualHeight,width,height,clamp);
    void Key(KeyEventArgs e,bool down) {
        var key=e.Key==System.Windows.Input.Key.System?e.SystemKey:e.Key==System.Windows.Input.Key.ImeProcessed?e.ImeProcessedKey:e.Key==System.Windows.Input.Key.DeadCharProcessed?e.DeadCharProcessedKey:e.Key;
        if(key==System.Windows.Input.Key.F11) { e.Handled=true; return; }
        var modifiers=readModifiers();
        if(down && key==System.Windows.Input.Key.Escape && modifiers.HasFlag(ModifierKeys.Control) && modifiers.HasFlag(ModifierKeys.Alt)) { Input(new {op="release_all"}); ConnectButton.Focus(); e.Handled=true; return; }
        int flags=(modifiers.HasFlag(ModifierKeys.Shift)?1:0)|(modifiers.HasFlag(ModifierKeys.Control)?2:0)|(modifiers.HasFlag(ModifierKeys.Alt)?4:0)|(modifiers.HasFlag(ModifierKeys.Windows)?8:0);
        var mapped=KeyboardMapping.Map(KeyInterop.VirtualKeyFromKey(key),flags,WindowsShortcuts.IsChecked==true);
        Input(new {op="key_event",vk=mapped.VirtualKey,down,flags=mapped.Flags}); e.Handled=true;
    }
    void KeyboardModeChanged() {
        Input(new {op="release_all"});
        if(!rememberConnection) return;
        try { SecretStore.SaveWindowsShortcuts(WindowsShortcuts.IsChecked==true); }
        catch(Exception ex) { Status.Text=$"快捷键模式已切换，但无法保存：{ex.Message}"; }
    }
    void Input(object message) {
        if(!connected || inputs==null) return;
        if(inputs.TryWrite(message)) Interlocked.Increment(ref inputsQueued);
        else { Status.Text="输入队列拥堵，已断开以避免按键卡住"; session?.Cancel(); }
    }
    async void ConnectClick(object sender,RoutedEventArgs e) {
        if(session!=null) { session.Cancel(); return; }
        var ip=Address.Text.Trim(); var secret=Secret.Password.Trim(); int quality=Quality.SelectedIndex;
        try {
            if(ip.Length==0 || Convert.FromHexString(secret).Length!=32) throw new ArgumentException("请填写 Mac IP 和 64 个十六进制字符的配对密钥。");
            if(rememberConnection) SecretStore.Remember(ip,secret);
        } catch(Exception ex) { Status.Text=$"无法记住连接：{ex.Message}"; return; }
        requestedQuality=quality; qualityChangePending=false;
        session=new(); ConnectButton.Content="断开"; Address.IsEnabled=Secret.IsEnabled=ForgetButton.IsEnabled=false;
        runner=Run(ip,secret,session.Token);
        try { await runner; } finally { connected=false; session.Dispose(); session=null; ConnectButton.Content="连接"; Address.IsEnabled=Secret.IsEnabled=Quality.IsEnabled=ForgetButton.IsEnabled=true; Desktop.Source=null; bitmap=null; width=height=0; }
    }
    async Task Run(string ip,string secret,CancellationToken ct) {
        bool paired=false;
        do {
            using var wire=new Wire(); IVideoDecoder? decoder=null;
            using var attempt=CancellationTokenSource.CreateLinkedTokenSource(ct);
            activeAttempt=attempt; qualityChangePending=false;
            int quality=requestedQuality;
            Task? heartbeat=null, controls=null;
            try {
                Status.Text="连接和验证 Mac…";
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct)) { timeout.CancelAfter(TimeSpan.FromSeconds(8)); await wire.Connect(ip,secret,timeout.Token); }
                paired=true; if(rememberConnection) SecretStore.Save(ip,secret);
                inputs=new InputQueue(); connected=true;
                Interlocked.Exchange(ref inputsQueued,0); Interlocked.Exchange(ref inputsSent,0);
                Interlocked.Exchange(ref hostReceived,0); Interlocked.Exchange(ref hostPosted,0);
                Interlocked.Exchange(ref hostBlocked,0); Interlocked.Exchange(ref hostRejected,0); hostInputError="";
                hostReportsInput=false;
                await wire.Json(new {op="handshake",version=1,quality},attempt.Token);
                heartbeat=Task.Run(async()=> { try { while(true) { await Task.Delay(3000,attempt.Token); await wire.Json(new {op="heartbeat"},attempt.Token); } } catch { attempt.Cancel(); } });
                controls=Task.Run(async()=> { try { await foreach(var m in inputs.ReadAllAsync(attempt.Token)) { await wire.Json(m,attempt.Token); Interlocked.Increment(ref inputsSent); } } catch { attempt.Cancel(); } });
                Surface.Focus(); Status.Text="已配对，等待画面…";
                inputAllowed=true;
                // Receive, authenticate/decrypt and feed FFmpeg away from the UI
                // thread; only creation of the displayed bitmap uses Dispatcher.
                await Task.Run(async()=> {
                while(true) {
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(attempt.Token); timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    var packet=await wire.Read(timeout.Token);
                    if(packet.type==1) {
                        using var json=JsonDocument.Parse(packet.data); var root=json.RootElement;
                        if(root.GetProperty("op").GetString()=="video_header") {
                            int videoWidth=root.GetProperty("width").GetInt32(), videoHeight=root.GetProperty("height").GetInt32();
                            if(videoWidth<2 || videoHeight<2 || videoWidth>3840 || videoHeight>2160) throw new IOException("Invalid video dimensions");
                            decoder?.Dispose(); Interlocked.Exchange(ref latest,null);
                            decoder=new FfmpegDecoder(videoWidth,videoHeight); decoder.Frame+=p=> { Interlocked.Exchange(ref latest,p); Interlocked.Increment(ref frames); };
                            _=decoder.Completion.ContinueWith(_=> { try { attempt.Cancel(); } catch(ObjectDisposedException) {} },TaskScheduler.Default);
                            await Dispatcher.InvokeAsync(()=> {
                                width=videoWidth; height=videoHeight;
                                bitmap=new WriteableBitmap(width,height,96,96,PixelFormats.Bgra32,null); Desktop.Source=bitmap;
                                stats.Restart(); Interlocked.Exchange(ref frames,0); Interlocked.Exchange(ref bytes,0);
                            });
                        } else if(root.GetProperty("op").GetString()=="metrics") Interlocked.Exchange(ref encodeMs,root.GetProperty("encodeMs").GetDouble());
                        if(root.TryGetProperty("accessibility",out var permission)) inputAllowed=permission.GetBoolean();
                        if(root.TryGetProperty("inputReceived",out var received)) { Interlocked.Exchange(ref hostReceived,received.GetInt64()); hostReportsInput=true; }
                        if(root.TryGetProperty("inputPosted",out var posted)) Interlocked.Exchange(ref hostPosted,posted.GetInt64());
                        if(root.TryGetProperty("inputBlocked",out var blocked)) Interlocked.Exchange(ref hostBlocked,blocked.GetInt64());
                        if(root.TryGetProperty("inputRejected",out var rejected)) Interlocked.Exchange(ref hostRejected,rejected.GetInt64());
                        if(root.TryGetProperty("inputError",out var error)) hostInputError=error.GetString()??"";
                    } else if(packet.type==2 && decoder!=null) { Interlocked.Add(ref bytes,packet.data.Length); await decoder.Feed(packet.data,attempt.Token); }
                }
                },attempt.Token);
            } catch(Exception ex) { Status.Text=ct.IsCancellationRequested?"已断开":qualityChangePending?"正在切换画质":$"连接中断：{ex.Message}"; }
            finally {
                connected=false; inputs?.Complete(); attempt.Cancel(); wire.Dispose(); decoder?.Dispose(); Interlocked.Exchange(ref latest,null);
                if(heartbeat!=null) await heartbeat; if(controls!=null) await controls;
                activeAttempt=null;
                if(Surface.IsMouseCaptured) Surface.ReleaseMouseCapture();
                Desktop.Source=null; bitmap=null; width=height=0;
            }
            if(ct.IsCancellationRequested || (!qualityChangePending && (!paired || Reconnect.IsChecked!=true))) break;
            Status.Text+="；3 秒后重连";
            try { await Task.Delay(3000,ct); } catch(OperationCanceledException) { break; }
        } while(!ct.IsCancellationRequested);
    }
    void Render() {
        var pixels=Interlocked.Exchange(ref latest,null);
        if(pixels!=null && bitmap!=null && pixels.Length==width*height*4) bitmap.WritePixels(new Int32Rect(0,0,width,height),pixels,width*4,0);
        if(connected && bitmap!=null && stats.ElapsedMilliseconds>=1000) {
            string inputStatus=!inputAllowed?"Mac 辅助功能未授权：只能查看":Surface.IsKeyboardFocusWithin?"键盘已启用；Ctrl+Alt+Esc 释放":"点击画面启用键盘";
            string feedback=hostReportsInput?$"Mac 收到 {Interlocked.Read(ref hostReceived)} / 已提交 {Interlocked.Read(ref hostPosted)}":"Mac 当前版本未回报输入诊断";
            double sec=stats.Elapsed.TotalSeconds; Status.Text=$"{width}×{height} | 解码 {Interlocked.Exchange(ref frames,0)/sec:F1} fps | 接收 {Interlocked.Exchange(ref bytes,0)*8/sec/1e6:F2} Mbps | Mac 编码 {Volatile.Read(ref encodeMs):F1} ms | {inputStatus} | 输入 已发 {Interlocked.Read(ref inputsSent)} / {feedback}"; stats.Restart();
        }
    }
    void FullscreenClick(object sender,RoutedEventArgs e)=>ToggleFull();
    void DiagnosticsClick(object sender,RoutedEventArgs e) {
        Clipboard.SetText($"LocalRemote input diagnostics\nConnected: {connected}\nKeyboardFocus: {Surface.IsKeyboardFocusWithin}\nWindowsShortcuts: {WindowsShortcuts.IsChecked==true}\nAccessibility: {inputAllowed}\nHostReportsInput: {hostReportsInput}\nQueued: {Interlocked.Read(ref inputsQueued)}\nSent: {Interlocked.Read(ref inputsSent)}\nHostReceived: {Interlocked.Read(ref hostReceived)}\nHostPosted: {Interlocked.Read(ref hostPosted)}\nHostBlocked: {Interlocked.Read(ref hostBlocked)}\nHostRejected: {Interlocked.Read(ref hostRejected)}\nHostError: {hostInputError}\nPosted means submitted to CGEvent, not confirmed application acceptance.");
        Status.Text="诊断已复制（不包含密钥、IP 或输入内容）";
    }
    void ForgetClick(object sender,RoutedEventArgs e) {
        try { SecretStore.Forget(Address.Text.Trim()); Secret.Password=""; Address.Text=LocalAddress.Find(); Status.Text="已清除当前 Mac 的 IP 和配对密钥记忆。"; }
        catch(Exception ex) { Status.Text=$"清除失败：{ex.Message}"; }
    }
    void ToggleFull() { full=!full; if(full) { previousState=WindowState; WindowState=WindowState.Normal; WindowStyle=WindowStyle.None; WindowState=WindowState.Maximized; Toolbar.Visibility=Visibility.Collapsed; Surface.Focus(); } else { WindowState=WindowState.Normal; WindowStyle=WindowStyle.SingleBorderWindow; WindowState=previousState; Toolbar.Visibility=Visibility.Visible; } }
}

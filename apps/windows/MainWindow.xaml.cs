using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace LocalRemoteDesktop;
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
    string rawStatus="", statusSuffix="";
    string rawClipboardStatus="文本同步：等待连接", rawFileStatus="文件同步：等待连接";
    volatile bool inputAllowed = true;
    volatile bool hostReportsInput;
    long inputsQueued, inputsSent, hostReceived, hostPosted, hostBlocked, hostRejected;
    string hostInputError = "";
    readonly bool rememberConnection;
    readonly Func<string,string> loadPassword;
    readonly Action<string,string> saveConnection;
    bool connectionShouldRemember, credentialSaved, manualConnecting, passwordSaveFailed, manualCancelled;
    IInputElement? manualReturnFocus;
    readonly Func<ModifierKeys> readModifiers;
    readonly object decoderGate=new();
    IVideoDecoder? activeDecoder;
    bool windowClosing;
    SharingWindow? sharingWindow;
    readonly Func<Action<bool>,SharingWindow> createSharing;
    internal SharingWindow? SharingController=>sharingWindow;
    bool closingSharingOwner;
    string targetPlatform="macos";
    readonly TextClipboardSync clipboard;
    readonly FileClipboardSession files;
    readonly int desktopPort;
    CancellationTokenSource? discoveryScan;
    readonly Func<CancellationToken,Task<IReadOnlyList<LanDevice>>> browse;
    IReadOnlyList<LanDevice> discovered=[];
    readonly Dictionary<string,LanDevice> knownPeers=new(StringComparer.OrdinalIgnoreCase);
    bool rebuildingDevices;
    long discoveryFinished;
    string discoveryStatus="尚未搜索";
    bool pasteSuppressed;
    readonly DispatcherTimer fileTimer=new() { Interval=TimeSpan.FromMilliseconds(500) };
    readonly DispatcherTimer fileNoticeTimer=new() { Interval=TimeSpan.FromSeconds(6) };
    bool peerClipboard;
    long clipboardRequestId;
    readonly DispatcherTimer clipboardTimer=new() { Interval=TimeSpan.FromMilliseconds(500) };
    WindowState previousState;
    readonly DispatcherTimer render = new() { Interval=TimeSpan.FromMilliseconds(16) };
    public MainWindow():this(true) {}
    internal MainWindow(bool remember):this(remember,null) {}
    internal MainWindow(bool remember, Func<ModifierKeys>? modifiers, ITextClipboard? board=null, IFileClipboard? fileBoard=null, string? fileRoot=null, int desktopPort=47980, int filePort=47981, Func<CancellationToken,Task<IReadOnlyList<LanDevice>>>? discovery=null, Func<Action<bool>,SharingWindow>? sharingFactory=null, Func<string,string>? passwordLoader=null, Action<string,string>? connectionSaver=null) {
        rememberConnection=remember; UiText.Current.Configure(remember);
        loadPassword=passwordLoader??(host=>remember?SecretStore.Load(host):"");
        saveConnection=connectionSaver??((host,password)=> {if(remember) SecretStore.Remember(host,password);});
        createSharing=sharingFactory??(changed=>remember?new SharingWindow(changed):new SharingWindow(changed,()=>HostCredential.Create(HostCredential.Generate()),_=>{}));
        this.desktopPort=desktopPort;
        browse=discovery??MdnsDiscovery.Scan;
        readModifiers=modifiers ?? (()=>Keyboard.Modifiers);
        clipboard=new TextClipboardSync(board ?? (remember?new WindowsClipboard():new DisabledClipboard()));
        InitializeComponent(); var savedHost=remember?SecretStore.LastHost():""; Address.Text=savedHost.Length>0?savedHost:LocalAddress.Find(); Secret.Password=loadPassword(Address.Text); RememberPassword.IsChecked=remember;
        var workArea=SystemParameters.WorkArea;
        var availableWidth=Math.Max(1,workArea.Width-48); var availableHeight=Math.Max(1,workArea.Height-48);
        MinWidth=Math.Min(MinWidth,availableWidth); MinHeight=Math.Min(MinHeight,availableHeight);
        Width=Math.Min(Width,availableWidth); Height=Math.Min(Height,availableHeight);
        LocalDeviceName.Text=remember?DeviceName.Default.Current:Environment.MachineName;
        DiscoverButton.ContextMenu=new(); RebuildDeviceMenu();
        UiText.Current.LanguageChanged+=ApplyLanguage; ApplyLanguage();
        if(remember || discovery!=null) Loaded+=(_,_)=>RefreshDevices();
        files=new FileClipboardSession(fileBoard ?? (remember?new WindowsFileClipboard():new DisabledFileClipboard()),ClipboardMessage,
            FileNotice, action=>Dispatcher.BeginInvoke(action),fileRoot,filePort);
        fileNoticeTimer.Tick+=(_,_)=> { fileNoticeTimer.Stop(); if(!files.Busy) TransferNotice.Visibility=Visibility.Collapsed; };
        fileTimer.Tick+=(_,_)=>files.Poll();
        GlobalFiles.IsChecked=!remember || SecretStore.LoadFileSync();
        void FileModeChanged() {
            files.Configure(EffectiveFiles);
        }
        FileSync.Checked+=(_,_)=>FileModeChanged(); FileSync.Unchecked+=(_,_)=>FileModeChanged();
        void GlobalFilesChanged() {
            sharingWindow?.SetFilesAllowed(GlobalFiles.IsChecked==true);
            FileSync.IsEnabled=GlobalFiles.IsChecked==true; FileModeChanged();
            if(rememberConnection) { try { SecretStore.SaveFileSync(GlobalFiles.IsChecked==true); } catch(Exception ex) { SetFileStatus($"文件同步已切换，但无法保存选择：{ex.Message}"); } }
        }
        GlobalFiles.Checked+=(_,_)=>GlobalFilesChanged(); GlobalFiles.Unchecked+=(_,_)=>GlobalFilesChanged();
        FileSync.IsEnabled=GlobalFiles.IsChecked==true; FileModeChanged();
        clipboardTimer.Tick+=(_,_)=>PollClipboard();
        GlobalClipboard.IsChecked=!remember || SecretStore.LoadClipboardSync();
        ClipboardSync.Checked+=(_,_)=>ClipboardModeChanged(); ClipboardSync.Unchecked+=(_,_)=>ClipboardModeChanged();
        void GlobalClipboardChanged() {
            sharingWindow?.SetClipboardAllowed(GlobalClipboard.IsChecked==true);
            ClipboardSync.IsEnabled=GlobalClipboard.IsChecked==true; ClipboardModeChanged();
            if(rememberConnection) { try { SecretStore.SaveClipboardSync(GlobalClipboard.IsChecked==true); } catch(Exception ex) { SetClipboardStatus($"文本同步已切换，但无法保存选择：{ex.Message}"); } }
        }
        GlobalClipboard.Checked+=(_,_)=>GlobalClipboardChanged(); GlobalClipboard.Unchecked+=(_,_)=>GlobalClipboardChanged();
        ClipboardSync.IsEnabled=GlobalClipboard.IsChecked==true;
        ClipboardModeChanged();
        DefaultShortcuts.IsChecked=!remember || SecretStore.LoadWindowsShortcuts();
        DefaultReconnect.IsChecked=!remember || SecretStore.LoadAutoReconnect();
        DefaultQuality.SelectedIndex=remember?SecretStore.LoadConnectionQuality():0;
        HomeQuality.SelectedIndex=DefaultQuality.SelectedIndex;
        DefaultShortcuts.Checked+=(_,_)=>SaveDefaults(); DefaultShortcuts.Unchecked+=(_,_)=>SaveDefaults();
        DefaultReconnect.Checked+=(_,_)=>SaveDefaults(); DefaultReconnect.Unchecked+=(_,_)=>SaveDefaults();
        DefaultQuality.SelectionChanged+=(_,_)=> { if(session==null) HomeQuality.SelectedIndex=DefaultQuality.SelectedIndex; SaveDefaults(); };
        WindowsShortcuts.Checked+=(_,_)=>KeyboardModeChanged(); WindowsShortcuts.Unchecked+=(_,_)=>KeyboardModeChanged();
        SettingsMenu.SubmenuClosed+=(_,e)=> { if(ReferenceEquals(e.OriginalSource,SettingsMenu) && connected) Surface.Focus(); };
        Address.TextChanged+=(_,_)=> {
            if(session!=null || DeviceList.SelectedItem is LanDevice device && Address.Text==device.Address) return;
            DeviceList.SelectedItem=null; Secret.Password=loadPassword(Address.Text.Trim());
            var peer=discovered.FirstOrDefault(d=>d.Address==Address.Text.Trim());
            SelectedDeviceName.Text=peer?.Name??UiText.Text("输入目标IP"); SelectedDeviceSystem.Text=peer?.SystemLabel??"";
        };

        Quality.SelectionChanged+=(_,_)=> {
            if(session==null) return;
            requestedQuality=Quality.SelectedIndex; qualityChangePending=true;
            Input(new {op="release_all"}); activeAttempt?.Cancel();
            SetStatus("正在切换画质，将自动重新连接…");
        };
        render.Tick+=(_,_)=>Render();
        Surface.MouseMove+=(_,e)=> { if(mouseClock.ElapsedMilliseconds<8) return; mouseClock.Restart(); SendPointerMove(e.GetPosition(Surface),Surface.IsMouseCaptured); };
        Surface.MouseDown+=(_,e)=> { Keyboard.Focus(Surface); Surface.CaptureMouse(); SendPointerButton(e.GetPosition(Surface),e.ChangedButton,true); e.Handled=true; };
        Surface.MouseUp+=(_,e)=> { SendPointerButton(e.GetPosition(Surface),e.ChangedButton,false); if(e.LeftButton==MouseButtonState.Released && e.RightButton==MouseButtonState.Released && e.MiddleButton==MouseButtonState.Released) Surface.ReleaseMouseCapture(); e.Handled=true; };
        Surface.MouseWheel+=(_,e)=> { Input(new {op="scroll",delta=e.Delta}); e.Handled=true; };
        Surface.PreviewKeyDown+=(_,e)=>Key(e,true); Surface.PreviewKeyUp+=(_,e)=>Key(e,false);
        Surface.LostKeyboardFocus+=(_,_)=>Input(new {op="release_all"}); Deactivated+=(_,_)=>Input(new {op="release_all"});
        Surface.LostMouseCapture+=(_,_)=> { if(Mouse.LeftButton==MouseButtonState.Pressed || Mouse.RightButton==MouseButtonState.Pressed || Mouse.MiddleButton==MouseButtonState.Pressed) Input(new {op="release_all"}); };
        PreviewKeyDown+=(_,e)=> { if(ManualDialog.Visibility!=Visibility.Visible && e.Key==System.Windows.Input.Key.F11) { ToggleFull(); e.Handled=true; } };
        Closing+=(_,e)=> {
            if(sharingWindow!=null) { e.Cancel=true; if(!closingSharingOwner) {closingSharingOwner=true; _=CloseSharingAndOwner();} return; }
            discoveryScan?.Cancel(); UiText.Current.LanguageChanged-=ApplyLanguage;
            render.Stop(); ResetClipboard(); ResetFiles(); session?.Cancel();
            // App shutdown can stop Dispatcher continuations before Run's
            // finally executes. Kill the owned decoder directly while closing.
            lock(decoderGate) { windowClosing=true; activeDecoder?.Dispose(); activeDecoder=null; }
        };
    }
    async Task CloseSharingAndOwner() {
        var sharing=sharingWindow; if(sharing==null) return;
        await sharing.Shutdown(); sharingWindow=null;
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background); Close();
    }
    void NavigateClick(object sender,RoutedEventArgs e) {
        if(ConnectPage==null || SharingPage==null || SettingsPage==null || session!=null || sender is not FrameworkElement {Tag:string page}) return;
        ShowPage(page);
    }
    void ShowPage(string page) {
        if(page=="share" && sharingWindow==null) {
            // Reuse the tested sharing lifecycle without creating another visible window.
            var sharing=createSharing(active=>StartConnection.IsEnabled=!active && (DeviceList.SelectedItem is not LanDevice peer || peer.Online));
            sharing.NameChanged+=name=>LocalDeviceName.Text=name;
            LocalDeviceName.Text=((TextBlock)sharing.FindName("SharingName")).Text;
            var content=sharing.Content; sharing.Content=null; SharingPage.Content=content;
            sharingWindow=sharing;
            sharing.SetClipboardAllowed(GlobalClipboard.IsChecked==true);
            sharing.SetFilesAllowed(GlobalFiles.IsChecked==true);
            sharing.Closed+=(_,_)=> { if(ReferenceEquals(sharingWindow,sharing)) { sharingWindow=null; SharingPage.Content=null; } };
        }
        ConnectPage.Visibility=page=="connect"?Visibility.Visible:Visibility.Collapsed;
        SharingPage.Visibility=page=="share"?Visibility.Visible:Visibility.Collapsed;
        SettingsPage.Visibility=page=="settings"?Visibility.Visible:Visibility.Collapsed;
    }
    internal void ApplyPeerName(JsonElement message,string ip) {
        var previous=discovered.FirstOrDefault(d=>d.Address==ip);
        var name=DeviceName.Peer(message,previous?.Name??ip);
        var device=new LanDevice(name,ip,targetPlatform,desktopPort);
        if(!knownPeers.ContainsKey(ip) && knownPeers.Count>=32) knownPeers.Remove(knownPeers.Keys.First());
        knownPeers[ip]=device;
        if(previous!=device) {
            discovered=discovered.Where(d=>d.Address!=ip).Append(device).Take(32).ToArray();
            RebuildDeviceMenu();
        }
        SelectedDeviceName.Text=name; SelectedDeviceSystem.Text=device.SystemLabel;
        ConnectionTarget.Text=$"{name} · {device.SystemLabel}"; ConnectionTarget.ToolTip=ip;
    }
    void SharingClick(object sender,RoutedEventArgs e) { if(session==null) NavShare.IsChecked=true; }
    internal bool EffectiveClipboard=>GlobalClipboard.IsChecked==true && ClipboardSync.IsChecked==true;
    internal bool EffectiveFiles=>GlobalFiles.IsChecked==true && FileSync.IsChecked==true;
    internal void BeginConnectionOptions() {
        Quality.SelectedIndex=HomeQuality.SelectedIndex;
        WindowsShortcuts.IsChecked=DefaultShortcuts.IsChecked==true; Reconnect.IsChecked=DefaultReconnect.IsChecked==true;
        ClipboardSync.IsChecked=true; FileSync.IsChecked=true;
    }
    void SaveDefaults() {
        if(!rememberConnection) return;
        try {
            SecretStore.SaveConnectionQuality(DefaultQuality.SelectedIndex);
            SecretStore.SaveAutoReconnect(DefaultReconnect.IsChecked==true);
            SecretStore.SaveWindowsShortcuts(DefaultShortcuts.IsChecked==true);
        } catch { SetStatus("默认选项无法保存；本次选择仍有效。"); }
    }
    void RefreshDevicesClick(object sender,RoutedEventArgs e)=>RefreshDevices();
    void DeviceSelected(object sender,SelectionChangedEventArgs e) {
        if(rebuildingDevices || session!=null || DeviceList.SelectedItem is not LanDevice device) return;
        Address.Text=device.Address; Secret.Password=loadPassword(device.Address);
        StartConnection.IsEnabled=device.Online && sharingWindow?.Sharing!=true;
        SelectedDeviceName.Text=device.Name; SelectedDeviceSystem.Text=device.SystemLabel;
        SetStatus(Secret.Password.Length>0?$"已选择 {device.Name}，可直接连接。":$"已选择 {device.Name}，填写配对密码后连接。");
    }
    void DeviceConnectClick(object sender,RoutedEventArgs e) {
        if(session!=null || sender is not FrameworkElement {DataContext:LanDevice device}) return;
        DeviceList.SelectedItem=device;
        Address.Text=device.Address;
        ManualClick(sender,e);
        ManualSecret.Focus();
    }
    void SetSessionLayout(bool active) {
        HomeShell.Visibility=active?Visibility.Collapsed:Visibility.Visible;
        SessionPage.Visibility=active?Visibility.Visible:Visibility.Collapsed;
        if(!active) { NavConnect.IsChecked=true; ShowPage("connect"); }
    }
    void DiscoverClick(object sender,RoutedEventArgs e) {
        if(session!=null) return;
        RebuildDeviceMenu(); DiscoverButton.ContextMenu.PlacementTarget=DiscoverButton; DiscoverButton.ContextMenu.IsOpen=true;
        if(discoveryScan==null && (discoveryFinished==0 || Environment.TickCount64-discoveryFinished>10000)) RefreshDevices();
    }
    void SetStatus(string value) { if(manualConnecting && ManualDialog.Visibility==Visibility.Visible) ManualError.Text=UiText.Text(value); rawStatus=value; statusSuffix=""; Status.Text=HomeStatus.Text=UiText.Text(value); }
    void AppendStatus(string value) { statusSuffix+=value; Status.Text=HomeStatus.Text=UiText.Text(rawStatus)+UiText.Text(statusSuffix); }
    void SetClipboardStatus(string value) { rawClipboardStatus=value; ClipboardStatus.Text=UiText.Text(value); }
    void SetFileStatus(string value) { rawFileStatus=value; FileStatus.Text=UiText.Text(value); }
    void ApplyLanguage() {
        Status.Text=HomeStatus.Text=UiText.Text(rawStatus)+UiText.Text(statusSuffix);
        ClipboardStatus.Text=UiText.Text(rawClipboardStatus); FileStatus.Text=UiText.Text(rawFileStatus);
        ConnectButton.Content=UiText.Text(session==null?"连接":"断开");
        LanguageEnglish.IsChecked=UiText.Current.Language=="en"; LanguageChinese.IsChecked=!LanguageEnglish.IsChecked;
        RebuildDeviceMenu(); UpdateManualPeer();
    }
    void LanguageClick(object sender,RoutedEventArgs e) {
        if(sender is not FrameworkElement {Tag:string code}) return;
        try { UiText.Current.Use(code,rememberConnection); }
        catch(Exception error) { SetStatus($"语言已切换，但无法保存：{error.Message}"); }
    }
    void RebuildDeviceMenu() {
        var selected=DeviceList.SelectedItem as LanDevice;
        rebuildingDevices=true;
        try {
            DeviceList.ItemsSource=discovered;DeviceList.Items.Refresh();
            if(selected!=null) DeviceList.SelectedItem=discovered.FirstOrDefault(d=>d.Address==selected.Address);
            StartConnection.IsEnabled=sharingWindow?.Sharing!=true && (DeviceList.SelectedItem is not LanDevice peer || peer.Online);
        } finally {rebuildingDevices=false;}
        DeviceSearchStatus.Text=UiText.Text(discoveryStatus);
        DeviceEmptyState.Visibility=discovered.Count==0?Visibility.Visible:Visibility.Collapsed;
        DeviceEmptyTitle.Text=UiText.Text(discoveryScan!=null?"正在搜索局域网设备…":"暂无设备记录");
        var menu=DiscoverButton.ContextMenu; menu.Items.Clear();
        if(discovered.Count==0) menu.Items.Add(new MenuItem {Header=UiText.Text(discoveryStatus),IsEnabled=false});
        foreach(var device in discovered) {
            var item=new MenuItem {Header=device.ToString(),ToolTip=UiText.Text("只填写地址；连接仍需配对密码"),IsEnabled=session==null};
            item.Click+=(_,_)=> {
                if(session!=null) return;
                Address.Text=device.Address; Secret.Password=loadPassword(device.Address);
                SetStatus($"已选择 {device.Name}，填写配对密码后连接。"); Secret.Focus();
            };
            menu.Items.Add(item);
        }
        menu.Items.Add(new Separator());
        var refresh=new MenuItem {Header=UiText.Text("刷新设备"),IsEnabled=discoveryScan==null && session==null};
        refresh.Click+=(_,_)=>RefreshDevices(); menu.Items.Add(refresh);
        menu.Items.Add(new MenuItem {Header=UiText.Text("也可以直接输入目标IP"),IsEnabled=false});
    }
    async void RefreshDevices() {
        if(windowClosing || session!=null || discoveryScan!=null) return;
        using var scan=new CancellationTokenSource(); discoveryScan=scan;
        discovered=knownPeers.Values.Select(d=>d with {Online=false}).ToArray(); discoveryStatus="正在搜索局域网设备…"; RebuildDeviceMenu();
        try {
            var devices=await browse(scan.Token);
            if(windowClosing || scan.IsCancellationRequested) return;
            discovered=devices.Concat(knownPeers.Values.Select(d=>d with {Online=false})).DistinctBy(d=>d.Address).Take(32).ToArray();
            discoveryStatus=devices.Count==0?"未发现设备；请确认目标设备已开始共享。":"已发现设备";
        } catch(OperationCanceledException) { return; }
        catch { discoveryStatus="发现不可用；仍可手动填写目标IP"; }
        finally {
            if(ReferenceEquals(discoveryScan,scan)) discoveryScan=null;
            discoveryFinished=Environment.TickCount64;
            if(!windowClosing) RebuildDeviceMenu();
        }
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
        var mapped=KeyboardMapping.Map(KeyInterop.VirtualKeyFromKey(key),flags,WindowsShortcuts.IsChecked==true && targetPlatform!="windows");
        // Queue a fresh local clipboard change ahead of the Mac paste key.
        // Both messages use the same encrypted writer/input ordering.
        if(key==System.Windows.Input.Key.V && !down && pasteSuppressed) { pasteSuppressed=false; e.Handled=true; return; }
        if(down && key==System.Windows.Input.Key.V && (mapped.Flags&(targetPlatform=="windows"?2:8))!=0) {
            PollClipboard(); files.Poll();
            if(!files.PasteReady) { pasteSuppressed=true; FileNotice(files.Busy?"文件尚未就绪；看到可粘贴提示后再按 Ctrl+V":"文件未就绪，请重新复制文件后重试",files.Busy); e.Handled=true; return; }
        }
        Input(new {op="key_event",vk=mapped.VirtualKey,down,flags=mapped.Flags}); e.Handled=true;
    }
    void ClipboardMessage(object message) {
        if(!connected || inputs==null || windowClosing) return;
        if(!inputs.TryWrite(message)) { SetStatus("发送队列拥堵，已断开"); session?.Cancel(); }
    }
    void ClipboardModeChanged() {
        clipboardTimer.Stop(); clipboard.SetEnabled(false);
        SetClipboardStatus(!EffectiveClipboard?"文本同步：关闭":!connected?"文本同步：等待连接":!peerClipboard?"文本同步：目标设备版本不支持":"文本同步：等待目标设备确认");
        if(connected && peerClipboard) ClipboardMessage(new {op="clipboard_sync",enabled=EffectiveClipboard,requestId=++clipboardRequestId});
    }
    void PollClipboard() {
        if(connected && clipboard.Enabled && clipboard.Poll() is {} text)
            ClipboardMessage(new {op="clipboard_text",requestId=clipboardRequestId,encoding="utf8-base64url",text});
    }
    void ResetClipboard() {
        clipboardTimer.Stop(); clipboard.SetEnabled(false); peerClipboard=false;
        SetClipboardStatus(EffectiveClipboard?"文本同步：等待连接":"文本同步：关闭");
    }
    void ResetFiles() {
        fileTimer.Stop(); fileNoticeTimer.Stop(); files.Reset(); pasteSuppressed=false; CancelFiles.IsEnabled=false; TransferNotice.Visibility=Visibility.Collapsed;
        SetFileStatus(EffectiveFiles?"文件同步：等待连接":"文件同步：关闭");
    }
    void FileNotice(string text,bool busy) {
        SetFileStatus(text); CancelFiles.IsEnabled=busy; CancelFiles.Visibility=busy?Visibility.Visible:Visibility.Collapsed; DismissFileNotice.IsEnabled=!busy;
        fileNoticeTimer.Stop();
        bool normal=text.StartsWith("文件同步：");
        TransferNotice.Visibility=normal?Visibility.Collapsed:Visibility.Visible;
        if(!busy && !normal && !text.Contains("失败") && !text.Contains("超时") && !text.Contains("未就绪")) fileNoticeTimer.Start();
    }
    void DismissFilesClick(object sender,RoutedEventArgs e) { if(!files.Busy) { fileNoticeTimer.Stop(); TransferNotice.Visibility=Visibility.Collapsed; } }
    void CancelFilesClick(object sender,RoutedEventArgs e) { files.Cancel(); FileNotice("文件传输已取消；重新复制可重试",false); }
    void OpenFilesClick(object sender,RoutedEventArgs e) {
        try { Directory.CreateDirectory(files.ReceivedDirectory); Process.Start(new ProcessStartInfo(files.ReceivedDirectory) { UseShellExecute=true }); }
        catch(Exception ex) { FileNotice($"无法打开接收目录：{ex.Message}",false); }
    }
    void ClipboardControl(JsonElement root) {
        string? op=root.GetProperty("op").GetString();
        if(op=="capabilities") {
            peerClipboard=root.TryGetProperty("clipboardText",out var supported) && supported.ValueKind==JsonValueKind.True;
            ClipboardModeChanged(); return;
        }
        if(!peerClipboard || !root.TryGetProperty("requestId",out var request) || request.ValueKind!=JsonValueKind.Number || !request.TryGetInt64(out var id) || id!=clipboardRequestId) return;
        if(op=="clipboard_sync" && root.TryGetProperty("enabled",out var permission) && permission.ValueKind is JsonValueKind.True or JsonValueKind.False) {
            bool enabled=permission.GetBoolean() && EffectiveClipboard && connected;
            clipboard.SetEnabled(enabled);
            if(enabled) clipboardTimer.Start(); else clipboardTimer.Stop();
            SetClipboardStatus(enabled?"文本同步：已启用（双向，最多 32 KiB）":EffectiveClipboard?"文本同步：等待目标设备允许同步":"文本同步：关闭");
        } else if(op=="clipboard_text" && root.TryGetProperty("encoding",out var encoding) && encoding.GetString()=="utf8-base64url"
                  && root.TryGetProperty("text",out var text) && text.ValueKind==JsonValueKind.String) {
            _=clipboard.Receive(text.GetString()!);
        }
    }
    void KeyboardModeChanged() {
        Input(new {op="release_all"});
    }
    void Input(object message) {
        if(!connected || inputs==null) return;
        if(bitmap==null && System.Text.Json.JsonSerializer.SerializeToElement(message).GetProperty("op").GetString()!="release_all") return;
        if(inputs.TryWrite(message)) Interlocked.Increment(ref inputsQueued);
        else { SetStatus("输入队列拥堵，已断开以避免按键卡住"); session?.Cancel(); }
    }
    void ManualClick(object sender,RoutedEventArgs e) {
        if(session!=null) return;
        manualCancelled=false;
        manualReturnFocus=Keyboard.FocusedElement;
        ManualAddress.Text=Address.Text; ManualSecret.Password=loadPassword(ManualAddress.Text.Trim());
        ManualRemember.IsChecked=RememberPassword.IsChecked; ManualError.Text="";
        ManualStopSharing.Visibility=sharingWindow?.Sharing==true?Visibility.Visible:Visibility.Collapsed;
        ManualStopSharing.IsChecked=false;
        UpdateManualPeer(); SetManualVisible(true); ManualAddress.Focus();
    }
    void SetManualVisible(bool visible) {
        ManualDialog.Visibility=visible?Visibility.Visible:Visibility.Collapsed;
        HomeShell.IsEnabled=SessionPage.IsEnabled=!visible;
        if(!visible) { ManualSecret.Password=""; if(manualReturnFocus is {} focus) Keyboard.Focus(focus); }
    }
    void UpdateManualPeer() {
        if(ManualDeviceName==null || ManualDeviceSystem==null) return;
        var peer=discovered.FirstOrDefault(d=>d.Address==ManualAddress.Text.Trim());
        ManualDeviceName.Text=peer?.Name??"";
        ManualDeviceName.Visibility=peer==null?Visibility.Collapsed:Visibility.Visible;
        ManualDeviceSystem.Text=peer?.SystemLabel??UiText.Text("连接后获取设备信息。");
    }
    void ManualAddressChanged(object sender,TextChangedEventArgs e) {
        if(ManualSecret==null) return;
        ManualSecret.Password=loadPassword(ManualAddress.Text.Trim());
        ManualError.Text=""; UpdateManualPeer();
    }
    void ManualCancelClick(object sender,RoutedEventArgs e) {
        if(manualConnecting) { manualCancelled=true; session?.Cancel(); return; }
        SetManualVisible(false);
    }
    void ManualKeyDown(object sender,KeyEventArgs e) {
        if(e.Key==System.Windows.Input.Key.Escape) {ManualCancelClick(sender,new RoutedEventArgs()); e.Handled=true;}
        else if(e.Key==System.Windows.Input.Key.Enter && !manualConnecting) {ManualConnectClick(sender,new RoutedEventArgs());e.Handled=true;}
    }
    static void ValidateTarget(string ip,string secret) {
        if(!System.Net.IPAddress.TryParse(ip,out var address) || address.AddressFamily!=System.Net.Sockets.AddressFamily.InterNetwork ||
           ip!=address.ToString() || !WindowsHost.IsLan(address)) throw new ArgumentException("请填写局域网 IPv4 地址。");
        PairingPassword.Validate(secret);
    }
    async void ManualConnectClick(object sender,RoutedEventArgs e) {
        if(manualConnecting || session!=null) return;
        var ip=ManualAddress.Text.Trim(); var password=ManualSecret.Password;
        try { ValidateTarget(ip,password); }
        catch(Exception error) { ManualError.Text=UiText.Text(error.Message); return; }
        if(sharingWindow?.Sharing==true && ManualStopSharing.IsChecked!=true) {
            ManualError.Text=UiText.Text("这将停止本机共享并断开当前控制者。"); return;
        }
        manualConnecting=true; HomeQuality.IsEnabled=ForgetButton.IsEnabled=false; ManualAddress.IsEnabled=ManualSecret.IsEnabled=ManualRemember.IsEnabled=ManualConnect.IsEnabled=ManualStopSharing.IsEnabled=false;
        try {
            if(sharingWindow?.Sharing==true) await sharingWindow.Stop();
            if(manualCancelled || windowClosing) return;
            Address.Text=ip; Secret.Password=password; RememberPassword.IsChecked=ManualRemember.IsChecked;
            await StartConnectionAsync();
        } catch(Exception error) {ManualError.Text=UiText.Text($"无法连接：{error.Message}");}
        finally {
            manualConnecting=false; HomeQuality.IsEnabled=ForgetButton.IsEnabled=true; ManualAddress.IsEnabled=ManualSecret.IsEnabled=ManualRemember.IsEnabled=ManualConnect.IsEnabled=ManualStopSharing.IsEnabled=true;
            if(manualCancelled) SetManualVisible(false);
        }
    }
    internal void PairingSucceeded(string ip,string password) {
        if(connectionShouldRemember && !credentialSaved) {
            credentialSaved=true;
            try {saveConnection(ip,password);}
            catch {passwordSaveFailed=true;}
        }
        if(manualConnecting) SetManualVisible(false);
    }
    async void ConnectClick(object sender,RoutedEventArgs e) => await StartConnectionAsync();
    async Task StartConnectionAsync() {
        if(windowClosing) return;
        if(!manualConnecting && DeviceList.SelectedItem is LanDevice {Online:false}) {SetStatus("本次未发现此设备；可刷新设备或通过目标IP重试。");return;}
        if(sharingWindow?.Sharing==true) return;
        if(session!=null) { session.Cancel(); return; }
        var ip=Address.Text.Trim(); var secret=Secret.Password; int quality=HomeQuality.SelectedIndex;
        try {
            ValidateTarget(ip,secret);
        } catch(Exception ex) { SetStatus($"无法连接：{ex.Message}"); return; }
        connectionShouldRemember=RememberPassword.IsChecked==true; credentialSaved=false; passwordSaveFailed=false;
        BeginConnectionOptions(); requestedQuality=quality; qualityChangePending=false;
        discoveryScan?.Cancel();
        targetPlatform="macos"; NavShare.IsEnabled=false; sharingWindow?.SetControlActive(true);
        session=new(); SetSessionLayout(true); render.Start(); ConnectButton.Content=UiText.Text("断开"); DiscoverButton.IsEnabled=Address.IsEnabled=Secret.IsEnabled=ForgetButton.IsEnabled=false;
        ConnectionFields.Visibility=Visibility.Collapsed; ConnectionTarget.Text=ip; ConnectionTarget.Visibility=Visibility.Visible;
        runner=Run(ip,secret,session.Token);
        try { await runner; } finally { render.Stop(); ResetClipboard(); ResetFiles(); connected=false; if(full) ToggleFull(); session.Dispose(); session=null; SetSessionLayout(false); NavShare.IsEnabled=true; sharingWindow?.SetControlActive(false); ConnectButton.Content=UiText.Text("连接"); DiscoverButton.IsEnabled=Address.IsEnabled=Secret.IsEnabled=Quality.IsEnabled=ForgetButton.IsEnabled=true; ConnectionFields.Visibility=Visibility.Visible; ConnectionTarget.Visibility=Visibility.Collapsed; Desktop.Source=null; bitmap=null; width=height=0; }
    }
    async Task Run(string ip,string secret,CancellationToken ct) {
        bool paired=false;
        do {
            using var wire=new Wire(); IVideoDecoder? decoder=null;
            using var attempt=CancellationTokenSource.CreateLinkedTokenSource(ct);
            activeAttempt=attempt; qualityChangePending=false;
            int quality=requestedQuality;
            Task? heartbeat=null, controls=null; bool retryBlocked=false;
            try {
                SetStatus("正在连接…");
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct)) { timeout.CancelAfter(TimeSpan.FromSeconds(8)); await wire.Connect(ip,secret,timeout.Token,desktopPort); }
                ct.ThrowIfCancellationRequested();
                paired=true; PairingSucceeded(ip,secret);
                inputs=new InputQueue(); connected=true;
                files.Connect(ip,secret); files.Configure(EffectiveFiles); fileTimer.Start();
                Interlocked.Exchange(ref inputsQueued,0); Interlocked.Exchange(ref inputsSent,0);
                Interlocked.Exchange(ref hostReceived,0); Interlocked.Exchange(ref hostPosted,0);
                Interlocked.Exchange(ref hostBlocked,0); Interlocked.Exchange(ref hostRejected,0); hostInputError="";
                hostReportsInput=false;
                await wire.Json(new {op="handshake",version=1,quality},attempt.Token);
                heartbeat=Task.Run(async()=> { try { while(true) { await Task.Delay(3000,attempt.Token); await wire.Json(new {op="heartbeat"},attempt.Token); } } catch { attempt.Cancel(); } });
                controls=Task.Run(async()=> { try { await foreach(var m in inputs.ReadAllAsync(attempt.Token)) { await wire.Json(m,attempt.Token); Interlocked.Increment(ref inputsSent); } } catch { attempt.Cancel(); } });
                Surface.Focus(); SetStatus(passwordSaveFailed?"已连接，但无法记住密码。":"已配对，等待画面…");
                inputAllowed=true;
                // Receive, authenticate/decrypt and feed FFmpeg away from the UI
                // thread; only creation of the displayed bitmap uses Dispatcher.
                await Task.Run(async()=> {
                while(true) {
                    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(attempt.Token); timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    var packet=await wire.Read(timeout.Token);
                    if(packet.type==1) {
                        using var json=JsonDocument.Parse(packet.data); var root=json.RootElement;
                        if(root.TryGetProperty("platform",out var platform)) {
                            var name=platform.GetString(); if(name is not ("windows" or "macos")) throw new NotSupportedException("Unsupported target platform");
                            if(!root.TryGetProperty("inputModel",out var model) || model.GetString()!=(name=="windows"?"win-vk-v1":"win-vk-mac-v1")) throw new NotSupportedException("Unsupported target input model");
                            await Dispatcher.InvokeAsync(()=> { targetPlatform=name; });
                        }
                        if(root.GetProperty("op").GetString() is "capabilities" or "heartbeat") await Dispatcher.InvokeAsync(()=>ApplyPeerName(root,ip));
                        if(root.GetProperty("op").GetString() is "capabilities" or "clipboard_sync" or "clipboard_text" or "file_sync" or "file_offer")
                            await Dispatcher.InvokeAsync(()=> { ClipboardControl(root); files.Control(root); });
                        if(root.GetProperty("op").GetString()=="video_header") {
                            int videoWidth=root.GetProperty("width").GetInt32(), videoHeight=root.GetProperty("height").GetInt32();
                            if(videoWidth<2 || videoHeight<2 || videoWidth>3840 || videoHeight>2160) throw new IOException("Invalid video dimensions");
                            decoder?.Dispose(); Interlocked.Exchange(ref latest,null);
                            var nextDecoder=new FfmpegDecoder(videoWidth,videoHeight);
                            lock(decoderGate) {
                                if(windowClosing) { nextDecoder.Dispose(); throw new OperationCanceledException(attempt.Token); }
                                decoder=nextDecoder; activeDecoder=decoder;
                            }
                            decoder.Frame+=p=> { Interlocked.Exchange(ref latest,p); Interlocked.Increment(ref frames); };
                            _=decoder.Completion.ContinueWith(_=> { try { attempt.Cancel(); } catch(ObjectDisposedException) {} },TaskScheduler.Default);
                            await Dispatcher.InvokeAsync(()=> {
                                width=videoWidth; height=videoHeight;
                                bitmap=new WriteableBitmap(width,height,96,96,PixelFormats.Bgra32,null); Desktop.Source=bitmap;
                                stats.Restart(); Interlocked.Exchange(ref frames,0); Interlocked.Exchange(ref bytes,0);
                            });
                        } else if(root.GetProperty("op").GetString()=="metrics") Interlocked.Exchange(ref encodeMs,root.GetProperty("encodeMs").GetDouble());
                        if(root.TryGetProperty("accessibility",out var permission)) inputAllowed=permission.GetBoolean();
                        if(targetPlatform=="windows" && root.TryGetProperty("inputEnabled",out var enabled)) inputAllowed=enabled.GetBoolean();
                        if(root.TryGetProperty("inputReceived",out var received)) { Interlocked.Exchange(ref hostReceived,received.GetInt64()); hostReportsInput=true; }
                        if(root.TryGetProperty("inputPosted",out var posted)) Interlocked.Exchange(ref hostPosted,posted.GetInt64());
                        if(root.TryGetProperty("inputBlocked",out var blocked)) Interlocked.Exchange(ref hostBlocked,blocked.GetInt64());
                        if(root.TryGetProperty("inputRejected",out var rejected)) Interlocked.Exchange(ref hostRejected,rejected.GetInt64());
                        if(root.TryGetProperty("inputError",out var error)) hostInputError=error.GetString()??"";
                    } else if(packet.type==2 && decoder!=null) { Interlocked.Add(ref bytes,packet.data.Length); await decoder.Feed(packet.data,attempt.Token); }
                }
                },attempt.Token);
            } catch(Exception ex) {
                // Retrying credentials or incompatible capabilities cannot heal
                // the connection. Return to Connect with the draft intact.
                retryBlocked=ex is CryptographicException or ArgumentException or NotSupportedException;
                SetStatus(ct.IsCancellationRequested?"已断开":ex is CryptographicException?
                    "目标设备身份验证未完成，请检查配对密码后重新连接。":qualityChangePending && !retryBlocked?"正在切换画质":$"连接中断：{ex.Message}");
            }
            finally {
                connected=false; ResetClipboard(); ResetFiles(); inputs?.Complete(); attempt.Cancel(); wire.Dispose();
                lock(decoderGate) { decoder?.Dispose(); if(ReferenceEquals(activeDecoder,decoder)) activeDecoder=null; }
                Interlocked.Exchange(ref latest,null);
                if(heartbeat!=null) await heartbeat; if(controls!=null) await controls;
                activeAttempt=null;
                if(Surface.IsMouseCaptured) Surface.ReleaseMouseCapture();
                Desktop.Source=null; bitmap=null; width=height=0;
            }
            if(ct.IsCancellationRequested || retryBlocked || (!qualityChangePending && (!paired || Reconnect.IsChecked!=true))) break;
            AppendStatus("；3 秒后重连");
            try { await Task.Delay(3000,ct); } catch(OperationCanceledException) { break; }
        } while(!ct.IsCancellationRequested);
    }
    void Render() {
        var pixels=Interlocked.Exchange(ref latest,null);
        if(pixels!=null && bitmap!=null && pixels.Length==width*height*4) bitmap.WritePixels(new Int32Rect(0,0,width,height),pixels,width*4,0);
        if(connected && bitmap!=null && stats.ElapsedMilliseconds>=1000) {
            string inputStatus=!inputAllowed?(targetPlatform=="windows"?"目标设备未允许输入：只能查看":"Mac 辅助功能未授权：只能查看"):Surface.IsKeyboardFocusWithin?"键盘已启用；Ctrl+Alt+Esc 释放":"点击画面启用键盘";
            inputStatus=UiText.Text(inputStatus);
            string feedback=hostReportsInput?$"目标设备收到 {Interlocked.Read(ref hostReceived)} / 已提交 {Interlocked.Read(ref hostPosted)}":"目标设备未回报输入诊断";
            feedback=UiText.Text(feedback);
            double sec=stats.Elapsed.TotalSeconds, fps=Interlocked.Exchange(ref frames,0)/sec, mbps=Interlocked.Exchange(ref bytes,0)*8/sec/1e6;
            string encoding=targetPlatform=="windows"?UiText.Text("Windows 编码耗时未测量"):$"{UiText.Text("Mac 编码")} {Volatile.Read(ref encodeMs):F1} ms";
            SetStatus(ShowPerformance.IsChecked?$"{width}×{height} | {UiText.Text("解码")} {fps:F1} fps | {UiText.Text("接收")} {mbps:F2} Mbps | {encoding} | {inputStatus} | {UiText.Text("输入 已发")} {Interlocked.Read(ref inputsSent)} / {feedback}":$"{UiText.Text("已连接")} · {inputStatus}"); stats.Restart();
        }
    }
    void FullscreenClick(object sender,RoutedEventArgs e)=>ToggleFull();
    void DiagnosticsClick(object sender,RoutedEventArgs e) {
        Clipboard.SetText($"Local Remote Desktop input diagnostics\nConnected: {connected}\nKeyboardFocus: {Surface.IsKeyboardFocusWithin}\nWindowsShortcuts: {WindowsShortcuts.IsChecked==true}\nTargetPlatform: {targetPlatform}\nInputAllowed: {inputAllowed}\nHostReportsInput: {hostReportsInput}\nQueued: {Interlocked.Read(ref inputsQueued)}\nSent: {Interlocked.Read(ref inputsSent)}\nHostReceived: {Interlocked.Read(ref hostReceived)}\nHostPosted: {Interlocked.Read(ref hostPosted)}\nHostBlocked: {Interlocked.Read(ref hostBlocked)}\nHostRejected: {Interlocked.Read(ref hostRejected)}\nHostError: {hostInputError}\nPosted means submitted to native OS input, not confirmed application acceptance.");
        SetStatus("诊断已复制（不包含密钥、IP 或输入内容）");
    }
    void ManualForgetClick(object sender,RoutedEventArgs e) {
        try {
            var ip=ManualAddress.Text.Trim();
            if(rememberConnection) SecretStore.Forget(ip);
            ManualSecret.Password="";
            if(Address.Text.Trim()==ip) Secret.Password="";
            ManualError.Text=UiText.Text("已忘记此设备的密码，目标地址保留。");
        } catch(Exception ex) {ManualError.Text=UiText.Text($"清除失败：{ex.Message}");}
    }
    void ForgetClick(object sender,RoutedEventArgs e) {
        try { if(rememberConnection) SecretStore.Forget(Address.Text.Trim()); Secret.Password=""; SetStatus("已忘记此设备的密码，目标地址保留。"); }
        catch(Exception ex) { SetStatus($"清除失败：{ex.Message}"); }
    }
    void ToggleFull() { if(session==null) return; full=!full; if(full) { previousState=WindowState; WindowState=WindowState.Normal; WindowStyle=WindowStyle.None; WindowState=WindowState.Maximized; Toolbar.Visibility=Visibility.Collapsed; Surface.Focus(); } else { WindowState=WindowState.Normal; WindowStyle=WindowStyle.SingleBorderWindow; WindowState=previousState; Toolbar.Visibility=Visibility.Visible; } }
}

using System.Security.Cryptography;
using System.Windows;
namespace LocalRemoteDesktop;

public partial class SharingWindow:Window {
    HostCredential? credential;
    WindowsHost? host;
    readonly CancellationTokenSource lifetime=new();
    readonly Action<bool> sharingChanged; readonly DeviceName deviceName;
    readonly Func<HostCredential> load;
    readonly Action<HostCredential> save;
    readonly Func<HostCredential,byte[],bool,Action<string>,WindowsHost> create;
    readonly IDeviceAdvertisement advertisement;
    bool clipboardAllowed=true;
    bool filesAllowed=true; IHostFiles? files;
    internal void SetClipboardAllowed(bool allowed) { clipboardAllowed=allowed; }
    internal void SetFilesAllowed(bool allowed) { filesAllowed=allowed; }
    Task? starting,shutdown,stopping; bool shutting,closed,revealed,saving,controlActive; string status="尚未共享";
    public SharingWindow():this(_=>{}) {}
    internal SharingWindow(Action<bool> changed,Func<HostCredential>? load=null,Action<HostCredential>? save=null,Func<HostCredential,byte[],bool,Action<string>,WindowsHost>? create=null,DeviceName? deviceName=null,IDeviceAdvertisement? advertisement=null) {
        sharingChanged=changed; this.deviceName=deviceName??DeviceName.Default; this.load=load??(()=>HostCredential.Load()); this.save=save??(c=>c.Save());
        this.create=create??((c,key,software,report)=> { var area=CaptureArea.Primary(); return new WindowsHost(key,c.Salt,area,()=>new FfmpegDesktopCapture(area,software),()=>new WindowsInput(area,CaptureArea.Virtual(),new SendInputAdapter()),report,deviceName:()=>this.deviceName.Current,clipboard:(wire,token)=>new WindowsHostClipboard(wire,new WindowsClipboard(),Dispatcher,()=>clipboardAllowed,token),files:(wire,k,s,token)=>files=new WindowsHostFiles(wire,k,s,new WindowsFileClipboard(),async action=> {await Dispatcher.InvokeAsync(action);},()=>filesAllowed,token,FileReport)); });
        this.advertisement=advertisement??(create!=null?new NoDeviceAdvertisement():new DeviceAdvertisement(available=>Dispatcher.BeginInvoke(()=> {if(!shutting && host!=null) DiscoveryNotice.Visibility=available?Visibility.Collapsed:Visibility.Visible;})));
        InitializeComponent(); SharingAddress.Text=LocalAddress.Find(); SharingName.Text=this.deviceName.Current;
        try { credential=this.load(); } catch { status="无法读取共享密码，请检查本机安全存储。"; SharingStart.IsEnabled=false; }
        UiText.Current.LanguageChanged+=Translate; Translate();
        Closing+=(_,e)=> { if(!closed) { e.Cancel=true; _=Shutdown(); } };
    }
    internal bool Sharing=>host!=null || starting!=null;
    internal event Action<string>? NameChanged;
    internal void SetControlActive(bool active) { controlActive=active; SharingStart.IsEnabled=!active && !shutting && !saving && starting==null && credential!=null; }
    void Translate() { SharingStatus.Text=UiText.Text(status); SharingStart.Content=UiText.Text(host==null?"开始共享":"停止共享"); Reveal.Content=UiText.Text(revealed?"隐藏":"查看"); }
    void Report(WindowsHost owner,string value) {
        Dispatcher.BeginInvoke(()=> {
            if(!ReferenceEquals(host,owner) || shutting) return;
            if(value=="Files unavailable") {FileReport("文件通道不可用，请检查 47981 端口；桌面共享可继续",false);return;}
            if(value is "Available" or "Off") {files=null;SharingTransfer.Visibility=Visibility.Collapsed;}
            status=value switch {"Available"=>owner.LastError.StartsWith("capture_")?"画面采集失败，请检查 FFmpeg，或停止共享后在故障排查中启用软件编码。":"等待连接 · TCP 47980","In use"=>"正在共享当前桌面 · 可控制","Session ended"=>"连接已结束","Off"=>"已停止",_=>"共享状态异常，请停止后重试。"}; Translate();
        });
    }
    void FileReport(string text,bool busy) { Dispatcher.BeginInvoke(()=> {if(shutting || host==null)return;SharingTransfer.Visibility=text.StartsWith("文件同步：")?Visibility.Collapsed:Visibility.Visible;SharingFileStatus.Text=UiText.Text(text);CancelSharingFiles.Visibility=busy?Visibility.Visible:Visibility.Collapsed;DismissSharingFiles.IsEnabled=!busy;}); }
    void CancelSharingFilesClick(object sender,RoutedEventArgs e) {files?.Cancel();}
    void DismissSharingFilesClick(object sender,RoutedEventArgs e) {SharingTransfer.Visibility=Visibility.Collapsed;}
    void OpenSharingFilesClick(object sender,RoutedEventArgs e) {
        try {var path=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalRemote","ReceivedFiles");System.IO.Directory.CreateDirectory(path);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});}
        catch {FileReport("无法打开接收目录，请稍后重试。",false);}
    }
    async void StartClick(object sender,RoutedEventArgs e) {
        if(SharingStart.IsEnabled!=true || credential==null || shutting || saving || controlActive) return;
        SharingStart.IsEnabled=false;
        try {
            if(host!=null) await Stop();
            else { starting=Start(credential); await starting; }
        } catch { status="无法开始共享，请检查 FFmpeg 和 TCP 47980 端口。"; }
        finally { starting=null; SharingStart.IsEnabled=!shutting && !controlActive && credential!=null; Translate(); }
    }
    async Task Start(HostCredential value) {
        status="正在启动共享…"; Translate(); sharingChanged(true);
        byte[]? key=null;
        try {
            key=await Task.Run(()=>PairingPassword.Derive(value.Password,value.Salt),lifetime.Token);
            if(stopping!=null) await stopping;
            stopping=null;
            if(shutting || lifetime.IsCancellationRequested) return;
            WindowsHost? next=null; next=create(value,key,Software.IsChecked==true,s=> {if(next!=null) Report(next,s);});
            host=next;
            try { next.Start(); Software.IsEnabled=false; advertisement.Publish(deviceName.Current,next.Port); }
            catch { host=null; await next.DisposeAsync(); throw; }
        } finally { if(key!=null) CryptographicOperations.ZeroMemory(key); if(host==null) sharingChanged(false); }
    }
    internal Task Stop() {
        var old=host; host=null;
        if(old==null) return stopping??Withdraw();
        return stopping=StopCore(old);
    }
    async Task StopCore(WindowsHost old) {
        var withdrawal=Withdraw();
        await old.DisposeAsync();
        await withdrawal;DiscoveryNotice.Visibility=Visibility.Collapsed;
        Software.IsEnabled=true; sharingChanged(false); status="已停止"; Translate();
    }
    async Task Withdraw() {
        // A stuck OS DNS completion must not block Stop/Exit. Its native request
        // stays rooted until callback; process exit removes OS registrations.
        try {await advertisement.Stop().WaitAsync(TimeSpan.FromSeconds(5));} catch { }
    }
    internal Task Shutdown()=>shutdown??=ShutdownCore();
    async Task ShutdownCore() {
        shutting=true; lifetime.Cancel(); SharingStart.IsEnabled=false;
        if(starting!=null) {try { await starting; } catch {} }
        await Stop(); UiText.Current.LanguageChanged-=Translate; lifetime.Dispose(); closed=true;
        // Even an idle stop can complete synchronously inside Closing. Let that
        // canceled event finish before asking WPF to close the window again.
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background); Close();
    }
    void NameEditClick(object sender,RoutedEventArgs e) { NameDraft.Text=deviceName.Current; NameError.Text=""; NameEditor.Visibility=Visibility.Visible; NameDraft.Focus(); }
    void NameRestoreClick(object sender,RoutedEventArgs e) { NameDraft.Text=deviceName.SystemName; NameError.Text=""; }
    void NameCancelClick(object sender,RoutedEventArgs e) { NameDraft.Text=""; NameError.Text=""; NameEditor.Visibility=Visibility.Collapsed; }
    void NameSaveClick(object sender,RoutedEventArgs e) {
        try { deviceName.Save(NameDraft.Text); SharingName.Text=deviceName.Current; if(host!=null) advertisement.Publish(deviceName.Current,host.Port); NameChanged?.Invoke(deviceName.Current); NameCancelClick(sender,e); }
        catch(Exception error) { NameError.Text=UiText.Text(error is ArgumentException?error.Message:"无法保存名称，原名称仍有效。"); }
    }
    void RevealClick(object sender,RoutedEventArgs e) { revealed=!revealed; CurrentPassword.Text=revealed?credential?.Password??"":"••••••••"; Translate(); }
    void CopyClick(object sender,RoutedEventArgs e) { if(credential!=null) { try { Clipboard.SetText(credential.Password); } catch { status="剪贴板忙，请重试。"; Translate(); } } }
    void EditClick(object sender,RoutedEventArgs e) { if(starting!=null || shutting || credential==null) return; Draft.Text=""; PasswordError.Text=""; Editor.Visibility=Visibility.Visible; Draft.Focus(); }
    void GenerateClick(object sender,RoutedEventArgs e) { Draft.Text=HostCredential.Generate(); PasswordError.Text=""; }
    void CancelEditClick(object sender,RoutedEventArgs e) { Draft.Text=""; PasswordError.Text=""; Editor.Visibility=Visibility.Collapsed; }
    async void SaveClick(object sender,RoutedEventArgs e) {
        if(starting!=null || shutting || credential==null || saving) return;
        saving=true; SharingStart.IsEnabled=false; bool persisted=false;
        try {
            var next=HostCredential.CreateNew(Draft.Text);
            if(next.Password==credential.Password) { PasswordError.Text=UiText.Text("与当前密码相同，无需保存。"); return; }
            // Persist atomically before disrupting the old authorized session.
            save(next); persisted=true; credential=next; revealed=false; CurrentPassword.Text="••••••••";
            await Stop(); Draft.Text=""; Editor.Visibility=Visibility.Collapsed;
            status="密码已保存，原连接已断开；点击开始共享后使用新密码连接。"; Translate();
        } catch(Exception error) { PasswordError.Text=UiText.Text(persisted?"密码已保存，但停止共享失败；请退出应用后重开。":error is ArgumentException?error.Message:"无法保存密码，原密码和共享状态保留。"); }
        finally { saving=false; SharingStart.IsEnabled=!shutting && !controlActive && credential!=null; }
    }
}

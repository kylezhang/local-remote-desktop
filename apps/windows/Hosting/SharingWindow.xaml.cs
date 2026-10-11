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
    Task? starting,shutdown,stopping; bool shutting,closed,revealed,saving,controlActive; string status="尚未共享";
    public SharingWindow():this(_=>{}) {}
    internal SharingWindow(Action<bool> changed,Func<HostCredential>? load=null,Action<HostCredential>? save=null,Func<HostCredential,byte[],bool,Action<string>,WindowsHost>? create=null,DeviceName? deviceName=null) {
        sharingChanged=changed; this.deviceName=deviceName??DeviceName.Default; this.load=load??(()=>HostCredential.Load()); this.save=save??(c=>c.Save());
        this.create=create??((c,key,software,report)=> { var area=CaptureArea.Primary(); return new WindowsHost(key,c.Salt,area,()=>new FfmpegDesktopCapture(area,software),()=>new WindowsInput(area,CaptureArea.Virtual(),new SendInputAdapter()),report,deviceName:()=>this.deviceName.Current); });
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
            status=value switch {"Available"=>owner.LastError.StartsWith("capture_")?"画面采集失败，请检查 FFmpeg 或选择兼容编码后重试。":"等待连接 · TCP 47980","In use"=>"正在共享当前桌面 · 可控制","Session ended"=>"连接已结束","Off"=>"已停止",_=>"共享状态异常，请停止后重试。"}; Translate();
        });
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
            try { next.Start(); Software.IsEnabled=false; }
            catch { host=null; await next.DisposeAsync(); throw; }
        } finally { if(key!=null) CryptographicOperations.ZeroMemory(key); if(host==null) sharingChanged(false); }
    }
    internal Task Stop() {
        var old=host; host=null;
        if(old==null) return stopping??Task.CompletedTask;
        return stopping=StopCore(old);
    }
    async Task StopCore(WindowsHost old) {
        await old.DisposeAsync();
        Software.IsEnabled=true; sharingChanged(false); status="已停止"; Translate();
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
        try { deviceName.Save(NameDraft.Text); SharingName.Text=deviceName.Current; NameChanged?.Invoke(deviceName.Current); NameCancelClick(sender,e); }
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

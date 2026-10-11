using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LocalRemoteDesktop;

static partial class Program {
    // Never Show/Activate a Window, capture a physical screen, access user
    // credentials/clipboard or inject OS input. Rendering stays in memory.
    static async Task CheckShell() {
        await CheckReverseClipboard();
        int loads=0, saved=0;var advertisement=new ShellAdvertisement();
        var nameRoot=Path.Combine(Path.GetTempPath(),"lrd-shell-name-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(nameRoot);
        var nameStore=new DeviceName(Path.Combine(nameRoot,"name.txt"),"Fixture PC");
        var credential=HostCredential.Create("Fixture-LAN#42");
        var area=new CaptureArea(0,0,320,180);
        var window=new MainWindow(false,null,discovery:_=>Task.FromResult<IReadOnlyList<LanDevice>>(
            [new("Fixture Mac","192.168.1.24","macos",47980)]),sharingFactory:changed=>new SharingWindow(changed,
                ()=> { loads++; return credential; },c=> { saved++; credential=c; },
                (c,key,software,report)=>new WindowsHost(key,c.Salt,area,()=>new SharingCapture(),
                    ()=>new WindowsInput(area,area,new SharingInput()),report,IPAddress.Loopback,0,deviceName:()=>nameStore.Current),deviceName:nameStore,advertisement:advertisement));
        ((TextBlock)window.FindName("LocalDeviceName")).Text="Fixture PC";
        T Find<T>(string name)=>(T)window.FindName(name);
        void Nav(string name)=>Find<RadioButton>(name).IsChecked=true;
        void Invoke(string name,params object[] args)=>typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,args);
        bool Visible(string name)=>Find<FrameworkElement>(name).Visibility==Visibility.Visible;
        void Snapshot(string name,int width,int dpi=96) {
            var content=(FrameworkElement)window.Content;
            content.Measure(new Size(width,640)); content.Arrange(new Rect(0,0,width,640)); content.UpdateLayout();
            Assert(!window.IsVisible && !window.IsActive,"Background check opened/activated a window");
            var image=new RenderTargetBitmap((int)Math.Ceiling(width*dpi/96d),(int)Math.Ceiling(640*dpi/96d),dpi,dpi,PixelFormats.Pbgra32); image.Render(content);
            var root=Environment.GetEnvironmentVariable("LOCALREMOTE_UI_SNAPSHOTS");
            if(root!=null) {
                Directory.CreateDirectory(root); var png=new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(image));
                using var file=File.Create(Path.Combine(root,name+".png")); png.Save(file);
            }
        }
        try {
            foreach(var lang in new[]{"en","zh-Hans"}) {
                UiText.Current.Use(lang,false); Nav("NavConnect");
                Assert(Visible("HomeShell") && Visible("ConnectPage") && !Visible("SessionPage"),"Initial role layout incorrect");
                Find<Button>("StartConnection").Content=UiText.Text("连接");
                Assert(Find<PasswordBox>("Secret").Password=="","Fixture accessed saved credentials");
                Invoke("RefreshDevicesClick",window,new RoutedEventArgs());
                Find<ListBox>("DeviceList").SelectedIndex=0;
                Assert(Find<TextBox>("Address").Text=="192.168.1.24" && Find<TextBlock>("SelectedDeviceName").Text=="Fixture Mac","Discovery selection lost target");
                Find<PasswordBox>("Secret").Password="Fixture-LAN#42";
                Invoke("ForgetClick",window,new RoutedEventArgs());
                Assert(Find<TextBox>("Address").Text=="192.168.1.24" && Find<PasswordBox>("Secret").Password=="","Forget password cleared address or kept credential");
                Find<PasswordBox>("Secret").Password="Fixture-LAN#42";
                Invoke("RefreshDevicesClick",window,new RoutedEventArgs());
                Assert(Find<PasswordBox>("Secret").Password=="Fixture-LAN#42","Refresh overwrote an unsaved password draft");
                foreach(int width in new[]{820,1100,1440}) Snapshot("connect-"+lang+"-"+width,width);
                Invoke("ManualClick",window,new RoutedEventArgs());
                Assert(Visible("ManualDialog") && !Find<Grid>("HomeShell").IsEnabled,"Manual dialog did not isolate navigation");
                Assert(Find<TextBlock>("ManualDeviceName").Text=="Fixture Mac" && Find<TextBlock>("ManualDeviceSystem").Text=="macOS","Known target metadata missing from manual dialog");
                Snapshot("manual-known-"+lang,820);
                Find<TextBox>("ManualAddress").Text="192.168.1.99";
                Assert(Find<TextBlock>("ManualDeviceName").Visibility==Visibility.Collapsed && Find<PasswordBox>("ManualSecret").Password=="","Unknown IP fabricated metadata or retained another password");
                Snapshot("manual-unknown-"+lang,820,144);
                Invoke("ManualCancelClick",window,new RoutedEventArgs());
                Assert(!Visible("ManualDialog") && Find<TextBox>("Address").Text=="192.168.1.24","Cancel changed the target");
                Nav("NavSettings");
                Assert(Visible("SettingsPage") && !Visible("ConnectPage"),"Settings did not replace connection page");
                var tabs=Find<TabControl>("SettingsTabs");
                Assert(tabs.Items.Count==4,"Settings category count incorrect");
                for(int tab=0;tab<4;tab++) {
                    tabs.SelectedIndex=tab; Snapshot("settings-"+lang+"-"+tab,1100);
                    Snapshot("settings-"+lang+"-"+tab+"-820",820);
                    foreach(int dpi in new[]{120,144}) Snapshot("settings-"+lang+"-"+tab+"-dpi"+dpi,820,dpi);
                }
                Find<ComboBox>("DefaultQuality").SelectedIndex=0;
                Find<CheckBox>("DefaultShortcuts").IsChecked=true; Find<CheckBox>("DefaultReconnect").IsChecked=true;
                window.BeginConnectionOptions();
                Find<ComboBox>("DefaultQuality").SelectedIndex=2;
                Find<CheckBox>("DefaultShortcuts").IsChecked=false; Find<CheckBox>("DefaultReconnect").IsChecked=false;
                Assert(Find<ComboBox>("Quality").SelectedIndex==0 && Find<MenuItem>("WindowsShortcuts").IsChecked && Find<MenuItem>("Reconnect").IsChecked,"Future defaults changed current options");
                Find<MenuItem>("WindowsShortcuts").IsChecked=true;
                Assert(Find<CheckBox>("DefaultShortcuts").IsChecked==false,"Session choice overwrote default");
                Find<CheckBox>("GlobalClipboard").IsChecked=false; Find<CheckBox>("GlobalFiles").IsChecked=false;
                Find<MenuItem>("ClipboardSync").IsChecked=true; Find<MenuItem>("FileSync").IsChecked=true;
                Assert(!window.EffectiveClipboard && !window.EffectiveFiles && !Find<MenuItem>("ClipboardSync").IsEnabled && !Find<MenuItem>("FileSync").IsEnabled,"Session expanded disabled global consent");
                Find<CheckBox>("GlobalClipboard").IsChecked=true; Find<CheckBox>("GlobalFiles").IsChecked=true;
                var fields=typeof(MainWindow);
                void Field(string name,object value)=>fields.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(window,value);
                long Request()=>(long)fields.GetField("clipboardRequestId",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
                var text=(TextClipboardSync)fields.GetField("clipboard",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
                void Clipboard(object value) {using var message=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(value));Invoke("ClipboardControl",message.RootElement);}
                Field("connected",true); Clipboard(new {op="capabilities",clipboardText=true});
                var oldRequest=Request();Clipboard(new {op="clipboard_sync",enabled=true,requestId=oldRequest});
                Assert(text.Enabled,"Fresh consent acknowledgement did not enable fixture clipboard");
                Find<CheckBox>("GlobalClipboard").IsChecked=false;
                Clipboard(new {op="clipboard_sync",enabled=true,requestId=oldRequest});
                Clipboard(new {op="clipboard_sync",enabled=true,requestId=Request()});
                Assert(!text.Enabled,"Revoked global consent accepted stale or positive peer acknowledgement");
                Find<CheckBox>("GlobalClipboard").IsChecked=true;
                Clipboard(new {op="clipboard_sync",enabled=true,requestId=oldRequest});
                Assert(!text.Enabled,"Re-enable revived old consent");
                Clipboard(new {op="clipboard_sync",enabled=true,requestId=Request()});
                Assert(text.Enabled,"Fresh consent after re-enable did not work");
                Field("connected",false);Invoke("ResetClipboard");
                window.BeginConnectionOptions();
                Assert(Find<ComboBox>("Quality").SelectedIndex==2 && !Find<MenuItem>("WindowsShortcuts").IsChecked && !Find<MenuItem>("Reconnect").IsChecked,"New logical connection did not adopt defaults");
                Console.WriteLine("PASS four settings categories, next-connection defaults, temporary session choices and global clipboard/file consent ceiling");
                Nav("NavShare");
                Assert(Visible("SharingPage") && window.SharingController!=null && loads==1,"Sharing view duplicated credential reads");
                var sharing=window.SharingController!;
                ((TextBlock)sharing.FindName("SharingAddress")).Text="192.168.1.50";
                Assert(!sharing.IsVisible && sharing.Content==null && Find<ContentControl>("SharingPage").Content!=null,"Sharing opened another window");
                Assert(((TextBlock)sharing.FindName("CurrentPassword")).Text=="••••••••","Password revealed while navigating");
                ((Button)sharing.FindName("ChangeSharingPassword")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                ((TextBox)sharing.FindName("Draft")).Text="Uncommitted#42";
                ((Button)sharing.FindName("CancelSharingPassword")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(saved==0 && credential.Password=="Fixture-LAN#42","Cancel persisted an edited password");
                ((Button)sharing.FindName("ChangeDeviceName")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                ((TextBox)sharing.FindName("NameDraft")).Text="Office fixture";
                ((Button)sharing.FindName("SaveDeviceName")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(nameStore.Current=="Office fixture" && Find<TextBlock>("LocalDeviceName").Text=="Office fixture",$"Saved name not adopted in sharing/sidebar; fixture name={nameStore.Current}; sidebar={Find<TextBlock>("LocalDeviceName").Text}; error={((TextBlock)sharing.FindName("NameError")).Text}");
                ((Button)sharing.FindName("ChangeDeviceName")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                ((TextBox)sharing.FindName("NameDraft")).Text="Discarded name";
                ((Button)sharing.FindName("CancelDeviceName")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(nameStore.Current=="Office fixture" && saved==0,"Name cancel changed identity or credential");
                using(var metadata=System.Text.Json.JsonDocument.Parse("{\"deviceName\":\"Manual fixture\"}")) window.ApplyPeerName(metadata.RootElement,"192.168.1.70");
                using(var metadata=System.Text.Json.JsonDocument.Parse("{\"deviceName\":\"Renamed fixture\"}")) window.ApplyPeerName(metadata.RootElement,"192.168.1.70");
                Assert(Find<ListBox>("DeviceList").Items.OfType<LanDevice>().Count(d=>d.Address=="192.168.1.70")==1 && Find<TextBlock>("SelectedDeviceName").Text=="Renamed fixture","Manual/renamed peer duplicated row or failed display");
                Invoke("RefreshDevicesClick",window,new RoutedEventArgs());
                Assert(Find<ListBox>("DeviceList").Items.OfType<LanDevice>().Count(d=>d.Address=="192.168.1.70")==1,"Refresh lost an authenticated manual peer");
                Find<TextBox>("Address").Text="192.168.1.99";
                Assert(Find<TextBlock>("SelectedDeviceSystem").Text=="" && Find<TextBlock>("SelectedDeviceName").Text!= "Renamed fixture","New manual address inherited unrelated peer metadata");
                Assert(Find<PasswordBox>("Secret").Password=="","New IP retained an unrelated password");
                Find<PasswordBox>("Secret").Password="Fixture-LAN#42";
                Snapshot("sharing-"+lang,1100);
                Invoke("SetSessionLayout",true);
                Assert(!Visible("HomeShell") && Visible("SessionPage"),"Session leaves credential navigation visible");
                Snapshot("session-"+lang,1100);
                Invoke("SetSessionLayout",false);
                Assert(Visible("ConnectPage") && Find<PasswordBox>("Secret").Password=="Fixture-LAN#42","Session exit lost local pairing draft");
                Find<PasswordBox>("Secret").Password="";
            }
            await CheckPairingDialog();
            var page=window.SharingController!;
            void ShareClick(string name)=>((Button)page.FindName(name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(advertisement.Names.Count==0,"Idle sharing advertised a target");
            var oldCredential=credential.Password;
            ShareClick("ChangeSharingPassword");ShareClick("GenerateSharingPassword");
            Assert(((TextBox)page.FindName("Draft")).Text.Length==8 && saved==0 && credential.Password==oldCredential,"Generate did not remain an eight-character draft");
            ShareClick("CancelSharingPassword");
            ShareClick("ChangeSharingPassword");((TextBox)page.FindName("Draft")).Text="short";ShareClick("SaveSharingPassword");
            Assert(saved==0 && credential.Password==oldCredential && ((TextBlock)page.FindName("PasswordError")).Text.Length>0,"Invalid new password changed credential");
            ((TextBox)page.FindName("Draft")).Text="Office#2";ShareClick("SaveSharingPassword");
            await Until(()=>((FrameworkElement)page.FindName("Editor")).Visibility==Visibility.Collapsed,"Valid new password editor stayed open");
            Assert(saved==1 && credential.Password=="Office#2" && ((TextBlock)page.FindName("CurrentPassword")).Text=="••••••••","Valid eight-character password was not adopted and hidden");
            ((Button)page.FindName("SharingStart")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Until(()=>page.Sharing && ((Button)page.FindName("SharingStart")).IsEnabled,"Fixture sharing did not start");
            Assert(!Find<Button>("StartConnection").IsEnabled,"Active sharing permits control role");
            Assert(advertisement.Names.Count==1,"Started sharing not advertised exactly once");
            ShareClick("ChangeDeviceName");((TextBox)page.FindName("NameDraft")).Text="Renamed active PC";ShareClick("SaveDeviceName");
            Assert(advertisement.Names.Last()=="Renamed active PC" && page.Sharing && credential.Password=="Office#2","Active rename changed listener or password");
            Invoke("ManualClick",window,new RoutedEventArgs());
            Assert(Find<CheckBox>("ManualStopSharing").Visibility==Visibility.Visible,"Sharing role transition was not disclosed");
            Find<TextBox>("ManualAddress").Text="8.8.8.8";Find<PasswordBox>("ManualSecret").Password="Office#2";Find<CheckBox>("ManualStopSharing").IsChecked=true;
            Invoke("ManualConnectClick",window,new RoutedEventArgs());
            Assert(page.Sharing && Find<TextBlock>("ManualError").Text.Length>0,"Invalid target stopped sharing");
            Invoke("ManualCancelClick",window,new RoutedEventArgs());
            Assert(page.Sharing,"Cancel stopped sharing");
            window.Close(); await Until(()=>!page.Sharing && window.SharingController==null,"Owner shutdown left listener alive");
            Assert(advertisement.Stops>0,"Owner shutdown did not withdraw discovery");
            Console.WriteLine("PASS unshown native WPF shell: English/Chinese, 820/1100/1440px layouts, device selection, single-window sharing, hidden password, edit cancellation, session navigation, exclusive role and listener cleanup");
        } finally { if(window.SharingController is {} sharing) await sharing.Shutdown(); window.Close(); Directory.Delete(nameRoot,true); }
    }
}

sealed class ShellAdvertisement:IDeviceAdvertisement {
    public List<string> Names=new();public int Stops;
    public void Publish(string name,int port) {Names.Add(name);}
    public Task Stop() {Stops++;return Task.CompletedTask;}
}

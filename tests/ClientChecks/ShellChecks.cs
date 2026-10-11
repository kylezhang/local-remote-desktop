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
        int loads=0, saved=0;
        var nameRoot=Path.Combine(Path.GetTempPath(),"lrd-shell-name-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(nameRoot);
        var nameStore=new DeviceName(Path.Combine(nameRoot,"name.txt"),"Fixture PC");
        var credential=HostCredential.Create("Fixture-LAN#42");
        var area=new CaptureArea(0,0,320,180);
        var window=new MainWindow(false,null,discovery:_=>Task.FromResult<IReadOnlyList<LanDevice>>(
            [new("Fixture Mac","192.168.1.24","macos",47980)]),sharingFactory:changed=>new SharingWindow(changed,
                ()=> { loads++; return credential; },c=> { saved++; credential=c; },
                (c,key,software,report)=>new WindowsHost(key,c.Salt,area,()=>new SharingCapture(),
                    ()=>new WindowsInput(area,area,new SharingInput()),report,IPAddress.Loopback,0,deviceName:()=>nameStore.Current),deviceName:nameStore));
        T Find<T>(string name)=>(T)window.FindName(name);
        void Nav(string name)=>Find<RadioButton>(name).IsChecked=true;
        void Invoke(string name,params object[] args)=>typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,args);
        bool Visible(string name)=>Find<FrameworkElement>(name).Visibility==Visibility.Visible;
        void Snapshot(string name,int width) {
            var content=(FrameworkElement)window.Content;
            content.Measure(new Size(width,640)); content.Arrange(new Rect(0,0,width,640)); content.UpdateLayout();
            Assert(!window.IsVisible && !window.IsActive,"Background check opened/activated a window");
            var image=new RenderTargetBitmap(width,640,96,96,PixelFormats.Pbgra32); image.Render(content);
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
                foreach(int width in new[]{820,1100,1440}) Snapshot("connect-"+lang+"-"+width,width);
                Nav("NavSettings");
                Assert(Visible("SettingsPage") && !Visible("ConnectPage"),"Settings did not replace connection page");
                Snapshot("settings-"+lang,1100);
                Nav("NavShare");
                Assert(Visible("SharingPage") && window.SharingController!=null && loads==1,"Sharing view duplicated credential reads");
                var sharing=window.SharingController!;
                ((TextBlock)sharing.FindName("SharingAddress")).Text="192.168.1.50";
                Assert(!sharing.IsVisible && sharing.Content==null && Find<ContentControl>("SharingPage").Content!=null,"Sharing opened another window");
                Assert(((TextBlock)sharing.FindName("CurrentPassword")).Text=="••••••••••••","Password revealed while navigating");
                ((Button)sharing.FindName("ChangeSharingPassword")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                ((TextBox)sharing.FindName("Draft")).Text="Uncommitted#42";
                ((Button)sharing.FindName("CancelSharingPassword")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(saved==0 && credential.Password=="Fixture-LAN#42","Cancel persisted an edited password");
                ((Button)sharing.FindName("ChangeDeviceName")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                ((TextBox)sharing.FindName("NameDraft")).Text="Office fixture";
                ((Button)sharing.FindName("SaveDeviceName")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(nameStore.Current=="Office fixture" && Find<TextBlock>("LocalDeviceName").Text=="Office fixture","Saved name not adopted in sharing/sidebar");
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
                Snapshot("sharing-"+lang,1100);
                Invoke("SetSessionLayout",true);
                Assert(!Visible("HomeShell") && Visible("SessionPage"),"Session leaves credential navigation visible");
                Snapshot("session-"+lang,1100);
                Invoke("SetSessionLayout",false);
                Assert(Visible("ConnectPage") && Find<PasswordBox>("Secret").Password=="Fixture-LAN#42","Session exit lost local pairing draft");
                Find<PasswordBox>("Secret").Password="";
            }
            var page=window.SharingController!;
            ((Button)page.FindName("SharingStart")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Until(()=>page.Sharing && ((Button)page.FindName("SharingStart")).IsEnabled,"Fixture sharing did not start");
            Assert(!Find<Button>("StartConnection").IsEnabled,"Active sharing permits control role");
            window.Close(); await Until(()=>!page.Sharing && window.SharingController==null,"Owner shutdown left listener alive");
            Console.WriteLine("PASS unshown native WPF shell: English/Chinese, 820/1100/1440px layouts, device selection, single-window sharing, hidden password, edit cancellation, session navigation, exclusive role and listener cleanup");
        } finally { if(window.SharingController is {} sharing) await sharing.Shutdown(); window.Close(); Directory.Delete(nameRoot,true); }
    }
}

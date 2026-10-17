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
        await CheckSharingFailures();
        await CheckReverseClipboard();
        await CheckReverseFiles();
        await CheckRecovery();
        await CheckSessionCleanup();
        await CheckSessionInteraction();
        var emptyWindow=new MainWindow(false,null);
        CheckTypography(emptyWindow);
        Assert(((Border)emptyWindow.FindName("DeviceEmptyState")).Visibility==Visibility.Visible,"Empty devices have no transition state");
        Assert(((TextBlock)emptyWindow.FindName("DeviceEmptyTitle")).Text.Length>0,"Empty device state has no title");
        emptyWindow.Close();
        int loads=0, saved=0; bool failSave=false; var encoderChoices=new List<bool>();var advertisement=new ShellAdvertisement();
        var nameRoot=Path.Combine(Path.GetTempPath(),"lrd-shell-name-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(nameRoot);
        var nameStore=new DeviceName(Path.Combine(nameRoot,"name.txt"),"Fixture PC");
        var credential=HostCredential.Create("Fixture-LAN#42");
        var area=new CaptureArea(0,0,320,180);
        var window=new MainWindow(false,null,discovery:_=>Task.FromResult<IReadOnlyList<LanDevice>>(
            [new("Fixture Mac","192.168.1.24","macos",47980)]),sharingFactory:changed=>new SharingWindow(changed,
                ()=> { loads++; return credential; },c=> { if(failSave) throw new IOException("Fixture persistence failure"); saved++; credential=c; },
                (c,key,software,report)=> {encoderChoices.Add(software);return new WindowsHost(key,c.Salt,area,()=>new SharingCapture(),
                    ()=>new WindowsInput(area,area,new SharingInput()),report,IPAddress.Loopback,0,deviceName:()=>nameStore.Current);},deviceName:nameStore,advertisement:advertisement));
        ((TextBlock)window.FindName("LocalDeviceName")).Text="Fixture PC";
        T Find<T>(string name)=>(T)window.FindName(name);
        void Nav(string name)=>Find<RadioButton>(name).IsChecked=true;
        void Invoke(string name,params object[] args)=>typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,args);
        bool Visible(string name)=>Find<FrameworkElement>(name).Visibility==Visibility.Visible;
        IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject {
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) {
                var child=VisualTreeHelper.GetChild(root,i);
                if(child is T match) yield return match;
                foreach(var nested in Descendants<T>(child)) yield return nested;
            }
        }
        bool warnedUnavailableRender=false;
        void Snapshot(string name,int width,int dpi=96,int height=640) {
            var content=(FrameworkElement)window.Content;
            content.Measure(new Size(width,height)); content.Arrange(new Rect(0,0,width,height)); content.UpdateLayout();
            Assert(!window.IsVisible && !window.IsActive,"Background check opened/activated a window");
            var root=Environment.GetEnvironmentVariable("LOCALREMOTE_UI_SNAPSHOTS");
            if(root==null)return;
            var image=new RenderTargetBitmap((int)Math.Ceiling(width*dpi/96d),(int)Math.Ceiling(height*dpi/96d),dpi,dpi,PixelFormats.Pbgra32);
            image.Render(content);
            var pixels=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(pixels,image.PixelWidth*4,0);
            if(!pixels.Any(value=>value!=0)) {
                if(!warnedUnavailableRender) {Console.WriteLine("SKIP visual screenshot acceptance: hidden WPF rendering returned an empty bitmap; layout measurements still checked, no window will be shown.");warnedUnavailableRender=true;}
                return;
            }
            // Composite already-rendered pixels onto the native window color.
            // A VisualBrush of the live content can trigger a new Measure and
            // invalidate the layout assertions that follow this optional capture.
            var drawing=new DrawingVisual();
            using(var context=drawing.RenderOpen()) {
                var bounds=new Rect(0,0,width,height);
                context.DrawRectangle(Brushes.White,null,bounds);
                context.DrawImage(image,bounds);
            }
            var opaque=new RenderTargetBitmap(image.PixelWidth,image.PixelHeight,dpi,dpi,PixelFormats.Pbgra32);opaque.Render(drawing);
            Directory.CreateDirectory(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(opaque));
            using var file=File.Create(Path.Combine(root,name+".png"));png.Save(file);
        }
        try {
            foreach(var lang in new[]{"en","zh-Hans"}) {
                UiText.Current.Use(lang,false);
                Invoke("HomeBackClick",window,new RoutedEventArgs());
                Assert(Visible("EntryPage") && !Visible("ConnectPage") && !Visible("SharingPage"),"Back did not return to the role chooser");
                Assert(loads==(lang=="en"?0:1),"Role chooser accessed the sharing credential");
                Snapshot("entry-default-"+lang,584,96,640);
                Assert(Find<ScrollViewer>("EntryPage").ScrollableHeight<=0.5,"Default homepage unnecessarily scrolls");
                Assert(Find<Image>("ConnectionStage").Source is BitmapSource {PixelWidth:1500,PixelHeight:798},"Shared screen composition is missing from the package");
                Assert(window.FindName("InitiateIllustration")==null && window.FindName("AcceptIllustration")==null,"Home duplicates its screen composition in each role");
                var left=Find<RadioButton>("NavConnect");var right=Find<RadioButton>("NavShare");
                // Odd device-pixel widths can give one of two equal star columns
                // one extra pixel under UseLayoutRounding.
                Assert(Math.Abs(left.ActualWidth-right.ActualWidth)<=1 && left.ActualHeight>=76 && left.ActualHeight<110,
                    $"Joined role choices lost compact equal prominence: {left.ActualWidth}x{left.ActualHeight}, {right.ActualWidth}x{right.ActualHeight}");
                Assert(Find<Image>("ConnectionStage").TranslatePoint(new Point(0,260),Find<ScrollViewer>("EntryPage")).Y
                    <= left.TranslatePoint(new Point(0,0),Find<ScrollViewer>("EntryPage")).Y,"Shared art overlaps the role choices");
                Assert(!Visible("HomeBack") && Visible("NavSettings"),"Role chooser navigation is not compact");
                Nav("NavConnect");
                Assert(Visible("HomeShell") && Visible("ConnectPage") && !Visible("SessionPage"),"Initial role layout incorrect");
                Find<Button>("StartConnection").Content=UiText.Text("连接");
                Assert(Find<PasswordBox>("Secret").Password=="","Fixture accessed saved credentials");
                Invoke("RefreshDevicesClick",window,new RoutedEventArgs());
                Find<ListBox>("DeviceList").SelectedIndex=0;
                Assert(Find<TextBox>("Address").Text=="192.168.1.24" && Find<TextBlock>("SelectedDeviceName").Text=="Fixture Mac","Discovery selection lost target");
                Assert(Find<Border>("DeviceEmptyState").Visibility==Visibility.Collapsed,"Discovered device left empty-state placeholder visible");
                Find<PasswordBox>("Secret").Password="Fixture-LAN#42";
                Invoke("ForgetClick",window,new RoutedEventArgs());
                Assert(Find<TextBox>("Address").Text=="192.168.1.24" && Find<PasswordBox>("Secret").Password=="","Forget password cleared address or kept credential");
                Find<PasswordBox>("Secret").Password="Fixture-LAN#42";
                Invoke("RefreshDevicesClick",window,new RoutedEventArgs());
                Assert(Find<PasswordBox>("Secret").Password=="Fixture-LAN#42","Refresh overwrote an unsaved password draft");
                foreach(int width in new[]{584,624,820,1100,1440}) Snapshot("connect-"+lang+"-"+width,width);
                Snapshot("connect-default-"+lang,584,96,640);
                Assert(window.Width==Math.Min(600,SystemParameters.WorkArea.Width-48) && window.Height==Math.Min(680,SystemParameters.WorkArea.Height-48) && window.MinWidth<=520,"Compact startup width changed");
                Assert(Find<ListBox>("DeviceList").ActualWidth>=400,"Device list does not use the single-column page width");
                Assert(!Visible("ManualDialog") && Find<TextBox>("Address").ActualWidth==0,"Permanent connection form remains visible");
                Assert(Descendants<ScrollViewer>(Find<ListBox>("DeviceList")).All(scroll=>scroll.ScrollableHeight<=0.5),"Routine device list needs scrolling at startup");
                var targetButton=Descendants<Button>(Find<ListBox>("DeviceList")).First(button=>button.DataContext is LanDevice);
                targetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(Visible("ManualDialog") && !Find<Grid>("HomeShell").IsEnabled,"Manual dialog did not isolate navigation");
                Assert(Find<TextBlock>("ManualDeviceName").Text=="Fixture Mac" && Find<TextBlock>("ManualDeviceSystem").Text=="macOS","Known target metadata missing from manual dialog");
                Snapshot("manual-default-"+lang,584,96,640);
                Assert(Find<ScrollViewer>("ManualFieldsScroll").ScrollableHeight<=0.5,"Routine target dialog scrolls at the default size");
                Find<TextBlock>("ManualError").Text=string.Join("\n",Enumerable.Repeat("Fixture long authentication error",30));
                Snapshot("manual-error-"+lang,584,96,450);
                var manualActions=Find<Button>("ManualConnect");
                var manualBottom=manualActions.TranslatePoint(new Point(0,manualActions.ActualHeight),Find<Grid>("ManualDialog"));
                Assert(manualBottom.Y<=450 && Find<Button>("ManualCancel").ActualHeight>0,"Long pairing error hides Cancel/Connect");
                Find<TextBlock>("ManualError").Text="";
                Snapshot("manual-known-"+lang,584,96,450);
                Assert(Find<TextBox>("ManualAddress").ActualWidth>=300 && Find<ComboBox>("HomeQuality").ActualWidth>=300,"Compact connection dialog clipped its fields");
                Assert(Find<Button>("ManualConnect").TranslatePoint(new Point(0,0),Find<Grid>("ManualDialog")).X>=0,"Modal actions extend outside the compact window");
                Find<PasswordBox>("Secret").Password="Fixture-LAN#42";
                Find<TextBox>("ManualAddress").Text="192.168.1.99";
                Assert(Find<TextBlock>("ManualDeviceName").Visibility==Visibility.Collapsed && Find<PasswordBox>("ManualSecret").Password=="","Unknown IP fabricated metadata or retained another password");
                Find<PasswordBox>("ManualSecret").Password="Different#42";
                Find<Button>("ForgetButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert(Find<PasswordBox>("ManualSecret").Password=="" && Find<PasswordBox>("Secret").Password=="Fixture-LAN#42","Dialog forget modified another target's draft");
                Snapshot("manual-unknown-"+lang,624,144);
                Invoke("ManualCancelClick",window,new RoutedEventArgs());
                Assert(!Visible("ManualDialog") && Find<TextBox>("Address").Text=="192.168.1.24","Cancel changed the target");
                Nav("NavSettings");
                Assert(Visible("SettingsPage") && !Visible("ConnectPage"),"Settings did not replace connection page");
                var tabs=Find<TabControl>("SettingsTabs");
                Assert(tabs.Items.Count==4,"Settings category count incorrect");
                for(int tab=0;tab<4;tab++) {
                    tabs.SelectedIndex=tab; Snapshot("settings-"+lang+"-"+tab,1100);
                    Snapshot("settings-"+lang+"-"+tab+"-584",584);
                    foreach(int dpi in new[]{120,144}) Snapshot("settings-"+lang+"-"+tab+"-dpi"+dpi,820,dpi);
                    Snapshot("settings-default-"+lang+"-"+tab,584,96,640);
                    Assert(Descendants<ScrollViewer>(tabs).All(scroll=>scroll.ScrollableHeight<=0.5 && scroll.ScrollableWidth<=0.5),"Routine settings create an unnecessary scrollbar");
                    var surface=(Border)tabs.Template.FindName("SettingsSurface",tabs);
                    Assert(surface.ActualHeight<420,"Settings surface stretches into an empty panel");
                    if(tab==0) Assert(surface.ActualHeight<180,"Single language setting occupies a full-height panel");
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
                Snapshot("sharing-default-"+lang,584,96,640);
                var sharingScroll=(ScrollViewer)sharing.FindName("SharingScroll");
                Assert(sharingScroll.ScrollableHeight<=0.5,"Routine Accept connections page does not fit the default window");
                var deviceSurface=(Border)sharing.FindName("SharingDeviceSurface");
                var passwordSurface=(Border)sharing.FindName("SharingPasswordSurface");
                Assert(deviceSurface.ActualWidth>=390 && passwordSurface.ActualWidth>=340,"Sharing does not fit the compact content width");
                var start=(Button)sharing.FindName("SharingStart");
                var sharePage=Find<ContentControl>("SharingPage");
                var passwordPosition=passwordSurface.TranslatePoint(new Point(0,0),deviceSurface);
                Assert(passwordPosition.Y>20 && Math.Abs(passwordPosition.X-20)<1,"Password no longer follows the device information");
                var passwordBottom=passwordSurface.TranslatePoint(new Point(0,passwordSurface.ActualHeight),sharePage);
                var startPosition=start.TranslatePoint(new Point(0,0),sharePage);
                Assert(startPosition.Y>passwordBottom.Y && start.ActualWidth>=390,"Sharing action does not follow IP/password at the page bottom");
                Snapshot("sharing-minimum-"+lang,584,96,450);
                var actionBottom=start.TranslatePoint(new Point(start.ActualWidth,start.ActualHeight),sharePage);
                Assert(actionBottom.Y<=sharePage.ActualHeight && actionBottom.X<=sharePage.ActualWidth && start.ActualWidth>=340,"Bottom sharing action becomes unreachable in a small window");
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
            var startAction=(Button)page.FindName("SharingStart");
            var oldCredential=credential.Password;
            ShareClick("ChangeDeviceName");
            Assert(!startAction.IsEnabled,"Unsaved name permits Start sharing");
            ShareClick("SharingStart"); Assert(!page.Sharing,"Routed Start bypassed unsaved name");
            ShareClick("CancelDeviceName"); Assert(startAction.IsEnabled,"Cancel name did not restore Start");
            ShareClick("ChangeSharingPassword");ShareClick("GenerateSharingPassword");
            var pending=((TextBox)page.FindName("Draft")).Text;
            Invoke("HomeBackClick",window,new RoutedEventArgs());Nav("NavSettings");Nav("NavShare");
            Assert(((TextBox)page.FindName("Draft")).Text==pending && !startAction.IsEnabled,"Navigation lost a draft or bypassed its Start guard");
            Assert(!startAction.IsEnabled,"Unsaved password permits Start sharing");
            ShareClick("SharingStart"); Assert(!page.Sharing,"Routed Start bypassed unsaved password");
            Assert(((TextBox)page.FindName("Draft")).Text.Length==8 && saved==0 && credential.Password==oldCredential,"Generate did not remain an eight-character draft");
            ShareClick("CancelSharingPassword"); Assert(startAction.IsEnabled,"Cancel password did not restore Start");
            ShareClick("ChangeSharingPassword");((TextBox)page.FindName("Draft")).Text="short";ShareClick("SaveSharingPassword");
            Assert(saved==0 && credential.Password==oldCredential && ((TextBlock)page.FindName("PasswordError")).Text.Length>0,"Invalid new password changed credential");
            ((TextBox)page.FindName("Draft")).Text="Office#2";
            failSave=true;ShareClick("SaveSharingPassword");
            Assert(saved==0 && credential.Password==oldCredential && !startAction.IsEnabled && !page.Sharing,"Failed idle save permits Start or replaced credential");
            failSave=false;ShareClick("SaveSharingPassword");
            await Until(()=>((FrameworkElement)page.FindName("Editor")).Visibility==Visibility.Collapsed,"Valid new password editor stayed open");
            Assert(saved==1 && credential.Password=="Office#2" && ((TextBlock)page.FindName("CurrentPassword")).Text=="••••••••","Valid eight-character password was not adopted and hidden");
            var software=(CheckBox)page.FindName("Software");
            Assert(!((Expander)page.FindName("SharingTroubleshooting")).IsExpanded && software.IsChecked!=true,"Software fallback became the default sharing path");
            software.IsChecked=true;
            ((Button)page.FindName("SharingStart")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Until(()=>page.Sharing && ((Button)page.FindName("SharingStart")).IsEnabled,"Fixture sharing did not start");
            Assert(!Find<Button>("StartConnection").IsEnabled,"Active sharing permits control role");
            Assert(advertisement.Names.Count==1,"Started sharing not advertised exactly once");
            Invoke("HomeBackClick",window,new RoutedEventArgs());
            Assert(page.Sharing && Visible("EntryPage") && Visible("ActiveSharingShortcut"),"Home silently ended sharing or hid the stop route");
            Nav("NavShare");
            Assert(ReferenceEquals(page,window.SharingController) && loads==1,"Returning to Accept recreated its credentials/lifecycle");
            Assert(encoderChoices.SequenceEqual(new[]{true}) && !software.IsEnabled,"Software fallback was not wired or can change mid-session");
            ShareClick("ChangeDeviceName");((TextBox)page.FindName("NameDraft")).Text="Renamed active PC";ShareClick("SaveDeviceName");
            Assert(advertisement.Names.Last()=="Renamed active PC" && page.Sharing && credential.Password=="Office#2","Active rename changed listener or password");
            Invoke("ManualClick",window,new RoutedEventArgs());
            Assert(Find<CheckBox>("ManualStopSharing").Visibility==Visibility.Visible,"Sharing role transition was not disclosed");
            Find<TextBox>("ManualAddress").Text="8.8.8.8";Find<PasswordBox>("ManualSecret").Password="Office#2";Find<CheckBox>("ManualStopSharing").IsChecked=true;
            Invoke("ManualConnectClick",window,new RoutedEventArgs());
            Assert(page.Sharing && Find<TextBlock>("ManualError").Text.Length>0,"Invalid target stopped sharing");
            Invoke("ManualCancelClick",window,new RoutedEventArgs());
            Assert(page.Sharing,"Cancel stopped sharing");
            ShareClick("ChangeSharingPassword");((TextBox)page.FindName("Draft")).Text="Other#23";failSave=true;ShareClick("SaveSharingPassword");
            Assert(page.Sharing && startAction.IsEnabled && credential.Password=="Office#2","Failed active save interrupted sharing or disabled Stop");
            typeof(SharingWindow).GetMethod("FileReport",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(page,["接收文件：fixture",true]);
            await Until(()=>((FrameworkElement)page.FindName("SharingTransfer")).Visibility==Visibility.Visible,"Fixture transfer status did not appear");
            failSave=false;ShareClick("SharingStart");
            await Until(()=>!page.Sharing && !startAction.IsEnabled,"Stop while editing did not settle in draft-only state");
            Assert(((FrameworkElement)page.FindName("SharingTransfer")).Visibility==Visibility.Collapsed,"Stopped sharing retains stale transfer notice");
            Assert(((FrameworkElement)page.FindName("Editor")).Visibility==Visibility.Visible && ((TextBox)page.FindName("Draft")).Text=="Other#23","Stop discarded failed save draft");
            ShareClick("CancelSharingPassword");Assert(startAction.IsEnabled,"Cancel after Stop did not restore Start");
            ShareClick("SharingStart");await Until(()=>page.Sharing && startAction.IsEnabled,"Restart did not restore sharing action");
            var oldFiles=new ShellFileOwner();var newFiles=new ShellFileOwner();
            var fileField=typeof(SharingWindow).GetField("files",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var reportMethod=typeof(SharingWindow).GetMethod("SessionFileReport",BindingFlags.Instance|BindingFlags.NonPublic)!;
            fileField.SetValue(page,oldFiles);reportMethod.Invoke(page,[oldFiles,"接收文件：old session",true]);
            fileField.SetValue(page,newFiles);await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
            Assert(((FrameworkElement)page.FindName("SharingTransfer")).Visibility==Visibility.Collapsed,"Queued old session report appeared after replacement");
            reportMethod.Invoke(page,[newFiles,"接收文件：new session",true]);
            await Until(()=>((TextBlock)page.FindName("SharingFileStatus")).Text.Contains("new session"),"Current session report was blocked");
            ShareClick("ChangeSharingPassword");((TextBox)page.FindName("Draft")).Text="NewKey#3";ShareClick("SaveSharingPassword");
            await Until(()=>!page.Sharing && startAction.IsEnabled && ((FrameworkElement)page.FindName("Editor")).Visibility==Visibility.Collapsed,"Successful active password change auto-restarted sharing");
            Assert(saved==2 && credential.Password=="NewKey#3","Successful active save did not adopt new credential");
            ShareClick("SharingStart");await Until(()=>page.Sharing && startAction.IsEnabled,"Explicit Start after password save failed");
            Console.WriteLine("PASS unshown sharing edits: unsaved name/password block Start; failed-save draft and active sharing preserved; Stop remains available during edits; successful password change waits for explicit Start");
            window.Close(); await Until(()=>!page.Sharing && window.SharingController==null,"Owner shutdown left listener alive");
            Assert(advertisement.Stops>0,"Owner shutdown did not withdraw discovery");
            Console.WriteLine("PASS unshown native WPF shell: English/Chinese, 584/624/820/1100/1440px layouts, 600x680 startup + role chooser, draft/active-listener retention, single-column device action/dialog and vertically stacked sharing, device selection, single-window sharing, hidden password, edit cancellation, session navigation, exclusive role and listener cleanup");
        } finally { if(window.SharingController is {} sharing) await sharing.Shutdown(); window.Close(); Directory.Delete(nameRoot,true); }
    }
}

sealed class ShellAdvertisement:IDeviceAdvertisement {
    public List<string> Names=new();public int Stops;
    public void Publish(string name,int port) {Names.Add(name);}
    public Task Stop() {Stops++;return Task.CompletedTask;}
}

sealed class ShellFileOwner:IHostFiles {
    public string Session=>"fixture";public int Port=>0;
    public Task Control(System.Text.Json.JsonElement message)=>Task.CompletedTask;
    public void Cancel() { }
    public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
}

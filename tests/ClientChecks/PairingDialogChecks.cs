using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using LocalRemoteDesktop;

static partial class Program {
    // Exercise production mutual authentication and the UI persistence boundary
    // without showing a window, opening user credentials or posting OS input.
    static async Task CheckPairingDialog() {
        var listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
        int saves=0; bool rejectSave=false;
        var saved=new Dictionary<string,string> { ["127.0.0.1"]="Old#1234" };
        var credential=HostCredential.Create("Office#2");
        var key=PairingPassword.Derive(credential.Password,credential.Salt);
        var window=new MainWindow(false,null,desktopPort:((IPEndPoint)listener.LocalEndpoint).Port,
            discovery:_=>Task.FromResult<IReadOnlyList<LanDevice>>([]),
            passwordLoader:host=>saved.GetValueOrDefault(host,""),
            connectionSaver:(host,password)=> {saves++;if(rejectSave) throw new System.IO.IOException("Fixture store failure");saved[host]=password;});
        T Find<T>(string name)=>(T)window.FindName(name);
        void Invoke(string name)=>typeof(MainWindow).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(window,[window,new RoutedEventArgs()]);
        bool Active()=>typeof(MainWindow).GetField("session",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!=null;
        bool Connected()=> (bool)typeof(MainWindow).GetField("connected",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;
        void Open() {Invoke("ManualClick");Find<TextBox>("ManualAddress").Text="127.0.0.1";}
        async Task Attempt(string password,bool remember,bool accepted) {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var peer=Task.Run(async()=> {
                using var tcp=await listener.AcceptTcpClientAsync(deadline.Token);
                try {
                    using var wire=await Wire.Accept(tcp,key,credential.Salt,deadline.Token);
                    await wire.Read(deadline.Token);
                    await wire.Json(new {op="capabilities",platform="windows",inputModel="win-vk-v1",deviceName="Authenticated fixture",inputEnabled=false,clipboardText=false,clipboardFiles=false},deadline.Token);
                    while(true) await wire.Read(deadline.Token);
                } catch(Exception error) when(error is System.IO.IOException or OperationCanceledException or System.Security.Cryptography.CryptographicException) { }
            });
            Find<PasswordBox>("ManualSecret").Password=password;
            Find<CheckBox>("ManualRemember").IsChecked=remember;
            Invoke("ManualConnectClick");
            if(accepted) {
                await Until(Connected,"Authentication never completed");
                Assert(Find<Grid>("ManualDialog").Visibility==Visibility.Collapsed,"Successful auth left pairing dialog open");
                await Until(()=>Find<TextBlock>("SelectedDeviceName").Text=="Authenticated fixture","Authenticated metadata missing");
                Invoke("ConnectClick"); await Until(()=>!Active(),"Disconnect left session alive");
            } else {
                await Until(()=>!Active() && Find<Button>("ManualConnect").IsEnabled,"Failed authentication did not settle");
                Assert(Find<Grid>("ManualDialog").Visibility==Visibility.Visible && Find<PasswordBox>("ManualSecret").Password==password,"Failed auth lost pairing draft");
                Assert(Find<ListBox>("DeviceList").Items.Count==0,"Failed auth created a peer record");
            }
            deadline.Cancel();await peer;
            Assert(!window.IsVisible && !window.IsActive,"Pairing check activated a window");
        }
        try {
            Open();Find<PasswordBox>("ManualSecret").Password="Cancelled";Invoke("ManualCancelClick");
            Assert(saves==0 && saved["127.0.0.1"]=="Old#1234","Cancel changed saved credentials");
            Open();Find<TextBox>("ManualAddress").Text="8.8.8.8";Find<PasswordBox>("ManualSecret").Password="Office#2";Invoke("ManualConnectClick");
            Assert(!Active() && saves==0 && Find<TextBlock>("ManualError").Text.Length>0,"Public target bypassed LAN validation");
            Find<TextBox>("ManualAddress").Text="127.0.0.1";
            using(var cancelled=new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
                var accepted=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var stalled=Task.Run(async()=> {using var tcp=await listener.AcceptTcpClientAsync(cancelled.Token);accepted.SetResult();try {await Task.Delay(-1,cancelled.Token);}catch(OperationCanceledException){}});
                Find<PasswordBox>("ManualSecret").Password="Office#2";Find<CheckBox>("ManualRemember").IsChecked=true;
                Invoke("ManualConnectClick");await accepted.Task;Invoke("ManualCancelClick");
                await Until(()=>!Active() && Find<Grid>("ManualDialog").Visibility==Visibility.Collapsed,"Cancel during authentication did not close dialog/session");
                Assert(saves==0 && saved["127.0.0.1"]=="Old#1234","Canceled authentication wrote credentials");
                cancelled.Cancel();await stalled;
            }
            Open();
            await Attempt("Wrong#12",true,false);
            Assert(saves==0 && saved["127.0.0.1"]=="Old#1234","Wrong password overwrote saved credential");
            await Attempt("Office#2",false,true);
            Assert(saves==0 && saved["127.0.0.1"]=="Old#1234","Unchecked remembrance wrote credentials");
            Open();await Attempt("Office#2",true,true);
            Assert(saves==1 && saved["127.0.0.1"]=="Office#2","Authenticated remember did not save exactly once");
            saved["127.0.0.1"]="Old#1234";rejectSave=true;
            Open();await Attempt("Office#2",true,true);
            Assert(saves==2 && saved["127.0.0.1"]=="Old#1234","Persistence failure damaged original credential or session");
            Assert(Find<ListBox>("DeviceList").Items.OfType<LanDevice>().Count(d=>d.Address=="127.0.0.1")==1,"Repeated manual pairing duplicated peer");
            Console.WriteLine("PASS unshown manual pairing: cancel, private IPv4 validation, real wrong/successful authentication, draft retention, remember opt-out, save failure, authenticated metadata and one peer per address");
        } finally {listener.Stop();window.Close();}
    }
}

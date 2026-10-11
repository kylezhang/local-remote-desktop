using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using LocalRemoteDesktop;

static partial class Program {
    static async Task CheckRecovery() {
        foreach(var mode in new[]{"rejected-proof","invalid-proof","incompatible","network","cancel-retry"}) {
            var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(16));
            var credential=HostCredential.Create("Recover2");
            var key=PairingPassword.Derive(credential.Password,credential.Salt);
            var changedKey=PairingPassword.Derive("Changed2",credential.Salt);
            var closeFirst=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var window=new MainWindow(false,null,desktopPort:((IPEndPoint)listener.LocalEndpoint).Port);
            T Find<T>(string name)=>(T)window.FindName(name);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            bool Active()=>typeof(MainWindow).GetField("session",flags)!.GetValue(window)!=null;
            bool Connected()=>(bool)typeof(MainWindow).GetField("connected",flags)!.GetValue(window)!;
            int accepted=0;
            var peer=Task.Run(async()=> {
                try {
                    for(int index=0;index<2;index++) {
                        using var tcp=await listener.AcceptTcpClientAsync(deadline.Token);Interlocked.Increment(ref accepted);
                        if(index==1 && mode=="invalid-proof") {
                            // Independent bad server proof, never production capture/input.
                            var stream=tcp.GetStream();
                            await stream.WriteAsync("RMP1"u8.ToArray().Concat(RandomNumberGenerator.GetBytes(32)).Concat(credential.Salt).ToArray(),deadline.Token);
                            var response=new byte[64];await stream.ReadExactlyAsync(response,deadline.Token);
                            await stream.WriteAsync(new byte[32],deadline.Token);return;
                        }
                        using var wire=await Wire.Accept(tcp,index==1 && mode=="rejected-proof"?changedKey:key,credential.Salt,deadline.Token);
                        await wire.Read(deadline.Token);
                        await wire.Json(new {op="capabilities",platform="windows",inputModel=index==1 && mode=="incompatible"?"unknown-v99":"win-vk-v1",deviceName="Recovery fixture",inputEnabled=false,clipboardText=false,fileClipboard=false},deadline.Token);
                        if(index==0) {await closeFirst.Task.WaitAsync(deadline.Token);continue;}
                        while(true) await wire.Read(deadline.Token);
                    }
                } catch(Exception error) when(error is IOException or OperationCanceledException or CryptographicException or SocketException) { }
            });
            Task? runner=null;
            try {
                Find<TextBox>("Address").Text="127.0.0.1";Find<PasswordBox>("Secret").Password=credential.Password;
                Find<CheckBox>("DefaultReconnect").IsChecked=true;
                typeof(MainWindow).GetField("manualConnecting",flags)!.SetValue(window,true);
                runner=(Task)typeof(MainWindow).GetMethod("StartConnectionAsync",flags)!.Invoke(window,null)!;
                await Until(Connected,"Initial recovery fixture did not connect");closeFirst.SetResult();
                await Until(()=>!Connected(),"Broken session stayed connected");
                if(mode=="cancel-retry") {
                    typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
                    await runner.WaitAsync(TimeSpan.FromSeconds(4));await Task.Delay(3200);
                    Assert(accepted==1 && !Active(),"Disconnect allowed a delayed retry");
                } else if(mode=="network") {
                    await Until(()=>accepted==2 && Connected(),"Ordinary network loss failed to reconnect",9000);
                    typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
                    await runner.WaitAsync(TimeSpan.FromSeconds(4));
                } else {
                    await runner.WaitAsync(TimeSpan.FromSeconds(9));
                    Assert(accepted==2 && !Active(),"Permanent failure continued reconnecting");
                    Assert(Find<PasswordBox>("Secret").Password==credential.Password,"Recovery lost password draft");
                    var status=Find<TextBlock>("Status").Text;
                    Assert(Find<TextBlock>("HomeStatus").Text==status,"Clipboard reset obscured connection failure on Connect page");
                    Assert(!status.Contains(credential.Password) && !status.Contains("3 秒"),"Failure status leaked a credential or promised another retry");
                }
                Assert(!window.IsVisible && !window.IsActive,"Recovery test interrupted the desktop");
            } finally {
                deadline.Cancel();closeFirst.TrySetResult();listener.Stop();
                if(Active()) typeof(MainWindow).GetMethod("ConnectClick",flags)!.Invoke(window,[window,new RoutedEventArgs()]);
                if(runner!=null) await runner.WaitAsync(TimeSpan.FromSeconds(4));await peer;
                window.Close();CryptographicOperations.ZeroMemory(key);CryptographicOperations.ZeroMemory(changedKey);
            }
        }
        Console.WriteLine("PASS unshown production reconnect: transient loss recovers, canceled retry stays stopped, changed password/invalid server proof/incompatible target stop retrying; draft retained, no real capture/input/clipboard");
    }
}

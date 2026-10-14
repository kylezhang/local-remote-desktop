using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using LocalRemoteDesktop;

static partial class Program {
    static async Task CheckSharingFailures() {
        var credential=HostCredential.Create("Fixture#42");
        var cases=new Exception[]{new SocketException(10048),new SocketException(10013),
            new InvalidOperationException("Physical display geometry unavailable"),new InvalidOperationException("private-fixture-value")};
        foreach(var error in cases) {
            bool failed=true,sharing=false;
            var area=new CaptureArea(0,0,320,180);
            var page=new SharingWindow(value=>sharing=value,()=>credential,create:(c,key,software,report)=> {
                if(failed) throw error;
                return new WindowsHost(key,c.Salt,area,()=>new SharingCapture(),()=>new WindowsInput(area,area,new SharingInput()),report,IPAddress.Loopback,0);
            });
            try {
                void Click()=>((Button)page.FindName("SharingStart")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Click();
                await Until(()=>((TextBlock)page.FindName("SharingError")).Visibility==Visibility.Visible && ((Button)page.FindName("SharingStart")).IsEnabled,"Startup failure lost diagnostics or left Start disabled");
                Assert(!page.Sharing && !sharing,"Failed startup retained the accepting role");
                var details=((TextBlock)page.FindName("SharingError")).Text;
                Assert(details.Contains(error.GetType().Name) && !details.Contains("private-fixture-value") && !details.Contains(credential.Password),"Details missing type or exposed error payload/credential");
                foreach(var language in new[]{"en","zh-Hans"}) {
                    UiText.Current.Use(language);
                    Assert(((TextBlock)page.FindName("SharingStatus")).Text==UiText.Text(SharingFailure.Message(error)),"Failure translation changed or was lost");
                }
                Assert(((Expander)page.FindName("SharingTroubleshooting")).IsExpanded,"Failure details are hidden");
                failed=false;Click();
                await Until(()=>page.Sharing && ((Button)page.FindName("SharingStart")).IsEnabled,"Retry after startup failure did not recover");
                Assert(((TextBlock)page.FindName("SharingError")).Visibility==Visibility.Collapsed,"Retry retained obsolete failure");
            } finally {await page.Shutdown();UiText.Current.Use("zh-Hans");}
        }
        Assert(SharingFailure.Message(cases[0])!=SharingFailure.Message(cases[1]) && SharingFailure.Message(cases[1])!=SharingFailure.Message(cases[2]),"Distinct startup causes share a misleading message");
        Console.WriteLine("PASS unshown Windows sharing failures: occupied/denied port vs display/unknown, sanitized native codes, bilingual messages, role cleanup and retry; no capture/input/clipboard");
    }
}

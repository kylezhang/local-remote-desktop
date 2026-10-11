using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using LocalRemoteDesktop;

static class Check {
    const string Service="_localremote._tcp.local",Instance="Office Mac."+Service,Host="office.local";
    static void Assert(bool okay,string why) { if(!okay) throw new Exception(why); }
    static void U16(Stream stream,int value) { var b=new byte[2]; BinaryPrimitives.WriteUInt16BigEndian(b,(ushort)value); stream.Write(b); }
    static byte[] Name(string name) {
        using var s=new MemoryStream(); foreach(var label in name.Split('.')) { var bytes=Encoding.UTF8.GetBytes(label); s.WriteByte((byte)bytes.Length); s.Write(bytes); } s.WriteByte(0); return s.ToArray();
    }
    static byte[] Txt(params string[] fields) { using var s=new MemoryStream(); foreach(var field in fields) { var b=Encoding.UTF8.GetBytes(field); s.WriteByte((byte)b.Length); s.Write(b); } return s.ToArray(); }
    static byte[] Srv(int port=47980) { using var s=new MemoryStream(); U16(s,0); U16(s,0); U16(s,port); s.Write(Name(Host)); return s.ToArray(); }
    static byte[] Packet(ushort id,bool compressed=false,int ttl=10,string platform="macos",bool ptrOnly=false) {
        using var s=new MemoryStream(); U16(s,id); U16(s,0x8400); U16(s,0); U16(s,ptrOnly?1:4); U16(s,0); U16(s,0);
        void RR(byte[] name,int type,byte[] data) {
            s.Write(name); U16(s,type); U16(s,1); var b=new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b,(uint)ttl); s.Write(b); U16(s,data.Length); s.Write(data);
        }
        RR(Name(Service),12,Name(Instance));
        if(!ptrOnly) {
            var instanceName=Name(Instance);
            // Instance is the first PTR RDATA, immediately after service owner/header.
            if(compressed) { int p=12+Name(Service).Length+10; instanceName=new[]{(byte)(0xc0|(p>>8)),(byte)p}; }
            RR(instanceName,33,Srv()); RR(instanceName,16,Txt("protocol=1","platform="+platform));
            RR(Name(Host),1,IPAddress.Parse("192.168.7.10").GetAddressBytes());
        }
        return s.ToArray();
    }

    sealed class Lease: IAsyncDisposable {
        public int Stops; public ValueTask DisposeAsync() {Stops++;return ValueTask.CompletedTask;}
    }
    static async Task Until(Func<bool> ready) {
        for(int i=0;i<500 && !ready();i++) await Task.Delay(10);
        Assert(ready(),"Advertisement lifecycle stalled");
    }
    static async Task CheckAdvertisement() {
        var pending=new TaskCompletionSource<IAsyncDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lease=new Lease();var names=new List<string>();var notices=new List<bool>();
        var ads=new DeviceAdvertisement(value=> {lock(notices) notices.Add(value);},(name,port)=> {
            lock(names) {names.Add(name);return names.Count==1?pending.Task:Task.FromResult<IAsyncDisposable>(new Lease());}
        });
        ads.Publish("Initial PC",47980);await Until(()=>names.Count==1);
        ads.Publish("Intermediate PC",47980);ads.Publish("Latest PC",47980);
        pending.SetResult(lease);await Until(()=>names.Count==2);
        Assert(names.SequenceEqual(new[]{"Initial PC","Latest PC"}) && lease.Stops==1,"Publication updates not coalesced");
        await ads.Stop();Assert(notices.Count==2 && notices.All(x=>x),"Successful publication reported unavailable");
        var delayed=new TaskCompletionSource<IAsyncDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);var late=new Lease();
        var stopped=new DeviceAdvertisement(_=>{},(_,_)=>delayed.Task);
        stopped.Publish("Pending PC",47980);var wait=stopped.Stop();delayed.SetResult(late);await wait;
        // If Stop ran before registration started, no native call was required.
        Assert(late.Stops<=1,"Late native registration freed twice");
        int calls=0,failures=0;
        var bad=new DeviceAdvertisement(ok=> {if(!ok) failures++;},(_,_)=> {calls++;return Task.FromException<IAsyncDisposable>(new IOException("fixture"));});
        bad.Publish("Unavailable PC",47980);await Until(()=>failures==1);await Task.Delay(50);
        Assert(calls==1,"Unavailable discovery busy-loops/retries by itself");await bad.Stop();
        var failure=new TaskCompletionSource<IAsyncDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);int attempts=0;
        var switched=new DeviceAdvertisement(_=>{},(_,_)=>++attempts==1?failure.Task:Task.FromResult<IAsyncDisposable>(new Lease()));
        switched.Publish("Old PC",47980);await Until(()=>attempts==1);switched.Publish("New PC",47980);
        failure.SetException(new IOException("fixture"));await Until(()=>attempts==2);await switched.Stop();
        if(OperatingSystem.IsWindows()) {
            Assert(Marshal.SizeOf<NativeAdvertisement.Request>()==(IntPtr.Size==8?48:28),"Native DNS request layout mismatch");
            var ptr=NativeAdvertisement.Construct("Fixture PC",47980);
            try {
                var value=Marshal.PtrToStructure<NativeAdvertisement.Instance>(ptr);
                Assert(value.Port==47980 && value.Count==3,"Native DNS instance layout mismatch");
                var values=new Dictionary<string,string>();
                for(int i=0;i<value.Count;i++) values.Add(Marshal.PtrToStringUni(Marshal.ReadIntPtr(value.Keys,i*IntPtr.Size))!,Marshal.PtrToStringUni(Marshal.ReadIntPtr(value.Values,i*IntPtr.Size))!);
                Assert(values.Count==3 && values["protocol"]=="1" && values["platform"]=="windows" && values["deviceName"]=="Fixture PC","Native TXT not cloned or contains credentials");
            } finally {NativeAdvertisement.DnsServiceFreeInstance(ptr);}
            Console.WriteLine("PASS actual Windows DNS native construction/free and pointer/UTF-16 TXT layout");
            if(Environment.GetEnvironmentVariable("LOCALREMOTE_NATIVE_DISCOVERY_CHECK")=="1") {
                var registered=await NativeAdvertisement.Register("LRD discovery fixture",47980).WaitAsync(TimeSpan.FromSeconds(10));
                await registered.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                Console.WriteLine("PASS actual Windows OS DNS-SD register/completion/deregister/free; temporary metadata only, no capture/input/credentials");
            } else Console.WriteLine("SKIP actual DNS-SD registration; opt in with LOCALREMOTE_NATIVE_DISCOVERY_CHECK=1");
        }
        Console.WriteLine("PASS publication manager coalescing, stop while pending, failure without polling and recovery after rename (native-operation doubles)");
    }
    static async Task Main() {
        await CheckAdvertisement();
        var packet=Packet(43,true); var records=MdnsDiscovery.Parse(packet,43); var devices=MdnsDiscovery.Devices(records);
        Assert(devices.Count==1 && devices[0].Name=="Office Mac" && devices[0].Address=="192.168.7.10","compressed DNS-SD fixture rejected");
        var custom=records.Select(r=>r.Type==16?r with {Txt=new Dictionary<string,string>{{"protocol","1"},{"platform","macos"},{"deviceName","办公室 Mac"}}}:r);
        Assert(MdnsDiscovery.Devices(custom).Single().Name=="办公室 Mac","Full display-name TXT ignored");
        var malformed=records.Select(r=>r.Type==16?r with {Txt=new Dictionary<string,string>{{"protocol","1"},{"platform","macos"},{"deviceName","Bad\nName"}}}:r);
        Assert(MdnsDiscovery.Devices(malformed).Single().Name=="Office Mac","Malformed TXT name failed legacy instance fallback");
        Assert(MdnsDiscovery.Devices(MdnsDiscovery.Parse(Packet(43,platform:"windows"),43)).Single().Platform=="windows","Windows sharing discovery rejected");
        Assert(MdnsDiscovery.Parse(packet,44).Count==0,"foreign transaction accepted");
        Assert(MdnsDiscovery.Devices(MdnsDiscovery.Parse(Packet(43,platform:"untrusted"),43)).Count==0,"unsupported platform accepted");
        Assert(MdnsDiscovery.Devices(MdnsDiscovery.Parse(Packet(43,ttl:0),43)).Count==0,"goodbye record accepted");
        Assert(MdnsDiscovery.Devices(records.Select(r=>r.Type==1?r with{Address="8.8.8.8"}:r)).Count==0,"public endpoint offered");
        Assert(MdnsDiscovery.Devices(records.Select(r=>r.Type==33?r with{Port=443}:r)).Count==0,"unexpected control port accepted");
        Assert(MdnsDiscovery.Devices(records.Concat(records)).Count==1,"duplicate IPs offered");
        for(int len=0;len<packet.Length;len++) Assert(MdnsDiscovery.Parse(packet[..len],43).Count==0,"truncated response accepted");
        var cycle=(byte[])packet.Clone(); cycle[12]=0xc0; cycle[13]=12;
        Assert(MdnsDiscovery.Parse(cycle,43).Count==0,"name compression cycle accepted");
        var random=new Random(17);
        for(int i=0;i<5000;i++) { var b=new byte[random.Next(0,512)]; random.NextBytes(b); _=MdnsDiscovery.Parse(b,43); }
        Console.WriteLine("PASS independent DNS-SD/compressed/foreign/TTL/port/LAN/duplicate/truncation/cycle/fuzz fixtures");

        using var responder=new UdpClient(new IPEndPoint(IPAddress.Loopback,0));
        var endpoint=(IPEndPoint)responder.Client.LocalEndPoint!; using var stop=new CancellationTokenSource();
        int received=0;
        var server=Task.Run(async()=> {
            try { while(true) {
                var request=await responder.ReceiveAsync(stop.Token); Interlocked.Increment(ref received);
                Assert(request.Buffer.Length>=12 && request.Buffer[2]==0,"malformed query");
                ushort id=BinaryPrimitives.ReadUInt16BigEndian(request.Buffer);
                await responder.SendAsync(Packet(id,ptrOnly:received==1),request.RemoteEndPoint,stop.Token);
            } } catch(OperationCanceledException) { }
        });
        var found=await MdnsDiscovery.ScanInterface(IPAddress.Loopback,endpoint,TimeSpan.FromMilliseconds(1200),CancellationToken.None);
        Assert(found.Count==1 && received>=3 && received<=5,"split-record followups/network discovery failed");
        using(var cancel=new CancellationTokenSource(30)) {
            try { await MdnsDiscovery.ScanInterface(IPAddress.Loopback,endpoint,TimeSpan.FromSeconds(10),cancel.Token); throw new Exception("cancel ignored"); }
            catch(OperationCanceledException) { }
        }
        stop.Cancel(); await server;
        Console.WriteLine("PASS actual UDP query/reply, split record followups, bounded sends and cancellation/socket cleanup");
        Console.WriteLine("SKIP actual Mac Bonjour multicast/network-permission acceptance; UDP peer is independent loopback fixture.");
    }
}

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalRemote;

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
    static async Task Main() {
        var packet=Packet(43,true); var records=MdnsDiscovery.Parse(packet,43); var devices=MdnsDiscovery.Devices(records);
        Assert(devices.Count==1 && devices[0].Name=="Office Mac" && devices[0].Address=="192.168.7.10","compressed DNS-SD fixture rejected");
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
        var found=await MdnsDiscovery.ScanInterface(IPAddress.Loopback,endpoint,TimeSpan.FromMilliseconds(350),CancellationToken.None);
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

using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
namespace LocalRemoteDesktop;

public sealed record LanDevice(string Name,string Address,string Platform,int Port) {
    public string SystemLabel=>Platform=="macos"?"macOS":Platform=="windows"?"Windows":"";
    public override string ToString()=>$"{Name} · {Address}";
}
// One-shot DNS-SD, not a continuously running resolver. Metadata never grants control.
internal static class MdnsDiscovery {
    internal const string Service="_localremote._tcp.local";
    internal sealed record Record(string Name,ushort Type,uint Ttl,string? Target=null,
        int Port=0,string? Address=null,IReadOnlyDictionary<string,string>? Txt=null);
    static readonly UTF8Encoding Utf8=new(false,true);
    static ushort U16(byte[] b,int at) {
        if(at<0 || at+2>b.Length) throw new InvalidDataException();
        return BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(at,2));
    }
    static string Name(byte[] b,ref int offset) {
        var parts=new List<string>(); var seen=new HashSet<int>(); int at=offset,end=-1,size=0;
        for(int step=0;step<128;step++) {
            if(at>=b.Length || !seen.Add(at)) throw new InvalidDataException();
            int n=b[at++];
            if(n==0) { offset=end<0?at:end; return string.Join('.',parts); }
            if((n&0xc0)==0xc0) {
                if(at>=b.Length) throw new InvalidDataException(); int pointer=((n&63)<<8)|b[at++];
                if(pointer>=at-2) throw new InvalidDataException();
                if(end<0) end=at; at=pointer; continue;
            }
            if((n&0xc0)!=0 || at+n>b.Length || (size+=n+1)>255) throw new InvalidDataException();
            var text=Utf8.GetString(b,at,n); if(text.Any(char.IsControl)) throw new InvalidDataException();
            parts.Add(text); at+=n;
        }
        throw new InvalidDataException();
    }
    internal static IReadOnlyList<Record> Parse(byte[] b,ushort id) {
        try {
            if(b.Length<12 || b.Length>9000 || U16(b,0)!=id || (U16(b,2)&0xf80f)!=0x8000) return [];
            int q=U16(b,4),count=U16(b,6)+U16(b,8)+U16(b,10),at=12;
            if(q>32 || count>256) return [];
            for(int i=0;i<q;i++) { Name(b,ref at); at+=4; if(at>b.Length) return []; }
            var result=new List<Record>();
            for(int i=0;i<count;i++) {
                var name=Name(b,ref at); ushort type=U16(b,at),cls=U16(b,at+2); at+=4;
                if(at+6>b.Length) return [];
                uint ttl=BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(at,4)); at+=4;
                int len=U16(b,at); at+=2; int end=at+len,value=at; if(end>b.Length) return [];
                if((cls&0x7fff)==1) {
                    if(type==12) { var target=Name(b,ref value); if(value!=end) return []; result.Add(new(name,type,ttl,Target:target)); }
                    else if(type==33 && len>=7) {
                        int port=U16(b,at+4); value=at+6; var target=Name(b,ref value);
                        if(value!=end) return []; result.Add(new(name,type,ttl,Target:target,Port:port));
                    } else if(type==1 && len==4) result.Add(new(name,type,ttl,Address:new IPAddress(b.AsSpan(at,4)).ToString()));
                    else if(type==16 && len<=1024) {
                        var txt=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                        while(value<end) {
                            int n=b[value++]; if(value+n>end) return [];
                            var text=Utf8.GetString(b,value,n); value+=n; int equal=text.IndexOf('=');
                            if(equal>0 && !text.Any(char.IsControl)) txt.TryAdd(text[..equal],text[(equal+1)..]);
                        }
                        result.Add(new(name,type,ttl,Txt:txt));
                    }
                }
                at=end;
            }
            return at==b.Length?result:[];
        } catch(Exception e) when(e is InvalidDataException or DecoderFallbackException or ArgumentException) { return []; }
    }
    internal static byte[] Query(string name,ushort type,ushort id) {
        using var stream=new MemoryStream(); var h=new byte[12]; BinaryPrimitives.WriteUInt16BigEndian(h,id); h[5]=1; stream.Write(h);
        foreach(var part in name.TrimEnd('.').Split('.')) {
            var bytes=Utf8.GetBytes(part); if(bytes.Length is <1 or >63) throw new ArgumentException("Invalid DNS label");
            stream.WriteByte((byte)bytes.Length); stream.Write(bytes);
        }
        stream.WriteByte(0); var tail=new byte[4]; BinaryPrimitives.WriteUInt16BigEndian(tail,type); tail[3]=1; stream.Write(tail);
        return stream.ToArray();
    }
    static bool Same(string? a,string? b)=>string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
    internal static bool Local(IPAddress ip) {
        if(ip.AddressFamily!=AddressFamily.InterNetwork) return false;
        var b=ip.GetAddressBytes(); return b[0]==10 || (b[0]==172 && b[1]>=16 && b[1]<=31) ||
            (b[0]==192 && b[1]==168) || (b[0]==169 && b[1]==254) || IPAddress.IsLoopback(ip);
    }
    internal static IReadOnlyList<LanDevice> Devices(IEnumerable<Record> records) {
        var live=records.Where(r=>r.Ttl>0).Take(256).ToArray(); var devices=new List<LanDevice>();
        foreach(var ptr in live.Where(r=>r.Type==12 && Same(r.Name,Service)).Take(32)) {
            if(ptr.Target==null || !ptr.Target.EndsWith('.'+Service,StringComparison.OrdinalIgnoreCase)) continue;
            var txt=live.FirstOrDefault(r=>r.Type==16 && Same(r.Name,ptr.Target))?.Txt;
            if(txt==null || !txt.TryGetValue("protocol",out var protocol) || protocol!="1" ||
                !txt.TryGetValue("platform",out var platform) || platform!="macos") continue;
            foreach(var srv in live.Where(r=>r.Type==33 && r.Port==47980 && Same(r.Name,ptr.Target))) {
                foreach(var a in live.Where(r=>r.Type==1 && Same(r.Name,srv.Target))) {
                    if(a.Address==null || !IPAddress.TryParse(a.Address,out var ip) || !Local(ip)) continue;
                    var name=DeviceName.Valid(txt.GetValueOrDefault("deviceName"))??ptr.Target[..^(Service.Length+1)];
                    if(name.Length==0 || name.Length>100 || name.Any(char.IsControl)) continue;
                    if(!devices.Any(d=>d.Address==ip.ToString())) devices.Add(new(name,ip.ToString(),platform,srv.Port));
                    if(devices.Count==32) return devices;
                }
            }
        }
        return devices;
    }
    internal static async Task<IReadOnlyList<LanDevice>> ScanInterface(IPAddress local,IPEndPoint destination,TimeSpan duration,CancellationToken token) {
        using var end=CancellationTokenSource.CreateLinkedTokenSource(token); end.CancelAfter(duration);
        using var udp=new UdpClient(new IPEndPoint(local,0));
        udp.Client.SetSocketOption(SocketOptionLevel.IP,SocketOptionName.MulticastTimeToLive,255);
        ushort id=(ushort)Random.Shared.Next(1,65536);
        var records=new Dictionary<(string,ushort,string),(Record record,long expires)>();
        var asked=new HashSet<(string,ushort)>(); int followups=0;
        async Task Send(string name,ushort type)=>await udp.SendAsync(Query(name,type,id),destination,end.Token).ConfigureAwait(false);
        try {
            await Send(Service,12);
            while(!end.IsCancellationRequested) {
                var reply=await udp.ReceiveAsync(end.Token).ConfigureAwait(false);
                if(reply.RemoteEndPoint.Port!=destination.Port || !Local(reply.RemoteEndPoint.Address)) continue;
                long now=Environment.TickCount64;
                foreach(var key in records.Where(x=>x.Value.expires<=now).Select(x=>x.Key).ToArray()) records.Remove(key);
                foreach(var item in Parse(reply.Buffer,id)) {
                    var key=(item.Name.ToLowerInvariant(),item.Type,item.Address??item.Target??"txt");
                    if(item.Ttl==0) { records.Remove(key); continue; }
                    if(records.Count<256 || records.ContainsKey(key)) records[key]=(item,now+Math.Min(item.Ttl,10u)*1000L);
                }
                foreach(var item in records.Values.Select(x=>x.record).ToArray()) {
                    var queries=item.Type==12 && Same(item.Name,Service) && item.Target!=null
                        ?new[]{(item.Target,(ushort)33),(item.Target,(ushort)16)}
                        :item.Type==33 && item.Target!=null?new[]{(item.Target,(ushort)1)}:[];
                    foreach(var query in queries) if(followups<24 && asked.Add(query)) { followups++; await Send(query.Item1,query.Item2); }
                }
            }
        } catch(OperationCanceledException) when(!token.IsCancellationRequested) { }
        token.ThrowIfCancellationRequested();
        return Devices(records.Values.Where(x=>x.expires>Environment.TickCount64).Select(x=>x.record));
    }
    internal static async Task<IReadOnlyList<LanDevice>> Scan(CancellationToken token) {
        var addresses=NetworkInterface.GetAllNetworkInterfaces()
            .Where(n=>n.OperationalStatus==OperationalStatus.Up && n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
            .SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address)
            .Where(a=>Local(a) && !IPAddress.IsLoopback(a)).Distinct().Take(4).ToArray();
        var tasks=addresses.Select(async address=> {
            try { return await ScanInterface(address,new(IPAddress.Parse("224.0.0.251"),5353),TimeSpan.FromSeconds(3),token); }
            catch(SocketException) { return (IReadOnlyList<LanDevice>)[]; }
        });
        return (await Task.WhenAll(tasks)).SelectMany(d=>d).DistinctBy(d=>d.Address).Take(32).OrderBy(d=>d.Name,StringComparer.OrdinalIgnoreCase).ToArray();
    }
}

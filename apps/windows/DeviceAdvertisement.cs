using System.Runtime.InteropServices;
namespace LocalRemoteDesktop;

internal interface IDeviceAdvertisement {
    void Publish(string name,int port);
    Task Stop();
}
internal sealed class NoDeviceAdvertisement:IDeviceAdvertisement {
    public void Publish(string name,int port) { }
    public Task Stop()=>Task.CompletedTask;
}

// Coalesce name changes. One native operation at a time; no polling timer,
// per-interface responder sockets or queue that grows while DNS is unavailable.
internal sealed class DeviceAdvertisement:IDeviceAdvertisement {
    readonly object gate=new();
    readonly Func<string,int,Task<IAsyncDisposable>> register;
    readonly Action<bool> available;
    (string name,int port)? desired,current;
    IAsyncDisposable? registration;Task? pump;
    internal DeviceAdvertisement(Action<bool> available,Func<string,int,Task<IAsyncDisposable>>? register=null) {
        this.available=available;this.register=register??NativeAdvertisement.Register;
    }
    public void Publish(string name,int port) {
        var valid=DeviceName.Valid(name)??throw new ArgumentException("Invalid display name");
        if(port!=47980) throw new ArgumentException("Unsupported discovery port");
        lock(gate) {desired=(valid,port);pump??=Task.Run(Update);}
    }
    public Task Stop() {lock(gate) {desired=null;return pump??=Task.Run(Update);}}
    async Task Update() {
        (string name,int port)? attempted=null;
        try {
            while(true) {
                (string name,int port)? next;
                lock(gate) {next=desired;if(next==current) {pump=null;return;}}
                attempted=next;
                if(registration!=null) {await registration.DisposeAsync();registration=null;current=null;}
                lock(gate) {if(next!=desired) continue;}
                if(next is {} value) {registration=await register(value.name,value.port);current=next;Notify(true);}
            }
        } catch {
            Notify(false);
            lock(gate) {pump=null;if(desired!=attempted) pump=Task.Run(Update);}
        }
    }
    void Notify(bool value) {try {available(value);} catch { /* UI may already be shutting down. */ }}
}

// Windows DNS-SD service registration is owned by this process. Use the OS
// responder (Windows 10+), so no Bonjour installation or UDP listener is needed.
internal sealed class NativeAdvertisement:IAsyncDisposable {
    [StructLayout(LayoutKind.Sequential)] internal struct Request {
        public uint Version,InterfaceIndex;
        public IntPtr Instance,Callback,Context,Credentials;
        public int Unicast;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Instance {
        public IntPtr Name,Host,Ip4,Ip6;
        public ushort Port,Priority,Weight;
        public uint Count;public IntPtr Keys,Values;public uint Interface;
    }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate void Completion(uint status,IntPtr context,IntPtr instance);
    static readonly Completion completed=Completed;
    const uint Pending=9506;
    [DllImport("dnsapi.dll",CharSet=CharSet.Unicode)] static extern IntPtr DnsServiceConstructInstance(string service,string host,IntPtr ipv4,IntPtr ipv6,ushort port,ushort priority,ushort weight,uint count,IntPtr keys,IntPtr values);
    [DllImport("dnsapi.dll")] static extern uint DnsServiceRegister(IntPtr request,IntPtr cancel);
    [DllImport("dnsapi.dll")] static extern uint DnsServiceDeRegister(IntPtr request,IntPtr cancel);
    [DllImport("dnsapi.dll")] internal static extern void DnsServiceFreeInstance(IntPtr instance);
    IntPtr instance;
    NativeAdvertisement(IntPtr instance) {this.instance=instance;}
    static void Completed(uint status,IntPtr context,IntPtr instance) {
        var handle=GCHandle.FromIntPtr(context);
        var source=(TaskCompletionSource<(uint,IntPtr)>)handle.Target!;
        source.TrySetResult((status,instance));
    }
    static async Task<IntPtr> Operation(IntPtr instance,bool remove) {
        var source=new TaskCompletionSource<(uint status,IntPtr instance)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context=GCHandle.Alloc(source);
        var request=Marshal.AllocHGlobal(Marshal.SizeOf<Request>());
        try {
            Marshal.StructureToPtr(new Request {Version=1,Instance=instance,Callback=Marshal.GetFunctionPointerForDelegate(completed),Context=GCHandle.ToIntPtr(context)},request,false);
            var status=remove?DnsServiceDeRegister(request,IntPtr.Zero):DnsServiceRegister(request,IntPtr.Zero);
            if(status!=Pending) throw new System.ComponentModel.Win32Exception((int)status);
            // Keep request/context alive until native completion. The stable
            // callback is rooted for process lifetime; never free pending memory.
            var result=await source.Task;
            if(result.status!=0) {
                if(result.instance!=IntPtr.Zero && result.instance!=instance) DnsServiceFreeInstance(result.instance);
                throw new System.ComponentModel.Win32Exception((int)result.status);
            }
            return result.instance;
        } finally {context.Free();Marshal.FreeHGlobal(request);}
    }
    internal static IntPtr Construct(string name,int port) {
        var label="lrd-"+Guid.NewGuid().ToString("N");
        var keys=new[]{"protocol","platform","deviceName"};var values=new[]{"1","windows",name};
        var allocated=new List<IntPtr>();
        IntPtr Array(string[] items) {
            var pointers=items.Select(value=> {var p=Marshal.StringToHGlobalUni(value);allocated.Add(p);return p;}).ToArray();
            var result=Marshal.AllocHGlobal(IntPtr.Size*items.Length);allocated.Add(result);Marshal.Copy(pointers,0,result,pointers.Length);return result;
        }
        try {
            var result=DnsServiceConstructInstance(label+"."+MdnsDiscovery.Service,label+".local",IntPtr.Zero,IntPtr.Zero,(ushort)port,0,0,3,Array(keys),Array(values));
            if(result==IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());return result;
        } finally {foreach(var p in allocated) Marshal.FreeHGlobal(p);}
    }
    internal static async Task<IAsyncDisposable> Register(string name,int port) {
        var initial=Construct(name,port);
        try {
            var registered=await Operation(initial,false);
            if(registered==IntPtr.Zero) throw new InvalidOperationException("DNS-SD registration returned no instance");
            // The callback returns the authoritative registered instance, used
            // unchanged for deregistration even if DNS resolved a name collision.
            if(registered==initial) initial=IntPtr.Zero;
            return new NativeAdvertisement(registered);
        } finally {if(initial!=IntPtr.Zero) DnsServiceFreeInstance(initial);}
    }
    public async ValueTask DisposeAsync() {
        if(instance==IntPtr.Zero) return;
        var removed=await Operation(instance,true);
        if(removed!=IntPtr.Zero && removed!=instance) DnsServiceFreeInstance(removed);
        DnsServiceFreeInstance(instance);instance=IntPtr.Zero;
    }
}

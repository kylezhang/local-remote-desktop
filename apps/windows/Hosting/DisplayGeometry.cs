using System.Runtime.InteropServices;
namespace LocalRemoteDesktop;

// Capture pixels and absolute input must use the same physical desktop. Window
// DPI units are unsuitable here, particularly with mixed-scale displays.
internal readonly record struct DisplayGeometry(CaptureArea Primary,CaptureArea Virtual) {
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    struct DisplayDevice {
        internal int Size;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] internal string Name;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] internal string Description;
        internal uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] internal string Id;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] internal string Key;
    }
    // DEVMODEW's display union and dimensions; unused fields remain zero.
    [StructLayout(LayoutKind.Explicit,Size=220)]
    struct DisplayMode {
        [FieldOffset(68)] internal ushort Size;
        [FieldOffset(72)] internal uint Fields;
        [FieldOffset(76)] internal int X;
        [FieldOffset(80)] internal int Y;
        [FieldOffset(172)] internal uint Width;
        [FieldOffset(176)] internal uint Height;
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]
    static extern bool EnumDisplayDevices(string? device,uint index,ref DisplayDevice display,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]
    static extern bool EnumDisplaySettingsEx(string device,int index,ref DisplayMode mode,uint flags);

    internal static DisplayGeometry Read()=>Resolve(ReadModes,ReadMonitors);
    internal static DisplayGeometry Resolve(Func<DisplayGeometry> modes,Func<DisplayGeometry> monitors) {
        try {return modes();}
        catch(Exception error) when(error is InvalidOperationException or ArgumentException) {return monitors();}
    }
    static DisplayGeometry ReadModes() {
        var displays=new List<(CaptureArea Area,bool Primary)>();
        for(uint i=0;i<64;i++) {
            var device=new DisplayDevice {Size=Marshal.SizeOf<DisplayDevice>()};
            if(!EnumDisplayDevices(null,i,ref device,0)) break;
            if((device.Flags&1)==0 || (device.Flags&8)!=0) continue; // Attached, non-mirroring adapter.
            var mode=new DisplayMode {Size=(ushort)Marshal.SizeOf<DisplayMode>()};
            // Unlike GetSystemMetrics, this API always returns physical pixels,
            // independently of the caller's DPI awareness (Microsoft API contract).
            if(!EnumDisplaySettingsEx(device.Name,-1,ref mode,0) ||
               (mode.Fields&0x180020u)!=0x180020u || mode.Width<2 || mode.Height<2 ||
               mode.Width>int.MaxValue || mode.Height>int.MaxValue)
                throw new InvalidOperationException("Physical display geometry unavailable");
            displays.Add((new(mode.X,mode.Y,(int)mode.Width,(int)mode.Height),(device.Flags&4)!=0));
        }
        return FromDisplays(displays);
    }
    [StructLayout(LayoutKind.Sequential)] struct Rect {internal int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct MonitorInfo {
        internal uint Size;internal Rect Monitor,Work;internal uint Flags;
    }
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] delegate bool MonitorCallback(nint monitor,nint dc,nint rect,nint data);
    [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(nint dc,nint clip,MonitorCallback callback,nint data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(nint monitor,ref MonitorInfo info);
    internal static DisplayGeometry ReadMonitors() {
        // Remote/virtual drivers can omit DEVMODE fields. Monitor rectangles are
        // an alternative, but only under physical DPI awareness; never use WPF
        // logical bounds as absolute-input or capture coordinates.
        using var dpi=new PhysicalDpiScope();
        var displays=new List<(CaptureArea Area,bool Primary)>();
        bool valid=true;
        MonitorCallback callback=(monitor,_,_,_)=> {
            var info=new MonitorInfo {Size=(uint)Marshal.SizeOf<MonitorInfo>()};
            if(!GetMonitorInfo(monitor,ref info)) {valid=false;return false;}
            long width=(long)info.Monitor.Right-info.Monitor.Left,height=(long)info.Monitor.Bottom-info.Monitor.Top;
            if(width<2 || height<2 || width>int.MaxValue || height>int.MaxValue) {valid=false;return false;}
            displays.Add((new(info.Monitor.Left,info.Monitor.Top,(int)width,(int)height),(info.Flags&1)!=0));
            return true;
        };
        if(!EnumDisplayMonitors(0,0,callback,0) || !valid) throw new InvalidOperationException("Physical display geometry unavailable");
        return FromDisplays(displays);
    }
    internal static DisplayGeometry FromDisplays(IEnumerable<(CaptureArea Area,bool Primary)> values) {
        var displays=values.ToArray();
        if(displays.Length==0 || displays.Count(d=>d.Primary)!=1 || displays.Any(d=>d.Area.Width<2 || d.Area.Height<2))
            throw new ArgumentException("Invalid display layout");
        int left=displays.Min(d=>d.Area.X),top=displays.Min(d=>d.Area.Y);
        long right=displays.Max(d=>(long)d.Area.X+d.Area.Width),bottom=displays.Max(d=>(long)d.Area.Y+d.Area.Height);
        if(right-left>int.MaxValue || bottom-top>int.MaxValue) throw new ArgumentException("Display layout too large");
        return new(displays.Single(d=>d.Primary).Area,new(left,top,(int)(right-left),(int)(bottom-top)));
    }
}

// Scope the synchronous native call, not just the geometry query: async host
// workers otherwise revert to WPF's system DPI context between packets.
internal readonly struct PhysicalDpiScope:IDisposable {
    [DllImport("user32.dll")] static extern nint SetThreadDpiAwarenessContext(nint context);
    readonly nint previous;
    public PhysicalDpiScope() {
        previous=SetThreadDpiAwarenessContext(-4); // PER_MONITOR_AWARE_V2; Windows 10/11.
        if(previous==0) throw new InvalidOperationException("Physical input DPI context unavailable");
    }
    public void Dispose() { if(previous!=0) SetThreadDpiAwarenessContext(previous); }
}

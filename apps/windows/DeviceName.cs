using System.IO;
using System.Text;
using System.Text.Json;
namespace LocalRemoteDesktop;

// A display label, never the identity used to look up a pairing credential.
internal sealed class DeviceName {
    static readonly Lazy<DeviceName> defaultName=new(()=>new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalRemote","device-name.txt"),Environment.MachineName));
    internal static DeviceName Default=>defaultName.Value;
    readonly object gate=new(); readonly string path; string? custom;
    internal string SystemName {get;}
    internal string Current {get {lock(gate) return custom??SystemName;}}
    internal static string? Valid(string? raw) {
        if(raw==null || raw.Length>128) return null;
        var value=raw.Trim(); var scalars=value.EnumerateRunes().ToArray();
        return scalars.Length is >=1 and <=32 && !scalars.Any(Rune.IsControl) && !scalars.Any(x=>x.Value==0xfffd)?value:null;
    }
    internal DeviceName(string path,string systemName) {
        this.path=path;
        var clean=new string(systemName.Where(c=>!char.IsControl(c)).ToArray()).Trim();
        var bounded=string.Concat(clean.EnumerateRunes().Take(32).Select(x=>x.ToString()));
        SystemName=Valid(bounded)??"Windows";
        try {if(new FileInfo(path).Length<=512) custom=Valid(File.ReadAllText(path,Encoding.UTF8));} catch(IOException) {} catch(UnauthorizedAccessException) {}
    }
    internal void Save(string draft) {
        var value=Valid(draft)??throw new ArgumentException("请输入 1–32 个字符的名称，不含控制字符。");
        lock(gate) {
            if(value==SystemName) {if(File.Exists(path)) File.Delete(path);custom=null;return;}
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {File.WriteAllText(temporary,value,new UTF8Encoding(false));File.Move(temporary,path,true);custom=value;}
            finally {if(File.Exists(temporary)) File.Delete(temporary);}
        }
    }
    internal static string Peer(JsonElement message,string fallback) {
        if(message.TryGetProperty("deviceName",out var name) && name.ValueKind==JsonValueKind.String)
            return Valid(name.GetString())??fallback;
        return fallback;
    }
}

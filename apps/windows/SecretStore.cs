using System.IO;
using System.Runtime.InteropServices;
namespace LocalRemoteDesktop;
// Current-user DPAPI; no plaintext keys on disk, no NuGet dependency.
static class SecretStore
{
    [StructLayout(LayoutKind.Sequential)] struct Blob { public int Length; public IntPtr Data; }
    [DllImport("crypt32.dll", SetLastError=true)] static extern bool CryptProtectData(ref Blob input, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out Blob output);
    [DllImport("crypt32.dll", SetLastError=true)] static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out Blob output);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr memory);
    static byte[] Transform(byte[] bytes, bool encrypt) {
        var input = new Blob { Length=bytes.Length, Data=Marshal.AllocHGlobal(bytes.Length) }; Blob output=default;
        try {
            Marshal.Copy(bytes, 0, input.Data, bytes.Length);
            bool ok = encrypt ? CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output) : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            var result=new byte[output.Length]; Marshal.Copy(output.Data, result, 0, result.Length); return result;
        } finally { Marshal.FreeHGlobal(input.Data); if(output.Data!=IntPtr.Zero) LocalFree(output.Data); }
    }
    internal static byte[] Protect(byte[] value)=>Transform(value,true);
    internal static byte[] Unprotect(byte[] value)=>Transform(value,false);
    internal static string HostCredentialPath()=>Path.Combine(DirectoryPath(),"host-credential.dpapi");
    static string DirectoryPath() {
        var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalRemote"); Directory.CreateDirectory(dir);
        MigrateLegacyDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RemoteMacLAN"),dir);
        return dir;
    }
    static readonly object migrationLock = new();
    // Copy opaque DPAPI blobs once. Never rotate keys, overwrite new settings,
    // delete the old store or resurrect forgotten data on later launches.
    internal static void MigrateLegacyDirectory(string legacy,string current) {
        lock(migrationLock) {
            Directory.CreateDirectory(current);
            var marker=Path.Combine(current,"legacy-migration-v1.complete");
            if(File.Exists(marker)) return;
            if(Directory.Exists(legacy)) {
                foreach(var path in Directory.EnumerateFiles(legacy,"*.dpapi",SearchOption.TopDirectoryOnly)) {
                    var name=Path.GetFileName(path);
                    if(name!="last-host.dpapi" && name!="windows-shortcuts.dpapi" &&
                       !System.Text.RegularExpressions.Regex.IsMatch(name,@"\A[0-9A-Fa-f]{64}\.dpapi\z")) continue;
                    var destination=Path.Combine(current,name);
                    if(!File.Exists(destination)) {
                        try { File.Copy(path,destination,false); }
                        catch(IOException e) when((e.HResult & 0xFFFF) is 80 or 183 && File.Exists(destination)) { /* Another process migrated it. */ }
                    }
                }
            }
            File.WriteAllText(marker,"LocalRemoteDesktop encrypted settings migration completed.\n");
        }
    }
    static string PathFor(string host,string? directory=null) => Path.Combine(directory??DirectoryPath(),Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(host)))+".dpapi");
    static void AtomicWrite(string path,byte[] value) {
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {File.WriteAllBytes(temporary,value);File.Move(temporary,path,true);}
        finally {if(File.Exists(temporary)) File.Delete(temporary);}
    }
    public static void Save(string host,string secret,string? directory=null) => AtomicWrite(PathFor(host,directory), Transform(System.Text.Encoding.UTF8.GetBytes(secret),true));
    public static string Load(string host,string? directory=null) { try { return System.Text.Encoding.UTF8.GetString(Transform(File.ReadAllBytes(PathFor(host,directory)),false)); } catch { return ""; } }
    public static void Remember(string host,string secret) {
        Save(host,secret);
        AtomicWrite(Path.Combine(DirectoryPath(),"last-host.dpapi"),Transform(System.Text.Encoding.UTF8.GetBytes(host),true));
    }
    public static string LastHost() { try { return System.Text.Encoding.UTF8.GetString(Transform(File.ReadAllBytes(Path.Combine(DirectoryPath(),"last-host.dpapi")),false)); } catch { return ""; } }
    public static void SaveWindowsShortcuts(bool enabled,string? preferencePath=null) => File.WriteAllBytes(preferencePath??Path.Combine(DirectoryPath(),"windows-shortcuts.dpapi"),Transform(new byte[]{enabled?(byte)1:(byte)0},true));
    public static bool LoadWindowsShortcuts(string? preferencePath=null) { try { var data=Transform(File.ReadAllBytes(preferencePath??Path.Combine(DirectoryPath(),"windows-shortcuts.dpapi")),false); return data.Length!=1 || data[0]!=0; } catch { return true; } }
    public static void SaveFileSync(bool enabled,string? preferencePath=null) => File.WriteAllBytes(preferencePath??Path.Combine(DirectoryPath(),"file-clipboard.dpapi"),Transform(new byte[]{enabled?(byte)1:(byte)0},true));
    static bool LoadSharing(string name,string? preferencePath) {
        try { var data=Transform(File.ReadAllBytes(preferencePath??Path.Combine(DirectoryPath(),name)),false); return data.Length==1 && data[0]==1; }
        catch(FileNotFoundException) { return true; } catch(DirectoryNotFoundException) { return true; } catch { return false; }
    }
    public static bool LoadFileSync(string? preferencePath=null)=>LoadSharing("file-clipboard.dpapi",preferencePath);
    public static void SaveClipboardSync(bool enabled,string? preferencePath=null) => File.WriteAllBytes(preferencePath??Path.Combine(DirectoryPath(),"text-clipboard.dpapi"),Transform(new byte[]{enabled?(byte)1:(byte)0},true));
    public static bool LoadClipboardSync(string? preferencePath=null)=>LoadSharing("text-clipboard.dpapi",preferencePath);
    public static void SaveConnectionQuality(int value,string? preferencePath=null) {
        if(value is <0 or >2) throw new ArgumentOutOfRangeException(nameof(value));
        File.WriteAllBytes(preferencePath??Path.Combine(DirectoryPath(),"connection-quality.dpapi"),Transform(new[]{(byte)value},true));
    }
    public static int LoadConnectionQuality(string? preferencePath=null) {
        try { var value=Transform(File.ReadAllBytes(preferencePath??Path.Combine(DirectoryPath(),"connection-quality.dpapi")),false); return value.Length==1 && value[0]<=2?value[0]:0; } catch { return 0; }
    }
    public static void SaveAutoReconnect(bool enabled,string? preferencePath=null)=>File.WriteAllBytes(preferencePath??Path.Combine(DirectoryPath(),"connection-reconnect.dpapi"),Transform(new[]{enabled?(byte)1:(byte)0},true));
    public static bool LoadAutoReconnect(string? preferencePath=null)=>LoadSharing("connection-reconnect.dpapi",preferencePath);
    public static void Forget(string host,string? directory=null) {
        File.Delete(PathFor(host,directory));
    }
}

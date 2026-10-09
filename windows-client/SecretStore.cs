using System.IO;
using System.Runtime.InteropServices;
namespace RemoteMacLAN;
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
    static string DirectoryPath() {
        var dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RemoteMacLAN"); Directory.CreateDirectory(dir);
        return dir;
    }
    static string PathFor(string host) => Path.Combine(DirectoryPath(),Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(host)))+".dpapi");
    public static void Save(string host,string secret) => File.WriteAllBytes(PathFor(host), Transform(System.Text.Encoding.UTF8.GetBytes(secret),true));
    public static string Load(string host) { try { return System.Text.Encoding.UTF8.GetString(Transform(File.ReadAllBytes(PathFor(host)),false)); } catch { return ""; } }
    public static void Remember(string host,string secret) {
        Save(host,secret);
        File.WriteAllBytes(Path.Combine(DirectoryPath(),"last-host.dpapi"),Transform(System.Text.Encoding.UTF8.GetBytes(host),true));
    }
    public static string LastHost() { try { return System.Text.Encoding.UTF8.GetString(Transform(File.ReadAllBytes(Path.Combine(DirectoryPath(),"last-host.dpapi")),false)); } catch { return ""; } }
    public static void SaveWindowsShortcuts(bool enabled,string? preferencePath=null) => File.WriteAllBytes(preferencePath??Path.Combine(DirectoryPath(),"windows-shortcuts.dpapi"),Transform(new byte[]{enabled?(byte)1:(byte)0},true));
    public static bool LoadWindowsShortcuts(string? preferencePath=null) { try { var data=Transform(File.ReadAllBytes(preferencePath??Path.Combine(DirectoryPath(),"windows-shortcuts.dpapi")),false); return data.Length!=1 || data[0]!=0; } catch { return true; } }
    public static void Forget(string host) {
        File.Delete(PathFor(host));
        if (LastHost()==host) File.Delete(Path.Combine(DirectoryPath(),"last-host.dpapi"));
    }
}

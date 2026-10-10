using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.IO;

// Both real assemblies; all writes use explicit fixture-path parameters.
// APPDATA overrides do not redirect Windows known folders reliably. Never call
// default Remember/Forget/HostCredential.Load/UiText.Use(persist:true) here.
var old=Assembly.LoadFrom(Environment.GetEnvironmentVariable("LOCALREMOTE_PREVIOUS_ASSEMBLY")??throw new Exception("Previous assembly required"));
var current=Assembly.LoadFrom(Environment.GetEnvironmentVariable("LOCALREMOTE_CURRENT_ASSEMBLY")??throw new Exception("Current assembly required"));
var root=Path.GetFullPath(Environment.GetEnvironmentVariable("LOCALREMOTE_NAMING_FIXTURE")??throw new Exception("Fixture path required"));
if(!root.Contains("product-rename-compat-",StringComparison.OrdinalIgnoreCase)) throw new Exception("An isolated fixture is required");
Directory.CreateDirectory(root);
if(old.GetName().Name!="LocalRemote" || current.GetName().Name!="LocalRemoteDesktop" || current.GetCustomAttribute<AssemblyProductAttribute>()?.Product!="Local Remote Desktop") throw new Exception("Wrong product assemblies");
const BindingFlags Flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
Type Type(Assembly a,string name)=>a.GetType(a.GetName().Name+"."+name,true)!;
object? Call(Assembly a,string type,string method,params object?[] args)=>Type(a,type).GetMethod(method,Flags)!.Invoke(null,args);
void Require(bool ok,string message) { if(!ok) throw new Exception(message); }
const string host="192.0.2.11",password="Migration-fixture#42";
foreach(var text in new[]{host,password}) {
    var encrypted=(byte[])Call(old,"SecretStore","Protect",Encoding.UTF8.GetBytes(text))!;
    Require(Encoding.UTF8.GetString((byte[])Call(current,"SecretStore","Unprotect",encrypted)!)==text,"Old DPAPI content incompatible");
}
foreach(var pair in new[]{("SaveWindowsShortcuts","LoadWindowsShortcuts"),("SaveClipboardSync","LoadClipboardSync"),("SaveFileSync","LoadFileSync")}) {
    var path=Path.Combine(root,pair.Item1+".dpapi"); Call(old,"SecretStore",pair.Item1,false,path);
    Require(!(bool)Call(current,"SecretStore",pair.Item2,path)!,"Old preference incompatible");
}
var file=Path.Combine(root,"sharing.dpapi");
var oldCredential=Call(old,"HostCredential","Load",file)!;
var oldBlob=File.ReadAllBytes(file); var restored=Call(current,"HostCredential","Load",file)!;
Require((string)oldCredential.GetType().GetProperty("Password")!.GetValue(oldCredential)! ==(string)restored.GetType().GetProperty("Password")!.GetValue(restored)!,"Sharing password rotated");
Require(((byte[])oldCredential.GetType().GetProperty("Salt")!.GetValue(oldCredential)!).SequenceEqual((byte[])restored.GetType().GetProperty("Salt")!.GetValue(restored)!),"Sharing salt changed");
Require(oldBlob.SequenceEqual(File.ReadAllBytes(file)),"Existing sharing credential rewritten");
Require((string)Type(old,"UiText").GetProperty("Preference",Flags)!.GetValue(null)! ==(string)Type(current,"UiText").GetProperty("Preference",Flags)!.GetValue(null)!,"Language preference path changed");
var legacy=Path.Combine(root,"RemoteMacLAN");var store=Path.Combine(root,"LocalRemote"); Directory.CreateDirectory(legacy);
var name=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(host)))+".dpapi";
var blob=(byte[])Call(old,"SecretStore","Protect",Encoding.UTF8.GetBytes(password))!;
File.WriteAllBytes(Path.Combine(legacy,name),blob);
Call(old,"SecretStore","MigrateLegacyDirectory",legacy,store);
Require(blob.SequenceEqual(File.ReadAllBytes(Path.Combine(store,name))),"Old migration changed encrypted blob");
File.Delete(Path.Combine(store,name));
Call(current,"SecretStore","MigrateLegacyDirectory",legacy,store);
Require(!File.Exists(Path.Combine(store,name)),"Completed migration resurrected forgotten credential");
Console.WriteLine("PASS actual previous and renamed assemblies: product identity, old DPAPI target/password data, keyboard/clipboard preferences, sharing password/salt/blob unchanged, retained language path and no credential resurrection (explicit fixture paths)");

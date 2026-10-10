using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
namespace LocalRemote;

internal sealed record HostCredential(string Password,byte[] Salt) {
    const string Alphabet="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    internal static string Generate()=>new(Enumerable.Range(0,12).Select(_=>Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());
    internal static HostCredential Create(string password) { PairingPassword.Validate(password); return new(password,RandomNumberGenerator.GetBytes(16)); }
    internal static HostCredential Load(string? path=null) {
        var file=path??SecretStore.HostCredentialPath();
        if(!File.Exists(file)) { var generated=Create(Generate()); generated.Save(file); return generated; }
        var data=SecretStore.Unprotect(File.ReadAllBytes(file));
        try {
            var value=JsonSerializer.Deserialize<HostCredential>(data)??throw new IOException("Invalid sharing credential");
            PairingPassword.Validate(value.Password); if(value.Salt.Length!=16) throw new IOException("Invalid sharing credential salt"); return value;
        } finally { CryptographicOperations.ZeroMemory(data); }
    }
    internal void Save(string? path=null) {
        PairingPassword.Validate(Password); if(Salt.Length!=16) throw new IOException("Invalid sharing salt");
        var data=JsonSerializer.SerializeToUtf8Bytes(this); byte[] encrypted;
        try { encrypted=SecretStore.Protect(data); } finally { CryptographicOperations.ZeroMemory(data); }
        var file=path??SecretStore.HostCredentialPath(); var temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
        try { File.WriteAllBytes(temporary,encrypted); File.Move(temporary,file,true); }
        finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
}

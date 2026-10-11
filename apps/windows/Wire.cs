using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace LocalRemoteDesktop;

public static class PairingPassword {
    public const int Rounds=600000;
    public static bool IsLegacy(string value)=>value.Length==64 && value.All(Uri.IsHexDigit);
    public static void Validate(string value) {
        if(value.Length<8 || value.Length>64 || value.Any(c=>c<=32 || c>=127))
            throw new ArgumentException("配对密码需为 8–64 位英文字母、数字或符号，不含空格；旧的 64 位密钥也可用。");
    }
    public static void ValidateNew(string value) {
        Validate(value);
        if(value.Length!=8) throw new ArgumentException("新密码需为 8 位英文字母、数字或符号，不含空格。");
    }
    public static byte[] Derive(string password,byte[] salt) {
        Validate(password); if(salt.Length!=16) throw new ArgumentException("Invalid pairing salt");
        return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password),salt,Rounds,HashAlgorithmName.SHA256,32);
    }
}
// All writes pass one gate; counters are implicit, monotonic, and direction-specific.
public sealed class Wire : IDisposable
{
    readonly TcpClient tcp;
    readonly SemaphoreSlim gate = new(1);
    NetworkStream stream = null!;
    byte[] tx = [], rx = [];
    ulong sent, received;
    readonly int maximumRead;
    readonly int ioTimeoutSeconds;
    readonly int maximumWrite;
    public Wire(int maximumRead=8*1024*1024,int ioTimeoutSeconds=0):this(new TcpClient(),maximumRead,ioTimeoutSeconds,65536) {}
    Wire(TcpClient tcp,int maximumRead,int ioTimeoutSeconds,int maximumWrite) {
        this.tcp=tcp; tcp.NoDelay=true; this.maximumRead=maximumRead;
        this.ioTimeoutSeconds=ioTimeoutSeconds; this.maximumWrite=maximumWrite;
    }
    // A caller must reserve its one-controller slot and filter the peer before
    // accepting. Capture/input start only after this mutual proof succeeds.
    internal static async Task<Wire> Accept(TcpClient peer,byte[] key,byte[]? salt,CancellationToken ct,bool fileChannel=false) {
        if(key.Length!=32 || (salt!=null && salt.Length!=16)) { peer.Dispose(); throw new ArgumentException("Invalid host credential"); }
        var wire=new Wire(peer,65536,fileChannel?15:10,fileChannel?65536:8*1024*1024);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(8));
        try {
            wire.stream=peer.GetStream(); var server=RandomNumberGenerator.GetBytes(32);
            await wire.stream.WriteAsync(salt==null?Join("RML1"u8.ToArray(),server):Join("RMP1"u8.ToArray(),server,salt),deadline.Token);
            var response=new byte[64]; await wire.stream.ReadExactlyAsync(response,deadline.Token);
            var transcript=salt==null?Join(server,response[..32]):Join("RMP1"u8.ToArray(),salt,server,response[..32]);
            if(!CryptographicOperations.FixedTimeEquals(response[32..],Mac(key,Join(Label("client"),transcript))))
                throw new CryptographicException("Controller authentication failed");
            await wire.stream.WriteAsync(Mac(key,Join(Label("server"),transcript)),deadline.Token);
            wire.tx=Mac(key,Join(Label("h2c"),transcript)); wire.rx=Mac(key,Join(Label("c2h"),transcript));
            return wire;
        } catch { wire.Dispose(); throw; }
    }
    CancellationTokenSource Timeout(CancellationToken ct) {
        var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);
        if(ioTimeoutSeconds>0) timeout.CancelAfter(TimeSpan.FromSeconds(ioTimeoutSeconds));
        return timeout;
    }
    public static byte[] Mac(byte[] key, byte[] data) => HMACSHA256.HashData(key, data);
    static byte[] Join(params byte[][] arrays) => arrays.SelectMany(a => a).ToArray();
    static byte[] Label(string label) => Encoding.ASCII.GetBytes(label);
    public async Task Connect(string ip, string secret, CancellationToken ct,int port=47980)
    {
        using var timeout=Timeout(ct); ct=timeout.Token;
        secret=secret.Trim(); PairingPassword.Validate(secret);
        await tcp.ConnectAsync(ip, port, ct).ConfigureAwait(false); stream = tcp.GetStream();
        var hello = new byte[36]; await stream.ReadExactlyAsync(hello, ct).ConfigureAwait(false);
        bool passwordMode=hello.AsSpan(0,4).SequenceEqual("RMP1"u8);
        if(!passwordMode && !hello.AsSpan(0,4).SequenceEqual("RML1"u8)) throw new NotSupportedException("不兼容的 Host 协议。");
        var salt=new byte[16];
        if(passwordMode) await stream.ReadExactlyAsync(salt,ct).ConfigureAwait(false);
        else if(!PairingPassword.IsLegacy(secret)) throw new ArgumentException("此 Mac 版本仍使用旧密钥；请更新 Mac 后设置密码。");
        var key=passwordMode ? await Task.Run(()=>PairingPassword.Derive(secret,salt),ct).ConfigureAwait(false) : Convert.FromHexString(secret);
        try {
            var server=hello[4..]; var client=RandomNumberGenerator.GetBytes(32);
            var transcript=passwordMode?Join("RMP1"u8.ToArray(),salt,server,client):Join(server,client);
            await stream.WriteAsync(Join(client,Mac(key,Join(Label("client"),transcript))),ct).ConfigureAwait(false);
            var proof=new byte[32];
            try { await stream.ReadExactlyAsync(proof,ct).ConfigureAwait(false); }
            catch(IOException error) { throw new CryptographicException("目标设备身份验证未完成，请检查配对密码后重新连接。",error); }
            if(!CryptographicOperations.FixedTimeEquals(proof,Mac(key,Join(Label("server"),transcript)))) throw new CryptographicException("目标设备身份验证未完成，请检查配对密码后重新连接。");
            tx=Mac(key,Join(Label("c2h"),transcript)); rx=Mac(key,Join(Label("h2c"),transcript));
        } finally { CryptographicOperations.ZeroMemory(key); }
    }

    static byte[] Nonce(ulong n) { var b = new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(4), n); return b; }
    public async Task Send(byte type, byte[] payload, CancellationToken ct)
    {
        using var timeout=Timeout(ct); ct=timeout.Token;
        if(payload.Length>maximumWrite-17) throw new IOException("Outgoing record too large");
        await gate.WaitAsync(ct);
        try {
            var plain = Join([type], payload); var cipher = new byte[plain.Length]; var tag = new byte[16];
            var header = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(header, (uint)(plain.Length + 16));
            using var aes = new AesGcm(tx, 16); aes.Encrypt(Nonce(sent++), plain, cipher, tag, header);
            await stream.WriteAsync(Join(header, cipher, tag), ct);
        } finally { gate.Release(); }
    }
    public Task Json(object value, CancellationToken ct) => Send(1, JsonSerializer.SerializeToUtf8Bytes(value), ct);
    public async Task<(byte type, byte[] data)> Read(CancellationToken ct)
    {
        using var timeout=Timeout(ct); ct=timeout.Token;
        var header = new byte[4]; await stream.ReadExactlyAsync(header, ct);
        var size = BinaryPrimitives.ReadUInt32BigEndian(header);
        if (size < 17 || size > maximumRead) throw new IOException("Invalid frame size");
        var body = new byte[(int)size]; await stream.ReadExactlyAsync(body, ct); var plain = new byte[body.Length - 16];
        using var aes = new AesGcm(rx, 16); aes.Decrypt(Nonce(received++), body.AsSpan(0, plain.Length), body.AsSpan(plain.Length), plain, header);
        return (plain[0], plain[1..]);
    }
    public void Dispose() { tcp.Dispose(); if (tx.Length > 0) CryptographicOperations.ZeroMemory(tx); if (rx.Length > 0) CryptographicOperations.ZeroMemory(rx); }
}

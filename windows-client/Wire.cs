using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace RemoteMacLAN;

// All writes pass one gate; counters are implicit, monotonic, and direction-specific.
public sealed class Wire : IDisposable
{
    readonly TcpClient tcp = new() { NoDelay = true };
    readonly SemaphoreSlim gate = new(1);
    NetworkStream stream = null!;
    byte[] tx = [], rx = [];
    ulong sent, received;
    public static byte[] Mac(byte[] key, byte[] data) => HMACSHA256.HashData(key, data);
    static byte[] Join(params byte[][] arrays) => arrays.SelectMany(a => a).ToArray();
    static byte[] Label(string label) => Encoding.ASCII.GetBytes(label);
    public async Task Connect(string ip, string secret, CancellationToken ct)
    {
        var key = Convert.FromHexString(secret.Trim());
        if (key.Length != 32) throw new ArgumentException("配对密钥必须为 64 个十六进制字符。");
        await tcp.ConnectAsync(ip, 47980, ct); stream = tcp.GetStream();
        var hello = new byte[36]; await stream.ReadExactlyAsync(hello, ct);
        if (!hello.AsSpan(0, 4).SequenceEqual("RML1"u8)) throw new IOException("不兼容的 Host 协议。");
        var server = hello[4..]; var client = RandomNumberGenerator.GetBytes(32);
        var transcript = Join(server, client);
        await stream.WriteAsync(Join(client, Mac(key, Join(Label("client"), transcript))), ct);
        var proof = new byte[32]; await stream.ReadExactlyAsync(proof, ct);
        if (!CryptographicOperations.FixedTimeEquals(proof, Mac(key, Join(Label("server"), transcript)))) throw new CryptographicException("Host 身份验证失败。");
        tx = Mac(key, Join(Label("c2h"), transcript)); rx = Mac(key, Join(Label("h2c"), transcript));
        CryptographicOperations.ZeroMemory(key);
    }
    static byte[] Nonce(ulong n) { var b = new byte[12]; BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(4), n); return b; }
    public async Task Send(byte type, byte[] payload, CancellationToken ct)
    {
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
        var header = new byte[4]; await stream.ReadExactlyAsync(header, ct);
        var size = BinaryPrimitives.ReadUInt32BigEndian(header);
        if (size < 17 || size > 8 * 1024 * 1024) throw new IOException("Invalid frame size");
        var body = new byte[(int)size]; await stream.ReadExactlyAsync(body, ct); var plain = new byte[body.Length - 16];
        using var aes = new AesGcm(rx, 16); aes.Decrypt(Nonce(received++), body.AsSpan(0, plain.Length), body.AsSpan(plain.Length), plain, header);
        return (plain[0], plain[1..]);
    }
    public void Dispose() { tcp.Dispose(); if (tx.Length > 0) CryptographicOperations.ZeroMemory(tx); if (rx.Length > 0) CryptographicOperations.ZeroMemory(rx); }
}

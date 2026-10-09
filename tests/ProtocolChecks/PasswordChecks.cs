using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using LocalRemote;
static class PasswordChecks {
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }
    static byte[] Join(params byte[][] values)=>values.SelectMany(x=>x).ToArray();
    static byte[] Proof(byte[] key,string label,byte[] transcript)=>HMACSHA256.HashData(key,Join(Encoding.ASCII.GetBytes(label),transcript));
    public static async Task Run() {
        byte[] salt=Enumerable.Range(0,16).Select(x=>(byte)x).ToArray();
        byte[] known=Convert.FromHexString("0dca4c657c16ea4e595de60ab5d6bad618ea62de9184dc41c08e40051905d87c");
        Require(PairingPassword.Derive("Office-LAN#42",salt).SequenceEqual(known),"PBKDF2 independent vector mismatch");
        foreach(string invalid in new[]{"1234567",new string('a',65),"hello world","中文password","abc\0defgh"}) {
            bool rejected=false; try { PairingPassword.Validate(invalid); } catch(ArgumentException) { rejected=true; }
            Require(rejected,"invalid password accepted");
        }
        foreach(string mode in new[]{"valid","wrong-password","bad-server-proof","changed-salt"}) {
            var listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try {
                var host=Task.Run(async()=> {
                    using var peer=await listener.AcceptTcpClientAsync(timeout.Token); var stream=peer.GetStream();
                    var nonce=RandomNumberGenerator.GetBytes(32); var advertised=salt.ToArray(); if(mode=="changed-salt") advertised[0]^=1;
                    await stream.WriteAsync(Join("RMP1"u8.ToArray(),nonce,advertised),timeout.Token);
                    var reply=new byte[64]; await stream.ReadExactlyAsync(reply,timeout.Token);
                    var transcript=Join("RMP1"u8.ToArray(),salt,nonce,reply[..32]);
                    if(mode is "wrong-password" or "changed-salt") { Require(!CryptographicOperations.FixedTimeEquals(reply[32..],Proof(known,"client",transcript)),"invalid proof accepted"); return; }
                    Require(CryptographicOperations.FixedTimeEquals(reply[32..],Proof(known,"client",transcript)),"valid password proof rejected");
                    var proof=Proof(known,"server",transcript); if(mode=="bad-server-proof") proof[0]^=1;
                    await stream.WriteAsync(proof,timeout.Token);
                });
                using var wire=new Wire(65536,5); bool rejected=false;
                try { await wire.Connect("127.0.0.1",mode=="wrong-password"?"Office-LAN#43":"Office-LAN#42",timeout.Token,((IPEndPoint)listener.LocalEndpoint).Port); }
                catch(Exception ex) when(ex is IOException or CryptographicException) { rejected=true; }
                await host;
                Require(rejected==(mode!="valid"),"unexpected password outcome "+mode);
            } finally { listener.Stop(); }
        }
        Console.WriteLine("PASS short password: independent PBKDF2 vector, policy, RMP1 mutual proof, wrong password/server proof/salt rejection");
    }
}

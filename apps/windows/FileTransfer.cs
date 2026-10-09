using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace LocalRemote;

public record FileItem(string name,long size);
public sealed class FileSource {
    public const int ChunkSize=32768, MaxFiles=32;
    public const long MaxFile=4L*1024*1024*1024, MaxBatch=8L*1024*1024*1024;
    public string[] Paths {get;}
    public FileItem[] Items {get;}
    FileSource(string[] paths,FileItem[] items) { Paths=paths; Items=items; }
    public static bool SafeName(string name) {
        if(string.IsNullOrWhiteSpace(name) || Encoding.UTF8.GetByteCount(name)>240 || name!=name.Normalize(NormalizationForm.FormC)
           || name.EndsWith('.') || name.EndsWith(' ') || name is "." or ".." || name.Any(c=>c<32 || "<>:\"/\\|?*".Contains(c))) return false;
        var stem=name.Split('.')[0].Trim().ToUpperInvariant();
        return stem is not ("CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" or "CLOCK$") && !(stem.Length==4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && "123456789¹²³".Contains(stem[3]));
    }
    public static FileItem[] Validate(FileItem[] items) {
        if(items.Length==0 || items.Length>MaxFiles) throw new IOException("Select 1–32 regular files");
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase); long total=0;
        foreach(var item in items) {
            if(!SafeName(item.name) || !names.Add(item.name) || item.size<0 || item.size>MaxFile) throw new IOException("Unsupported file name/size or duplicate name");
            total=checked(total+item.size);
        }
        if(total>MaxBatch) throw new IOException("File batch exceeds 8 GiB");
        return items;
    }
    public static FileSource Prepare(IEnumerable<string> paths) {
        var list=paths.Take(MaxFiles+1).Select(Path.GetFullPath).ToArray();
        var items=list.Select(path=> {
            var info=new FileInfo(path);
            if(!info.Exists || (info.Attributes&(FileAttributes.Directory|FileAttributes.ReparsePoint))!=0) throw new IOException("Only regular files are supported");
            return new FileItem(info.Name.Normalize(NormalizationForm.FormC),info.Length);
        }).ToArray();
        return new FileSource(list,Validate(items));
    }
}
// Files are never addressed by a peer-supplied path. A unique, locally generated
// private staging directory is promoted only after all byte counts/hashes match.
public sealed class FileSink : IDisposable {
    readonly FileItem[] items;
    readonly string final;
    string? stage;
    FileStream? output;
    IncrementalHash? digest;
    long received;
    int next;
    public FileSink(string root,FileItem[] items) {
        this.items=FileSource.Validate(items); root=Path.GetFullPath(root);
        Directory.CreateDirectory(root);
        if(new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace < items.Sum(x=>x.size)+16*1024*1024) throw new IOException("Not enough free space for file batch");
        string id=Guid.NewGuid().ToString("N"); stage=Path.Combine(root,".incoming-"+id); final=Path.Combine(root,id);
        Directory.CreateDirectory(stage);
    }
    public void Start(int index) {
        if(stage==null || output!=null || index!=next || index>=items.Length) throw new IOException("Unexpected file index");
        output=new FileStream(Path.Combine(stage,items[index].name),FileMode.CreateNew,FileAccess.Write,FileShare.None,FileSource.ChunkSize);
        digest=IncrementalHash.CreateHash(HashAlgorithmName.SHA256); received=0;
    }
    public void Write(byte[] data) {
        if(output==null || data.Length==0 || data.Length>FileSource.ChunkSize || received+data.Length>items[next].size) throw new IOException("Invalid file chunk/length");
        output.Write(data); digest!.AppendData(data); received+=data.Length;
    }
    public void End(int index,string hash) {
        if(output==null || index!=next || received!=items[next].size || hash.Length!=64) throw new IOException("Incomplete file");
        byte[] expected; try { expected=Convert.FromHexString(hash); } catch(FormatException) { throw new IOException("Invalid file hash"); }
        if(!CryptographicOperations.FixedTimeEquals(digest!.GetHashAndReset(),expected)) throw new IOException("File hash mismatch");
        output.Dispose(); output=null; digest.Dispose(); digest=null; next++;
    }
    public string[] Commit() {
        if(stage==null || output!=null || next!=items.Length) throw new IOException("Incomplete batch");
        Directory.Move(stage,final); stage=null;
        return items.Select(x=>Path.Combine(final,x.name)).ToArray();
    }
    public void Dispose() { output?.Dispose(); digest?.Dispose(); if(stage!=null) { Directory.Delete(stage,true); stage=null; } }
}
// Cumulative pacing accounts for time already spent on disk/network work. A small
// burst avoids per-chunk timer overhead without creating an unbounded send queue.
public static class FilePacing {
    public const long Rate=8L*1024*1024, Burst=512L*1024;
    public static double DelaySeconds(long bytes,double elapsed)=>Math.Max(0,(bytes-Burst)/(double)Rate-Math.Max(0,elapsed));
}
public static class FileTransfer {
    public static string ProgressText(string direction,long done,long total,double elapsed) {
        if(done>=total) return "文件传输：正在校验并准备剪贴板…";
        int percent=(int)Math.Clamp(done*100.0/Math.Max(1,total),0,99);
        string speed=elapsed>=0.2 && done>0?$" · {done/1048576.0/elapsed:F1} MiB/s":"";
        return $"{direction}文件：{percent}% · {done/1048576.0:F1} / {total/1048576.0:F1} MiB{speed}";
    }
    static async Task<Wire> Connect(string ip,string key,CancellationToken ct,int port) {
        // Replacing a copy cancels the previous socket. Give its Host worker a
        // brief chance to finish cleanup before accepting the new connection.
        for(int attempt=0;;attempt++) {
            var wire=new Wire(65536,15);
            try { await wire.Connect(ip,key,ct,port); return wire; }
            catch(Exception ex) when(ex is IOException or System.Net.Sockets.SocketException) {
                wire.Dispose(); if(attempt>=3) throw;
                await Task.Delay(100*(attempt+1),ct);
            } catch { wire.Dispose(); throw; }
        }
    }
    public static async Task<JsonElement> Json(Wire wire,CancellationToken ct) {
        var packet=await wire.Read(ct);
        if(packet.type!=1) throw new IOException("Expected file control record");
        using var json=JsonDocument.Parse(packet.data); return json.RootElement.Clone();
    }
    public static void Expect(JsonElement message,string op) {
        if(message.TryGetProperty("op",out var error) && error.GetString()=="file_error") {
            var code=message.TryGetProperty("code",out var reason)?reason.GetString():null;
            throw new IOException(code switch {
                "invalid"=>"文件名、类型、数量或大小不支持", "changed"=>"复制后来源文件已变化", "checksum"=>"文件完整性校验失败",
                "incomplete"=>"文件接收不完整", "denied"=>"Mac 未允许文件共享或连接已失效", "diskFull"=>"Mac 磁盘空间不足", _=>"Mac 文件传输失败"
            });
        }
        if(!message.TryGetProperty("op",out var value) || value.GetString()!=op) throw new IOException("File transfer rejected or interrupted");
    }
    public static async Task Send(Wire wire,FileSource source,Action<long,long>? progress,CancellationToken ct) {
        long total=source.Items.Sum(x=>x.size), done=0; var clock=Stopwatch.StartNew();
        progress?.Invoke(0,total);
        for(int i=0;i<source.Items.Length;i++) {
            var current=FileSource.Prepare([source.Paths[i]]).Items[0];
            if(current!=source.Items[i]) throw new IOException("Source changed since copy");
            await wire.Json(new {op="file_start",index=i},ct);
            using var input=new FileStream(source.Paths[i],FileMode.Open,FileAccess.Read,FileShare.Read,FileSource.ChunkSize,true);
            using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer=new byte[FileSource.ChunkSize]; long count=0;
            while(true) {
                int length=await input.ReadAsync(buffer,ct); if(length==0) break;
                count+=length; if(count>source.Items[i].size) throw new IOException("Source size changed");
                hash.AppendData(buffer,0,length);
                double delay=FilePacing.DelaySeconds(done+length,clock.Elapsed.TotalSeconds);
                if(delay>=0.001) await Task.Delay(TimeSpan.FromSeconds(delay),ct);
                await wire.Send(3,length==buffer.Length?buffer:buffer[..length],ct);
                done+=length; progress?.Invoke(done,total);

            }
            if(count!=source.Items[i].size) throw new IOException("Source size changed");
            await wire.Json(new {op="file_end",index=i,sha256=Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()},ct);
        }
        await wire.Json(new {op="file_complete"},ct);
    }
    public static async Task<string[]> Receive(Wire wire,FileItem[] items,string root,Action<long,long>? progress,CancellationToken ct) {
        using var sink=new FileSink(root,items); long total=items.Sum(x=>x.size),done=0;
        progress?.Invoke(0,total);
        for(int i=0;i<items.Length;i++) {
            var start=await Json(wire,ct); Expect(start,"file_start"); sink.Start(start.GetProperty("index").GetInt32());
            while(true) {
                var packet=await wire.Read(ct);
                if(packet.type==3) { sink.Write(packet.data); done+=packet.data.Length; progress?.Invoke(done,total); }
                else {
                    if(packet.type!=1) throw new IOException("Invalid file record");
                    using var json=JsonDocument.Parse(packet.data); var end=json.RootElement; Expect(end,"file_end");
                    sink.End(end.GetProperty("index").GetInt32(),end.GetProperty("sha256").GetString()!); break;
                }
            }
        }
        Expect(await Json(wire,ct),"file_complete"); return sink.Commit();
    }
    public static async Task<bool> Upload(string ip,string key,string token,long requestId,FileSource source,Action<long,long>? progress,CancellationToken ct,int port=47981) {
        using var wire=await Connect(ip,key,ct,port);
        await wire.Json(new {op="file_push",session=token,requestId,items=source.Items},ct); Expect(await Json(wire,ct),"file_ready");
        await Send(wire,source,progress,ct); var done=await Json(wire,ct); Expect(done,"file_done");
        return done.GetProperty("clipboardReady").GetBoolean();
    }
    public static async Task<string[]> Download(string ip,string key,string token,long requestId,string offer,FileItem[] items,string root,Action<long,long>? progress,CancellationToken ct,int port=47981,Func<string[],CancellationToken,Task<bool>>? publish=null) {
        using var wire=await Connect(ip,key,ct,port);
        await wire.Json(new {op="file_pull",session=token,requestId,offer},ct); var ready=await Json(wire,ct); Expect(ready,"file_ready");
        var declared=FileSource.Validate(ready.GetProperty("items").Deserialize<FileItem[]>()!);
        if(!declared.SequenceEqual(items)) throw new IOException("File offer changed");
        var paths=await Receive(wire,declared,root,progress,ct);
        bool clipboardReady=publish!=null && await publish(paths,ct);
        await wire.Json(new {op="file_done",clipboardReady},ct); return paths;
    }
}

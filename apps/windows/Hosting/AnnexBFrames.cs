using System.IO;
namespace LocalRemoteDesktop;

// The encoder inserts AUDs. Emit one bounded access unit, not arbitrary pipe reads.
internal sealed class AnnexBFrames {
    const int Limit=8*1024*1024-17;
    byte[] buffer=new byte[65536]; int size,scan; bool hasAud;
    internal async Task Feed(ReadOnlyMemory<byte> bytes,Func<byte[],Task> emit) {
        if(bytes.Length>Limit-size) throw new IOException("H264 access unit exceeds bound");
        if(size+bytes.Length>buffer.Length) Array.Resize(ref buffer,Math.Min(Limit,Math.Max(size+bytes.Length,buffer.Length*2)));
        bytes.Span.CopyTo(buffer.AsSpan(size)); size+=bytes.Length;
        while(scan+4<=size) {
            int start=scan, prefix=buffer[scan]==0 && buffer[scan+1]==0 ? buffer[scan+2]==1?3:buffer[scan+2]==0 && buffer[scan+3]==1?4:0:0;
            if(prefix==0) { scan++; continue; }
            if(start+prefix>=size) break;
            if((buffer[start+prefix]&31)==9) {
                if(hasAud && start>0) {
                    await emit(buffer.AsSpan(0,start).ToArray());
                    Buffer.BlockCopy(buffer,start,buffer,0,size-start); size-=start; scan=0;
                }
                hasAud=true;
            }
            scan+=prefix+1;
        }
    }
    internal async Task Finish(Func<byte[],Task> emit) {
        if(size>0) { if(!hasAud) throw new IOException("Encoder did not emit H264 AUDs"); await emit(buffer.AsSpan(0,size).ToArray()); }
        size=scan=0; hasAud=false;
    }
}

using System.Runtime.InteropServices;
using System.Windows;
namespace LocalRemoteDesktop;
public sealed class WindowsClipboard : ITextClipboard {
    [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
    public long Revision=>GetClipboardSequenceNumber();
    public bool TryRead(out string? text) {
        text=null;
        try { var value=Clipboard.GetDataObject(); if(value?.GetDataPresent(DataFormats.FileDrop,false)!=true) text=value?.GetData(DataFormats.UnicodeText,false) as string; return true; }
        catch(ExternalException) { return false; }
    }
    public bool TryWrite(string text) {
        try { var value=new DataObject(); value.SetData(DataFormats.UnicodeText,text); Clipboard.SetDataObject(value,true); return true; }
        catch(ExternalException) { return false; }
    }
}
internal sealed class DisabledClipboard : ITextClipboard {
    public long Revision=>0;
    public bool TryRead(out string? text) { text=null; return true; }
    public bool TryWrite(string text)=>false;
}

using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows;
namespace LocalRemoteDesktop;
public sealed class WindowsFileClipboard : IFileClipboard {
    [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
    public long Revision=>GetClipboardSequenceNumber();
    public bool TryReadFiles(out string[]? paths) {
        paths=null;
        try {
            var value=Clipboard.GetDataObject();
            if(value?.GetData(DataFormats.FileDrop,false) is string[] files) paths=files;
            return true;
        } catch(ExternalException) { return false; }
    }
    public bool TryWriteFiles(string[] paths) {
        try { var values=new StringCollection(); values.AddRange(paths); Clipboard.SetFileDropList(values); return true; }
        catch(ExternalException) { return false; }
    }
}

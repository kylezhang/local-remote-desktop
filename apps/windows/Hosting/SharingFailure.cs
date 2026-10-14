using System.ComponentModel;
using System.Net.Sockets;
namespace LocalRemoteDesktop;

internal static class SharingFailure {
    internal static string Message(Exception error) => error switch {
        SocketException e when e.SocketErrorCode==SocketError.AddressAlreadyInUse => "TCP 47980 已被占用，请停止另一份共享程序后重试。",
        SocketException e when e.SocketErrorCode==SocketError.AccessDenied => "系统拒绝监听 TCP 47980，请检查系统端口保留或安全软件。",
        SocketException => "无法监听 TCP 47980，请查看下方错误详情。",
        InvalidOperationException e when e.Message.Contains("display",StringComparison.OrdinalIgnoreCase) => "无法读取当前显示器，请确认 Windows 已登录且显示器可用。",
        ArgumentException e when e.Message.Contains("display",StringComparison.OrdinalIgnoreCase) => "无法读取当前显示器，请确认 Windows 已登录且显示器可用。",
        Win32Exception => "系统组件启动失败，请查看下方错误详情。",
        _ => "无法开始接受连接，请查看下方错误详情。"
    };
    internal static string Details(Exception error) {
        var code=error is Win32Exception native?$"NativeError: {native.NativeErrorCode}\n":"";
        if(error is SocketException socket) code+=$"SocketError: {socket.SocketErrorCode}\n";
        // No credential, exception stack, user path or clipboard is included.
        return $"{error.GetType().Name}\n{code}HResult: 0x{error.HResult:X8}";
    }
}

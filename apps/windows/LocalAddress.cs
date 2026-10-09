using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
namespace LocalRemote;
static class LocalAddress
{
    public static string Find()
    {
        try {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                    (n.NetworkInterfaceType == NetworkInterfaceType.Ethernet || n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                .Select(n => n.GetIPProperties())
                .SelectMany(p => p.UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a.Address) &&
                        a.Address.GetAddressBytes()[0] != 0 && !(a.Address.GetAddressBytes()[0] == 169 && a.Address.GetAddressBytes()[1] == 254))
                    .Select(a => new { a.Address, Gateway = p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)) }))
                .OrderByDescending(a => a.Gateway)
                .ThenByDescending(a => IsPrivate(a.Address))
                .Select(a => a.Address.ToString()).FirstOrDefault() ?? "";
        } catch (NetworkInformationException) { return ""; }
    }
    static bool IsPrivate(IPAddress ip) {
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
    }
}

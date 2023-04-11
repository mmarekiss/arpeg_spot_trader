using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace ARPEG.Spot.Trader.Utils;

public static class IpFetcher
{
    public static IEnumerable<UnicastIPAddressInformation> GetAddressess(ILogger logger)
    {
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            logger.LogInformation("Iface {Iface}", adapter.GetPhysicalAddress());
            foreach (var unicastIpInfo in adapter.GetIPProperties().UnicastAddresses)
            {
                logger.LogInformation("addr {Iface}", unicastIpInfo.Address);
                if (unicastIpInfo.Address.AddressFamily == AddressFamily.InterNetwork
                    && unicastIpInfo.Address.ToString().Contains(".55."))
                    yield return unicastIpInfo;
            }
        }
    }
}
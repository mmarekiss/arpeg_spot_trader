using System.Net;
using Microsoft.Extensions.Logging;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Utils;

public class GoodWeFinder
{
    private readonly GoodWeCom _goodWe;
    private readonly ILogger<GoodWeFinder> _logger;

    public GoodWeFinder(GoodWeCom goodWe,
        ILogger<GoodWeFinder> logger)
    {
        _goodWe = goodWe;
        _logger = logger;
    }

    public async Task<(string SN, IPAddress address)> GetGoodWe(IPAddress address,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var sn = await _goodWe.GetInverterName(address);
            if (!string.IsNullOrWhiteSpace(sn))
                return (sn, address);
            _logger.LogError("Cannot connect to Inverter at address {Address}", address);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        return ("", IPAddress.None);
    }

    public async IAsyncEnumerable<(string SN, IPAddress address)> FindGoodWees(
        params (IPAddress address, IPAddress mask)[] ips)
    {
        var result = new List<(string SN, IPAddress address)>();
        var findTasks = new List<Task>();
        foreach (var iface in ips)
        {
            foreach (var chunk in GetIps(iface).Chunk(20))
                findTasks.Add(Task.Run(async () =>
                {
                    foreach (var ip in chunk)
                        try
                        {
                            var sn = await _goodWe.GetInverterName(ip);
                            if (!string.IsNullOrWhiteSpace(sn))
                                lock (result)
                                {
                                    result.Add((sn, ip));
                                }
                        }
                        catch
                        {
                            _logger.LogInformation($"{ip} is not GoodWe");
                        }
                }));
            await Task.WhenAll(findTasks.ToArray());
            foreach (var r in result) yield return r;
            result.Clear();
        }
    }

    private IEnumerable<IPAddress> GetIps((IPAddress address, IPAddress mask) iface)
    {
        var ip = iface.address.GetAddressBytes();
        var mask = iface.mask.GetAddressBytes();

        var baseIp = new[] { ip[0] & mask[0], ip[1] & mask[1], ip[2] & mask[2], ip[3] & mask[3] };


        for (short i0 = 0; i0 <= 255 && CheckMask(0, i0, ip, mask); i0++)
        for (short i1 = 0; i1 <= 255 && CheckMask(1, i1, ip, mask); i1++)
        for (short i2 = 0; i2 <= 255 && CheckMask(2, i2, ip, mask); i2++)
        for (short i3 = 0; i3 <= 255 && CheckMask(3, i3, ip, mask); i3++)
            yield return new IPAddress(new[]
                { (byte)(baseIp[0] + i0), (byte)(baseIp[1] + i1), (byte)(baseIp[2] + i2), (byte)(baseIp[3] + i3) });
    }

    private bool CheckMask(
        int index,
        short i,
        byte[] ip,
        byte[] mask)
    {
        return ((ip[index] + i) & mask[index]) == (ip[index] & mask[index]);
    }
}
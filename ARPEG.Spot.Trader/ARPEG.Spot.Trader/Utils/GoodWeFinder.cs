using System.Net;
using System.Net.NetworkInformation;
using ARPEG.Spot.Trader.GoodWeCommunication.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Utils;

public class GoodWeFinder
{
    private readonly GoodWeCom goodWe;
    private readonly ILogger<GoodWeFinder> logger;
    private readonly IServiceProvider serviceProvider;

    public GoodWeFinder(GoodWeCom goodWe,
        ILogger<GoodWeFinder> logger,
        IServiceProvider serviceProvider)
    {
        this.goodWe = goodWe;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    public bool PingHost(string nameOrAddress)
    {
        var pingable = false;

        var pinger = new Ping();
        var reply = pinger.Send(nameOrAddress);
        pingable = reply.Status == IPStatus.Success;

        logger.LogError("{Address} is reachable {Reachable}", nameOrAddress, pingable);

        return pingable;
    }

    public async Task<(string SN, IConnection?)> GetGoodWeRs485(CancellationToken cancellationToken)
    {
        var rs485Connection = ActivatorUtilities.CreateInstance<RS485Connection>(serviceProvider);

        logger.LogInformation("Try check address RS485");
        try
        {
            for (var i = 0; i < 10; i++)
            {
                logger.LogError("try connect to Inverter at address RS485, attempt [{i}]", i);

                var (sn, connection) = await goodWe.GetInverterName(rs485Connection);
                if (!string.IsNullOrWhiteSpace(sn))
                {
                    logger.LogInformation("Inverter found at address RS485 {sn}", sn);
                    return (sn, connection);
                }
                logger.LogError("Cannot connect to Inverter at address RS485");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        catch (Exception exc)
        {
            logger.LogInformation("RS485 not found");
        }

        return ("", null);
    }

    public async Task<(string SN, IConnection? connection)> GetGoodWe(IPAddress address,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var udpConnection = ActivatorUtilities.CreateInstance<UdpConnection>(serviceProvider);

            udpConnection.Init(address);

            logger.LogInformation("Try check address {0}", address);
            var (sn, connection) = await goodWe.GetInverterName(udpConnection);
            if (!string.IsNullOrWhiteSpace(sn))
                return (sn, connection);
            logger.LogError("Cannot connect to Inverter at address {Address}", address);
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            var result = PingHost(address.ToString());
            if (!result)
                return ("", null);
        }

        return ("", null);
    }

    public async IAsyncEnumerable<(string SN, IConnection connection)> FindGoodWees(
        params (IPAddress address, IPAddress mask)[] ips)
    {
        var result = new List<(string SN, IConnection connection)>();
        var findTasks = new List<Task>();
        foreach (var iface in ips)
        {
            foreach (var chunk in GetIps(iface).Chunk(20))
                findTasks.Add(Task.Run(async () =>
                {
                    foreach (var ip in chunk)
                    {
                        if (result.Any())
                            return; //Stop when somebody found GW
                        try
                        {
                            var udpConnection = ActivatorUtilities.CreateInstance<UdpConnection>(serviceProvider);

                            udpConnection.Init(ip);
                            logger.LogInformation("Try check address {0}", ip);
                            var connection = await goodWe.GetInverterName(udpConnection);

                            if (!string.IsNullOrWhiteSpace(connection.sn) && connection.connection is not null)
                                lock (connection.sn)
                                {
                                    logger.LogInformation($"{ip} found GoodWe {connection.sn}");
                                    result.Add((connection.sn, connection.connection));
                                    return;
                                }
                            else
                                logger.LogInformation($"{ip} is not GoodWe");
                        }
                        catch
                        {
                            logger.LogInformation($"{ip} is not GoodWe");
                        }
                    }
                }));

            await Task.WhenAll(findTasks.ToArray());
            foreach (var r in result)
            {
                yield return r;
                yield break;
            }
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
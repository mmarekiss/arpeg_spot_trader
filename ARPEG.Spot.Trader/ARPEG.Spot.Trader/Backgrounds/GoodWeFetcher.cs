using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ARPEG.Spot.Trader.Backgrounds.Helpers;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.GoodWeCommunication;
using ARPEG.Spot.Trader.GoodWeCommunication.Connections;
using ARPEG.Spot.Trader.Integration;
using ARPEG.Spot.Trader.Services;
using ARPEG.Spot.Trader.Store;
using ARPEG.Spot.Trader.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Backgrounds;

public class GoodWeFetcher : BackgroundService
{
    private readonly List<IPAddress> broadcasts = new();
    private readonly IConfigUpdater configUpdater;
    private readonly GoodWeFinder finder;
    private readonly Gauge gauge;
    private readonly Gauge gaugeIp;
    private readonly IOptions<GoodWe> goodWeConfig;
    private readonly IGoodWeInvStore invStore;
    private readonly ILogger<GoodWeFetcher> logger;
    private readonly List<string> myIps = new();
    private readonly SearchFactory searchFactory;
    private readonly IServiceProvider serviceProvider;
    private readonly Version version;

    public GoodWeFetcher(
        GoodWeFinder finder,
        IOptions<GoodWe> goodWeConfig,
        IGoodWeInvStore invStore,
        ILogger<GoodWeFetcher> logger,
        SearchFactory searchFactory,
        IServiceProvider serviceProvider,
        IConfigUpdater configUpdater)
    {
        this.finder = finder;
        this.goodWeConfig = goodWeConfig;
        this.invStore = invStore;
        this.logger = logger;
        this.searchFactory = searchFactory;
        this.serviceProvider = serviceProvider;
        this.configUpdater = configUpdater;
        version = GetType().Assembly.GetName().Version ?? new Version(0, 0);
        this.logger.LogInformation("Start application with version [{Version}]", version);
        gauge = Metrics.CreateGauge("Version", "GoodWe traced value",
            new GaugeConfiguration { LabelNames = new[] { "sn", "part" } });
        gaugeIp = Metrics.CreateGauge("IP", "trader IP", new GaugeConfiguration { LabelNames = new[] { "sn", "ip" } });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            SshHelper.SetupTemporalLog(logger);
            var addresses = SshHelper.ConnectWiFi(logger).ToList();
            myIps.AddRange(addresses.Select(x => x.unicast));
            broadcasts.AddRange(addresses.Select(x => x.broadcast));
            ExposeVersionToTraces($"STARTUP-{Guid.NewGuid()}");

            (var SN, var connection) = await finder.GetGoodWeRs485(stoppingToken);
            if (connection is not null)
                await RunTrader(SN, connection, stoppingToken);
            else
                await RunGoodWeAtUdp(stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError("Good We didn't found, next try after 30minutes");
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }

    private async Task RunGoodWeAtUdp(CancellationToken stoppingToken)
    {
        if (IPAddress.TryParse(goodWeConfig.Value.Ip, out var ipAddress))
        {
            (string SN, IConnection? connection) goodWee;
            goodWee = await finder.GetGoodWe(ipAddress, stoppingToken);
            if (goodWee.connection is null)
                goodWee = await CheckLastKnownIp(stoppingToken);

            if (goodWee.connection is null)
                goodWee = await FindInNetworks(stoppingToken);

            if (goodWee.connection is null)
                throw new EntryPointNotFoundException();

            await RunTrader(goodWee.SN, goodWee.connection, stoppingToken);
        }
        else
        {
            var addresses = IpFetcher.GetAddressess(logger).ToArray();
            await foreach ((var SN, var connection) in finder.FindGoodWees(addresses)
                               .WithCancellation(stoppingToken))
            {
                logger.LogInformation("Found GoodWe at {Ip}", connection);
                await RunTrader(SN, connection, stoppingToken);
            }
        }
    }

    private async Task<(string SN, IConnection? connection)> CheckLastKnownIp(CancellationToken stoppingToken)
    {
        return IPAddress.TryParse(goodWeConfig.Value.LastKnownIp ?? "", out var ipAddress)
            ? await finder.GetGoodWe(ipAddress, stoppingToken)
            : ("", null);
    }

    private async Task<(string SN, IConnection connection)> FindInNetworks(CancellationToken stoppingToken)
    {
        foreach (var broadcast in broadcasts)
        {
            var ipAddress = await searchFactory.TryFetchBroadcastAsync(broadcast, stoppingToken);
            if (ipAddress is not null)
            {
                (var sn, var connection) = await finder.GetGoodWe(ipAddress, stoppingToken);
                if (connection is not null)
                    return (sn, connection);
            }
        }

        return await finder.FindGoodWees(GetIpsFromHost()).FirstOrDefaultAsync(stoppingToken);
    }

    private (IPAddress address, IPAddress mask)[] GetIpsFromHost()
    {
        return myIps.OrderByDescending(x => x).Select(x => (IPAddress.Parse(x), IPAddress.Parse("255.255.255.0")))
            .ToArray();
    }

    private void ExposeVersionToTraces(string sn)
    {
        foreach (var labelValue in gauge.GetAllLabelValues())
            gauge.RemoveLabelled(labelValue);
        foreach (var labelValue in gaugeIp.GetAllLabelValues())
            gaugeIp.RemoveLabelled(labelValue);

        gauge.WithLabels(sn, "Major").Set(version.Major);
        gauge.WithLabels(sn, "Minor").Set(version.Minor);

        foreach (var adr in myIps)
        {
            var match = Regex.IsMatch(adr, "\\d+\\.\\d+\\.\\d+\\.\\d+");
            if (match)
                gaugeIp.WithLabels(sn, adr).Set(1);
        }
    }

    private async Task RunTrader(
        string sn,
        IConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection is UdpConnection udpConnection)
        {
            var root = configUpdater.GetCurrent();
            root.GoodWe.LastKnownIp = udpConnection.IpAddress.ToString();
            await configUpdater.SaveCurrent(root, cancellationToken);
        }

        ExposeVersionToTraces(sn);
        var licence = await FetchLicence(sn);

        if (licence is not null && licence.LicenceVersion != LicenceVersion.None)
        {
            logger.LogWarning("Your licence for {name} is {lic}", sn, licence.LicenceVersion.ToString());

            Definition definition = new(sn, licence.LicenceVersion, connection);
            invStore.AddGoodWe(definition);
            var communicator = serviceProvider.GetService<GoodWeCom>() ??
                               throw new ApplicationException("please define goodWe comm");

            while (!cancellationToken.IsCancellationRequested)
                await communicator.GetHomeConsumption(definition, cancellationToken);
        }
        else
        {
            logger.LogWarning("You don't have licence for {name}", sn);
        }
    }

    private async Task<RunLicence?> FetchLicence(string name)
    {
        HttpClient client = new();
        return await client.GetFromJsonAsync<RunLicence>(
            $"https://arpeg-licences.azurewebsites.net/api/getLicence/{name}");
    }
}
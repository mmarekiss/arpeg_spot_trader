using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ARPEG.Spot.Trader.Backgrounds.Helpers;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.GoodWeCommunication;
using ARPEG.Spot.Trader.GoodWeCommunication.Connections;
using ARPEG.Spot.Trader.Integration;
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
    private readonly GoodWeFinder finder;
    private readonly Gauge gauge;
    private readonly Gauge gaugeIp;
    private readonly IOptions<GoodWe> goodWeConfig;
    private readonly IGoodWeInvStore invStore;
    private readonly ILogger<GoodWeFetcher> logger;
    private readonly List<string> myIps = new();
    private readonly IServiceProvider serviceProvider;
    private readonly Version version;

    public GoodWeFetcher(GoodWeFinder finder,
        IOptions<GoodWe> goodWeConfig,
        IGoodWeInvStore invStore,
        ILogger<GoodWeFetcher> logger,
        IServiceProvider serviceProvider)
    {
        this.finder = finder;
        this.goodWeConfig = goodWeConfig;
        this.invStore = invStore;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
        version = GetType().Assembly.GetName().Version ?? new Version(0, 0);
        this.logger.LogInformation("Start application with version [{Version}]", version);
        gauge = Metrics.CreateGauge("Version", "GoodWe traced value",
            new GaugeConfiguration { LabelNames = new[] { "sn", "part" } });
        gaugeIp = Metrics.CreateGauge("IP", "trader IP", new GaugeConfiguration { LabelNames = new[] { "sn", "ip" } });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       
        myIps.AddRange(SshHelper.ConnectWiFi(logger));
        ExposeVersionToTraces($"STARTUP-{Guid.NewGuid()}");

        (string SN, IConnection? connection) goodWee = await finder.GetGoodWeRs485(stoppingToken);
        if (goodWee.connection is not null)
            await RunTrader(goodWee.SN, goodWee.connection, stoppingToken);
        else
            await RunGoodWeAtUdp(stoppingToken);
    }

    private async Task RunGoodWeAtUdp(CancellationToken stoppingToken)
    {
        if (IPAddress.TryParse(goodWeConfig.Value.Ip, out var ipAddress))
        {
            (string SN, IConnection? connection) goodWee;
            goodWee = await finder.GetGoodWe(ipAddress, stoppingToken);
            if (goodWee.connection is null)
                goodWee = await finder.FindGoodWees(GetIpsFromHost()).FirstOrDefaultAsync(stoppingToken);

            if (goodWee.connection is null) 
                throw new EntryPointNotFoundException();

            await RunTrader(goodWee.SN, goodWee.connection, stoppingToken);
        }
        else
        {
            var addresses = IpFetcher.GetAddressess(logger).ToArray();
            await foreach (var goodWee in finder.FindGoodWees(addresses)
                               .WithCancellation(stoppingToken))
            {
                logger.LogInformation("Found GoodWe at {Ip}", goodWee.connection);
                await RunTrader(goodWee.SN, goodWee.connection, stoppingToken);
            }
        }
    }

    private (IPAddress address, IPAddress mask)[] GetIpsFromHost()
    {
        return myIps.OrderByDescending(x => x).Select(x => (IPAddress.Parse(x), IPAddress.Parse("255.255.255.0")))
            .ToArray();
    }

    private void ExposeVersionToTraces(string sn)
    {
        gauge.WithLabels(sn, "Major").Set(version.Major);
        gauge.WithLabels(sn, "Minor").Set(version.Minor);

        foreach (var adr in myIps)
        {
            var match = Regex.IsMatch(adr, "\\d+\\.\\d+\\.\\d+\\.\\d+");
            if (match) gaugeIp.WithLabels(sn, adr).Set(1);
        }
    }

    private async Task RunTrader(string sn,
        IConnection connection,
        CancellationToken cancellationToken)
    {

        SshHelper.SetupGwSnLog(sn, logger);
        ExposeVersionToTraces(sn);
        var licence = await FetchLicence(sn);

        if (licence is not null && licence.LicenceVersion != LicenceVersion.None)
        {
            logger.LogWarning("Your licence for {name} is {lic}", sn, licence.LicenceVersion.ToString());

            var definition = new Definition(sn, licence.LicenceVersion, connection);
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
        var client = new HttpClient();
        return await client.GetFromJsonAsync<RunLicence>(
            $"https://arpeg-licences.azurewebsites.net/api/getLicence/{name}");
    }
}
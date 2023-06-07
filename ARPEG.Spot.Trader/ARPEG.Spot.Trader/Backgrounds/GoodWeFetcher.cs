﻿using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Integration;
using ARPEG.Spot.Trader.Store;
using ARPEG.Spot.Trader.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using Renci.SshNet;
using Renci.SshNet.Common;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Backgrounds;

public class GoodWeFetcher : BackgroundService
{
    private readonly GoodWeFinder finder;
    private readonly IOptions<GoodWe> goodWeConfig;
    private readonly Version version;
    private readonly IGoodWeInvStore invStore;
    private readonly ILogger<GoodWeFetcher> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly Gauge gauge;

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
        this.version = GetType().Assembly.GetName().Version ?? new Version(0, 0); 
        
        gauge = Metrics.CreateGauge("Version", "GoodWe traced value", new GaugeConfiguration { LabelNames = new[] { "sn", "part" } });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var login = "rock";
        var password = "rock";
        if (!Debugger.IsAttached)
        {
            ConnectWiFi(login, password);
        }

        if (IPAddress.TryParse(goodWeConfig.Value.Ip, out var ipAddress))
        {
            var goodWee = await finder.GetGoodWe(ipAddress, stoppingToken);
            if (goodWee.address.Equals(IPAddress.None)) throw new ApplicationException("GoodWe Not Found");
            await RunTrader(goodWee.SN, goodWee.address, stoppingToken);
        }
        else
        {
            var addresses = IpFetcher.GetAddressess(logger).ToArray();
            await foreach (var goodWee in finder.FindGoodWees(addresses)
                               .WithCancellation(stoppingToken))
            {
                logger.LogInformation("Found GoodWe at {Ip}", goodWee.address);
                await RunTrader(goodWee.SN, goodWee.address, stoppingToken);
            }
        }
    }

    private void ConnectWiFi(string login,
        string password)
    {
        if (!Debugger.IsAttached)
        {
            var serverAddress = "172.17.0.1";

            var client = new SshClient(serverAddress, 22, login, password);
            client.Connect();

            IDictionary<TerminalModes, uint> modes =
                new Dictionary<TerminalModes, uint>();

            modes.Add(TerminalModes.ECHO, 53);

            var shellStream =
                client.CreateShellStream("xterm", 80, 24, 800, 600, 1024, modes);
            var output = shellStream.Expect(new Regex(@"[$>]"));

            shellStream.WriteLine(
                "sudo nmcli -f ssid dev wifi | grep Solar | sed 's/ *$//g' | head -1 | xargs -I % sudo nmcli dev wifi connect % password '12345678'");
            output = shellStream.Expect(new Regex(@"([$#>:])"));
            logger.LogInformation("Connect To WiFi command {WiFiCommand}", output);
            shellStream.WriteLine(password);
            output = shellStream.Expect(new Regex(@"[$>]"));
            shellStream.WriteLine("nmcli device");
            output = shellStream.Expect(new Regex(@"[$>]"));
            logger.LogInformation("Connect To WiFi? {WiFiCommand}", output);
            // shellStream.WriteLine("docker image prune");
            // output = shellStream.Expect(new Regex(@"[N]]"));
            // shellStream.WriteLine("y");
            // output = shellStream.Expect(new Regex(@"[$>]"));
            // _logger.LogInformation("Docker images pruned {Output}", output);
            client.Disconnect();
        }
    }

    private void ExposeVersionToTraces(string sn)
    {
        gauge.WithLabels(sn, "Major").Set(version.Major);
        gauge.WithLabels(sn, "Minor").Set(version.Minor);
    }

    private async Task RunTrader(string sn,
        IPAddress address,
        CancellationToken cancellationToken)
    {
        ExposeVersionToTraces(sn);
        var licence = await FetchLicence(sn);

        if (licence is not null && licence.LicenceVersion != LicenceVersion.None)
        {
            logger.LogWarning("Your licence for {name} is {lic}", sn, licence.LicenceVersion.ToString());

            var definition = new Definition
            {
                SN = sn,
                Address = address,
                Licence = licence.LicenceVersion
            };
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
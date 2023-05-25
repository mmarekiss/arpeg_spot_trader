using System.Diagnostics;
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
using Renci.SshNet;
using Renci.SshNet.Common;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Backgrounds;

public class GoodWeFetcher : BackgroundService
{
    private readonly GoodWeFinder _finder;
    private readonly IOptions<GoodWe> _goodWeConfig;
    private readonly IGoodWeInvStore _invStore;
    private readonly ILogger<GoodWeFetcher> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GoodWeFetcher(GoodWeFinder finder,
        IOptions<GoodWe> goodWeConfig,
        IGoodWeInvStore invStore,
        ILogger<GoodWeFetcher> logger,
        IServiceProvider serviceProvider)
    {
        _finder = finder;
        _goodWeConfig = goodWeConfig;
        _invStore = invStore;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var login = "rock";
        var password = "rock";
        if (!Debugger.IsAttached)
        {
            ConnectWiFi(login, password);
        }

        if (IPAddress.TryParse(_goodWeConfig.Value.Ip, out var ipAddress))
        {
            var goodWee = await _finder.GetGoodWe(ipAddress, stoppingToken);
            if (goodWee.address.Equals(IPAddress.None)) throw new ApplicationException("GoodWe Not Found");

            await RunTrader(goodWee.SN, goodWee.address, stoppingToken);
        }
        else
        {
            var addresses = IpFetcher.GetAddressess(_logger).ToArray();
            await foreach (var goodWee in _finder.FindGoodWees(addresses)
                               .WithCancellation(stoppingToken))
            {
                _logger.LogInformation("Found GoodWe at {Ip}", goodWee.address);
                await RunTrader(goodWee.SN, goodWee.address, stoppingToken);
            }
        }
    }

    private void ConnectWiFi(string login,
        string password)
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
        _logger.LogInformation("Connect To WiFi command {WiFiCommand}", output);
        shellStream.WriteLine(password);
        output = shellStream.Expect(new Regex(@"[$>]"));
        shellStream.WriteLine("nmcli device");
        output = shellStream.Expect(new Regex(@"[$>]"));
        _logger.LogInformation("Connect To WiFi? {WiFiCommand}", output);
        // shellStream.WriteLine("docker image prune");
        // output = shellStream.Expect(new Regex(@"[N]]"));
        // shellStream.WriteLine("y");
        // output = shellStream.Expect(new Regex(@"[$>]"));
        // _logger.LogInformation("Docker images pruned {Output}", output);
        client.Disconnect();
    }

    private async Task RunTrader(string name,
        IPAddress address,
        CancellationToken cancellationToken)
    {
        var licence = await FetchLicence(name);

        if (licence is not null && licence.LicenceVersion != LicenceVersion.None)
        {
            _logger.LogWarning("Your licence for {name} is {lic}", name, licence.LicenceVersion.ToString());

            var definition = new Definition
            {
                SN = name,
                Address = address,
                Licence = licence.LicenceVersion
            };
            _invStore.AddGoodWe(definition);
            var communicator = _serviceProvider.GetService<GoodWeCom>() ??
                               throw new ApplicationException("please define goodWe comm");

            while (!cancellationToken.IsCancellationRequested)
                await communicator.GetHomeConsumption(definition, cancellationToken);
        }
        else
        {
            _logger.LogWarning("You don't have licence for {name}", name);
        }
    }

    private async Task<RunLicence?> FetchLicence(string name)
    {
        var client = new HttpClient();
        return await client.GetFromJsonAsync<RunLicence>(
            $"https://arpeg-licences.azurewebsites.net/api/getLicence/{name}");
    }
}
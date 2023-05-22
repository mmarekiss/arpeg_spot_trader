using System.Net;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Integration;
using ARPEG.Spot.Trader.Store;
using ARPEG.Spot.Trader.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        if (Environment.GetEnvironmentVariable("Env") == "Dev")
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProps = nic.GetIPProperties();
                // check if localAddr is in ipProps.UnicastAddresses
                _logger.LogInformation(String.Join("; ",ipProps.UnicastAddresses.Select(x=>x.Address)));
            }
        }
        
        if (IPAddress.TryParse(_goodWeConfig.Value.Ip, out var ipAddress))
        {
            var goodWee = await _finder.GetGoodWe(ipAddress, stoppingToken);
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

    private async Task RunTrader(string name,
        IPAddress address,
        CancellationToken cancellationToken)
    {
        var licence = await FetchLicence(name);

        if (licence is not null && licence.LicenceVersion != LicenceVersion.None)
        {
            _logger.LogWarning("Your licence for {name} is {lic}", name, licence.LicenceVersion.ToString());

            var definition = new Definition()
            {
                SN = name,
                Address = address,
                Licence = licence.LicenceVersion
            }; 
            _invStore.AddGoodWe(definition);
            var communicator = _serviceProvider.GetService<GoodWeCom>() ??
                               throw new ApplicationException("please define goodWe comm");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                await communicator.GetHomeConsumption(definition, cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("You don't have licence for {name}", name);
        }
    }

    private async Task<RunLicence?> FetchLicence(string name)
    {
        var client = new HttpClient();
        return await client.GetFromJsonAsync<RunLicence>($"https://arpeg-licences.azurewebsites.net/api/getLicence/{name}");
    }
}
﻿using System.Net;
using System.Net.Http.Json;
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
    private readonly IOptionsMonitor<GoodWe> _goodWeConfig;
    private readonly GoodWeInvStore _invStore;
    private readonly ILogger<GoodWeFetcher> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GoodWeFetcher(GoodWeFinder finder,
        IOptionsMonitor<GoodWe> goodWeConfig,
        GoodWeInvStore invStore,
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
        var tasks = new List<Task>();
        
        var addressess = IPAddress.TryParse(_goodWeConfig.CurrentValue.Ip, out var ipAddress) 
            ? new [] { (ipAddress, IPAddress.Parse("255.255.255.255"))}  
            : IpFetcher.GetAddressess(_logger).ToArray();
        
        await foreach (var goodWee in _finder.FindGoodWees(addressess)
                           .WithCancellation(stoppingToken))
        {
            tasks.Add(RunTraded(goodWee.SN, goodWee.address, stoppingToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task RunTraded(string name,
        IPAddress address,
        CancellationToken cancellationToken)
    {
        var licence = await FetchLicence(name);

        if (licence is not null && licence.LicenceVersion != LicenceVersion.None)
        {
            _logger.LogWarning("Your licence for {name} is {lic}", name, licence.LicenceVersion.ToString());

            var communicator = _serviceProvider.GetService<GoodWeCom>() ??
                               throw new ApplicationException("please define goodWe comm");
            communicator.InitHostname(address);
            communicator.InitInverterName(name);
            communicator.SetLicence(licence.LicenceVersion);
            _invStore.AddGoodWe(communicator);
            while (!cancellationToken.IsCancellationRequested)
            {
                await communicator.GetHomeConsumption(cancellationToken);
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
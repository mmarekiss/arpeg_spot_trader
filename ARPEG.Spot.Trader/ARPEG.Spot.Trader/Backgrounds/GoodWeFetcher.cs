using System.Net;
using ARPEG.Spot.Trader.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Backgrounds;

public class GoodWeFetcher : BackgroundService
{
    private readonly GoodWeFinder _finder;
    private readonly ILogger<GoodWeFetcher> _logger;
    private readonly IServiceProvider _serviceProvider;

    public GoodWeFetcher(GoodWeFinder finder,
        ILogger<GoodWeFetcher> logger,
        IServiceProvider serviceProvider)
    {
        _finder = finder;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>();
        var addressess = IpFetcher.GetAddressess(_logger).ToArray();
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
        var communicator = _serviceProvider.GetService<GoodWeCom>() ??
                           throw new ApplicationException("please define goodWe comm");
        communicator.InitHostname(address);
        communicator.InitInverterName(name);
        while (!cancellationToken.IsCancellationRequested)
        {
            await communicator.GetHomeConsumption();
        }
    }
}
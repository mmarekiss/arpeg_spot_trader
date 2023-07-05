using System.Net.Http.Json;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.GoodWeCommunication;
using ARPEG.Spot.Trader.Integration;
using ARPEG.Spot.Trader.Services;
using ARPEG.Spot.Trader.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Backgrounds;

public class PriceFetcher : BackgroundService
{
    private readonly GoodWeCom communicator;
    private readonly ForecastService forecastService;
    private readonly Gauge gauge;
    private readonly Gauge gaugePvForecast;

    private readonly IGoodWeInvStore invStore;
    private readonly ManualBatteryConfig manualBatteryConfig;
    private readonly ILogger<PriceFetcher> logger;
    private readonly PriceService priceService;
    private readonly NowManualBatteryService nowManualBatteryService;

    public PriceFetcher(
        PriceService priceService,
        GoodWeCom communicator,
        ForecastService forecastService,
        IGoodWeInvStore invStore,
        ManualBatteryConfig manualBatteryConfig,
        ILogger<PriceFetcher> logger,
        NowManualBatteryService nowManualBatteryService)
    {
        this.priceService = priceService;
        this.communicator = communicator;
        this.forecastService = forecastService;
        this.invStore = invStore;
        this.manualBatteryConfig = manualBatteryConfig;
        this.logger = logger;
        this.nowManualBatteryService = nowManualBatteryService;

        gauge = Metrics.CreateGauge("Price", "Current price from OTE");
        gaugePvForecast = Metrics.CreateGauge("PV_forecast", "Solar forecast", "part");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await priceService.FetchPrices(stoppingToken);
                await forecastService.GetForecast(stoppingToken);

                await HandleCurrentPrice(stoppingToken);
            }
            catch (Exception exc)
            {
                logger.LogWarning(exc, "Some error during price fetching");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }

    private async Task HandleCurrentPrice(CancellationToken stoppingToken)
    {
        var price = priceService.GetCurrentPrice();
        var pvForecast = forecastService.GetCurrentForecast();
        var pForecast24 = forecastService.GetForecast24();
        gauge.Set(price);
        gaugePvForecast.WithLabels("now").Set(pvForecast);
        gaugePvForecast.WithLabels("24").Set(pForecast24);

        if (!await ManualBattery(stoppingToken))
        {
            await HandleManualBatteryManagement(price, stoppingToken);
        }

        await AvoidNegativePrice(price, stoppingToken);
        await HandleExternalBatteryManagement(stoppingToken);
    }

    private async Task<bool> ManualBattery(CancellationToken stoppingToken)
    {
        var direction = nowManualBatteryService.GetActiveDirection();
        if (direction == NowManualBatteryService.BatteryChargeDirection.None)
            return false;
        
        foreach (var gw in invStore.GoodWes.Where(g => g.Licence.HasFlag(LicenceVersion.ManualBattery)))
        {
            switch (direction)
            {
                case NowManualBatteryService.BatteryChargeDirection.Charge:
                    await communicator.ForceBatteryCharge(gw, 10_000, stoppingToken);
                    break;
                case NowManualBatteryService.BatteryChargeDirection.Discharge:
                    await communicator.ForceBatteryDisCharge(gw, 10_000, stoppingToken);
                    break;
            }
        }
        return true;
    }

    private async Task HandleManualBatteryManagement(double price,
        CancellationToken stoppingToken)
    {
        foreach (var gw in invStore.GoodWes.Where(g => g.Licence.HasFlag(LicenceVersion.ManualBattery)))
        {
            if (manualBatteryConfig.Charge && price < manualBatteryConfig.MaxPriceForCharge)
            {
                await communicator.ForceBatteryCharge(gw, 10_000, stoppingToken);
            }
            else if (manualBatteryConfig.Discharge && price > manualBatteryConfig.MinPriceForDischarge)
            {
                await communicator.ForceBatteryDisCharge(gw, 10_000, stoppingToken);
            }
            else
            {
                await communicator.SetSelfConsumptionMode(gw, stoppingToken);
            }
        }
    }

    private async Task AvoidNegativePrice(double price,
        CancellationToken stoppingToken)
    {
        foreach (var gw in invStore.GoodWes.Where(g => g.Licence.HasFlag(LicenceVersion.AvoidNegativePrice)))
            if (price < 0)
                await communicator.SetExportLimit(0, gw, stoppingToken);
            else
                await communicator.DisableExportLimit(gw, stoppingToken);
        
    }

    private async Task HandleExternalBatteryManagement(CancellationToken stoppingToken)
    {
        foreach (var gw in invStore.GoodWes.Where(g => g.Licence.HasFlag(LicenceVersion.Spot)))
        {
            var batteryManagement = await FetchBatteryCommand(gw.Sn, stoppingToken);
            switch (batteryManagement?.BatteryManagement)
            {
                case BatteryManagement.ForceCharge:
                    await communicator.ForceBatteryCharge(gw, 10_000, stoppingToken);
                    break;
                case BatteryManagement.ForceDischarge:
                    await communicator.ForceBatteryDisCharge(gw, 10_000, stoppingToken);
                    break;
                default:
                    await communicator.SetSelfConsumptionMode(gw, stoppingToken);
                    break;
            };
        }
    }

    private async Task InvokeMethodNonSpot(Func<Definition, Task> invokeFunc)
    {
        await Task.WhenAll(invStore.GoodWes.Where(x => !x.Licence.HasFlag(LicenceVersion.Spot)).Select(invokeFunc));
    }

    private async Task InvokeMethodSpot(Func<Definition, Task> invokeFunc)
    {
        await Task.WhenAll(invStore.GoodWes.Where(x => x.Licence.HasFlag(LicenceVersion.Spot)).Select(invokeFunc));
    }

    private async Task<BatteryManagementCommands?> FetchBatteryCommand(string name,
        CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        return await client.GetFromJsonAsync<BatteryManagementCommands>(
            $"https://arpeg-licences.azurewebsites.net/api/batterymanagement/{name}?code=avtH1HThysw7MbnrtWo7rLxipfEkPCX18jHghGClZ2deAzFuF7K0_Q=="
            , cancellationToken);
    }
}
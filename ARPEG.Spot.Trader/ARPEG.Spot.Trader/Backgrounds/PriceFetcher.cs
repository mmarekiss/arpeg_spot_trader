using System.Net.Http.Json;
using ARPEG.Spot.Trader.Config;
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

    private Grid GridOptions { get; set; }

    private readonly IGoodWeInvStore invStore;
    private readonly ILogger<PriceFetcher> logger;
    private readonly PriceService priceService;

    public PriceFetcher(PriceService priceService,
        GoodWeCom communicator,
        ForecastService forecastService,
        Grid options,
        IGoodWeInvStore invStore,
        ILogger<PriceFetcher> logger)
    {
        this.priceService = priceService;
        this.communicator = communicator;
        this.forecastService = forecastService;
        GridOptions = options;
        this.invStore = invStore;
        this.logger = logger;

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

        // if (GridOptions.TradeEnergy)
        // {
        //     var exportLimitDef = GridOptions.ExportLimit;
        //     var exportLimit = exportLimitDef ?? 10_000;
        //
        //     if (price < 10)
        //     {
        //         exportLimit = Math.Min(exportLimit, (ushort)200);
        //         await InvokeMethodNonSpot(g => communicator.SetExportLimit((ushort)exportLimit, g, stoppingToken));
        //     }
        //     else if (exportLimitDef.HasValue)
        //     {
        //         await InvokeMethodNonSpot(g => communicator.SetExportLimit((ushort)exportLimit, g, stoppingToken));
        //     }
        //     else
        //     {
        //         await InvokeMethodNonSpot(g => communicator.DisableExportLimit(g, stoppingToken));
        //     }
        //
        //     if ((price < -10 && pvForecast < 100)
        //         || (priceService.IsMinPriceOfNight() && !forecastService.PossibleFulfillBattery()))
        //         await InvokeMethodNonSpot(g =>
        //             communicator.ForceBatteryCharge(g, (ushort)GridOptions.ChargePower, stoppingToken));
        //     else
        //         await InvokeMethodNonSpot(g => communicator.StopForceBatteryCharge(g, stoppingToken));
        // }
        // else
        // {
        //     logger.LogInformation("Energy trading is disabled");
        // }

        await HandleExternalBatteryManagement(stoppingToken);
    }

    private async Task HandleExternalBatteryManagement(CancellationToken stoppingToken)
    {
        foreach (var gw in invStore.GoodWes.Where(g=>g.Licence.HasFlag(LicenceVersion.Spot)))
        {
            var batteryManagement = await FetchBatteryCommand(gw.SN, stoppingToken);
            switch(batteryManagement?.BatteryManagement) 
            {
                case BatteryManagement.ForceCharge :
                    await communicator.ForceBatteryCharge(gw, 10_000, stoppingToken);
                    break;
                case BatteryManagement.ForceDischarge:
                    await communicator.ForceBatteryDisCharge(gw, 10_000, stoppingToken);
                    break;
                default:
                    await communicator.StopForceBatteryCharge(gw, stoppingToken);
                    break;
            };
        }
    }

    private async Task InvokeMethodNonSpot(Func<Definition, Task> invokeFunc)
    {
        await Task.WhenAll(invStore.GoodWes.Where(x=>!x.Licence.HasFlag(LicenceVersion.Spot)).Select(invokeFunc));
    }
    
    private async Task InvokeMethodSpot(Func<Definition, Task> invokeFunc)
    {
        await Task.WhenAll(invStore.GoodWes.Where(x=>x.Licence.HasFlag(LicenceVersion.Spot)).Select(invokeFunc));
    }
    
    private async Task<BatteryManagementCommands?> FetchBatteryCommand(string name, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        return await client.GetFromJsonAsync<BatteryManagementCommands>(
            $"https://arpeg-licences.azurewebsites.net/api/batterymanagement/{name}?code=avtH1HThysw7MbnrtWo7rLxipfEkPCX18jHghGClZ2deAzFuF7K0_Q=="
            , cancellationToken: cancellationToken);
    }
}
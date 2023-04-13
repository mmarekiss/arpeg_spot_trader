using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Services;
using ARPEG.Spot.Trader.Store;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader.Backgrounds;

public class PriceFetcher : BackgroundService
{
    private readonly ForecastService _forecastService;
    private readonly IOptionsMonitor<Grid> _gridOptions;
    private readonly Gauge _gauge;
    private readonly Gauge _gaugePvForecast;
    private readonly GoodWeInvStore _invStore;
    private readonly ILogger<PriceFetcher> _logger;
    private readonly PriceService _priceService;

    public PriceFetcher(PriceService priceService,
        ForecastService forecastService,
        IOptionsMonitor<Grid> gridOptions,
        GoodWeInvStore invStore,
        ILogger<PriceFetcher> logger)
    {
        _priceService = priceService;
        _forecastService = forecastService;
        _gridOptions = gridOptions;
        _invStore = invStore;
        _logger = logger;

        _gauge = Metrics.CreateGauge("Price", "Current price from OTE");
        _gaugePvForecast = Metrics.CreateGauge("PV_forecast", "Solar forecast");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _priceService.FetchPrices(stoppingToken);
                await _forecastService.GetForecast(stoppingToken);

                await HandleCurrentPrice(stoppingToken);
            }
            catch (Exception exc)
            {
                _logger.LogWarning(exc, "Some error during price fetching");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task HandleCurrentPrice(CancellationToken stoppingToken)
    {
        var price = _priceService.GetCurrentPrice();
        var pvForecast = _forecastService.GetCurrentForecast();
        var pvMaxForecast = _forecastService.GetCurrentForecast();
        _gauge.Set(price);
        _gaugePvForecast.Set(pvForecast);

        if (_gridOptions.CurrentValue.TradeEnergy)
        {
            var exportLimitDef = _gridOptions.CurrentValue.ExportLimit;
            var exportLimit = exportLimitDef ?? (ushort)10_000;

            if (price < 10)
            {
                exportLimit = Math.Min(exportLimit, (ushort)200);
                await InvokeMethod(g => g.SetExportLimit(exportLimit, stoppingToken));
            }
            else if (exportLimitDef.HasValue)
            {
                await InvokeMethod(g => g.SetExportLimit(exportLimit, stoppingToken));
            }
            else 
            {
                await InvokeMethod(g => g.DisableExportLimit(stoppingToken));
            }

            if (price < -10 && pvForecast < 10 && pvMaxForecast < 50)
                await InvokeMethod(g => g.ForceBatteryCharge(stoppingToken));
            else
                await InvokeMethod(g => g.StopForceBatteryCharge(stoppingToken));
        }
    }

    private async Task InvokeMethod(Func<GoodWeCom, Task> invokeFunc)
    {
        await Task.WhenAll(_invStore.GoodWes.Select(invokeFunc.Invoke));
    }
}
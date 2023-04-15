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
    private readonly GoodWeCom _communicator;
    private readonly ForecastService _forecastService;
    private readonly Gauge _gauge;
    private readonly Gauge _gaugePvForecast;
    private readonly IOptionsMonitor<Grid> _gridOptions;
    private readonly GoodWeInvStore _invStore;
    private readonly ILogger<PriceFetcher> _logger;
    private readonly PriceService _priceService;

    public PriceFetcher(PriceService priceService,
        GoodWeCom communicator,
        ForecastService forecastService,
        IOptionsMonitor<Grid> gridOptions,
        GoodWeInvStore invStore,
        ILogger<PriceFetcher> logger)
    {
        _priceService = priceService;
        _communicator = communicator;
        _forecastService = forecastService;
        _gridOptions = gridOptions;
        _invStore = invStore;
        _logger = logger;

        _gauge = Metrics.CreateGauge("Price", "Current price from OTE");
        _gaugePvForecast = Metrics.CreateGauge("PV_forecast", "Solar forecast", "part");
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

            await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken);
        }
    }

    private async Task HandleCurrentPrice(CancellationToken stoppingToken)
    {
        var price = _priceService.GetCurrentPrice();
        var pvForecast = _forecastService.GetCurrentForecast();
        var pForecast24 = _forecastService.GetForecast24();
        _gauge.Set(price);
        _gaugePvForecast.WithLabels("now").Set(pvForecast);
        _gaugePvForecast.WithLabels("24").Set(pForecast24);

        if (_gridOptions.CurrentValue.TradeEnergy)
        {
            var exportLimitDef = _gridOptions.CurrentValue.ExportLimit;
            var exportLimit = exportLimitDef ?? 10_000;

            if (price < 10)
            {
                exportLimit = Math.Min(exportLimit, (ushort)200);
                await InvokeMethod(g => _communicator.SetExportLimit(exportLimit, g, stoppingToken));
            }
            else if (exportLimitDef.HasValue)
            {
                await InvokeMethod(g => _communicator.SetExportLimit(exportLimit, g, stoppingToken));
            }
            else
            {
                await InvokeMethod(g => _communicator.DisableExportLimit(g, stoppingToken));
            }

            if ((price < -10 && pvForecast < 100)
                || (_priceService.IsMinPriceOfNight() && !_forecastService.PossibleFulfillBattery()))
                await InvokeMethod(g =>
                    _communicator.ForceBatteryCharge(g, _gridOptions.CurrentValue.ChargePower, stoppingToken));
            else
                await InvokeMethod(g => _communicator.StopForceBatteryCharge(g, stoppingToken));
        }
        else
        {
            _logger.LogInformation("Energy trading is disabled");
        }
    }

    private async Task InvokeMethod(Func<Definition, Task> invokeFunc)
    {
        await Task.WhenAll(_invStore.GoodWes.Select(invokeFunc));
    }
}
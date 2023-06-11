using ARPEG.Spot.Trader.Backgrounds;
using ARPEG.Spot.Trader.BitOutputs;
using ARPEG.Spot.Trader.BitOutputs.Handlers;
using ARPEG.Spot.Trader.Config;
using ARPEG.Spot.Trader.Config.BitOutputs;
using ARPEG.Spot.Trader.Services;
using ARPEG.Spot.Trader.Store;
using ARPEG.Spot.Trader.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader;

public static class AddServicesStartup
{
    public static void AddServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoodWe>(configuration.GetSection(nameof(GoodWe)));
        services.Configure<PvForecast>(configuration.GetSection(nameof(PvForecast)));
        services.RegisterSingletonConfig<ManualBatteryConfig>( configuration);

        services.AddSingleton<Root>(s =>
            new Root
            {
                ManualBatteryConfig = s.GetRequiredService<ManualBatteryConfig>(),
                BitOutput1 = s.GetRequiredService<BitOutput1>(),
                BitOutput2 = s.GetRequiredService<BitOutput2>(),
                BitOutput3 = s.GetRequiredService<BitOutput3>(),
                BitOutput4 = s.GetRequiredService<BitOutput4>(),
                BitOutput5 = s.GetRequiredService<BitOutput5>(),
                BitOutput6 = s.GetRequiredService<BitOutput6>(),
                BitOutput7 = s.GetRequiredService<BitOutput7>(),
                BitOutput8 = s.GetRequiredService<BitOutput8>(),
            }
        );

        services.AddSingleton<NowManualBatteryService>();

        services.RegisterBitController<BitOutput1>(configuration);
        services.RegisterBitController<BitOutput2>(configuration);
        services.RegisterBitController<BitOutput3>(configuration);
        services.RegisterBitController<BitOutput4>(configuration);
        services.RegisterBitController<BitOutput5>(configuration);
        services.RegisterBitController<BitOutput6>(configuration);
        services.RegisterBitController<BitOutput7>(configuration);
        services.RegisterBitController<BitOutput8>(configuration);

        services.AddTransient<IDataValueHandler, SocDataValueHandler>();
        services.AddTransient<IDataValueHandler, TemperatureValueHandler>();
        services.AddTransient<IDataValueHandler, PvPowerDataValueHandler>();
        services.AddTransient<IDataValueHandler, ExportDataValueHandler>();


        services.AddTransient<GoodWeFinder>();
        services.AddTransient<GoodWeCom>();
        services.AddSingleton<PriceService>();
        services.AddSingleton<ForecastService>();
        services.AddSingleton<IGoodWeInvStore, GoodWeInvStore>();
        services.AddSingleton<IConfigUpdater, ConfigUpdater>();

        services.AddHostedService<GoodWeFetcher>();
        services.AddHostedService<PriceFetcher>();

        Metrics.SuppressDefaultMetrics();
        services.AddMetricServer(options => { 
            options.Port = 12345; 
        });
    }

    private static void RegisterSingletonConfig<T>(this IServiceCollection services,
        IConfiguration configuration)
    where T : class
    {
        services.Configure<T>(configuration.GetSection(typeof(T).Name));
        services.AddSingleton<T>(s => s.GetRequiredService<IOptions<T>>().Value);
    }

    private static void RegisterBitController<TOptions>(this IServiceCollection services,
        IConfiguration configuration) where TOptions : BitOutputOptions
    {
        services.Configure<TOptions>(configuration.GetSection(typeof(TOptions).Name));
        services.AddSingleton<TOptions>(s => s.GetRequiredService<IOptions<TOptions>>().Value);
        services.AddSingleton<IBitController, BitController<TOptions>>();
    }
}

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
using Prometheus;
using TecoBridge.GoodWe;

namespace ARPEG.Spot.Trader
{
    public static class AddServicesStartup
    {
        public static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<Grid>(configuration.GetSection(nameof(Grid)));
            services.Configure<GoodWe>(configuration.GetSection(nameof(GoodWe)));
            services.Configure<PvForecast>(configuration.GetSection(nameof(PvForecast)));
            services.Configure<Root>(configuration);

            services.RegisterBitController<BitOutput1>(configuration);
            services.RegisterBitController<BitOutput2>(configuration);
            services.RegisterBitController<BitOutput3>(configuration);
            services.RegisterBitController<BitOutput4>(configuration);
            services.RegisterBitController<BitOutput5>(configuration);
            services.RegisterBitController<BitOutput6>(configuration);
            services.RegisterBitController<BitOutput7>(configuration);
            services.RegisterBitController<BitOutput8>(configuration);

            services.AddTransient<IDataValueHandler, SocDataValueHandler>();
            services.AddTransient<IDataValueHandler, PvPowerDataValueHandler>();
            services.AddTransient<IDataValueHandler, ExportDataValueHandler>();
           
            
            services.AddTransient<GoodWeFinder>();
            services.AddTransient<GoodWeCom>();
            services.AddSingleton<PriceService>();
            services.AddSingleton<ForecastService>();
            services.AddSingleton<IGoodWeInvStore, GoodWeInvStore>();
            services.AddSingleton<IConfigUpdater, ConfigUpdater>();

            services.AddHostedService<GoodWeFetcher>();
            // services.AddHostedService<PriceFetcher>();
            
            services.AddMetricServer(options =>
            {
                options.Port = 12345;
            });

        }

        private static void RegisterBitController<TOptions>(this IServiceCollection services, IConfiguration configuration) where TOptions : BitOutputOptions
        {
            services.Configure<TOptions>(configuration.GetSection(typeof(TOptions).Name));
            services.AddTransient<IBitController, BitController<TOptions>>();
        }

    }
}

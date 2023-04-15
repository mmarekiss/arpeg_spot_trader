﻿using ARPEG.Spot.Trader.Backgrounds;
using ARPEG.Spot.Trader.Config;
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
            
            
            services.AddTransient<GoodWeFinder>();
            services.AddTransient<GoodWeCom>();
            services.AddSingleton<PriceService>();
            services.AddSingleton<ForecastService>();
            services.AddSingleton<GoodWeInvStore>();

            services.AddHostedService<GoodWeFetcher>();
            services.AddHostedService<PriceFetcher>();
            
            services.AddMetricServer(options =>
            {
                options.Port = 12345;
            });

        }
        
    }
}

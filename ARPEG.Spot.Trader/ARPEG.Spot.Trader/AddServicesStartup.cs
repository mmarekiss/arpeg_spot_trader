using ARPEG.Spot.Trader.Backgrounds;
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
            services.AddTransient<GoodWeFinder>();
            services.AddTransient<GoodWeCom>();

            services.AddHostedService<GoodWeFetcher>();
            
            services.AddMetricServer(options =>
            {
                options.Port = 12345;
            });

        }
        
    }
}

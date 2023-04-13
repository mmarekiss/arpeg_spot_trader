using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ARPEG.Spot.Trader.Services;

public class PriceService
{
    private readonly ILogger<PriceService> _logger;

    private readonly double[] _todayPrices = new double[24];
    private readonly double[] _tommorowPrices = new double[24];

    public PriceService(ILogger<PriceService> logger)
    {
        _logger = logger;
    }

    private DateTime TodayFetched { get; set; } = DateTime.MinValue;

    private DateTime TommorowFetched { get; set; } = DateTime.MinValue;

    public double GetCurrentPrice()
    {
        var hour = DateTime.Now.Hour;
        _logger.LogInformation("Price for today at hour {i} is {p}", hour, _todayPrices[hour]);
        return _todayPrices[hour];
    }


    public async Task FetchPrices(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://www.ote-cr.cz/");

        var today = DateTime.UtcNow.Date;
        if (TodayFetched < today)
            if (await FetchDayPrices(client, _todayPrices, today, cancellationToken))
                TodayFetched = today;

        var tomorrow = today.AddDays(1);
        if (TommorowFetched < tomorrow)
            if (await FetchDayPrices(client, _tommorowPrices, today.AddDays(1), cancellationToken))
                TommorowFetched = tomorrow;
    }

    private async Task<bool> FetchDayPrices(HttpClient client,
        double[] prices,
        DateTime utcDate,
        CancellationToken cancellationToken)
    {
        var fetched = false;
        var path = $"/cs/kratkodobe-trhy/elektrina/denni-trh/@@chart-data?report_date={utcDate:yyyy-MM-dd}";

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(path, UriKind.Relative)
        };

        var result = await client.SendAsync(requestMessage, cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            var pricesDefinition =
                await result.Content.ReadFromJsonAsync<OtePrices>(JsonSerializerOptions.Default, cancellationToken);
            var priceArray = pricesDefinition?.data.dataLine.FirstOrDefault(x => x.type == "1");

            for (var i = 0; priceArray?.point?.Length > 1 && i < 24; i++)
            {
                prices[i] = priceArray.point[i].y;
                _logger.LogInformation("Price for day {d} at hour {i} is {p}", utcDate, i, prices[i]);
                fetched = true;
            }
        }

        return fetched;
    }
}
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;

namespace ARPEG.Spot.Trader.Services;

public class PriceService
{

    private readonly double[] _todayPrices = new double[24];
    private readonly double[] _tommorowPrices = new double[24];

    private DateTime LastFetchedDay { get; set; } = DateTime.MinValue;

    public double GetCurrentPrice()
        => _todayPrices[DateTime.UtcNow.Hour];


    public async Task FetchPrices(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://www.ote-cr.cz/");

        var today = DateTime.UtcNow.Date;
        if (LastFetchedDay == today)
        {
            Array.Copy(_tommorowPrices, _todayPrices, _todayPrices.Length);
        }
        else
        {
            await FetchDayPrices(client, _todayPrices, today, cancellationToken);
        }

        await FetchDayPrices(client, _tommorowPrices, today.AddDays(1), cancellationToken);
        
    }

    private async Task FetchDayPrices(HttpClient client,
        double[] prices,
        DateTime utcDate,
        CancellationToken cancellationToken)
    {
       
        if (LastFetchedDay < utcDate)
        {
            var path = $"/cs/kratkodobe-trhy/elektrina/denni-trh/@@chart-data?report_date={utcDate:yy-MM-dd}";

            HttpRequestMessage requestMessage = new HttpRequestMessage()
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

                for (int i = 0; priceArray?.point?.Length > 1 && i < 24; i++)
                {
                    prices[i] = priceArray.point[i].y;
                    LastFetchedDay = utcDate;
                }

            }
        }
    }
}
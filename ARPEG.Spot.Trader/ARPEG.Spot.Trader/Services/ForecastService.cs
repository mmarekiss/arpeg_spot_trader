using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ARPEG.Spot.Trader.Services;

public class ForecastService
{
    private readonly ILogger<ForecastService> _logger;

    private readonly int[] _forecastForToday = new int[24];
    private readonly int[] _forecastForTommorow = new int[24];

    private DateTime ForecastDay = DateTime.MinValue;

    public ForecastService(ILogger<ForecastService> logger)
    {
        _logger = logger;
    }

    public int GetCurrentForecast()
    {
        _logger.LogInformation("Current date time is {dt}", DateTime.Now);
        return _forecastForToday[DateTime.Now.Hour];
    }
    
    public int GetMaxForecast()
    {
        return _forecastForToday.Max();
    }


    public async Task GetForecast(CancellationToken cancellationToken)
    {
        if (ForecastDay < DateTime.UtcNow.Date)
        {
            await GetForecast("today", _forecastForToday, cancellationToken);
            await GetForecast("tommorow", _forecastForTommorow, cancellationToken);

            ForecastDay = DateTime.UtcNow.Date;
        }
    }

    private async Task GetForecast(string type, int[] array, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        var result = await client.GetAsync($"http://www.pvforecast.cz/api/?key=me8nvi&lat=49.418&lon=16.7&start{type}",
            cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            var values = await result.Content.ReadAsStringAsync(cancellationToken);
            var splitedValues = values?.Split("|");

            if (splitedValues?.Length == 25 && DateTime.TryParseExact(splitedValues[0], "yyyy-MM-dd",
                                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                                            && ForecastDay < date)
            {
                Array.Copy(splitedValues.Skip(1).Select(int.Parse).ToArray(), array, 24);
            }
        }
    }
}
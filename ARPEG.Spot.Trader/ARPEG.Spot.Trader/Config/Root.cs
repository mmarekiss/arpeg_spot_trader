namespace ARPEG.Spot.Trader.Config;

public class Root
{
    public required GoodWe GoodWe { get; init; }

    public required Grid Grid { get; init; }

    public required PvForecast PvForecast { get; init; }
}
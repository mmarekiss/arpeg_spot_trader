using ARPEG.Spot.Trader.Config.BitOutputs;

namespace ARPEG.Spot.Trader.Config;

public class Root
{
    public required GoodWe GoodWe { get; init; }

    public required Grid Grid { get; init; }

    public required PvForecast PvForecast { get; init; }

    public required BitOutput1 BitOutput1 { get; init; }

    public required BitOutput1 BitOutput2 { get; init; }

    public required BitOutput1 BitOutput3 { get; init; }

    public required BitOutput1 BitOutput4 { get; init; }

    public required BitOutput1 BitOutput5 { get; init; }

    public required BitOutput1 BitOutput6 { get; init; }

    public required BitOutput1 BitOutput7 { get; init; }

    public required BitOutput1 BitOutput8 { get; init; }
}